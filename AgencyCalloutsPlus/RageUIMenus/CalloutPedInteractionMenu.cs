using AgencyCalloutsPlus.RageUIMenus.Events;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;

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
        public bool Enabled { get; protected set; }

        /// <summary>
        /// Creates a new instance of <see cref="CalloutPedInteractionMenu"/>
        /// </summary>
        /// <param name="title"></param>
        /// <param name="subTitle"></param>
        public CalloutPedInteractionMenu(string title, string subTitle)
        {
            // Create main menu
            MainUIMenu = new UIMenu(title, subTitle);

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
        /// Starts showing the menu when the player presses the proper keybind near
        /// a Ped
        /// </summary>
        public void Enable()
        {
            // DO NOT allow more than 1!
            if (Enabled) return;

            // Run menu in a new tread
            GameFiber.StartNew(delegate
            {
                while (Enabled)
                {
                    // Allow other plugins to tick
                    GameFiber.Yield();

                    // If player is close to and facing another ped, show press Y to open menu
                    if (!MainUIMenu.Visible)
                    {
                        // if (Game.LocalPlayer.Character.)

                        // Wait for key press, then open menu
                    }
                }
            });
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
