using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using ChebsMercenaries.Minions;
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
    public class ChebsMercenaries : BaseUnityPlugin
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

        private void Awake()
        {
            CreateConfigValues();
            LoadChebGonazAssetBundle();
            harmony.PatchAll();
            SetupWatcher();
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
            }
            catch (Exception exc)
            {
                Logger.LogError($"There was an issue loading your {ConfigFileName}: {exc}");
                Logger.LogError("Please check your config entries for spelling and format!");
            }
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