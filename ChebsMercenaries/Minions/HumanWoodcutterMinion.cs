using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions.AI;

namespace ChebsMercenaries.Minions
{
    public class HumanWoodcutterMinion : HumanMinion
    {
        public static ConfigEntry<float> UpdateDelay, LookRadius;
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

            SyncInternalsWithConfigs();
        }

        public static void SyncInternalsWithConfigs()
        {
            // awful stuff. Is there a better way?
            WoodcutterAI.UpdateDelay = UpdateDelay.Value;
            WoodcutterAI.LookRadius = LookRadius.Value;
            WoodcutterAI.RoamRange = RoamRange.Value;
        }

        public override void AfterAwake()
        {
            ConfigureHealth();
            canBeCommanded = false;
            if (!TryGetComponent(out WoodcutterAI _)) gameObject.AddComponent<WoodcutterAI>();
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
                Jotunn.Logger.LogError("Error: Failed to get Humanoid component to set health value.");
            }
        }
    }
}