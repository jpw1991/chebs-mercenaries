using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using Jotunn;

namespace ChebsMercenaries.Minions
{
    internal class CatapultMinion : MercenaryMinion
    {
        public new static ConfigEntry<float> Health;
        public static MemoryConfigEntry<string, List<string>> ItemsCost;

        public new static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSynced = "CatapultMinion (Server Synced)";

            var itemsCost = plugin.ModConfig(serverSynced, "ItemsCost", "Wood:25,RoundLog:5,Bronze:1",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount. Alternative items can be specified with a | eg. Wood|Coal:5 to mean wood and/or coal.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').Select(str => str.Trim()).ToList());
            Health = plugin.Config.Bind(serverSynced, "Health",
                250f, new ConfigDescription("How much health the mercenary has.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
        
        public sealed override void Awake()
        {
            base.Awake();
            
            AfterAwake();
        }

        public virtual void AfterAwake()
        {
            ConfigureHealth();
        }

        protected virtual void ConfigureHealth()
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