using UnityEngine;

namespace PlanB
{
    public sealed class Weapon : MonoBehaviour
    {
        public const int MagazineSize = 12;
        public int Magazine { get; private set; } = MagazineSize;
        public int Reserve = 24;
        public bool Reloading { get; private set; }
        public float ReloadProgress { get { return Reloading ? 1 - Mathf.Clamp01((reloadEnds - Time.time) / reloadDuration) : 0; } }
        float reloadDuration = 1.2f, reloadEnds, fireReady, flashUntil;
        SpriteRenderer gun, muzzle;

        void Awake()
        {
            gun = Make("Pistol", "gun");
            muzzle = Make("Muzzle flash", "muzzle"); muzzle.enabled = false;
        }
        SpriteRenderer Make(string label, string key)
        {
            var renderer = new GameObject(label).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(transform, false); renderer.sprite = SpriteLibrary.Get(key);
            renderer.sharedMaterial = SpriteLibrary.Material; return renderer;
        }
        public void Configure(CharacterClass character)
        {
            Magazine = MagazineSize; Reserve = character == CharacterClass.Engineer ? 36 : 24;
            reloadDuration = character == CharacterClass.Engineer ? .8f : 1.2f;
            Reloading = false; fireReady = 0;
        }
        public void Tick(Vector2 aim)
        {
            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            gun.transform.localPosition = (Vector3)(aim * .37f) + Vector3.down * .1f;
            gun.transform.rotation = Quaternion.Euler(0, 0, angle);
            gun.flipY = aim.x < 0;
            gun.sortingOrder = 105 - Mathf.RoundToInt(transform.position.y * 5);
            muzzle.transform.localPosition = (Vector3)(aim * .76f) + Vector3.down * .1f;
            muzzle.transform.rotation = gun.transform.rotation; muzzle.sortingOrder = gun.sortingOrder + 1;
            muzzle.enabled = Time.time < flashUntil;
            if (Reloading && Time.time >= reloadEnds)
            {
                int count = Mathf.Min(MagazineSize - Magazine, Reserve);
                Magazine += count; Reserve -= count; Reloading = false;
            }
            if (InputBridge.ReloadPressed) Reload();
            if (InputBridge.ShootHeld) Fire(aim);
        }
        public void Reload()
        {
            if (Reloading || Magazine == MagazineSize || Reserve <= 0) return;
            Reloading = true; reloadEnds = Time.time + reloadDuration;
            AudioFactory.Play("reload");
        }
        public bool Fire(Vector2 aim)
        {
            if (Reloading || Time.time < fireReady || !GameManager.Instance.IsPlaying) return false;
            if (Magazine <= 0) { Reload(); return false; }
            Magazine--; fireReady = Time.time + .25f; flashUntil = Time.time + .055f;
            Vector2 origin = (Vector2)transform.position + Vector2.down * .1f;
            // Cast from the actor, so a muzzle close to a wall cannot fire through it.
            Bullet.Spawn(origin, aim, false, 1, 18, GameManager.Instance.Floors.ActiveRoot);
            AudioFactory.Play("shoot", .65f); GameManager.Instance.CameraRig.Shake(.035f);
            return true;
        }
    }
}
