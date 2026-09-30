#if UNITY_EDITOR
using System.IO;
using IOSVN.TuTien.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IOSVN.TuTien.Editor
{
    [InitializeOnLoad]
    internal static class OnlinePrototypeProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/OnlinePrototype.unity";
        private const string ConfigPath = "Assets/Resources/GameServerConfig.asset";

        static OnlinePrototypeProjectSetup()
        {
            EditorApplication.delayCall += EnsurePrototypeScene;
        }

        [MenuItem("iOSVN/Unity/Create Online Prototype Scene")]
        public static void EnsurePrototypeScene()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Resources");
            if (!File.Exists(ConfigPath))
            {
                var config = ScriptableObject.CreateInstance<GameServerConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var bootstrap = new GameObject("OnlineGameBootstrap");
                bootstrap.AddComponent<PrototypeBootstrap>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            var scenes = EditorBuildSettings.scenes;
            var exists = false;
            foreach (var entry in scenes)
                if (entry.path == ScenePath) { exists = true; break; }
            if (!exists)
            {
                var next = new EditorBuildSettingsScene[scenes.Length + 1];
                next[0] = new EditorBuildSettingsScene(ScenePath, true);
                for (var i = 0; i < scenes.Length; i++) next[i + 1] = scenes[i];
                EditorBuildSettings.scenes = next;
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
