using System;
using System.IO;
using UnityEngine;

namespace PlanB.Editor
{
    public static class PlanBMapChecks
    {
        static void Require(bool value, string message) { if (!value) throw new Exception("Map regression: " + message); }
        public static void Verify(GameManager game, int floor)
        {
            var level = game.Floors.ActiveLevel;
            Require(level.MapPreview != null && level.TerrainRendererCount == 29, "Baked terrain/minimap missing.");
            int sprites = level.GetComponentsInChildren<SpriteRenderer>().Length;
            Require(sprites < 250, "Floor exceeded renderer budget: " + sprites);
            Physics2D.SyncTransforms();
            foreach (var door in level.GetComponentsInChildren<Door>())
            {
                var visual = door.GetComponent<SpriteRenderer>();
                Bounds opening = door.OpeningBounds;
                Require(visual.bounds.size.x <= opening.size.x + .01f && visual.bounds.size.y <= opening.size.y + .01f,
                    "Door art extends outside its opening.");
                Require(Mathf.Abs(door.transform.localScale.x - door.transform.localScale.y) < .001f, "Door art is distorted.");
                var hit = Physics2D.CircleCast((Vector2)opening.center + Vector2.left * 1.3f, .28f, Vector2.right, 2.6f, 1 << 8);
                Require(door.Locked ? hit.collider == door.GetComponent<Collider2D>() : hit.collider == null,
                    "Door passage or locked gate collision is incorrect: " + door.name);
            }
            if (floor == 1)
            {
                var nav = new GridNavigationCache();
                Vector2 from = LevelBuilder.Cell(5, 8), goal = LevelBuilder.Cell(5, 23);
                Vector2 direction = nav.Steer(level, from, goal, false);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++) nav.Steer(level, from, goal, false);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Require(direction.sqrMagnitude > .1f && nav.BuildCount == 1 && nav.CacheHits == 1000 && allocated == 0,
                    "Warmed navigation did not reuse its flow field without allocations.");
            }
            var camera = game.CameraRig.GetComponent<Camera>();
            game.CameraRig.Snap(); Vector3 p = camera.transform.position;
            float h = camera.orthographicSize, w = h * camera.aspect;
            Require(w >= 24 || p.x - w >= -24.51f && p.x + w <= 23.51f, "Camera exposes horizontal void.");
            Require(h >= 14 || p.y - h >= -13.51f && p.y + h <= 14.51f, "Camera exposes vertical void.");
            Debug.Log("PLANB_MAP_CHECK_PASS: floor " + floor + ", " + sprites + " sprites; door fit/collision, minimap, camera bounds and navigation passed.");
        }

        public static void CaptureOverview(GameManager game, string name)
        {
            var camera = game.CameraRig.GetComponent<Camera>();
            var previousTarget = camera.targetTexture; float size = camera.orthographicSize;
            Vector3 position = camera.transform.position; var active = RenderTexture.active;
            var target = RenderTexture.GetTemporary(1600, 1000, 24);
            var texture = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.orthographicSize = 15.3f;
                camera.transform.position = new Vector3(-.5f, .5f, -10); camera.Render();
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); texture.Apply();
                Directory.CreateDirectory("Assets/PlanB/Verification");
                File.WriteAllBytes("Assets/PlanB/Verification/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; camera.orthographicSize = size; camera.transform.position = position;
                RenderTexture.active = active; UnityEngine.Object.DestroyImmediate(texture); RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
