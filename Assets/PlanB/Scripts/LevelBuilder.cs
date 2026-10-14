using System.Collections.Generic;
using UnityEngine;

namespace PlanB
{
    // Walls are merged in each row, so actors cannot slip between individual tiles.
    public sealed class SolidWall : MonoBehaviour { }

    public sealed class LevelBuilder : MonoBehaviour
    {
        public const int Width = 48;
        public const int Height = 28;
        public int FloorNumber { get; private set; }
        private string[] map;
        private readonly int[,] distances = new int[Width, Height];
        private readonly Queue<Vector2Int> frontier = new Queue<Vector2Int>(Width * Height);
        private Vector2Int lastTarget = new Vector2Int(-999, -999);
        private bool lastGateOpen;
        private readonly HashSet<Vector2Int> furniture = new HashSet<Vector2Int>();
        private static readonly Vector2Int[] Neighbors =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        private static readonly string[] Floor1Map =
        {
            "################################################",
            "#............#...................#mmmmmmmmmmmmm#",
            "#............#...................#mmmmmmmmmmmmm#",
            "#............#...................#mmmmmmmmmmmmm#",
            "#............#...................#mmmmmmmmmmmmm#",
            "#............#...................#mmmmmmmmmmmmm#",
            "#............#..#######..#########mmmmmmmmmmmmm#",
            "#...............#...............##mmmmmmmmmmmmm#",
            "#...............#...............#.mmmmmmmmmmmmm#",
            "#............#..#..ccccccccccc..#.mmmmmmmmmmmmm#",
            "#............#.....ccccccccccc..##mmmmmmmmmmmmm#",
            "######..######.....ccccccccccc...#mmmmmmmmmmmmm#",
            "#######..########..ccccccccccc...#mmmmmmmmmmmmm#",
            "#..............##..ccccccccccc..##mmmmmmmmmmmmm#",
            "#..............##..ccccccccccc..########..######",
            "#..............##..ccccccccccc..#..............#",
            "#..............##...............#..............#",
            "#..............##...............#..............#",
            "#...............########..#######..............#",
            "#..............................................#",
            "#..............#...mmmmmmmmmmm.................#",
            "#..............#...mmmmmmmmmmm.................#",
            "#..............#...mmmmmmmmmmm.................#",
            "#..............#...mmmmmmmmmmm.................#",
            "#..............#...mmmmmmmmmmm.................#",
            "#..............#...mmmmmmmmmmm.................#",
            "#..............#...mmmmmmmmmmm.................#",
            "################################################",
        };

        private static readonly string[] Floor2Map =
        {
            "################################################",
            "#mmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmm#",
            "#mmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmm#",
            "#mmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmm#",
            "#mmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmm#",
            "#mmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmm#",
            "#mmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmm#",
            "#mmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmm#",
            "#mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm#",
            "#mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm#",
            "#mmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmm#",
            "#mmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmm#",
            "#######mm######mmmmmmmmmmmmmmmm########mm#######",
            "#mmmmmmmmmmmmmmm######mm######mmmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmm#mmmmmmmmmmmm#mmmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmm#mmmmmmmmmmmm#mmmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmmmmmmmmmmmmmmmm##################",
            "#mhhhhhhmmmmmmmmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmm#mmmmmmmmmmmm##mmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmm#######mm######mmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#",
            "#mhhhhhhmmmmmmmmmmmmmmmmmmmmmm#mmmmmmmmmmmmmmmm#",
            "################################################",
        };

        public static Vector2 Cell(int column, int row) { return new Vector2(column - 24, 14 - row); }
        public static Vector2Int Grid(Vector2 position)
        {
            return new Vector2Int(Mathf.RoundToInt(position.x + 24), Mathf.RoundToInt(14 - position.y));
        }

