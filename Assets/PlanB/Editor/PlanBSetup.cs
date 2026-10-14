using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlanB.Editor
{
    public static class PlanBSetup
    {
        public const string ScenePath = "Assets/PlanB/Scenes/PlanB_Game.unity";

        [MenuItem("PlanB/Setup")]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateSceneOnly();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Plan B ready. Press Play. All gameplay and art are generated at runtime.");
        }

        public static void CreateSceneOnly()
        {
            const string materialPath = "Assets/PlanB/Resources/PlanB/SpriteMaterial.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
            {
                Directory.CreateDirectory("Assets/PlanB/Resources/PlanB");
                AssetDatabase.Refresh();
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) throw new System.Exception("Unity's built-in sprite shader is unavailable.");
                AssetDatabase.CreateAsset(new Material(shader) { name = "PlanB Sprite Material" }, materialPath);
                AssetDatabase.SaveAssets();
            }
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory("Assets/PlanB/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }

        [MenuItem("PlanB/Build Windows Demo")]
        public static void BuildWindows()
        {
            CreateSceneOnly();
            Directory.CreateDirectory("Builds/PlanB");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = "Builds/PlanB/PlanB.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Plan B Windows build failed: " + report.summary.result);
            Debug.Log("Windows demo built: Builds/PlanB/PlanB.exe");
        }

        [MenuItem("PlanB/Verify Sprite Library")]
        public static void VerifySprites()
        {
            string[] keys = { "fighter", "engineer", "scout", "walker", "runner", "crawler", "spitter", "warden", "scientist", "tile_wood", "tile_metal", "tile_wall", "ammo", "medkit", "fuse", "key", "generator", "elevator", "lockeddoor" };
            foreach (string key in keys)
            {
                Sprite sprite = SpriteLibrary.Get(key);
                if (sprite == null || sprite.texture == null) throw new System.Exception("Missing procedural sprite: " + key);
            }
            Debug.Log("Plan B sprite library verified: " + keys.Length + " required sprite categories.");
        }
    }
}
