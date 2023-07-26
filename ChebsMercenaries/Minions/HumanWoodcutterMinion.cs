using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsMercenaries.Minions.WorkerAI;
using ChebsValheimLibrary.Common;
using Jotunn;

namespace ChebsMercenaries.Minions
{
    public class HumanWoodcutterMinion : HumanMinion
    {
        public static ConfigEntry<float> UpdateDelay, LookRadius, ToolDamage, ChatInterval, ChatDistance;
        public static ConfigEntry<short> ToolTier;
        public new static ConfigEntry<float> RoamRange, Health;
        public static MemoryConfigEntry<string, List<string>> ItemsCost;

        public new static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSynced = "HumanWoodcutter (Server Synced)";
            UpdateDelay = plugin.Config.Bind(serverSynced, "UpdateDelay",
                6f, new ConfigDescription(
                    "The delay, in seconds, between wood searching attempts. Attention: small values may impact performance.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            LookRadius = plugin.Config.Bind(serverSynced, "LookRadius",
                50f, new ConfigDescription("How far it can see wood. High values may damage performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            RoamRange = plugin.Config.Bind(serverSynced, "RoamRange",
                50f, new ConfigDescription("How far it will randomly run to in search of wood.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            var itemsCost = plugin.ModConfig(serverSynced, "ItemsCost", "CookedMeat|Coins:5,Flint:1",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount. Alternative items can be specified with a | eg. Wood|Coal:5 to mean wood and/or coal.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').ToList());
            Health = plugin.Config.Bind(serverSynced, "Health",
                50f, new ConfigDescription("How much health the mercenary has.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            ToolDamage = plugin.Config.Bind(serverSynced, "ToolDamage", 6f,
                new ConfigDescription("Damage dealt by the worker's tool to stuff it's harvesting.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            ToolTier = plugin.Config.Bind(serverSynced, "ToolTier", (short)2,
                new ConfigDescription("Worker's tool tier (determines what stuff it can mine/harvest).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            ChatInterval = plugin.Config.Bind(serverSynced, "ChatInterval", 6f,
                new ConfigDescription("The delay, in seconds, between worker updates. Set to 0 for no chatting.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            ChatDistance = plugin.Config.Bind(serverSynced, "ChatDistance", 6f,
                new ConfigDescription("How close a player must be for the worker to speak.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void AfterAwake()
        {
            ConfigureHealth();
            canBeCommanded = false;
            if (!TryGetComponent(out HumanWoodcutterAI _)) gameObject.AddComponent<HumanWoodcutterAI>();
        }
        
        protected override void ConfigureHealth()
        {
            if (TryGetComponent(out Humanoid humanoid))
            {
                humanoid.SetMaxHealth(Health.Value);
                humanoid.SetHealth(Health.Value);
            }
            else
            {
                Logger.LogError("Error: Failed to get Humanoid component to set health value.");
            }
        }
    }
}