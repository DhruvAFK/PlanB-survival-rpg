using System;
using UnityEngine;

namespace PlanB
{
    /// <summary>Original pixel art drawn in code from the attached facility art direction.</summary>
    public static class SpriteFactory
    {
        public const float PixelsPerUnit = 48f;
        private static readonly Color32 Ink = C(0x090e17), Navy = C(0x171e2b), Deep = C(0x232b37);
        private static readonly Color32 Steel = C(0x46515c), SteelLight = C(0x74818a), Teal = C(0x3f7d89);
        private static readonly Color32 Wood = C(0x68452c), WoodLight = C(0x98704a), Gold = C(0xd9a24c);
        private static readonly Color32 Cream = C(0xd7c8a7), Skin = C(0xc99e7e), SkinDark = C(0x8c6557);
        private static readonly Color32 Red = C(0x9c383c), RedLight = C(0xdb6252), Green = C(0x48644d);

        public static Sprite Create(string key, int frame = 0, int direction = 0)
        {
            key = string.IsNullOrEmpty(key) ? "tile_metal" : key.ToLowerInvariant();
            frame = ((frame % 3) + 3) % 3;
            direction = ((direction % 4) + 4) % 4;
            bool actor = key == "fighter" || key == "engineer" || key == "scout" || key == "scientist";
            int width = actor ? 40 : 48;
            int height = key.StartsWith("elevator", StringComparison.Ordinal) ? 64 : 48;
            if (key == "gun") { width = 24; height = 12; }
            if (key == "bullet") { width = 12; height = 5; }
            if (key == "laser") { width = 24; height = 9; }
            if (key == "muzzle") { width = 24; height = 24; }
            Canvas p = new Canvas(width, height, StableHash(key));
            if (actor) Character(p, key, frame, direction);
            else if (key == "walker" || key == "runner") Mutant(p, frame, key == "runner");
            else if (key == "crawler" || key == "spitter" || key == "warden") Spider(p, frame, key);
            else if (key.StartsWith("tile_", StringComparison.Ordinal) || key == "carpet") Tile(p, key);
            else ObjectArt(p, key);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "PlanB Procedural " + key + " " + direction + " " + frame;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(p.Pixels);
            texture.Apply(false, true);
            Sprite result = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            result.name = texture.name;
            return result;
        }

        private static void Tile(Canvas p, string key)
        {
            if (key == "tile_wood")
            {
                p.Rect(0, 0, 48, 48, Wood);
                for (int row = 0; row < 6; row++)
                {
                    int y = row * 8;
                    p.Rect(0, y + 1, 48, 6, Shade(Wood, 0.94f + row % 3 * 0.08f));
                    p.Line(0, y, 47, y, C(0x392c24));
                    p.Line(0, y + 7, 47, y + 7, C(0x84603e));
                    int join = (row % 2 == 0 ? 13 : 34);
                    p.Line(join, y, join, y + 7, C(0x463326));
                    p.Pixel(join + 2, y + 2, C(0x30271f));
                    p.Pixel(join - 2, y + 5, C(0x30271f));
                }
                for (int i = 0; i < 50; i++)
                {
                    int x = p.Rand(1, 46), y = p.Rand(1, 46);
                    p.Rect(x, y, p.Rand(1, 6), 1, i % 3 == 0 ? C(0x533d2b) : C(0x775134));
                }
                p.Line(4, 10, 8, 10, C(0xa27b4d));
            }
            else if (key == "carpet")
            {
                p.Rect(0, 0, 48, 48, C(0x6e3035));
                p.Frame(1, 1, 46, 46, C(0xb0784f), 2);
                p.Frame(5, 5, 38, 38, C(0x913d3e), 1);
                p.Frame(7, 7, 34, 34, C(0x9e6645), 1);
                for (int x = 11; x < 39; x += 6)
                {
                    p.Pixel(x, 10, Gold); p.Pixel(x, 37, Gold);
                    p.Pixel(10, x, Gold); p.Pixel(37, x, Gold);
                }
                p.Oval(19, 19, 10, 10, C(0x8c4540));
                p.Frame(22, 22, 4, 4, C(0xb38156));
                p.Noise(120, C(0x773338), C(0x81393b));
            }
            else if (key == "tile_wall")
            {
                p.Rect(0, 0, 48, 48, Navy);
                for (int y = 0; y < 48; y += 12)
                {
                    int offset = y % 24 == 0 ? -12 : 0;
                    for (int x = offset; x < 48; x += 24)
                    {
                        p.Rect(x + 1, y + 1, 22, 10, Shade(Deep, 0.95f + p.Rand(0, 15) * 0.01f));
                        p.Line(x + 2, y + 10, x + 20, y + 10, C(0x35404a));
                        p.Line(x + 22, y + 1, x + 22, y + 9, C(0x101722));
                    }
                }
                p.Rect(0, 43, 48, 5, Ink);
                p.Rect(0, 43, 48, 2, Steel);
                p.Rect(0, 0, 48, 3, C(0x10151f));
                p.Noise(36, C(0x303946), C(0x1e2632));
                p.Rect(3, 39, 8, 1, C(0x55534a));
            }
            else
            {
                p.Rect(0, 0, 48, 48, C(0x343d47));
                for (int y = 0; y < 48; y += 16)
                    for (int x = 0; x < 48; x += 16)
                    {
                        p.Rect(x + 1, y + 1, 14, 14, Shade(C(0x3f4750), 0.91f + p.Rand(0, 16) * 0.01f));
                        p.Line(x + 1, y + 15, x + 15, y + 15, C(0x56616a));
                        p.Pixel(x + 2, y + 2, C(0x69717a));
                    }
                p.Noise(70, C(0x363e47), C(0x4a535a));
                if (key == "tile_cracked")
                {
                    p.Line(0, 39, 17, 26, C(0x1c232d)); p.Line(17, 26, 22, 13, Ink);
                    p.Line(22, 13, 40, 2, C(0x242a33)); p.Line(17, 26, 37, 29, C(0x202831));
                    p.Line(22, 14, 21, 25, C(0x5c5c59));
                    p.Rect(34, 36, 5, 2, C(0x686655)); p.Rect(7, 7, 2, 3, C(0x766d59));
                }
                else
                {
                    p.Pixel(14, 14, C(0x1a222c)); p.Pixel(46, 14, C(0x1a222c));
                    p.Pixel(14, 46, C(0x1a222c)); p.Pixel(46, 46, C(0x1a222c));
                }
            }
        }

