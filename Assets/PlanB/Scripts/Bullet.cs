using System.Collections.Generic;
using UnityEngine;

namespace PlanB
{
    /// <summary>A bounded, warmed projectile pool independent of floor-root replacement.</summary>
    public sealed class Bullet : MonoBehaviour
    {
        const string PoolName = "PlanB Projectile Pool";
        const int MaximumCapacity = 128;
        static readonly List<Bullet> Pool = new List<Bullet>(MaximumCapacity);
        static readonly ContactFilter2D HitFilter = MakeFilter();
        static Transform poolRoot;
        static int activeCount, createdCount, recycleIndex;
        readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        SpriteRenderer sprite;
        TrailRenderer trail;
        Vector2 direction;
        bool hostile, inUse;
        int damage, floorNumber;
        float speed, life;

        public static int CreatedCount { get { return createdCount; } }
        public static int ActiveCount { get { return activeCount; } }

        static ContactFilter2D MakeFilter()
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.AllLayers);
            return filter;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntime()
        {
            if (poolRoot != null)
            {
                poolRoot.gameObject.SetActive(false);
                Destroy(poolRoot.gameObject);
            }
            poolRoot = null;
            Pool.Clear();
            activeCount = createdCount = recycleIndex = 0;
        }

        public static void Warmup(int capacity = 64)
        {
            EnsureRoot();
            Sprite round = SpriteLibrary.Get("bullet");
            SpriteLibrary.Get("laser");
            capacity = Mathf.Clamp(capacity, 16, MaximumCapacity);
            for (int i = Pool.Count - 1; i >= 0; i--) if (Pool[i] == null) Pool.RemoveAt(i);
            while (Pool.Count < capacity)
            {
                var go = new GameObject("Pooled projectile");
                go.transform.SetParent(poolRoot, false);
                go.SetActive(false);
                var bullet = go.AddComponent<Bullet>();
                bullet.BindVisuals();
                bullet.sprite.sprite = round;
                Pool.Add(bullet);
                createdCount++;
            }
        }

        static bool EnsureRoot(bool create = true)
        {
            if (poolRoot != null) return true;
            // Script reloads can retain scene objects while resetting static references.
            var previous = GameObject.Find(PoolName);
            if (previous == null && !create) return false;
            Pool.Clear(); activeCount = createdCount = recycleIndex = 0;
            if (previous != null)
            {
                poolRoot = previous.transform;
                foreach (var bullet in previous.GetComponentsInChildren<Bullet>(true))
                {
                    bullet.BindVisuals();
                    bullet.inUse = false;
                    bullet.trail.emitting = false;
                    bullet.trail.Clear();
                    bullet.gameObject.SetActive(false);
                    Pool.Add(bullet);
                }
                createdCount = Pool.Count;
            }
            else poolRoot = new GameObject(PoolName).transform;
            return true;
        }

        void BindVisuals()
        {
            if (sprite == null) sprite = GetComponent<SpriteRenderer>();
            if (sprite == null) sprite = gameObject.AddComponent<SpriteRenderer>();
            sprite.sharedMaterial = SpriteLibrary.Material;
            sprite.sortingOrder = 350;
            if (trail == null) trail = GetComponent<TrailRenderer>();
            if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = SpriteLibrary.Material;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sortingOrder = 349;
            trail.autodestruct = false; trail.emitting = false;
            trail.minVertexDistance = .08f;
            trail.numCornerVertices = 0; trail.numCapVertices = 0;
            trail.endWidth = 0;
            trail.endColor = new Color(1, .3f, .05f, 0);
        }

        public static Bullet Spawn(Vector2 position, Vector2 direction, bool hostile, int damage, float speed, Transform parent = null)
        {
            if (poolRoot == null || Pool.Count == 0) Warmup();
            Bullet bullet = null;
            for (int i = 0; i < Pool.Count; i++)
                if (Pool[i] != null && !Pool[i].inUse) { bullet = Pool[i]; break; }
            if (bullet == null)
            {
                // Saturation recycles one projectile instead of allocating during combat.
                recycleIndex %= Pool.Count;
                bullet = Pool[recycleIndex++];
                if (bullet == null) { Warmup(); return Spawn(position, direction, hostile, damage, speed, parent); }
                bullet.Release();
            }
            bullet.direction = direction.sqrMagnitude > .001f ? direction.normalized : Vector2.right;
            bullet.hostile = hostile; bullet.damage = damage; bullet.speed = Mathf.Max(0, speed); bullet.life = 3;
            var game = GameManager.Instance;
            bullet.floorNumber = game != null && game.Floors != null ? game.Floors.CurrentFloor : 0;
            bullet.transform.SetPositionAndRotation(position,
                Quaternion.Euler(0, 0, Mathf.Atan2(bullet.direction.y, bullet.direction.x) * Mathf.Rad2Deg));
            bullet.sprite.sprite = SpriteLibrary.Get(hostile ? "laser" : "bullet");
            bullet.trail.time = hostile ? .12f : .055f;
            bullet.trail.startWidth = hostile ? .065f : .035f;
            bullet.trail.startColor = hostile ? new Color(1, .12f, .15f, .8f) : new Color(1, .8f, .3f, .8f);
            bullet.trail.Clear();
            bullet.inUse = true; activeCount++;
            bullet.gameObject.SetActive(true);
            bullet.trail.emitting = true;
            return bullet;
        }

        public static void ClearActive()
        {
            if (!EnsureRoot(false)) return;
            for (int i = 0; i < Pool.Count; i++) if (Pool[i] != null) Pool[i].Release();
            activeCount = 0;
        }

        void Release()
        {
            if (inUse) { inUse = false; activeCount = Mathf.Max(0, activeCount - 1); }
            if (trail != null) { trail.emitting = false; trail.Clear(); }
            gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (inUse) { inUse = false; activeCount = Mathf.Max(0, activeCount - 1); }
            if (trail != null) { trail.emitting = false; trail.Clear(); }
        }

        void OnDestroy()
        {
            if (inUse) activeCount = Mathf.Max(0, activeCount - 1);
            Pool.Remove(this);
        }

        void Update()
        {
            if (!inUse) return;
            var game = GameManager.Instance;
            if (game == null || game.Floors == null || (floorNumber != 0 && game.Floors.CurrentFloor != floorNumber))
            { Release(); return; }
            if (!game.IsPlaying) return;
            float step = speed * Time.deltaTime;
            Vector2 origin = transform.position;
            int count = Physics2D.Raycast(origin, direction, HitFilter, hits, step);
            Collider2D closest = null; float distance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var collider = hits[i].collider;
                if (collider == null) continue;
                var enemy = collider.GetComponentInParent<Enemy>();
                var player = collider.GetComponentInParent<Player>();
                if (enemy != null && (hostile || enemy.IsDead)) continue;
                if (player != null && !hostile) continue;
                if (hits[i].distance < distance) { closest = collider; distance = hits[i].distance; }
            }
            if (closest != null)
            {
                var enemy = closest.GetComponentInParent<Enemy>();
                var player = closest.GetComponentInParent<Player>();
                if (enemy != null) enemy.Damage(damage);
                else if (player != null) player.Damage(damage);
                else AudioFactory.PlayAt("hit", origin + direction * distance, .15f);
                Effects.Burst(origin + direction * distance, hostile ? Color.red : new Color(1, .7f, .25f), 3);
                Release(); return;
            }
            transform.position = origin + direction * step;
            life -= Time.deltaTime;
            if (life <= 0) Release();
        }
    }
}
