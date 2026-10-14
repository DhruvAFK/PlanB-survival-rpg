using UnityEngine;

namespace PlanB
{
    public sealed class Player : MonoBehaviour
    {
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public float MoveSpeed { get; private set; }
        public Weapon Weapon { get; private set; }
        public Vector2 Aim { get; private set; } = Vector2.right;
        public Interactable Nearby { get; private set; }
        Rigidbody2D body;
        SpriteRenderer sprite;
        Vector2 movement;
        string spriteKey;
        float invincibleUntil, bashReady, walkTime;
        int direction, meleeDamage;

        void Awake()
        {
            body = gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = .28f; collider.offset = new Vector2(0, -.17f);
            sprite = gameObject.AddComponent<SpriteRenderer>();
            sprite.sharedMaterial = SpriteLibrary.Material;
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(transform, false); shadow.transform.localPosition = new Vector3(0, -.38f, 0);
            shadow.sprite = SpriteLibrary.Get("shadow"); shadow.sharedMaterial = SpriteLibrary.Material; shadow.sortingOrder = 8;
            Weapon = gameObject.AddComponent<Weapon>();
        }

        public void ResetFor(CharacterClass character)
        {
            MaxHealth = character == CharacterClass.Fighter ? 120 : character == CharacterClass.Scout ? 90 : 100;
            Health = MaxHealth;
            MoveSpeed = character == CharacterClass.Scout ? 4.25f : 3.4f;
            meleeDamage = character == CharacterClass.Fighter ? 2 : 1;
            spriteKey = character.ToString().ToLowerInvariant();
            Weapon.Configure(character);
            sprite.sprite = SpriteLibrary.Get(spriteKey, 1, 0);
        }

        void Update()
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsPlaying) { movement = Vector2.zero; return; }
            movement = InputBridge.Move;
            var camera = game.CameraRig.GetComponent<Camera>();
            Vector3 mouse = camera.ScreenToWorldPoint(new Vector3(InputBridge.MousePosition.x, InputBridge.MousePosition.y, -camera.transform.position.z));
            Vector2 aim = (Vector2)(mouse - transform.position);
            if (aim.sqrMagnitude > .02f) Aim = aim.normalized;
            var facing = movement.sqrMagnitude > .01f ? movement : Aim;
            direction = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? facing.x < 0 ? 2 : 3 : facing.y > 0 ? 1 : 0;
            if (movement.sqrMagnitude > .01f) walkTime += Time.deltaTime * 8;
            int tick = Mathf.FloorToInt(walkTime) % 4;
            int frame = movement.sqrMagnitude > .01f ? tick == 3 ? 1 : tick : 1;
            sprite.sprite = SpriteLibrary.Get(spriteKey, frame, direction);
            sprite.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 5);
            sprite.color = Time.time < invincibleUntil ? new Color(1, .55f, .5f, .8f + .2f * Mathf.Sin(Time.time * 30)) : Color.white;
            FindInteraction();
            if (InputBridge.InteractPressed && Nearby != null) Nearby.Interact();
            if (InputBridge.BashPressed) Bash();
            if (InputBridge.TrapPressed) Trap.TryPlace();
            Weapon.Tick(Aim);
        }

        void FixedUpdate()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
                body.MovePosition(body.position + movement * (MoveSpeed * Time.fixedDeltaTime));
        }

        void FindInteraction()
        {
            Nearby = null;
            float closest = float.MaxValue;
            foreach (var item in Interactable.All)
            {
                if (item == null || !item.isActiveAndEnabled || !item.Available) continue;
                float distance = ((Vector2)(item.transform.position - transform.position)).sqrMagnitude;
                if (distance < item.Range * item.Range && distance < closest) { Nearby = item; closest = distance; }
            }
        }

        public void Bash()
        {
            if (Time.time < bashReady) return;
            bashReady = Time.time + .6f;
            AudioFactory.Play("hit", .45f);
            Effects.Burst((Vector2)transform.position + Aim * .8f, new Color(1, .7f, .35f), 5);
            var enemies = GameManager.Instance.Floors.ActiveRoot.GetComponentsInChildren<Enemy>();
            foreach (Enemy enemy in enemies)
            {
                Vector2 offset = enemy.transform.position - transform.position;
                if (!enemy.IsDead && offset.sqrMagnitude <= 1.55f * 1.55f && Vector2.Dot(offset.normalized, Aim) > -.15f)
                {
                    var wall = Physics2D.Linecast(transform.position, enemy.transform.position, 1 << 8);
                    if (wall.collider == null) { enemy.Damage(meleeDamage); enemy.Stun(.2f); }
                }
            }
        }

        public void Damage(int damage)
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsPlaying || Time.time < invincibleUntil || Health <= 0) return;
            invincibleUntil = Time.time + .75f;
            Health = Mathf.Max(0, Health - damage);
            AudioFactory.Play("hurt"); game.DamageFlash(); game.CameraRig.Shake(.18f);
            if (Health == 0) game.EndRun(false);
        }
        public void Heal(int amount) { Health = Mathf.Min(MaxHealth, Health + amount); }
        public void Teleport(Vector2 position)
        {
            body.position = position; transform.position = position;
            movement = Vector2.zero; Nearby = null; invincibleUntil = Time.time + 1.5f;
        }
    }
}
