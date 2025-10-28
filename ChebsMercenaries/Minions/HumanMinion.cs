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

            var zdo = humanoid.m_nview.GetZDO();
            if (zdo != null && zdo.GetInt("HasCustomEquipment", 0) == 1)
            {
                Logger.LogInfo($"{gameObject.name}: Already has saved equipment, skipping re-equip.");

                // Check if this is a special minion and clear random weapons
                var savedWeapon = zdo.GetString("EquippedWeapon", "");
                bool isSpecialMinion = !string.IsNullOrEmpty(savedWeapon) &&
                                       (savedWeapon.Contains("Carapace") || savedWeapon.Contains("Flametal") ||
                                        savedWeapon.Contains("JotunBane") || savedWeapon.Contains("Mistwalker") ||
                                        savedWeapon.Contains("Eldner") || savedWeapon.Contains("Niedhogg") ||
                                        savedWeapon.Contains("Splitner"));

                if (isSpecialMinion)
                {
                    humanoid.m_randomWeapon = Array.Empty<GameObject>();
                    humanoid.m_randomShield = Array.Empty<GameObject>();
                    humanoid.m_randomArmor = Array.Empty<GameObject>();
                    humanoid.m_randomSets = Array.Empty<Humanoid.ItemSet>();
                    humanoid.m_randomItems = Array.Empty<Humanoid.RandomItem>();
                    Logger.LogInfo(
                        $"Cleared random equipment for special minion {gameObject.name} with weapon {savedWeapon}");

                    // Also remove any default weapons from inventory that might have spawned
                    var inventory = humanoid.GetInventory();
                    var itemsToRemove = new List<ItemDrop.ItemData>();

                    foreach (var item in inventory.GetAllItems())
                    {
                        if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                            item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                            item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
                        {
                            var itemName = item.m_shared.m_name;
                            bool isSpecialWeapon = itemName.Contains("JotunBane") || itemName.Contains("Mistwalker") ||
                                                   itemName.Contains("SpearCarapace") || itemName.Contains("Eldner") ||
                                                   itemName.Contains("Niedhogg") || itemName.Contains("Splitner");

                            if (!isSpecialWeapon)
                            {
                                itemsToRemove.Add(item);
                                Logger.LogInfo($"Removing default weapon from reload: {itemName}");
                            }
                        }
                    }

                    foreach (var item in itemsToRemove)
                    {
                        inventory.RemoveItem(item);
                    }
                }

                // Re-equip armor
                var equipmentHashes = new List<int>
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

                // Re-equip saved weapon
                var weaponName = zdo.GetString("EquippedWeapon");
                if (!string.IsNullOrEmpty(weaponName))
                {
                    var weaponPrefab = ZNetScene.instance.GetPrefab(weaponName);
                    if (weaponPrefab != null)
                    {
                        var itemDrop = weaponPrefab.GetComponent<ItemDrop>();
                        if (itemDrop != null)
                        {
                            // First give the item to inventory
                            humanoid.GiveDefaultItem(weaponPrefab);

                            // Find the item in inventory and equip it
                            var inventory = humanoid.GetInventory();
                            var weaponItem = inventory.GetAllItems().FirstOrDefault(item =>
                                item.m_shared.m_name == itemDrop.m_itemData.m_shared.m_name);

                            if (weaponItem != null)
                            {
                                humanoid.EquipItem(weaponItem);
                                Logger.LogInfo($"Utrustade vapen: {weaponName}");
                            }
                            else
                            {
                                Logger.LogWarning($"Could not find weapon {weaponName} in inventory after giving it.");
                            }
                        }
                        else
                        {
                            Logger.LogWarning($"ItemDrop saknas p� prefab '{weaponName}'.");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Could not find weapon prefab '{weaponName}' in ZNetScene.");
                    }
                }

                // Re-equip saved shield
                var shieldName = zdo.GetString("EquippedShield");
                if (!string.IsNullOrEmpty(shieldName))
                {
                    var shieldPrefab = ZNetScene.instance.GetPrefab(shieldName);
                    if (shieldPrefab != null)
                    {
                        humanoid.GiveDefaultItem(shieldPrefab);
                        var shieldDrop = shieldPrefab.GetComponent<ItemDrop>();
                        if (shieldDrop != null)
                        {
                            // Find the shield in inventory and equip it
                            var inventory = humanoid.GetInventory();
                            var shieldItem = inventory.GetAllItems().FirstOrDefault(item =>
                                item.m_shared.m_name == shieldDrop.m_itemData.m_shared.m_name);

                            if (shieldItem != null)
                            {
                                humanoid.EquipItem(shieldItem);
                                Logger.LogInfo($"Equipped shield: {shieldName}");
                            }
                            else
                            {
                                Logger.LogWarning($"Could not find shield {shieldName} in inventory after giving it.");
                            }
                        }
                        else
                        {
                            Logger.LogWarning($"ItemDrop is missing on prefab '{shieldName}'.");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Failed to find prefab '{shieldName}' in ZNetScene.");
                    }
                }

                // Mark as equipped if not already done
                zdo.Set("HasCustomEquipment", 1);

                // Adjust AI if necessary
                if (TryGetComponent(out MonsterAI monsterAI))
                {
                    monsterAI.m_randomMoveRange = RoamRange.Value;

                    if (!zdo.GetBool("HasCustomAI", false))
                    {
                        zdo.Set("HasCustomAI", true);
                    }
                }
                else
                {
                    Logger.LogWarning($"{gameObject.name}: Failed to set roam range: no MonsterAI component.");
                }

                // Restore command state - handle Following with delay to wait for players to spawn
                var waitPos = GetWaitPosition();
                Logger.LogInfo($"DEBUG: {gameObject.name} wait position = {waitPos}");
                Logger.LogInfo($"DEBUG: UndeadMinionMaster = '{UndeadMinionMaster}'");
                Logger.LogInfo($"Restoring state for {gameObject.name}: Status = {Status}");

                if (waitPos.Equals(Vector3.positiveInfinity)) // Following state
                {
                    // For Following, wait a bit for the player to spawn then restore
                    StartCoroutine(WaitForPlayerThenFollow());
                }
                else
                {
                    // For Wait and Roam states, use normal restoration immediately
                    RoamFollowOrWait();
                }

                RestoreDrops();

                if (BasePlugin.HeavyLogging.Value)
                {
                    Logger.LogInfo($"Health set for {gameObject.name}: humanoid.m_health={humanoid.m_health}");
                }
            }
            else
            {
                // Original behavior for new spawns
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

                if (TryGetComponent(out MonsterAI monsterAI))
                {
                    monsterAI.m_randomMoveRange = RoamRange.Value;
                }

                RestoreDrops();
            }
        }

        public void ScaleEquipment(MercenaryType mercenaryType, ArmorType armorType, bool useCarapace = false,
            bool useFlametal = false)
        {
            var defaultItems = new List<GameObject>();

            var humanoid = GetComponent<Humanoid>();
            if (humanoid == null)
            {
                Logger.LogError("ScaleEquipment: humanoid is null!");
                return;
            }

            // Build defaultItems exactly like original, but add special weapons for carapace/flametal
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
                    break;
                case ArmorType.Iron:
                    defaultItems.AddRange(new[]
                    {
                        ZNetScene.instance.GetPrefab("HelmetIron"),
                        ZNetScene.instance.GetPrefab("ArmorIronChest"),
                        ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                        ZNetScene.instance.GetPrefab("CapeLox"),
                    });
                    break;
                case ArmorType.BlackMetal:
                    if (useCarapace)
                    {
                        var carapaceWeapons = new List<GameObject>
                        {
                            ZNetScene.instance.GetPrefab("AxeJotunBane"),
                            ZNetScene.instance.GetPrefab("SwordMistwalker"),
                            ZNetScene.instance.GetPrefab("SpearCarapace")
                        }.Where(p => p != null).ToList();

                        var chosenWeapon = carapaceWeapons[UnityEngine.Random.Range(0, carapaceWeapons.Count)];

                        defaultItems.AddRange(new[]
                        {
                            ZNetScene.instance.GetPrefab("HelmetCarapace"),
                            ZNetScene.instance.GetPrefab("ArmorCarapaceChest"),
                            ZNetScene.instance.GetPrefab("ArmorCarapaceLegs"),
                            ZNetScene.instance.GetPrefab("CapeFeather"),
                        });

                        // Only warriors get weapons and shields, archers get bows
                        if (mercenaryType == MercenaryType.WarriorTier1 ||
                            mercenaryType == MercenaryType.WarriorTier2 ||
                            mercenaryType == MercenaryType.WarriorTier3 ||
                            mercenaryType == MercenaryType.WarriorTier4)
                        {
                            defaultItems.Add(ZNetScene.instance.GetPrefab("ShieldCarapace"));
                            defaultItems.Add(chosenWeapon);

                            // Save the chosen weapon for persistence
                            var weaponZdo = humanoid.m_nview.GetZDO();
                            if (weaponZdo != null)
                            {
                                weaponZdo.Set("EquippedWeapon", CleanName(chosenWeapon.name));
                                weaponZdo.Set("EquippedShield", "ShieldCarapace");
                            }
                        }
                        else if (mercenaryType == MercenaryType.ArcherTier1 ||
                                 mercenaryType == MercenaryType.ArcherTier2 ||
                                 mercenaryType == MercenaryType.ArcherTier3)
                        {
                            // Apply visual changes to AI bow appearance (preserves functionality)
                            SwapAllMercenaryBowPrefabs(true, false);

                            // Add special arrows
                            var carapaceArrows = ZNetScene.instance.GetPrefab("ArrowCarapace");
                            if (carapaceArrows != null)
                            {
                                defaultItems.Add(carapaceArrows);
                                Logger.LogInfo("Added ArrowCarapace for carapace archer");
                            }
                        }
                    }
                    else if (useFlametal)
                    {
                        var flametalWeapons = new List<GameObject>
                        {
                            ZNetScene.instance.GetPrefab("MaceEldner"),
                            ZNetScene.instance.GetPrefab("SwordNiedhogg"),
                            ZNetScene.instance.GetPrefab("SpearSplitner")
                        }.Where(p => p != null).ToList();

                        var chosenWeapon = flametalWeapons[UnityEngine.Random.Range(0, flametalWeapons.Count)];

                        defaultItems.AddRange(new[]
                        {
                            ZNetScene.instance.GetPrefab("HelmetFlametal"),
                            ZNetScene.instance.GetPrefab("ArmorFlametalChest"),
                            ZNetScene.instance.GetPrefab("ArmorFlametalLegs"),
                            ZNetScene.instance.GetPrefab("CapeAsh"),
                        });

                        // Only warriors get weapons and shields, archers get bows
                        if (mercenaryType == MercenaryType.WarriorTier1 ||
                            mercenaryType == MercenaryType.WarriorTier2 ||
                            mercenaryType == MercenaryType.WarriorTier3 ||
                            mercenaryType == MercenaryType.WarriorTier4)
                        {
                            defaultItems.Add(ZNetScene.instance.GetPrefab("ShieldFlametal"));
                            defaultItems.Add(chosenWeapon);

                            // Save the chosen weapon for persistence
                            var flametalZdo = humanoid.m_nview.GetZDO();
                            if (flametalZdo != null)
                            {
                                flametalZdo.Set("EquippedWeapon", CleanName(chosenWeapon.name));
                                flametalZdo.Set("EquippedShield", "ShieldFlametal");
                            }
                        }
                        else if (mercenaryType == MercenaryType.ArcherTier1 ||
                                 mercenaryType == MercenaryType.ArcherTier2 ||
                                 mercenaryType == MercenaryType.ArcherTier3)
                        {
                            // Apply visual changes to AI bow appearance (preserves functionality)
                            SwapAllMercenaryBowPrefabs(false, true);

                            // Add special arrows
                            var charredArrows = ZNetScene.instance.GetPrefab("ArrowCharred");
                            if (charredArrows != null)
                            {
                                defaultItems.Add(charredArrows);
                                Logger.LogInfo("Added ArrowCharred for flametal archer");
                            }
                        }
                    }
                    else
                    {
                        // Regular BlackMetal - exactly like original
                        defaultItems.AddRange(new[]
                        {
                            ZNetScene.instance.GetPrefab("HelmetPadded"),
                            ZNetScene.instance.GetPrefab("ArmorPaddedCuirass"),
                            ZNetScene.instance.GetPrefab("ArmorPaddedGreaves"),
                            ZNetScene.instance.GetPrefab("CapeLox")
                        });
                    }

                    break;
            }

            // Use original Valheim method - this handles weapons from m_randomWeapon automatically
            humanoid.m_defaultItems = defaultItems.ToArray();

            if (BasePlugin.HeavyLogging.Value)
            {
                var equipmentStringLog = string.Join(", ", defaultItems.Select(a => a?.name ?? "null"));
                Logger.LogInfo($"Provided equipment {mercenaryType} {armorType}: {equipmentStringLog}");
            }

            // This gives all items in defaultItems + any weapons from the prefab's m_randomWeapon
            humanoid.GiveDefaultItems();
            humanoid.m_visEquipment.UpdateEquipmentVisuals();

            // Mark as having custom equipment for reload handling
            var zdo = humanoid.m_nview.GetZDO();
            if (zdo != null)
            {
                zdo.Set("HasCustomEquipment", 1);
            }
        }

        // Static dictionary to track which prefabs we've already modified
        private static Dictionary<string, bool> modifiedBowPrefabs = new Dictionary<string, bool>();

        // Visual properties container
        private struct VisualProperties
        {
            public Texture MainTexture;
            public Color Color;
            public Color EmissionColor;
            public float Metallic;
            public float Smoothness;
        }

        // SOLUTION: Enhanced AI bow visual modification using MaterialPropertyBlocks
        private static void SwapAllMercenaryBowPrefabs(bool useCarapace, bool useFlametal)
        {
            if (!useCarapace && !useFlametal) return;

            string targetBowName = useCarapace ? "BowSpineSnap" : "BowAshlands";
            string swapType = useCarapace ? "carapace" : "flametal";

            // Check if already done
            string swapKey = $"{targetBowName}_swap";
            if (modifiedBowPrefabs.ContainsKey(swapKey))
            {
                Logger.LogInfo($"Visual modification already applied for {targetBowName}");
                return;
            }

            var targetBowPrefab = ZNetScene.instance.GetPrefab(targetBowName);

            string[] mercenaryBowPrefabs =
            {
                "ChebGonaz_MercenaryBow",
                "ChebGonaz_MercenaryBow2",
                "ChebGonaz_MercenaryBow3"
            };

            foreach (string bowPrefabName in mercenaryBowPrefabs)
            {
                var mercenaryBowPrefab = ZNetScene.instance.GetPrefab(bowPrefabName);
                if (mercenaryBowPrefab == null) continue;

                bool success = false;

                // Method 1: Try MaterialPropertyBlock (best)
                if (targetBowPrefab != null)
                {
                    try
                    {
                        ApplyMaterialPropertyBlock(mercenaryBowPrefab, targetBowPrefab, targetBowName);
                        success = true;
                        Logger.LogInfo($"MaterialPropertyBlock method succeeded for {bowPrefabName}");
                    }
                    catch (System.Exception ex)
                    {
                        Logger.LogWarning($"MaterialPropertyBlock method failed for {bowPrefabName}: {ex.Message}");
                    }
                }

                // Method 2: Try shared material reference (backup)
                if (!success && targetBowPrefab != null)
                {
                    try
                    {
                        ApplySharedMaterialReference(mercenaryBowPrefab, targetBowPrefab, targetBowName);
                        success = true;
                        Logger.LogInfo($"Shared material method succeeded for {bowPrefabName}");
                    }
                    catch (System.Exception ex)
                    {
                        Logger.LogWarning($"Shared material method failed for {bowPrefabName}: {ex.Message}");
                    }
                }

                // Method 3: Color tinting (always works)
                if (!success)
                {
                    try
                    {
                        ApplyColorTinting(mercenaryBowPrefab, useCarapace, useFlametal);
                        success = true;
                        Logger.LogInfo($"Color tinting method succeeded for {bowPrefabName}");
                    }
                    catch (System.Exception ex)
                    {
                        Logger.LogError($"All methods failed for {bowPrefabName}: {ex.Message}");
                    }
                }
            }

            // Mark as completed
            modifiedBowPrefabs[swapKey] = true;
            Logger.LogInfo($"Completed visual modification using {targetBowName} for {swapType} archers");
        }

        // MaterialPropertyBlock approach - changes visuals without breaking components
        private static void ApplyMaterialPropertyBlock(GameObject aiBowPrefab, GameObject targetBowPrefab,
            string targetName)
        {
            var aiBowRenderers = aiBowPrefab.GetComponentsInChildren<Renderer>(true);
            var targetRenderers = targetBowPrefab.GetComponentsInChildren<Renderer>(true);

            if (targetRenderers.Length == 0)
            {
                Logger.LogWarning($"No renderers found in target bow {targetName}");
                return;
            }

            // Extract visual properties from target bow
            var targetProperties = ExtractVisualProperties(targetRenderers);

            if (targetProperties.MainTexture == null && targetProperties.Color == Color.white)
            {
                Logger.LogWarning($"No visual properties extracted from {targetName}");
                return;
            }

            // Apply properties to AI bow renderers using MaterialPropertyBlocks
            foreach (var renderer in aiBowRenderers)
            {
                if (renderer == null) continue;

                try
                {
                    ApplyPropertyBlockToRenderer(renderer, targetProperties, targetName);
                }
                catch (System.Exception ex)
                {
                    Logger.LogWarning($"Failed to apply properties to renderer {renderer.name}: {ex.Message}");
                }
            }

            Logger.LogInfo($"Applied MaterialPropertyBlock from {targetName} to {aiBowPrefab.name}");
        }

        // Extract visual properties from target renderers
        private static VisualProperties ExtractVisualProperties(Renderer[] targetRenderers)
        {
            var properties = new VisualProperties
            {
                MainTexture = null,
                Color = Color.white,
                EmissionColor = Color.black,
                Metallic = 0f,
                Smoothness = 0f
            };

            foreach (var renderer in targetRenderers)
            {
                if (renderer?.sharedMaterial == null) continue;

                var material = renderer.sharedMaterial;

                // Extract main texture
                if (properties.MainTexture == null && material.HasProperty("_MainTex"))
                {
                    properties.MainTexture = material.GetTexture("_MainTex");
                }

                // Extract color
                if (material.HasProperty("_Color"))
                {
                    properties.Color = material.GetColor("_Color");
                }

                // Extract emission color
                if (material.HasProperty("_EmissionColor"))
                {
                    properties.EmissionColor = material.GetColor("_EmissionColor");
                }

                // Extract metallic
                if (material.HasProperty("_Metallic"))
                {
                    properties.Metallic = material.GetFloat("_Metallic");
                }

                // Extract smoothness
                if (material.HasProperty("_Glossiness") || material.HasProperty("_Smoothness"))
                {
                    properties.Smoothness = material.HasProperty("_Smoothness")
                        ? material.GetFloat("_Smoothness")
                        : material.GetFloat("_Glossiness");
                }

                // If we found good properties, break
                if (properties.MainTexture != null) break;
            }

            return properties;
        }

        // Apply properties using MaterialPropertyBlock (doesn't break AI)
        private static void ApplyPropertyBlockToRenderer(Renderer renderer, VisualProperties properties,
            string sourceName)
        {
            var propertyBlock = new MaterialPropertyBlock();

            // Get existing properties to preserve them
            renderer.GetPropertyBlock(propertyBlock);

            bool appliedSomething = false;

            // Apply main texture
            if (properties.MainTexture != null)
            {
                propertyBlock.SetTexture("_MainTex", properties.MainTexture);
                appliedSomething = true;
            }

            // Apply color
            if (properties.Color != Color.white)
            {
                propertyBlock.SetColor("_Color", properties.Color);
                appliedSomething = true;
            }

            // Apply emission
            if (properties.EmissionColor != Color.black)
            {
                propertyBlock.SetColor("_EmissionColor", properties.EmissionColor);
                appliedSomething = true;
            }

            // Apply metallic
            if (properties.Metallic > 0f)
            {
                propertyBlock.SetFloat("_Metallic", properties.Metallic);
                appliedSomething = true;
            }

            // Apply smoothness
            if (properties.Smoothness > 0f)
            {
                if (renderer.sharedMaterial.HasProperty("_Smoothness"))
                {
                    propertyBlock.SetFloat("_Smoothness", properties.Smoothness);
                }
                else if (renderer.sharedMaterial.HasProperty("_Glossiness"))
                {
                    propertyBlock.SetFloat("_Glossiness", properties.Smoothness);
                }

                appliedSomething = true;
            }

            // Apply the property block
            if (appliedSomething)
            {
                renderer.SetPropertyBlock(propertyBlock);
                Logger.LogInfo($"Applied properties to {renderer.name} from {sourceName}");
            }
        }

        // Backup method using shared material reference (if PropertyBlock fails)
        private static void ApplySharedMaterialReference(GameObject aiBowPrefab, GameObject targetBowPrefab,
            string targetName)
        {
            var aiBowRenderers = aiBowPrefab.GetComponentsInChildren<Renderer>(true);
            var targetRenderers = targetBowPrefab.GetComponentsInChildren<Renderer>(true);

            if (targetRenderers.Length == 0) return;

            // Get the best material from target
            Material targetMaterial = null;
            foreach (var renderer in targetRenderers)
            {
                if (renderer?.sharedMaterial != null)
                {
                    targetMaterial = renderer.sharedMaterial;
                    break;
                }
            }

            if (targetMaterial == null) return;

            // Apply to AI bow renderers by updating material reference
            foreach (var renderer in aiBowRenderers)
            {
                if (renderer == null) continue;

                try
                {
                    // Create a new material array with target material
                    var newMaterials = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < newMaterials.Length; i++)
                    {
                        newMaterials[i] = targetMaterial;
                    }

                    // Use sharedMaterials to avoid breaking references
                    renderer.sharedMaterials = newMaterials;
                }
                catch (System.Exception ex)
                {
                    Logger.LogWarning($"Failed to apply shared material to {renderer.name}: {ex.Message}");
                }
            }

            Logger.LogInfo($"Applied shared material from {targetName} to {aiBowPrefab.name}");
        }

        // Color tinting approach (simplest, always works)
        private static void ApplyColorTinting(GameObject aiBowPrefab, bool useCarapace, bool useFlametal)
        {
            var renderers = aiBowPrefab.GetComponentsInChildren<Renderer>(true);

            // Define color themes
            Color tintColor;
            if (useCarapace)
            {
                tintColor = new Color(0.4f, 0.6f, 0.4f, 1f); // Green tint for carapace
            }
            else if (useFlametal)
            {
                tintColor = new Color(0.8f, 0.4f, 0.2f, 1f); // Orange/red tint for flametal
            }
            else
            {
                return;
            }

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                var propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", tintColor);
                renderer.SetPropertyBlock(propertyBlock);
            }

            string theme = useCarapace ? "carapace" : "flametal";
            Logger.LogInfo($"Applied {theme} color tint to {aiBowPrefab.name}");
        }

        // Helper method to remove "(Clone)" from prefab names
        private string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            if (name.EndsWith("(Clone)"))
                return name.Substring(0, name.Length - 7);
            return name;
        }

        // Wait for player to spawn then restore follow behavior
        private IEnumerator WaitForPlayerThenFollow()
        {
            Logger.LogInfo($"Waiting for owner '{UndeadMinionMaster}' to spawn...");

            // Wait up to 15 seconds for the player to spawn
            float waitTime = 0f;
            const float maxWaitTime = 15f;

            while (waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(1f);
                waitTime += 1f;

                var allPlayers = Player.GetAllPlayers();
                if (allPlayers.Count > 0)
                {
                    Logger.LogInfo($"Found {allPlayers.Count} players online after {waitTime}s");

                    // Check if our specific owner is online
                    var owner = allPlayers.FirstOrDefault(p => BelongsToPlayer(p.GetPlayerName()));
                    if (owner != null)
                    {
                        Logger.LogInfo($"Owner '{UndeadMinionMaster}' found! Starting to follow.");
                        RoamFollowOrWait(); // This will now find the player and start following
                        yield break;
                    }
                    // I don't like this below because it could effect PvP
                    // else if (!string.IsNullOrEmpty(UndeadMinionMaster))
                    // {
                    //     Logger.LogInfo($"Owner '{UndeadMinionMaster}' not online yet, but other players found.");
                    //     // Continue waiting for the specific owner
                    // }
                    // else
                    // {
                    //     // No specific owner set, assign to first player
                    //     var firstPlayer = allPlayers.FirstOrDefault();
                    //     if (firstPlayer != null)
                    //     {
                    //         Logger.LogInfo($"No owner set, assigning to '{firstPlayer.GetPlayerName()}'");
                    //         UndeadMinionMaster = firstPlayer.GetPlayerName();
                    //         RoamFollowOrWait();
                    //         yield break;
                    //     }
                    // }
                }
            }

            // After max wait time, if still no owner found, stay in follow state but don't roam
            //Logger.LogInfo($"Owner '{UndeadMinionMaster}' not found after {maxWaitTime}s. Staying in follow state.");
            // Don't call Roam() - keep the follow state for when owner eventually comes online
        }

        // Method to restore original bow appearance (for testing/debugging)
        public static void RestoreOriginalBowAppearance(GameObject mercenaryBowPrefab)
        {
            Logger.LogInfo($"Attempting to restore original appearance for {mercenaryBowPrefab.name}");

            var renderers = mercenaryBowPrefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                // Clear MaterialPropertyBlock to restore original appearance
                renderer.SetPropertyBlock(null);
            }

            Logger.LogInfo($"Cleared MaterialPropertyBlocks for {mercenaryBowPrefab.name}");
        }

        public static void Spawn(MercenaryType mercenaryType, ArmorType armorType, Transform spawner,
            float chanceOfFemale, List<Vector3> skinColors, List<Vector3> hairColors, bool useCarapace = false,
            bool useFlametal = false)
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
            }

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

            if (!spawnedChar.TryGetComponent(out Humanoid humanoid))
            {
                Logger.LogError("Spawn: spawnedChar has no humanoid component");
                return;
            }

            // Set skin color, hair and beard
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

            if (!spawnedChar.TryGetComponent(out HumanMinion minion))
            {
                Logger.LogError("Spawn: spawnedChar has no HumanMinion component");
                return;
            }

            // Scale equipment
            minion.ScaleEquipment(mercenaryType, armorType, useCarapace, useFlametal);

            if (mercenaryType != MercenaryType.Miner && mercenaryType != MercenaryType.Woodcutter)
                minion.Roam();

            // Handle drops on death - exactly like original
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
                        Random.value > .5f ? "DeerHide" : "LeatherScraps",
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
                    if (useCarapace)
                    {
                        AddOrUpdateDrop(characterDrop, "Carapace", MercenaryChest.ArmorCarapaceRequiredConfig.Value);
                    }
                    else if (useFlametal)
                    {
                        AddOrUpdateDrop(characterDrop, "FlametalNew", MercenaryChest.ArmorFlametalRequiredConfig.Value);
                    }
                    else
                    {
                        AddOrUpdateDrop(characterDrop, "BlackMetal", MercenaryChest.ArmorBlackIronRequiredConfig.Value);
                    }

                    break;
            }

            minion.RecordDrops(characterDrop);
        }

        // Method to reset bow prefabs if needed (for testing)
        public static void ResetAllMercenaryBowPrefabs()
        {
            modifiedBowPrefabs.Clear();
            Logger.LogInfo("Reset MercenaryBow prefab modification tracking");

            // Also restore all bow appearances
            string[] mercenaryBowPrefabs =
            {
                "ChebGonaz_MercenaryBow",
                "ChebGonaz_MercenaryBow2",
                "ChebGonaz_MercenaryBow3"
            };

            foreach (string bowPrefabName in mercenaryBowPrefabs)
            {
                var mercenaryBowPrefab = ZNetScene.instance.GetPrefab(bowPrefabName);
                if (mercenaryBowPrefab != null)
                {
                    RestoreOriginalBowAppearance(mercenaryBowPrefab);
                }
            }
        }
    }
}