        private static void Character(Canvas p, string role, int frame, int direction)
        {
            bool engineer = role == "engineer", scout = role == "scout", scientist = role == "scientist";
            bool back = direction == 1, side = direction >= 2;
            int step = frame == 0 ? -2 : frame == 2 ? 2 : 0;
            int bob = frame == 1 ? 0 : 1;
            Color32 coat = engineer ? C(0xa17a37) : scout ? C(0x3c5648) : scientist ? C(0xb1c5c9) : Red;
            Color32 coatLight = engineer ? C(0xd3a150) : scout ? C(0x627a58) : scientist ? C(0xe0dfcd) : C(0xc95c4b);
            Color32 coatDark = engineer ? C(0x72522b) : scout ? C(0x293e37) : scientist ? C(0x6c929d) : C(0x642a31);
            p.Oval(7, 1, 27, 7, C(0x050b13, 105));
            if (!side)
            {
                p.Rect(12, 4 + step, 7, 14, Ink); p.Rect(22, 4 - step, 7, 14, Ink);
                p.Rect(13, 7 + step, 5, 11, C(0x344049)); p.Rect(23, 7 - step, 5, 11, C(0x2b333d));
                p.Rect(11, 3 + step, 9, 5, C(0x141a23)); p.Rect(21, 3 - step, 9, 5, C(0x141a23));
                p.Line(12, 4 + step, 18, 4 + step, C(0x596066));
                p.Line(22, 4 - step, 28, 4 - step, C(0x596066));
                p.Rect(9, 17 + bob, 22, 16, Ink); p.Rect(11, 18 + bob, 18, 15, coat);
                p.Rect(11, 18 + bob, 4, 13, coatDark); p.Rect(25, 20 + bob, 4, 10, coatDark);
                p.Rect(14, 28 + bob, 11, 3, coatLight);
                p.Rect(7, 18 + bob - step / 2, 5, 12, Ink); p.Rect(29, 18 + bob + step / 2, 5, 12, Ink);
                p.Rect(8, 19 + bob - step / 2, 4, 9, coat); p.Rect(29, 19 + bob + step / 2, 4, 9, coatDark);
                p.Rect(8, 17 + bob - step / 2, 4, 4, SkinDark); p.Rect(29, 17 + bob + step / 2, 4, 4, Skin);
                if (back)
                {
                    p.Rect(15, 21 + bob, 10, 9, coatDark); p.Frame(15, 21 + bob, 10, 9, C(0x837253));
                    p.Rect(17, 23 + bob, 6, 4, engineer ? Gold : scout ? C(0x52664c) : scientist ? Teal : C(0x8e4140));
                }
                else
                {
                    p.Line(20, 19 + bob, 20, 29 + bob, C(0x2b2930));
                    p.Line(21, 19 + bob, 21, 29 + bob, coatLight);
                    p.Rect(13, 23 + bob, 4, 3, coatDark); p.Pixel(14, 25 + bob, Gold);
                    p.Rect(24, 23 + bob, 3, 3, coatDark);
                    p.Rect(11, 17 + bob, 18, 3, C(0x2e282a)); p.Pixel(20, 18 + bob, Gold);
                    if (engineer || scout) { p.Rect(14, 20 + bob, 3, 8, C(0x4c4538)); p.Rect(24, 20 + bob, 3, 8, C(0x4c4538)); }
                    if (scientist) { p.Line(14, 29 + bob, 18, 24 + bob, C(0x6b9098)); p.Line(26, 29 + bob, 22, 24 + bob, C(0x6b9098)); }
                }
                p.Rect(17, 30 + bob, 7, 4, SkinDark);
                p.Oval(10, 30 + bob, 21, 17, Ink); p.Oval(12, 31 + bob, 17, 14, back ? C(0x44312d) : Skin);
                if (back)
                {
                    p.Oval(11, 33 + bob, 19, 13, scout ? coatDark : C(0x342728));
                    p.Rect(15, 31 + bob, 11, 3, scout ? coat : C(0x50362d));
                    p.Line(15, 42 + bob, 24, 43 + bob, scout ? coat : C(0x634538));
                }
                else
                {
                    p.Rect(12, 33 + bob, 3, 8, SkinDark); p.Rect(26, 33 + bob, 3, 7, C(0xa77b65));
                    p.Rect(16, 37 + bob, 3, 2, Ink); p.Rect(23, 37 + bob, 3, 2, Ink);
                    p.Pixel(17, 38 + bob, Cream); p.Pixel(24, 38 + bob, Cream);
                    p.Pixel(21, 35 + bob, C(0xe1b995)); p.Line(19, 33 + bob, 23, 33 + bob, C(0x735047));
                    p.Oval(10, 39 + bob, 20, 8, C(0x322328));
                    p.Rect(12, 38 + bob, 4, 7, C(0x39272a)); p.Rect(25, 39 + bob, 4, 5, C(0x39272a));
                    p.Rect(14, 42 + bob, 5, 3, C(0x5f3b30)); p.Rect(20, 43 + bob, 6, 2, C(0x644335));
                    p.Rect(19, 39 + bob, 4, 4, C(0x322328));
                }
                if (scout)
                {
                    p.OvalFrame(9, 29 + bob, 23, 19, coatDark, 3);
                    p.Line(13, 44 + bob, 25, 45 + bob, coatLight);
                    p.Rect(15, 31 + bob, 12, 5, C(0x233c36));
                    p.Line(15, 34 + bob, 25, 34 + bob, Green);
                }
                if (engineer)
                {
                    p.Oval(9, 39 + bob, 23, 9, Ink); p.Oval(11, 40 + bob, 19, 7, C(0xcb8b2f));
                    p.Rect(9, 39 + bob, 23, 3, C(0xe5aa44));
                    p.Rect(17, 43 + bob, 4, 4, C(0xf0bd60)); p.Line(12, 41 + bob, 28, 41 + bob, C(0x835529));
                    if (!back) { p.Frame(14, 35 + bob, 6, 5, C(0x352d27)); p.Frame(22, 35 + bob, 6, 5, C(0x352d27)); p.Line(19, 37 + bob, 22, 37 + bob, C(0x352d27)); }
                }
                if (scientist && !back) { p.Frame(14, 35 + bob, 6, 5, Ink); p.Frame(22, 35 + bob, 6, 5, Ink); p.Line(19, 37 + bob, 22, 37 + bob, Ink); p.Pixel(15, 38 + bob, Cream); }
            }
            else
            {
                p.Rect(15, 4 + step, 7, 15, Ink); p.Rect(22, 4 - step, 6, 15, Ink);
                p.Rect(16, 7 + step, 5, 10, C(0x36404a)); p.Rect(23, 7 - step, 4, 10, C(0x2c333e));
                p.Rect(14, 3 + step, 10, 5, C(0x151b25)); p.Rect(21, 3 - step, 10, 5, C(0x151b25));
                p.Line(15, 4 + step, 22, 4 + step, Steel); p.Line(22, 4 - step, 29, 4 - step, Steel);
                p.Rect(12, 17 + bob, 17, 16, Ink); p.Rect(14, 19 + bob, 13, 13, coatDark);
                p.Rect(16, 24 + bob, 9, 7, coat); p.Rect(14, 19 + bob, 4, 11, coatLight);
                p.Rect(14, 17 + bob, 15, 3, C(0x2a2729)); p.Rect(15, 20 + bob, 5, 10, Ink);
                p.Rect(16, 21 + bob + step / 2, 4, 8, coat); p.Rect(16, 18 + bob + step / 2, 4, 4, Skin);
                p.Rect(21, 30 + bob, 6, 4, SkinDark);
                p.Oval(11, 31 + bob, 19, 17, Ink); p.Oval(14, 32 + bob, 15, 13, Skin);
                p.Rect(27, 35 + bob, 5, 5, Skin); p.Rect(28, 38 + bob, 3, 2, SkinDark);
                p.Rect(25, 39 + bob, 3, 2, Ink); p.Pixel(26, 40 + bob, Cream);
                p.Rect(14, 35 + bob, 5, 7, SkinDark);
                p.Oval(10, 39 + bob, 19, 9, C(0x34252a)); p.Rect(11, 34 + bob, 7, 11, C(0x34252a));
                p.Rect(14, 42 + bob, 8, 3, C(0x624032));
                if (scout)
                {
                    p.Oval(9, 32 + bob, 19, 16, coatDark); p.Oval(15, 34 + bob, 14, 11, SkinDark);
                    p.Rect(23, 39 + bob, 5, 2, Ink); p.Pixel(25, 40 + bob, Cream);
                    p.Rect(18, 32 + bob, 12, 5, Green); p.Line(12, 44 + bob, 24, 45 + bob, coatLight);
                    p.Rect(11, 34 + bob, 6, 10, coatDark);
                }
                if (engineer)
                {
                    p.Oval(10, 40 + bob, 22, 8, C(0xcd8c30)); p.Rect(9, 39 + bob, 25, 3, Gold);
                    p.Rect(17, 44 + bob, 4, 3, C(0xf0bc5e)); p.Frame(23, 37 + bob, 7, 5, C(0x342d29));
                }
                if (scientist) { p.Frame(23, 37 + bob, 7, 5, Ink); p.Rect(25, 20 + bob, 2, 10, Cream); }
                // The profile is authored facing right and mirrored for left.
                if (direction == 2) p.FlipX();
            }
        }

