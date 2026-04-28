using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FrontierDepths.Core
{
    public enum GameplayInputAction
    {
        MoveForward,
        MoveBack,
        MoveLeft,
        MoveRight,
        Jump,
        Sprint,
        Interact,
        Fire,
        Reload,
        PistolWhip,
        Inventory,
        Dash,
        EquipPrimary,
        EquipSecondary,
        ToggleFullMap,
        ManaSense,
        RunInfo,
        Minimap,
        Pause
    }

    [Serializable]
    public sealed class InputBindingState
    {
        public int version = 1;
        public List<InputBindingRecord> bindings = new List<InputBindingRecord>();

        public void Normalize()
        {
            bindings ??= new List<InputBindingRecord>();
            foreach (GameplayInputAction action in Enum.GetValues(typeof(GameplayInputAction)))
            {
                if (Find(action) >= 0)
                {
                    continue;
                }

                bindings.Add(InputBindingService.GetDefaultRecord(action));
            }

            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                if (!Enum.TryParse(bindings[i].action, out GameplayInputAction action))
                {
                    bindings.RemoveAt(i);
                    continue;
                }

                InputBindingRecord record = bindings[i];
                InputBindingRecord defaults = InputBindingService.GetDefaultRecord(action);
                record.primary = NormalizeKeyName(record.primary, defaults.primary);
                record.secondary = NormalizeKeyName(record.secondary, defaults.secondary);
                bindings[i] = record;
            }
        }

        public InputBindingRecord Get(GameplayInputAction action)
        {
            int index = Find(action);
            if (index >= 0)
            {
                return bindings[index];
            }

            InputBindingRecord record = InputBindingService.GetDefaultRecord(action);
            bindings.Add(record);
            return record;
        }

        public void Set(GameplayInputAction action, KeyCode key, bool secondary)
        {
            int index = Find(action);
            InputBindingRecord record = index >= 0 ? bindings[index] : InputBindingService.GetDefaultRecord(action);
            string keyName = key.ToString();
            if (secondary)
            {
                record.secondary = keyName;
            }
            else
            {
                record.primary = keyName;
            }

            if (index >= 0)
            {
                bindings[index] = record;
            }
            else
            {
                bindings.Add(record);
            }
        }

        private int Find(GameplayInputAction action)
        {
            string actionName = action.ToString();
            for (int i = 0; i < bindings.Count; i++)
            {
                if (string.Equals(bindings[i].action, actionName, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string NormalizeKeyName(string keyName, string fallback)
        {
            return Enum.TryParse(keyName, out KeyCode _) ? keyName : fallback;
        }
    }

    [Serializable]
    public struct InputBindingRecord
    {
        public string action;
        public string primary;
        public string secondary;
    }

    public static class InputBindingService
    {
        private const string FileName = "bindings.json";
        private static InputBindingState cached;

        private static readonly KeyCode[] RebindCandidates =
        {
            KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Space, KeyCode.LeftShift,
            KeyCode.LeftControl, KeyCode.LeftAlt, KeyCode.E, KeyCode.R, KeyCode.V, KeyCode.I, KeyCode.G, KeyCode.M, KeyCode.Escape,
            KeyCode.Q, KeyCode.F, KeyCode.C, KeyCode.X, KeyCode.Z, KeyCode.Tab,
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4
        };

        public static string BindingsPath => Path.Combine(Application.persistentDataPath, FileName);

        public static InputBindingState Current
        {
            get
            {
                cached ??= Load();
                return cached;
            }
        }

        public static InputBindingState Load()
        {
            try
            {
                if (!File.Exists(BindingsPath))
                {
                    cached = CreateDefaultState();
                    Save(cached);
                    return cached;
                }

                cached = JsonUtility.FromJson<InputBindingState>(File.ReadAllText(BindingsPath)) ?? CreateDefaultState();
            }
            catch
            {
                cached = CreateDefaultState();
            }

            cached.Normalize();
            Save(cached);
            return cached;
        }

        public static void Save(InputBindingState state = null)
        {
            cached = state ?? cached ?? CreateDefaultState();
            cached.Normalize();
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(BindingsPath, JsonUtility.ToJson(cached, true));
            }
            catch
            {
                // Input settings must never block scene loading.
            }
        }

        public static void ResetToDefaults()
        {
            cached = CreateDefaultState();
            Save(cached);
        }

        public static bool GetKey(GameplayInputAction action)
        {
            InputBindingRecord record = Current.Get(action);
            return IsKeyHeld(record.primary) || IsKeyHeld(record.secondary);
        }

        public static bool GetKeyDown(GameplayInputAction action)
        {
            InputBindingRecord record = Current.Get(action);
            return IsKeyDown(record.primary) || IsKeyDown(record.secondary);
        }

        public static Vector2 GetMovementVector()
        {
            float x = 0f;
            float y = 0f;
            if (GetKey(GameplayInputAction.MoveLeft)) x -= 1f;
            if (GetKey(GameplayInputAction.MoveRight)) x += 1f;
            if (GetKey(GameplayInputAction.MoveBack)) y -= 1f;
            if (GetKey(GameplayInputAction.MoveForward)) y += 1f;

            if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f))
            {
                x = Input.GetAxisRaw("Horizontal");
                y = Input.GetAxisRaw("Vertical");
            }

            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        public static string GetDisplay(GameplayInputAction action)
        {
            InputBindingRecord record = Current.Get(action);
            return string.IsNullOrWhiteSpace(record.secondary)
                ? PrettyKey(record.primary)
                : $"{PrettyKey(record.primary)} / {PrettyKey(record.secondary)}";
        }

        public static void SetPrimaryBinding(GameplayInputAction action, KeyCode key)
        {
            RemoveDuplicateCriticalBinding(action, key);
            Current.Set(action, key, false);
            Save(Current);
        }

        public static bool TryReadPressedCandidate(out KeyCode key)
        {
            for (int i = 0; i < RebindCandidates.Length; i++)
            {
                if (Input.GetKeyDown(RebindCandidates[i]))
                {
                    key = RebindCandidates[i];
                    return true;
                }
            }

            key = KeyCode.None;
            return false;
        }

        public static InputBindingRecord GetDefaultRecord(GameplayInputAction action)
        {
            return action switch
            {
                GameplayInputAction.MoveForward => Record(action, KeyCode.W),
                GameplayInputAction.MoveBack => Record(action, KeyCode.S),
                GameplayInputAction.MoveLeft => Record(action, KeyCode.A),
                GameplayInputAction.MoveRight => Record(action, KeyCode.D),
                GameplayInputAction.Jump => Record(action, KeyCode.Space),
                GameplayInputAction.Sprint => Record(action, KeyCode.LeftShift),
                GameplayInputAction.Interact => Record(action, KeyCode.E),
                GameplayInputAction.Fire => Record(action, KeyCode.Mouse0),
                GameplayInputAction.Reload => Record(action, KeyCode.R),
                GameplayInputAction.PistolWhip => Record(action, KeyCode.V, KeyCode.Mouse2),
                GameplayInputAction.Inventory => Record(action, KeyCode.I, KeyCode.Tab),
                GameplayInputAction.Dash => Record(action, KeyCode.LeftControl, KeyCode.LeftAlt),
                GameplayInputAction.EquipPrimary => Record(action, KeyCode.Alpha1),
                GameplayInputAction.EquipSecondary => Record(action, KeyCode.Alpha2),
                GameplayInputAction.ToggleFullMap => Record(action, KeyCode.M),
                GameplayInputAction.ManaSense => Record(action, KeyCode.C),
                GameplayInputAction.RunInfo => Record(action, KeyCode.G),
                GameplayInputAction.Minimap => Record(action, KeyCode.None),
                GameplayInputAction.Pause => Record(action, KeyCode.Escape),
                _ => Record(action, KeyCode.None)
            };
        }

        private static InputBindingState CreateDefaultState()
        {
            InputBindingState state = new InputBindingState();
            foreach (GameplayInputAction action in Enum.GetValues(typeof(GameplayInputAction)))
            {
                state.bindings.Add(GetDefaultRecord(action));
            }

            return state;
        }

        private static InputBindingRecord Record(GameplayInputAction action, KeyCode primary, KeyCode secondary = KeyCode.None)
        {
            return new InputBindingRecord
            {
                action = action.ToString(),
                primary = primary.ToString(),
                secondary = secondary == KeyCode.None ? string.Empty : secondary.ToString()
            };
        }

        private static bool IsKeyHeld(string keyName)
        {
            return Enum.TryParse(keyName, out KeyCode keyCode) && keyCode != KeyCode.None && Input.GetKey(keyCode);
        }

        private static bool IsKeyDown(string keyName)
        {
            return Enum.TryParse(keyName, out KeyCode keyCode) && keyCode != KeyCode.None && Input.GetKeyDown(keyCode);
        }

        private static string PrettyKey(string keyName)
        {
            return keyName switch
            {
                "Mouse0" => "Mouse Left",
                "Mouse1" => "Mouse Right",
                "Mouse2" => "Mouse Middle",
                "LeftShift" => "Left Shift",
                "LeftControl" => "Left Ctrl",
                "LeftAlt" => "Left Alt",
                "Alpha1" => "1",
                "Alpha2" => "2",
                _ => string.IsNullOrWhiteSpace(keyName) ? "Unbound" : keyName
            };
        }

        private static void RemoveDuplicateCriticalBinding(GameplayInputAction targetAction, KeyCode key)
        {
            if (key == KeyCode.None)
            {
                return;
            }

            InputBindingState state = Current;
            for (int i = 0; i < state.bindings.Count; i++)
            {
                InputBindingRecord record = state.bindings[i];
                if (!Enum.TryParse(record.action, out GameplayInputAction action) || action == targetAction)
                {
                    continue;
                }

                if (string.Equals(record.primary, key.ToString(), StringComparison.Ordinal))
                {
                    record.primary = GetDefaultRecord(action).primary;
                }

                if (string.Equals(record.secondary, key.ToString(), StringComparison.Ordinal))
                {
                    record.secondary = string.Empty;
                }

                state.bindings[i] = record;
            }
        }
    }
}
