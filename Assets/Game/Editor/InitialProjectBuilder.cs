using System.IO;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.Progression;
using FrontierDepths.UI;
using FrontierDepths.World;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FrontierDepths.Editor
{
    public static class InitialProjectBuilder
    {
        [MenuItem("FrontierDepths/Build/Generate Initial Content")]
        public static void GenerateInitialContent()
        {
            EnsureDefinitions();
            CreateBootstrapScene();
            CreateMainMenuScene();
            CreateTownHubScene();
            CreateDungeonScene();
            CreateSandboxScene();
            CreateNetPlaytestScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateInitialContentBatch()
        {
            GenerateInitialContent();
            EditorApplication.Exit(0);
        }

        private static void EnsureDefinitions()
        {
            EnsureFolder("Assets/Resources/Definitions");
            EnsureFolder("Assets/Resources/Definitions/Shops");
            EnsureFolder("Assets/Resources/Definitions/World");
            EnsureFolder("Assets/Resources/Definitions/Combat");

            CreateShop(
                "Assets/Resources/Definitions/Shops/Quartermaster.asset",
                "shop.quartermaster",
                TownServiceType.Quartermaster,
                "Quartermaster",
                "Stock up before the dark starts biting back.",
                new[]
                {
                    new ShopOffer
                    {
                        offerId = "offer.town_sigil",
                        displayName = "Town Sigil",
                        description = "One emergency return to town. Carry cap: 1.",
                        cost = 125,
                        purchaseLimit = 1,
                        action = ShopOfferAction.BuyPortalSigil,
                        rewardId = "item.town_sigil"
                    }
                });

            CreateShop(
                "Assets/Resources/Definitions/Shops/Blacksmith.asset",
                "shop.blacksmith",
                TownServiceType.Blacksmith,
                "Blacksmith",
                "Steel, powder, and small miracles. Pick one.",
                new[]
                {
                    new ShopOffer
                    {
                        offerId = "offer.weapon.rifle",
                        displayName = "Unlock Frontier Rifle",
                        description = "Prototype long gun for later combat milestones.",
                        cost = 90,
                        purchaseLimit = 1,
                        action = ShopOfferAction.UnlockWeapon,
                        rewardId = "weapon.frontier_rifle"
                    }
                });

            CreateShop(
                "Assets/Resources/Definitions/Shops/CurioDealer.asset",
                "shop.curio",
                TownServiceType.CurioDealer,
                "Curio Dealer",
                "Sometimes the junk remembers older gods than you do.",
                new[]
                {
                    new ShopOffer
                    {
                        offerId = "offer.curio_dust",
                        displayName = "Dusty Charm",
                        description = "Gain one Curio Dust for future relic rolls.",
                        cost = 45,
                        purchaseLimit = 3,
                        action = ShopOfferAction.GainCurioDust,
                        rewardId = "curio.dust"
                    }
                });

            CreateShop(
                "Assets/Resources/Definitions/Shops/BountyBoard.asset",
                "shop.bounty_board",
                TownServiceType.BountyBoard,
                "Bounty Board",
                "Work notices pinned over older blood stains.",
                new[]
                {
                    new ShopOffer
                    {
                        offerId = "offer.bounty.lantern_shrine",
                        displayName = "Map the Lantern Shrine",
                        description = "Complete a landmark floor and bring back the route.",
                        cost = 0,
                        purchaseLimit = 1,
                        action = ShopOfferAction.AcceptBounty,
                        rewardId = "bounty.lantern_shrine"
                    }
                });

            CreateShop(
                "Assets/Resources/Definitions/Shops/Stash.asset",
                "shop.stash",
                TownServiceType.Stash,
                "Heirloom Chest",
                "A place for one thing too valuable to lose.",
                new[]
                {
                    new ShopOffer
                    {
                        offerId = "offer.heirloom.old_map",
                        displayName = "Store Old Map",
                        description = "Placeholder heirloom slot for persistent run tech.",
                        cost = 0,
                        purchaseLimit = 1,
                        action = ShopOfferAction.StoreHeirloom,
                        rewardId = "heirloom.old_map"
                    }
                });

            CreateAssetIfMissing(
                "Assets/Resources/Definitions/World/Theme_FrontierTown.asset",
                () => MakeTheme("theme.frontier_town", "Frontier Town", new Color(0.63f, 0.44f, 0.25f), new Color(0.95f, 0.75f, 0.34f)));
            CreateAssetIfMissing(
                "Assets/Resources/Definitions/World/Theme_CorruptedQuarry.asset",
                () => MakeTheme("theme.corrupted_quarry", "Corrupted Quarry", new Color(0.35f, 0.19f, 0.2f), new Color(0.82f, 0.35f, 0.42f)));
            CreateAssetIfMissing(
                "Assets/Resources/Definitions/World/Theme_BuriedTemple.asset",
                () => MakeTheme("theme.buried_temple", "Buried Temple", new Color(0.35f, 0.33f, 0.18f), new Color(0.36f, 0.72f, 0.68f)));

            CreateAssetIfMissing(
                "Assets/Resources/Definitions/World/FloorBand_FrontierMine.asset",
                () => MakeFloorBand("floorband.frontier_mine", "Frontier Mine", 1, 10, "theme.frontier_town"));
            CreateAssetIfMissing(
                "Assets/Resources/Definitions/World/FloorBand_CorruptedQuarry.asset",
                () => MakeFloorBand("floorband.corrupted_quarry", "Corrupted Quarry", 11, 20, "theme.corrupted_quarry"));
            CreateAssetIfMissing(
                "Assets/Resources/Definitions/World/FloorBand_BuriedTemple.asset",
                () => MakeFloorBand("floorband.buried_temple", "Buried Temple", 21, 40, "theme.buried_temple"));

            CreateAssetIfMissing(
                "Assets/Resources/Definitions/World/Chapter_FrontierDescent.asset",
                () => MakeChapter("chapter.frontier_descent", "Frontier Descent", 1, 20, "Linger too long and the mine starts listening back."));
            CreateAssetIfMissing(
                "Assets/Resources/Definitions/World/Chapter_BuriedTemple.asset",
                () => MakeChapter("chapter.buried_temple", "Buried Temple", 21, 40, "Temple depths twist space and intention."));

            CreateAssetIfMissing(
                "Assets/Resources/Definitions/Combat/Weapon_FrontierRevolver.asset",
                () =>
                {
                    WeaponDefinition asset = ScriptableObject.CreateInstance<WeaponDefinition>();
                    asset.weaponId = "weapon.frontier_revolver";
                    asset.displayName = "Frontier Revolver";
                    asset.description = "Starter sidearm for the first descent.";
                    return asset;
                });

            CreateAssetIfMissing(
                "Assets/Resources/Definitions/Combat/Upgrade_EmberRound.asset",
                () =>
                {
                    UpgradeDefinition asset = ScriptableObject.CreateInstance<UpgradeDefinition>();
                    asset.upgradeId = "upgrade.fire_ember";
                    asset.displayName = "Ember Round";
                    asset.description = "Shots light enemies for a short burn.";
                    asset.primaryTag = GameplayTag.Fire;
                    asset.modifier = new StatModifier { statId = "damage", additive = 3f, multiplier = 1f };
                    return asset;
                });

            CreateAssetIfMissing(
                "Assets/Resources/Definitions/Combat/Enemy_MireHound.asset",
                () =>
                {
                    EnemyDefinition asset = ScriptableObject.CreateInstance<EnemyDefinition>();
                    asset.enemyId = "enemy.mire_hound";
                    asset.displayName = "Mire Hound";
                    asset.maxHealth = 55f;
                    asset.moveSpeed = 4.5f;
                    return asset;
                });
        }

        private static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Bootstrap");
            root.AddComponent<GameBootstrap>();
            SaveScene(scene, GameSceneCatalog.GetPath(GameSceneId.Bootstrap));
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLight();
            CreateGround("MenuGround", new Vector3(0f, -2f, 0f), new Vector3(80f, 1f, 80f), new Color(0.12f, 0.11f, 0.1f));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Image background = CreateImage("Background", canvasObject.transform, Vector2.zero, new Color(0.05f, 0.05f, 0.06f, 0.92f));
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            Image panel = CreateImage("Panel", canvasObject.transform, new Vector2(760f, 500f), new Color(0.1f, 0.08f, 0.07f, 0.9f));
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 0f);

            Text title = CreateText("Title", panel.transform, font, 56, TextAnchor.MiddleCenter);
            title.text = "FRONTIER DEPTHS";
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(680f, 80f);
            titleRect.anchoredPosition = new Vector2(0f, -72f);

            Text subtitle = CreateText("Subtitle", panel.transform, font, 24, TextAnchor.MiddleCenter);
            subtitle.text = "Pick up where you left off or wipe the slate clean.";
            RectTransform subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0.5f, 1f);
            subtitleRect.anchorMax = new Vector2(0.5f, 1f);
            subtitleRect.sizeDelta = new Vector2(620f, 72f);
            subtitleRect.anchoredPosition = new Vector2(0f, -152f);

            Button newGameButton = CreateMenuButton(panel.transform, font, "New Game", new Vector2(0f, -250f));
            Button loadGameButton = CreateMenuButton(panel.transform, font, "Load Game", new Vector2(0f, -340f));

            Text hint = CreateText("Hint", panel.transform, font, 20, TextAnchor.MiddleCenter);
            hint.text = "N = New Game    L = Load Game";
            RectTransform hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(520f, 42f);
            hintRect.anchoredPosition = new Vector2(0f, 42f);

            MainMenuController menu = canvasObject.AddComponent<MainMenuController>();
            SerializedObject menuSo = new SerializedObject(menu);
            menuSo.FindProperty("subtitleText").objectReferenceValue = subtitle;
            menuSo.FindProperty("hintText").objectReferenceValue = hint;
            menuSo.FindProperty("newGameButton").objectReferenceValue = newGameButton;
            menuSo.FindProperty("loadGameButton").objectReferenceValue = loadGameButton;
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(newGameButton.onClick, menu.StartNewGame);
            UnityEventTools.AddPersistentListener(loadGameButton.onClick, menu.LoadGame);

            CreateEventSystem();
            SaveScene(scene, GameSceneCatalog.GetPath(GameSceneId.MainMenu));
        }

        private static void CreateTownHubScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLight();
            CreateGround("TownGround", Vector3.zero, new Vector3(120f, 1f, 120f), new Color(0.48f, 0.42f, 0.31f));

            GameObject townRoot = new GameObject("TownHubRoot");
            townRoot.AddComponent<TownHubController>();

            CreateStructure("MainStreet", new Vector3(0f, 0.62f, 8f), new Vector3(18f, 0.25f, 78f), new Color(0.58f, 0.53f, 0.44f));
            CreateStructure("TownSquare", new Vector3(0f, 0.7f, 0f), new Vector3(26f, 0.4f, 26f), new Color(0.62f, 0.57f, 0.47f));
            CreateStructure("WestPath", new Vector3(-22f, 0.63f, -8f), new Vector3(22f, 0.25f, 10f), new Color(0.58f, 0.53f, 0.44f));
            CreateStructure("EastPath", new Vector3(24f, 0.63f, -8f), new Vector3(24f, 0.25f, 10f), new Color(0.58f, 0.53f, 0.44f));
            CreateStructure("ForgePath", new Vector3(30f, 0.63f, 10f), new Vector3(20f, 0.25f, 10f), new Color(0.58f, 0.53f, 0.44f));
            CreateStructure("NorthwestPath", new Vector3(-24f, 0.63f, 16f), new Vector3(22f, 0.25f, 10f), new Color(0.58f, 0.53f, 0.44f));

            CreateStructure("Saloon", new Vector3(-28f, 4f, -14f), new Vector3(18f, 8f, 12f), new Color(0.39f, 0.23f, 0.14f));
            CreateStructure("QuartermasterStall", new Vector3(28f, 2f, -14f), new Vector3(10f, 4f, 10f), new Color(0.34f, 0.28f, 0.18f));
            CreateStructure("ForgePad", new Vector3(34f, 2f, 12f), new Vector3(12f, 4f, 10f), new Color(0.27f, 0.27f, 0.3f));
            CreateStructure("CurioStall", new Vector3(-30f, 2f, 18f), new Vector3(10f, 4f, 10f), new Color(0.35f, 0.24f, 0.35f));
            CreateStructure("TrainingYard", new Vector3(-36f, 0.5f, 26f), new Vector3(18f, 1f, 14f), new Color(0.42f, 0.31f, 0.19f));
            CreateStructure("DungeonGateFrame", new Vector3(0f, 6f, 44f), new Vector3(14f, 12f, 4f), new Color(0.19f, 0.19f, 0.19f));

            CreateInteractableStation("Quartermaster", new Vector3(28f, 1f, -14f), "shop.quartermaster");
            CreateInteractableStation("Blacksmith", new Vector3(34f, 1f, 12f), "shop.blacksmith");
            CreateInteractableStation("CurioDealer", new Vector3(-30f, 1f, 18f), "shop.curio");
            CreateInteractableStation("BountyBoard", new Vector3(-8f, 1f, -6f), "shop.bounty_board");
            CreateInteractableStation("StashChest", new Vector3(8f, 1f, -6f), "shop.stash");
            CreateDungeonGate(new Vector3(0f, 1f, 40f));

            CreatePlayerRig(new Vector3(0f, 2f, -28f));
            CreateHudCanvas();
            CreateEventSystem();
            SaveScene(scene, GameSceneCatalog.GetPath(GameSceneId.TownHub));
        }

        private static void CreateDungeonScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLight();
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.1f, 0.1f, 0.12f);
            RenderSettings.fogDensity = 0.01f;

            GameObject runtimeRoot = new GameObject("DungeonRuntimeRoot");
            GameObject generatedRoot = new GameObject("GeneratedFloor");
            generatedRoot.transform.SetParent(runtimeRoot.transform);

            DungeonSceneController controller = runtimeRoot.AddComponent<DungeonSceneController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("runtimeRoot").objectReferenceValue = generatedRoot.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CreatePlayerRig(new Vector3(0f, 2f, 0f));
            CreateHudCanvas();
            CreateEventSystem();
            SaveScene(scene, GameSceneCatalog.GetPath(GameSceneId.DungeonRuntime));
        }

        private static void CreateSandboxScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLight();
            CreateGround("SandboxGround", Vector3.zero, new Vector3(40f, 1f, 40f), new Color(0.45f, 0.45f, 0.45f));
            SaveScene(scene, GameSceneCatalog.GetPath(GameSceneId.SandboxArtImport));
        }

        private static void CreateNetPlaytestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLight();
            CreateGround("NetGround", Vector3.zero, new Vector3(30f, 1f, 30f), new Color(0.2f, 0.24f, 0.28f));
            new GameObject("NetworkingPlaceholder");
            SaveScene(scene, GameSceneCatalog.GetPath(GameSceneId.NetPlaytest));
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameSceneCatalog.GetPath(GameSceneId.Bootstrap), true),
                new EditorBuildSettingsScene(GameSceneCatalog.GetPath(GameSceneId.MainMenu), true),
                new EditorBuildSettingsScene(GameSceneCatalog.GetPath(GameSceneId.TownHub), true),
                new EditorBuildSettingsScene(GameSceneCatalog.GetPath(GameSceneId.DungeonRuntime), true),
                new EditorBuildSettingsScene(GameSceneCatalog.GetPath(GameSceneId.SandboxArtImport), true),
                new EditorBuildSettingsScene(GameSceneCatalog.GetPath(GameSceneId.NetPlaytest), true)
            };
        }

        private static void CreatePlayerRig(Vector3 position)
        {
            GameObject player = new GameObject("Player");
            player.transform.position = position;
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);

            GameObject cameraPivot = new GameObject("Camera");
            cameraPivot.transform.SetParent(player.transform, false);
            cameraPivot.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            Camera camera = cameraPivot.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraPivot.AddComponent<AudioListener>();

            PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
            SerializedObject interactorSo = new SerializedObject(interactor);
            interactorSo.FindProperty("interactionCamera").objectReferenceValue = camera;
            interactorSo.ApplyModifiedPropertiesWithoutUndo();

            FirstPersonController controller = player.AddComponent<FirstPersonController>();
            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("playerCamera").objectReferenceValue = camera;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHudCanvas()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Image crosshair = CreateImage("Crosshair", canvasObject.transform, new Vector2(6f, 6f), new Color(1f, 1f, 1f, 0.95f));
            RectTransform crosshairRect = crosshair.rectTransform;
            crosshairRect.anchorMin = crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRect.anchoredPosition = Vector2.zero;

            Text prompt = CreateText("Prompt", canvasObject.transform, font, 22, TextAnchor.MiddleCenter);
            RectTransform promptRect = prompt.rectTransform;
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.sizeDelta = new Vector2(900f, 80f);
            promptRect.anchoredPosition = new Vector2(0f, 96f);

            Text status = CreateText("Status", canvasObject.transform, font, 20, TextAnchor.UpperLeft);
            RectTransform statusRect = status.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(0f, 1f);
            statusRect.sizeDelta = new Vector2(520f, 220f);
            statusRect.anchoredPosition = new Vector2(24f, -24f);

            Image panelBackground = CreateImage("PanelBackground", canvasObject.transform, new Vector2(520f, 420f), new Color(0f, 0f, 0f, 0.72f));
            RectTransform panelRect = panelBackground.rectTransform;
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-24f, -24f);
            panelBackground.enabled = false;

            Text panelText = CreateText("PanelText", panelBackground.transform, font, 18, TextAnchor.UpperLeft);
            RectTransform panelTextRect = panelText.rectTransform;
            panelTextRect.anchorMin = Vector2.zero;
            panelTextRect.anchorMax = Vector2.one;
            panelTextRect.offsetMin = new Vector2(18f, 18f);
            panelTextRect.offsetMax = new Vector2(-18f, -18f);
            panelText.enabled = false;

            GameHudController hud = canvasObject.AddComponent<GameHudController>();
            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("promptText").objectReferenceValue = prompt;
            hudSo.FindProperty("statusText").objectReferenceValue = status;
            hudSo.FindProperty("panelText").objectReferenceValue = panelText;
            hudSo.FindProperty("panelBackground").objectReferenceValue = panelBackground;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateInteractableStation(string name, Vector3 position, string shopId)
        {
            GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
            station.name = name;
            station.transform.position = position;
            station.transform.localScale = new Vector3(2f, 2f, 2f);
            TownServiceStation interactable = station.AddComponent<TownServiceStation>();
            SerializedObject so = new SerializedObject(interactable);
            so.FindProperty("shopId").stringValue = shopId;
            so.FindProperty("prompt").stringValue = $"Open {name}";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDungeonGate(Vector3 position)
        {
            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = "DungeonGate";
            gate.transform.position = position;
            gate.transform.localScale = new Vector3(4f, 4f, 2f);
            gate.GetComponent<Renderer>().sharedMaterial.color = new Color(0.12f, 0.12f, 0.14f);
            gate.AddComponent<DungeonGateInteractable>();
        }

        private static void CreateGround(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = name;
            ground.transform.position = position;
            ground.transform.localScale = scale;
            ground.GetComponent<Renderer>().sharedMaterial.color = color;
        }

        private static void CreateStructure(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject structure = GameObject.CreatePrimitive(PrimitiveType.Cube);
            structure.name = name;
            structure.transform.position = position;
            structure.transform.localScale = scale;
            structure.GetComponent<Renderer>().sharedMaterial.color = color;
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.84f);
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor anchor)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        private static Button CreateMenuButton(Transform parent, Font font, string label, Vector2 anchoredPosition)
        {
            GameObject buttonObject = new GameObject(label.Replace(" ", string.Empty) + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.26f, 0.17f, 0.11f, 0.96f);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(320f, 64f);
            rect.anchoredPosition = anchoredPosition;

            Text text = CreateText("Label", buttonObject.transform, font, 28, TextAnchor.MiddleCenter);
            text.text = label;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return buttonObject.GetComponent<Button>();
        }

        private static void CreateShop(string path, string shopId, TownServiceType type, string name, string greeting, ShopOffer[] offers)
        {
            CreateAssetIfMissing(path, () =>
            {
                ShopDefinition asset = ScriptableObject.CreateInstance<ShopDefinition>();
                asset.shopId = shopId;
                asset.serviceType = type;
                asset.displayName = name;
                asset.greeting = greeting;
                asset.offers = offers;
                return asset;
            });
        }

        private static ThemeKitDefinition MakeTheme(string id, string displayName, Color keyColor, Color accentColor)
        {
            ThemeKitDefinition asset = ScriptableObject.CreateInstance<ThemeKitDefinition>();
            asset.themeId = id;
            asset.displayName = displayName;
            asset.keyColor = keyColor;
            asset.accentColor = accentColor;
            return asset;
        }

        private static FloorBandDefinition MakeFloorBand(string id, string displayName, int startFloor, int endFloor, string themeKitId)
        {
            FloorBandDefinition asset = ScriptableObject.CreateInstance<FloorBandDefinition>();
            asset.floorBandId = id;
            asset.displayName = displayName;
            asset.startFloor = startFloor;
            asset.endFloor = endFloor;
            asset.themeKitId = themeKitId;
            return asset;
        }

        private static ChapterDefinition MakeChapter(string id, string displayName, int startFloor, int endFloor, string modifier)
        {
            ChapterDefinition asset = ScriptableObject.CreateInstance<ChapterDefinition>();
            asset.chapterId = id;
            asset.displayName = displayName;
            asset.startFloor = startFloor;
            asset.endFloor = endFloor;
            asset.macroModifier = modifier;
            return asset;
        }

        private static void CreateAssetIfMissing<T>(string path, System.Func<T> factory) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                return;
            }

            T asset = factory();
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folder);
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
