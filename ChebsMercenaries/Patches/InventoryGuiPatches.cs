using ChebsMercenaries.Structure;
using ChebsValheimLibrary.Minions;
using HarmonyLib;
using UnityEngine;
using Logger = Jotunn.Logger;

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
    [HarmonyPatch(typeof(InventoryGui), "Show")]
    class InventoryGuiPatch
    {
        [HarmonyPostfix]
        static void Postfix(Container container, int activeGroup)
        {
            if (container != null && container.name.Contains("ChebGonaz_MercenaryChest"))
            {
                Logger.LogInfo($"HERPDERP Show {container.name}");
                MercenaryChestOptionsGUI.Show(container);
            }
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), "Hide")]
    class InventoryGuiPatch2
    {
        [HarmonyPostfix]
        static void Postfix(InventoryGui __instance)
        {
            // if (__instance != null
            //     && __instance.m_currentContainer != null
            //     && __instance.m_currentContainer.name.Contains("ChebGonaz_MercenaryChest"))
            // {
            //     Logger.LogInfo($"HERPDERP Hide {__instance.name}");
            //     MercenaryChestOptionsGUI.Hide();
            // }
            MercenaryChestOptionsGUI.Hide();
        }
    }
}