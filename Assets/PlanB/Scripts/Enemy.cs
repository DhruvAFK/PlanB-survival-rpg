using UnityEngine;

namespace PlanB
{
    public enum EnemyKind { Walker, Runner, Crawler, Spitter, Warden }

    /// <summary>Shared steering, damage, animation and melee for facility creatures.</summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyKind Kind { get; private set; }
        public int FloorNumber { get; set; }
        public bool IsDead { get; private set; }
        public int Health { get; protected set; }
        public int MaxHealth { get; protected set; }

        protected Rigidbody2D Body;
        protected SpriteRenderer Visual;
        protected float MoveSpeed;
        protected int ContactDamage;
        protected float NextShot;
        protected float StunnedUntil;
        protected Color BaseColor = Color.white;
        protected Vector2 Facing = Vector2.down;
        private float flashUntil;
        private float nextContact;
        private float animationClock;
        private string spriteName;
        private Vector2 lastMovement;

        public static Enemy Spawn(EnemyKind kind, Vector2 position, Transform parent)
        {
            var go = new GameObject(kind.ToString());
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = SpriteLibrary.Material;
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = .29f;
            collider.offset = new Vector2(0, -.12f);
            Enemy enemy;
            if (kind == EnemyKind.Warden) enemy = go.AddComponent<Boss>();
            else enemy = go.AddComponent<Enemy>();
            enemy.Initialize(kind);
            var game = GameManager.Instance;
            int floor = game != null && game.Floors != null ? game.Floors.CurrentFloor : 1;
            if (parent != null)
            {
                var parentName = parent.name.ToLowerInvariant();
                if (parentName.Contains("2") || parentName.Contains("research")) floor = 2;
                else if (parentName.Contains("1") || parentName.Contains("residential")) floor = 1;
            }
            enemy.FloorNumber = floor;
            if (game != null && game.Floors != null) game.Floors.RegisterEnemy(enemy, floor);
            return enemy;
        }

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Visual = GetComponent<SpriteRenderer>();
        }

        protected virtual void Initialize(EnemyKind kind)
        {
            Kind = kind;
            spriteName = kind.ToString().ToLowerInvariant();
            switch (kind)
            {
                case EnemyKind.Runner:
                    MaxHealth = 2; MoveSpeed = 2.5f; ContactDamage = 8;
                    transform.localScale = Vector3.one * .78f;
                    BaseColor = new Color(1f, .81f, .72f);
                    break;
                case EnemyKind.Crawler:
                    MaxHealth = 2; MoveSpeed = 2.7f; ContactDamage = 8;
                    break;
                case EnemyKind.Spitter:
                    MaxHealth = 3; MoveSpeed = 1.4f; ContactDamage = 8;
                    BaseColor = new Color(.88f, .91f, 1f);
                    break;
                case EnemyKind.Warden:
                    MaxHealth = 25; MoveSpeed = 1.25f; ContactDamage = 15;
                    transform.localScale = Vector3.one * 2f;
                    break;
                default:
                    MaxHealth = 3; MoveSpeed = 1.15f; ContactDamage = 10;
                    transform.localScale = Vector3.one * .95f;
                    break;
            }
            Health = MaxHealth;
            NextShot = Time.time + Random.Range(1f, 2f);
            if (Visual != null)
            {
                Visual.sprite = SpriteLibrary.Get(spriteName, 1);
                Visual.color = BaseColor;
            }
        }

        protected virtual bool CanAct(GameManager game)
        {
            return !IsDead && game != null && game.IsPlaying && game.Player != null &&
                   game.Player.Health > 0 && game.Floors != null &&
                   game.Floors.CurrentFloor == FloorNumber;
        }

        private void Update()
        {
            if (IsDead || Visual == null) return;
            Visual.color = Time.time < flashUntil ? new Color(1f, .22f, .21f) : BaseColor;
            Visual.sortingOrder = Mathf.RoundToInt(100 - transform.position.y * 10);
            if (lastMovement.sqrMagnitude > .01f) animationClock += Time.deltaTime * 7;
            int frame = lastMovement.sqrMagnitude > .01f ? (int)animationClock % 3 : 1;
            Visual.sprite = SpriteLibrary.Get(spriteName, frame);
            Visual.flipX = Facing.x < -.05f;
        }

        private void FixedUpdate()
        {
            var game = GameManager.Instance;
            lastMovement = Vector2.zero;
            if (!CanAct(game) || Time.time < StunnedUntil || Body == null) return;
            var difference = (Vector2)game.Player.transform.position - Body.position;
            float distance = difference.magnitude;
            float aggro = Time.time < game.AlarmUntil ? 28 : 14;
            if (distance > aggro) return;
            if (distance > .001f) Facing = difference / distance;
            Think(game, difference, distance);
        }

        protected virtual void Think(GameManager game, Vector2 difference, float distance)
        {
            Vector2 target = game.Player.transform.position;
            if (Kind == EnemyKind.Spitter)
            {
                if (distance < 4.6f) Move(game, Body.position - Facing * 2);
                else if (distance > 6.7f) Move(game, target);
                if (Time.time >= NextShot && distance < 13 && distance > .1f)
                {
                    NextShot = Time.time + 2;
                    Fire(Facing, 12, 4f);
                }
            }
            else if (distance > .55f) Move(game, target);
        }

        protected void Move(GameManager game, Vector2 target)
        {
            Vector2 steering = game.Floors.Steer(Body.position, target);
            if (steering.sqrMagnitude < .001f) return;
            steering.Normalize();
            lastMovement = steering;
            Body.MovePosition(Body.position + steering * (MoveSpeed * Time.fixedDeltaTime));
        }

        protected void Fire(Vector2 direction, int damage, float speed)
        {
            Vector2 origin = Body != null ? Body.position : (Vector2)transform.position;
            Bullet.Spawn(origin + direction * .65f, direction, true, damage, speed, transform.parent);
            Effects.Burst(origin + direction * .65f, new Color(1f, .15f, .2f), 3);
            AudioFactory.Play("laser", .25f);
        }

        private void OnCollisionEnter2D(Collision2D collision) { Touch(collision.collider); }
        private void OnCollisionStay2D(Collision2D collision) { Touch(collision.collider); }

        private void Touch(Collider2D other)
        {
            if (Time.time < nextContact || Time.time < StunnedUntil || !CanAct(GameManager.Instance)) return;
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;
            nextContact = Time.time + .7f;
            player.Damage(ContactDamage);
        }

        public virtual void Damage(int damage)
        {
            if (IsDead || damage <= 0) return;
            Health = Mathf.Max(0, Health - damage);
            flashUntil = Time.time + .13f;
            Effects.Burst(transform.position, new Color(.67f, .17f, .21f), 5);
            AudioFactory.Play("hit", .35f);
            if (Health <= 0) Die();
        }

        public void Stun(float seconds)
        {
            if (!IsDead) StunnedUntil = Mathf.Max(StunnedUntil, Time.time + Mathf.Max(0, seconds));
        }

        protected virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Effects.Burst(transform.position, new Color(.5f, .14f, .2f), 14);
            if (Random.value < .25f)
                Pickup.Spawn(Random.value < .65f ? PickupKind.Ammo : PickupKind.Medkit, transform.position, transform.parent);
            var game = GameManager.Instance;
            if (game != null) game.OnEnemyKilled(this);
            if (Body != null) Body.simulated = false;
            if (Visual != null) Visual.enabled = false;
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
            Destroy(gameObject, .05f);
        }
    }
}
