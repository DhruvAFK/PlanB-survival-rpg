using UnityEngine;

namespace PlanB
{
    /// <summary>All pixel debris shares one bounded native particle system.</summary>
    public sealed class Effects : MonoBehaviour
    {
        const string PoolName = "PlanB Debris Pool";
        const float Lifetime = .36f;
        static ParticleSystem particles;
        static int createdCount;
        int floorNumber;

        public static int CreatedCount { get { return createdCount; } }
        /// <summary>Number of currently simulated debris fragments.</summary>
        public static int ActiveCount { get { return particles != null ? particles.particleCount : 0; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntime()
        {
            if (particles != null)
            {
                particles.gameObject.SetActive(false);
                Destroy(particles.gameObject);
            }
            particles = null; createdCount = 0;
        }

        public static void Warmup()
        {
            if (particles != null) return;
            // Recover the pool after script reloads instead of leaking a second system.
            var root = GameObject.Find(PoolName);
            if (root == null) root = new GameObject(PoolName);
            var owner = root.GetComponent<Effects>();
            if (owner == null) owner = root.AddComponent<Effects>();
            particles = root.GetComponent<ParticleSystem>();
            if (particles == null) particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false; main.loop = false; main.duration = Lifetime;
            main.startLifetime = Lifetime; main.startSpeed = 0; main.startSize = 1f / 12;
            main.startColor = Color.white; main.gravityModifier = 0;
            main.maxParticles = 512; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.useUnscaledTime = false; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape; shape.enabled = false;
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.speedModifier = new ParticleSystem.MinMaxCurve(1,
                AnimationCurve.Linear(0, 1, 1, Mathf.Pow(.03f, Lifetime)));
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = SpriteLibrary.Material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 2000;
            particles.Play(false);
            var game = GameManager.Instance;
            owner.floorNumber = game != null && game.Floors != null ? game.Floors.CurrentFloor : 0;
            createdCount = 1;
        }

        public static void ClearActive()
        {
            if (particles == null)
            {
                var existing = GameObject.Find(PoolName);
                if (existing == null) return;
                particles = existing.GetComponent<ParticleSystem>();
                if (particles == null) return;
                createdCount = 1;
            }
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(false);
        }

        public static void Burst(Vector2 position, Color color, int count = 8)
        {
            if (count <= 0) return;
            Warmup();
            if (!particles.isPlaying) particles.Play(false);
            count = Mathf.Min(count, 32);
            var emit = new ParticleSystem.EmitParams { position = position, startColor = color, startLifetime = Lifetime };
            for (int i = 0; i < count; i++)
            {
                emit.velocity = Random.insideUnitCircle.normalized * Random.Range(.7f, 3f);
                emit.startSize = Random.Range(1f / 24, 5f / 48);
                particles.Emit(emit, 1);
            }
        }

        void LateUpdate()
        {
            var game = GameManager.Instance;
            if (game == null || game.Floors == null) return;
            if (floorNumber != 0 && floorNumber != game.Floors.CurrentFloor) ClearActive();
            floorNumber = game.Floors.CurrentFloor;
        }

        void OnDestroy()
        {
            if (particles != null && particles.gameObject == gameObject) particles = null;
        }
    }
}
