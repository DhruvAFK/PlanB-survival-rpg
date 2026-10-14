using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanB
{
    /// <summary>One cached, point-filtered sprite per visual state; never requires an art import.</summary>
    public static class SpriteLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
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
            key = string.IsNullOrEmpty(key) ? "tile_metal" : key.ToLowerInvariant();
            frame = ((frame % 3) + 3) % 3;
            direction = ((direction % 4) + 4) % 4;
            bool animated = key == "fighter" || key == "engineer" || key == "scout" ||
                            key == "scientist" || key == "walker" || key == "runner" ||
                            key == "crawler" || key == "spitter" || key == "warden";
            string id = animated ? key + "_" + direction + "_" + frame : key;
            Sprite sprite;
            if (Cache.TryGetValue(id, out sprite) && sprite != null) return sprite;
            // Individual replacement sprites can be added later without changing gameplay.
            sprite = Resources.Load<Sprite>("Sprites/" + id);
            if (sprite == null) sprite = Resources.Load<Sprite>("PlanB/Sprites/" + id);
            if (sprite == null) sprite = SpriteFactory.Create(key, frame, direction);
            if (sprite != null)
            {
                sprite.texture.filterMode = FilterMode.Point;
                sprite.texture.wrapMode = TextureWrapMode.Clamp;
                Cache[id] = sprite;
            }
            return sprite;
        }
    }
}
