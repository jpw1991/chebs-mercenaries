using ChebsMercenaries.Minions;
using ChebsNecromancy.Minions;
using ChebsValheimLibrary.Minions;
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

namespace ChebsNecromancy.Patches
{
    [HarmonyPatch(typeof(MonsterAI))]
    class FriendlySkeletonPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MonsterAI.Awake))]
        static void AwakePostfix(ref Character __instance)
        {
            if (__instance.name.StartsWith("ChebGonaz") && __instance.name.Contains("Human"))
            {
                if (!__instance.TryGetComponent(out ChebGonazMinion _))
                {
                    if (__instance.name.Contains("Miner"))
                    {
                        __instance.gameObject.AddComponent<HumanMinerMinion>();
                    }
                    else if (__instance.name.Contains("Woodcutter"))
                    {
                        __instance.gameObject.AddComponent<HumanWoodcutterMinion>();
                    }
                    else
                    {
                        __instance.gameObject.AddComponent<HumanMinion>();
                    }
                }
            }
        }
    }
}