using System;
using System.IO;
using UnityEngine;

namespace FrontierDepths.Core
{
    [Serializable]
    public sealed class GameSettingsState
    {
        public float mouseSensitivity = 1.2f;
        public float fov = 70f;
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public float musicVolume = 1f;
        public bool invertY;
        public float crosshairSize = 14f;
        public float minimapSize = 190f;
        public float minimapOpacity = 0.9f;
        public float minimapZoom = 1f;

        public void Clamp()
        {
            mouseSensitivity = Mathf.Clamp(mouseSensitivity, 0.1f, 10f);
            fov = Mathf.Clamp(fov, 60f, 100f);
            masterVolume = Mathf.Clamp01(masterVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            crosshairSize = Mathf.Clamp(crosshairSize, 6f, 36f);
            minimapSize = Mathf.Clamp(minimapSize, 120f, 360f);
            minimapOpacity = Mathf.Clamp(minimapOpacity, 0.1f, 1f);
            minimapZoom = Mathf.Clamp(minimapZoom, 0.5f, 3f);
        }
    }

    public static class GameSettingsService
    {
        private const string FileName = "settings.json";
        private static GameSettingsState cached;

        public static GameSettingsState Current
        {
            get
            {
                cached ??= Load();
                return cached;
            }
        }

        public static string SettingsPath => Path.Combine(Application.persistentDataPath, FileName);

        public static GameSettingsState Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    cached = new GameSettingsState();
                    Save(cached);
                    return cached;
                }

                string json = File.ReadAllText(SettingsPath);
                cached = JsonUtility.FromJson<GameSettingsState>(json) ?? new GameSettingsState();
            }
            catch
            {
                cached = new GameSettingsState();
            }

            cached.Clamp();
            Save(cached);
            return cached;
        }

        public static void Save(GameSettingsState state = null)
        {
            cached = state ?? cached ?? new GameSettingsState();
            cached.Clamp();
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(SettingsPath, JsonUtility.ToJson(cached, true));
            }
            catch
            {
                // Settings must never block scene loading or gameplay.
            }
        }

        public static void ApplyRuntime(GameSettingsState state = null)
        {
            cached = state ?? Current;
            cached.Clamp();
            AudioListener.volume = cached.masterVolume;
        }
    }
}
