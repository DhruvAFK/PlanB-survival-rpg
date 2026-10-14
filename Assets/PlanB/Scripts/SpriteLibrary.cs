using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanB
{
    /// <summary>One cached, point-filtered sprite per visual state; never requires an art import.</summary>
    public static class SpriteLibrary
    {
        private sealed class SpriteSet
        {
            public string Key;
            public bool Animated;
            public Sprite[] Frames;
        }
        // Case-insensitive keys avoid lowercase allocations for every animated actor frame.
        private static readonly Dictionary<string, SpriteSet> Cache = new Dictionary<string, SpriteSet>(64, StringComparer.OrdinalIgnoreCase);
        private static Material material;

        public static Material Material
        {
            get
            {
                if (material == null)
                {
                    material = Resources.Load<Material>("PlanB/SpriteMaterial");
                    if (material != null) return material;
                    Shader shader = Shader.Find("Sprites/Default");
                    if (shader == null) shader = Shader.Find("UI/Default");
                    material = new Material(shader) { name = "PlanB Pixel Sprites" };
                }
                return material;
            }
        }

        public static Sprite Get(string key, int frame = 0, int direction = 0)
        {
            if (string.IsNullOrEmpty(key)) key = "tile_metal";
            SpriteSet set;
            if (!Cache.TryGetValue(key, out set))
            {
                string canonical = key.ToLowerInvariant();
                bool animated = canonical == "fighter" || canonical == "engineer" || canonical == "scout" ||
                                canonical == "scientist" || canonical == "walker" || canonical == "runner" ||
                                canonical == "crawler" || canonical == "spitter" || canonical == "warden";
                set = new SpriteSet { Key = canonical, Animated = animated, Frames = new Sprite[animated ? 12 : 1] };
                Cache.Add(key, set);
            }
            frame = ((frame % 3) + 3) % 3;
            direction = ((direction % 4) + 4) % 4;
            int index = set.Animated ? direction * 3 + frame : 0;
            Sprite sprite = set.Frames[index];
            if (sprite != null && sprite.texture != null) return sprite;
            string id = set.Animated ? set.Key + "_" + direction + "_" + frame : set.Key;
            // Individual replacement sprites can be added later without changing gameplay.
            sprite = Resources.Load<Sprite>("Sprites/" + id);
            if (sprite == null) sprite = Resources.Load<Sprite>("PlanB/Sprites/" + id);
            if (sprite == null) sprite = SpriteFactory.Create(set.Key, frame, direction);
            if (sprite != null)
            {
                sprite.texture.filterMode = FilterMode.Point;
                sprite.texture.wrapMode = TextureWrapMode.Clamp;
                set.Frames[index] = sprite;
            }
            return sprite;
        }
    }
}
