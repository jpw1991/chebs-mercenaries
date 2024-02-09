using ChebsMercenaries.Minions;
using ChebsValheimLibrary.Minions;
using ChebsValheimLibrary.PvP;
using HarmonyLib;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local

// Harmony patching is very sensitive regarding parameter names. Everything in this region should be hand crafted
// and not touched by well-meaning but clueless IDE optimizations.
// eg.
// * __instance MUST be named with exactly two underscores.
// * ___m_drops MUST be named with exactly three underscores.
// * Unused parameters must be left there because they must match the method to override
// * All patch methods need to be static
//
// This is because all of this has a special meaning to Harmony.

namespace ChebsMercenaries.Patches
{
    [HarmonyPatch(typeof(MonsterAI))]
    class MonsterAIPatch1
    {
        [HarmonyPatch(nameof(MonsterAI.UpdateTarget))]
        [HarmonyPostfix]
        static void Postfix(MonsterAI __instance, Humanoid humanoid,
            float dt,
            ref bool canHearTarget,
            ref bool canSeeTarget)
        {
            if (__instance.m_attackPlayerObjects
                && __instance.m_targetStatic != null
                && __instance.TryGetComponent(out CatapultMinion catapultMinion))
            {
                // we're checking for PvP here
                if (BasePlugin.PvPAllowed.Value && __instance.m_targetStatic.TryGetComponent(out Piece piece))
                {
                    var structureCreator = piece.m_creator;
                    var creatorPlayer = Player.s_players.Find(p => p.GetPlayerID() == structureCreator);
                    if (creatorPlayer != null)
                    {
                        var catapultMinionMaster = catapultMinion.UndeadMinionMaster;
                        var creatorPlayerName = creatorPlayer.GetPlayerName();
                        
                        var catapultFriendlyToStructure = PvPManager.Friendly(catapultMinionMaster, creatorPlayerName);
                        var structureFriendlyToCatapult = PvPManager.Friendly(creatorPlayerName, catapultMinionMaster);

                        var isEnemy = !(catapultFriendlyToStructure && structureFriendlyToCatapult);
                        if (!isEnemy)
                        {
                            // forget target
                            __instance.m_targetStatic = null;
                        }
                    }
                }
            }
        }
    }
}