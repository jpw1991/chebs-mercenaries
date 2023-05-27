using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsMercenaries.Structure;
using ChebsNecromancy.Minions;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsMercenaries.Minions
{
    public class HumanMinion : ChebGonazMinion
    {
        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate;
        public static ConfigEntry<bool> Commandable;
        public static ConfigEntry<float> FollowDistance, RunDistance;
        public static ConfigEntry<float> ChanceOfFemale;
        public static MemoryConfigEntry<string, List<Vector3>> HairColors, SkinColors;

        private static List<ItemDrop> _hairs, _beards;

        public static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSync = "HumanMinion (Server Synced)";
            const string client = "HumanMinion (Client)";
            DropOnDeath = plugin.Config.Bind(serverSync, 
                "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PackDropItemsIntoCargoCrate = plugin.Config.Bind(serverSync, 
                "PackDroppedItemsIntoCargoCrate",
                true, new ConfigDescription("If set to true, dropped items will be packed into a cargo crate. This means they won't sink in water, which is useful for more valuable drops like Surtling Cores and metal ingots.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            Commandable = plugin.Config.Bind(client, "Commandable",
                true, new ConfigDescription("If true, minions can be commanded individually with E (or equivalent) keybind."));
            
            FollowDistance = plugin.Config.Bind(client, "FollowDistance",
                3f, new ConfigDescription("How closely a minion will follow you (0 = standing on top of you, 3 = default)."));
            
            RunDistance = plugin.Config.Bind(client, "RunDistance",
                3f, new ConfigDescription("How close a following minion needs to be to you before it stops running and starts walking (0 = always running, 10 = default)."));
            
            ChanceOfFemale = plugin.ModConfig(serverSync, "ChanceOfFemale", 0.5f,
                "Chance of a mercenary spawning being female. 0 = 0%, 1 = 100% (Default = 0.5 = 50%)", 
                new AcceptableValueRange<float>(0f, 1f), true);
            
            var hairColors = plugin.ModConfig(serverSync, "HairColors", "#F7DC6F,#935116,#AFABAB,#FF5733,#1C2833",
                "Comma delimited list of HTML color codes.", null, true);
            HairColors = new MemoryConfigEntry<string, List<Vector3>>(hairColors, s =>
            {
                var cols = s?.Split(',').ToList().Select(colorCode => 
                    ColorUtility.TryParseHtmlString(colorCode, out Color color)
                    ? Utils.ColorToVec3(color)
                    : Vector3.zero).ToList();
                return cols;
            });
            
            var skinColors = plugin.ModConfig(serverSync, "SkinColors", "#FEF5E7,#F5CBA7,#784212,#F5B041",
                "Comma delimited list of HTML color codes.", null, true);
            SkinColors = new MemoryConfigEntry<string, List<Vector3>>(skinColors, s =>
            {
                var cols = s?.Split(',').ToList().Select(colorCode => 
                    ColorUtility.TryParseHtmlString(colorCode, out Color color)
                        ? Utils.ColorToVec3(color)
                        : Vector3.zero).ToList();
                return cols;
            });
        }
        
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
        };

        private void Awake()
        {
            _hairs ??= ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair");
            _beards ??= ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard");
            
            StartCoroutine(WaitForZNet());
        }

        IEnumerator WaitForZNet()
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

            RestoreDrops();
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
                    defaultItems.AddRange(new[] {
                        ZNetScene.instance.GetPrefab("HelmetLeather"),
                        ZNetScene.instance.GetPrefab("ArmorLeatherChest"),
                        ZNetScene.instance.GetPrefab("ArmorLeatherLegs"),
                        ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    break;
                case ArmorType.LeatherTroll:
                    defaultItems.AddRange(new[] {
                        ZNetScene.instance.GetPrefab("ChebGonaz_HelmetLeatherTroll"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherChestTroll"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherLegsTroll"),
                        ZNetScene.instance.GetPrefab("CapeTrollHide"),
                    });
                    break;
                case ArmorType.LeatherWolf:
                    defaultItems.AddRange(new[] {
                        ZNetScene.instance.GetPrefab("ChebGonaz_HelmetLeatherWolf"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherChestWolf"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherLegsWolf"),
                        ZNetScene.instance.GetPrefab("CapeWolf"),
                    });
                    break;
                case ArmorType.LeatherLox:
                    defaultItems.AddRange(new[] {
                        ZNetScene.instance.GetPrefab("ChebGonaz_HelmetLeatherLox"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherChestLox"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherLegsLox"),
                        ZNetScene.instance.GetPrefab("CapeLox"),
                    });
                    break;
                case ArmorType.Bronze:
                    defaultItems.AddRange(new[] {
                        ZNetScene.instance.GetPrefab("HelmetBronze"),
                        ZNetScene.instance.GetPrefab("ArmorBronzeChest"),
                        ZNetScene.instance.GetPrefab("ArmorBronzeLegs"),
                        ZNetScene.instance.GetPrefab("CapeLox"),
                    });
                    //Emblem = InternalName.GetName(NecromancerCape.EmblemConfig.Value);
                    break;
                case ArmorType.Iron:
                    defaultItems.AddRange(new[] {
                        ZNetScene.instance.GetPrefab("HelmetIron"),
                        ZNetScene.instance.GetPrefab("ArmorIronChest"),
                        ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                        ZNetScene.instance.GetPrefab("CapeLox"),
                    });
                    //Emblem = InternalName.GetName(NecromancerCape.EmblemConfig.Value);
                    break;
                case ArmorType.BlackMetal:
                    defaultItems.AddRange(new[] {
                        ZNetScene.instance.GetPrefab("ChebGonaz_HelmetBlackIron"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronChest"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronLegs"),
                        ZNetScene.instance.GetPrefab("CapeLox"),
                    });
                    //Emblem = InternalName.GetName(NecromancerCape.EmblemConfig.Value);
                    break;
            }

            humanoid.m_defaultItems = defaultItems.ToArray();

            humanoid.GiveDefaultItems();
        }
        
        public static void Spawn(MercenaryType mercenaryType, ArmorType armorType, Transform spawner)
        {
            if (mercenaryType is MercenaryType.None) return;

            var female = Random.value < ChanceOfFemale.Value;
            
            var prefabName = female ? PrefabNamesFemale[mercenaryType] : PrefabNames[mercenaryType];
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"Spawn: spawning {prefabName} failed");
                return;
            }
            var spawnedChar = Instantiate(prefab,
                spawner.position + spawner.forward * 2f + Vector3.up, Quaternion.identity);
            spawnedChar.AddComponent<FreshMinion>();

            // set hair and skin color
            var humanoid = spawnedChar.GetComponent<Humanoid>();
            var randomSkinColor = SkinColors.Value[Random.Range(0, SkinColors.Value.Count)];
            humanoid.m_visEquipment.SetSkinColor(randomSkinColor);
            var randomHair = _hairs[Random.Range(0, _hairs.Count)].gameObject.name;
            humanoid.SetHair(randomHair);
            var randomHairColor = HairColors.Value[Random.Range(0, HairColors.Value.Count)];
            humanoid.m_visEquipment.SetHairColor(randomHairColor);
            if (!female)
            {
                var randomBeard = _beards[Random.Range(0, _beards.Count)].gameObject.name;
                humanoid.SetBeard(randomBeard);
                humanoid.m_visEquipment.SetBeardItem(humanoid.m_beardItem);
            }
            humanoid.m_visEquipment.SetHairItem(humanoid.m_hairItem);
            humanoid.m_visEquipment.UpdateEquipmentVisuals();

            var minion = mercenaryType switch
            {
                MercenaryType.Miner => spawnedChar.AddComponent<HumanMinerMinion>(),
                MercenaryType.Woodcutter => spawnedChar.AddComponent<HumanWoodcutterMinion>(),
                _ => spawnedChar.AddComponent<HumanMinion>()
            };
            
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