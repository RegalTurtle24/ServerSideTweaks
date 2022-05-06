using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using R2API;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion(ServerSideTweaks.ServerSideTweaksPlugin.Version)]
namespace ServerSideTweaks
{
    [BepInDependency(InLobbyConfigIntegration.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency("com.bepis.r2api")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(LanguageAPI), nameof(RecalculateStatsAPI), nameof(ItemAPI), nameof(EliteAPI), nameof(ContentAddition))]
    public class ServerSideTweaksPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.RegalTurtle.ServerSideTweaks";
        public const string Name = "Server Side Tweaks";
        public const string Version = "1.0.0";

        private static int runMountainCount = 0;
        private static int stageMountainCount = 0;

        private static readonly HashSet<NetworkUser> votedForMetamorphosis = new HashSet<NetworkUser>();

        internal static ConfigEntry<bool> IsEnabled { get; set; }

        internal static ConfigEntry<bool> ArroganceEnabled { get; set; }
        internal static ConfigEntry<int> AddedMountainShrinesPerStage { get; set; }
        internal static ConfigEntry<int> MultMountainShrinesPerStage { get; set; }

        internal static ConfigEntry<bool> InfiniteMountainEnabled { get; set; }
        internal static ConfigEntry<int> InfiniteMountainPurchaseLimit { get; set; }


        public void Start()
        {
            IsEnabled = Config.Bind("Main", "enabled", true, "Is mod enabled");

            ArroganceEnabled = Config.Bind("ArtifactOfArrogance", nameof(ArroganceEnabled), true, "Is Artifact of Arrogance enabled");
            AddedMountainShrinesPerStage = Config.Bind("ArtifactOfArrogance", nameof(AddedMountainShrinesPerStage), 0, "How many mountain shrines to add per stage");
            MultMountainShrinesPerStage = Config.Bind("ArtifactOfArrogance", nameof(MultMountainShrinesPerStage), 1, "How many times to multiply mountain shrines each stage");

            InfiniteMountainEnabled = Config.Bind("InfiniteMountain", nameof(InfiniteMountainEnabled), true, "Is infinite mountain shrines enabled");
            InfiniteMountainPurchaseLimit = Config.Bind("InfiniteMountain", nameof(InfiniteMountainPurchaseLimit), 1, "How many times can you hit 1 mountain shrine");

            InLobbyConfigIntegration.OnStart();
        }

        public void Destroy()
        {
            InLobbyConfigIntegration.OnDestroy();
        }

        public void Awake()
        {
            On.RoR2.ShrineBossBehavior.AddShrineStack += (orig, self, interactor) =>
            {
                if (ArroganceEnabled.Value)
                {
                    stageMountainCount++;
                }

                if (InfiniteMountainEnabled.Value)
                {
                    self.GetComponent<ShrineBossBehavior>().maxPurchaseCount = InfiniteMountainPurchaseLimit.Value;
                }

                orig(self, interactor);
            };

            On.RoR2.Run.Start += (orig, self) =>
            {
                runMountainCount = 0;
                stageMountainCount = 0;

                if (InfiniteMountainPurchaseLimit.Value < 0 || InfiniteMountainPurchaseLimit.Value > 2147483647)
                {
                    InfiniteMountainPurchaseLimit.Value = 2147483647;
                }

                orig(self);
            };

            On.RoR2.Stage.Start += (orig, self) =>
            {
                if (ArroganceEnabled.Value)
                {
                    runMountainCount += (stageMountainCount + AddedMountainShrinesPerStage.Value) * MultMountainShrinesPerStage.Value;
                    stageMountainCount = 0;
                    orig(self);

                    if (TeleporterInteraction.instance)
                    {
                        for (int i = 0; i < runMountainCount; i++)
                        {
                            TeleporterInteraction.instance.AddShrineStack();
                        }
                    }
                }
            };
        }
    }
}