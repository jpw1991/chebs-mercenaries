using ChebsMercenaries.Minions;
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

namespace ChebsMercenaries.Patches
{
    [HarmonyPatch(typeof(Tameable), "Interact")]
    class TameablePatch1
    {
        [HarmonyPrefix]
        static bool InteractPrefix(Humanoid user, bool hold, bool alt, Tameable __instance)
        {
            // Stop players that aren't the owner of a minion from interacting
            // with it. Also call HumanMinion wait/follow methods to
            // properly update the ZDO with the waiting position.
            if (__instance.TryGetComponent(out MercenaryMinion mercenaryMinion)
                && user.TryGetComponent(out Player player))
            {
                // if nobody owns the minion yet, claim ownership
                if (mercenaryMinion.UndeadMinionMaster == "") mercenaryMinion.UndeadMinionMaster = player.GetPlayerName();

                if (!mercenaryMinion.BelongsToPlayer(player.GetPlayerName()))
                {
                    user.Message(MessageHud.MessageType.Center, "$chebgonaz_mercenaries_notyourminion");
                    return false; // deny base method completion
                }

                if (!MercenaryMinion.Commandable.Value)
                {
                    return false; // deny base method completion
                }

                var currentStatus = mercenaryMinion.Status;
                var nextStatus = currentStatus switch
                {
                    ChebGonazMinion.State.Following => ChebGonazMinion.State.Waiting,
                    ChebGonazMinion.State.Waiting => ChebGonazMinion.State.Roaming,
                    _ => ChebGonazMinion.State.Following
                };

                // use the minion methods to ensure the ZDO is updated
                if (nextStatus.Equals(ChebGonazMinion.State.Following))
                {
                    user.Message(MessageHud.MessageType.Center, "$chebgonaz_mercenaries_humanfollowing");
                    mercenaryMinion.Follow(player.gameObject);
                    return false; // deny base method completion
                }

                if (nextStatus.Equals(ChebGonazMinion.State.Waiting))
                {
                    user.Message(MessageHud.MessageType.Center, "$chebgonaz_mercenaries_humanwaiting");
                    mercenaryMinion.Wait(player.transform.position);
                    return false; // deny base method completion
                }

                user.Message(MessageHud.MessageType.Center, "$chebgonaz_mercenaries_humanroaming");
                mercenaryMinion.Roam();
                return false; // deny base method completion
            }

            return true; // permit base method to complete
        }
    }

    [HarmonyPatch(typeof(Tameable))]
    class TameablePatch2
    {
        [HarmonyPatch(nameof(Tameable.GetHoverText))]
        [HarmonyPostfix]
        static void Postfix(Tameable __instance, ref string __result)
        {
            if (__instance.m_nview.IsValid()
                && __instance.m_commandable
                && __instance.TryGetComponent(out MercenaryMinion mercenaryMinion)
                && Player.m_localPlayer != null)
            {
                __result = mercenaryMinion.Status switch
                {
                    ChebGonazMinion.State.Following => Localization.instance.Localize("$chebgonaz_mercenaries_wait"),
                    ChebGonazMinion.State.Waiting => Localization.instance.Localize("$chebgonaz_mercenaries_roam"),
                    _ => Localization.instance.Localize("$chebgonaz_mercenaries_follow")
                };
            }
        }
    }

    [HarmonyPatch(typeof(Tameable))]
    class TameablePatch3
    {
        [HarmonyPatch(nameof(Tameable.GetHoverName))]
        [HarmonyPostfix]
        static void Postfix(Tameable __instance, ref string __result)
        {
            if (__instance.m_nview.IsValid()
                && __instance.TryGetComponent(out MercenaryMinion mercenaryMinion))
            {
                __result = $@"{Localization.instance.Localize("$chebgonaz_mercenaries_owner")}: {mercenaryMinion.UndeadMinionMaster} ({mercenaryMinion.Status switch {
                    ChebGonazMinion.State.Following => Localization.instance.Localize("$chebgonaz_minionstatus_following"),
                    ChebGonazMinion.State.Roaming => Localization.instance.Localize("$chebgonaz_minionstatus_roaming"),
                    _ => Localization.instance.Localize("$chebgonaz_minionstatus_waiting") }})";
            }
        }
    }
}