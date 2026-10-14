using UnityEngine;

namespace PlanB
{
    public sealed class Generator : Interactable
    {
        private SpriteRenderer sprite;
        private float sparkTimer;
        public override bool Available { get { return GameManager.Instance != null && !GameManager.Instance.PowerRestored; } }
        public override string Prompt
        {
            get
            {
                var gm = GameManager.Instance;
                return gm != null && gm.HasFuse ? "[E] Insert fuse • Restore power" : "[E] Generator • Fuse required";
            }
        }
        private void Awake() { Range = 2.5f; sprite = GetComponent<SpriteRenderer>(); }
        public override void Interact()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.PowerRestored) return;
            if (!gm.HasFuse) { gm.Notify("The generator needs a fuse. Search the kitchen storage.", 3); return; }
            gm.HasFuse = false; gm.PowerRestored = true;
            SetSprite("generator_on"); sparkTimer = 1.4f;
            gm.Notify("POWER RESTORED  •  The south elevator is online.", 5);
            AudioFactory.Play("generator");
            gm.RefreshObjective();
        }

        private void Update()
        {
            if (sparkTimer <= 0 || sprite == null) return;
            sparkTimer -= Time.deltaTime;
            sprite.color = Mathf.Sin(Time.time * 35) > 0 ? new Color(1, .79f, .35f) : Color.white;
            if (sparkTimer <= 0) sprite.color = Color.white;
        }
    }
}