        private static void Mutant(Canvas p, int frame, bool runner)
        {
            int step = frame == 0 ? -3 : frame == 2 ? 3 : 0;
            int bob = frame == 1 ? 0 : 1;
            Color32 flesh = runner ? C(0x997b73) : C(0x857d77);
            Color32 muscle = runner ? C(0x663b43) : C(0x52404c);
            p.Oval(6, 1, 36, 7, C(0x050913, 120));
            p.Line(17, 19, 12 + step, 8, Ink, 7); p.Line(12 + step, 8, 8 + step, 4, Ink, 5);
            p.Line(29, 18, 33 - step, 8, Ink, 7); p.Line(33 - step, 8, 38 - step, 4, Ink, 5);
            p.Line(17, 18, 12 + step, 8, muscle, 4); p.Line(29, 18, 33 - step, 8, muscle, 4);
            p.Line(11 + step, 5, 6 + step, 3, C(0x766669), 2); p.Line(35 - step, 5, 41 - step, 3, C(0x766669), 2);
            p.Oval(10, 13 + bob, 24, 23, Ink); p.Oval(12, 15 + bob, 20, 19, muscle);
            p.Oval(10, 25 + bob, 20, 13, C(0x483344));
            p.Line(17, 30 + bob, 12, 22 + bob, C(0x786270), 3);
            p.Line(21, 30 + bob, 16, 23 + bob, C(0x665361), 2);
            p.Rect(22, 17 + bob, 7, 3, C(0x7c4c51));
            p.Line(12, 28 + bob, 6, 17 + step / 2, Ink, 6); p.Line(6, 17 + step / 2, 5, 10, Ink, 5);
            p.Line(12, 28 + bob, 6, 17 + step / 2, muscle, 3); p.Line(6, 17 + step / 2, 5, 10, flesh, 2);
            p.Line(31, 27 + bob, 36, 16 - step / 2, Ink, 6); p.Line(36, 16 - step / 2, 42, 12, Ink, 4);
            p.Line(31, 27 + bob, 36, 16 - step / 2, muscle, 3); p.Line(36, 16 - step / 2, 42, 12, flesh, 2);
            for (int n = 0; n < 3; n++) { p.Line(4 + n * 2, 11, 3 + n * 3, 7, C(0xb2a08a)); p.Line(40, 13 + n, 45, 11 + n, C(0xb2a08a)); }
            p.Oval(24, 29 + bob, 17, 17, Ink); p.Oval(26, 30 + bob, 14, 15, flesh);
            p.Oval(26, 37 + bob, 13, 8, C(0xb5b0a1)); p.Oval(27, 35 + bob, 5, 5, C(0x33262c));
            p.Oval(35, 35 + bob, 5, 5, C(0x33262c)); p.Pixel(29, 37 + bob, RedLight); p.Pixel(37, 37 + bob, RedLight);
            p.Rect(32, 33 + bob, 3, 3, C(0x514348)); p.Rect(29, 30 + bob, 9, 3, C(0x2b1c28));
            for (int x = 30; x < 38; x += 3) p.Rect(x, 31 + bob, 1, 2, Cream);
            p.Rect(26, 40 + bob, 2, 4, C(0x7e7b72)); p.Pixel(35, 43 + bob, Cream);
            for (int i = 0; i < 4; i++) { p.Line(12 + i * 4, 33 + bob, 10 + i * 4, 38 + bob, Ink, 2); p.Pixel(10 + i * 4, 38 + bob, C(0x8a7b83)); }
            p.Rect(17, 14 + bob, 9, 4, C(0x22202c)); p.Rect(20, 15 + bob, 3, 2, C(0x765b58));
        }

