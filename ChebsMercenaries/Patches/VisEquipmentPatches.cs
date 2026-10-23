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
            var trinket = 0;
            var leftItem = 0;
            var rightItem = 0;
            var shoulderItemVariant = __instance.m_shoulderItemVariant;
            var leftItemVariant = __instance.m_leftItemVariant;
            var leftBackItemVariant = __instance.m_leftBackItemVariant;
            var rightQuality = __instance.m_rightItemQuality;
            var rightBackQuality = __instance.m_rightBackItemQuality;
            var shoulderQuality = __instance.m_shoulderItemQuality;
            var leftQuality = __instance.m_leftItemQuality;
            var leftBackQuality = __instance.m_leftBackItemQuality;
            var zdo = __instance.m_nview.GetZDO();
            if (zdo != null)
            {
                hash1 = zdo.GetInt(ZDOVars.s_leftItem);
                hash2 = zdo.GetInt(ZDOVars.s_rightItem);
                hash3 = zdo.GetInt(ZDOVars.s_chestItem);
                hash4 = zdo.GetInt(ZDOVars.s_legItem);
                hash5 = zdo.GetInt(ZDOVars.s_helmetItem);
                hash7 = zdo.GetInt(ZDOVars.s_shoulderItem);
                hash8 = zdo.GetInt(ZDOVars.s_utilityItem);
                trinket = zdo.GetInt(ZDOVars.s_trinketItem);
                hash6 = zdo.GetInt(ZDOVars.s_beardItem);
                num = zdo.GetInt(ZDOVars.s_hairItem);
                leftItem = zdo.GetInt(ZDOVars.s_leftBackItem);
                rightItem = zdo.GetInt(ZDOVars.s_rightBackItem);
                shoulderItemVariant = zdo.GetInt(ZDOVars.s_shoulderItemVariant);
                leftItemVariant = zdo.GetInt(ZDOVars.s_leftItemVariant);
                leftBackItemVariant = zdo.GetInt(ZDOVars.s_leftBackItemVariant);
                rightQuality = zdo.GetInt(ZDOVars.s_rightItemQuality);
                rightBackQuality = zdo.GetInt(ZDOVars.s_rightBackItemQuality);
                shoulderQuality = zdo.GetInt(ZDOVars.s_shoulderItemQuality);
                leftQuality = zdo.GetInt(ZDOVars.s_leftItemQuality);
                leftBackQuality = zdo.GetInt(ZDOVars.s_leftBackItemQuality);
            }
            else
            {
                hash1 = __instance.m_leftItem;
                hash2 = __instance.m_rightItem;
                hash3 = __instance.m_chestItem;
                hash4 = __instance.m_legItem;
                hash5 = __instance.m_helmetItem;
                hash7 = __instance.m_shoulderItem;
                hash8 = __instance.m_utilityItem;
                if (!string.IsNullOrEmpty(__instance.m_trinketItem))
                    trinket = __instance.m_trinketItem.GetStableHashCode();
                hash6 = __instance.m_beardItem;
                num = __instance.m_hairItem;
                leftItem = __instance.m_leftBackItem;
                rightItem = __instance.m_rightBackItem;
            }

            var flag = __instance.SetRightHandEquipped(hash2, rightQuality);
            flag = __instance.SetLeftHandEquipped(hash1, leftItemVariant, leftQuality) | flag;
            flag = __instance.SetChestEquipped(hash3) | flag;
            flag = __instance.SetLegEquipped(hash4) | flag;
            flag = __instance.SetHelmetEquipped(hash5, num) | flag;
            flag = __instance.SetShoulderEquipped(hash7, shoulderItemVariant, shoulderQuality) | flag;
            flag = __instance.SetUtilityEquipped(hash8) | flag;
            flag = __instance.SetTrinketEquipped(trinket) | flag;
            hash6 = __instance.GetHairItem(__instance.m_helmetHideBeard, hash6, ItemDrop.ItemData.AccessoryType.Beard);
            flag = __instance.SetBeardEquipped(hash6) | flag;
            num = __instance.GetHairItem(__instance.m_helmetHideHair, num, ItemDrop.ItemData.AccessoryType.Hair);
            flag = __instance.SetHairEquipped(num) | flag;
            flag = __instance.SetBackEquipped(leftItem, rightItem, leftBackItemVariant, leftBackQuality, rightBackQuality) | flag;

            if (!flag)
                return;
            __instance.UpdateLodgroup();
        }
    }
}
