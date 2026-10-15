using UnityEngine;

namespace PlanB
{
    public sealed class Door : Interactable
    {
        public bool Locked;
        [SerializeField] private bool open;
        [SerializeField] private BoxCollider2D gate;
        [SerializeField] private Vector2 openingSize = new Vector2(1, 2);
        [SerializeField] private SpriteRenderer visual;
        private float nextScan;
        public Bounds OpeningBounds { get { return new Bounds(transform.position, new Vector3(openingSize.x, openingSize.y, .1f)); } }
        public override bool Available { get { return Locked && !open; } }
        public override string Prompt
        {
            get
            {
                var gm = GameManager.Instance;
                return gm != null && gm.HasKey ? "[E] Unlock quarantine door" : "[E] Quarantine door • Security key required";
            }
        }

        public void ConfigureGate(Vector2 worldSize)
        {
            ConfigureOpening(worldSize);
            gameObject.layer = 8;
            if (gate == null) gate = gameObject.AddComponent<BoxCollider2D>();
            gate.size = new Vector2(worldSize.x / transform.localScale.x, worldSize.y / transform.localScale.y);
            Range = 2.0f;
        }

        public void ConfigureOpening(Vector2 worldSize)
        {
            openingSize = worldSize;
            visual = GetComponent<SpriteRenderer>();
            if (visual == null || visual.sprite == null) return;
            Vector2 native = visual.sprite.bounds.size;
            float scale = Mathf.Min(worldSize.x / Mathf.Max(.01f, native.x), worldSize.y / Mathf.Max(.01f, native.y));
            transform.localScale = new Vector3(scale, scale, 1);
            visual.sortingOrder = 100 - Mathf.RoundToInt((transform.position.y - native.y * scale * .5f) * 5);
        }

        private void Update()
        {
            if (Locked || open && gate != null || Time.time < nextScan) return;
            nextScan = Time.time + .1f;
            var gm = GameManager.Instance;
            bool near = gm != null && gm.Player != null && Vector2.Distance(gm.Player.transform.position, transform.position) < 1.9f;
            if (near != open)
            {
                open = near; SetSprite(open ? "door_open" : "door");
                if (gm != null && gm.IsPlaying) AudioFactory.PlayAt("door", transform.position, .22f);
            }
        }

        public override void Interact()
        {
            var gm = GameManager.Instance;
            if (!Locked || open || gm == null) return;
            if (!gm.HasKey)
            {
                gm.Notify("Find the security key in the server room.", 3); AudioFactory.Play("locked", .3f); return;
            }
            open = true;
            gm.DoorUnlocked = true;
            if (gate != null) gate.enabled = false;
            SetSprite("lockeddoor_open");
            if (gm.Floors != null && gm.Floors.ActiveLevel != null) gm.Floors.ActiveLevel.InvalidatePaths();
            gm.Notify("QUARANTINE OPEN  •  Defeat the Warden.", 4);
            AudioFactory.Play("unlock", .65f);
            gm.RefreshObjective();
        }
    }
}