        private static void Spider(Canvas p, int frame, string kind)
        {
            int twitch = frame == 0 ? -2 : frame == 2 ? 2 : 0;
            bool boss = kind == "warden", spitter = kind == "spitter";
            Color32 shell = boss ? C(0x59434d) : spitter ? C(0x394a53) : C(0x41414c);
            Color32 shine = boss ? C(0x8f6865) : spitter ? C(0x607d80) : C(0x77717b);
            p.Oval(3, 1, 42, 10, C(0x050912, 125));
            for (int side = -1; side <= 1; side += 2)
                for (int leg = 0; leg < 4; leg++)
                {
                    int rootX = 24 + side * 8, rootY = 14 + leg * 5;
                    int kneeX = 24 + side * (17 + leg % 2 * 3), kneeY = rootY + 5 + (leg % 2 == 0 ? twitch : -twitch);
                    int tipX = 24 + side * (21 - leg % 2 * 2), tipY = rootY - 8 + (leg % 2 == 0 ? -twitch : twitch);
                    p.Line(rootX, rootY, kneeX, kneeY, Ink, 6); p.Line(kneeX, kneeY, tipX, tipY, Ink, 5);
                    p.Line(rootX, rootY + 1, kneeX, kneeY, shell, 3); p.Line(kneeX, kneeY, tipX, tipY, C(0x685365), 2);
                    p.Pixel(kneeX - side, kneeY + 1, shine); p.Line(tipX, tipY, tipX + side * 2, tipY - 2, C(0xad8c86));
                }
            p.Oval(11, 23, 27, 23, Ink); p.Oval(13, 25, 23, 19, shell);
            p.Oval(15, 30, 17, 13, Shade(shell, 1.28f)); p.Line(17, 39, 23, 42, shine, 2);
            p.Line(25, 26, 29, 37, Shade(shell, 0.7f), 2); p.Pixel(19, 40, C(0xa1a0a0));
            p.Oval(13, 10, 23, 23, Ink); p.Oval(15, 12, 19, 19, shell);
            p.Oval(18, 16, 13, 15, C(0x1d222d)); p.Oval(19, 18, 11, 12, C(0x6d202d));
            p.Oval(21, 19, 7, 9, boss ? C(0xff6a4f) : C(0xe44446));
            p.Oval(22, 22, 4, 5, C(0xffb280)); p.Rect(23, 24, 2, 2, C(0xffe9bd));
            p.Line(17, 14, 15, 8, Ink, 4); p.Line(31, 14, 33, 8, Ink, 4);
            p.Line(17, 14, 16, 9, Cream, 2); p.Line(31, 14, 32, 9, Cream, 2);
            if (spitter) { p.Oval(19, 32, 5, 6, C(0x497c68)); p.Pixel(20, 35, C(0x8caf76)); }
            if (boss)
            {
                p.Line(17, 42, 15, 47, Ink, 3); p.Line(31, 42, 33, 47, Ink, 3);
                p.Pixel(15, 47, Cream); p.Pixel(33, 47, Cream);
                p.Line(18, 35, 21, 31, C(0xa56b5b), 2); p.Line(30, 35, 27, 31, C(0xa56b5b), 2);
            }
        }

