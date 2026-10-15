using System.Collections.Generic;
using UnityEngine;

namespace PlanB
{
    /// <summary>Two baked textures replace the tile-object grid. Walls retain row depth ordering.</summary>
    public sealed class StaticMapRenderer : MonoBehaviour
    {
        const int Tile = 48;
        const int PixelWidth = LevelBuilder.Width * Tile;
        const int PixelHeight = LevelBuilder.Height * Tile;
        [System.Serializable]
        sealed class BakedSet
        {
            public int Floor, Users;
            public Texture2D Ground, Walls, Preview;
            public Sprite GroundSprite, PreviewSprite;
            public Sprite[] Rows;
        }
        static readonly Dictionary<int, BakedSet> Cache = new Dictionary<int, BakedSet>();
        static readonly Dictionary<string, Color32[]> Sources = new Dictionary<string, Color32[]>();
        static bool recoveredOwners;
        [SerializeField] BakedSet art;
        [System.NonSerialized] bool ownershipRegistered;
        public Sprite MapPreview { get { return art != null ? art.PreviewSprite : null; } }

        public void Build(string[] map, int floor)
        {
            RecoverOwners();
            BakedSet cached;
            if (!Cache.TryGetValue(floor, out cached) || cached.Ground == null)
            {
                cached = Bake(map, floor);
                Cache[floor] = cached;
            }
            art = cached; art.Users++; ownershipRegistered = true;
            AddSprite("Baked facility floor", art.GroundSprite, new Vector2(-.5f, .5f), -1000);
            for (int r = 0; r < LevelBuilder.Height; r++)
                AddSprite("Wall depth row " + r, art.Rows[r], new Vector2(-.5f, 14 - r),
                    100 - Mathf.RoundToInt((14 - r - .5f) * 5));
        }

        void OnEnable()
        {
            RecoverOwners();
            if (art != null && art.Ground != null && !ownershipRegistered) RegisterOwnership();
        }

        static void RecoverOwners()
        {
            if (recoveredOwners) return;
            recoveredOwners = true;
            // Hot reload resets dictionaries but keeps serialized texture references. Include the
            // disabled floor so the next restart can reuse its bake before it is enabled again.
            foreach (var owner in Resources.FindObjectsOfTypeAll<StaticMapRenderer>())
            {
                if (owner == null || !owner.gameObject.scene.IsValid() || !owner.gameObject.scene.isLoaded ||
                    owner.art == null || owner.art.Ground == null || owner.ownershipRegistered) continue;
                owner.RegisterOwnership();
            }
        }

        void RegisterOwnership()
        {
            if (ownershipRegistered || art == null) return;
            BakedSet canonical;
            if (Cache.TryGetValue(art.Floor, out canonical) && canonical.Ground == art.Ground)
                art = canonical;
            else
            {
                // Plain serialized classes can be duplicated by reload; native texture identity
                // above joins their ownership without counting the old serialized Users value.
                art.Users = 0;
                if (canonical == null || canonical.Ground == null || gameObject.activeInHierarchy)
                    Cache[art.Floor] = art;
            }
            art.Users++; ownershipRegistered = true;
        }

        void AddSprite(string label, Sprite sprite, Vector2 position, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false); go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sharedMaterial = SpriteLibrary.Material;
            renderer.sortingOrder = order;
        }

        static Color32[] Source(string key)
        {
            Color32[] pixels;
            if (!Sources.TryGetValue(key, out pixels))
            {
                int width, height;
                pixels = SpriteFactory.CreatePixels(key, out width, out height);
                Sources[key] = pixels;
            }
            return pixels;
        }

