using System.Collections;
using ChebsMercenaries.Structure;
using ChebsValheimLibrary.Minions;
using UnityEngine;
using Logger = Jotunn.Logger;
using Random = UnityEngine.Random;

namespace ChebsMercenaries.Minions
{
    public class HumanMinion : MercenaryMinion
    {
        private static List<ItemDrop> _hairs, _beards;
        
        public sealed override void Awake()
        {
            base.Awake();
            _hairs ??= ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair");
            _beards ??= ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard");

            StartCoroutine(WaitForZNet());
            
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

        protected IEnumerator WaitForZNet()
        {
            yield return new WaitUntil(() => ZNetScene.instance != null);

            if (!TryGetComponent(out Humanoid humanoid))
            {
                Logger.LogError("Humanoid component missing!");
                yield break;
            }

            // VisEquipment remembers what armor the skeleton is wearing.
            // Exploit this to reapply the armor so the armor values work
            // again.
            var equipmentHashes = new List<int>()
            {
                humanoid.m_visEquipment.m_currentChestItemHash,
                humanoid.m_visEquipment.m_currentLegItemHash,
                humanoid.m_visEquipment.m_currentHelmetItemHash,
            };
            equipmentHashes.ForEach(hash =>
            {
                var equipmentPrefab = ZNetScene.instance.GetPrefab(hash);
                if (equipmentPrefab != null)
                {
                    humanoid.GiveDefaultItem(equipmentPrefab);
                }
            });

            // todo: implement custom emblems like for skeletons
            // var shoulderHash = humanoid.m_visEquipment.m_currentShoulderItemHash;
            // var shoulderPrefab = ZNetScene.instance.GetPrefab(shoulderHash);
            // if (shoulderPrefab != null
            //     && shoulderPrefab.TryGetComponent(out ItemDrop itemDrop)
            //     && itemDrop.name.Equals("CapeLox"))
            // {
            //     var emblem = Emblem;
            //     if (Emblem.Contains(emblem))
            //     {
            //         var material = NecromancerCape.Emblems[Emblem];
            //         humanoid.m_visEquipment.m_shoulderItemInstances.ForEach(g => 
            //             g.GetComponentsInChildren<SkinnedMeshRenderer>().ToList().ForEach(m =>
            //             {
            //                 var mats = m.materials;
            //                 for (int i = 0; i < mats.Length; i++)
            //                 {
            //                     mats[i] = material;
            //                 }
            //                 m.materials = mats;
            //             })
            //         );   
            //     }
            // }
            if (TryGetComponent(out MonsterAI monsterAI))
            {
                monsterAI.m_randomMoveRange = RoamRange.Value;
            }
            else
            {
                Logger.LogWarning($"{gameObject.name}: Failed to set roam range: no MonsterAI component.");
            }

            RestoreDrops();
            
            if (BasePlugin.HeavyLogging.Value)
            {
                Logger.LogInfo($"Health set for {gameObject.name}: humanoid.m_health={humanoid.m_health}");
            }
        }

        public void ScaleEquipment(MercenaryType mercenaryType, ArmorType armorType)
        {
            var defaultItems = new List<GameObject>();

            var humanoid = GetComponent<Humanoid>();
            if (humanoid == null)
            {
                Logger.LogError("ScaleEquipment: humanoid is null!");
                return;
            }

            // note: as of 1.2.0 weapons were moved into skeleton prefab variants
            // with different m_randomWeapons set. This is because trying to set
            // dynamically seems very difficult -> skeletons forgetting their weapons
            // on logout/log back in; skeletons thinking they have no weapons
            // and running away from enemies.
            //
            // Fortunately, armor seems to work fine.
            switch (armorType)
            {
                case ArmorType.Leather:
                    defaultItems.AddRange(new[]
                    {
                        ZNetScene.instance.GetPrefab("HelmetLeather"),
                        ZNetScene.instance.GetPrefab("ArmorLeatherChest"),
                        ZNetScene.instance.GetPrefab("ArmorLeatherLegs"),
                        ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    break;
                case ArmorType.LeatherTroll:
                    defaultItems.AddRange(new[]
                    {
                        ZNetScene.instance.GetPrefab("ChebGonaz_HelmetLeatherTroll"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherChestTroll"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherLegsTroll"),
                        ZNetScene.instance.GetPrefab("CapeTrollHide"),
                    });
                    break;
                case ArmorType.LeatherWolf:
                    defaultItems.AddRange(new[]
                    {
                        ZNetScene.instance.GetPrefab("ChebGonaz_HelmetLeatherWolf"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherChestWolf"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherLegsWolf"),
                        ZNetScene.instance.GetPrefab("CapeWolf"),
                    });
                    break;
                case ArmorType.LeatherLox:
                    defaultItems.AddRange(new[]
                    {
                        ZNetScene.instance.GetPrefab("ChebGonaz_HelmetLeatherLox"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherChestLox"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherLegsLox"),
                        ZNetScene.instance.GetPrefab("CapeLox"),
                    });
                    break;
                case ArmorType.Bronze:
                    defaultItems.AddRange(new[]
                    {
                        ZNetScene.instance.GetPrefab("HelmetBronze"),
                        ZNetScene.instance.GetPrefab("ArmorBronzeChest"),
                        ZNetScene.instance.GetPrefab("ArmorBronzeLegs"),
                        ZNetScene.instance.GetPrefab("CapeLox"),
                    });
                    //Emblem = InternalName.GetName(NecromancerCape.EmblemConfig.Value);
                    break;
                case ArmorType.Iron:
                    defaultItems.AddRange(new[]
                    {
                        ZNetScene.instance.GetPrefab("HelmetIron"),
                        ZNetScene.instance.GetPrefab("ArmorIronChest"),
                        ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                        ZNetScene.instance.GetPrefab("CapeLox"),
                    });
                    //Emblem = InternalName.GetName(NecromancerCape.EmblemConfig.Value);
                    break;
                case ArmorType.BlackMetal:
                    defaultItems.AddRange(new[]
                    {
                        ZNetScene.instance.GetPrefab("HelmetPadded"),
                        ZNetScene.instance.GetPrefab("ArmorPaddedCuirass"),
                        ZNetScene.instance.GetPrefab("ArmorPaddedGreaves"),
                        ZNetScene.instance.GetPrefab("CapeLox"),
                    });
                    //Emblem = InternalName.GetName(NecromancerCape.EmblemConfig.Value);
                    break;
            }

            humanoid.m_defaultItems = defaultItems.ToArray();

            if (BasePlugin.HeavyLogging.Value)
            {
                var equipmentStringLog = string.Join(", ", defaultItems.Select(a => a.name));
                Logger.LogInfo($"Provided equipment {mercenaryType} {armorType}: {equipmentStringLog}");
            }

            humanoid.GiveDefaultItems();
            humanoid.m_visEquipment.UpdateEquipmentVisuals();
        }

        public static void Spawn(MercenaryType mercenaryType, ArmorType armorType, Transform spawner, 
            float chanceOfFemale, List<Vector3> skinColors, List<Vector3> hairColors)
        {
            if (mercenaryType is MercenaryType.None) return;
            
            if (ZNetScene.instance == null)
            {
                Logger.LogWarning("Spawn: ZNetScene.instance is null, trying again later...");
                return;
            }

            var female = Random.value < chanceOfFemale;

            var prefabName = female ? PrefabNamesFemale[mercenaryType] : PrefabNames[mercenaryType];
            if (BasePlugin.HeavyLogging.Value)
            {
                var genderLog = female ? "female" : "male";
                Logger.LogInfo($"Spawning {genderLog} {mercenaryType} with {armorType} from {prefabName}.");
            };
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"Spawn: spawning {prefabName} failed");
                return;
            }

            var spawnedChar = Instantiate(prefab,
                spawner.position + spawner.forward * 2f + Vector3.up, Quaternion.identity);
            
            if (spawnedChar == null)
            {
                Logger.LogError("Spawn: spawnedChar is null");
                return;
            }
            
            spawnedChar.AddComponent<FreshMinion>();

            // set hair and skin color

            if (!spawnedChar.TryGetComponent(out Humanoid humanoid))
            {
                Logger.LogError("Spawn: spawnedChar has no humanoid component");
                return;
            }
            
            var randomSkinColor = skinColors[Random.Range(0, skinColors.Count)];
            humanoid.m_visEquipment.SetSkinColor(randomSkinColor);
            humanoid.m_nview.GetZDO().Set("SkinColor", randomSkinColor);
            var randomHair = _hairs[Random.Range(0, _hairs.Count)].gameObject.name;
            humanoid.SetHair(randomHair);
            var randomHairColor = hairColors[Random.Range(0, hairColors.Count)];
            humanoid.m_visEquipment.SetHairColor(randomHairColor);
            if (!female)
            {
                var randomBeard = _beards[Random.Range(0, _beards.Count)].gameObject.name;
                if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Applying beard {randomBeard}.");
                humanoid.SetBeard(randomBeard);
                humanoid.m_visEquipment.SetBeardItem(humanoid.m_beardItem);
            }

            if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Applying hair {humanoid.m_hairItem}.");
            humanoid.m_visEquipment.SetHairItem(humanoid.m_hairItem);
            humanoid.m_visEquipment.UpdateEquipmentVisuals();

            // var minion = mercenaryType switch
            // {
            //     MercenaryType.Miner => spawnedChar.AddComponent<HumanMinerMinion>(),
            //     MercenaryType.Woodcutter => spawnedChar.AddComponent<HumanWoodcutterMinion>(),
            //     _ => spawnedChar.AddComponent<HumanMinion>()
            // };
            if (!spawnedChar.TryGetComponent(out HumanMinion minion))
            {
                Logger.LogError("Spawn: spawnedChar has no HumanMinion component");
                return;
            }

            minion.ScaleEquipment(mercenaryType, armorType);

            if (mercenaryType != MercenaryType.Miner && mercenaryType != MercenaryType.Woodcutter)
                minion.Roam();

            // handle refunding of resources on death
            if (DropOnDeath.Value == DropType.Nothing) return;

            var characterDrop = spawnedChar.AddComponent<CharacterDrop>();
            if (DropOnDeath.Value == DropType.Everything)
            {
                switch (mercenaryType)
                {
                    case MercenaryType.Miner:
                        GenerateDeathDrops(characterDrop, HumanMinerMinion.ItemsCost);
                        break;
                    case MercenaryType.Woodcutter:
                        GenerateDeathDrops(characterDrop, HumanWoodcutterMinion.ItemsCost);
                        break;
                    case MercenaryType.ArcherTier1:
                        GenerateDeathDrops(characterDrop, MercenaryArcherTier1Minion.ItemsCost);
                        break;
                    case MercenaryType.ArcherTier2:
                        GenerateDeathDrops(characterDrop, MercenaryArcherTier2Minion.ItemsCost);
                        break;
                    case MercenaryType.ArcherTier3:
                        GenerateDeathDrops(characterDrop, MercenaryArcherTier3Minion.ItemsCost);
                        break;
                    case MercenaryType.WarriorTier1:
                        GenerateDeathDrops(characterDrop, MercenaryWarriorTier1Minion.ItemsCost);
                        break;
                    case MercenaryType.WarriorTier2:
                        GenerateDeathDrops(characterDrop, MercenaryWarriorTier2Minion.ItemsCost);
                        break;
                    case MercenaryType.WarriorTier3:
                        GenerateDeathDrops(characterDrop, MercenaryWarriorTier3Minion.ItemsCost);
                        break;
                    case MercenaryType.WarriorTier4:
                        GenerateDeathDrops(characterDrop, MercenaryWarriorTier4Minion.ItemsCost);
                        break;
                }
            }

            switch (armorType)
            {
                case ArmorType.Leather:
                    AddOrUpdateDrop(characterDrop,
                        Random.value > .5f ? "DeerHide" : "LeatherScraps", // flip a coin for deer or scraps
                        MercenaryChest.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.LeatherTroll:
                    AddOrUpdateDrop(characterDrop, "TrollHide", MercenaryChest.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.LeatherWolf:
                    AddOrUpdateDrop(characterDrop, "WolfPelt", MercenaryChest.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.LeatherLox:
                    AddOrUpdateDrop(characterDrop, "LoxPelt", MercenaryChest.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.Bronze:
                    AddOrUpdateDrop(characterDrop, "Bronze", MercenaryChest.ArmorBronzeRequiredConfig.Value);
                    break;
                case ArmorType.Iron:
                    AddOrUpdateDrop(characterDrop, "Iron", MercenaryChest.ArmorIronRequiredConfig.Value);
                    break;
                case ArmorType.BlackMetal:
                    AddOrUpdateDrop(characterDrop, "BlackMetal", MercenaryChest.ArmorBlackIronRequiredConfig.Value);
                    break;
            }

            // the component won't be remembered by the game on logout because
            // only what is on the prefab is remembered. Even changes to the prefab
            // aren't remembered. So we must write what we're dropping into
            // the ZDO as well and then read & restore this on Awake
            minion.RecordDrops(characterDrop);
        }
    }
}