        private static void ObjectArt(Canvas p, string key)
        {
            switch (key)
            {
                case "gun":
                    p.Rect(2, 5, 21, 5, Ink); p.Rect(4, 6, 17, 3, Steel);
                    p.Rect(6, 7, 14, 2, SteelLight); p.Rect(21, 6, 3, 2, Ink);
                    p.Rect(5, 1, 5, 6, Ink); p.Rect(6, 2, 3, 4, C(0x806348)); p.Pixel(9, 9, Cream);
                    p.Frame(10, 3, 5, 3, Steel); break;
                case "bullet":
                    p.Rect(0, 1, 8, 2, C(0xe69543, 90)); p.Rect(5, 1, 6, 3, Gold); p.Rect(7, 2, 5, 2, C(0xffe8a5)); break;
                case "laser":
                    p.Line(0, 4, 23, 4, C(0xa92e49, 60), 9); p.Line(3, 4, 23, 4, C(0xff3949, 140), 5);
                    p.Line(6, 4, 23, 4, C(0xff735e), 3); p.Line(12, 4, 23, 4, C(0xffdec1)); break;
                case "muzzle":
                    p.Line(1, 12, 22, 12, C(0xe5a643, 80), 7); p.Line(6, 3, 16, 20, C(0xffb953, 130), 3);
                    p.Line(7, 21, 17, 5, Gold, 3); p.Oval(8, 8, 9, 9, C(0xffdc79)); p.Oval(11, 10, 6, 5, C(0xffffda)); break;
                case "ammo":
                    Shadow(p, 9, 8, 31, 8); Box(p, 10, 12, 29, 24, C(0x6c6948), C(0xa19a66));
                    p.Rect(12, 14, 25, 6, C(0x3b3f34)); p.Rect(16, 23, 16, 8, C(0xc4b17b));
                    for (int x = 19; x < 32; x += 5) { p.Rect(x, 24, 3, 5, C(0x594b35)); p.Rect(x, 28, 2, 2, Gold); }
                    p.Line(13, 34, 36, 34, Cream); p.Rect(25, 11, 10, 3, Wood); break;
                case "medkit":
                    Shadow(p, 8, 8, 33, 9); p.Frame(16, 34, 17, 7, Ink, 2); p.Line(17, 39, 31, 39, RedLight, 2);
                    Box(p, 9, 11, 31, 26, C(0xb4b4a2), Cream); p.Rect(11, 12, 27, 4, C(0x7d8c8b));
                    p.Rect(21, 17, 7, 15, Red); p.Rect(17, 21, 15, 7, Red); p.Rect(22, 19, 2, 12, RedLight);
                    p.Rect(12, 29, 3, 4, Steel); p.Rect(35, 29, 3, 4, Steel); break;
                case "fuse":
                    Shadow(p, 8, 9, 32, 8); p.Line(12, 13, 35, 35, Ink, 12); p.Line(14, 15, 33, 33, C(0x879b91), 8);
                    p.Line(17, 19, 29, 30, C(0xb0d0ba), 6); p.Line(19, 22, 27, 29, C(0x4e7f79), 3);
                    p.Line(10, 13, 16, 19, Gold, 7); p.Line(30, 33, 36, 39, Gold, 7);
                    p.Line(11, 16, 14, 19, Cream); p.Line(31, 36, 35, 39, Cream); break;
                case "key":
                    Shadow(p, 12, 5, 28, 8); p.Oval(12, 25, 21, 21, Ink); p.Oval(14, 27, 17, 17, Gold);
                    p.Oval(18, 31, 9, 9, C(0x503b2b)); p.Oval(20, 33, 5, 5, C(0x0a111b));
                    p.Line(23, 27, 23, 9, Ink, 7); p.Line(23, 27, 23, 10, Gold, 4);
                    p.Rect(24, 10, 9, 4, Gold); p.Rect(24, 17, 7, 4, Gold); p.Line(15, 39, 19, 42, Cream); break;
                case "cabinet": case "cabinet_open": Cabinet(p, key == "cabinet_open"); break;
                case "generator": case "generator_on": Generator(p, key == "generator_on"); break;
                case "elevator": case "elevator_on": Elevator(p, key == "elevator_on"); break;
                case "door": case "door_open": case "lockeddoor": case "lockeddoor_open": Door(p, key); break;
                case "sofa":
                    Shadow(p, 1, 1, 46, 10); Box(p, 3, 9, 42, 28, C(0x56332e), C(0x9c5c3f));
                    p.Rect(6, 26, 36, 10, C(0x7e4533)); p.Line(7, 34, 40, 34, C(0xb4774c));
                    for (int x = 7; x < 39; x += 12) { Box(p, x, 11, 11, 17, C(0x8c4c37), C(0xb0774c)); p.Line(x + 2, 13, x + 8, 13, C(0x5e352c)); }
                    Box(p, 1, 10, 6, 21, C(0x79422f), C(0xb27749)); Box(p, 41, 10, 6, 21, C(0x79422f), C(0xb27749));
                    p.Rect(5, 6, 4, 4, Ink); p.Rect(39, 6, 4, 4, Ink); break;
                case "bed":
                    Shadow(p, 4, 0, 41, 9); Box(p, 5, 3, 38, 43, Wood, WoodLight);
                    Box(p, 8, 6, 32, 35, C(0x788d94), C(0xa8b4ad)); p.Rect(10, 9, 28, 21, C(0x365e68));
                    p.Line(12, 29, 36, 29, C(0x6f9297)); p.Rect(12, 12, 24, 16, C(0x426f77));
                    Box(p, 11, 32, 25, 9, C(0xb4af98), Cream); p.Line(14, 38, 32, 38, C(0xe6d7b1));
                    p.Line(36, 13, 36, 28, C(0x2c4b58)); p.Rect(7, 2, 5, 4, Ink); p.Rect(36, 2, 5, 4, Ink); break;
                case "plant":
                    Shadow(p, 8, 1, 32, 8); p.Oval(14, 2, 22, 14, Ink); p.Oval(16, 4, 18, 10, C(0x725236));
                    p.Rect(18, 6, 14, 9, Wood); p.Oval(14, 12, 22, 7, C(0x987044)); p.Oval(17, 14, 16, 4, C(0x29251f));
                    p.Line(24, 16, 24, 38, C(0x36533e), 3);
                    Leaf(p, 12, 19, 16, 10); Leaf(p, 23, 24, 15, 10); Leaf(p, 14, 30, 13, 11); Leaf(p, 21, 36, 10, 10);
                    p.Line(17, 22, 24, 18, C(0x6e8d53)); p.Line(25, 29, 33, 30, C(0x6e8d53)); break;
                case "computer":
                    Shadow(p, 4, 1, 40, 8); Box(p, 3, 3, 42, 13, Steel, SteelLight); p.Rect(7, 7, 34, 7, C(0x202c38));
                    for (int x = 9; x < 38; x += 4) for (int y = 8; y < 13; y += 3) p.Rect(x, y, 2, 1, C(0x829a9e));
                    p.Rect(21, 15, 7, 6, Steel); Box(p, 8, 20, 33, 25, Steel, SteelLight);
                    p.Rect(11, 24, 27, 18, C(0x102b40)); p.Rect(13, 26, 23, 14, C(0x225169));
                    p.Line(15, 37, 25, 37, C(0x73b5b1)); p.Rect(15, 30, 4, 4, C(0x4e8d99));
                    p.Line(21, 33, 33, 33, Teal); p.Line(21, 30, 29, 30, Teal); p.Pixel(36, 22, C(0x80ba8e)); break;
                case "server":
                    Shadow(p, 8, 0, 32, 8); Box(p, 8, 3, 33, 44, C(0x28343e), Steel);
                    p.Rect(11, 5, 27, 38, C(0x111e2a));
                    for (int y = 7; y < 42; y += 7)
                    {
                        Box(p, 13, y, 23, 6, C(0x253541), C(0x43525b));
                        p.Rect(15, y + 2, 9, 1, C(0x101b26)); p.Rect(26, y + 2, 2, 2, Teal);
                        p.Rect(31, y + 2, 2, 2, y % 3 == 0 ? Gold : C(0x81b7bb));
                    }
                    p.Line(10, 44, 38, 44, C(0x78858a)); break;
                case "table":
                    Shadow(p, 2, 0, 44, 9); p.Rect(5, 3, 5, 20, Ink); p.Rect(38, 3, 5, 20, Ink);
                    Box(p, 2, 11, 44, 25, C(0x654b32), C(0x9b784d)); p.Rect(5, 14, 38, 19, C(0x7f5e3c));
                    p.Line(7, 30, 40, 30, C(0xa68252)); p.Line(8, 20, 35, 20, C(0x67482f));
                    p.Rect(27, 21, 10, 8, C(0xddd1b2)); p.Line(28, 27, 35, 27, Steel);
                    p.Oval(10, 20, 10, 7, Ink); p.Oval(12, 22, 6, 4, C(0x716657)); break;
                case "lamp":
                    Shadow(p, 9, 0, 30, 7); p.Oval(15, 1, 19, 7, Ink); p.Oval(17, 3, 15, 4, Gold);
                    p.Rect(23, 5, 4, 26, Ink); p.Rect(24, 7, 2, 24, C(0x9f804b));
                    p.Oval(12, 26, 26, 18, C(0xe5b25b, 70)); p.Rect(15, 27, 20, 15, Ink);
                    p.Rect(17, 29, 16, 12, Gold); p.Rect(20, 30, 10, 10, C(0xffd27a));
                    p.Rect(22, 31, 6, 8, C(0xffedba)); p.Line(16, 27, 34, 27, WoodLight); break;
                case "crate":
                    Shadow(p, 3, 0, 42, 7); Box(p, 3, 4, 42, 41, C(0x4a3729), C(0x8b6945));
                    p.Frame(7, 8, 34, 32, C(0x99794c), 3); p.Line(10, 12, 37, 36, Ink, 5);
                    p.Line(10, 36, 37, 12, Ink, 5); p.Line(10, 13, 37, 37, C(0x806440), 3);
                    p.Line(10, 37, 37, 13, C(0x806440), 3); p.Line(11, 15, 35, 37, C(0xb68d56));
                    foreach (int x in new[] { 8, 38 }) foreach (int y in new[] { 9, 38 }) p.Rect(x, y, 2, 2, Steel); break;
                case "security_camera":
                    p.Rect(27, 32, 5, 13, Ink); p.Rect(28, 33, 3, 10, Steel); p.Line(19, 31, 29, 35, Steel, 4);
                    p.Oval(6, 17, 34, 20, Ink); p.Oval(8, 20, 30, 16, C(0x929b98));
                    p.Rect(9, 23, 28, 9, C(0xb1b5a9)); p.Line(12, 32, 33, 32, Cream);
                    p.Oval(16, 15, 17, 13, Ink); p.Oval(20, 18, 9, 8, C(0x57232e));
                    p.Oval(22, 20, 5, 5, C(0xf75857)); p.Pixel(24, 23, C(0xffd7b3)); break;
                case "trap":
                    Shadow(p, 2, 6, 44, 11); p.OvalFrame(3, 8, 43, 29, Ink, 6); p.OvalFrame(5, 10, 39, 25, Steel, 4);
                    p.Line(9, 19, 37, 19, SteelLight, 2); p.Rect(19, 16, 10, 9, Ink); p.Rect(21, 18, 6, 5, WoodLight);
                    for (int x = 9; x < 40; x += 6) { p.Line(x, 12, x + 2, 18, Cream, 2); p.Line(x, 32, x + 2, 26, Cream, 2); } break;
                case "scrap":
                    Shadow(p, 4, 3, 39, 10);
                    for (int i = 0; i < 8; i++) { int x = p.Rand(5, 34), y = p.Rand(8, 31); Box(p, x, y, 10, 8, Steel, SteelLight); p.Line(x + 2, y + 6, x + 7, y + 2, C(0xb2ada0)); } break;
                case "battery":
                    Shadow(p, 12, 2, 26, 8); Box(p, 15, 7, 21, 33, Steel, SteelLight);
                    p.Rect(19, 40, 12, 4, Ink); p.Rect(21, 40, 8, 3, SteelLight); p.Rect(17, 12, 17, 21, Gold);
                    p.Line(26, 30, 21, 22, Ink, 3); p.Line(21, 22, 28, 23, Ink, 3); p.Line(28, 23, 23, 15, Ink, 3);
                    p.Line(18, 35, 32, 35, Cream); break;
                case "wire":
                    Shadow(p, 6, 3, 36, 8); p.OvalFrame(7, 8, 35, 28, Ink, 5);
                    p.OvalFrame(9, 10, 31, 24, C(0x9f5041), 3); p.OvalFrame(12, 13, 25, 18, C(0x512d31), 3);
                    p.OvalFrame(14, 15, 21, 14, C(0x8f493b), 2); p.Line(36, 15, 42, 7, RedLight, 2); p.Rect(40, 5, 4, 4, Gold); break;
                case "shadow": p.Oval(3, 8, 42, 22, C(0x000713, 105)); break;
                case "spark":
                    p.Line(24, 4, 24, 43, C(0xff9d43, 100), 3); p.Line(4, 24, 43, 24, C(0xff9d43, 100), 3);
                    p.Line(12, 12, 36, 36, Gold, 2); p.Line(12, 36, 36, 12, Gold, 2); p.Oval(20, 20, 9, 9, C(0xffefb1)); break;
                default: Box(p, 8, 8, 32, 32, Steel, SteelLight); break;
            }
        }