        static BakedSet Bake(string[] map, int floor)
        {
            var ground = new Color32[PixelWidth * PixelHeight];
            var walls = new Color32[ground.Length];
            var preview = new Color32[LevelBuilder.Width * LevelBuilder.Height];
            Color32[] wood = Source("tile_wood"), metal = Source("tile_metal");
            Color32[] wall = Source("tile_wall"), cracked = Source("tile_cracked");
            for (int r = 0; r < LevelBuilder.Height; r++)
            {
                for (int c = 0; c < LevelBuilder.Width; c++)
                {
                    char tile = map[r][c];
                    bool kitchen = floor == 1 && c < 15 && r >= 13 && r <= 18;
                    bool bedroom = floor == 2 && c >= 41 && r >= 22 && r <= 26;
                    bool metalFloor = tile == 'm' || tile == 'h' || kitchen;
                    var source = metalFloor && !bedroom ? metal : wood;
                    if (tile != '#' && tile != 'c' && (c * 7 + r * 11) % 47 == 0 && metalFloor) source = cracked;
                    bool north = r > 0 && map[r - 1][c] == '#';
                    bool southOpen = r < LevelBuilder.Height - 1 && map[r + 1][c] != '#';
                    bool west = c > 0 && map[r][c - 1] == '#';
                    bool east = c < LevelBuilder.Width - 1 && map[r][c + 1] == '#';
                    float lightNW = LightAt(c - .5f, r - .5f, floor), lightNE = LightAt(c + .5f, r - .5f, floor);
                    float lightSW = LightAt(c - .5f, r + .5f, floor), lightSE = LightAt(c + .5f, r + .5f, floor);
                    int baseY = (LevelBuilder.Height - 1 - r) * Tile;
                    for (int py = 0; py < Tile; py++)
                    {
                        int target = (baseY + py) * PixelWidth + c * Tile;
                        float lightLeft = Mathf.Lerp(lightSW, lightNW, py / 47f), lightRight = Mathf.Lerp(lightSE, lightNE, py / 47f);
                        for (int px = 0; px < Tile; px++)
                        {
                            int sourceIndex = py * Tile + px;
                            Color32 pixel = tile == '#' ? Hex(0x111923) : source[sourceIndex];
                            if (tile == 'c') pixel = Carpet(c, r, px, py);
                            if (kitchen)
                            {
                                float checker = ((c + r) & 1) == 0 ? 1.18f : .88f;
                                pixel = Tint(pixel, checker, checker * .98f, checker * .86f);
                            }
                            else if (floor == 2 && !bedroom) pixel = Tint(pixel, .78f, .90f, 1.02f);
                            if (tile == 'h' && (c == 2 && px < 7 || c == 7 && px > 40))
                                pixel = ((px + py + r * Tile) / 9 & 1) == 0 ? Hex(0xc7953e) : Hex(0x202930);
                            if (tile != '#')
                            {
                                float light = Mathf.Lerp(lightLeft, lightRight, px / 47f);
                                float shade = 1;
                                if (north && py > 34) shade *= .60f + (47 - py) * .029f;
                                if (west && px < 6) shade *= .72f + px * .045f;
                                if (east && px > 41) shade *= .72f + (47 - px) * .045f;
                                pixel = Tint(pixel, shade, shade, shade);
                                if (floor == 1 || bedroom)
                                    pixel = Mix(pixel, Hex(0xe6ad63), light);
                                else pixel = Mix(pixel, Hex(0x5aa5b4), light * .75f);
                            }
                            ground[target + px] = pixel;
                            if (tile == '#')
                            {
                                Color32 brick = floor == 1 ? Tint(wall[sourceIndex], 1.10f, 1.02f, .91f) : Tint(wall[sourceIndex], .84f, 1, 1.12f);
                                if (southOpen && py < 5) brick = py < 2 ? Hex(0x090f17) : floor == 1 ? Hex(0x6d6350) : Hex(0x4e6068);
                                if (!north && py > 43) brick = py == 44 ? Hex(0x6a7271) : Hex(0x0c131d);
                                if (!west && px < 3 || !east && px > 44) brick = Hex(0x0b121c);
                                walls[target + px] = brick;
                            }
                        }
                    }
                    preview[(LevelBuilder.Height - r - 1) * LevelBuilder.Width + c] =
                        tile == '#' ? Hex(0x15202b) : tile == 'c' ? Hex(0x854349) :
                        floor == 1 && !metalFloor ? Hex(0x91724c) : kitchen ? Hex(0x777567) : Hex(0x506777);
                }
            }
            // Small authored signs are part of the floor texture, with no TextMesh objects.
            Color32 sign = floor == 1 ? Hex(0xc6ac78) : Hex(0x83b2bc);
            if (floor == 1)
            {
                Sign(ground, "RESIDENTIAL", 7, 2, sign); Sign(ground, "LOUNGE", 25, 8, sign);
                Sign(ground, "MAINTENANCE", 40, 2, sign); Sign(ground, "KITCHEN / STORAGE", 8, 14, sign);
                Sign(ground, "ELEVATOR", 24, 22, sign);
                Threshold(ground, 13, 7.5f, false); Threshold(ground, 32.5f, 8.5f, false);
            }
            else
            {
                Sign(ground, "RESEARCH", 7, 2, sign); Sign(ground, "SERVERS", 40, 1, sign);
                Sign(ground, "QUARANTINE", 40, 18, Hex(0xc87967)); Sign(ground, "SECURITY", 23, 15, sign);
                Sign(ground, "EXIT LIFT", 24, 23, sign); Threshold(ground, 30, 21.5f, true);
            }
            var result = new BakedSet { Floor = floor };
            result.Ground = Texture(ground, PixelWidth, PixelHeight, "PlanB floor " + floor);
            result.Walls = Texture(walls, PixelWidth, PixelHeight, "PlanB wall atlas " + floor);
            result.Preview = Texture(preview, LevelBuilder.Width, LevelBuilder.Height, "PlanB map preview " + floor);
            result.GroundSprite = MakeSprite(result.Ground, new Rect(0, 0, PixelWidth, PixelHeight), Tile);
            result.PreviewSprite = MakeSprite(result.Preview, new Rect(0, 0, LevelBuilder.Width, LevelBuilder.Height), 1);
            result.Rows = new Sprite[LevelBuilder.Height];
            for (int r = 0; r < result.Rows.Length; r++)
                result.Rows[r] = MakeSprite(result.Walls, new Rect(0, (LevelBuilder.Height - 1 - r) * Tile, PixelWidth, Tile), Tile);
            return result;
        }

