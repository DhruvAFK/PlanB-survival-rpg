using UnityEngine;

namespace PlanB
{
    public sealed class Trap : MonoBehaviour
    {
        private bool spent;
        private float armedAt;

        public static bool TryPlace()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying || gm.Player == null) return false;
            if (gm.TrapCount <= 0) { gm.Notify("No traps carried. Search the supply rooms.", 2); return false; }
            gm.TrapCount--;
            Place(gm.Player.transform.position, gm.Floors == null ? null : gm.Floors.ActiveRoot);
            gm.Notify("Trap armed • 4 damage and 2 seconds of stun.", 2);
            AudioFactory.Play("reload", .45f);
            return true;
        }

        public static Trap Place(Vector2 position, Transform parent = null)
        {
            var renderer = LevelBuilder.Sprite("trap", position, parent, 12, new Vector2(.85f, .7f));
            var trap = renderer.gameObject.AddComponent<Trap>();
            trap.armedAt = Time.time + .3f;
            return trap;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (spent || gm == null || !gm.IsPlaying || Time.time < armedAt) return;
            var hits = Physics2D.OverlapCircleAll(transform.position, .62f);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDead) continue;
                spent = true;
                enemy.Stun(2);
                enemy.Damage(4);
                AudioFactory.Play("hit", .9f);
                var renderer = GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.color = new Color(.4f, .3f, .3f);
                Destroy(gameObject, .5f);
                break;
            }
        }
    }
}