        private static void Cabinet(Canvas p, bool opened)
        {
            Shadow(p, 5, 0, 39, 8); Box(p, 6, 3, 36, 43, C(0x503e2c), C(0x92704b));
            p.Rect(10, 7, 28, 35, C(0x2c2827));
            for (int y = 9; y < 40; y += 10)
            {
                p.Rect(11, y, 26, 2, WoodLight);
                if (opened) { p.Rect(13, y + 3, 6, 5, C(0x707e75)); p.Rect(24, y + 3, 9, 4, C(0x856848)); p.Pixel(16, y + 5, Gold); }
                else { Box(p, 11, y + 2, 26, 8, Wood, C(0x8d6b46)); p.Line(22, y + 5, 28, y + 5, Gold); }
            }
            p.Line(8, 44, 39, 44, C(0xb19360));
            if (opened) { p.Rect(39, 6, 7, 35, Ink); p.Rect(40, 8, 5, 31, C(0x7c5b3b)); p.Rect(41, 21, 2, 4, Gold); }
        }

        private static void Generator(Canvas p, bool on)
        {
            Shadow(p, 0, 0, 48, 9);
            p.Line(3, 10, 3, 41, C(0x73343b), 4); p.Line(44, 7, 44, 38, C(0x8c4c36), 4);
            p.Line(3, 40, 42, 40, Steel, 4); p.Line(5, 6, 42, 6, Steel, 4);
            Box(p, 7, 7, 35, 35, Steel, SteelLight); p.Frame(9, 9, 31, 31, Ink, 2);
            p.Rect(12, 12, 25, 25, C(0x2e343a));
            for (int x = 10; x < 39; x += 8) { p.Rect(x, 38, 5, 3, Gold); p.Rect(x, 9, 5, 3, Gold); }
            p.Frame(15, 15, 20, 21, C(0x8d703d), 2); p.Rect(18, 18, 14, 15, C(0x1a232d));
            p.Line(27, 32, 21, 24, on ? C(0xffd981) : Gold, 3);
            p.Line(21, 24, 28, 25, on ? C(0xffd981) : Gold, 3); p.Line(28, 25, 23, 18, Gold, 3);
            p.Rect(11, 17, 3, 9, Ink); p.Rect(36, 17, 3, 9, Ink);
            p.Rect(15, 5, 5, 3, on ? C(0x91c296) : C(0x7b3a3e)); p.Rect(25, 5, 5, 3, C(0x717770));
            foreach (int x in new[] { 9, 37 }) foreach (int y in new[] { 10, 38 }) p.Rect(x, y, 2, 2, Cream);
            if (on) { p.Pixel(5, 42, C(0xffedba)); p.Line(2, 44, 6, 46, Gold); p.Line(42, 4, 46, 1, Gold); }
        }

