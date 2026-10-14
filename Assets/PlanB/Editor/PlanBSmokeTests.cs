using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlanB.Editor
{
    // Real Unity play-mode checks without a test-package or project-settings dependency.
    [InitializeOnLoad]
    public static class PlanBSmokeTests
    {
        const string Active = "PlanB.Smoke.Active";
        static int step;
        static double due, deadline;
        static Enemy target;
        static bool capture;
        static readonly List<string> errors = new List<string>();

        static PlanBSmokeTests()
        {
            if (SessionState.GetBool(Active, false)) Attach();
        }

        public static void RunBatch() { Begin(false); }
        public static void RunBatchCapture() { Begin(true); }
        static void Begin(bool screenshots)
        {
            PlanBSetup.CreateSceneOnly();
            EditorSceneManager.OpenScene(PlanBSetup.ScenePath);
            SessionState.SetBool(Active, true);
            SessionState.SetInt("PlanB.Smoke.Step", 0);
            SessionState.SetBool("PlanB.Smoke.Capture", screenshots);
            Attach();
            EditorApplication.isPlaying = true;
        }
        static void Attach()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= Log;
            step = SessionState.GetInt("PlanB.Smoke.Step", 0); due = EditorApplication.timeSinceStartup + 1;
            deadline = EditorApplication.timeSinceStartup + 100;
            capture = SessionState.GetBool("PlanB.Smoke.Capture", false);
            EditorApplication.update += Tick;
            Application.logMessageReceived += Log;
        }
        static void Log(string message, string stack, LogType type)
        {
            if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert) &&
                (stack.Contains("PlanB") || message.Contains("NullReferenceException") || message.Contains("MissingReferenceException")))
                errors.Add(message);
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        static T Find<T>() where T : Component
        {
            var result = GameManager.Instance.Floors.ActiveRoot.GetComponentInChildren<T>();
            Require(result != null, "Missing " + typeof(T).Name); return result;
        }
        static void Shot(string name)
        {
            if (!capture) return;
            Directory.CreateDirectory("Assets/PlanB/Verification");
            var camera = GameManager.Instance.CameraRig.GetComponent<Camera>();
            var canvas = GameManager.Instance.Hud.GetComponentInChildren<Canvas>();
            var oldTarget = camera.targetTexture;
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            var oldDistance = canvas.planeDistance;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            bool oldScaler = scaler.enabled;
            float oldScale = canvas.scaleFactor;
            var oldActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                scaler.enabled = false; canvas.scaleFactor = 1;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases(); camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); pixels.Apply();
                File.WriteAllBytes("Assets/PlanB/Verification/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget; canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                scaler.enabled = oldScaler; canvas.scaleFactor = oldScale;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(pixels); RenderTexture.ReleaseTemporary(target);
            }
        }
        static void Later(float seconds = .25f)
        {
            step++; due = EditorApplication.timeSinceStartup + seconds;
            SessionState.SetInt("PlanB.Smoke.Step", step);
            Debug.Log("PLANB_SMOKE_STEP " + step);
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup > deadline) { Finish(false, "Play-mode check timed out."); return; }
            if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup < due) return;
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.EnterPlaymode(); due = EditorApplication.timeSinceStartup + 1; return;
            }
            if (!EditorApplication.isPlaying) return;
            var game = GameManager.Instance;
            if (game == null || game.Player == null) return;
            try
            {
                Require(errors.Count == 0, string.Join("\n", errors));
                switch (step)
                {
                    case 0:
                        Require(game.Phase == GamePhase.Title, "Bootstrap did not open title.");
                        PlanBSetup.VerifySprites(); Shot("01-title"); Later(.5f); break;
                    case 1:
                        game.StartRun(); Later(.5f); break;
                    case 2:
                        Require(game.IsPlaying && game.Player.Health == 120, "Fighter initialization failed.");
                        Require(game.Floors.EnemiesRemaining == 11, "Floor1 enemy count.");
                        ValidateRoute(1); Shot("02-residential");
                        target = Enemy.Spawn(EnemyKind.Walker, LevelBuilder.Cell(8, 8), game.Floors.ActiveRoot);
                        target.enabled = false;
                        Require(game.Player.Weapon.Fire(Vector2.right), "Pistol failed to fire.");
                        Later(.5f); break;
                    case 3:
                        Require(target != null && target.Health == 2, "Visible projectile failed to damage the walker.");
                        target.Damage(3); game.Player.Weapon.Reload(); Later(1.5f); break;
                    case 4:
                        Require(game.Player.Weapon.Magazine == 12 && game.Player.Weapon.Reserve == 23, "Reload did not transfer reserve ammunition.");
                        game.SetPaused(true); Require(Time.timeScale == 0 && game.Phase == GamePhase.Paused, "Pause failed.");
                        game.SetPaused(false);
                        game.Player.Teleport(LevelBuilder.Cell(5, 23)); Later(.3f); break;
                    case 5:
                        Require(game.HasFuse, "Fuse did not collect.");
                        game.Player.Teleport(Find<Generator>().transform.position + Vector3.down);
                        Find<Generator>().Interact();
                        Require(game.PowerRestored && !game.HasFuse, "Generator failed to consume fuse.");
                        Find<Elevator>().Interact(); Later(.4f); break;
                    case 6:
                        Require(game.Floors.CurrentFloor == 2 && game.Floors.EnemiesRemaining == 11, "Research transition or enemies failed.");
                        int activeFloors = 0;
                        foreach (var level in game.Floors.GetComponentsInChildren<LevelBuilder>(true))
                            if (level.gameObject.activeInHierarchy) activeFloors++;
                        Require(activeFloors == 1, "A prior floor remained active after restart or script reload.");
                        ValidateRoute(2);
                        var boss = Find<Boss>(); boss.Damage(1);
                        Require(boss.Health == 25, "Dormant boss accepted damage through containment.");
                        game.Player.Teleport(LevelBuilder.Cell(42, 5)); Later(.3f); break;
                    case 7:
                        Require(game.HasKey, "Key did not collect.");
                        var door = Find<Door>(); door.Interact();
                        Require(game.DoorUnlocked && !door.GetComponent<Collider2D>().enabled, "Containment failed to open.");
                        Require(Reachable(game.Floors.ActiveLevel, LevelBuilder.Grid(game.Floors.SpawnPoint(2))).Contains(new Vector2Int(40, 22)), "Boss arena disconnected after unlocking.");
                        game.Player.Teleport(LevelBuilder.Cell(35, 22)); game.CameraRig.Snap();
                        Shot("03-research");
                        Find<Boss>().Damage(14);
                        Require(game.Floors.EnemiesRemaining == 13, "Boss did not summon two crawlers at half health.");
                        Find<Boss>().Damage(11); Later(.3f); break;
                    case 8:
                        Require(game.BossDefeated, "Boss death did not power exit.");
                        Find<Elevator>().Interact();
                        Require(game.Phase == GamePhase.Won, "Exit did not open win screen.");
                        Shot("04-escaped"); Later(.5f); break;
                    case 9:
                        game.StartRun(); Later(.4f); break;
                    case 10:
                        Require(!game.HasKey && !game.HasFuse && !game.PowerRestored && !game.BossDefeated && !game.DoorUnlocked && game.TrapCount == 2, "Restart retained progression state.");
                        Require(game.Floors.EnemiesRemaining == 11 && game.Player.Health == 120 && game.Player.Weapon.Reserve == 24, "Restart retained combat state.");
                        game.Player.Damage(999); Require(game.Phase == GamePhase.Lost, "Death did not open game-over screen.");
                        game.SelectedClass = CharacterClass.Engineer; game.StartRun(); Later(.4f); break;
                    case 11:
                        Require(game.Player.MaxHealth == 100 && game.Player.Weapon.Reserve == 36, "Engineer stats failed.");
                        game.SelectedClass = CharacterClass.Scout; game.StartRun(); Later(.4f); break;
                    case 12:
                        Require(game.Player.MaxHealth == 90 && Mathf.Abs(game.Player.MoveSpeed - 4.25f) < .01f, "Scout stats failed.");
                        Require(Trap.TryPlace() && game.TrapCount == 1, "Trap placement failed.");
                        Finish(true, "Title, real projectile damage, reload, pause, both map routes, fuse, generator, elevator, key, containment, boss reinforcements/death, escape, death/restart and all survivor classes passed."); break;
                }
            }
            catch (Exception exception) { Finish(false, exception.ToString()); }
        }

        static HashSet<Vector2Int> Reachable(LevelBuilder level, Vector2Int start)
        {
            var seen = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>();
            queue.Enqueue(start); seen.Add(start);
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var direction in directions)
                {
                    var next = cell + direction;
                    if (level.IsWalkable(next.x, next.y) && seen.Add(next)) queue.Enqueue(next);
                }
            }
            return seen;
        }
        static void ValidateRoute(int floor)
        {
            var game = GameManager.Instance;
            var seen = Reachable(game.Floors.ActiveLevel, LevelBuilder.Grid(game.Floors.SpawnPoint(floor)));
            Vector2Int[] objectives = floor == 1
                ? new[] { new Vector2Int(5,23), new Vector2Int(40,5), new Vector2Int(24,25) }
                : new[] { new Vector2Int(42,5), new Vector2Int(29,21), new Vector2Int(24,25) };
            foreach (var cell in objectives) Require(seen.Contains(cell), "Unreachable objective " + floor + ":" + cell);
            if (floor == 2) Require(!seen.Contains(new Vector2Int(40,22)), "Locked arena can be bypassed.");
        }
        static void Finish(bool success, string message)
        {
            SessionState.SetBool(Active, false);
            EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
            Directory.CreateDirectory("Assets/PlanB/Verification");
            File.WriteAllText("Assets/PlanB/Verification/SmokeResult.txt", (success ? "PASS\n" : "FAIL\n") + message);
            if (success) Debug.Log("PLANB_SMOKE_PASS: " + message); else Debug.LogError("PLANB_SMOKE_FAIL: " + message);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
