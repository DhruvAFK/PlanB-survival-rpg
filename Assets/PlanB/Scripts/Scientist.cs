using UnityEngine;

namespace PlanB
{
    public sealed class Scientist : Interactable
    {
        public int FloorNumber = 1;
        private int line;
        public override string Prompt { get { return "[E] Talk to scientist"; } }
        public override void Interact()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            string[] hints;
            if (FloorNumber == 1)
                hints = new[] {
                    "Scientist: A spare fuse is in the kitchen storage, southwest. Watch the creatures.",
                    "Scientist: Bring the fuse to the generator in northeast maintenance.",
                    "Scientist: Once power returns, take the elevator at the south end. Keep spare ammunition." };
            else
                hints = new[] {
                    "Scientist: The security key is in the server room, northeast.",
                    "Scientist: Unlock the red quarantine door east of here. The Warden controls our exit.",
                    "Scientist: Save room to dodge its bursts. Return to this elevator when it falls." };
            gm.Notify(hints[line % hints.Length], 5.5f); line++;
            AudioFactory.PlayAt("scientist", transform.position, .25f);
        }
    }
}

