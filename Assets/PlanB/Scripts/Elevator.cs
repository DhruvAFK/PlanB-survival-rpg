using UnityEngine;

namespace PlanB
{
    public sealed class Elevator : Interactable
    {
        public int FloorNumber = 1;
        private bool shownPowered;
        public override string Prompt
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null) return "";
                if (FloorNumber == 1) return gm.PowerRestored ? "[E] Descend to research floor" : "[E] Elevator • Restore generator power";
                return gm.BossDefeated ? "[E] Escape the facility" : "[E] Exit sealed • Defeat the Warden";
            }
        }
        private void Awake() { Range = 2.1f; }
        private void Update()
        {
            var gm = GameManager.Instance;
            bool powered = gm != null && (FloorNumber == 1 ? gm.PowerRestored : gm.BossDefeated);
            if (powered != shownPowered) { shownPowered = powered; SetSprite(powered ? "elevator_on" : "elevator"); }
        }
        public override void Interact()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (FloorNumber == 1)
            {
                if (!gm.PowerRestored) { gm.Notify("Restore the maintenance generator before descending.", 3); return; }
                AudioFactory.Play("elevator"); gm.ChangeFloor(2);
            }
            else
            {
                if (!gm.BossDefeated) { gm.Notify("The exit is sealed until the Warden is eliminated.", 3); return; }
                AudioFactory.Play("elevator"); gm.EndRun(true);
            }
        }
    }
}