        public void Build(FloorManager manager, int floor)
        {
            FloorNumber = floor;
            map = floor == 1 ? Floor1Map : Floor2Map;
            var ground = new GameObject("Ground and wall sprites").transform;
            ground.SetParent(transform, false);
            for (int r = 0; r < Height; r++)
            {
                for (int c = 0; c < Width; c++)
                {
                    char tile = map[r][c];
                    string key = tile == '#' ? "tile_wall" : tile == 'm' || tile == 'h' ? "tile_metal" : "tile_wood";
                    if (tile != '#' && tile != 'c' && (c * 7 + r * 11) % 37 == 0) key = "tile_cracked";
                    var piece = Sprite(key, Cell(c, r), ground, tile == '#' ? 30 : -100);
                    if (tile != '#' && floor == 2) piece.color = new Color(.73f, .84f, .93f);
                    if (tile == 'c') Sprite("carpet", Cell(c, r), ground, -95);
                    if (tile == 'h' && (c == 2 || c == 7)) piece.color = (r % 2 == 0) ? new Color(.82f, .58f, .18f) : new Color(.29f, .31f, .29f);
                }
                // Merge all adjoining solid tiles on this row into a single collider.
                int begin = -1;
                for (int c = 0; c <= Width; c++)
                {
                    bool solid = c < Width && map[r][c] == '#';
                    if (solid && begin < 0) begin = c;
                    if (!solid && begin >= 0)
                    {
                        var wall = new GameObject("Wall " + r + ":" + begin);
                        wall.transform.SetParent(transform, false);
                        wall.layer = 8;
                        wall.transform.position = Cell(begin, r) + Vector2.right * ((c - begin - 1) * .5f);
                        wall.AddComponent<SolidWall>();
                        wall.AddComponent<BoxCollider2D>().size = new Vector2(c - begin, 1);
                        begin = -1;
                    }
                }
            }
            if (floor == 1) FurnishResidential(manager); else FurnishResearch(manager);
        }

        public static SpriteRenderer Sprite(string key, Vector2 position, Transform parent, int sorting = 20, Vector2? size = null)
        {
            var go = new GameObject(key);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteLibrary.Get(key);
            if (SpriteLibrary.Material != null) renderer.sharedMaterial = SpriteLibrary.Material;
            renderer.sortingOrder = sorting;
            Vector2 desired = size ?? Vector2.one;
            if (renderer.sprite != null)
            {
                Vector2 dimensions = renderer.sprite.bounds.size;
                go.transform.localScale = new Vector3(desired.x / Mathf.Max(.01f, dimensions.x), desired.y / Mathf.Max(.01f, dimensions.y), 1);
            }
            return renderer;
        }

        private void Prop(string key, int c, int r, float width = 1.5f, float height = 1.5f, bool solid = false)
        {
            var renderer = Sprite(key, Cell(c, r), transform, 40 - Mathf.RoundToInt(Cell(c, r).y * 2), new Vector2(width, height));
            if (solid)
            {
                furniture.Add(new Vector2Int(c, r));
                renderer.gameObject.layer = 8;
                var collider = renderer.gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(.72f / Mathf.Max(.01f, renderer.transform.localScale.x), .55f / Mathf.Max(.01f, renderer.transform.localScale.y));
                collider.offset = new Vector2(0, -.18f / Mathf.Max(.01f, renderer.transform.localScale.y));
            }
        }

        private T Object<T>(string key, int c, int r, Vector2 size) where T : Component
        {
            return Sprite(key, Cell(c, r), transform, 55, size).gameObject.AddComponent<T>();
        }

        private void Label(string value, int c, int r, Color color, float scale = .14f)
        {
            var go = new GameObject(value);
            go.transform.SetParent(transform, false);
            go.transform.position = Cell(c, r);
            var label = go.AddComponent<TextMesh>();
            label.text = value; label.fontSize = 42; label.characterSize = scale;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.color = color;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = -70;
        }

        private void Ammo(params Vector2Int[] positions)
        {
            foreach (var p in positions) Pickup.Spawn(PickupKind.Ammo, Cell(p.x, p.y), transform);
        }

        private void SpawnEnemies(FloorManager manager, EnemyKind kind, params Vector2Int[] cells)
        {
            foreach (var cell in cells)
            {
                var enemy = Enemy.Spawn(kind, Cell(cell.x, cell.y), transform);
                if (enemy != null) manager.RegisterEnemy(enemy, FloorNumber);
            }
        }

