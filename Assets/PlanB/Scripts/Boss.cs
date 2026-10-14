using UnityEngine;

namespace PlanB
{
    /// <summary>The Warden wakes when the security door is opened.</summary>
    public sealed class Boss : Enemy
    {
        private float nextBurst;
        private bool summonedCrawlers;
        private bool awakened;

        protected override bool CanAct(GameManager game)
        {
            return base.CanAct(game) && game.DoorUnlocked && game.Floors.CurrentFloor == 2;
        }

        protected override void Think(GameManager game, Vector2 difference, float distance)
        {
            if (!awakened)
            {
                awakened = true;
                nextBurst = Time.time + 2.8f;
                game.Notify("WARDEN AWAKENED — keep moving", 3);
                AudioFactory.Play("boss_roar", .7f);
            }
            if (distance > .75f) Move(game, game.Player.transform.position);
            if (Time.time < nextBurst) return;
            nextBurst = Time.time + 5;
            AudioFactory.PlayAt("boss_burst", transform.position, .65f);
            float offset = Random.Range(0f, 30f);
            for (int i = 0; i < 12; i++)
            {
                float radians = (i * 30 + offset) * Mathf.Deg2Rad;
                Fire(new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)), 12, 3.7f);
            }
            if (game.CameraRig != null) game.CameraRig.Shake(.1f);
            Effects.Burst(transform.position, new Color(.95f, .15f, .2f), 12);
        }

        public override void Damage(int damage)
        {
            var game = GameManager.Instance;
            if (game == null || !game.DoorUnlocked || game.Floors == null || game.Floors.CurrentFloor != 2) return;
            base.Damage(damage);
            if (IsDead || summonedCrawlers || Health > MaxHealth / 2) return;
            summonedCrawlers = true;
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = new Vector2(i == 0 ? -1 : 1, -.3f);
                Enemy crawler = Spawn(EnemyKind.Crawler, (Vector2)transform.position + offset, transform.parent);
                crawler.FloorNumber = 2;
                game.Floors.RegisterEnemy(crawler, 2);
                Effects.Burst(crawler.transform.position, new Color(.55f, .25f, .45f), 8);
            }
            game.Notify("The Warden called reinforcements", 2.5f);
        }

        protected override void Die()
        {
            if (IsDead) return;
            AudioFactory.Play("boss_death", .75f);
            var game = GameManager.Instance;
            if (game != null && game.Floors != null) game.Floors.BossKilled();
            base.Die();
            if (game != null && game.CameraRig != null) game.CameraRig.Shake(.3f);
        }
    }
}
