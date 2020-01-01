using AgencyCalloutsPlus.Extensions;
using AgencyCalloutsPlus.RageUIMenus.Events;
using Rage;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LSPD_First_Response.Mod.Callouts;

namespace AgencyCalloutsPlus.RageUIMenus
{
    /// <summary>
    /// Represents a basic <see cref="RAGENativeUI.MenuPool"/> for questioning peds during a callout
    /// </summary>
    public class CalloutPedInteractionMenu
    {
        private UIMenu MainUIMenu;
        private MenuPool AllMenus;

        /// <summary>
        /// Speak with Subject button
        /// </summary>
        private UIMenuItem SpeakWithButton;

        /// <summary>
        /// Contains a list of <see cref="Ped"/> entities that this menu can be used with
        /// </summary>
        private List<Ped> Peds { get; set; }

        /// <summary>
        /// Event fired when "Speak with Subject" button is clicked in the menu
        /// </summary>
        public EventHandler<SpeakWithPedArgs> SpeakWithPedEvent { get; protected set; }

        /// <summary>
        /// Indicates whether this menu can be opened by the player
        /// </summary>
        public bool Enabled { get; protected set; } = false;

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

            // Add main menu buttons
            SpeakWithButton = new UIMenuItem("Speak with Subject", "Advance the conversation with the ~y~Subject");
            MainUIMenu.AddItem(SpeakWithButton);

            // Create menu pool
            AllMenus = new MenuPool();
            AllMenus.Add(MainUIMenu);

            // internals
            Peds = new List<Ped>();
        }

        /// <summary>
        /// Processes the menu. This should be called in your <see cref="Callout.Process()"/> method
        /// </summary>
        public void Process()
        {
            // Process menus
            AllMenus.ProcessMenus();

            // vars
            var hasModifier = (Settings.OpenCalloutInteractionMenuModifierKey != Keys.None);
            var player = Game.LocalPlayer.Character;

            // If player is close to and facing another ped, show press Y to open menu
            if (!MainUIMenu.Visible)
            {
                // Only allow interaction menu to open on Foot
                if (!player.IsOnFoot) return;

                // Distance and facing check
                if (!TryGetPedForConversation(player, out Ped ped))
                {
                    // No ped in range or facing? skip for now
                    return;
                }

                // Let player know they can open the menu
                var k1 = Settings.OpenCalloutInteractionMenuModifierKey.ToString("F");
                var k2 = Settings.OpenCalloutInteractionMenuKey.ToString("F");
                if (hasModifier)
                {
                    Game.DisplayHelp($"Press the ~y~{k1}~s~ + ~y~{k2}~s~ keys to open the interaction menu.");
                }
                else
                {
                    Game.DisplayHelp($"Press the ~y~{k2}~s~ key to open the interaction menu.");
                }

                // Is modifier key pressed
                if (hasModifier)
                {
                    if (!Game.IsKeyDown(Settings.OpenCalloutInteractionMenuModifierKey))
                    {
                        return;
                    }
                }

                // Wait for key press, then open menu
                if (Game.IsKeyDown(Settings.OpenCalloutInteractionMenuKey))
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
            foreach (Ped subject in Peds)
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

        /// <summary>
        /// Registers a <see cref="Ped"/> that can be questioned by this Menu
        /// </summary>
        /// <param name="ped"></param>
        public void RegisterPed(Ped ped)
        {
            Peds.Add(ped);
        }
    }
}
