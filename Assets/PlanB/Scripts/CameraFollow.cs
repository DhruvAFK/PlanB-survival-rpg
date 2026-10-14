using UnityEngine;

namespace PlanB
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        float shake;
        Vector3 center;
        void Awake()
        {
            var camera = GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 6.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .035f, .05f);
            camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            transform.position = new Vector3(0, 0, -10);
        }
        public void Snap()
        {
            if (Target == null) return;
            center = new Vector3(Target.position.x, Target.position.y, -10);
            transform.position = center; shake = 0;
        }
        public void Shake(float strength) { shake = Mathf.Max(shake, strength); }
        void LateUpdate()
        {
            if (Target == null) return;
            var target = new Vector3(Target.position.x, Target.position.y, -10);
            center = Vector3.Lerp(center, target, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime));
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * .7f);
                transform.position = center + (Vector3)(Random.insideUnitCircle * shake);
            }
            else transform.position = center;
        }
    }
}
