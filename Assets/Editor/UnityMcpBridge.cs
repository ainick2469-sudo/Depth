using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class UnityMcpBridge
{
    private const string DefaultServerUrl = "ws://127.0.0.1:6400/unity-bridge";
    private const string ServerUrlEditorPrefKey = "UnityMcpBridge.ServerUrl";
    private const int MaxConsoleEntries = 200;
    private const string DefaultAutoSaveSceneRelativePath = "Assets/Scenes/AutoSavedScene.unity";

    private static readonly ConcurrentQueue<BridgeMessage> MainThreadQueue = new ConcurrentQueue<BridgeMessage>();
    private static readonly List<ConsoleEntryPayload> ConsoleEntries = new List<ConsoleEntryPayload>();
    private static readonly SemaphoreSlim SendLock = new SemaphoreSlim(1, 1);

    private static CancellationTokenSource lifetimeCts;
    private static ClientWebSocket socket;
    private static bool isConnecting;
    private static DateTime nextReconnectAttemptUtc;
    private static string lastStatus = "Booting";

    static UnityMcpBridge()
    {
        lifetimeCts = new CancellationTokenSource();
        nextReconnectAttemptUtc = DateTime.UtcNow;

        Application.logMessageReceived += OnLogMessageReceived;
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.quitting += Shutdown;
        AssemblyReloadEvents.beforeAssemblyReload += Shutdown;

        QueueReconnect(TimeSpan.Zero, "Bridge initialized");
    }

    [MenuItem("Tools/Unity MCP Bridge/Reconnect")]
    private static void ReconnectNow()
    {
        QueueReconnect(TimeSpan.Zero, "Manual reconnect requested");
    }

    [MenuItem("Tools/Unity MCP Bridge/Log Status")]
    private static void LogStatus()
    {
        Debug.Log("[UnityMcpBridge] " + lastStatus);
    }

    private static void OnEditorUpdate()
    {
        ProcessMainThreadQueue();

        if (isConnecting || EditorApplication.isCompiling)
        {
            return;
        }

        if (socket != null && socket.State == WebSocketState.Open)
        {
            return;
        }

        if (DateTime.UtcNow < nextReconnectAttemptUtc)
        {
            return;
        }

        _ = ConnectAsync();
    }

    private static void ProcessMainThreadQueue()
    {
        while (MainThreadQueue.TryDequeue(out var message))
        {
            _ = HandleCommandOnMainThreadAsync(message);
        }
    }

    private static async Task ConnectAsync()
    {
        isConnecting = true;
        lastStatus = "Connecting to Node bridge";

        DisposeSocket();
        socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

        try
        {
            var serverUrl = EditorPrefs.GetString(ServerUrlEditorPrefKey, DefaultServerUrl);
            await socket.ConnectAsync(new Uri(serverUrl), lifetimeCts.Token);
            lastStatus = "Connected to " + serverUrl;

            var helloPayload = new HelloPayload
            {
                productName = Application.productName,
                editorVersion = Application.unityVersion,
                projectPath = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty,
                activeScene = SceneManager.GetActiveScene().name,
                activeScenePath = SceneManager.GetActiveScene().path,
                isPlaying = EditorApplication.isPlaying,
                isBatchMode = Application.isBatchMode,
                timestampUtc = DateTime.UtcNow.ToString("o")
            };

            await SendMessageAsync(new BridgeMessage
            {
                type = "hello",
                payloadJson = JsonUtility.ToJson(helloPayload)
            });

            _ = ReceiveLoopAsync(socket, lifetimeCts.Token);
        }
        catch (Exception ex)
        {
            lastStatus = "Connect failed: " + ex.Message;
            QueueReconnect(TimeSpan.FromSeconds(3), lastStatus);
            DisposeSocket();
        }
        finally
        {
            isConnecting = false;
        }
    }

    private static async Task ReceiveLoopAsync(ClientWebSocket connectedSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var builder = new StringBuilder();

        try
        {
            while (!cancellationToken.IsCancellationRequested && connectedSocket.State == WebSocketState.Open)
            {
                builder.Length = 0;

                WebSocketReceiveResult receiveResult;
                do
                {
                    receiveResult = await connectedSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        lastStatus = "Node bridge closed the socket";
                        QueueReconnect(TimeSpan.FromSeconds(2), lastStatus);
                        return;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
                } while (!receiveResult.EndOfMessage);

                var json = builder.ToString();
                var message = JsonUtility.FromJson<BridgeMessage>(json);
                if (message == null)
                {
                    continue;
                }

                if (message.type == "command")
                {
                    MainThreadQueue.Enqueue(message);
                }
                else if (message.type == "helloAck")
                {
                    lastStatus = "Node bridge handshake acknowledged";
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            lastStatus = "Receive loop failed: " + ex.Message;
            QueueReconnect(TimeSpan.FromSeconds(3), lastStatus);
        }
        finally
        {
            DisposeSocket();
        }
    }

    private static async Task HandleCommandOnMainThreadAsync(BridgeMessage message)
    {
        try
        {
            switch (message.action)
            {
                case "getEditorState":
                    var editorState = GetEditorState();
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(editorState), null);
                    return;

                case "spawnGameAsset":
                    var spawnPayload = JsonUtility.FromJson<SpawnGameAssetPayload>(message.payloadJson);
                    var spawnResult = SpawnGameAsset(spawnPayload);
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(spawnResult), null);
                    return;

                case "createPrimitiveObject":
                    var createPayload = JsonUtility.FromJson<CreatePrimitiveObjectPayload>(message.payloadJson);
                    var createResult = CreatePrimitiveObject(createPayload);
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(createResult), null);
                    return;

                case "listSceneObjects":
                    var listPayload = JsonUtility.FromJson<ListSceneObjectsPayload>(message.payloadJson);
                    var listResult = ListSceneObjects(listPayload);
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(listResult), null);
                    return;

                case "selectSceneObject":
                    var selectPayload = JsonUtility.FromJson<SelectSceneObjectPayload>(message.payloadJson);
                    var selectResult = SelectSceneObject(selectPayload);
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(selectResult), null);
                    return;

                case "setGameObjectTransform":
                    var transformPayload = JsonUtility.FromJson<SetGameObjectTransformPayload>(message.payloadJson);
                    var transformResult = SetGameObjectTransform(transformPayload);
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(transformResult), null);
                    return;

                case "executeMenuItem":
                    var menuPayload = JsonUtility.FromJson<ExecuteMenuItemPayload>(message.payloadJson);
                    var menuResult = ExecuteMenuItem(menuPayload);
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(menuResult), null);
                    return;

                case "saveActiveScene":
                    var savePayload = JsonUtility.FromJson<SaveActiveScenePayload>(message.payloadJson);
                    var saveResult = SaveActiveScene(savePayload);
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(saveResult), null);
                    return;

                case "managePlayMode":
                    var playModePayload = JsonUtility.FromJson<ManagePlayModePayload>(message.payloadJson);
                    var playModeResult = ManagePlayMode(playModePayload);
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(playModeResult), null);
                    return;

                case "readConsole":
                    var consolePayload = JsonUtility.FromJson<ReadConsolePayload>(message.payloadJson);
                    var consoleResult = ReadConsole(consolePayload);
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(consoleResult), null);
                    return;

                case "captureScenePreview":
                    var previewPayload = JsonUtility.FromJson<CaptureScenePreviewPayload>(message.payloadJson);
                    var previewResult = CaptureScenePreview(previewPayload);
                    await SendResultAsync(message.requestId, message.action, true, JsonUtility.ToJson(previewResult), null);
                    return;

                default:
                    await SendResultAsync(
                        message.requestId,
                        message.action,
                        false,
                        string.Empty,
                        "Unsupported Unity bridge action '" + message.action + "'.");
                    return;
            }
        }
        catch (Exception ex)
        {
            await SendResultAsync(message.requestId, message.action, false, string.Empty, ex.ToString());
        }
    }

    private static SpawnGameAssetResult SpawnGameAsset(SpawnGameAssetPayload payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.assetName))
        {
            throw new InvalidOperationException("SpawnGameAsset payload was missing a valid assetName.");
        }

        var createdObject = CreatePrimitiveGameObject(
            PrimitiveType.Cube,
            payload.assetName,
            new Vector3(payload.x, payload.y, payload.z),
            true);

        var activeScene = SceneManager.GetActiveScene();
        return new SpawnGameAssetResult
        {
            createdObjectName = createdObject.name,
            activeScene = activeScene.name,
            selectionName = Selection.activeGameObject != null ? Selection.activeGameObject.name : string.Empty,
            position = new Vector3Payload
            {
                x = createdObject.transform.position.x,
                y = createdObject.transform.position.y,
                z = createdObject.transform.position.z
            }
        };
    }

    private static EditorStatePayload GetEditorState()
    {
        var activeScene = SceneManager.GetActiveScene();
        var consoleEntryCount = 0;

        lock (ConsoleEntries)
        {
            consoleEntryCount = ConsoleEntries.Count;
        }

        return new EditorStatePayload
        {
            projectPath = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty,
            activeSceneName = activeScene.name ?? string.Empty,
            activeScenePath = activeScene.path ?? string.Empty,
            isSceneDirty = activeScene.IsValid() && activeScene.isDirty,
            isPlaying = EditorApplication.isPlaying,
            isPlayingOrWillChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode,
            isCompiling = EditorApplication.isCompiling,
            selectionName = Selection.activeGameObject != null ? Selection.activeGameObject.name : string.Empty,
            selectionPath = BuildHierarchyPath(Selection.activeTransform),
            rootObjectCount = activeScene.IsValid() && activeScene.isLoaded ? activeScene.rootCount : 0,
            consoleEntryCount = consoleEntryCount
        };
    }

    private static CreatePrimitiveObjectResult CreatePrimitiveObject(CreatePrimitiveObjectPayload payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.objectName))
        {
            throw new InvalidOperationException("CreatePrimitiveObject payload was missing objectName.");
        }

        var primitiveType = ParsePrimitiveType(payload.primitiveType);
        var createdObject = CreatePrimitiveGameObject(
            primitiveType,
            payload.objectName,
            new Vector3(payload.x, payload.y, payload.z),
            payload.selectAfterCreate);

        var snapshot = SnapshotTransform(createdObject.transform);
        return new CreatePrimitiveObjectResult
        {
            primitiveType = primitiveType.ToString(),
            name = snapshot.name,
            path = snapshot.path,
            activeSelf = snapshot.activeSelf,
            position = snapshot.position,
            localPosition = snapshot.localPosition,
            rotationEuler = snapshot.rotationEuler,
            localScale = snapshot.localScale
        };
    }

    private static ListSceneObjectsResult ListSceneObjects(ListSceneObjectsPayload payload)
    {
        var includeInactive = payload == null || payload.includeInactive;
        var activeScene = SceneManager.GetActiveScene();
        var roots = activeScene.GetRootGameObjects();
        var objects = new List<SceneObjectPayload>();

        foreach (var root in roots)
        {
            CollectSceneObjects(root.transform, root.name, includeInactive, objects);
        }

        return new ListSceneObjectsResult
        {
            activeScene = activeScene.name,
            objects = objects.ToArray()
        };
    }

    private static void CollectSceneObjects(
        Transform current,
        string currentPath,
        bool includeInactive,
        List<SceneObjectPayload> results)
    {
        if (includeInactive || current.gameObject.activeSelf)
        {
            results.Add(new SceneObjectPayload
            {
                name = current.name,
                path = currentPath,
                activeSelf = current.gameObject.activeSelf,
                position = new Vector3Payload
                {
                    x = current.position.x,
                    y = current.position.y,
                    z = current.position.z
                }
            });
        }

        for (var childIndex = 0; childIndex < current.childCount; childIndex++)
        {
            var child = current.GetChild(childIndex);
            CollectSceneObjects(child, currentPath + "/" + child.name, includeInactive, results);
        }
    }

    private static ReadConsoleResult ReadConsole(ReadConsolePayload payload)
    {
        var maxEntries = payload != null && payload.maxEntries > 0 ? payload.maxEntries : 20;

        lock (ConsoleEntries)
        {
            var count = Math.Min(maxEntries, ConsoleEntries.Count);
            var slice = ConsoleEntries.GetRange(0, count);
            return new ReadConsoleResult
            {
                logs = slice.ToArray()
            };
        }
    }

    private static TransformSnapshotPayload SelectSceneObject(SelectSceneObjectPayload payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.objectPathOrName))
        {
            throw new InvalidOperationException("SelectSceneObject requires objectPathOrName.");
        }

        var targetObject = FindSceneObject(payload.objectPathOrName);
        if (targetObject == null)
        {
            throw new InvalidOperationException("Unable to find scene object '" + payload.objectPathOrName + "'.");
        }

        Selection.activeGameObject = targetObject;
        EditorGUIUtility.PingObject(targetObject);

        if (payload.frameInSceneView && SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        return SnapshotTransform(targetObject.transform);
    }

    private static TransformSnapshotPayload SetGameObjectTransform(SetGameObjectTransformPayload payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.objectPathOrName))
        {
            throw new InvalidOperationException("SetGameObjectTransform requires objectPathOrName.");
        }

        var targetObject = FindSceneObject(payload.objectPathOrName);
        if (targetObject == null)
        {
            throw new InvalidOperationException("Unable to find scene object '" + payload.objectPathOrName + "'.");
        }

        Undo.RecordObject(targetObject.transform, "Set MCP GameObject Transform");

        if (payload.position != null)
        {
            var nextPosition = ToVector3(payload.position);
            if (payload.useLocalSpace)
            {
                targetObject.transform.localPosition = nextPosition;
            }
            else
            {
                targetObject.transform.position = nextPosition;
            }
        }

        if (payload.rotationEuler != null)
        {
            var nextRotation = ToVector3(payload.rotationEuler);
            if (payload.useLocalSpace)
            {
                targetObject.transform.localEulerAngles = nextRotation;
            }
            else
            {
                targetObject.transform.eulerAngles = nextRotation;
            }
        }

        if (payload.localScale != null)
        {
            targetObject.transform.localScale = ToVector3(payload.localScale);
        }

        EditorSceneManager.MarkSceneDirty(targetObject.scene);
        return SnapshotTransform(targetObject.transform);
    }

    private static ExecuteMenuItemResult ExecuteMenuItem(ExecuteMenuItemPayload payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.menuPath))
        {
            throw new InvalidOperationException("ExecuteMenuItem requires menuPath.");
        }

        var succeeded = EditorApplication.ExecuteMenuItem(payload.menuPath);
        return new ExecuteMenuItemResult
        {
            menuPath = payload.menuPath,
            succeeded = succeeded,
            editorState = GetEditorState()
        };
    }

    private static SaveActiveSceneResult SaveActiveScene(SaveActiveScenePayload payload)
    {
        var activeScene = EnsureActiveSceneLoaded();
        var targetScenePath = !string.IsNullOrWhiteSpace(payload != null ? payload.scenePath : null)
            ? NormalizeSceneAssetPath(payload.scenePath)
            : (!string.IsNullOrWhiteSpace(activeScene.path) ? activeScene.path : DefaultAutoSaveSceneRelativePath);

        EnsureProjectRelativeDirectory(targetScenePath);

        var saved = EditorSceneManager.SaveScene(activeScene, targetScenePath, true);
        if (!saved)
        {
            throw new InvalidOperationException("Unity did not save scene '" + targetScenePath + "'.");
        }

        AssetDatabase.Refresh();

        return new SaveActiveSceneResult
        {
            sceneName = !string.IsNullOrWhiteSpace(SceneManager.GetActiveScene().name)
                ? SceneManager.GetActiveScene().name
                : Path.GetFileNameWithoutExtension(targetScenePath),
            scenePath = !string.IsNullOrWhiteSpace(SceneManager.GetActiveScene().path)
                ? SceneManager.GetActiveScene().path
                : targetScenePath,
            savedAtUtc = DateTime.UtcNow.ToString("o")
        };
    }

    private static ManagePlayModeResult ManagePlayMode(ManagePlayModePayload payload)
    {
        var requestedMode = payload != null && !string.IsNullOrWhiteSpace(payload.mode)
            ? payload.mode.ToLowerInvariant()
            : "query";

        switch (requestedMode)
        {
            case "enter":
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = true;
                }
                break;

            case "exit":
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }
                break;

            case "toggle":
                EditorApplication.isPlaying = !EditorApplication.isPlaying;
                break;

            case "query":
                break;

            default:
                throw new InvalidOperationException("Unsupported play mode request '" + requestedMode + "'.");
        }

        return new ManagePlayModeResult
        {
            requestedMode = requestedMode,
            editorState = GetEditorState()
        };
    }

    private static CaptureScenePreviewResult CaptureScenePreview(CaptureScenePreviewPayload payload)
    {
        var width = payload != null && payload.width >= 64 ? payload.width : 512;
        var height = payload != null && payload.height >= 64 ? payload.height : 512;
        var projectPath = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
        var focusObject = ResolveFocusObject(payload);
        var focusName = focusObject != null ? focusObject.name : string.Empty;

        var outputPath = payload != null ? payload.outputPath : null;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(
                projectPath,
                "Temp",
                "UnityMcpBridge",
                "scene-preview-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".png");
        }

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var cameraObject = new GameObject("UnityMcpPreviewCamera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.16f, 0.18f, 0.23f);
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 1000f;

        var targetPosition = focusObject != null ? focusObject.transform.position : Vector3.zero;
        var cameraOffset = new Vector3(4f, 3f, -4f);

        if (focusObject != null)
        {
            var renderer = focusObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                var bounds = renderer.bounds;
                targetPosition = bounds.center;
                var largestExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                var distance = Mathf.Max(3f, largestExtent * 4f);
                cameraOffset = new Vector3(distance, distance * 0.75f, -distance);
            }
        }

        camera.transform.position = targetPosition + cameraOffset;
        camera.transform.LookAt(targetPosition);

        var previousRenderTexture = RenderTexture.active;
        var renderTexture = new RenderTexture(width, height, 24);
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previousRenderTexture;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        return new CaptureScenePreviewResult
        {
            focusedObjectName = focusName,
            outputPath = outputPath,
            width = width,
            height = height
        };
    }

    private static GameObject CreatePrimitiveGameObject(
        PrimitiveType primitiveType,
        string objectName,
        Vector3 worldPosition,
        bool selectAfterCreate)
    {
        var activeScene = EnsureActiveSceneLoaded();
        var createdObject = GameObject.CreatePrimitive(primitiveType);
        Undo.RegisterCreatedObjectUndo(createdObject, "Create MCP Primitive Object");
        SceneManager.MoveGameObjectToScene(createdObject, activeScene);

        createdObject.name = objectName;
        createdObject.transform.position = worldPosition;

        if (selectAfterCreate)
        {
            Selection.activeGameObject = createdObject;
            EditorGUIUtility.PingObject(createdObject);

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        return createdObject;
    }

    private static Scene EnsureActiveSceneLoaded()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            activeScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        return activeScene;
    }

    private static PrimitiveType ParsePrimitiveType(string primitiveTypeName)
    {
        if (string.IsNullOrWhiteSpace(primitiveTypeName))
        {
            return PrimitiveType.Cube;
        }

        switch (primitiveTypeName.Trim().ToLowerInvariant())
        {
            case "cube":
                return PrimitiveType.Cube;
            case "sphere":
                return PrimitiveType.Sphere;
            case "capsule":
                return PrimitiveType.Capsule;
            case "cylinder":
                return PrimitiveType.Cylinder;
            case "plane":
                return PrimitiveType.Plane;
            case "quad":
                return PrimitiveType.Quad;
            default:
                throw new InvalidOperationException("Unsupported primitive type '" + primitiveTypeName + "'.");
        }
    }

    private static Vector3 ToVector3(Vector3Payload payload)
    {
        return new Vector3(payload.x, payload.y, payload.z);
    }

    private static TransformSnapshotPayload SnapshotTransform(Transform target)
    {
        return new TransformSnapshotPayload
        {
            name = target.name,
            path = BuildHierarchyPath(target),
            activeSelf = target.gameObject.activeSelf,
            position = new Vector3Payload
            {
                x = target.position.x,
                y = target.position.y,
                z = target.position.z
            },
            localPosition = new Vector3Payload
            {
                x = target.localPosition.x,
                y = target.localPosition.y,
                z = target.localPosition.z
            },
            rotationEuler = new Vector3Payload
            {
                x = target.eulerAngles.x,
                y = target.eulerAngles.y,
                z = target.eulerAngles.z
            },
            localScale = new Vector3Payload
            {
                x = target.localScale.x,
                y = target.localScale.y,
                z = target.localScale.z
            }
        };
    }

    private static string BuildHierarchyPath(Transform target)
    {
        if (target == null)
        {
            return string.Empty;
        }

        if (target.parent == null)
        {
            return target.name;
        }

        return BuildHierarchyPath(target.parent) + "/" + target.name;
    }

    private static GameObject FindSceneObject(string objectPathOrName)
    {
        if (string.IsNullOrWhiteSpace(objectPathOrName))
        {
            return Selection.activeGameObject;
        }

        var activeScene = EnsureActiveSceneLoaded();
        var roots = activeScene.GetRootGameObjects();

        if (objectPathOrName.Contains("/"))
        {
            var byPath = FindByPath(roots, objectPathOrName);
            if (byPath != null)
            {
                return byPath.gameObject;
            }
        }

        foreach (var root in roots)
        {
            var match = FindByName(root.transform, objectPathOrName);
            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static Transform FindByPath(GameObject[] roots, string hierarchyPath)
    {
        var segments = hierarchyPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        foreach (var root in roots)
        {
            if (!string.Equals(root.name, segments[0], StringComparison.Ordinal))
            {
                continue;
            }

            Transform current = root.transform;
            var foundAllSegments = true;

            for (var index = 1; index < segments.Length; index++)
            {
                var nextChild = current.Find(segments[index]);
                if (nextChild == null)
                {
                    foundAllSegments = false;
                    break;
                }

                current = nextChild;
            }

            if (foundAllSegments)
            {
                return current;
            }
        }

        return null;
    }

    private static Transform FindByName(Transform current, string objectName)
    {
        if (string.Equals(current.name, objectName, StringComparison.Ordinal))
        {
            return current;
        }

        for (var childIndex = 0; childIndex < current.childCount; childIndex++)
        {
            var match = FindByName(current.GetChild(childIndex), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static string NormalizeSceneAssetPath(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            return DefaultAutoSaveSceneRelativePath;
        }

        var normalized = scenePath.Replace("\\", "/");
        if (!normalized.StartsWith("Assets/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "scenePath must be a Unity asset path under Assets/, for example Assets/Scenes/MyScene.unity.");
        }

        return normalized;
    }

    private static void EnsureProjectRelativeDirectory(string unityAssetPath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
        var relativeDirectory = Path.GetDirectoryName(unityAssetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (string.IsNullOrWhiteSpace(relativeDirectory))
        {
            return;
        }

        var absoluteDirectory = Path.Combine(projectRoot, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);
    }

    private static GameObject ResolveFocusObject(CaptureScenePreviewPayload payload)
    {
        if (payload != null && !string.IsNullOrWhiteSpace(payload.objectName))
        {
            var found = FindSceneObject(payload.objectName);
            if (found != null)
            {
                return found;
            }
        }

        if (Selection.activeGameObject != null)
        {
            return Selection.activeGameObject;
        }

        var activeScene = SceneManager.GetActiveScene();
        var roots = activeScene.GetRootGameObjects();
        return roots.Length > 0 ? roots[0] : null;
    }

    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        var entry = new ConsoleEntryPayload
        {
            type = type.ToString(),
            message = condition ?? string.Empty,
            stackTrace = stackTrace ?? string.Empty,
            createdAtUtc = DateTime.UtcNow.ToString("o")
        };

        lock (ConsoleEntries)
        {
            ConsoleEntries.Insert(0, entry);
            if (ConsoleEntries.Count > MaxConsoleEntries)
            {
                ConsoleEntries.RemoveRange(MaxConsoleEntries, ConsoleEntries.Count - MaxConsoleEntries);
            }
        }
    }

    private static async Task SendResultAsync(
        string requestId,
        string action,
        bool success,
        string payloadJson,
        string error)
    {
        await SendMessageAsync(new BridgeMessage
        {
            type = "result",
            requestId = requestId,
            action = action,
            success = success,
            error = error,
            payloadJson = payloadJson
        });
    }

    private static async Task SendMessageAsync(BridgeMessage message)
    {
        if (socket == null || socket.State != WebSocketState.Open || lifetimeCts == null)
        {
            return;
        }

        var json = JsonUtility.ToJson(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        await SendLock.WaitAsync();
        try
        {
            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                lifetimeCts.Token);
        }
        finally
        {
            SendLock.Release();
        }
    }

    private static void QueueReconnect(TimeSpan delay, string reason)
    {
        nextReconnectAttemptUtc = DateTime.UtcNow + delay;
        lastStatus = reason;
    }

    private static void DisposeSocket()
    {
        if (socket == null)
        {
            return;
        }

        try
        {
            socket.Dispose();
        }
        catch
        {
        }

        socket = null;
    }

    private static void Shutdown()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.quitting -= Shutdown;
        AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;

        if (lifetimeCts != null)
        {
            if (!lifetimeCts.IsCancellationRequested)
            {
                lifetimeCts.Cancel();
            }

            lifetimeCts.Dispose();
            lifetimeCts = null;
        }

        DisposeSocket();
    }

    [Serializable]
    private class BridgeMessage
    {
        public string type;
        public string requestId;
        public string action;
        public bool success;
        public string error;
        public string payloadJson;
    }

    [Serializable]
    private class HelloPayload
    {
        public string productName;
        public string editorVersion;
        public string projectPath;
        public string activeScene;
        public string activeScenePath;
        public bool isPlaying;
        public bool isBatchMode;
        public string timestampUtc;
    }

    [Serializable]
    private class EditorStatePayload
    {
        public string projectPath;
        public string activeSceneName;
        public string activeScenePath;
        public bool isSceneDirty;
        public bool isPlaying;
        public bool isPlayingOrWillChangePlaymode;
        public bool isCompiling;
        public string selectionName;
        public string selectionPath;
        public int rootObjectCount;
        public int consoleEntryCount;
    }

    [Serializable]
    private class SpawnGameAssetPayload
    {
        public string assetName;
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    private class SpawnGameAssetResult
    {
        public string createdObjectName;
        public string activeScene;
        public string selectionName;
        public Vector3Payload position;
    }

    [Serializable]
    private class CreatePrimitiveObjectPayload
    {
        public string primitiveType;
        public string objectName;
        public float x;
        public float y;
        public float z;
        public bool selectAfterCreate = true;
    }

    [Serializable]
    private class CreatePrimitiveObjectResult : TransformSnapshotPayload
    {
        public string primitiveType;
    }

    [Serializable]
    private class ListSceneObjectsPayload
    {
        public bool includeInactive = true;
    }

    [Serializable]
    private class ListSceneObjectsResult
    {
        public string activeScene;
        public SceneObjectPayload[] objects;
    }

    [Serializable]
    private class ReadConsolePayload
    {
        public int maxEntries = 20;
    }

    [Serializable]
    private class ReadConsoleResult
    {
        public ConsoleEntryPayload[] logs;
    }

    [Serializable]
    private class SelectSceneObjectPayload
    {
        public string objectPathOrName;
        public bool frameInSceneView = true;
    }

    [Serializable]
    private class SetGameObjectTransformPayload
    {
        public string objectPathOrName;
        public bool useLocalSpace;
        public Vector3Payload position;
        public Vector3Payload rotationEuler;
        public Vector3Payload localScale;
    }

    [Serializable]
    private class ExecuteMenuItemPayload
    {
        public string menuPath;
    }

    [Serializable]
    private class ExecuteMenuItemResult
    {
        public string menuPath;
        public bool succeeded;
        public EditorStatePayload editorState;
    }

    [Serializable]
    private class SaveActiveScenePayload
    {
        public string scenePath;
    }

    [Serializable]
    private class SaveActiveSceneResult
    {
        public string sceneName;
        public string scenePath;
        public string savedAtUtc;
    }

    [Serializable]
    private class ManagePlayModePayload
    {
        public string mode = "query";
    }

    [Serializable]
    private class ManagePlayModeResult
    {
        public string requestedMode;
        public EditorStatePayload editorState;
    }

    [Serializable]
    private class CaptureScenePreviewPayload
    {
        public string objectName;
        public int width = 512;
        public int height = 512;
        public string outputPath;
    }

    [Serializable]
    private class CaptureScenePreviewResult
    {
        public string focusedObjectName;
        public string outputPath;
        public int width;
        public int height;
    }

    [Serializable]
    private class SceneObjectPayload
    {
        public string name;
        public string path;
        public bool activeSelf;
        public Vector3Payload position;
    }

    [Serializable]
    private class TransformSnapshotPayload
    {
        public string name;
        public string path;
        public bool activeSelf;
        public Vector3Payload position;
        public Vector3Payload localPosition;
        public Vector3Payload rotationEuler;
        public Vector3Payload localScale;
    }

    [Serializable]
    private class Vector3Payload
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    private class ConsoleEntryPayload
    {
        public string type;
        public string message;
        public string stackTrace;
        public string createdAtUtc;
    }
}
