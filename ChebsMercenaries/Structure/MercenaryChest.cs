using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using ChebsMercenaries.Minions;
using ChebsNecromancy.Minions;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsMercenaries.Structure
{
    internal class MercenaryChest : ChebsValheimLibrary.Structures.Structure
    {
        public static ConfigEntry<int> ContainerWidth, ContainerHeight;
        public static ConfigEntry<float> RecruitmentInterval;

        public static ConfigEntry<string> WarriorTier1Cost;
        public static ConfigEntry<string> WarriorTier2Cost;
        public static ConfigEntry<string> WarriorTier3Cost;
        public static ConfigEntry<string> WarriorTier4Cost;
        public static ConfigEntry<string> ArcherTier1Cost;
        public static ConfigEntry<string> ArcherTier2Cost;
        public static ConfigEntry<string> ArcherTier3Cost;
        public static ConfigEntry<string> MinerCost;
        public static ConfigEntry<string> WoodcutterCost;
        
        public static ConfigEntry<int> ArmorLeatherScrapsRequiredConfig;
        public static ConfigEntry<int> ArmorBronzeRequiredConfig;
        public static ConfigEntry<int> ArmorIronRequiredConfig;
        public static ConfigEntry<int> ArmorBlackIronRequiredConfig;

        public enum MercenaryType
        {
            None,
            [InternalName("CookedMeat:5,Club:1")] WarriorTier1,
            [InternalName("Coins:25")] WarriorTier2,
            [InternalName("Coins:50")] WarriorTier3,
            [InternalName("Coins:100")] WarriorTier4,
            [InternalName("CookedMeat:5,ArrowWood:20")] ArcherTier1,
            [InternalName("Coins:50,ArrowBronze:10")] ArcherTier2,
            [InternalName("Coins:100,ArrowIron:10")] ArcherTier3,
            [InternalName("HardAntler:1")] Miner,
            [InternalName("Flint:1")] Woodcutter,
        }


        private Container _container;
        private Inventory _inventory;

        private float _lastRecruitmentAt;

        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Wood:25,Coins:100",
            IconName = "chebgonaz_mercenarychest_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_mercenarychest_name",
            PieceDescription = "$chebgonaz_mercenarychest_desc",
            PrefabName = "ChebGonaz_MercenaryChest.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        private static Dictionary<MercenaryType, List<(GameObject, int)>> _mercCosts;
        private static Dictionary<MercenaryType, string> _prefabNames = new()
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
        };

        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }
        
        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "Allowed", true,
                "Whether making this is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BuildCosts",
                ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build. None or Blank will use Default settings. Format: " +
                ChebsRecipeConfig.RecipeValue,
                null, true);

            ContainerWidth = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ContainerWidth", 4,
                "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(2, 10), true);

            ContainerHeight = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ContainerHeight", 4,
                "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(4, 20), true);
            
            RecruitmentInterval = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RecruitmentInterval", 60f,
                "Every X seconds, attempt to recruit a mercenary", new AcceptableValueRange<int>(4, 20), true);
            
            WarriorTier1Cost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "WarriorTier1Cost", InternalName.GetName(MercenaryType.WarriorTier1),
                "The items that are consumed when creating the Warrior Tier 1 mercenary. Please use a comma-delimited list of prefab names.", null, true);

            WarriorTier2Cost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "WarriorTier2Cost", InternalName.GetName(MercenaryType.WarriorTier2),
                            "The items that are consumed when creating the Warrior Tier 2 mercenary. Please use a comma-delimited list of prefab names.", null, true);

            WarriorTier3Cost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "WarriorTier3Cost", InternalName.GetName(MercenaryType.WarriorTier3),
                            "The items that are consumed when creating the Warrior Tier 3 mercenary. Please use a comma-delimited list of prefab names.", null, true);

            WarriorTier4Cost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "WarriorTier4Cost", InternalName.GetName(MercenaryType.WarriorTier4),
                            "The items that are consumed when creating the Warrior Tier 4 mercenary. Please use a comma-delimited list of prefab names.", null, true);

            ArcherTier1Cost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArcherTier1Cost", InternalName.GetName(MercenaryType.ArcherTier1),
                            "The items that are consumed when creating the Archer Tier 1 mercenary. Please use a comma-delimited list of prefab names.", null, true);

            ArcherTier2Cost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArcherTier2Cost", InternalName.GetName(MercenaryType.ArcherTier2),
                            "The items that are consumed when creating the Archer Tier 2 mercenary. Please use a comma-delimited list of prefab names.", null, true);

            ArcherTier3Cost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArcherTier3Cost", InternalName.GetName(MercenaryType.ArcherTier3),
                            "The items that are consumed when creating the Archer Tier 3 mercenary. Please use a comma-delimited list of prefab names.", null, true);

            MinerCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "MinerCost", InternalName.GetName(MercenaryType.Miner),
                            "The items that are consumed when creating the Miner mercenary. Please use a comma-delimited list of prefab names.", null, true);

            WoodcutterCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "WoodcutterCost", InternalName.GetName(MercenaryType.Woodcutter),
                            "The items that are consumed when creating the Woodcutter mercenary. Please use a comma-delimited list of prefab names.", null, true);
            
            ArmorLeatherScrapsRequiredConfig = plugin.Config.Bind("General (Server Synced)", "ArmorLeatherScrapsRequired",
                2, new ConfigDescription("The amount of LeatherScraps required to craft a minion in leather armor.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorBronzeRequiredConfig = plugin.Config.Bind("General (Server Synced)", "ArmorBronzeRequired",
                1, new ConfigDescription("The amount of Bronze required to craft a minion in bronze armor.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorIronRequiredConfig = plugin.Config.Bind("General (Server Synced)", "ArmoredIronRequired",
                1, new ConfigDescription("The amount of Iron required to craft a minion in iron armor.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorBlackIronRequiredConfig = plugin.Config.Bind("General (Server Synced)", "ArmorBlackIronRequired",
                1, new ConfigDescription("The amount of Black Metal required to craft a minion in black iron armor.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public static void ParseMercCosts()
        {
            bool ValidRequirement(string nameValue, string amountValue,
                out GameObject prefab, out int amount)
            {
                prefab = ZNetScene.instance.GetPrefab(nameValue);
                return int.TryParse(amountValue, out amount) && prefab != null;
            }

            var enumAndConfigs = new List<(MercenaryType, ConfigEntry<string>)>
            {
                (MercenaryType.WarriorTier1, WarriorTier1Cost),
                (MercenaryType.WarriorTier2, WarriorTier2Cost),
                (MercenaryType.WarriorTier3, WarriorTier3Cost),
                (MercenaryType.WarriorTier4, WarriorTier4Cost),
                (MercenaryType.ArcherTier1, ArcherTier1Cost),
                (MercenaryType.ArcherTier2, ArcherTier2Cost),
                (MercenaryType.ArcherTier3, ArcherTier3Cost),
                (MercenaryType.Miner, MinerCost),
                (MercenaryType.Woodcutter, WoodcutterCost)
            };
            
            _mercCosts = new Dictionary<MercenaryType, List<(GameObject, int)>>();
            
            foreach (var enumAndConfig in enumAndConfigs)
            {
                var enumValue = enumAndConfig.Item1;
                var configValue = enumAndConfig.Item2;
                
                // splut = ["Coins:10", "Club:1"]
                var configValueSplut = configValue.Value.Split(',');
                _mercCosts[enumValue] = new List<(GameObject, int)>();
                foreach (var splut in configValueSplut)
                {
                    // splat = ["Coins", "10"]
                    var splat = splut.Split(':');
                    if (splat.Length != 2)
                    {
                        Logger.LogError($"{enumValue} costs invalid - please review config values. Reverting to default value.");
                        splat = new[] { "CookedMeat:5", "Club:1" };
                    }
                
                    if (ValidRequirement(splat[0], splat[1], out GameObject splatPrefab, out int splatAmount))
                    {
                        _mercCosts[enumValue].Add((splatPrefab, splatAmount));
                    }
                    else
                    {
                        Logger.LogError($"{enumValue} costs invalid - please review config values. Failed to set it up at all.");
                    }
                }
            }
        }
        
        private void Awake()
        {
            _container = GetComponent<Container>();
            _inventory = _container.GetInventory();

            _container.m_width = ContainerWidth.Value;
            _container.m_height = ContainerHeight.Value;

            StartCoroutine(Recruitment());
        }

        private MercenaryType NextMercenary()
        {
            var mercenaryType = MercenaryType.None;

            var orderedByPreference = new List<MercenaryType>
            {
                MercenaryType.WarriorTier4,
                MercenaryType.ArcherTier3,
                MercenaryType.WarriorTier3,
                MercenaryType.ArcherTier2,
                MercenaryType.WarriorTier2,
                MercenaryType.ArcherTier1,
                MercenaryType.WarriorTier1,
                MercenaryType.Woodcutter,
                MercenaryType.Miner
            };

            foreach (var merc in orderedByPreference)
            {
                var costs = _mercCosts[merc];
                foreach (var cost in costs)
                {
                    var prefab = cost.Item1;
                    var amount = cost.Item2;
                    if (AmountInInventory(prefab) >= amount)
                    {
                        mercenaryType = merc;
                        return mercenaryType;
                    }
                }
            }

            return mercenaryType;
        }

        private void PayForMercenary(MercenaryType mercenaryType)
        {
            var costs = _mercCosts[mercenaryType];
            foreach (var cost in costs)
            {
                _inventory.RemoveItem(cost.Item1.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name, cost.Item2);   
            }
        }

        private ChebGonazMinion.ArmorType UpgradeMercenaryEquipment()
        {
            var armorType = ChebGonazMinion.DetermineArmorType(
                _inventory,
                ArmorBlackIronRequiredConfig.Value,
                ArmorIronRequiredConfig.Value,
                ArmorBronzeRequiredConfig.Value,
                ArmorLeatherScrapsRequiredConfig.Value);

            switch (armorType)
            {
                case ChebGonazMinion.ArmorType.BlackMetal:
                    _inventory.RemoveItem("$item_blackmetal", ArmorBlackIronRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Iron:
                    _inventory.RemoveItem("$item_iron", ArmorBlackIronRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Bronze:
                    _inventory.RemoveItem("$item_bronze", ArmorBlackIronRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Leather:
                    var leatherItemTypes = new List<string>()
                    {
                        "$item_leatherscraps",
                        "$item_deerhide",
                        "$item_scalehide"
                    };
                    
                    foreach (var leatherItem in leatherItemTypes)
                    {
                        var leatherItemsInInventory = _inventory.CountItems(leatherItem);
                        if (leatherItemsInInventory >= ArmorLeatherScrapsRequiredConfig.Value)
                        {
                            _inventory.RemoveItem(leatherItem, ArmorLeatherScrapsRequiredConfig.Value);
                            break;
                        }
                    }
                    break;
                case ChebGonazMinion.ArmorType.LeatherLox:
                    _inventory.RemoveItem("$item_loxpelt", ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.LeatherTroll:
                    _inventory.RemoveItem("$item_wolfpelt", ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.LeatherWolf:
                    _inventory.RemoveItem("$item_trollhide", ArmorLeatherScrapsRequiredConfig.Value);
                    break;

            }
            
            return armorType;
        }

        private void SpawnMercenary(MercenaryType mercenaryType, ChebGonazMinion.ArmorType armorType)
        {
            if (mercenaryType is MercenaryType.None) return;

            var prefabName = _prefabNames[mercenaryType];
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"SpawnMercenary: spawning {prefabName} failed");
                return;
            }
            var spawnedChar = Instantiate(prefab,
                transform.position + transform.forward * 2f + Vector3.up, Quaternion.identity);
            spawnedChar.AddComponent<FreshMinion>();

            var minion = mercenaryType switch
            {
                MercenaryType.Miner => spawnedChar.AddComponent<HumanMinerMinion>(),
                MercenaryType.Woodcutter => spawnedChar.AddComponent<HumanWoodcutterMinion>(),
                _ => spawnedChar.AddComponent<HumanMinion>()
            };
            
            if (mercenaryType != MercenaryType.Miner && mercenaryType != MercenaryType.Woodcutter)
                minion.Roam();

            var piece = GetComponent<Piece>();
            var playerName = Player.GetAllPlayers().Find(p => p.m_nview.m_zdo.m_owner == piece.m_creator)
                .GetPlayerName();
            Logger.LogInfo($"MercenaryChest's creator = {piece.m_creator} which corresponds to {playerName}");
            minion.UndeadMinionMaster = playerName;
            
            // handle refunding of resources on death
            if (HumanMinion.DropOnDeath.Value == ChebGonazMinion.DropType.Nothing) return;
            
            // we have to be a little bit cautious. It normally shouldn't exist yet, but maybe some other mod
            // added it? Who knows
            var characterDrop = minion.gameObject.GetComponent<CharacterDrop>();
            if (characterDrop == null)
            {
                characterDrop = minion.gameObject.AddComponent<CharacterDrop>();
            }

            if (HumanMinion.DropOnDeath.Value == ChebGonazMinion.DropType.Everything)
            {
                var costs = _mercCosts[mercenaryType];
                foreach (var cost in costs)
                {
                    var costPrefab = cost.Item1;
                    var costAmount = cost.Item2;
                    
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, costPrefab.name, costAmount);
                }
            }

            switch (armorType)
            {
                case ChebGonazMinion.ArmorType.Leather:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, 
                        Random.value > .5f ? "DeerHide" : "LeatherScraps", // flip a coin for deer or scraps
                        ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.LeatherTroll:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "TrollHide", ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.LeatherWolf:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "WolfPelt", ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.LeatherLox:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "LoxPelt", ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Bronze:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "Bronze", ArmorBronzeRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Iron:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "Iron", ArmorIronRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.BlackMetal:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "BlackMetal", ArmorBlackIronRequiredConfig.Value);
                    break;
            }

            // the component won't be remembered by the game on logout because
            // only what is on the prefab is remembered. Even changes to the prefab
            // aren't remembered. So we must write what we're dropping into
            // the ZDO as well and then read & restore this on Awake
            minion.RecordDrops(characterDrop);
        }

        IEnumerator Recruitment()
        {
            yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            while (true)
            {
                yield return new WaitWhile(() => Player.m_localPlayer != null && Player.m_localPlayer.m_sleeping);
                
                yield return new WaitForSeconds(5);
                
                var nextMerc = NextMercenary();
                
                var player = Player.m_localPlayer;
                if (Vector3.Distance(player.transform.position, transform.position) < 5)
                {
                    Chat.instance.SetNpcText(gameObject, Vector3.up, 5f, 2f, "", 
                        $"Recruiting {nextMerc} in {(RecruitmentInterval.Value - (Time.time - _lastRecruitmentAt)).ToString("0.##")}%)...", false);
                }

                if (nextMerc != MercenaryType.None)
                {
                    PayForMercenary(nextMerc);
                    SpawnMercenary(nextMerc, UpgradeMercenaryEquipment());
                }
            }
        }

        private int AmountInInventory(GameObject prefab)
        {
            return _inventory.CountItems(prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name);
        }
    }
}