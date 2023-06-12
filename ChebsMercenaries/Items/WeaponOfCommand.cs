using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ChebsValheimLibrary.Items;
using ChebsValheimLibrary.Minions;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace ChebsMercenaries.Items
{
    internal class WeaponOfCommand : Item
    {
        protected ConfigEntry<KeyCode> FollowConfig;
        protected ConfigEntry<InputManager.GamepadButton> FollowGamepadConfig;
        protected ButtonConfig FollowButton;

        protected ConfigEntry<KeyCode> WaitConfig;
        protected ConfigEntry<InputManager.GamepadButton> WaitGamepadConfig;
        protected ButtonConfig WaitButton;

        protected ConfigEntry<KeyCode> TeleportConfig;
        protected ConfigEntry<InputManager.GamepadButton> TeleportGamepadConfig;
        protected ButtonConfig TeleportButton;

        // ReSharper disable once MemberCanBePrivate.Global
        protected ConfigEntry<float> TeleportDurabilityCost;

        // ReSharper disable once MemberCanBePrivate.Global
        protected ConfigEntry<float> TeleportCooldown;

        // ReSharper disable once MemberCanBePrivate.Global
        protected static float lastTeleport;

        protected ConfigEntry<float> CommandRange;

        protected ConfigEntry<KeyCode> ShiftConfig;
        protected ButtonConfig ShiftButton;
        protected bool ShiftPressed = false;

        // ReSharper disable once MemberCanBePrivate.Global
        protected bool CanTeleport =>
            TeleportCooldown.Value == 0f || Time.time - lastTeleport > TeleportCooldown.Value;

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            const string client = "WeaponOfCommand (Client)";
            const string server = "WeaponOfCommand (Server Sync)";
            FollowConfig = plugin.Config.Bind(client, ItemName + "Follow",
                KeyCode.F, new ConfigDescription("The key to tell minions to follow."));

            FollowGamepadConfig = plugin.Config.Bind(client, ItemName + "FollowGamepad",
                InputManager.GamepadButton.ButtonWest,
                new ConfigDescription("The gamepad button to tell minions to follow."));

            WaitConfig = plugin.Config.Bind(client, ItemName + "Wait",
                KeyCode.T, new ConfigDescription("The key to tell minions to wait."));

            WaitGamepadConfig = plugin.Config.Bind(client, ItemName + "WaitGamepad",
                InputManager.GamepadButton.ButtonEast,
                new ConfigDescription("The gamepad button to tell minions to wait."));

            TeleportConfig = plugin.Config.Bind(client, ItemName + "Teleport",
                KeyCode.G, new ConfigDescription("The key to teleport following minions to you."));

            TeleportGamepadConfig = plugin.Config.Bind(client, ItemName + "TeleportGamepad",
                InputManager.GamepadButton.SelectButton,
                new ConfigDescription("The gamepad button to teleport following minions to you."));

            TeleportCooldown = plugin.Config.Bind(server,
                ItemName + "TeleportCooldown",
                5f, new ConfigDescription(
                    "How long a player must wait before being able to teleport minions again (0 for no cooldown).",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            TeleportDurabilityCost = plugin.Config.Bind(server,
                ItemName + "TeleportDurabilityCost",
                0f, new ConfigDescription(
                    "How much damage a wand receives from being used to teleport minions with (0 for no damage).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CommandRange = plugin.Config.Bind(server,
                ItemName + "CommandRange",
                10f, new ConfigDescription("How far minions will hear the commands.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ShiftConfig = plugin.Config.Bind(client, ItemName + "Shift",
                KeyCode.LeftShift,
                new ConfigDescription("The key to permit alternate actions such as Shift+T for Roam."));
        }

        public virtual void CreateButtons()
        {
            if (FollowConfig.Value != KeyCode.None)
            {
                FollowButton = new ButtonConfig
                {
                    Name = ItemName + "Follow",
                    Config = FollowConfig,
                    GamepadConfig = FollowGamepadConfig,
                    HintToken = "$chebsmercenaries_weaponofcommand_follow",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, FollowButton);
            }

            if (WaitConfig.Value != KeyCode.None)
            {
                WaitButton = new ButtonConfig
                {
                    Name = ItemName + "Wait",
                    Config = WaitConfig,
                    GamepadConfig = WaitGamepadConfig,
                    HintToken = "$chebsmercenaries_weaponofcommand_wait",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, WaitButton);
            }

            if (TeleportConfig.Value != KeyCode.None)
            {
                TeleportButton = new ButtonConfig
                {
                    Name = ItemName + "Teleport",
                    Config = TeleportConfig,
                    GamepadConfig = TeleportGamepadConfig,
                    HintToken = "$chebsmercenaries_weaponofcommand_teleport",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, TeleportButton);
            }

            if (ShiftConfig.Value != KeyCode.None)
            {
                ShiftButton = new ButtonConfig
                {
                    Name = ItemName + "Shift",
                    Config = ShiftConfig,
                    //GamepadConfig = AttackTargetGamepadConfig,
                    HintToken = "$chebsmercenaries_weaponofcommand_shift",
                    BlockOtherInputs = false
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, ShiftButton);
            }
        }

        public virtual KeyHintConfig GetKeyHint()
        {
            var buttonConfigs = new List<ButtonConfig>();

            if (FollowButton != null) buttonConfigs.Add(FollowButton);
            if (WaitButton != null) buttonConfigs.Add(WaitButton);
            if (TeleportButton != null) buttonConfigs.Add(TeleportButton);

            return new KeyHintConfig
            {
                Item = ItemName,
                ButtonConfigs = buttonConfigs.ToArray()
            };
        }

        public virtual void AddInputs()
        {
        }

        public virtual bool HandleInputs()
        {
            if (MessageHud.instance == null
                || Player.m_localPlayer == null
                || Player.m_localPlayer.GetInventory().GetEquippedItems().Find(
                    equippedItem => equippedItem.TokenName().Equals(NameLocalization)
                ) == null) return false;

            ShiftPressed = ZInput.GetButton(ShiftButton.Name);

            // handle visual side of keyhints
            // if (TeleportButton != null && TeleportCooldown.Value > 0)
            // {
            //     TeleportButton.HintToken = Time.time - lastTeleport < TeleportCooldown.Value ? "Cooldown" : "$friendlyskeletonwand_teleport";
            // }

            if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
            {
                MakeNearbyMinionsFollow(CommandRange.Value, true);
                return true;
            }

            if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
            {
                if (ShiftPressed)
                {
                    MakeNearbyMinionsRoam(CommandRange.Value);
                }
                else
                {
                    MakeNearbyMinionsFollow(CommandRange.Value, false);
                }

                return true;
            }

            if (TeleportButton != null && ZInput.GetButton(TeleportButton.Name))
            {
                TeleportFollowingMinionsToPlayer();
                return true;
            }

            return false;
        }

        public void MakeNearbyMinionsRoam(float radius)
        {
            var player = Player.m_localPlayer;
            var allCharacters = new List<Character>();
            Character.GetCharactersInRange(player.transform.position, radius, allCharacters);
            foreach (var character in allCharacters)
            {
                if (character.IsDead()) continue;

                var minion = character.GetComponent<ChebGonazMinion>();
                if (minion == null || !minion.canBeCommanded
                                   || !minion.BelongsToPlayer(player.GetPlayerName())) continue;

                if (!character.IsOwner())
                {
                    character.m_nview.ClaimOwnership();
                }

                if (character.GetComponent<MonsterAI>().GetFollowTarget() != player.gameObject) continue;

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$chebgonaz_mercenaries_humanroaming");
                minion.Roam();
            }
        }

        public void MakeNearbyMinionsFollow(float radius, bool follow)
        {
            var player = Player.m_localPlayer;
            // based off BaseAI.FindClosestCreature
            var allCharacters = Character.GetAllCharacters();
            foreach (var character in allCharacters)
            {
                if (character.IsDead())
                {
                    continue;
                }

                var minion = character.GetComponent<ChebGonazMinion>();
                if (minion == null || !minion.canBeCommanded
                                   || !minion.BelongsToPlayer(player.GetPlayerName())) continue;

                if (!character.IsOwner())
                {
                    character.m_nview.ClaimOwnership();
                }

                var distance = Vector3.Distance(character.transform.position, player.transform.position);
                if (distance > radius) continue;

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    follow
                        ? "chebgonaz_mercenaries_humanfollowing"
                        : "chebgonaz_mercenaries_humanwaiting");
                if (follow)
                {
                    minion.Follow(player.gameObject);
                    continue;
                }

                var minionFollowTarget = character.GetComponent<MonsterAI>().GetFollowTarget();
                if (minionFollowTarget == player.gameObject)
                {
                    minion.Wait(player.transform.position);
                }
            }
        }

        public void TeleportFollowingMinionsToPlayer()
        {
            if (!CanTeleport) return;
            var player = Player.m_localPlayer;
            var rightItem = player.GetRightItem();
            if (TeleportDurabilityCost.Value > 0 && rightItem != null)
            {
                rightItem.m_durability -= TeleportDurabilityCost.Value;
            }

            lastTeleport = Time.time;

            // based off BaseAI.FindClosestCreature
            var allCharacters = Character.GetAllCharacters();
            foreach (var character in allCharacters)
            {
                if (character.IsDead())
                {
                    continue;
                }

                if (character.GetComponent<ChebGonazMinion>() != null
                    && character.TryGetComponent(out MonsterAI monsterAI)
                    && monsterAI.GetFollowTarget() == player.gameObject)
                {
                    if (!character.IsOwner())
                    {
                        character.m_nview.ClaimOwnership();
                    }

                    character.transform.position = player.transform.position;
                    // forget position of current enemy so they don't start chasing after it. Cannot set it to null
                    // via monsterAI.SetTarget(null) because this has no effect. Code below inspired by reading
                    // MonsterAI.UpdateTarget
                    monsterAI.SetAlerted(false);
                    monsterAI.m_targetCreature = null;
                    monsterAI.m_targetStatic = null;
                    monsterAI.m_timeSinceAttacking = 0.0f;
                }
            }
        }
    }
}