        private static void Elevator(Canvas p, bool on)
        {
            p.Rect(1, 0, 46, 63, Ink); p.Rect(4, 3, 40, 57, C(0x46515a));
            p.Frame(5, 4, 38, 55, C(0x9c8860), 2); p.Rect(9, 7, 30, 43, C(0x17232d));
            p.Rect(11, 8, 13, 41, C(0x56605e)); p.Rect(25, 8, 12, 41, C(0x495354));
            p.Rect(12, 11, 10, 36, C(0x626862)); p.Rect(27, 11, 8, 36, C(0x535d5b));
            p.Line(23, 8, 23, 49, Ink, 2); p.Rect(19, 23, 2, 9, Gold); p.Rect(27, 23, 2, 9, Gold);
            p.Rect(8, 51, 32, 8, Ink); p.Rect(10, 53, 28, 4, on ? C(0x588d6b) : C(0x6c5140));
            p.Line(15, 54, 18, 56, Cream); p.Line(18, 56, 21, 54, Cream);
            p.Line(27, 56, 30, 54, Cream); p.Line(30, 54, 33, 56, Cream);
            p.Rect(40, 22, 4, 9, Ink); p.Rect(41, 25, 2, 3, on ? C(0x9bd196) : RedLight);
            p.Rect(5, 0, 38, 4, C(0x716a59)); p.Line(8, 3, 40, 3, Gold);
            if (on) { p.Rect(11, 50, 26, 1, C(0xc9a65b)); p.Line(14, 47, 21, 47, C(0xa39773)); }
        }

        private static void Door(Canvas p, string key)
        {
            bool locked = key.StartsWith("locked", StringComparison.Ordinal), opened = key.EndsWith("_open", StringComparison.Ordinal);
            Box(p, 3, 1, 42, 46, locked ? C(0x563039) : C(0x55412f), locked ? C(0x98504c) : C(0x94704c));
            p.Rect(7, 3, 34, 40, Ink);
            if (opened)
            {
                p.Rect(9, 4, 25, 37, C(0x131c26)); p.Rect(7, 3, 10, 40, locked ? C(0x763c42) : Wood);
                p.Line(15, 5, 15, 39, locked ? RedLight : WoodLight); p.Rect(11, 21, 3, 5, Gold);
            }
            else
            {
                Box(p, 8, 3, 32, 39, locked ? C(0x512c35) : Wood, locked ? C(0x9b4a48) : C(0xa2794b));
                p.Frame(12, 7, 24, 29, locked ? C(0x783b41) : C(0x493628), 2);
                p.Rect(31, 20, 3, 6, Ink); p.Pixel(32, 23, Gold);
                if (locked)
                {
                    p.Frame(16, 14, 17, 18, C(0xc5584b), 2); p.Rect(20, 17, 9, 7, C(0x1a202a));
                    p.Frame(22, 23, 5, 6, RedLight, 2); p.Pixel(24, 19, Gold);
                    p.Rect(17, 40, 14, 3, RedLight);
                }
                else { p.Line(18, 10, 18, 32, C(0x805735)); p.Line(27, 10, 27, 32, C(0x805735)); }
            }
            p.Line(5, 45, 42, 45, locked ? RedLight : C(0xc1995b)); p.Rect(4, 0, 40, 2, Steel);
        }