        private void FurnishResidential(FloorManager manager)
        {
            Label("RESIDENTIAL", 7, 2, new Color(.72f, .60f, .38f));
            Label("LOUNGE", 25, 8, new Color(.76f, .57f, .31f));
            Label("MAINTENANCE", 40, 2, new Color(.76f, .65f, .32f), .11f);
            Label("KITCHEN / STORAGE", 8, 14, new Color(.72f, .60f, .38f), .10f);
            Label("ELEVATOR", 24, 22, new Color(.82f, .66f, .32f), .11f);
            Prop("bed", 5, 5, 2.6f, 3.4f, true);
            Prop("table", 9, 5, 1.6f, 1.3f, true);
            Prop("plant", 11, 3);
            Prop("sofa", 24, 10, 4.2f, 1.9f, true);
            Prop("table", 24, 13, 2.3f, 1.7f, true);
            Prop("plant", 19, 16);
            Prop("plant", 30, 8);
            Prop("computer", 3, 15, 1.6f, 2);
            Prop("table", 7, 16, 3.7f, 1.3f, true);
            Prop("table", 12, 17, 1.8f, 2.8f, true);
            Prop("crate", 3, 25); Prop("crate", 9, 25); Prop("crate", 12, 25);
            Prop("computer", 44, 7, 1.8f, 2.2f);
            Prop("crate", 37, 11); Prop("crate", 43, 11);
            Prop("crate", 35, 25); Prop("crate", 43, 25);
            Object<Cabinet>("cabinet", 3, 3, new Vector2(1.4f, 2)).GuaranteedAmmo = true;
            Object<Cabinet>("cabinet", 11, 21, new Vector2(1.4f, 2)).GuaranteedAmmo = true;
            Object<Cabinet>("cabinet", 29, 10, new Vector2(1.4f, 2));
            Object<Cabinet>("cabinet", 43, 4, new Vector2(1.4f, 2));
            Object<Door>("door", 13, 7, new Vector2(1, 1.8f));
            Object<Door>("door", 33, 8, new Vector2(1, 1.8f));
            Object<Generator>("generator", 40, 5, new Vector2(3, 3));
            var lift = Object<Elevator>("elevator", 24, 25, new Vector2(3.2f, 2.7f));
            lift.FloorNumber = 1;
            var scientist = Object<Scientist>("scientist", 18, 4, new Vector2(.9f, 1.3f));
            scientist.FloorNumber = 1;
            Ammo(new Vector2Int(7,9), new Vector2Int(7,18), new Vector2Int(12,24),
                 new Vector2Int(22,15), new Vector2Int(37,7), new Vector2Int(36,21));
            Pickup.Spawn(PickupKind.Medkit, Cell(10, 8), transform);
            Pickup.Spawn(PickupKind.Medkit, Cell(28, 22), transform);
            Pickup.Spawn(PickupKind.Fuse, Cell(5, 23), transform);
            Pickup.Spawn(PickupKind.Scrap, Cell(4, 24), transform);
            Pickup.Spawn(PickupKind.Battery, Cell(38, 10), transform);
            Pickup.Spawn(PickupKind.Wire, Cell(36, 4), transform);
            Pickup.Spawn(PickupKind.Trap, Cell(11, 22), transform);
            SpawnEnemies(manager, EnemyKind.Walker, new Vector2Int(9,18), new Vector2Int(4,21),
                new Vector2Int(11,24), new Vector2Int(22,10), new Vector2Int(29,15),
                new Vector2Int(40,9), new Vector2Int(44,10), new Vector2Int(34,22));
            SpawnEnemies(manager, EnemyKind.Runner, new Vector2Int(12,15), new Vector2Int(24,4), new Vector2Int(43,23));
        }

