using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using Jotunn;

namespace ChebsMercenaries.Minions
{
    internal class MercenaryWarriorTier3Minion : HumanMinion
    {
        public new static ConfigEntry<float> Health;
        public static MemoryConfigEntry<string, List<string>> ItemsCost;

        public new static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSynced = "MercenaryWarriorTier3Minion (Server Synced)";

            var itemsCost = plugin.ModConfig(serverSynced, "ItemsCost", "Coins:50",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount. Alternative items can be specified with a | eg. Wood|Coal:5 to mean wood and/or coal.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').Select(str => str.Trim()).ToList());
            Health = plugin.Config.Bind(serverSynced, "Health",
                200f, new ConfigDescription("How much health the mercenary has.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
        
        public override void AfterAwake()
        {
            ConfigureHealth();
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