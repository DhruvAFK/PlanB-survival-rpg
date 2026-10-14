using UnityEngine;
using UnityEngine.Rendering;

namespace PlanB
{
    public enum GamePhase { Title, Playing, Paused, Won, Lost }
    public enum CharacterClass { Fighter, Engineer, Scout }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public Player Player;
        public FloorManager Floors;
        public HUD Hud;
        public CameraFollow CameraRig;
        public GamePhase Phase { get; private set; }
        public CharacterClass SelectedClass = CharacterClass.Fighter;
        public bool HasFuse, PowerRestored, HasKey, DoorUnlocked, BossDefeated;
        public int Scrap, Battery, Wire, TrapCount = 2, Kills;
        public float AlarmUntil, RunTime;
        RenderPipelineAsset originalPipeline, originalQualityPipeline;
        public bool IsPlaying { get { return Phase == GamePhase.Playing; } }
        public string Objective
        {
            get
            {
                if (Floors == null || Floors.CurrentFloor == 1)
                    return !HasFuse && !PowerRestored ? "Find the fuse" : !PowerRestored ? "Power the elevator" : "Take the elevator down";
                return BossDefeated ? "Escape via the elevator" : DoorUnlocked ? "Defeat the Warden" : HasKey ? "Open the containment door" : "Find the key";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; Time.timeScale = 1f; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance == null) new GameObject("PLAN B — Facility survival").AddComponent<GameManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // Override only this running session; the existing URP project assets remain untouched.
            originalPipeline = GraphicsSettings.defaultRenderPipeline;
            originalQualityPipeline = QualitySettings.renderPipeline;
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            Application.targetFrameRate = 60;
            foreach (Camera oldCamera in Camera.allCameras)
            {
                oldCamera.enabled = false;
                var listener = oldCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
            var cameraObject = new GameObject("PlanB Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
            CameraRig = cameraObject.GetComponent<CameraFollow>();
            Floors = gameObject.AddComponent<FloorManager>();
            Hud = gameObject.AddComponent<HUD>();
            Phase = GamePhase.Title;
        }

        void OnEnable() { if (Instance == null) Instance = this; }
        void OnDestroy()
        {
            if (Instance != this) return;
            Time.timeScale = 1;
            GraphicsSettings.defaultRenderPipeline = originalPipeline;
            QualitySettings.renderPipeline = originalQualityPipeline;
            Instance = null;
        }

        void Start()
        {
            BuildRun();
            Hud.ShowMenu(GamePhase.Title);
        }

        void Update()
        {
            if (InputBridge.PausePressed && (Phase == GamePhase.Playing || Phase == GamePhase.Paused))
                SetPaused(Phase == GamePhase.Playing);
            if (IsPlaying) RunTime += Time.deltaTime;
        }

        void BuildRun()
        {
            Time.timeScale = 1f;
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); }
            Floors.Clear();
            HasFuse = PowerRestored = HasKey = DoorUnlocked = BossDefeated = false;
            Scrap = Battery = Wire = Kills = 0; TrapCount = 2;
            AlarmUntil = RunTime = 0;
            Floors.Build();
            var actor = new GameObject("Survivor");
            Player = actor.AddComponent<Player>();
            Player.ResetFor(SelectedClass);
            Floors.EnterFloor(1);
            Player.transform.position = Floors.SpawnPoint(1);
            CameraRig.Target = Player.transform;
            CameraRig.Snap();
            RefreshObjective();
        }

        public void StartRun()
        {
            Phase = GamePhase.Title;
            BuildRun();
            Phase = GamePhase.Playing;
            Hud.ShowMenu(Phase);
            Notify("Find the fuse in storage. E to interact. Space to bash.", 6);
        }

        public void ShowTitle()
        {
            Time.timeScale = 1;
            Phase = GamePhase.Title;
            Hud.ShowMenu(Phase);
        }

        public void SetPaused(bool paused)
        {
            Phase = paused ? GamePhase.Paused : GamePhase.Playing;
            Time.timeScale = paused ? 0 : 1;
            Hud.ShowMenu(Phase);
        }

        public void ChangeFloor(int floor)
        {
            if (Player == null) return;
            Floors.EnterFloor(floor);
            Player.Teleport(Floors.SpawnPoint(floor));
            CameraRig.Snap();
            AudioFactory.Play("elevator");
            RefreshObjective();
            Notify(floor == 2 ? "Research level. Find the security key, then enter containment." : "Residential level.", 5);
        }

        public void EndRun(bool won)
        {
            if (Phase == GamePhase.Won || Phase == GamePhase.Lost) return;
            Phase = won ? GamePhase.Won : GamePhase.Lost;
            Time.timeScale = 0;
            Hud.ShowMenu(Phase);
        }

        public void OnEnemyKilled(Enemy enemy)
        {
            Kills++;
            if (enemy.Kind == EnemyKind.Warden && !BossDefeated)
            {
                BossDefeated = true;
                Floors.BossKilled();
                Notify("WARDEN ELIMINATED. The elevator is ready — get out!", 7);
            }
            RefreshObjective();
        }

        public void Alarm(float duration)
        {
            AlarmUntil = Time.time + duration;
            Notify("SECURITY ALERT — creatures are tracking you", 3);
        }
        public void Notify(string message, float duration = 3f) { if (Hud != null) Hud.Notify(message, duration); }
        public void RefreshObjective() { if (Hud != null) Hud.Refresh(); }
        public void DamageFlash() { if (Hud != null) Hud.Flash(); }
        public void Quit()
        {
            Time.timeScale = 1;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