        private void FurnishResearch(FloorManager manager)
        {
            Label("RESEARCH", 7, 2, new Color(.43f, .64f, .76f));
            Label("SERVERS", 40, 2, new Color(.43f, .64f, .76f));
            Label("QUARANTINE", 40, 18, new Color(.72f, .27f, .25f), .12f);
            Label("SECURITY", 23, 15, new Color(.43f, .64f, .76f), .11f);
            Label("EXIT LIFT", 24, 23, new Color(.43f, .64f, .76f), .10f);
            Prop("computer", 5, 4, 2.2f, 1.9f, true);
            Prop("computer", 10, 4, 2.2f, 1.9f, true);
            Prop("table", 7, 7, 3.1f, 1.3f, true);
            Prop("plant", 2, 9); Prop("plant", 13, 2);
            Prop("computer", 35, 4, 1.7f, 2.8f, true);
            Prop("computer", 39, 4, 1.7f, 2.8f, true);
            Prop("computer", 44, 9, 1.7f, 2.8f, true);
            Prop("computer", 20, 16, 2, 2);
            Prop("crate", 12, 24); Prop("crate", 14, 24);
            Prop("plant", 19, 22);
            Prop("bed", 44, 24, 2.3f, 2.8f, true);
            Prop("table", 44, 20, 2.2f, 1.3f, true);
            Object<Cabinet>("cabinet", 3, 4, new Vector2(1.4f, 2)).GuaranteedAmmo = true;
            Object<Cabinet>("cabinet", 26, 3, new Vector2(1.4f, 2)).GuaranteedAmmo = true;
            Object<Cabinet>("cabinet", 44, 11, new Vector2(1.4f, 2));
            Object<Cabinet>("cabinet", 42, 25, new Vector2(1.4f, 2));
            var gate = Object<Door>("lockeddoor", 30, 21, new Vector2(1.4f, 2.4f));
            gate.transform.position = (Cell(30, 21) + Cell(30, 22)) * .5f;
            gate.Locked = true;
            gate.ConfigureGate(new Vector2(1, 2));
            var lift = Object<Elevator>("elevator", 24, 25, new Vector2(3.2f, 2.7f));
            lift.FloorNumber = 2;
            var scientist = Object<Scientist>("scientist", 26, 21, new Vector2(.9f, 1.3f));
            scientist.FloorNumber = 2;
            var camera = Object<SecurityCamera>("security_camera", 36, 10, new Vector2(1.2f, 1));
            camera.Direction = Vector2.left;
            Ammo(new Vector2Int(8,9), new Vector2Int(16,23), new Vector2Int(26,8),
                 new Vector2Int(42,7), new Vector2Int(39,20), new Vector2Int(22,25));
            Pickup.Spawn(PickupKind.Medkit, Cell(6,10), transform);
            Pickup.Spawn(PickupKind.Medkit, Cell(28,21), transform);
            Pickup.Spawn(PickupKind.Medkit, Cell(34,25), transform);
            Pickup.Spawn(PickupKind.Key, Cell(42,5), transform);
            Pickup.Spawn(PickupKind.Scrap, Cell(12,21), transform);
            Pickup.Spawn(PickupKind.Battery, Cell(36,6), transform);
            Pickup.Spawn(PickupKind.Wire, Cell(21,17), transform);
            Pickup.Spawn(PickupKind.Trap, Cell(27,5), transform);
            SpawnEnemies(manager, EnemyKind.Crawler, new Vector2Int(6,9), new Vector2Int(11,6),
                new Vector2Int(12,19), new Vector2Int(26,10), new Vector2Int(36,8), new Vector2Int(43,6));
            SpawnEnemies(manager, EnemyKind.Spitter, new Vector2Int(10,16), new Vector2Int(20,7),
                new Vector2Int(40,10), new Vector2Int(27,17));
            SpawnEnemies(manager, EnemyKind.Warden, new Vector2Int(40,22));
        }

        public bool IsWalkable(int column, int row)
        {
            if (column < 0 || column >= Width || row < 0 || row >= Height || map == null || map[row][column] == '#' || furniture.Contains(new Vector2Int(column, row))) return false;
            if (FloorNumber == 2 && column == 30 && (row == 21 || row == 22))
            {
                var gm = GameManager.Instance;
                return gm != null && gm.DoorUnlocked;
            }
            return true;
        }

        public void InvalidatePaths() { lastTarget = new Vector2Int(-999, -999); }

        public Vector2 Steer(Vector2 from, Vector2 target)
        {
            Vector2 delta = target - from;
            if (delta.sqrMagnitude < .04f) return delta;
            bool clear = true;
            int samples = Mathf.CeilToInt(delta.magnitude * 3);
            for (int i = 1; i <= samples; i++)
            {
                Vector2Int cell = Grid(Vector2.Lerp(from, target, (float)i / samples));
                if (!IsWalkable(cell.x, cell.y)) { clear = false; break; }
            }
            if (clear) return delta.normalized;
            var goal = Grid(target);
            var current = Grid(from);
            if (!IsWalkable(goal.x, goal.y) || !IsWalkable(current.x, current.y)) return Vector2.zero;
            bool open = GameManager.Instance != null && GameManager.Instance.DoorUnlocked;
            if (goal != lastTarget || open != lastGateOpen)
            {
                lastTarget = goal; lastGateOpen = open;
                for (int r = 0; r < Height; r++) for (int c = 0; c < Width; c++) distances[c, r] = -1;
                frontier.Clear(); frontier.Enqueue(goal); distances[goal.x, goal.y] = 0;
                while (frontier.Count > 0)
                {
                    var cell = frontier.Dequeue();
                    foreach (var step in Neighbors)
                    {
                        var next = cell + step;
                        if (!IsWalkable(next.x, next.y) || distances[next.x, next.y] >= 0) continue;
                        distances[next.x, next.y] = distances[cell.x, cell.y] + 1;
                        frontier.Enqueue(next);
                    }
                }
            }
            int bestDistance = distances[current.x, current.y];
            Vector2Int best = current;
            foreach (var step in Neighbors)
            {
                var next = current + step;
                if (!IsWalkable(next.x, next.y)) continue;
                int d = distances[next.x, next.y];
                if (d >= 0 && (bestDistance < 0 || d < bestDistance)) { bestDistance = d; best = next; }
            }
            return best == current ? delta.normalized : (Cell(best.x, best.y) - from).normalized;
        }
    }
}

