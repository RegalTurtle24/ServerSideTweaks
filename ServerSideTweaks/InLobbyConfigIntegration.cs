using BepInEx.Bootstrap;
using InLobbyConfig;
using InLobbyConfig.Fields;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ServerSideTweaks
{
    public static class InLobbyConfigIntegration
    {
        public const string GUID = "com.KingEnderBrine.InLobbyConfig";
        private static bool Enabled => Chainloader.PluginInfos.ContainsKey(GUID);
        private static object ModConfig { get; set; }

        public static void OnStart()
        {
            if (Enabled)
            {
                OnStartInternal();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnStartInternal()
        {
            var modConfig = new ModConfigEntry
            {
                DisplayName = "Server Side Tweaks",
                EnableField = new BooleanConfigField("", () => ServerSideTweaksPlugin.IsEnabled.Value, (newValue) => ServerSideTweaksPlugin.IsEnabled.Value = newValue),

            };

            modConfig.SectionFields["Artifact of Arrogance"] = new List<IConfigField>
            {
                ConfigFieldUtilities.CreateFromBepInExConfigEntry(ServerSideTweaksPlugin.ArroganceEnabled),
                ConfigFieldUtilities.CreateFromBepInExConfigEntry(ServerSideTweaksPlugin.AddedMountainShrinesPerStage),
                ConfigFieldUtilities.CreateFromBepInExConfigEntry(ServerSideTweaksPlugin.MultMountainShrinesPerStage)
            };

            modConfig.SectionFields["Infinite Mountain Shrines"] = new List<IConfigField>
            {
                ConfigFieldUtilities.CreateFromBepInExConfigEntry(ServerSideTweaksPlugin.InfiniteMountainEnabled),
                ConfigFieldUtilities.CreateFromBepInExConfigEntry(ServerSideTweaksPlugin.InfiniteMountainPurchaseLimit)
            };


            ModConfigCatalog.Add(modConfig);
            ModConfig = modConfig;
        }
        public static void OnDestroy()
        {
            if (Enabled)
            {
                OnDestroyInternal();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnDestroyInternal()
        {
            ModConfigCatalog.Remove(ModConfig as ModConfigEntry);
            ModConfig = null;
        }
    }
}