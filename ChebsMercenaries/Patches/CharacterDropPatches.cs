using System.Collections.Generic;
using ChebsMercenaries.Minions;
using ChebsValheimLibrary.Minions;
using HarmonyLib;
using Jotunn;

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
    [HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
    class CharacterDrop_Patches
    {
        [HarmonyPrefix]
        static void AddBonesToDropList(ref List<CharacterDrop.Drop> ___m_drops, CharacterDrop __instance)
        {
            // For all other minions, check if they're supposed to be dropping
            // items and whether these should be packed into a crate or not.
            // We don't want ppls surtling cores and things to be claimed by davey jones
            if (__instance.TryGetComponent(out HumanMinion humanMinion))
            {
                if (HumanMinion.DropOnDeath.Value != ChebGonazMinion.DropType.Nothing
                    && HumanMinion.PackDropItemsIntoCargoCrate.Value)
                {
                    humanMinion.DepositIntoNearbyDeathCrate(__instance);
                }

                if (humanMinion.ItemsDropped)
                {
                    Logger.LogInfo("items dropped is true");
                    ___m_drops.RemoveAll(a => true);
                }
            }
        }
    }
}