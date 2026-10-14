using UnityEngine;

namespace PlanB
{
    public sealed class Cabinet : Interactable
    {
        public bool GuaranteedAmmo;
        private bool searched;
        public override bool Available { get { return !searched; } }
        public override string Prompt { get { return searched ? "Cabinet searched" : "[E] Search cabinet"; } }
        public override void Interact()
        {
            if (searched) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return;
            searched = true; SetSprite("cabinet_open");
            bool ammo = GuaranteedAmmo || Random.value < .65f;
            Pickup.Spawn(ammo ? PickupKind.Ammo : PickupKind.Medkit,
                (Vector2)transform.position + Vector2.down * .85f, transform.parent);
            gm.Notify(ammo ? "Cabinet: ammunition recovered." : "Cabinet: medical supplies recovered.", 2);
            AudioFactory.Play("pickup", .6f);
        }
    }
}

