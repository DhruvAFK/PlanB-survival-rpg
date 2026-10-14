using System.Collections.Generic;
using UnityEngine;

namespace PlanB
{
    public sealed class FloorManager : MonoBehaviour
    {
        public int CurrentFloor = 1;
        public Transform ActiveRoot { get; private set; }
        public LevelBuilder ActiveLevel { get; private set; }
        [SerializeField] private LevelBuilder[] levels = new LevelBuilder[2];
        [SerializeField] private List<Enemy> enemies = new List<Enemy>();
        public int EnemiesRemaining
        {
            get
            {
                int count = 0;
                foreach (var enemy in enemies)
                    if (enemy != null && !enemy.IsDead && enemy.FloorNumber == CurrentFloor) count++;
                return count;
            }
        }

        public void Build()
        {
            Clear();
            for (int i = 0; i < levels.Length; i++)
            {
                var root = new GameObject(i == 0 ? "Floor 1 - Residential" : "Floor 2 - Research");
                root.transform.SetParent(transform, false);
                levels[i] = root.AddComponent<LevelBuilder>();
                levels[i].Build(this, i + 1);
                root.SetActive(false);
            }
            EnterFloor(1);
        }

        public void EnterFloor(int floor)
        {
            floor = Mathf.Clamp(floor, 1, 2);
            if (levels[floor - 1] == null) return;
            for (int i = 0; i < levels.Length; i++) if (levels[i] != null) levels[i].gameObject.SetActive(i == floor - 1);
            CurrentFloor = floor;
            ActiveLevel = levels[floor - 1];
            ActiveRoot = ActiveLevel.transform;
            var gm = GameManager.Instance;
            if (gm != null && gm.Player != null)
            {
                Vector2 spawn = SpawnPoint(floor);
                gm.Player.transform.position = spawn;
                var body = gm.Player.GetComponent<Rigidbody2D>();
                if (body != null) body.position = spawn;
            }
            Physics2D.SyncTransforms();
        }

        public void RegisterEnemy(Enemy enemy, int floor)
        {
            if (enemy == null) return;
            enemy.FloorNumber = floor;
            if (!enemies.Contains(enemy)) enemies.Add(enemy);
        }

        public Vector2 SpawnPoint(int floor)
        {
            return floor == 1 ? LevelBuilder.Cell(5, 8) : LevelBuilder.Cell(24, 22);
        }

        public Vector2 Steer(Vector2 from, Vector2 target)
        {
            return ActiveLevel == null ? (target - from).normalized : ActiveLevel.Steer(from, target);
        }

        public void BossKilled()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.BossDefeated) return;
            gm.BossDefeated = true;
            gm.Notify("WARDEN DEFEATED  •  Return to the exit elevator.", 5);
            gm.RefreshObjective();
            AudioFactory.Play("elevator");
        }

        public void Clear()
        {
            enemies.Clear();
            // Enumerate owned roots as well: an editor assembly reload can reset managed caches.
            foreach (var ownedLevel in GetComponentsInChildren<LevelBuilder>(true))
            {
                ownedLevel.gameObject.SetActive(false);
                Destroy(ownedLevel.gameObject);
            }
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = null;
            }
            CurrentFloor = 1; ActiveRoot = null; ActiveLevel = null;
        }
    }
}

