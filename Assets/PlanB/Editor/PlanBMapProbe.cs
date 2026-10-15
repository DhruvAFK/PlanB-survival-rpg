using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlanB.Editor
{
    [InitializeOnLoad]
    public static class PlanBMapProbe
    {
        const string ActiveKey = "PlanB.MapProbe.Active";
        static int samples, warmFrames;
        static double renderTotal;
        static double deadline;
        static string mode;
        static RenderTexture target;
        static Camera camera;

        static PlanBMapProbe() { if (SessionState.GetBool(ActiveKey, false)) Attach(); }
        public static void RunBaseline() { Begin("baseline"); }
        public static void RunAfter() { Begin("after"); }
        static void Begin(string name)
        {
            PlanBSetup.CreateSceneOnly();
            EditorSceneManager.OpenScene(PlanBSetup.ScenePath);
            SessionState.SetBool(ActiveKey, true); SessionState.SetString("PlanB.MapProbe.Mode", name);
            Attach(); EditorApplication.isPlaying = true;
        }
        static void Attach()
        {
            EditorApplication.update -= Tick;
            mode = SessionState.GetString("PlanB.MapProbe.Mode", "baseline");
            samples = warmFrames = 0; renderTotal = 0;
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += Tick;
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup > deadline) { Finish("Probe timed out", 1); return; }
            if (EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
            var game = GameManager.Instance;
            if (game == null || game.Player == null) return;
            if (game.Phase == GamePhase.Title) { game.StartRun(); warmFrames = 0; }
            if (target == null)
            {
                camera = game.CameraRig.GetComponent<Camera>();
                if (camera.targetTexture != null && camera.targetTexture.name == "PlanB probe target")
                    UnityEngine.Object.DestroyImmediate(camera.targetTexture);
                target = new RenderTexture(1600, 900, 24) { name = "PlanB probe target" };
                target.Create(); camera.targetTexture = target;
            }
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            camera.Render();
            double elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            if (++warmFrames < 45) return;
            renderTotal += elapsed;
            if (++samples < 120) return;
            var renderers = game.Floors.ActiveRoot.GetComponentsInChildren<Renderer>();
            int sprites = game.Floors.ActiveRoot.GetComponentsInChildren<SpriteRenderer>().Length;
            int transforms = game.Floors.ActiveRoot.GetComponentsInChildren<Transform>().Length;
            string report = "Mode: " + mode + "\nActive floor renderers: " + renderers.Length +
                "\nActive floor sprite renderers: " + sprites + "\nActive floor transforms: " + transforms +
                "\n120 warmed 1600x900 offscreen world renders; mean Camera.Render CPU ms: " + (renderTotal / samples).ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                "\nThis isolates world-render submission/culling; it excludes full-player update/UI cost and is not a standalone FPS guarantee.\n";
            Directory.CreateDirectory("Assets/PlanB/Verification");
            File.WriteAllText("Assets/PlanB/Verification/MapProbe-" + mode + ".txt", report);
            Finish(report, 0);
        }
        static void Finish(string result, int code)
        {
            SessionState.SetBool(ActiveKey, false); EditorApplication.update -= Tick;
            if (camera != null) camera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            Debug.Log("PLANB_MAP_PROBE: " + result); EditorApplication.Exit(code);
        }
    }
}
