using UnityEngine;

namespace PlanB
{
    public enum PickupKind { Ammo, Medkit, Fuse, Key, Scrap, Battery, Wire, Trap }

    public sealed class Pickup : MonoBehaviour
    {
        public PickupKind Kind;
        public int Amount;
        private Vector2 origin;
        private float phase;
        private bool collected;

        public static Pickup Spawn(PickupKind kind, Vector2 position, Transform parent = null, int amount = 0)
        {
            string key = kind.ToString().ToLowerInvariant();
            var renderer = LevelBuilder.Sprite(key, position, parent, 65, new Vector2(.68f, .68f));
            var pickup = renderer.gameObject.AddComponent<Pickup>();
            pickup.Kind = kind;
            pickup.Amount = amount > 0 ? amount : kind == PickupKind.Ammo ? 12 : kind == PickupKind.Medkit ? 30 : 1;
            pickup.origin = position;
            pickup.phase = position.x * .67f + position.y * .93f;
            var trigger = renderer.gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true; trigger.radius = .55f;
            return pickup;
        }

        public static Pickup Spawn(string kind, Vector2 position, Transform parent = null)
        {
            PickupKind parsed;
            if (!System.Enum.TryParse(kind, true, out parsed)) parsed = PickupKind.Ammo;
            return Spawn(parsed, position, parent);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying || gm.Player == null || collected) return;
            transform.position = origin + Vector2.up * (Mathf.Sin(Time.time * 3 + phase) * .07f);
            if (((Vector2)gm.Player.transform.position - origin).sqrMagnitude > .74f * .74f) return;
            if (Kind == PickupKind.Medkit && gm.Player.Health >= gm.Player.MaxHealth) return;
            if (Kind == PickupKind.Trap && gm.TrapCount >= 2) return;
            Collect(gm);
        }

        private void Collect(GameManager gm)
        {
            string message = "";
            switch (Kind)
            {
                case PickupKind.Ammo:
                    if (gm.Player.Weapon == null) return;
                    gm.Player.Weapon.Reserve += Amount;
                    message = "+" + Amount + " reserve ammo"; break;
                case PickupKind.Medkit:
                    gm.Player.Heal(Amount); message = "+" + Amount + " health"; break;
                case PickupKind.Fuse:
                    gm.HasFuse = true; message = "FUSE FOUND  •  Insert it into the maintenance generator."; break;
                case PickupKind.Key:
                    gm.HasKey = true; message = "SECURITY KEY FOUND  •  Open the red quarantine door."; break;
                case PickupKind.Scrap: gm.Scrap += Amount; message = "Salvaged scrap"; break;
                case PickupKind.Battery: gm.Battery += Amount; message = "Salvaged battery"; break;
                case PickupKind.Wire: gm.Wire += Amount; message = "Salvaged wire"; break;
                case PickupKind.Trap: gm.TrapCount = Mathf.Min(2, gm.TrapCount + Amount); message = "TRAP FOUND  •  Press T to place."; break;
            }
            collected = true;
            AudioFactory.Play("pickup", .8f);
            gm.Notify(message, Kind == PickupKind.Fuse || Kind == PickupKind.Key ? 4 : 2);
            gm.RefreshObjective();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}

