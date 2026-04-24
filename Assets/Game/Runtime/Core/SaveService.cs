using System.IO;
using UnityEngine;

namespace FrontierDepths.Core
{
    public sealed class SaveService
    {
        private const string ProfileFileName = "profile.json";
        private const string RunFileName = "run.json";
        private readonly string saveDirectory;

        public SaveService()
        {
            saveDirectory = Path.Combine(Application.persistentDataPath, "FrontierDepths", "Saves");
            Directory.CreateDirectory(saveDirectory);
        }

        public ProfileState LoadProfile()
        {
            return Load(ProfilePath, new ProfileState());
        }

        public void SaveProfile(ProfileState state)
        {
            state.Normalize();
            Save(ProfilePath, state);
        }

        public RunState LoadRun()
        {
            return Load(RunPath, new RunState());
        }

        public void SaveRun(RunState state)
        {
            state.Normalize();
            Save(RunPath, state);
        }

        public bool HasProfileData()
        {
            return File.Exists(ProfilePath);
        }

        public bool HasRunData()
        {
            return File.Exists(RunPath);
        }

        public void DeleteProfile()
        {
            if (File.Exists(ProfilePath))
            {
                File.Delete(ProfilePath);
            }
        }

        public void DeleteRun()
        {
            if (File.Exists(RunPath))
            {
                File.Delete(RunPath);
            }
        }

        private string ProfilePath => Path.Combine(saveDirectory, ProfileFileName);
        private string RunPath => Path.Combine(saveDirectory, RunFileName);

        private static T Load<T>(string path, T fallback) where T : class
        {
            if (!File.Exists(path))
            {
                return fallback;
            }

            try
            {
                string json = File.ReadAllText(path);
                return string.IsNullOrWhiteSpace(json) ? fallback : JsonUtility.FromJson<T>(json) ?? fallback;
            }
            catch (IOException)
            {
                return fallback;
            }
        }

        private static void Save<T>(string path, T value)
        {
            File.WriteAllText(path, JsonUtility.ToJson(value, true));
        }
    }
}
