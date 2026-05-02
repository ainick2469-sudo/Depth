using System;
using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonRewardChoiceController : MonoBehaviour
    {
        private const string ControllerName = "DungeonRewardChoiceController";
        private const int ChoiceCount = 3;

        private readonly List<RunUpgradeDefinition> choices = new List<RunUpgradeDefinition>(ChoiceCount);
        private FirstPersonController playerController;
        private Action descendAfterReward;
        private string errorMessage = string.Empty;
        private bool active;
        private bool selectionInProgress;
        private int focusedChoiceIndex;

        public static bool IsRewardChoiceActive => Instance != null && Instance.active;
        public IReadOnlyList<RunUpgradeDefinition> Choices => choices;
        internal bool ActiveForTests => active;
        internal bool SelectionInProgressForTests => selectionInProgress;
        internal int ChoiceCountForTests => choices.Count;
        internal int FocusedChoiceIndexForTests => focusedChoiceIndex;
        internal bool CursorUnlockedForTests => Cursor.visible && Cursor.lockState == CursorLockMode.None;

        private static DungeonRewardChoiceController Instance { get; set; }

        public static bool ShouldOfferDescentReward(RunState run, bool isNormalGameplayDescent)
        {
            return isNormalGameplayDescent &&
                   run != null &&
                   run.isActive &&
                   run.currentFloor != null &&
                   !run.currentFloor.rewardGranted;
        }

        public static bool TryBeginDescentReward(PlayerInteractor interactor, Action descendCallback)
        {
            if (IsRewardChoiceActive)
            {
                return true;
            }

            GameBootstrap bootstrap = GameBootstrap.Instance;
            RunState run = bootstrap != null && bootstrap.RunService != null ? bootstrap.RunService.Current : null;
            if (!ShouldOfferDescentReward(run, true))
            {
                return false;
            }

            List<RunUpgradeDefinition> rewardChoices = RunUpgradeCatalog.CreateRewardChoicesForFloor(run, run.floorIndex, ChoiceCount);
            if (rewardChoices.Count == 0)
            {
                Debug.LogWarning("Descent reward generation failed; allowing descent without reward.");
                return false;
            }

            DungeonRewardChoiceController controller = GetOrCreate();
            controller.Begin(interactor, descendCallback, rewardChoices);
            return true;
        }

        public static bool TryApplySelectionForTests(RunState run, RunUpgradeDefinition definition)
        {
            if (run == null || definition == null || !RunUpgradeCatalog.TryGet(definition.upgradeId, out _))
            {
                return false;
            }

            run.AddOrStackUpgrade(definition.upgradeId);
            if (run.currentFloor != null)
            {
                run.currentFloor.rewardGranted = true;
                run.SetVisitedFloor(run.currentFloor);
            }

            run.Normalize();
            return true;
        }

        private static DungeonRewardChoiceController GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject existing = GameObject.Find(ControllerName);
            if (existing != null && existing.TryGetComponent(out DungeonRewardChoiceController existingController))
            {
                Instance = existingController;
                return Instance;
            }

            GameObject controllerObject = new GameObject(ControllerName);
            Instance = controllerObject.AddComponent<DungeonRewardChoiceController>();
            DontDestroyOnLoad(controllerObject);
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectChoice(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectChoice(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectChoice(2);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                SelectChoice(0);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad2))
            {
                SelectChoice(1);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad3))
            {
                SelectChoice(2);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                focusedChoiceIndex = choices.Count > 0 ? (focusedChoiceIndex + 1) % choices.Count : 0;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                focusedChoiceIndex = choices.Count > 0 ? (focusedChoiceIndex + choices.Count - 1) % choices.Count : 0;
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                SelectChoice(focusedChoiceIndex);
            }
        }

        private void OnGUI()
        {
            if (!active)
            {
                return;
            }

            const float width = 620f;
            const float height = 330f;
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Space(10f);
            GUILayout.Label("Choose Your Descent Reward", CreateHeaderStyle());
            GUILayout.Label("Pick 1 of 3 upgrades. The run gets stronger, then the next floor loads.", CreateBodyStyle());
            GUILayout.Space(12f);

            for (int i = 0; i < choices.Count; i++)
            {
                RunUpgradeDefinition choice = choices[i];
                if (GUILayout.Button($"{i + 1}. {BuildChoiceLabel(choice)}", CreateChoiceStyle(i == focusedChoiceIndex), GUILayout.Height(82f)))
                {
                    SelectChoice(i);
                }

                Rect choiceRect = GUILayoutUtility.GetLastRect();
                Event currentEvent = Event.current;
                if (currentEvent != null && choiceRect.Contains(currentEvent.mousePosition))
                {
                    focusedChoiceIndex = i;
                }
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                GUILayout.Space(8f);
                GUILayout.Label(errorMessage, CreateErrorStyle());
            }

            GUILayout.EndArea();
            ConsumeModalInputEvent();
        }

        private void Begin(PlayerInteractor interactor, Action descendCallback, List<RunUpgradeDefinition> rewardChoices)
        {
            choices.Clear();
            choices.AddRange(rewardChoices);
            descendAfterReward = descendCallback;
            errorMessage = string.Empty;
            selectionInProgress = false;
            focusedChoiceIndex = 0;
            playerController = interactor != null ? interactor.GetComponent<FirstPersonController>() : FindAnyObjectByType<FirstPersonController>();
            active = true;
            playerController?.SetUiCaptured(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }

        private bool SelectChoice(int index)
        {
            if (selectionInProgress || index < 0 || index >= choices.Count)
            {
                return false;
            }

            selectionInProgress = true;
            RunUpgradeDefinition selected = choices[index];
            if (!RunUpgradeCatalog.TryGet(selected.upgradeId, out _))
            {
                errorMessage = $"Failed to apply upgrade: {selected.upgradeId}";
                Debug.LogError(errorMessage);
                selectionInProgress = false;
                return false;
            }

            try
            {
                GameBootstrap bootstrap = GameBootstrap.Instance;
                if (bootstrap == null || bootstrap.RunService == null)
                {
                    throw new InvalidOperationException("Run service is unavailable.");
                }

                bootstrap.RunService.AddRunUpgrade(selected.upgradeId);
                bootstrap.RunService.MarkCurrentFloorRewardGranted();
                playerController?.GetComponent<PlayerHealth>()?.RefreshRunStatBonuses();
                int stackCount = bootstrap.RunService.Current.GetUpgradeStackCount(selected.upgradeId);
                Debug.Log($"{selected.displayName} upgraded to Lv. {Mathf.Max(1, stackCount)}.");
            }
            catch (Exception exception)
            {
                errorMessage = $"Failed to apply upgrade: {exception.Message}";
                Debug.LogError(errorMessage);
                selectionInProgress = false;
                return false;
            }

            CompleteAndDescend();
            return true;
        }

        private void CompleteAndDescend()
        {
            active = false;
            selectionInProgress = false;
            choices.Clear();
            errorMessage = string.Empty;
            playerController?.SetUiCaptured(false);
            if (playerController == null)
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Action callback = descendAfterReward;
            descendAfterReward = null;
            callback?.Invoke();
        }

        internal void BeginForTests(List<RunUpgradeDefinition> rewardChoices, Action descendCallback = null)
        {
            Begin(null, descendCallback, rewardChoices);
        }

        internal bool TrySelectChoiceForTests(int index)
        {
            return SelectChoice(index);
        }

        internal bool TrySelectChoiceForTests(int index, RunState run)
        {
            if (run == null)
            {
                return SelectChoice(index);
            }

            if (selectionInProgress || index < 0 || index >= choices.Count)
            {
                return false;
            }

            selectionInProgress = true;
            if (!TryApplySelectionForTests(run, choices[index]))
            {
                selectionInProgress = false;
                return false;
            }

            CompleteAndDescend();
            return true;
        }

        private string BuildChoiceLabel(RunUpgradeDefinition choice)
        {
            GameBootstrap bootstrap = GameBootstrap.Instance;
            RunState run = bootstrap != null && bootstrap.RunService != null ? bootstrap.RunService.Current : null;
            return RunUpgradeCatalog.BuildRewardChoiceLabel(run, choice);
        }

        private static GUIStyle CreateHeaderStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = new Color(1f, 0.86f, 0.45f, 1f);
            return style;
        }

        private static GUIStyle CreateBodyStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                wordWrap = true
            };
            style.normal.textColor = Color.white;
            return style;
        }

        private static GUIStyle CreateChoiceStyle(bool focused)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = focused ? 16 : 15,
                fontStyle = focused ? FontStyle.Bold : FontStyle.Normal,
                wordWrap = true,
                padding = new RectOffset(18, 18, 8, 8)
            };
            if (focused)
            {
                style.normal.textColor = new Color(1f, 0.9f, 0.52f, 1f);
            }

            return style;
        }

        private static GUIStyle CreateErrorStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            style.normal.textColor = new Color(1f, 0.34f, 0.24f, 1f);
            return style;
        }

        private static void ConsumeModalInputEvent()
        {
            Event current = Event.current;
            if (current == null)
            {
                return;
            }

            if (current.isKey ||
                current.type == EventType.MouseDown ||
                current.type == EventType.MouseUp ||
                current.type == EventType.MouseDrag ||
                current.type == EventType.ScrollWheel)
            {
                current.Use();
            }
        }
    }
}
