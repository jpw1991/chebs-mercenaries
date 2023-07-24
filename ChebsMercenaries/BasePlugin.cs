using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using ChebsMercenaries.Items;
using ChebsMercenaries.Minions;
using ChebsMercenaries.Structure;
using ChebsValheimLibrary;
using HarmonyLib;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using Paths = BepInEx.Paths;

namespace ChebsMercenaries
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class BasePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.chebgonaz.chebsmercenaries";
        public const string PluginName = "ChebsMercenaries";
        public const string PluginVersion = "1.7.0";
        private const string ConfigFileName = PluginGuid + ".cfg";
        private static readonly string ConfigFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

        public readonly System.Version ChebsValheimLibraryVersion = new("2.1.2");

        private readonly Harmony harmony = new(PluginGuid);

        private List<WeaponOfCommand> _weaponsOfCommand = new()
        {
            new AxeOfCommand(),
            new MaceOfCommand(),
            new SwordOfCommand(),
        };

        // if set to true, the particle effects that for some reason hurt radeon are dynamically disabled
        public static ConfigEntry<bool> RadeonFriendly;

        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        #region ConfigStuff

        // Global Config Acceptable Values
        public AcceptableValueList<bool> BoolValue = new(true, false);
        public AcceptableValueRange<float> FloatQuantityValue = new(1f, 1000f);
        public AcceptableValueRange<int> IntQuantityValue = new(1, 1000);

        private double _inputDelay;

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

            MercenaryArcherTier1Minion.CreateConfigs(this);
            MercenaryArcherTier2Minion.CreateConfigs(this);
            MercenaryArcherTier3Minion.CreateConfigs(this);
            MercenaryWarriorTier1Minion.CreateConfigs(this);
            MercenaryWarriorTier2Minion.CreateConfigs(this);
            MercenaryWarriorTier3Minion.CreateConfigs(this);
            MercenaryWarriorTier4Minion.CreateConfigs(this);

            MercenaryChest.CreateConfigs(this);

            _weaponsOfCommand.ForEach(w => w.CreateConfigs(this));
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
                MercenaryChest.UpdateRecipe();

                _weaponsOfCommand.ForEach(w => w.UpdateRecipe());
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
            if (!Base.VersionCheck(ChebsValheimLibraryVersion, out string message))
            {
                Jotunn.Logger.LogWarning(message);
            }

            CreateConfigValues();
            LoadChebGonazAssetBundle();
            harmony.PatchAll();

            HumanMinerMinion.SyncInternalsWithConfigs();
            HumanWoodcutterMinion.SyncInternalsWithConfigs();

            SetupWatcher();
        }

        private void LoadChebGonazAssetBundle()
        {
            // order is important (I think): items, creatures, structures
            var assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "chebgonaz");
            var chebgonazAssetBundle = AssetUtils.LoadAssetBundle(assetBundlePath);
            try
            {
                #region Items

                Base.LoadMinionItems(chebgonazAssetBundle, RadeonFriendly.Value);
                _weaponsOfCommand.ForEach(w =>
                {
                    var prefab = Base.LoadPrefabFromBundle(w.PrefabName, chebgonazAssetBundle, RadeonFriendly.Value);
                    w.CreateButtons();
                    KeyHintManager.Instance.AddKeyHint(w.GetKeyHint());
                    ItemManager.Instance.AddItem(w.GetCustomItemFromPrefab(prefab));
                });

                #endregion

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

                prefabNames.Add("ChebGonaz_HumanMinerFemale.prefab");
                prefabNames.Add("ChebGonaz_HumanWoodcutterFemale.prefab");
                prefabNames.Add("ChebGonaz_HumanArcherFemale.prefab");
                prefabNames.Add("ChebGonaz_HumanArcherTier2Female.prefab");
                prefabNames.Add("ChebGonaz_HumanArcherTier3Female.prefab");
                prefabNames.Add("ChebGonaz_HumanWarriorFemale.prefab");
                prefabNames.Add("ChebGonaz_HumanWarriorTier2Female.prefab");
                prefabNames.Add("ChebGonaz_HumanWarriorTier3Female.prefab");
                prefabNames.Add("ChebGonaz_HumanWarriorTier4Female.prefab");

                prefabNames.ForEach(prefabName =>
                {
                    var prefab = Base.LoadPrefabFromBundle(prefabName, chebgonazAssetBundle, RadeonFriendly.Value);
                    switch (prefabName)
                    {
                        case "ChebGonaz_HumanMiner.prefab":
                        case "ChebGonaz_HumanMinerFemale.prefab":
                            prefab.AddComponent<HumanMinerMinion>();
                            break;
                        case "ChebGonaz_HumanWoodcutter.prefab":
                        case "ChebGonaz_HumanWoodcutterFemale.prefab":
                            prefab.AddComponent<HumanWoodcutterMinion>();
                            break;
                        
                        case "ChebGonaz_HumanWarrior.prefab":
                        case "ChebGonaz_HumanWarriorFemale.prefab":
                            prefab.AddComponent<MercenaryWarriorTier1Minion>();
                            break;
                        case "ChebGonaz_HumanWarriorTier2.prefab":
                        case "ChebGonaz_HumanWarriorTier2Female.prefab":
                            prefab.AddComponent<MercenaryWarriorTier2Minion>();
                            break;
                        case "ChebGonaz_HumanWarriorTier3.prefab":
                        case "ChebGonaz_HumanWarriorTier3Female.prefab":
                            prefab.AddComponent<MercenaryWarriorTier3Minion>();
                            break;
                        case "ChebGonaz_HumanWarriorTier4.prefab":
                        case "ChebGonaz_HumanWarriorTier4Female.prefab":
                            prefab.AddComponent<MercenaryWarriorTier4Minion>();
                            break;
                        
                        case "ChebGonaz_HumanArcher.prefab":
                        case "ChebGonaz_HumanArcherFemale.prefab":
                            prefab.AddComponent<MercenaryArcherTier1Minion>();
                            break;
                        case "ChebGonaz_HumanArcherTier2.prefab":
                        case "ChebGonaz_HumanArcherTier2Female.prefab":
                            prefab.AddComponent<MercenaryArcherTier2Minion>();
                            break;
                        case "ChebGonaz_HumanArcherTier3.prefab":
                        case "ChebGonaz_HumanArcherTier3Female.prefab":
                            prefab.AddComponent<MercenaryArcherTier3Minion>();
                            break;

                        default:
                            prefab.gameObject.AddComponent<HumanMinion>();
                            break;
                    }
                    CreatureManager.Instance.AddCreature(new CustomCreature(prefab, true));
                });

                #endregion

                #region Structures

                var mercenaryChestPrefab =
                    chebgonazAssetBundle.LoadAsset<GameObject>(MercenaryChest.ChebsRecipeConfig.PrefabName);
                mercenaryChestPrefab.AddComponent<MercenaryChest>();
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

        private void Update()
        {
            if (ZInput.instance != null)
            {
                if (Time.time > _inputDelay)
                {
                    _weaponsOfCommand.ForEach(w =>
                    {
                        if (w.HandleInputs())
                        {
                            _inputDelay = Time.time + .5f;
                        }
                    });
                }
            }
        }
    }
}