using BepInEx.Configuration;
using ChebsMercenaries.Structure;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using UnityEngine;
using Logger = Jotunn.Logger;
using Random = System.Random;

namespace ChebsMercenaries.Minions
{
    internal class CatapultMinion : MercenaryMinion
    {
        public new static ConfigEntry<float> Health;
        public static MemoryConfigEntry<string, List<string>> ItemsCost;

        public new static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSynced = "CatapultMinion (Server Synced)";

            var itemsCost = plugin.ModConfig(serverSynced, "ItemsCost", "Wood:25,RoundLog:5,Bronze:1",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount. Alternative items can be specified with a | eg. Wood|Coal:5 to mean wood and/or coal.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').Select(str => str.Trim()).ToList());
            Health = plugin.Config.Bind(serverSynced, "Health",
                250f, new ConfigDescription("How much health the mercenary has.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
        
        public sealed override void Awake()
        {
            base.Awake();
            
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
        
        public static void Spawn(Transform spawner)
        {
            if (ZNetScene.instance == null)
            {
                Logger.LogWarning("Spawn: ZNetScene.instance is null, trying again later...");
                return;
            }
            
            var prefab = ZNetScene.instance.GetPrefab(PrefabNames[MercenaryType.Catapult]);
            if (!prefab)
            {
                Logger.LogError($"Spawn: spawning catapult failed - can't find prefab");
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

            if (!spawnedChar.TryGetComponent(out ChebGonazMinion minion))
            {
                Logger.LogError("Spawn: spawnedChar has no ChebGonazMinion component");
                return;
            }
            
            minion.Roam();

            // handle refunding of resources on death
            if (DropOnDeath.Value == DropType.Nothing) return;

            var characterDrop = spawnedChar.AddComponent<CharacterDrop>();
            if (DropOnDeath.Value == DropType.Everything)
            {
                GenerateDeathDrops(characterDrop, ItemsCost);
            }

            // the component won't be remembered by the game on logout because
            // only what is on the prefab is remembered. Even changes to the prefab
            // aren't remembered. So we must write what we're dropping into
            // the ZDO as well and then read & restore this on Awake
            minion.RecordDrops(characterDrop);
        }
    }
}