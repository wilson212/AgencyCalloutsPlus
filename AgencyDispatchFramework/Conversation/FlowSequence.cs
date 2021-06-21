using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.NativeUI;
using LSPD_First_Response.Engine.Scripting.Entities;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a conversation flow for a <see cref="Rage.Ped"/> based on menu input by the player
    /// </summary>
    public class FlowSequence : IDisposable
    {
        /// <summary>
        /// Contains the regular expression to replace variables within Ped responses
        /// </summary>
        private static Regex VariableExpression = new Regex(@"\$(\w+)\$", RegexOptions.Compiled);

        /// <summary>
        /// Gets the sequence ID for this conversation event
        /// </summary>
        public string SequenceId { get; private set; }

        /// <summary>
        /// Gets the <see cref="Rage.Ped"/> this <see cref="FlowSequence"/> is attacthed to
        /// </summary>
        public GamePed SubjectPed { get; private set; }

        /// <summary>
        /// Contains the main menu string name
        /// </summary>
        internal string InitialMenuName { get; set; }

        /// <summary>
        /// A <see cref="MenuPool"/> that contains all of this <see cref="FlowSequence"/> <see cref="UIMenu"/>s
        /// </summary>
        internal MenuPool AllMenus { get; set; }

        /// <summary>
        /// Contains a list of flow return menu's by name
        /// </summary>
        internal Dictionary<string, UIMenu> FlowReturnMenus { get; set; }

        /// <summary>
        /// Contains a list of <see cref="UIMenuItem"/>s by id
        /// that it belongs to
        /// </summary>
        internal Dictionary<string, UIMenuItem> MenuButtonsById { get; set; }

        /// <summary>
        /// Contains a list of flow return menu items that are initially no visible
        /// </summary>
        internal Dictionary<string, UIMenuItem> HiddenMenuItems { get; set; }

        /// <summary>
        /// Contains a list of flow return menu items that are initially no visible
        /// </summary>
        internal Dictionary<string, Action> Callbacks { get; set; }

        /// <summary>
        /// Contains our <see cref="ResponseSet"/> for this <see cref="FlowSequence"/>
        /// </summary>
        internal ResponseSet PedResponses { get; set; }

        /// <summary>
        /// Contains officer dialogs attached to each menu option
        /// </summary>
        internal Dictionary<string, Subtitle[]> OfficerDialogs { get; set; }

        /// <summary>
        /// Contains string replacements in the Output strings
        /// </summary>
        protected Dictionary<string, object> Variables { get; set; }

        /// <summary>
        /// Gets the FlowOutcome name selected for this conversation event
        /// </summary>
        public string FlowOutcomeId { get; protected set; }

        /// <summary>
        /// Event fired whenever a <see cref="PedResponse"/> is displayed
        /// </summary>
        public event PedResponseEventHandler OnPedResponse;

        /// <summary>
        /// Creates a new <see cref="FlowSequence"/> instance
        /// </summary>
        /// <param name="sequenceId">The unique name of this sequence. Usually the filename without extension</param>
        /// <param name="ped">The conversation subject ped</param>
        /// <param name="outcome">The selected outcome <see cref="ResponseSet"/></param>
        internal FlowSequence(string sequenceId, GamePed ped, FlowOutcome outcome)
        {
            // Set internals
            SequenceId = sequenceId;
            SubjectPed = ped;
            FlowOutcomeId = outcome.Id;

            // Create empty containers
            AllMenus = new MenuPool();
            FlowReturnMenus = new Dictionary<string, UIMenu>();
            OfficerDialogs = new Dictionary<string, Subtitle[]>();
            Variables = new Dictionary<string, object>();
            HiddenMenuItems = new Dictionary<string, UIMenuItem>();
            MenuButtonsById = new Dictionary<string, UIMenuItem>();
            Callbacks = new Dictionary<string, Action>();
        }

        /// <summary>
        /// Method to be called within the <see cref="LSPD_First_Response.Mod.Callouts.Callout.Process()"/> method
        /// each tick to process the internal menus
        /// </summary>
        /// <param name="player"></param>
        public void Process(Ped player)
        {
            try
            {
                // Process all menu's
                AllMenus.ProcessMenus();

                // Ensure we are still talking to the Ped and in distance
                if (!InTalkingDistance(player))
                {
                    AllMenus.CloseAllMenus();
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        /// <summary>
        /// Indicates whether the <see cref="Player"/> is within talking distance of this <see cref="SubjectPed"/>
        /// </summary>
        /// <param name="player">The <see cref="Player"/> instance</param>
        /// <returns></returns>
        private bool InTalkingDistance(Ped player)
        {
            // Is player within 3m of the ped?
            if (player.Position.DistanceTo(SubjectPed.Ped.Position) > 3f)
            {
                // too far away
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the <see cref="Ped"/> response to the question
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public PedResponse GetResponseToQuestionId(string text)
        {
            return PedResponses.GetResponseTo(text);
        }

        /// <summary>
        /// Sets a variable to be parsed on each conversation line
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetVariable<T>(string name, T value)
        {
            if (Variables.ContainsKey(name))
            {
                Variables[name] = value;
            }
            else
            {
                Variables.Add(name, value);
            }
        }

        /// <summary>
        /// Sets a variable dictionary with string replacements to be parsed on each conversation line
        /// </summary>
        public void SetVariableDictionary(Dictionary<string, object> variables)
        {
            Variables = variables;
        }

        /// <summary>
        /// Registers a callack listener on certain <see cref="Statement"/>s
        /// </summary>
        /// <param name="id"></param>
        /// <param name="action"></param>
        public void RegisterCallback(string id, Action action)
        {
            if (Callbacks.ContainsKey(id))
                Callbacks[id] = action;
            else
                Callbacks.Add(id, action);
        }

        /// <summary>
        /// Dispose method to clear memory
        /// </summary>
        public void Dispose()
        {
            if (AllMenus != null)
            {
                AllMenus.CloseAllMenus();
                AllMenus.Clear();
                AllMenus = null;

                foreach (var menu in FlowReturnMenus)
                {
                    menu.Value.Clear();
                }

                FlowReturnMenus.Clear();
                FlowReturnMenus = null;

                MenuButtonsById.Clear();
                MenuButtonsById = null;

                HiddenMenuItems.Clear();
                SubjectPed = null;

                Callbacks.Clear();
                Callbacks = null;

                OfficerDialogs = null;

                Variables.Clear();
                Variables = null;
            }
        }

        /// <summary>
        /// Sets the visibility of the <see cref="FlowSequence"/> <see cref="UIMenu"/>
        /// </summary>
        /// <param name="visible">if true, the menu is shown, otherwise menu is closed</param>
        public void SetMenuVisible(bool visible)
        {
            bool isAnyOpen = AllMenus.IsAnyMenuOpen();
            if (!visible && isAnyOpen)
            {
                AllMenus.CloseAllMenus();
            }
            else if (visible && !isAnyOpen)
            {
                // Open default menu
                FlowReturnMenus[InitialMenuName].Visible = true;
            }
        }

        /// <summary>
        /// Hides a questioning menu item by id
        /// </summary>
        /// <param name="buttonId">the button id to hide</param>
        public bool HideMenuItem(string buttonId)
        {
            // Already hidden?
            if (HiddenMenuItems.ContainsKey(buttonId) || !MenuButtonsById.ContainsKey(buttonId))
                return false;

            // Grab menu ID for this item ID
            var menuItem = MenuButtonsById[buttonId];

            // Add to hidden items
            HiddenMenuItems.Add(buttonId, menuItem);

            // Remove Item
            int index = menuItem.Parent.MenuItems.IndexOf(menuItem);
            menuItem.Parent.RemoveItemAt(index);
            menuItem.Parent.RefreshIndex();
            return true;
        }

        /// <summary>
        /// Displays a hidden questioning menu item by id
        /// </summary>
        /// <param name="buttonId">the button id to show</param>
        public bool ShowMenuItem(string buttonId)
        {
            // Already being displayed?
            if (!HiddenMenuItems.ContainsKey(buttonId))
                return false;

            // Grab menu ID for this item ID
            var menuItem = HiddenMenuItems[buttonId];

            // Add Item
            menuItem.Parent.AddItem(menuItem);
            menuItem.Parent.RefreshIndex();

            // Remove from hidden items
            HiddenMenuItems.Remove(buttonId);
            return true;
        }

        /// <summary>
        /// If a question is asked more than once, this method is called and returns
        /// a response that is prefixed into the <see cref="SubtitleQueue"/> to make
        /// the <see cref="Ped"/> feel more alive in thier repsonses.
        /// </summary>
        /// <returns></returns>
        private string GetRepeatPrefixText()
        {
            var random = new CryptoRandom();

            // Return a random demeanor
            switch (SubjectPed.Demeanor)
            {
                case PedDemeanor.Angry:
                case PedDemeanor.Hostile:
                    return random.PickOne("Did you not hear what I already told you??", "Are you deaf? I already told you!");
                case PedDemeanor.Agitated:
                case PedDemeanor.Annoyed:
                    return random.PickOne("(Sighs) Are you not hearing what I am saying??", "I already told you this...");
                default:
                    return random.PickOne("Like I said already officer...", "Didn't you ask me this already?");
            }
        }

        /// <summary>
        /// Method called on event <see cref="UIMenuItem.Activated"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="selectedItem"></param>
        private void On_ItemActivated(UIMenu sender, UIMenuItem selectedMenuItem)
        {
            // Convert to my menu item
            var selectedItem = selectedMenuItem as MyUIMenuItem<string>;
            if (selectedMenuItem == null)
            {
                return;
            }

            // Disable button from spam clicks
            selectedItem.Enabled = false;

            // Dont do anything right now!
            var response = GetResponseToQuestionId(selectedItem.Tag);
            if (response == null)
            {
                Rage.Game.DisplayNotification($"~r~No XML Response[@to='~b~{selectedItem.Tag}~r~'] node for this Ped.");
                return;
            }

            // Grab dialog
            var statement = response.GetStatement();
            if (statement == null || statement.Subtitles.Length == 0)
            {
                Log.Error($"FlowSequence.Item_Activated: Response to '{selectedItem.Tag}' has no lines");
                return;
            }

            // Clear any queued messages
            SubtitleQueue.Clear();
            var player = Rage.Game.LocalPlayer.Character;

            // Officer Dialogs first
            foreach (Subtitle line in OfficerDialogs[selectedItem.Tag])
            {
                // Set ped in statement to allow animations
                line.Speaker = player;
                line.PrefixText = "~y~You~w~:";
                line.Text = VariableExpression.ReplaceTokens(line.Text, Variables);

                // Add line
                SubtitleQueue.Add(line);
            }

            // Has the player asked this question already?
            if (selectedItem.ForeColor == Color.Gray)
            {
                var line = new Subtitle()
                {
                    PrefixText = $"~y~{SubjectPed.Persona.Forename}~w~:",
                    Speaker = SubjectPed.Ped,
                    Text = GetRepeatPrefixText(),
                    Duration = 2500
                };

                // Add line
                SubtitleQueue.Add(line);
            }

            // Add Ped response
            int index = 0;
            int lastIndex = statement.Subtitles.Length - 1;
            foreach (Subtitle line in statement.Subtitles)
            {
                // Do we have a callback for "onFirstShown" ?
                if (index == 0 && !String.IsNullOrEmpty(statement.CallOnFirstShown))
                {
                    if (Callbacks.TryGetValue(statement.CallOnFirstShown, out Action action))
                    {
                        void handler(Subtitle s)
                        {
                            line.OnDisplayed -= handler;
                            action.Invoke();
                        }

                        line.OnDisplayed += handler;
                    }
                }

                // Check for last subtitle events
                if (index == lastIndex)
                {
                    // Do we have a callback for "onLastShown" ?
                    if (!String.IsNullOrEmpty(statement.CallOnLastShown) && Callbacks.TryGetValue(statement.CallOnLastShown, out Action action))
                    {
                        void handler(Subtitle s)
                        {
                            line.OnDisplayed -= handler;
                            action.Invoke();
                        }

                        line.OnDisplayed += handler;
                    }

                    // Do we have a callback for "elapsed" ?
                    if (!String.IsNullOrEmpty(statement.CallOnElapsed) && Callbacks.TryGetValue(statement.CallOnElapsed, out Action action2))
                    {
                        void handler(Subtitle s)
                        {
                            line.Elapsed -= handler;
                            action2.Invoke();
                        }

                        line.Elapsed += handler;
                    }
                }

                // Set ped in statement to allow animations
                line.Speaker = SubjectPed.Ped;
                line.PrefixText = $"~y~{SubjectPed.Persona.Forename}~w~:";
                line.Text = VariableExpression.ReplaceTokens(line.Text, Variables);

                // Add line
                SubtitleQueue.Add(line);
                index++;
            }

            // Enable button and change font color to show its been clicked before
            selectedItem.Enabled = true;
            selectedItem.ForeColor = Color.Gray;

            // Fire off event
            OnPedResponse(this, response, statement);
        }

        /// <summary>
        /// Adds a menu button to the internal list, and registers for the click event
        /// </summary>
        /// <param name="buttonId"></param>
        /// <param name="item"></param>
        /// <param name="visible"></param>
        internal void AddMenuButton(string buttonId, MyUIMenuItem<string> item, bool visible)
        {
            // Register for event
            item.Activated += On_ItemActivated;

            // Add named item to our hash table
            if (!MenuButtonsById.ContainsKey(buttonId))
            {
                MenuButtonsById.Add(buttonId, item);
            }

            // Handle item visibility
            if (!visible)
            {
                // Store for later
                HiddenMenuItems.Add(buttonId, item);
            }
            else
            {
                item.Parent.AddItem(item);
            }
        }
    }
}
