using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using UnityEngine;

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
                        Jotunn.Logger.LogError($"{enumValue} costs invalid - please review config values. Reverting to default value.");
                        splat = new[] { "CookedMeat:5", "Club:1" };
                    }
                
                    if (ValidRequirement(splat[0], splat[1], out GameObject splatPrefab, out int splatAmount))
                    {
                        _mercCosts[enumValue].Add((splatPrefab, splatAmount));
                    }
                    else
                    {
                        Jotunn.Logger.LogError($"{enumValue} costs invalid - please review config values. Failed to set it up at all.");
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

            
            
            // foreach (MercenaryTypes value in Enum.GetValues(typeof(MercenaryTypes)))
            // {
            //     if (value is MercenaryTypes.None) continue;
            //     
            // }
            
            return mercenaryType;
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
                
                yield return new WaitForSeconds(1);
                
                var nextMerc = NextMercenary();
                
                var player = Player.m_localPlayer;
                if (Vector3.Distance(player.transform.position, transform.position) < 5)
                {
                    Chat.instance.SetNpcText(gameObject, Vector3.up, 5f, 2f, "", 
                        $"Recruiting {nextMerc} in {(RecruitmentInterval.Value - (Time.time - _lastRecruitmentAt)).ToString("0.##")}%)...", false);
                }
                
                
            }
        }

        
        
        private int FuelInInventory
        {
            get
            {
                var accumulator = 0;
                foreach (var fuel in Fuels.Value.Split(','))
                {
                    var fuelPrefab = ZNetScene.instance.GetPrefab(fuel);
                    if (fuelPrefab == null) continue;
                    accumulator +=
                        _inventory.CountItems(fuelPrefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name);
                }
                return accumulator;
            }
        }

        private bool ConsumeFuel(int fuelToConsume)
        {
            var consumableFuels = new Dictionary<string, int>();
            var fuelAvailable = 0;
            foreach (var fuel in Fuels.Value.Split(','))
            {
                var fuelPrefab = ZNetScene.instance.GetPrefab(fuel);
                if (fuelPrefab == null) continue;
                var fuelName = fuelPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                var canConsume = _inventory.CountItems(fuelName);
                consumableFuels[fuelName] = canConsume;
                fuelAvailable += canConsume;

                if (fuelAvailable >= fuelToConsume) break;
            }

            // not enough fuel
            if (fuelAvailable < fuelToConsume) return false;
            
            // enough fuel; consume
            foreach (var key in consumableFuels.Keys)
            {
                var fuel = consumableFuels[key];
                if (fuelToConsume <= fuel)
                {
                    _inventory.RemoveItem(key, fuelToConsume);
                    return true;
                }
                
                fuelToConsume -= fuel;
                _inventory.RemoveItem(key, fuel);
            }

            return true;
        }

        private bool ConsumeFuel(WearNTear wearNTear)
        {
            // first pay any fuel debts
            if (_fuelAccumulator >= 1 && FuelInInventory >= _fuelAccumulator)
            {
                ConsumeFuel((int)_fuelAccumulator);
                _fuelAccumulator -= (int)_fuelAccumulator;
            }
            
            // debts paid - continue on to repair the current damage
            var percentage = wearNTear.GetHealthPercentage();
            if (percentage <= 0) return false;

            var fuelToConsume = (100 - (percentage * 100)) * FuelConsumedPerPointOfDamage.Value;
            // fuel to consume is too small to be currently deducated -> remember the amount and attempt to deduct
            // once it is larger
            if (fuelToConsume < 1) _fuelAccumulator += fuelToConsume;

            if (fuelToConsume > FuelInInventory) return false;

            ConsumeFuel((int)fuelToConsume);

            return true;
        }

        private List<WearNTear> PiecesInRange()
        {
            var nearbyColliders = Physics.OverlapSphere(transform.position + Vector3.up, SightRadius.Value, pieceMask);
            if (nearbyColliders.Length < 1) return null;

            var result = new List<WearNTear>();
            //var repairPylonSkipped = new List<string>();
            foreach (var nearbyCollider in nearbyColliders)
            {
                var wearAndTear = nearbyCollider.GetComponentInParent<WearNTear>();
                if (wearAndTear == null || !wearAndTear.m_nview.IsValid())
                {
                    //repairPylonSkipped.Add($"{nearbyCollider.name}");
                    continue;
                }
                result.Add(wearAndTear);
            }
            //Jotunn.Logger.LogInfo($"Skipped {string.Join(",", repairPylonSkipped)}");

            return result;
        }
    }
}