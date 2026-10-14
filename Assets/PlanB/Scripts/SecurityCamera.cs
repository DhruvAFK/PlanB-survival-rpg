using UnityEngine;

namespace PlanB
{
    public sealed class SecurityCamera : MonoBehaviour
    {
        public Vector2 Direction = Vector2.left;
        private float nextAlarm;
        private Mesh cone;
        private const float Reach = 6;
        private const float HalfAngle = 26;

        private void Start()
        {
            var go = new GameObject("Visible security cone");
            go.transform.SetParent(transform, false);
            // Cancel the camera sprite scale: the cone is measured in world units.
            go.transform.localScale = new Vector3(1 / transform.localScale.x, 1 / transform.localScale.y, 1);
            cone = new Mesh { name = "Security camera cone" };
            Vector2 d = Direction.normalized;
            Vector2 a = Quaternion.Euler(0, 0, -HalfAngle) * d;
            Vector2 b = Quaternion.Euler(0, 0, HalfAngle) * d;
            cone.vertices = new[] { Vector3.zero, (Vector3)(a * Reach), (Vector3)(b * Reach) };
            cone.triangles = new[] { 0, 1, 2, 0, 2, 1 };
            cone.colors = new[] { new Color(1, .06f, .08f, .30f), new Color(1, .06f, .08f, .015f), new Color(1, .06f, .08f, .015f) };
            cone.uv = new[] { Vector2.zero, Vector2.right, Vector2.up };
            cone.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = cone;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SpriteLibrary.Material;
            renderer.sortingOrder = -60;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying || gm.Player == null || Time.time < nextAlarm) return;
            Vector2 delta = gm.Player.transform.position - transform.position;
            if (delta.magnitude > Reach || Vector2.Angle(Direction, delta) > HalfAngle) return;
            var hits = Physics2D.LinecastAll(transform.position, gm.Player.transform.position);
            foreach (var hit in hits) if (hit.collider != null && hit.collider.GetComponent<SolidWall>() != null) return;
            nextAlarm = Time.time + 12;
            gm.Alarm(10);
        }

        private void OnDestroy() { if (cone != null) Destroy(cone); }
    }
}

