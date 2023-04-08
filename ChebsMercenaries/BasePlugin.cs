﻿using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using ChebsMercenaries.Minions;
using ChebsMercenaries.Structure;
using ChebsNecromancy.Minions;
using ChebsValheimLibrary.Minions.AI;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using Paths = BepInEx.Paths;

namespace ChebsMercenaries
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class BasePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.chebgonaz.chebsmercenaries";
        public const string PluginName = "ChebsMercenaries";
        public const string PluginVersion = "0.0.1";
        private const string ConfigFileName =  PluginGuid + ".cfg";
        private static readonly string ConfigFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

        private readonly Harmony harmony = new(PluginGuid);
        
        // if set to true, the particle effects that for some reason hurt radeon are dynamically disabled
        public static ConfigEntry<bool> RadeonFriendly;
        
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        
        #region ConfigStuff
        // Global Config Acceptable Values
        public AcceptableValueList<bool> BoolValue = new(true, false);
        public AcceptableValueRange<float> FloatQuantityValue = new(1f, 1000f);
        public AcceptableValueRange<int> IntQuantityValue = new(1, 1000);
        
        public ConfigEntry<T> ModConfig<T>(
            string group,
            string name,
            T default_value,
            string description = "",
            AcceptableValueBase acceptableValues = null,
            bool serverSync = false,
            params object[] tags)
        {
            // Create extended description with list of valid values and server sync
            ConfigDescription extendedDescription = new(
                description + (serverSync
                    ? " [Synced with Server]"
                    : " [Not Synced with Server]"),
                acceptableValues,
                new ConfigurationManagerAttributes { IsAdminOnly = serverSync },
                tags);

            var configEntry = Config.Bind(group, name, default_value, extendedDescription);

            return configEntry;
        }

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            RadeonFriendly = Config.Bind("General (Client)", "RadeonFriendly",
                false, new ConfigDescription("ONLY set this to true if you have graphical issues with " +
                                             "the mod. It will disable all particle effects for the mod's prefabs " +
                                             "which seem to give users with Radeon cards trouble for unknown " +
                                             "reasons. If you have problems with lag it might also help to switch" +
                                             "this setting on."));

            HumanMinion.CreateConfigs(this);
            HumanWoodcutterMinion.CreateConfigs(this);
            HumanMinerMinion.CreateConfigs(this);
            
            MercenaryChest.CreateConfigs(this);
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.Error += (sender, e) => Jotunn.Logger.LogError($"Error watching for config changes: {e}");
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                Logger.LogInfo("Read updated config values");
                Config.Reload();
                
                HumanMinerMinion.SyncInternalsWithConfigs();
                HumanWoodcutterMinion.SyncInternalsWithConfigs();
                MercenaryChest.ParseMercCosts();
            }
            catch (Exception exc)
            {
                Logger.LogError($"There was an issue loading your {ConfigFileName}: {exc}");
                Logger.LogError("Please check your config entries for spelling and format!");
            }
        }
        #endregion
        
        private void Awake()
        {
            CreateConfigValues();
            LoadChebGonazAssetBundle();
            harmony.PatchAll();
            SetupWatcher();
        }
        
        private void LoadChebGonazAssetBundle()
        {
            // order is important (I think): items, creatures, structures
            var assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "chebgonaz");
            var chebgonazAssetBundle = AssetUtils.LoadAssetBundle(assetBundlePath);
            try
            {
                GameObject LoadPrefabFromBundle(string prefabName, AssetBundle bundle)
                {
                    var prefab = bundle.LoadAsset<GameObject>(prefabName);
                    if (prefab == null)
                    {
                        Jotunn.Logger.LogFatal($"LoadPrefabFromBundle: {prefabName} is null!");
                    }

                    if (RadeonFriendly.Value)
                    {
                        foreach (var child in prefab.GetComponentsInChildren<ParticleSystem>())
                        {
                            Destroy(child);
                        }
                    
                        if (prefab.TryGetComponent(out Humanoid humanoid))
                        {
                            humanoid.m_deathEffects = new EffectList();
                            humanoid.m_dropEffects = new EffectList();
                            humanoid.m_equipEffects = new EffectList();
                            humanoid.m_pickupEffects = new EffectList();
                            humanoid.m_consumeItemEffects = new EffectList();
                            humanoid.m_hitEffects = new EffectList();
                            humanoid.m_jumpEffects = new EffectList();
                            humanoid.m_slideEffects = new EffectList();
                            humanoid.m_perfectBlockEffect = new EffectList();
                            humanoid.m_tarEffects = new EffectList();
                            humanoid.m_waterEffects = new EffectList();
                            humanoid.m_flyingContinuousEffect = new EffectList();
                        }
                    }
                    
                    return prefab;
                }

                #region Creatures
                var prefabNames = new List<string>();

                prefabNames.Add("ChebGonaz_HumanMiner.prefab");
                prefabNames.Add("ChebGonaz_HumanWoodcutter.prefab");
                prefabNames.Add("ChebGonaz_HumanArcher.prefab");
                prefabNames.Add("ChebGonaz_HumanArcherTier2.prefab");
                prefabNames.Add("ChebGonaz_HumanArcherTier3.prefab");
                prefabNames.Add("ChebGonaz_HumanWarrior.prefab");
                prefabNames.Add("ChebGonaz_HumanWarriorTier2.prefab");
                prefabNames.Add("ChebGonaz_HumanWarriorTier3.prefab");
                prefabNames.Add("ChebGonaz_HumanWarriorTier4.prefab");

                prefabNames.ForEach(prefabName =>
                {
                    var prefab = LoadPrefabFromBundle(prefabName, chebgonazAssetBundle);
                    CreatureManager.Instance.AddCreature(new CustomCreature(prefab, true));
                });
                #endregion
                #region Structures
                var mercenaryChestPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(MercenaryChest.ChebsRecipeConfig.PrefabName);
                PieceManager.Instance.AddPiece(
                    MercenaryChest.ChebsRecipeConfig.GetCustomPieceFromPrefab(mercenaryChestPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(MercenaryChest.ChebsRecipeConfig.IconName))
                );
                #endregion
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while loading assets: {ex}");
            }
            finally
            {
                chebgonazAssetBundle.Unload(false);
            }
        }
    }
}