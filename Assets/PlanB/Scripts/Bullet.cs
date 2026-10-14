using UnityEngine;

namespace PlanB
{
    public sealed class Bullet : MonoBehaviour
    {
        readonly RaycastHit2D[] hits = new RaycastHit2D[24];
        Vector2 direction;
        bool hostile;
        int damage;
        float speed, life;

        public static Bullet Spawn(Vector2 position, Vector2 direction, bool hostile, int damage, float speed, Transform parent = null)
        {
            var go = new GameObject(hostile ? "Red laser" : "Pistol round");
            if (parent != null) go.transform.SetParent(parent);
            go.transform.position = position;
            var bullet = go.AddComponent<Bullet>();
            bullet.direction = direction.sqrMagnitude > .001f ? direction.normalized : Vector2.right;
            bullet.hostile = hostile; bullet.damage = damage; bullet.speed = speed; bullet.life = 3;
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = SpriteLibrary.Get(hostile ? "laser" : "bullet"); sprite.sharedMaterial = SpriteLibrary.Material; sprite.sortingOrder = 350;
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = SpriteLibrary.Material; trail.time = hostile ? .12f : .055f;
            trail.startWidth = hostile ? .065f : .035f; trail.endWidth = 0;
            trail.startColor = hostile ? new Color(1, .12f, .15f, .8f) : new Color(1, .8f, .3f, .8f);
            trail.endColor = new Color(1, .3f, .05f, 0); trail.sortingOrder = 349;
            return bullet;
        }

        void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            float step = speed * Time.deltaTime;
            Vector2 origin = transform.position;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.AllLayers);
            int count = Physics2D.Raycast(origin, direction, filter, hits, step);
            Collider2D closest = null; float distance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var collider = hits[i].collider;
                if (collider == null || collider.isTrigger) continue;
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
                Effects.Burst(origin + direction * distance, hostile ? Color.red : new Color(1, .7f, .25f), 3);
                Destroy(gameObject); return;
            }
            transform.position = origin + direction * step;
            life -= Time.deltaTime;
            if (life <= 0) Destroy(gameObject);
        }
    }
}
