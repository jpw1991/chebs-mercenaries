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

using HarmonyLib;

namespace ChebsMercenaries.Patches
{
    /// <summary>
    /// Copies of VisEquipment methods but with the "isPlayer" check removed so that the NPCs can display their hair
    /// and beards. Only affects prefabs that start with ChebGonaz_Human.
    /// </summary>
    [HarmonyPatch(typeof(VisEquipment), "UpdateVisuals")]
    class VisEquipmentPatch1
    {
        [HarmonyPostfix]
        static void UpdateVisualsPostfix(VisEquipment __instance)
        {
            if (!__instance.gameObject.name.StartsWith("ChebGonaz_Human")) return;
            __instance.UpdateBaseModel();
            __instance.UpdateColors();
        }
    }
    
    [HarmonyPatch(typeof(VisEquipment), "UpdateEquipmentVisuals")]
    class VisEquipmentPatch2
    {
        [HarmonyPostfix]
        static void Postfix(VisEquipment __instance)
        {
            // This is a copy of VisEquipment.UpdateEquipmentVisuals except there is no "isPlayer" check for the beard,
            // hair, etc. so the NPC can display the hair/beard
            if (!__instance.gameObject.name.StartsWith("ChebGonaz_Human")) return;
            
            var hash1 = 0;
            var hash2 = 0;
            var hash3 = 0;
            var hash4 = 0;
            var hash5 = 0;
            var hash6 = 0;
            var num = 0;
            var hash7 = 0;
            var hash8 = 0;
            var leftItem = 0;
            var rightItem = 0;
            var shoulderItemVariant = __instance.m_shoulderItemVariant;
            var leftItemVariant = __instance.m_leftItemVariant;
            var leftBackItemVariant = __instance.m_leftBackItemVariant;
            var zdo = __instance.m_nview.GetZDO();
            if (zdo != null)
            {
                hash1 = zdo.GetInt("LeftItem");
                hash2 = zdo.GetInt("RightItem");
                hash3 = zdo.GetInt("ChestItem");
                hash4 = zdo.GetInt("LegItem");
                hash5 = zdo.GetInt("HelmetItem");
                hash7 = zdo.GetInt("ShoulderItem");
                hash8 = zdo.GetInt("UtilityItem");
                hash6 = zdo.GetInt("BeardItem");
                num = zdo.GetInt("HairItem");
                leftItem = zdo.GetInt("LeftBackItem");
                rightItem = zdo.GetInt("RightBackItem");
                shoulderItemVariant = zdo.GetInt("ShoulderItemVariant");
                leftItemVariant = zdo.GetInt("LeftItemVariant");
                leftBackItemVariant = zdo.GetInt("LeftBackItemVariant");
            }
            else
            {
                if (!string.IsNullOrEmpty(__instance.m_leftItem))
                    hash1 = __instance.m_leftItem.GetStableHashCode();
                if (!string.IsNullOrEmpty(__instance.m_rightItem))
                    hash2 = __instance.m_rightItem.GetStableHashCode();
                if (!string.IsNullOrEmpty(__instance.m_chestItem))
                    hash3 = __instance.m_chestItem.GetStableHashCode();
                if (!string.IsNullOrEmpty(__instance.m_legItem))
                    hash4 = __instance.m_legItem.GetStableHashCode();
                if (!string.IsNullOrEmpty(__instance.m_helmetItem))
                    hash5 = __instance.m_helmetItem.GetStableHashCode();
                if (!string.IsNullOrEmpty(__instance.m_shoulderItem))
                    hash7 = __instance.m_shoulderItem.GetStableHashCode();
                if (!string.IsNullOrEmpty(__instance.m_utilityItem))
                    hash8 = __instance.m_utilityItem.GetStableHashCode();
                if (!string.IsNullOrEmpty(__instance.m_beardItem))
                    hash6 = __instance.m_beardItem.GetStableHashCode();
                if (!string.IsNullOrEmpty(__instance.m_hairItem))
                    num = __instance.m_hairItem.GetStableHashCode();
                if (!string.IsNullOrEmpty(__instance.m_leftBackItem))
                    leftItem = __instance.m_leftBackItem.GetStableHashCode();
                if (!string.IsNullOrEmpty(__instance.m_rightBackItem))
                    rightItem = __instance.m_rightBackItem.GetStableHashCode();
            }

            var flag1 = false;
            var flag2 = __instance.SetRightHandEquiped(hash2) | flag1;
            var flag3 = __instance.SetLeftHandEquiped(hash1, leftItemVariant) | flag2;
            var flag4 = __instance.SetChestEquiped(hash3) | flag3;
            var flag5 = __instance.SetLegEquiped(hash4) | flag4;
            var flag6 = __instance.SetHelmetEquiped(hash5, num) | flag5;
            var flag7 = __instance.SetShoulderEquiped(hash7, shoulderItemVariant) | flag6;
            var flag8 = __instance.SetUtilityEquiped(hash8) | flag7;
            if (__instance.m_helmetHideBeard)
                hash6 = 0;
            var flag9 = __instance.SetBeardEquiped(hash6) | flag8;
            var flag10 = __instance.SetBackEquiped(leftItem, rightItem, leftBackItemVariant) | flag9;
            if (__instance.m_helmetHideHair)
                num = 0;
            flag8 = __instance.SetHairEquiped(num) | flag10;

            if (!flag8)
                return;
            __instance.UpdateLodgroup();
        }
    }
}