        static float LightAt(float c, float r, int floor)
        {
            float a, b, d;
            if (floor == 1)
            {
                a = Glow(c, r, 9, 5, 6); b = Glow(c, r, 27, 11, 7); d = Glow(c, r, 40, 5, 6);
            }
            else
            {
                a = Glow(c, r, 7, 4, 7); b = Glow(c, r, 40, 4, 8); d = Glow(c, r, 44, 24, 5);
            }
            return Mathf.Max(a, Mathf.Max(b, d)) * .16f;
        }
        static float Glow(float c, float r, float x, float y, float radius)
        {
            float value = Mathf.Clamp01(1 - ((c - x) * (c - x) + (r - y) * (r - y)) / (radius * radius));
            return value * value;
        }
        static Color32 Carpet(int c, int r, int px, int py)
        {
            int x = (c - 19) * Tile + px, y = (15 - r) * Tile + py;
            int edge = Mathf.Min(Mathf.Min(x, 11 * Tile - 1 - x), Mathf.Min(y, 7 * Tile - 1 - y));
            int noise = ((x * 37 + y * 19) ^ (x * y)) & 7;
            Color32 color = new Color32((byte)(103 + noise), (byte)(43 + noise / 2), (byte)(49 + noise / 2), 255);
            if (edge < 4) color = Hex(0x3b2729);
            else if (edge < 7 || edge >= 15 && edge < 17) color = Hex(0xb18153);
            else if (edge < 13) color = Hex(0x813b3b);
            float dx = (x - 264) / 70f, dy = (y - 168) / 45f;
            float diamond = Mathf.Abs(dx) + Mathf.Abs(dy);
            if (diamond > .94f && diamond < 1.03f || diamond < .10f) color = Hex(0xa5724f);
            return color;
        }
        static void Threshold(Color32[] pixels, float c, float r, bool hazard)
        {
            int cx = Mathf.RoundToInt((c + .5f) * Tile), cy = Mathf.RoundToInt((27.5f - r) * Tile);
            for (int y = cy - 45; y < cy + 45; y++)
                for (int x = cx - 15; x <= cx + 15; x++)
                {
                    if (x < 0 || y < 0 || x >= PixelWidth || y >= PixelHeight) continue;
                    if (Mathf.Abs(x - cx) > 11 || Mathf.Abs(y - cy) > 41)
                        pixels[y * PixelWidth + x] = hazard && ((x + y) / 7 & 1) == 0 ? Hex(0xb38b43) : Hex(0x25323a);
                }
        }

