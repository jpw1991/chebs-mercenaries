using ChebsMercenaries.Minions;
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
    [HarmonyPatch]
    public class ZNetScenePatches
    {
        // Steal the Player's material from the player and give it to the mercenaries so that their armour displays
        // properly. Thanks to JustAFrogger for this solution.
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
        public static void ZNetScenePatch(ZNetScene __instance)
        {
            var playerPrefab = __instance.GetPrefab("Player");
            if (playerPrefab == null)
            {
                Jotunn.Logger.LogError($"ZNetScenePatches: Failed to get player prefab. Armor may not display" +
                                       $"correctly on mercenaries.");
                return;
            }

            if (!playerPrefab.TryGetComponent(out VisEquipment playerVisEquipment))
            {
                Jotunn.Logger.LogError($"ZNetScenePatches: Failed to get Player's VisEquipment " +
                                       $"component. Armor may not display correctly on mercenaries.");
                return;
            }

            var male = playerVisEquipment.m_models[0].m_baseMaterial;
            var female = playerVisEquipment.m_models[1].m_baseMaterial;
            
            BasePlugin.MercenaryPrefabPaths.ForEach(prefabFileName =>
            {
                var prefabName = prefabFileName.Replace(".prefab", "");
                var mercPrefab = __instance.GetPrefab(prefabName);
                if (mercPrefab == null)
                {
                    Jotunn.Logger.LogError($"ZNetScenePatches: Failed to get {prefabName}. Armor may not display" +
                                           $"correctly on mercenaries.");
                    return;
                }

                if (!mercPrefab.TryGetComponent(out VisEquipment visEquipment))
                {
                    Jotunn.Logger.LogError($"ZNetScenePatches: Failed to get {prefabName}'s VisEquipment " +
                                           $"component. Armor may not display correctly on mercenaries.");
                    return;
                }

                visEquipment.m_bodyModel.material = prefabName.Contains("Female") ? female : male;
            });
        }
    }
}