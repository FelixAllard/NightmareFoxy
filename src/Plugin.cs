using System.Reflection;
using UnityEngine;
using BepInEx;
using LethalLib.Modules;
using BepInEx.Logging;
using System.IO;
using HarmonyLib;
using NightmareFoxyLC.Configurations;

namespace NightmareFoxyLC {
    [BepInPlugin(ModGUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    
    
    public class Plugin : BaseUnityPlugin {
        // It is a good idea for our GUID to be more unique than only the plugin name. Notice that it is used in the BepInPlugin attribute.
        // The GUID is also used for the config file name by default.
        public const string ModGUID = "Xilef992." + PluginInfo.PLUGIN_NAME;
        internal static new ManualLogSource Logger;
        public static AssetBundle ModAssets; 
        public static new Config FoxyConfig { get; internal set; }
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        private void Awake() {
            Logger = base.Logger;

            Debug.Log("LOADING NIGHTMARE FOXY : " + ModGUID + " " + PluginInfo.PLUGIN_NAME);
            
            FoxyConfig = new(base.Config);
            
            //_configuration = new FoxyConfig(Config);

            // If you don't want your mod to use a configuration file, you can remove this line, Configuration.cs, and other references.

            // This should be ran before Network Prefabs are registered.
            InitializeNetworkBehaviours();

            // We load the asset bundle that should be next to our DLL file, with the specified name.
            // You may want to rename your asset bundle from the AssetBundle Browser in order to avoid an issue with
            // asset bundle identifiers being the same between multiple bundles, allowing the loading of only one bundle from one mod.
            // In that case also remember to change the asset bundle copying code in the csproj.user file.
            var bundleName = "foxymodasset";
            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), bundleName));
            if (ModAssets == null) {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }

            // We load our assets from our asset bundle. Remember to rename them both here and in our Unity project.
            var FoxyEnemy = ModAssets.LoadAsset<EnemyType>("Foxy");
            var FoxyTN = ModAssets.LoadAsset<TerminalNode>("FoxyTN");
            var FoxyTk = ModAssets.LoadAsset<TerminalKeyword>("FoxyTK");
            
            // Network Prefabs need to be registered. See https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
            // LethalLib registers prefabs on GameNetworkManager.Start.
            NetworkPrefabs.RegisterNetworkPrefab(FoxyEnemy.enemyPrefab);
			Enemies.RegisterEnemy(FoxyEnemy, FoxyConfig.RARITY.Value, Levels.LevelTypes.All, Enemies.SpawnType.Default, FoxyTN, FoxyTk);
            Debug.Log("\n                     \u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591                     \n                 \u2591\u2591\u2591\u2592\u2592\u2592\u2592\u2592\u2592\u2592\u2592\u2592\u2592\u2592\u2592\u2591\u2591\u2591                 \n               \u2591\u2591\u2592\u2592\u2592\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2592\u2592\u2591\u2591\u2591              \n             \u2591\u2591\u2592\u2592\u2593\u2593\u2593\u2593\u2593\u2592\u2592\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2592\u2592\u2593\u2593\u2593\u2592\u2592\u2591             \n            \u2591\u2592\u2592\u2593\u2593\u2593\u2593\u2592\u2592\u2591           \u2591\u2591\u2592\u2593\u2593\u2592\u2592\u2591           \n           \u2591\u2592\u2592\u2593\u2593\u2593\u2592\u2592\u2591                \u2591\u2591\u2592\u2593\u2592\u2591          \n          \u2591\u2592\u2592\u2593\u2593\u2593\u2592\u2591\u2591                                 \n          \u2591\u2592\u2593\u2593\u2593\u2593\u2592\u2591                                  \n          \u2591\u2592\u2593\u2593\u2593\u2592\u2592\u2591                                  \n          \u2591\u2592\u2593\u2593\u2593\u2592\u2592\u2591                                  \n          \u2591\u2592\u2593\u2593\u2593\u2592\u2592\u2592\u2591                                 \n           \u2591\u2592\u2593\u2593\u2593\u2592\u2592\u2592\u2591                                \n           \u2591\u2592\u2593\u2593\u2593\u2593\u2592\u2592\u2592\u2591                               \n            \u2591\u2592\u2593\u2593\u2593\u2593\u2592\u2592\u2592\u2591\u2591                             \n              \u2591\u2592\u2593\u2593\u2593\u2593\u2592\u2591\u2592\u2591\u2591                           \n               \u2591\u2592\u2592\u2593\u2593\u2593\u2593\u2592\u2592\u2592\u2591                          \n                 \u2591\u2592\u2592\u2593\u2593\u2593\u2592\u2592\u2592\u2591                         \n                   \u2592\u2592\u2593\u2593\u2593\u2592\u2592\u2592\u2591                        \n                    \u2592\u2593\u2593\u2593\u2593\u2592\u2592\u2591                        \n                    \u2591\u2592\u2593\u2593\u2593\u2593\u2592\u2592                        \n                   \u2591\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2593\u2592\u2591\u2591                     \n                \u2591\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2593\u2592\u2591                  \n              \u2591\u2592\u2593\u2588\u2588\u2593\u2593\u2593\u2588\u2588\u2588\u2588\u2593\u2593\u2593\u2593\u2593\u2588\u2588\u2593\u2592\u2591                \n             \u2591\u2592\u2593\u2588\u2593\u2593\u2593\u2593\u2593\u2588\u2588\u2588\u2588\u2593\u2593\u2593\u2593\u2593\u2593\u2588\u2588\u2588\u2592\u2591               \n             \u2591\u2593\u2593\u2593\u2593\u2593\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2593\u2593\u2593\u2593\u2588\u2588\u2588\u2588\u2592\u2591              \n            \u2591\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2593\u2593\u2593\u2588\u2588\u2588\u2588\u2593\u2591              \n           \u2591\u2591\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2591             \n           \u2591\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2591             \n           \u2591\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2592             \n           \u2591\u2593\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2593\u2591            \n           \u2591\u2593\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2593\u2591            \n          \u2591\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2591            \n          \u2591\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2592            \n          \u2591\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2593\u2591           \n          \u2591\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2591           \n          \u2591\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2591           \n          \u2591\u2592\u2593\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2593\u2592\u2591            \n              \u2591\u2591\u2592\u2592\u2592\u2592\u2593\u2593\u2593\u2593\u2593\u2593\u2593\u2592\u2592\u2592\u2592\u2591\u2591\u2591\u2591                 ");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            harmony.PatchAll(typeof(ConfigurationsPatch));
        }

        private static void InitializeNetworkBehaviours() {
            // See https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        } 
    }
    
}