        static readonly string[] Glyphs =
        {
            "010101111101101","110101110101110","011100100100011","110101101101110","111100110100111",
            "111100110100100","011100101101011","101101111101101","111010010010111","001001001101010",
            "101101110101101","100100100100111","101111111101101","101111111111101","010101101101010",
            "110101110100100","010101101111011","110101110101101","011100010001110","111010010010010",
            "101101101101111","101101101101010","101101111111101","101101010101101","101101010010010",
            "111001010100111"
        };
        static void Sign(Color32[] pixels, string text, int column, int row, Color32 color)
        {
            const int scale = 2;
            int width = text.Length * 8 - 2, x0 = column * Tile + Tile / 2 - width / 2;
            int y0 = (27 - row) * Tile + 19;
            Fill(pixels, x0 - 7, y0 - 5, width + 14, 20, Hex(0x1b252c));
            Fill(pixels, x0 - 5, y0 + 13, width + 10, 1, Mix(Hex(0x27343a), color, .45f));
            for (int i = 0; i < text.Length; i++)
            {
                int letter = text[i] - 'A';
                if (letter < 0 || letter >= Glyphs.Length) continue;
                string shape = Glyphs[letter];
                for (int y = 0; y < 5; y++) for (int x = 0; x < 3; x++)
                    if (shape[y * 3 + x] == '1') Fill(pixels, x0 + i * 8 + x * scale, y0 + (4 - y) * scale, scale, scale, color);
            }
        }
        static void Fill(Color32[] pixels, int x, int y, int w, int h, Color32 color)
        {
            for (int yy = Mathf.Max(0, y); yy < Mathf.Min(PixelHeight, y + h); yy++)
                for (int xx = Mathf.Max(0, x); xx < Mathf.Min(PixelWidth, x + w); xx++) pixels[yy * PixelWidth + xx] = color;
        }
        static Texture2D Texture(Color32[] pixels, int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(false, true); return texture;
        }
        static Sprite MakeSprite(Texture2D texture, Rect rect, float ppu)
        {
            return Sprite.Create(texture, rect, new Vector2(.5f, .5f), ppu, 0, SpriteMeshType.FullRect);
        }
        static Color32 Hex(uint hex) { return new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255); }
        static Color32 Tint(Color32 color, float r, float g, float b)
        {
            return new Color32((byte)Mathf.Min(255, color.r * r), (byte)Mathf.Min(255, color.g * g), (byte)Mathf.Min(255, color.b * b), color.a);
        }
        static Color32 Mix(Color32 a, Color32 b, float t)
        {
            return new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), 255);
        }
        void OnDestroy()
        {
            RecoverOwners();
            if (art != null && !ownershipRegistered) RegisterOwnership();
            if (art == null || --art.Users > 0) return;
            foreach (var row in art.Rows) if (row != null) Destroy(row);
            Destroy(art.GroundSprite); Destroy(art.PreviewSprite);
            Destroy(art.Ground); Destroy(art.Walls); Destroy(art.Preview);
            BakedSet current;
            if (Cache.TryGetValue(art.Floor, out current) && ReferenceEquals(current, art)) Cache.Remove(art.Floor);
            art = null;
        }
    }
}
