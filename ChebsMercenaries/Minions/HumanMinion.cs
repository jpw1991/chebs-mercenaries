using BepInEx;
using BepInEx.Configuration;
using ChebsValheimLibrary.Minions;

namespace ChebsMercenaries.Minions
{
    public class HumanMinion : ChebGonazMinion
    {
        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate;
        public static ConfigEntry<bool> Commandable;
        public static ConfigEntry<float> FollowDistance, RunDistance;

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            DropOnDeath = plugin.Config.Bind("HumanMinion (Server Synced)", 
                "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PackDropItemsIntoCargoCrate = plugin.Config.Bind("HumanMinion (Server Synced)", 
                "PackDroppedItemsIntoCargoCrate",
                true, new ConfigDescription("If set to true, dropped items will be packed into a cargo crate. This means they won't sink in water, which is useful for more valuable drops like Surtling Cores and metal ingots.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            Commandable = plugin.Config.Bind("HumanMinion (Client)", "Commandable",
                true, new ConfigDescription("If true, minions can be commanded individually with E (or equivalent) keybind."));
            
            FollowDistance = plugin.Config.Bind("HumanMinion (Client)", "FollowDistance",
                3f, new ConfigDescription("How closely a minion will follow you (0 = standing on top of you, 3 = default)."));
            
            RunDistance = plugin.Config.Bind("HumanMinion (Client)", "RunDistance",
                3f, new ConfigDescription("How close a following minion needs to be to you before it stops running and starts walking (0 = always running, 10 = default)."));
        }
    }
}