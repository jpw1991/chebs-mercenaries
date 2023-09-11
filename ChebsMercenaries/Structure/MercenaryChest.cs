using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using ChebsMercenaries.Minions;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using UnityEngine;

namespace ChebsMercenaries.Structure
{
    public class MercenaryChest : ChebsValheimLibrary.Structures.Structure
    {
        // Cannot set custom width/height due to https://github.com/jpw1991/chebs-necromancy/issues/100
        //public static ConfigEntry<int> ContainerWidth, ContainerHeight;
        public static ConfigEntry<float> RecruitmentInterval;

        public static ConfigEntry<int> ArmorLeatherScrapsRequiredConfig;
        public static ConfigEntry<int> ArmorBronzeRequiredConfig;
        public static ConfigEntry<int> ArmorIronRequiredConfig;
        public static ConfigEntry<int> ArmorBlackIronRequiredConfig;

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

        private List<HumanMinion.MercenaryType> _orderedByPreference = new()
        {
            HumanMinion.MercenaryType.Woodcutter,
            HumanMinion.MercenaryType.Miner,
            HumanMinion.MercenaryType.ArcherTier3,
            HumanMinion.MercenaryType.ArcherTier2,
            HumanMinion.MercenaryType.ArcherTier1,
            HumanMinion.MercenaryType.WarriorTier4,
            HumanMinion.MercenaryType.WarriorTier3,
            HumanMinion.MercenaryType.WarriorTier2,
            HumanMinion.MercenaryType.WarriorTier1,
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

            // Cannot set custom width/height due to https://github.com/jpw1991/chebs-necromancy/issues/100
            // ContainerWidth = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ContainerWidth", 4,
            //     "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(2, 10), true);
            //
            // ContainerHeight = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ContainerHeight", 4,
            //     "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(4, 20), true);

            RecruitmentInterval = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RecruitmentInterval", 30f,
                "Every X seconds, attempt to recruit a mercenary", null, true);

            ArmorLeatherScrapsRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName,
                "ArmorLeatherScrapsRequired", 2,
                "The amount of LeatherScraps required to craft a minion in leather armor.", null, true);

            ArmorBronzeRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArmorBronzeRequired",
                1, "The amount of Bronze required to craft a minion in bronze armor.", null, true);

            ArmorIronRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArmoredIronRequired",
                1, "The amount of Iron required to craft a minion in iron armor.", null,
                true);

            ArmorBlackIronRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArmorBlackIronRequired",
                1, "The amount of Black Metal required to craft a minion in black iron armor.", null,
                true);
        }

        private void Awake()
        {
            StartCoroutine(Recruitment());
        }

        private HumanMinion.MercenaryType NextMercenary()
        {
            foreach (var merc in _orderedByPreference)
            {
                var itemsCost = merc switch
                {
                    HumanMinion.MercenaryType.WarriorTier1 => MercenaryWarriorTier1Minion.ItemsCost,
                    HumanMinion.MercenaryType.WarriorTier2 => MercenaryWarriorTier2Minion.ItemsCost,
                    HumanMinion.MercenaryType.WarriorTier3 => MercenaryWarriorTier3Minion.ItemsCost,
                    HumanMinion.MercenaryType.WarriorTier4 => MercenaryWarriorTier4Minion.ItemsCost,
                    HumanMinion.MercenaryType.ArcherTier1 => MercenaryArcherTier1Minion.ItemsCost,
                    HumanMinion.MercenaryType.ArcherTier2 => MercenaryArcherTier2Minion.ItemsCost,
                    HumanMinion.MercenaryType.ArcherTier3 => MercenaryArcherTier3Minion.ItemsCost,
                    HumanMinion.MercenaryType.Miner => HumanMinerMinion.ItemsCost,
                    HumanMinion.MercenaryType.Woodcutter => HumanWoodcutterMinion.ItemsCost,
                    _ => null
                };

                if (ChebGonazMinion.CanSpawn(itemsCost, _inventory, out _))
                    return merc;
            }

            return HumanMinion.MercenaryType.None;
        }

        private void PayForMercenary(HumanMinion.MercenaryType mercenaryType)
        {
            var itemsCost = mercenaryType switch
            {
                HumanMinion.MercenaryType.WarriorTier1 => MercenaryWarriorTier1Minion.ItemsCost,
                HumanMinion.MercenaryType.WarriorTier2 => MercenaryWarriorTier2Minion.ItemsCost,
                HumanMinion.MercenaryType.WarriorTier3 => MercenaryWarriorTier3Minion.ItemsCost,
                HumanMinion.MercenaryType.WarriorTier4 => MercenaryWarriorTier4Minion.ItemsCost,
                HumanMinion.MercenaryType.ArcherTier1 => MercenaryArcherTier1Minion.ItemsCost,
                HumanMinion.MercenaryType.ArcherTier2 => MercenaryArcherTier2Minion.ItemsCost,
                HumanMinion.MercenaryType.ArcherTier3 => MercenaryArcherTier3Minion.ItemsCost,
                HumanMinion.MercenaryType.Miner => HumanMinerMinion.ItemsCost,
                HumanMinion.MercenaryType.Woodcutter => HumanWoodcutterMinion.ItemsCost,
                _ => null
            };
            if (BasePlugin.HeavyLogging.Value)
            {
                var itemsCostLog = itemsCost?.Value == null ? "" : string.Join(", ", itemsCost.Value);
                Jotunn.Logger.LogInfo($"Paying for mercenary {mercenaryType} with {itemsCostLog}...");
            }
            ChebGonazMinion.ConsumeRequirements(itemsCost, _inventory);
        }

        private ChebGonazMinion.ArmorType UpgradeMercenaryEquipment()
        {
            var armorType = ChebGonazMinion.DetermineArmorType(
                _inventory,
                ArmorBlackIronRequiredConfig.Value,
                ArmorIronRequiredConfig.Value,
                ArmorBronzeRequiredConfig.Value,
                ArmorLeatherScrapsRequiredConfig.Value);
            
            if (BasePlugin.HeavyLogging.Value) Jotunn.Logger.LogInfo($"Determining mercenary's armour type: {armorType}.");

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
                    _inventory.RemoveItem("$item_trollhide", ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.LeatherWolf:
                    _inventory.RemoveItem("$item_wolfpelt", ArmorLeatherScrapsRequiredConfig.Value);
                    break;
            }

            return armorType;
        }

        IEnumerator Recruitment()
        {
            //yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            // originally the Container was set on the prefab in unity and set up properly, but it will cause the
            // problem here:  https://github.com/jpw1991/chebs-necromancy/issues/100
            // So we add it here like this instead.
            // Pros: No bug
            // Cons: Cannot set custom width/height
            _container = gameObject.AddComponent<Container>();
            _container.m_name = "$chebgonaz_mercenarychest_name";
            // _container.m_width = ContainerWidth.Value;
            // _container.m_height = ContainerHeight.Value;

            _inventory = _container.GetInventory();
            _inventory.m_name = Localization.instance.Localize(_container.m_name);
            // trying to set width causes error here: https://github.com/jpw1991/chebs-necromancy/issues/100
            // inv.m_width = ContainerWidth.Value;
            // inv.m_height = ContainerHeight.Value;

            while (true)
            {
                yield return new WaitForSeconds(5);
                
                if (!piece.m_nview.IsOwner()) continue;
                
                var playersInRange = new List<Player>();
                Player.GetPlayersInRange(transform.position, PlayerDetectionDistance, playersInRange);
                if (playersInRange.Count < 1) continue;

                yield return new WaitWhile(() => playersInRange[0].IsSleeping());
                
                var nextMerc = NextMercenary();

                if (playersInRange.Any(player => Vector3.Distance(player.transform.position, transform.position) < 5))
                {
                    var nextMercLocalized = nextMerc switch
                    {
                        HumanMinion.MercenaryType.WarriorTier1 => Localization.instance.Localize("$chebgonaz_mercenarytype_warriortier1"),
                        HumanMinion.MercenaryType.WarriorTier2 => Localization.instance.Localize("$chebgonaz_mercenarytype_warriortier2"),
                        HumanMinion.MercenaryType.WarriorTier3 => Localization.instance.Localize("$chebgonaz_mercenarytype_warriortier3"),
                        HumanMinion.MercenaryType.WarriorTier4 => Localization.instance.Localize("$chebgonaz_mercenarytype_warriortier4"),
                        HumanMinion.MercenaryType.ArcherTier1 => Localization.instance.Localize("$chebgonaz_mercenarytype_archertier1"),
                        HumanMinion.MercenaryType.ArcherTier2 => Localization.instance.Localize("$chebgonaz_mercenarytype_archertier2"),
                        HumanMinion.MercenaryType.ArcherTier3 => Localization.instance.Localize("$chebgonaz_mercenarytype_archertier3"),
                        HumanMinion.MercenaryType.Miner => Localization.instance.Localize("$chebgonaz_mercenarytype_miner"),
                        HumanMinion.MercenaryType.Woodcutter => Localization.instance.Localize("$chebgonaz_mercenarytype_woodcutter"),
                        _ => Localization.instance.Localize("$chebgonaz_mercenarytype_none")
                    };
                    var recruitmentMessage = 
                        Localization.instance.Localize("$chebgonaz_mercenarychest_recruitmentmessage")
                            .Replace("%1", nextMercLocalized)
                            .Replace("%2", (RecruitmentInterval.Value - (Time.time - _lastRecruitmentAt)).ToString("0"));
                    if (BasePlugin.HeavyLogging.Value) Jotunn.Logger.LogInfo(recruitmentMessage);
                    Chat.instance.SetNpcText(gameObject, Vector3.up, 5f, 4f, "",
                        recruitmentMessage,
                        false);
                }

                if (Time.time - _lastRecruitmentAt > RecruitmentInterval.Value)
                {
                    _lastRecruitmentAt = Time.time;
                    if (nextMerc != HumanMinion.MercenaryType.None)
                    {
                        PayForMercenary(nextMerc);
                        HumanMinion.Spawn(nextMerc, UpgradeMercenaryEquipment(), transform);
                    }
                }
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}