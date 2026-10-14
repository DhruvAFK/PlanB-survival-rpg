using UnityEngine;

namespace PlanB
{
    /// <summary>Short-lived point-filtered debris without particle-system assets.</summary>
    public sealed class Effects : MonoBehaviour
    {
        private static Sprite fragment;
        private Transform[] pieces;
        private SpriteRenderer[] renderers;
        private Vector2[] velocities;
        private Color tint;
        private float age;
        private const float Lifetime = .36f;

        public static void Burst(Vector2 position, Color color, int count = 8)
        {
            if (count <= 0) return;
            if (fragment == null)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply(false, true);
                fragment = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 24);
                fragment.name = "PlanB Debris";
            }
            var root = new GameObject("Impact debris");
            root.transform.position = position;
            var effect = root.AddComponent<Effects>();
            effect.tint = color;
            effect.pieces = new Transform[count];
            effect.renderers = new SpriteRenderer[count];
            effect.velocities = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                var piece = new GameObject("Fragment");
                piece.transform.SetParent(root.transform, false);
                piece.transform.localScale = Vector3.one * Random.Range(.5f, 1.25f);
                var renderer = piece.AddComponent<SpriteRenderer>();
                renderer.sprite = fragment;
                renderer.sharedMaterial = SpriteLibrary.Material;
                renderer.color = color;
                renderer.sortingOrder = 2000;
                effect.pieces[i] = piece.transform;
                effect.renderers[i] = renderer;
                effect.velocities[i] = Random.insideUnitCircle.normalized * Random.Range(.7f, 3f);
            }
            Destroy(root, Lifetime);
        }

        private void Update()
        {
            if (pieces == null) return;
            age += Time.deltaTime;
            var fade = tint;
            fade.a *= Mathf.Clamp01(1 - age / Lifetime);
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i].localPosition += (Vector3)(velocities[i] * Time.deltaTime);
                velocities[i] *= Mathf.Pow(.03f, Time.deltaTime);
                renderers[i].color = fade;
            }
        }
    }
}
