﻿using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Conversation;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// Represents a basic <see cref="MenuPool"/> for questioning peds during a callout
    /// </summary>
    public class CalloutInteractionMenu
    {
        private UIMenu MainUIMenu;
        private MenuPool AllMenus;
        
        /// <summary>
        /// 
        /// </summary>
        public bool HasModifier { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string OpenMenuKeyString { get; private set; } 

        /// <summary>
        /// 
        /// </summary>
        public string OpenMenuModifierKeyString { get; private set; }

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
        /// Indicates whether a menu is open
        /// </summary>
        public bool IsMenuVisible
        {
            get
            {
                // Check if child sequence menu is open
                if (CurrentSequence != null && CurrentSequence.AllMenus.IsAnyMenuOpen())
                    return true;

                return AllMenus.IsAnyMenuOpen();
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="CalloutInteractionMenu"/>
        /// </summary>
        /// <param name="calloutName">The name of the <see cref="AgencyCallout"/></param>
        /// <param name="scenarioName">The name of the <see cref="Callouts.CalloutScenario"/></param>
        public CalloutInteractionMenu(string calloutName, string scenarioName)
        {
            // Create main menu
            MainUIMenu = new UIMenu("Callout Interaction", $"~b~{calloutName}: ~y~{scenarioName}")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

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
            OpenMenuKeyString = $"~{Settings.OpenCalloutMenuKey.GetInstructionalId()}~";
            OpenMenuModifierKeyString = $"~{Settings.OpenCalloutMenuModifierKey.GetInstructionalId()}~";
        }

        /// <summary>
        /// Processes the menu. This should be called in your <see cref="Callout.Process()"/> method
        /// </summary>
        public void Process()
        {
            // Process menus
            AllMenus.ProcessMenus();

            // vars
            var player = Rage.Game.LocalPlayer.Character;

            // If a FlowSequence is open, process that!
            if (CurrentSequence != null)
            {
                if (CurrentSequence.AllMenus.IsAnyMenuOpen())
                {
                    CurrentSequence.Process(player);
                    return;
                }
            }

            // See if we have a ped we can speak with
            if (TryGetPedForConversation(player, out Ped ped))
            {
                // Set current ped
                CurrentPed = ped;
                SpeakWithButton.Enabled = true;
            }
            else
            {
                // Disable the speak with button
                SpeakWithButton.Enabled = false;
            }

            // If player is close to and facing another ped, show press Y to open menu
            if (!MainUIMenu.Visible)
            {
                // Only allow interaction menu to open on Foot
                if (!player.IsOnFoot) return;

                // Only show if we havent spoken with this ped yet!
                if (CurrentPed != null && Peds[CurrentPed] == false)
                {
                    // Let player know they can open the menu
                    DisplayMenuHelpMessage();
                }

                // If menu key is pressed, show menu
                if (Keyboard.IsKeyDownWithModifier(Settings.OpenCalloutMenuKey, Settings.OpenCalloutMenuModifierKey))
                {
                    Rage.Game.HideHelp();
                    MainUIMenu.Visible = true;

                    // Disable on first frame!
                    SpeakWithButton.Enabled = false;
                }
            }
            else // Menu is open
            {
                // If menu key is pressed, hide menu
                if (Keyboard.IsKeyDownWithModifier(Settings.OpenCalloutMenuKey, Settings.OpenCalloutMenuModifierKey))
                {
                    SpeakWithButton.Enabled = false;
                    MainUIMenu.Visible = false;
                }

                // See if we have a ped we can speak with
                if (SpeakWithButton.Enabled == false)
                {
                    // If a FlowSequence is open, close it
                    if (CurrentSequence != null && CurrentSequence.AllMenus.IsAnyMenuOpen())
                    {
                        CurrentSequence.AllMenus.CloseAllMenus();
                        CurrentSequence = null;
                    }
                }
            }
        }

        /// <summary>
        /// Displays a help message to the player specifying which keys to press to open the menu
        /// </summary>
        public void DisplayMenuHelpMessage()
        {
            // Let player know they can open the menu
            if (HasModifier)
            {
                Rage.Game.DisplayHelp($"Press the {OpenMenuModifierKeyString} ~+~ {OpenMenuKeyString} keys to open the interaction menu.", false);
            }
            else
            {
                Rage.Game.DisplayHelp($"Press the {OpenMenuKeyString} key to open the interaction menu.", false);
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
                // Skip
                if (!subject.Exists()) continue;

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

        /// <summary>
        /// Adds the menu to the internal <see cref="MenuPool"/>
        /// </summary>
        /// <param name="menu"></param>
        public void AddMenu(UIMenu menu)
        {
            AllMenus.Add(menu);
        }

        /// <summary>
        /// Adds the <see cref="UIMenuItem"/> to the main <see cref="UIMenu"/>
        /// </summary>
        /// <param name="item"></param>
        public void AddMenuItem(UIMenuItem item)
        {
            MainUIMenu.AddItem(item);
        }

        /// <summary>
        /// Registers a <see cref="Ped"/> that can be questioned by this Menu given
        /// the supplied <see cref="FlowSequence"/>
        /// </summary>
        /// <param name="ped"></param>
        public void AddConversation(FlowSequence sequence)
        {
            Ped ped = sequence.SubjectPed ?? throw new ArgumentNullException(nameof(sequence.SubjectPed));
            if (!Conversations.ContainsKey(ped))
            {
                Peds.Add(ped, false);
                Conversations.Add(ped, sequence);
                ConversationsById.Add(sequence.SequenceId, sequence);

                // Fire event on PedResponse
                sequence.OnPedResponse += FlowSequence_OnPedResponse;
            }
        }

        /// <summary>
        /// Displays the specified hidden menu items 
        /// </summary>
        /// <param name="questionIds"></param>
        /// <param name="defaultSequence"></param>
        private void ShowQuestionsById(string[] questionIds, FlowSequence defaultSequence)
        {
            string referenceMenuId = defaultSequence.SequenceId;
            string questionId = String.Empty;

            foreach (string id in questionIds)
            {
                // Extract full name of the menu
                if (id.Contains("."))
                {
                    var path = id.Split(new[] { '.' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    referenceMenuId = path[0];
                    questionId = path[1];
                }
                else
                {
                    questionId = id;
                }

                // Grab FlowSequence by ID
                if (!ConversationsById.TryGetValue(referenceMenuId, out FlowSequence referenceMenu))
                {
                    Log.Debug($"CalloutInteractionMenu.ShowQuestionsById(): Reference FlowSequence menu with ID '{id}' does not exist");
                    continue;
                }

                // Debug logging
                if (!referenceMenu.ShowQuestionById(questionId))
                {
                    Log.Debug($"CalloutInteractionMenu.ShowQuestionsById(): Failed to show question with ID '{id}'");
                }
            }
        }

        /// <summary>
        /// Hides the specified menu items
        /// </summary>
        /// <param name="questionIds"></param>
        /// <param name="defaultSequence"></param>
        private void HideQuestionsById(string[] questionIds, FlowSequence defaultSequence)
        {
            string referenceMenuId = defaultSequence.SequenceId;
            string questionId = String.Empty;

            foreach (string id in questionIds)
            {
                // Extract full name of the menu
                if (id.Contains("."))
                {
                    var path = id.Split(new[] { '.' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    referenceMenuId = path[0];
                    questionId = path[1];
                }
                else
                {
                    questionId = id;
                }

                // Grab FlowSequence by ID
                if (!ConversationsById.TryGetValue(referenceMenuId, out FlowSequence referenceMenu))
                {
                    Log.Debug($"CalloutInteractionMenu.HideQuestionsById(): Reference FlowSequence menu with ID '{id}' does not exist");
                    continue;
                }

                // Debug logging
                if (!referenceMenu.HideQuestionById(questionId))
                {
                    Log.Debug($"CalloutInteractionMenu.HideQuestionsById(): Failed to show question with ID '{id}'");
                }
            }
        }

        /// <summary>
        /// Method called everytime a <see cref="PedResponse"/> is displayed. We
        /// use this method to hide and show questioning menu items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        /// <param name="statement"></param>
        private void FlowSequence_OnPedResponse(FlowSequence sender, Question question, PedResponse response, Dialog statement)
        {
            // Does this change visibility of a menu option?
            if (response.ShowQuestionIds.Length > 0)
            {
                ShowQuestionsById(response.ShowQuestionIds, sender);
            }

            if (statement.ShowsQuestionIds.Length > 0)
            {
                ShowQuestionsById(statement.ShowsQuestionIds, sender);
            }

            // Does this change visibility of a menu option?
            if (response.HideQuestionIds.Length > 0)
            {
                HideQuestionsById(response.HideQuestionIds, sender);
            }

            if (statement.HidesQuestionIds.Length > 0)
            {
                HideQuestionsById(statement.HidesQuestionIds, sender);
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

        /// <summary>
        /// Disposes this instance and all <see cref="FlowSequence"/> instances
        /// </summary>
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
    }
}
