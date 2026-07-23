using UnityEngine;

namespace BCClashers.Redesign
{
    /// <summary>
    /// Recolors a brainBot's body + cape renderers by team at runtime, leaving gold/steel/glow/dark
    /// trim untouched. Added to the "BotVisual" node by BrainBotFactory with all four materials wired.
    /// TroopAI.AssignToTeam calls Apply(isDefender): the local player's invaders stay blue, the
    /// opponent's defenders turn red. Swaps shared-material references (no per-instance copies).
    /// </summary>
    public class BotTeamPainter : MonoBehaviour
    {
        public Material bodyBlue, bodyRed, capeBlue, capeRed;

        public void Apply(bool red)
        {
            var body = red ? bodyRed : bodyBlue;
            var cape = red ? capeRed : capeBlue;
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                var m = r.sharedMaterial;
                if (m == bodyBlue || m == bodyRed) r.sharedMaterial = body;
                else if (m == capeBlue || m == capeRed) r.sharedMaterial = cape;
            }
        }
    }
}
