using System.Collections;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Configuration;
using ChebsMercenaries.Commands.PvP;
using ChebsMercenaries.Items;
using ChebsMercenaries.Minions;
using ChebsMercenaries.Structure;
using ChebsValheimLibrary;
using ChebsValheimLibrary.PvP;
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
        public const string PluginVersion = "2.4.0";
        private const string ConfigFileName = PluginGuid + ".cfg";
        private static readonly string ConfigFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

        public readonly System.Version ChebsValheimLibraryVersion = new("2.6.1");

        private readonly Harmony harmony = new(PluginGuid);

        private List<WeaponOfCommand> _weaponsOfCommand = new()
        {
            new AxeOfCommand(),
            new MaceOfCommand(),
            new SwordOfCommand(),
        };
        
        public static ConfigEntry<bool> PvPAllowed;

        // if set to true, the particle effects that for some reason hurt radeon are dynamically disabled
        public static ConfigEntry<bool> RadeonFriendly, HeavyLogging;

        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        public static readonly List<string> MercenaryPrefabPaths = new()
        {
            "ChebGonaz_HumanMiner.prefab",
            "ChebGonaz_HumanWoodcutter.prefab",
            "ChebGonaz_HumanArcher.prefab",
            "ChebGonaz_HumanArcherTier2.prefab",
            "ChebGonaz_HumanArcherTier3.prefab",
            "ChebGonaz_HumanWarrior.prefab",
            "ChebGonaz_HumanWarriorTier2.prefab",
            "ChebGonaz_HumanWarriorTier3.prefab",
            "ChebGonaz_HumanWarriorTier4.prefab",

            "ChebGonaz_HumanMinerFemale.prefab",
            "ChebGonaz_HumanWoodcutterFemale.prefab",
            "ChebGonaz_HumanArcherFemale.prefab",
            "ChebGonaz_HumanArcherTier2Female.prefab",
            "ChebGonaz_HumanArcherTier3Female.prefab",
            "ChebGonaz_HumanWarriorFemale.prefab",
            "ChebGonaz_HumanWarriorTier2Female.prefab",
            "ChebGonaz_HumanWarriorTier3Female.prefab",
            "ChebGonaz_HumanWarriorTier4Female.prefab",
            
            "ChebGonaz_Catapult.prefab",
        };

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
            
            PvPAllowed = Config.Bind("General (Server Synced)", "PvPAllowed",
                false, new ConfigDescription("Whether minions will target and attack other players and their minions.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            RadeonFriendly = Config.Bind("General (Client)", "RadeonFriendly",
                false, new ConfigDescription("ONLY set this to true if you have graphical issues with " +
                                             "the mod. It will disable all particle effects for the mod's prefabs " +
                                             "which seem to give users with Radeon cards trouble for unknown " +
                                             "reasons. If you have problems with lag it might also help to switch" +
                                             "this setting on."));
            
            HeavyLogging = Config.Bind("General (Client)", "HeavyLogging",
                false, new ConfigDescription("Turn this on for debugging. Lots of things will get logged."));

            MercenaryMinion.CreateConfigs(this);
            
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
            
            CatapultMinion.CreateConfigs(this);

            MercenaryChest.CreateConfigs(this);

            _weaponsOfCommand.ForEach(w => w.CreateConfigs(this));
        }

        #endregion

        #region ConfigUpdate
        private byte[] GetFileHash(string fileName)
        {
            var sha1 = HashAlgorithm.Create();
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            return sha1.ComputeHash(stream);
        }

        private IEnumerator WatchConfigFile()
        {
            var lastHash = GetFileHash(ConfigFileFullPath);
            while (true)
            {
                yield return new WaitForSeconds(5);
                var hash = GetFileHash(ConfigFileFullPath);
                if (!hash.SequenceEqual(lastHash))
                {
                    lastHash = hash;
                    ReadConfigValues();
                }
            }
        }
        
        private void ReadConfigValues()
        {
            try
            {
                var adminOrLocal = ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance();
                Logger.LogInfo($"Read updated config values (admin/local={adminOrLocal})");
                if (adminOrLocal) Config.Reload();
                MercenaryChest.UpdateRecipe();
                _weaponsOfCommand.ForEach(w => w.UpdateRecipe());
            }
            catch (Exception exc)
            {
                Logger.LogError($"There was an issue loading your {ConfigFileName}: {exc}");
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
            PvPManager.ConfigureRPC();
            LoadChebGonazAssetBundle();
            harmony.PatchAll();
            
            var pvpCommands = new List<ConsoleCommand>()
                { new PvPAddFriend(), new PvPRemoveFriend(), new PvPListFriends() };
            foreach (var pvpCommand in pvpCommands)
            {
                if (!CommandManager.Instance.CustomCommands
                        .ToList().Exists(c => c.Name == pvpCommand.Name))
                    CommandManager.Instance.AddConsoleCommand(pvpCommand);
            }

            SynchronizationManager.OnConfigurationSynchronized += (obj, attr) =>
            {
                Logger.LogInfo(!attr.InitialSynchronization
                    ? "Syncing configuration changes from server..."
                    : "Syncing initial configuration...");
                StartCoroutine(RequestPvPDict());
            };

            StartCoroutine(WatchConfigFile());
        }
        
        private IEnumerator RequestPvPDict()
        {
            yield return new WaitUntil(() => ZNet.instance != null && Player.m_localPlayer != null);
            PvPManager.InitialFriendsListRequest();
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
                MercenaryPrefabPaths.ForEach(prefabName =>
                {
                    if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Loading prefab {prefabName}...");
                    
                    var prefab = Base.LoadPrefabFromBundle(prefabName, chebgonazAssetBundle, RadeonFriendly.Value);
                    switch (prefabName)
                    {
                        case "ChebGonaz_HumanMiner.prefab":
                        case "ChebGonaz_HumanMinerFemale.prefab":
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding HumanMinerMinion component to {prefabName}.");
                            prefab.AddComponent<HumanMinerMinion>();
                            break;
                        case "ChebGonaz_HumanWoodcutter.prefab":
                        case "ChebGonaz_HumanWoodcutterFemale.prefab":
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding HumanWoodcutterMinion component to {prefabName}.");
                            prefab.AddComponent<HumanWoodcutterMinion>();
                            break;
                        
                        case "ChebGonaz_HumanWarrior.prefab":
                        case "ChebGonaz_HumanWarriorFemale.prefab":
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding MercenaryWarriorTier1Minion component to {prefabName}.");
                            prefab.AddComponent<MercenaryWarriorTier1Minion>();
                            break;
                        case "ChebGonaz_HumanWarriorTier2.prefab":
                        case "ChebGonaz_HumanWarriorTier2Female.prefab":
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding MercenaryWarriorTier2Minion component to {prefabName}.");
                            prefab.AddComponent<MercenaryWarriorTier2Minion>();
                            break;
                        case "ChebGonaz_HumanWarriorTier3.prefab":
                        case "ChebGonaz_HumanWarriorTier3Female.prefab":
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding MercenaryWarriorTier3Minion component to {prefabName}.");
                            prefab.AddComponent<MercenaryWarriorTier3Minion>();
                            break;
                        case "ChebGonaz_HumanWarriorTier4.prefab":
                        case "ChebGonaz_HumanWarriorTier4Female.prefab":
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding MercenaryWarriorTier4Minion component to {prefabName}.");
                            prefab.AddComponent<MercenaryWarriorTier4Minion>();
                            break;
                        
                        case "ChebGonaz_HumanArcher.prefab":
                        case "ChebGonaz_HumanArcherFemale.prefab":
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding MercenaryArcherTier1Minion component to {prefabName}.");
                            prefab.AddComponent<MercenaryArcherTier1Minion>();
                            break;
                        case "ChebGonaz_HumanArcherTier2.prefab":
                        case "ChebGonaz_HumanArcherTier2Female.prefab":
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding MercenaryArcherTier2Minion component to {prefabName}.");
                            prefab.AddComponent<MercenaryArcherTier2Minion>();
                            break;
                        case "ChebGonaz_HumanArcherTier3.prefab":
                        case "ChebGonaz_HumanArcherTier3Female.prefab":
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding MercenaryArcherTier3Minion component to {prefabName}.");
                            prefab.AddComponent<MercenaryArcherTier3Minion>();
                            break;
                        
                        case "ChebGonaz_Catapult.prefab":
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding CatapultMinion component to {prefabName}.");
                            prefab.AddComponent<CatapultMinion>();
                            break;

                        default:
                            if (HeavyLogging.Value) Jotunn.Logger.LogInfo($"Adding HumanMinion component to {prefabName}.");
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