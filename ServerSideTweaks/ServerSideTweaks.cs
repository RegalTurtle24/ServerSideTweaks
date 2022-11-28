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

using UnityEngine.Networking;
using PickupIndex = RoR2.PickupIndex;
using CommandArtifactManager = RoR2.Artifacts.CommandArtifactManager;
using PickupTransmutationManager = RoR2.PickupTransmutationManager;
using PickupDropletController = RoR2.PickupDropletController;
using UnityEngine.XR;

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
        public const string Version = "1.1.2";

        private static readonly MethodInfo startRun = typeof(PreGameController).GetMethod(nameof(PreGameController.StartRun), BindingFlags.NonPublic | BindingFlags.Instance);

        private static int runMountainCount = 0;
        private static int stageMountainCount = 0;

        private static Interactor lastInteractor = null;

        private static readonly HashSet<String> votedForCommand = new HashSet<String>();

        internal static ConfigEntry<bool> IsEnabled { get; set; }

        internal static ConfigEntry<bool> ArroganceEnabled { get; set; }
        internal static ConfigEntry<int> AddedMountainShrinesPerStage { get; set; }
        internal static ConfigEntry<int> MultMountainShrinesPerStage { get; set; }
        internal static ConfigEntry<bool> MountainShrinesChallengeMode { get; set; }

        internal static ConfigEntry<bool> InfiniteMountainEnabled { get; set; }
        internal static ConfigEntry<int> InfiniteMountainPurchaseLimit { get; set; }


        public void Start()
        {
            IsEnabled = Config.Bind("Main", "enabled", true, "Is mod enabled");

            ArroganceEnabled = Config.Bind("ArtifactOfArrogance", nameof(ArroganceEnabled), true, "Is Artifact of Arrogance enabled");
            AddedMountainShrinesPerStage = Config.Bind("ArtifactOfArrogance", nameof(AddedMountainShrinesPerStage), 0, "How many mountain shrines to add per stage");
            MultMountainShrinesPerStage = Config.Bind("ArtifactOfArrogance", nameof(MultMountainShrinesPerStage), 1, "How many times to multiply mountain shrines each stage");
            MountainShrinesChallengeMode = Config.Bind("ArtifactOfArrogance", nameof(MountainShrinesChallengeMode), false, "Enable challenge mode, where added mountain shrines only give 1 item");

            InfiniteMountainEnabled = Config.Bind("InfiniteMountain", nameof(InfiniteMountainEnabled), true, "Is infinite mountain shrines enabled");
            InfiniteMountainPurchaseLimit = Config.Bind("InfiniteMountain", nameof(InfiniteMountainPurchaseLimit), 1, "How many times can you hit 1 mountain shrine");

            /* HookEndpointManager.Add(startRun, (Action<Action<PreGameController>, PreGameController>)PreGameControllerStartRun); */

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
                if (IsEnabled.Value && ArroganceEnabled.Value)
                {
                    stageMountainCount++;
                }

                if (IsEnabled.Value && InfiniteMountainEnabled.Value)
                {
                    self.GetComponent<ShrineBossBehavior>().maxPurchaseCount = InfiniteMountainPurchaseLimit.Value;
                }

                orig(self, interactor);
            };

            On.RoR2.Run.Start += (orig, self) =>
            {
                if (IsEnabled.Value && ArroganceEnabled.Value)
                {
                    runMountainCount = 0;
                    stageMountainCount = 0;
                }

                if(IsEnabled.Value && InfiniteMountainEnabled.Value)
                {
                    if (InfiniteMountainPurchaseLimit.Value < 0 || InfiniteMountainPurchaseLimit.Value > 2147483647)
                    {
                        InfiniteMountainPurchaseLimit.Value = 2147483647;
                    }
                }

                orig(self);
            };

            On.RoR2.Stage.Start += (orig, self) =>
            {
                if (IsEnabled.Value && ArroganceEnabled.Value)
                {
                    runMountainCount += (stageMountainCount + AddedMountainShrinesPerStage.Value) * MultMountainShrinesPerStage.Value;
                    stageMountainCount = 0;
                    orig(self);

                    if (TeleporterInteraction.instance)
                    {
                        for (int i = 0; i < runMountainCount; i++)
                        {
                            if (MountainShrinesChallengeMode.Value)
                            {
                                TeleporterInteraction.instance.shrineBonusStacks++;
                            }
                            else
                            {
                                TeleporterInteraction.instance.AddShrineStack();
                            }
                        }
                    }
                } else
                {
                    orig(self);
                }
            };

            /*On.RoR2.PurchaseInteraction.OnInteractionBegin += (orig, self, interactor) =>
            {
                lastInteractor = interactor;
                orig(self, interactor);
            };*/

            /*On.RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer += (orig, self, createPickupInfo, shouldSpawn) =>
            {
                if (votedForCommand.Contains(lastInteractor.netIdentity))
                {
                    RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer(orig, self, createPickupInfo, shouldSpawn);
                }
                else
                {
                    orig(self, createPickupInfo, shouldSpawn);
                }
            };*/

            /*On.RoR2.PickupDropletController.PickupDropletHitGroundDelegate.Invoke += (RoR2.PickupDropletController.PickupDropletHitGroundDelegate.orig_Invoke orig, RoR2.PickupDropletController.PickupDropletHitGroundDelegate self, ref GenericPickupController.CreatePickupInfo createPickupInfo, ref bool shouldSpawn) =>
            {
                if (votedForCommand.Contains(lastInteractor.netIdentity))
                {
                    RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer(orig, self, createPickupInfo, shouldSpawn);
                }
                else
                {
                    orig(self, createPickupInfo, shouldSpawn);
                }
            };*/

            /*On.RoR2.Artifacts.CommandArtifactManager.OnArtifactEnabled += (orig, self, artifactDef) =>
            {
                if (artifactDef != CommandArtifactManager.myArtifact)
	            {
		            return;
	            }
	            if (NetworkServer.active)
	            {
		            //PickupDropletController.onDropletHitGroundServer += CommandArtifactManager.OnDropletHitGroundServer;
		            SceneDirector.onGenerateInteractableCardSelection += CommandArtifactManager.OnGenerateInteractableCardSelection;
	            }
            };

            On.RoR2.Artifacts.CommandArtifactManager.OnArtifactDisabled += (orig, self, artifactDef) =>
            {
                if (artifactDef != CommandArtifactManager.myArtifact)
	            {
		            return;
	            }
	            SceneDirector.onGenerateInteractableCardSelection -= CommandArtifactManager.OnGenerateInteractableCardSelection;
	            //PickupDropletController.onDropletHitGroundServer -= CommandArtifactManager.OnDropletHitGroundServer;
            };*/

            /*On.RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer += (On.RoR2.Artifacts.CommandArtifactManager.orig_OnDropletHitGroundServer orig, ref GenericPickupController.CreatePickupInfo createPickupInfo, ref bool shouldSpawn) =>
            {
                Chat.AddMessage(lastInteractor.netIdentity.ToString());
                Chat.AddMessage(lastInteractor.netId.ToString());
                Chat.AddMessage(lastInteractor.netIdentity.name.ToString());

                //foreach (string s in votedForCommand)
                //{
                //    Chat.AddMessage(s);
                //}
                if(votedForCommand.Contains(lastInteractor.netIdentity.ToString()))
                {
                    //Chat.AddMessage("Spawning Command Orb");
                    orig(ref createPickupInfo, ref shouldSpawn);
                    //RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer(ref createPickupInfo, ref shouldSpawn);
                }
                else
                {
                    //Chat.AddMessage("Spawning Item");
                    orig(ref createPickupInfo, ref shouldSpawn);
                    //RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer(ref createPickupInfo, ref shouldSpawn);
                }
            };*/
        }

        /*private static void PreGameControllerStartRun(Action<PreGameController> orig, PreGameController self)
        {
            votedForCommand.Clear();
            var choice = RuleCatalog.FindChoiceDef("Artifacts.Command.On");
            foreach (var user in NetworkUser.readOnlyInstancesList)
            {
                var voteController = PreGameRuleVoteController.FindForUser(user);
                var isCommandVoted = voteController.IsChoiceVoted(choice);

                if (isCommandVoted)
                {
                    votedForCommand.Add(user.netIdentity.ToString());
                    Chat.AddMessage(user.netIdentity.ToString());
                    Chat.AddMessage(user.netId.ToString());
                    Chat.AddMessage(user.NetworkuserColor.ToString());
                }
            }
            orig(self);
        }*/
    }
}