using UnityEngine;

namespace PlanB
{
    // A few shared flow fields prevent spitters' retreat targets from invalidating every chaser's route.
    public sealed class GridNavigationCache
    {
        const int Width = LevelBuilder.Width, Height = LevelBuilder.Height, Size = Width * Height;
        sealed class Field
        {
            public readonly short[] Distance = new short[Size];
            public int Goal = -1, Used;
        }
        readonly Field[] fields = new Field[6];
        readonly bool[] walkable = new bool[Size];
        readonly int[] queue = new int[Size];
        LevelBuilder level;
        bool gate;
        int tick;
        public int BuildCount { get; private set; }
        public int CacheHits { get; private set; }

        public GridNavigationCache() { for (int i = 0; i < fields.Length; i++) fields[i] = new Field(); }
        public void Reset() { level = null; for (int i = 0; i < fields.Length; i++) fields[i].Goal = -1; }
        bool Open(int x, int y) { return x >= 0 && x < Width && y >= 0 && y < Height && walkable[y * Width + x]; }

        public Vector2 Steer(LevelBuilder map, Vector2 from, Vector2 target, bool gateOpen)
        {
            if (map == null) return (target - from).normalized;
            if (level != map || gate != gateOpen)
            {
                level = map; gate = gateOpen;
                for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) walkable[y * Width + x] = map.IsWalkable(x, y);
                for (int i = 0; i < fields.Length; i++) fields[i].Goal = -1;
            }
            Vector2 delta = target - from;
            if (delta.sqrMagnitude < .04f) return delta;
            int samples = Mathf.CeilToInt(delta.magnitude * 3);
            bool clear = true;
            for (int i = 1; i <= samples; i++)
            {
                var cell = LevelBuilder.Grid(Vector2.Lerp(from, target, (float)i / samples));
                if (!Open(cell.x, cell.y)) { clear = false; break; }
            }
            if (clear) return delta.normalized;
            var start = LevelBuilder.Grid(from); var goal = LevelBuilder.Grid(target);
            if (!Open(start.x, start.y) || !Open(goal.x, goal.y)) return Vector2.zero;
            int goalIndex = goal.y * Width + goal.x;
            Field field = null, oldest = fields[0];
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Goal == goalIndex) { field = fields[i]; break; }
                if (fields[i].Goal < 0 || fields[i].Used < oldest.Used) oldest = fields[i];
            }
            if (field == null)
            {
                field = oldest; field.Goal = goalIndex; BuildCount++;
                for (int i = 0; i < Size; i++) field.Distance[i] = -1;
                int head = 0, tail = 0; queue[tail++] = goalIndex; field.Distance[goalIndex] = 0;
                while (head < tail)
                {
                    int current = queue[head++], x = current % Width, y = current / Width;
                    short distance = (short)(field.Distance[current] + 1);
                    if (x > 0) Visit(current - 1, distance, field, ref tail);
                    if (x + 1 < Width) Visit(current + 1, distance, field, ref tail);
                    if (y > 0) Visit(current - Width, distance, field, ref tail);
                    if (y + 1 < Height) Visit(current + Width, distance, field, ref tail);
                }
            }
            else CacheHits++;
            field.Used = ++tick;
            int currentIndex = start.y * Width + start.x, best = currentIndex;
            int bestDistance = field.Distance[currentIndex];
            // Match the level's up/right/down/left preference so motion keeps the same character.
            Choose(start.x, start.y + 1, field, ref best, ref bestDistance);
            Choose(start.x + 1, start.y, field, ref best, ref bestDistance);
            Choose(start.x, start.y - 1, field, ref best, ref bestDistance);
            Choose(start.x - 1, start.y, field, ref best, ref bestDistance);
            return best == currentIndex ? delta.normalized : (LevelBuilder.Cell(best % Width, best / Width) - from).normalized;
        }
        void Visit(int cell, short distance, Field field, ref int tail)
        {
            if (!walkable[cell] || field.Distance[cell] >= 0) return;
            field.Distance[cell] = distance; queue[tail++] = cell;
        }
        void Choose(int x, int y, Field field, ref int best, ref int bestDistance)
        {
            if (!Open(x, y)) return;
            int cell = y * Width + x, distance = field.Distance[cell];
            if (distance >= 0 && (bestDistance < 0 || distance < bestDistance)) { best = cell; bestDistance = distance; }
        }
    }
}