        private static void Leaf(Canvas p, int x, int y, int w, int h)
        {
            p.Oval(x - 1, y - 1, w + 2, h + 2, Ink); p.Oval(x, y, w, h, C(0x35583e));
            p.Oval(x + 2, y + h / 2, w - 4, h / 2, C(0x52784b)); p.Pixel(x + w / 2, y + h - 2, C(0x87a063));
        }

        private static void Shadow(Canvas p, int x, int y, int w, int h) { p.Oval(x, y, w, h, C(0x000713, 120)); }
        private static void Box(Canvas p, int x, int y, int w, int h, Color32 fill, Color32 highlight)
        {
            p.Rect(x, y, w, h, Ink); p.Rect(x + 1, y + 1, w - 2, h - 2, fill);
            p.Line(x + 2, y + h - 2, x + w - 3, y + h - 2, highlight);
            p.Line(x + 1, y + 2, x + 1, y + h - 3, Shade(highlight, 0.8f));
            p.Line(x + w - 2, y + 1, x + w - 2, y + h - 3, Shade(fill, 0.6f));
            p.Line(x + 2, y + 1, x + w - 3, y + 1, Shade(fill, 0.65f));
        }

        private static Color32 C(uint hex, byte alpha = 255) { return new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, alpha); }
        private static Color32 Shade(Color32 c, float amount)
        {
            return new Color32((byte)Mathf.Clamp(c.r * amount, 0, 255), (byte)Mathf.Clamp(c.g * amount, 0, 255), (byte)Mathf.Clamp(c.b * amount, 0, 255), c.a);
        }
        private static uint StableHash(string key)
        {
            uint result = 2166136261u;
            for (int i = 0; i < key.Length; i++) result = (result ^ key[i]) * 16777619u;
            return result;
        }

        private sealed class Canvas
        {
            public readonly Color32[] Pixels;
            private readonly int width, height;
            private uint seed;
            public Canvas(int w, int h, uint random) { width = w; height = h; Pixels = new Color32[w * h]; seed = random; }
            public void Pixel(int x, int y, Color32 color)
            {
                if (x >= 0 && y >= 0 && x < width && y < height) Pixels[y * width + x] = color;
            }
            public void Rect(int x, int y, int w, int h, Color32 color)
            {
                for (int yy = Math.Max(0, y); yy < Math.Min(height, y + h); yy++)
                    for (int xx = Math.Max(0, x); xx < Math.Min(width, x + w); xx++) Pixels[yy * width + xx] = color;
            }
            public void Frame(int x, int y, int w, int h, Color32 color, int thickness = 1)
            {
                Rect(x, y, w, thickness, color); Rect(x, y + h - thickness, w, thickness, color);
                Rect(x, y, thickness, h, color); Rect(x + w - thickness, y, thickness, h, color);
            }
            public void Line(int x0, int y0, int x1, int y1, Color32 color, int thickness = 1)
            {
                int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
                int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, error = dx + dy;
                while (true)
                {
                    Rect(x0 - thickness / 2, y0 - thickness / 2, thickness, thickness, color);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = error * 2;
                    if (e2 >= dy) { error += dy; x0 += sx; }
                    if (e2 <= dx) { error += dx; y0 += sy; }
                }
            }
            public void Oval(int x, int y, int w, int h, Color32 color)
            {
                if (w <= 0 || h <= 0) return;
                float rx = w / 2f, ry = h / 2f, cx = x + rx - 0.5f, cy = y + ry - 0.5f;
                for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++)
                {
                    float nx = (xx - cx) / rx, ny = (yy - cy) / ry;
                    if (nx * nx + ny * ny <= 1f) Pixel(xx, yy, color);
                }
            }
            public void OvalFrame(int x, int y, int w, int h, Color32 color, int thickness)
            {
                if (w <= 0 || h <= 0) return;
                float rx = w / 2f, ry = h / 2f, cx = x + rx - 0.5f, cy = y + ry - 0.5f;
                float innerX = Math.Max(0.5f, rx - thickness), innerY = Math.Max(0.5f, ry - thickness);
                for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++)
                {
                    float dx = xx - cx, dy = yy - cy;
                    if (dx * dx / (rx * rx) + dy * dy / (ry * ry) <= 1f && dx * dx / (innerX * innerX) + dy * dy / (innerY * innerY) >= 1f) Pixel(xx, yy, color);
                }
            }
            public int Rand(int min, int max)
            {
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                return min + (int)(seed % (uint)Math.Max(1, max - min));
            }
            public void Noise(int count, Color32 a, Color32 b)
            {
                for (int i = 0; i < count; i++)
                {
                    int x = Rand(0, width), y = Rand(0, height);
                    if (Pixels[y * width + x].a != 0) Pixel(x, y, i % 2 == 0 ? a : b);
                }
            }
            public void FlipX()
            {
                for (int y = 0; y < height; y++) for (int x = 0; x < width / 2; x++)
                {
                    int a = y * width + x, b = y * width + width - x - 1;
                    Color32 temp = Pixels[a]; Pixels[a] = Pixels[b]; Pixels[b] = temp;
                }
            }
        }
    }
}
