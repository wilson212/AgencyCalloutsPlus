using AgencyCalloutsPlus.Extensions;
using AgencyCalloutsPlus.Mod.Conversation;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AgencyCalloutsPlus.RageUIMenus
{
    /// <summary>
    /// Represents a basic <see cref="RAGENativeUI.MenuPool"/> for questioning peds during a callout
    /// </summary>
    public class CalloutPedInteractionMenu
    {
        private UIMenu MainUIMenu;
        private MenuPool AllMenus;
        private bool HasModifier;

        /// <summary>
        /// Speak with Subject button
        /// </summary>
        public UIMenuItem SpeakWithButton { get; private set; }

        /// <summary>
        /// Contains a list of <see cref="Ped"/> entities that this menu can be used with.
        /// The bool value indicates wether the Ped has been spoken with yet.
        /// </summary>
        private Dictionary<Ped, bool> Peds { get; set; }

        /// <summary>
        /// Contains a list of <see cref="Ped"/> entities that this menu can be used with.
        /// The bool value indicates wether the Ped has been spoken with yet.
        /// </summary>
        private Dictionary<Ped, FlowSequence> Conversations { get; set; }

        /// <summary>
        /// Contains a list of <see cref="Ped"/> entities that this menu can be used with.
        /// The bool value indicates wether the Ped has been spoken with yet.
        /// </summary>
        private Dictionary<string, FlowSequence> ConversationsById { get; set; }

        /// <summary>
        /// Gets the Ped that was last within range and angle to have a conversation with
        /// </summary>
        private Ped CurrentPed { get; set; }

        /// <summary>
        /// Indicates whether this menu can be opened by the player
        /// </summary>
        public bool Enabled { get; protected set; } = false;

        /// <summary>
        /// Gets or sets the current open FlowSequence Meny
        /// </summary>
        protected FlowSequence CurrentSequence { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="CalloutPedInteractionMenu"/>
        /// </summary>
        /// <param name="title"></param>
        /// <param name="subTitle"></param>
        public CalloutPedInteractionMenu(string title, string subTitle)
        {
            // Create main menu
            MainUIMenu = new UIMenu(title, subTitle);
            MainUIMenu.MouseControlsEnabled = false;
            MainUIMenu.AllowCameraMovement = true;
            MainUIMenu.SetMenuWidthOffset(12);

            // Add main menu buttons
            SpeakWithButton = new UIMenuItem("Speak with Subject", "Advance the conversation with the ~y~Subject");
            MainUIMenu.AddItem(SpeakWithButton);

            // Internal tracker
            SpeakWithButton.Activated += SpeakWithButton_Activated;

            // Create menu pool
            AllMenus = new MenuPool();
            AllMenus.Add(MainUIMenu);
            AllMenus.RefreshIndex();

            // internals
            Peds = new Dictionary<Ped, bool>();
            Conversations = new Dictionary<Ped, FlowSequence>();
            ConversationsById = new Dictionary<string, FlowSequence>();
            HasModifier = (Settings.OpenCalloutMenuModifierKey != Keys.None);
        }

        /// <summary>
        /// Processes the menu. This should be called in your <see cref="Callout.Process()"/> method
        /// </summary>
        public void Process()
        {
            // Process menus
            AllMenus.ProcessMenus();

            // vars
            var player = Game.LocalPlayer.Character;

            // If player is close to and facing another ped, show press Y to open menu
            if (!MainUIMenu.Visible)
            {
                // Only allow interaction menu to open on Foot
                if (!player.IsOnFoot) return;

                // If a FlowSequence is open, process that!
                if (CurrentSequence != null)
                {
                    if (CurrentSequence.AllMenus.IsAnyMenuOpen())
                    {
                        CurrentSequence.Process(player);
                        return;
                    }
                }

                // Distance and facing check
                if (!TryGetPedForConversation(player, out Ped ped))
                {
                    // No ped in range or facing? skip for now
                    return;
                }

                // Set current ped
                CurrentPed = ped;

                // Only show if we havent spoken with this ped yet!
                if (Peds[ped] == false)
                {
                    // Let player know they can open the menu
                    var k1 = Settings.OpenCalloutMenuModifierKey.ToString("F");
                    var k2 = Settings.OpenCalloutMenuKey.ToString("F");
                    if (HasModifier)
                    {
                        Game.DisplayHelp($"Press the ~y~{k1}~s~ + ~y~{k2}~s~ keys to open the interaction menu.");
                    }
                    else
                    {
                        Game.DisplayHelp($"Press the ~y~{k2}~s~ key to open the interaction menu.");
                    }
                }

                // Is modifier key pressed
                if (HasModifier)
                {
                    if (Globals.IsKeyDownWithModifier(Settings.OpenCalloutMenuKey, Settings.OpenCalloutMenuModifierKey))
                    {
                        Game.HideHelp();
                        MainUIMenu.Visible = true;
                    }
                }
                else if (Game.IsKeyDown(Settings.OpenCalloutMenuKey))
                {
                    Game.HideHelp();
                    MainUIMenu.Visible = true;
                }
            }
            else // Menu is open
            {
                // Distance and facing check
                if (!TryGetPedForConversation(player, out Ped ped))
                {
                    MainUIMenu.Visible = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Checks to see if a <see cref="Ped"/> is within 3 meters of the player,
        /// and if the Player is facing that ped within the specified angle
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ped"></param>
        /// <returns></returns>
        private bool TryGetPedForConversation(Ped player, out Ped ped)
        {
            // Distance and facing check
            ped = null;
            foreach (Ped subject in Peds.Keys)
            {
                // Is player within 3m of the ped?
                if (player.Position.DistanceTo(subject.Position) > 3f)
                {
                    // too far away
                    continue;
                }

                // Check if player is facing the ped
                if (player.IsFacingPed(subject, 45f))
                {
                    // Logic
                    ped = subject;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Starts showing the menu when the player presses the proper keybind near
        /// a Ped
        /// </summary>
        public void Enable()
        {
            Enabled = true;
        }

        /// <summary>
        /// Stops showing this menu
        /// </summary>
        public void Disable()
        {
            MainUIMenu.Visible = false;
            this.Enabled = false;
        }

        public void Dispose()
        {
            foreach (var kvp in Conversations)
            {
                kvp.Value.Dispose();
            }

            foreach (var kvp in ConversationsById)
            {
                kvp.Value.Dispose();
            }

            AllMenus.CloseAllMenus();
            AllMenus.Clear();
            AllMenus = null;
        }

        /// <summary>
        /// Registers a <see cref="Ped"/> that can be questioned by this Menu given
        /// the supplied <see cref="FlowSequence"/>
        /// </summary>
        /// <param name="ped"></param>
        public void RegisterPedConversation(Ped ped, FlowSequence sequence)
        {
            Peds.Add(ped, false);
            Conversations.Add(ped, sequence);
            ConversationsById.Add(sequence.SequenceId, sequence);

            // Fire event on PedResponse
            sequence.OnPedResponse += FlowSequence_OnPedResponse;
        }

        /// <summary>
        /// Displays the specified hidden menu items 
        /// </summary>
        /// <param name="menuPaths"></param>
        /// <param name="defaultSequence"></param>
        private void ShowMenuItems(string[] menuPaths, FlowSequence defaultSequence)
        {
            string referenceMenuId = defaultSequence.SequenceId;
            string buttonId = String.Empty;

            foreach (string id in menuPaths)
            {
                // Extract full name of the menu
                if (id.Contains("."))
                {
                    var path = id.Split(new[] { '.' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    referenceMenuId = path[0];
                    buttonId = path[1];
                }
                else
                {
                    buttonId = id;
                }

                // Grab FlowSequence by ID
                if (!ConversationsById.TryGetValue(referenceMenuId, out FlowSequence referenceMenu))
                {
                    Log.Debug($"InteractionMenu.ShowMenuItems: Reference FlowSequence menu with ID '{id}' does not exist");
                    continue;
                }

                // Debug logging
                if (!referenceMenu.ShowMenuItem(buttonId))
                {
                    Log.Debug($"InteractionMenu.ShowMenuItems: Failed to show button with ID '{id}'");
                }
            }
        }

        /// <summary>
        /// Hides the specified menu items
        /// </summary>
        /// <param name="menuPaths"></param>
        /// <param name="defaultSequence"></param>
        private void HideMenuItems(string[] menuPaths, FlowSequence defaultSequence)
        {
            string referenceMenuId = defaultSequence.SequenceId;
            string buttonId = String.Empty;

            foreach (string id in menuPaths)
            {
                // Extract full name of the menu
                if (id.Contains("."))
                {
                    var path = id.Split(new[] { '.' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    referenceMenuId = path[0];
                    buttonId = path[1];
                }
                else
                {
                    buttonId = id;
                }

                // Grab FlowSequence by ID
                if (!ConversationsById.TryGetValue(referenceMenuId, out FlowSequence referenceMenu))
                {
                    Log.Debug($"InteractionMenu.HideMenuItems: Reference FlowSequence menu with ID '{id}' does not exist");
                    continue;
                }

                // Debug logging
                if (!referenceMenu.HideMenuItem(buttonId))
                {
                    Log.Debug($"InteractionMenu.HideMenuItems: Failed to show button with ID '{id}'");
                }
            }
        }

        /// <summary>
        /// Method called everytime a <see cref="PedResponse"/> is displayed. We
        /// use this method to hide and show questioning menu items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        /// <param name="lineSet"></param>
        private void FlowSequence_OnPedResponse(FlowSequence sender, PedResponse response, LineSet lineSet)
        {
            // Does this change visibility of a menu option?
            if (response.ShowMenuItems.Length > 0)
            {
                ShowMenuItems(response.ShowMenuItems, sender);
            }

            if (lineSet.ShowMenuItems.Length > 0)
            {
                ShowMenuItems(lineSet.ShowMenuItems, sender);
            }

            // Does this change visibility of a menu option?
            if (response.HidesMenuItems.Length > 0)
            {
                HideMenuItems(response.HidesMenuItems, sender);
            }

            if (lineSet.HidesMenuItems.Length > 0)
            {
                HideMenuItems(lineSet.HidesMenuItems, sender);
            }
        }

        /// <summary>
        /// Method called everytime a player selects an option in the questioning menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="selectedItem"></param>
        private void SpeakWithButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Distance and facing check
            if (CurrentPed == null) return;

            // Indicate that this ped has been talked to. This will stop displaying
            // the hint that appears on the top left of the screen
            Peds[CurrentPed] = true;

            // Check for an open FlowSequence menu!
            if (CurrentSequence != null)
            {
                CurrentSequence.SetMenuVisible(false);
            }

            // Open flow sequence main menu
            CurrentSequence = Conversations[CurrentPed];
            CurrentSequence.SetMenuVisible(true);

            // Close current menu
            AllMenus.CloseAllMenus();
        }
    }
}
