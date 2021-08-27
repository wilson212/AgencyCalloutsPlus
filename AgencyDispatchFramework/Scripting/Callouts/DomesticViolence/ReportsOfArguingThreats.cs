using AgencyDispatchFramework.Conversation;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.NativeUI;
using AgencyDispatchFramework.Xml;
using Rage;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace AgencyDispatchFramework.Scripting.Callouts.DomesticViolence
{
    /// <summary>
    /// One scenario of the <see cref="DomesticViolence"/> callout. This scenario represents
    /// a concerned person has called in stating that there here a couple fighting, with a suspect
    /// claiming they are going to inflict harm to the victim.
    /// </summary>
    internal class ReportsOfArguingThreats : CalloutScenario
    {
        /// <summary>
        /// The callout that owns this instance
        /// </summary>
        private Controller Callout { get; set; }

        /// <summary>
        /// Gets the SpawnPoint location of this <see cref="CalloutScenario"/>
        /// </summary>
        public Residence Residence { get; protected set; }

        protected UIMenuItem KnockButton { get; set; }

        private GamePed Victim;
        private PedGender VictimGender;
        private bool InitialPedWasVictim = false;
        private Blip VictimBlip;

        private GamePed Suspect;
        private PedGender SuspectGender;
        private Blip SuspectBlip;

        private PedVariantGroup PedGroup;

        private ResidencePosition SpawnId;

        private Vector3 CheckpointPosition;
        private int CheckpointHandle = 0;

        private ScenarioProgress SceneProgress;

        private bool ForceFacing = false;

        private CalloutInteractionMenu Menu;
        private Dialogue SuspectDialogue;
        private Dialogue VictimDialogue;

        private Blip AddressBlip; 

        public ReportsOfArguingThreats(Controller callout, CalloutScenarioInfo scenarioInfo) : base(scenarioInfo)
        {
            // Set internals
            Callout = callout;

            // Store spawn point
            this.Residence = callout.Location;
        }

        /// <summary>
        /// Sets up the current CalloutScene vehicles and peds. This method must be called
        /// in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.OnCalloutAccepted()"/> method
        /// </summary>
        public override void Setup()
        {
            // Ensure our FlowOutcome is not null
            if (SelectedCircumstance == null)
            {
                throw new ArgumentNullException(nameof(SelectedCircumstance));
            }

            // Show player notification
            Rage.Game.DisplayNotification(
                "3dtextures",
                "mpgroundlogo_cops",
                "~b~Dispatch",
                "~r~Disturbance",
                "Reports of a two people arguing with threats, respond ~b~CODE-2"
            );

            // Select roles
            var random = new CryptoRandom();
            SuspectGender = random.PickOne(PedGender.Male, PedGender.Female);
            VictimGender = (SuspectGender == PedGender.Male) ? PedGender.Female : PedGender.Male;

            // Select random PedVariantGroup
            PedGroup = random.PickOne(PedVariantGroup.GenericYoung, PedVariantGroup.GenericMiddleAge);

            // Grab spawnpoints for the peds
            SpawnId = ResidencePosition.BackYardPedGroup; //random.PickOne(HomeSpawnId.FrontDoorPed, HomeSpawnId.BackYardPed);

            // Create a marker for the player to walk into
            CheckpointPosition = Residence.GetSpawnPositionById(ResidencePosition.FrontDoorPolicePed1);
            CheckpointPosition.Z -= 2f; // Checkpoints spawn at waist level... Bring it down some
            CheckpointHandle = GameWorld.CreateCheckpoint(CheckpointPosition, Color.Yellow, forceGround: true);

            // Lets grab a spawn location
            var loc = new SpawnPoint(Vector3.Zero); // Location.GetSpawnPoint(SpawnId);

            // Try and spawn suspect at the front door
            if (!GameWorld.TrySpawnRandomPedAtPosition(loc, PedGroup, SuspectGender, true, out Ped s))
            {
                throw new Exception("Spawning Suspect ped at position failed");
            }

            // Try and spawn suspect at the front door
            SpawnPoint point = new SpawnPoint(s.GetOffsetPositionFront(3f));
            if (!GameWorld.TrySpawnRandomPedAtPosition(point, PedGroup, VictimGender, true, out Ped v))
            {
                throw new Exception("Spawning Victim ped at position failed");
            }

            // Create Victim
            Victim = v;
            Victim.Ped.IsPersistent = true;
            Victim.Ped.BlockPermanentEvents = true;

            // Create suspect
            Suspect = s;
            Suspect.Ped.IsPersistent = true;
            Suspect.Ped.BlockPermanentEvents = true;

            // Add parser variables
            Parser.SetParamater("Victim", Victim);
            Parser.SetParamater("Suspect", Suspect);

            // Register menu
            Menu = new CalloutInteractionMenu("Domestic Violence", "Domestic Arguing Only");

            // Create variable dictionary
            var variables = new Dictionary<string, object>()
            {
                { "SuspectTitle", $"{Suspect.GenderTitle}. {Suspect.Persona.Surname}" },
                { "VictimTitle", $"{Victim.GenderTitle}. {Victim.Persona.Surname}" },
            };

            // Load converation flow sequence for the suspect
            var path = Path.Combine(Main.FrameworkFolderPath, "Callouts", "DomesticViolence", nameof(ReportsOfArguingThreats), "Suspect.xml");
            using (var file = new DialogueFile(path))
            {
                SuspectDialogue = file.Parse(SelectedCircumstance, Suspect, Parser);
                SuspectDialogue.SetVariableDictionary(variables);
                Menu.AddDialogue(SuspectDialogue);
            }

            // Load converation flow sequence for the victim
            path = Path.Combine(Main.FrameworkFolderPath, "Callouts", "DomesticViolence", nameof(ReportsOfArguingThreats), "Victim.xml");
            using (var file = new DialogueFile(path))
            {
                VictimDialogue = file.Parse(SelectedCircumstance, Victim, Parser);
                VictimDialogue.SetVariableDictionary(variables);
                Menu.AddDialogue(VictimDialogue);
            }

            // Create menu button
            KnockButton = new UIMenuItem("Knock on the Door");
            KnockButton.Activated += KnockButton_Activated;
            Menu.AddMenuItem(KnockButton);

            // Create blip at the hime
            AddressBlip = new Blip(Residence.GetSpawnPositionById(ResidencePosition.FrontDoorPed1))
            {
                IsRouteEnabled = true,
                Sprite = BlipSprite.PointOfInterest
            };
        }

        /// <summary>
        /// Processes the current <see cref="CalloutScenario"/>. This method must be called
        /// in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.Process()"/> method
        /// on every tick
        /// </summary>
        public override void Process()
        {
            // Process out buttons
            KnockButton.Enabled = (SceneProgress == ScenarioProgress.Arrived);

            // Always process the menu
            Menu.Process();

            // Grab player
            var player = Rage.Game.LocalPlayer.Character;

            // Use a switch statement to execute code based on where we are at within the scenario
            switch (SceneProgress)
            {
                case ScenarioProgress.BeforeArrival:
                    if (player.DistanceTo(CheckpointPosition) < 3f)
                    {
                        // Alert player to knock on the door
                        Menu.DisplayMenuHelpMessage();

                        // Update status
                        SceneProgress = ScenarioProgress.Arrived;
                    }
                    break;
                case ScenarioProgress.Arrived:
                    // Remove the checkpoint!
                    GameWorld.DeleteCheckpoint(CheckpointHandle);

                    // Remove address blip if still active
                    if (AddressBlip != null && AddressBlip.Exists() && AddressBlip.IsValid())
                    {
                        AddressBlip.Delete();
                    }

                    AddressBlip = null;

                    // Now we wait for the player to knock on the door!
                    break;
                case ScenarioProgress.Knocked:
                    // Play player knocking animation
                    player.Tasks.PlayAnimation("timetable@jimmy@doorknock@", "knockdoor_idle", 1f, AnimationFlags.SecondaryTask).WaitForCompletion();

                    // Spawn a Ped out of view close by... prevents ped from falling down when 
                    // their position is changed to the front door directly
                    var ped = new CryptoRandom().PickOne(Suspect, Victim);
                    ped.Ped.Position = Residence.GetSpawnPositionById(ResidencePosition.BackYardPedGroup);

                    // Wait for an answer
                    GameFiber.Wait(3000);

                    // Spawn a Ped at the door
                    ped.Ped.Position = Residence.GetSpawnPositionById(ResidencePosition.FrontDoorPed1);

                    // Attach Blip
                    if (ped == Suspect)
                    {
                        SuspectBlip = Suspect.Ped.AttachBlip();
                        SuspectBlip.Sprite = BlipSprite.Friend;

                        // Roll to see if the suspect attacks
                    }
                    else
                    {
                        InitialPedWasVictim = true;
                        VictimBlip = Victim.Ped.AttachBlip();
                        VictimBlip.Sprite = BlipSprite.Friend;
                    }

                    // Update scene progress
                    SceneProgress = ScenarioProgress.DoorAnswered;
                    break;
                case ScenarioProgress.DoorAnswered:
                    break;
            }

            GameFiber.Yield();
        }

        /// <summary>
        /// This method is responsible for cleaning up all of the objects in this <see cref="CalloutScenario"/>.
        /// This method must be called in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.End()"/> 
        /// method
        /// </summary>
        public override void Cleanup()
        {
            // Clean up
            Menu?.Dispose();
            Menu = null;

            Victim?.Ped?.Cleanup();
            Victim = null;

            Suspect?.Ped?.Cleanup();
            Suspect = null;

            if (VictimBlip.Exists() && VictimBlip.IsValid())
            {
                VictimBlip.Delete();
                //VictimBlip = null;
            }

            if (SuspectBlip.Exists() && SuspectBlip.IsValid())
            {
                SuspectBlip.Delete();
                //SuspectBlip = null;
            }

            // Erase the checkpoint if it exists
            if (SceneProgress == ScenarioProgress.BeforeArrival)
                GameWorld.DeleteCheckpoint(CheckpointHandle);

            SuspectDialogue?.Dispose();
            VictimDialogue?.Dispose();
        }

        /// <summary>
        /// Method called when the player knocks on the door
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="selectedItem"></param>
        private void KnockButton_Activated(RAGENativeUI.UIMenu sender, UIMenuItem selectedItem)
        {
            SceneProgress = ScenarioProgress.Knocked;
        }

        /// <summary>
        /// An enumeration to assist with keeping track of the current progress
        /// within the scenario
        /// </summary>
        private enum ScenarioProgress
        {
            BeforeArrival,
            Arrived,
            Knocked,
            DoorAnswered,
        }
    }
}
