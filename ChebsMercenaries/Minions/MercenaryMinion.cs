using BepInEx.Configuration;
using ChebsValheimLibrary.Minions;

namespace ChebsMercenaries.Minions
{
    public class MercenaryMinion : ChebGonazMinion
    {
        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate;
        public static ConfigEntry<bool> Commandable;
        public static ConfigEntry<float> FollowDistance, RunDistance, RoamRange;
        
        public static ConfigEntry<float> Health;
        
        public enum MercenaryType
        {
            None,
            WarriorTier1,
            WarriorTier2,
            WarriorTier3,
            WarriorTier4,
            ArcherTier1,
            ArcherTier2,
            ArcherTier3,
            Miner,
            Woodcutter,
            Catapult,
        }

        public static readonly Dictionary<MercenaryType, string> PrefabNames = new()
        {
            { MercenaryType.WarriorTier1, "ChebGonaz_HumanWarrior" },
            { MercenaryType.WarriorTier2, "ChebGonaz_HumanWarriorTier2" },
            { MercenaryType.WarriorTier3, "ChebGonaz_HumanWarriorTier3" },
            { MercenaryType.WarriorTier4, "ChebGonaz_HumanWarriorTier4" },
            { MercenaryType.ArcherTier1, "ChebGonaz_HumanArcher" },
            { MercenaryType.ArcherTier2, "ChebGonaz_HumanArcherTier2" },
            { MercenaryType.ArcherTier3, "ChebGonaz_HumanArcherTier3" },
            { MercenaryType.Miner, "ChebGonaz_HumanMiner" },
            { MercenaryType.Woodcutter, "ChebGonaz_HumanWoodcutter" },
            { MercenaryType.Catapult, "ChebGonaz_Catapult" },
        };

        public static readonly Dictionary<MercenaryType, string> PrefabNamesFemale = new()
        {
            { MercenaryType.WarriorTier1, "ChebGonaz_HumanWarriorFemale" },
            { MercenaryType.WarriorTier2, "ChebGonaz_HumanWarriorTier2Female" },
            { MercenaryType.WarriorTier3, "ChebGonaz_HumanWarriorTier3Female" },
            { MercenaryType.WarriorTier4, "ChebGonaz_HumanWarriorTier4Female" },
            { MercenaryType.ArcherTier1, "ChebGonaz_HumanArcherFemale" },
            { MercenaryType.ArcherTier2, "ChebGonaz_HumanArcherTier2Female" },
            { MercenaryType.ArcherTier3, "ChebGonaz_HumanArcherTier3Female" },
            { MercenaryType.Miner, "ChebGonaz_HumanMinerFemale" },
            { MercenaryType.Woodcutter, "ChebGonaz_HumanWoodcutterFemale" },
            { MercenaryType.Catapult, "ChebGonaz_Catapult" },
        };


        public static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSync = "MercenaryMinion (Server Synced)";
            const string client = "MercenaryMinion (Client)";
            DropOnDeath = plugin.Config.Bind(serverSync,
                "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PackDropItemsIntoCargoCrate = plugin.Config.Bind(serverSync,
                "PackDroppedItemsIntoCargoCrate",
                true, new ConfigDescription(
                    "If set to true, dropped items will be packed into a cargo crate. This means they won't sink in water, which is useful for more valuable drops like Surtling Cores and metal ingots.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            Commandable = plugin.Config.Bind(client, "Commandable",
                true,
                new ConfigDescription(
                    "If true, minions can be commanded individually with E (or equivalent) keybind."));

            FollowDistance = plugin.Config.Bind(client, "FollowDistance",
                3f,
                new ConfigDescription(
                    "How closely a minion will follow you (0 = standing on top of you, 3 = default)."));

            RunDistance = plugin.Config.Bind(client, "RunDistance",
                3f,
                new ConfigDescription(
                    "How close a following minion needs to be to you before it stops running and starts walking (0 = always running, 10 = default)."));
            
            RoamRange = plugin.Config.Bind(client, "RoamRange",
                10f, new ConfigDescription("How far a unit is allowed to roam from its current position."));
            
            Health = plugin.Config.Bind(serverSync, "Health",
                50f, new ConfigDescription("How much health the mercenary has (default fallback value).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
    }
}