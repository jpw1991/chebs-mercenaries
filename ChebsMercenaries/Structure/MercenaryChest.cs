using System.Collections;
using System.Reflection;
using BepInEx.Configuration;
using ChebsMercenaries.Minions;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using UnityEngine;
using Logger = Jotunn.Logger;

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
        public static ConfigEntry<int> ArmorCarapaceRequiredConfig;
        public static ConfigEntry<int> ArmorFlametalRequiredConfig;

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

        private List<MercenaryMinion.MercenaryType> _orderedByPreference = new()
        {
            MercenaryMinion.MercenaryType.Catapult,
            MercenaryMinion.MercenaryType.Woodcutter,
            MercenaryMinion.MercenaryType.Miner,
            MercenaryMinion.MercenaryType.ArcherTier3,
            MercenaryMinion.MercenaryType.ArcherTier2,
            MercenaryMinion.MercenaryType.ArcherTier1,
            MercenaryMinion.MercenaryType.WarriorTier4,
            MercenaryMinion.MercenaryType.WarriorTier3,
            MercenaryMinion.MercenaryType.WarriorTier2,
            MercenaryMinion.MercenaryType.WarriorTier1,
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

            ArmorCarapaceRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArmorCarapaceRequired",
                1, "The amount of Carapace required to craft a minion in carapace armor.", null,
                true);

            ArmorFlametalRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArmorFlametalRequired",
                1, "The amount of Flametal required to craft a minion in flametal armor.", null,
                true);
        }

        private void Awake()
        {
            StartCoroutine(Recruitment());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private MercenaryMinion.MercenaryType NextMercenary()
        {
            foreach (var merc in _orderedByPreference)
            {
                var itemsCost = merc switch
                {
                    MercenaryMinion.MercenaryType.Catapult => CatapultMinion.ItemsCost,
                    MercenaryMinion.MercenaryType.WarriorTier1 => MercenaryWarriorTier1Minion.ItemsCost,
                    MercenaryMinion.MercenaryType.WarriorTier2 => MercenaryWarriorTier2Minion.ItemsCost,
                    MercenaryMinion.MercenaryType.WarriorTier3 => MercenaryWarriorTier3Minion.ItemsCost,
                    MercenaryMinion.MercenaryType.WarriorTier4 => MercenaryWarriorTier4Minion.ItemsCost,
                    MercenaryMinion.MercenaryType.ArcherTier1 => MercenaryArcherTier1Minion.ItemsCost,
                    MercenaryMinion.MercenaryType.ArcherTier2 => MercenaryArcherTier2Minion.ItemsCost,
                    MercenaryMinion.MercenaryType.ArcherTier3 => MercenaryArcherTier3Minion.ItemsCost,
                    MercenaryMinion.MercenaryType.Miner => HumanMinerMinion.ItemsCost,
                    MercenaryMinion.MercenaryType.Woodcutter => HumanWoodcutterMinion.ItemsCost,
                    _ => null
                };

                if (ChebGonazMinion.CanSpawn(itemsCost, _inventory, out _))
                    return merc;
            }

            return MercenaryMinion.MercenaryType.None;
        }

        private void PayForMercenary(MercenaryMinion.MercenaryType mercenaryType)
        {
            var itemsCost = mercenaryType switch
            {
                MercenaryMinion.MercenaryType.Catapult => CatapultMinion.ItemsCost,
                MercenaryMinion.MercenaryType.WarriorTier1 => MercenaryWarriorTier1Minion.ItemsCost,
                MercenaryMinion.MercenaryType.WarriorTier2 => MercenaryWarriorTier2Minion.ItemsCost,
                MercenaryMinion.MercenaryType.WarriorTier3 => MercenaryWarriorTier3Minion.ItemsCost,
                MercenaryMinion.MercenaryType.WarriorTier4 => MercenaryWarriorTier4Minion.ItemsCost,
                MercenaryMinion.MercenaryType.ArcherTier1 => MercenaryArcherTier1Minion.ItemsCost,
                MercenaryMinion.MercenaryType.ArcherTier2 => MercenaryArcherTier2Minion.ItemsCost,
                MercenaryMinion.MercenaryType.ArcherTier3 => MercenaryArcherTier3Minion.ItemsCost,
                MercenaryMinion.MercenaryType.Miner => HumanMinerMinion.ItemsCost,
                MercenaryMinion.MercenaryType.Woodcutter => HumanWoodcutterMinion.ItemsCost,
                _ => null
            };
            if (BasePlugin.HeavyLogging.Value)
            {
                var itemsCostLog = itemsCost?.Value == null ? "" : string.Join(", ", itemsCost.Value);
                Logger.LogInfo($"Paying for mercenary {mercenaryType} with {itemsCostLog}...");
            }
            ChebGonazMinion.ConsumeRequirements(itemsCost, _inventory);
        }

        private ChebGonazMinion.ArmorType UpgradeMercenaryEquipment(out bool useCarapace, out bool useFlametal)
        {
            useCarapace = false;
            useFlametal = false;

            // Check for Flametal first (highest priority)
            if (_inventory.CountItems("$item_flametal") >= ArmorFlametalRequiredConfig.Value)
            {
                _inventory.RemoveItem("$item_flametal", ArmorFlametalRequiredConfig.Value);
                useFlametal = true;
                return ChebGonazMinion.ArmorType.BlackMetal; // Use BlackMetal as base type
            }
            // Check for Carapace second
            if (_inventory.CountItems("$item_carapace") >= ArmorCarapaceRequiredConfig.Value)
            {
                _inventory.RemoveItem("$item_carapace", ArmorCarapaceRequiredConfig.Value);
                useCarapace = true;
                return ChebGonazMinion.ArmorType.BlackMetal; // Use BlackMetal as base type
            }

            // Fall back to original armor type determination
            var armorType = ChebGonazMinion.DetermineArmorType(
                _inventory,
                ArmorBlackIronRequiredConfig.Value,
                ArmorIronRequiredConfig.Value,
                ArmorBronzeRequiredConfig.Value,
                ArmorLeatherScrapsRequiredConfig.Value);

            if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Determining mercenary's armour type: {armorType}.");

            switch (armorType)
            {
                case ChebGonazMinion.ArmorType.BlackMetal:
                    _inventory.RemoveItem("$item_blackmetal", ArmorBlackIronRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Iron:
                    _inventory.RemoveItem("$item_iron", ArmorIronRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Bronze:
                    _inventory.RemoveItem("$item_bronze", ArmorBronzeRequiredConfig.Value);
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
                        MercenaryMinion.MercenaryType.Catapult => Localization.instance.Localize("$chebgonaz_mercenarytype_catapult"),
                        MercenaryMinion.MercenaryType.WarriorTier1 => Localization.instance.Localize("$chebgonaz_mercenarytype_warriortier1"),
                        MercenaryMinion.MercenaryType.WarriorTier2 => Localization.instance.Localize("$chebgonaz_mercenarytype_warriortier2"),
                        MercenaryMinion.MercenaryType.WarriorTier3 => Localization.instance.Localize("$chebgonaz_mercenarytype_warriortier3"),
                        MercenaryMinion.MercenaryType.WarriorTier4 => Localization.instance.Localize("$chebgonaz_mercenarytype_warriortier4"),
                        MercenaryMinion.MercenaryType.ArcherTier1 => Localization.instance.Localize("$chebgonaz_mercenarytype_archertier1"),
                        MercenaryMinion.MercenaryType.ArcherTier2 => Localization.instance.Localize("$chebgonaz_mercenarytype_archertier2"),
                        MercenaryMinion.MercenaryType.ArcherTier3 => Localization.instance.Localize("$chebgonaz_mercenarytype_archertier3"),
                        MercenaryMinion.MercenaryType.Miner => Localization.instance.Localize("$chebgonaz_mercenarytype_miner"),
                        MercenaryMinion.MercenaryType.Woodcutter => Localization.instance.Localize("$chebgonaz_mercenarytype_woodcutter"),
                        _ => Localization.instance.Localize("$chebgonaz_mercenarytype_none")
                    };
                    var recruitmentMessage =
                        Localization.instance.Localize("$chebgonaz_mercenarychest_recruitmentmessage")
                            .Replace("%1", nextMercLocalized)
                            .Replace("%2", (RecruitmentInterval.Value - (Time.time - _lastRecruitmentAt)).ToString("0"));
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo(recruitmentMessage);
                    Chat.instance.SetNpcText(gameObject, Vector3.up, 5f, 4f, "",
                        recruitmentMessage,
                        false);
                }

                if (Time.time - _lastRecruitmentAt > RecruitmentInterval.Value)
                {
                    _lastRecruitmentAt = Time.time;
                    if (nextMerc != MercenaryMinion.MercenaryType.None)
                    {
                        PayForMercenary(nextMerc);
                        if (nextMerc == MercenaryMinion.MercenaryType.Catapult)
                        {
                            CatapultMinion.Spawn(transform);
                        }
                        else
                        {
                            var skinColors = MercenaryChestOptionsGUI.GetSkins(_container)
                                .Select(str => str.Trim()).ToList().Select(html =>
                                ColorUtility.TryParseHtmlString(html, out Color color)
                                    ? Utils.ColorToVec3(color)
                                    : Vector3.zero).ToList();
                            var hairColors = MercenaryChestOptionsGUI.GetHairs(_container)
                                .Select(str => str.Trim()).ToList().Select(html =>
                                    ColorUtility.TryParseHtmlString(html, out Color color)
                                        ? Utils.ColorToVec3(color)
                                        : Vector3.zero).ToList();
                            var genderStr = MercenaryChestOptionsGUI.GetGender(_container);
                            if (!float.TryParse(genderStr, out var chanceOfFemale))
                            {
                                Logger.LogError($"Failed to parse {genderStr}, defaulting to 50%");
                                chanceOfFemale = 50f;
                            }
                            chanceOfFemale /= 100; // convert from eg. 50% to 0.5

                            var armorType = UpgradeMercenaryEquipment(out bool useCarapace, out bool useFlametal);
                            HumanMinion.Spawn(nextMerc, armorType, transform,
                                chanceOfFemale, skinColors, hairColors, useCarapace, useFlametal);
                        }
                    }
                }
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}