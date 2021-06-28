using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.NativeUI;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a conversation flow sequence between a <see cref="Ped"/> and the <see cref="Player"/>
    /// using <see cref="RAGENativeUI"/> <see cref="UIMenu"/>s
    /// </summary>
    public class Dialogue : IDisposable
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
        /// Gets the <see cref="Rage.Ped"/> this <see cref="Dialogue"/> is attacthed to
        /// </summary>
        public GamePed SubjectPed { get; protected set; }

        /// <summary>
        /// Gets the <see cref="Conversation.Circumstance"/> selected for this <see cref="Dialogue"/>
        /// </summary>
        public Circumstance Circumstance { get; protected set; }

        /// <summary>
        /// Contains the main menu string name
        /// </summary>
        internal string InitialMenuName { get; set; }

        /// <summary>
        /// A <see cref="MenuPool"/> that contains all of this <see cref="Dialogue"/> <see cref="UIMenu"/>s
        /// </summary>
        internal MenuPool AllMenus { get; set; }

        /// <summary>
        /// Contains a list of flow return menu's by name
        /// </summary>
        protected Dictionary<string, UIMenu> MenusById { get; set; }

        /// <summary>
        /// Contains a list of flow return menu items that are initially no visible
        /// </summary>
        protected Dictionary<string, UIMenuItem> HiddenMenuItems { get; set; }

        /// <summary>
        /// Contains a list of flow return menu items that are initially no visible
        /// </summary>
        protected Dictionary<string, Action> Callbacks { get; set; }

        /// <summary>
        /// Contains our <see cref="PedResponse"/>s for this <see cref="Dialogue"/>
        /// </summary>
        protected Dictionary<string, PedResponse> PedResponses { get; set; }

        /// <summary>
        /// Contains officer dialogs attached to each menu option
        /// </summary>
        protected Dictionary<string, UIMenuItem<Question>> Questions { get; set; }

        /// <summary>
        /// Contains string replacements in the Output strings
        /// </summary>
        protected Dictionary<string, object> Variables { get; set; }

        /// <summary>
        /// Event fired whenever a <see cref="PedResponse"/> is displayed
        /// </summary>
        public event PedResponseEventHandler OnPedResponse;

        /// <summary>
        /// Creates a new <see cref="Dialogue"/> instance
        /// </summary>
        /// <param name="sequenceId">The unique name of this sequence. Usually the filename without extension</param>
        /// <param name="ped">The conversation subject ped</param>
        /// <param name="scenario">The selected outcome <see cref="ResponseSet"/></param>
        internal Dialogue(string sequenceId, GamePed ped, Circumstance scenario)
        {
            // Set internals
            SequenceId = sequenceId;
            SubjectPed = ped;
            Circumstance = scenario;

            // Create empty containers
            AllMenus = new MenuPool();
            MenusById = new Dictionary<string, UIMenu>();
            PedResponses = new Dictionary<string, PedResponse>();
            Questions = new Dictionary<string, UIMenuItem<Question>>();
            Variables = new Dictionary<string, object>();
            HiddenMenuItems = new Dictionary<string, UIMenuItem>();
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
        /// Gets the <see cref="Ped"/> response to the question
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public PedResponse GetResponseToQuestionId(string statementId)
        {
            if (!PedResponses.TryGetValue(statementId, out PedResponse response))
            {
                return null;
            }

            return response;
        }

        /// <summary>
        /// Determines whether this <see cref="Dialogue"/> contains a question
        /// with the specified key
        /// </summary>
        /// <param name="questionId"></param>
        /// <returns></returns>
        public bool ContainsQuestionById(string questionId)
        {
            return Questions.ContainsKey(questionId);
        }

        /// <summary>
        /// Determines whether this <see cref="Dialogue"/> contains a menu
        /// with the specified key
        /// </summary>
        public bool ContainsMenuById(string menuId)
        {
            return MenusById.ContainsKey(menuId);
        }

        /// <summary>
        /// Returns the <see cref="UIMenu"/> with the provided key
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public UIMenu GetMenyById(string menuId)
        {
            return MenusById[menuId];
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
        /// Registers a callack listener on certain <see cref="CommunicationSequence"/>s
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
        /// Sets the visibility of the <see cref="Dialogue"/> <see cref="UIMenu"/>
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
                MenusById[InitialMenuName].Visible = true;
            }
        }

        /// <summary>
        /// Hides a questioning menu item by id
        /// </summary>
        /// <param name="questionId">the button id to hide</param>
        public bool HideQuestionById(string questionId)
        {
            // Already hidden?
            if (HiddenMenuItems.ContainsKey(questionId) || !Questions.ContainsKey(questionId))
                return false;

            // Grab menu ID for this item ID
            var menuItem = Questions[questionId];

            // Add to hidden items
            HiddenMenuItems.Add(questionId, menuItem);

            // Remove Item
            int index = menuItem.Parent.MenuItems.IndexOf(menuItem);
            menuItem.Parent.RemoveItemAt(index);
            menuItem.Parent.RefreshIndex();
            return true;
        }

        /// <summary>
        /// Displays a hidden questioning menu item by id
        /// </summary>
        /// <param name="questionId">the button id to show</param>
        public bool ShowQuestionById(string questionId)
        {
            // Already being displayed?
            if (!HiddenMenuItems.ContainsKey(questionId) || !Questions.ContainsKey(questionId))
                return false;

            // Grab menu ID for this item ID
            var menuItem = HiddenMenuItems[questionId];

            // Add Item
            menuItem.Parent.AddItem(menuItem);
            menuItem.Parent.RefreshIndex();

            // Remove from hidden items
            HiddenMenuItems.Remove(questionId);
            return true;
        }

        /// <summary>
        /// Adds a question menu item to the internal list, and registers for the click event
        /// </summary>
        /// <param name="question"></param>
        /// <param name="visible">Indicates whether this <see cref="UIMenuItem{T}"/> is initially visible</param>
        public void AddQuestion(UIMenuItem<Question> question, bool visible)
        {
            // Register for event
            question.Activated += On_QuestionActivate;

            // Add named item to our hash table
            if (!Questions.ContainsKey(question.Tag.Id))
            {
                Questions.Add(question.Tag.Id, question);
            }

            // Handle item visibility
            if (!visible)
            {
                // Store for later
                HiddenMenuItems.Add(question.Tag.Id, question);
            }
            else
            {
                question.Parent.AddItem(question);
            }
        }

        /// <summary>
        /// Adds a <see cref="PedResponse"/> to a <see cref="Question"/>
        /// </summary>
        /// <param name="questionId"></param>
        /// <param name="response"></param>
        /// <returns>
        /// False if the question does not exist in the officer dialog, or if the <see cref="PedResponse"/> has already been added. 
        /// True otherwise
        /// </returns>
        public bool AddPedResponse(string questionId, PedResponse response)
        {
            // Ensure the question exists, and not a duplicate
            if (!Questions.ContainsKey(questionId) || PedResponses.ContainsKey(questionId))
            {
                return false;
            }

            PedResponses.Add(questionId, response);
            return true;
        }

        /// <summary>
        /// Adds a menu with the unique menu id
        /// </summary>
        /// <param name="menuId"></param>
        /// <param name="menu"></param>
        public void AddQuestioningSubMenu(string menuId, UIMenu menu)
        {
            if (!MenusById.ContainsKey(menuId))
            {
                AllMenus.Add(menu);
                MenusById.Add(menuId, menu);

                AllMenus.RefreshIndex();
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
        private void On_QuestionActivate(UIMenu sender, UIMenuItem selectedMenuItem)
        {
            // Convert to my menu item
            var selectedItem = selectedMenuItem as UIMenuItem<Question>;
            if (selectedMenuItem == null) return;

            // Disable button from spam clicks
            selectedItem.Enabled = false;
            var question = selectedItem.Tag;

            // Dont do anything right now!
            var response = GetResponseToQuestionId(question.Id);
            if (response == null)
            {
                Rage.Game.DisplayNotification($"~r~No XML Response[@to='~b~{question.Id}~r~'] node for this Ped.");
                return;
            }

            // Grab dialog for player
            var officerSequence = question.GetRandomSequence();
            if (officerSequence == null || officerSequence.Elements.Length == 0)
            {
                Log.Error($"FlowSquence.On_QuestionActivated: Question '{question.Id}' has no Dialog");
                return;
            }

            // Grab dialog for ped
            var pedSequence = response.GetPersistantSequence();
            if (pedSequence == null || pedSequence.Elements.Length == 0)
            {
                Log.Error($"FlowSquence.On_QuestionActivate: Response to '{question.Id}' has no Dialog");
                return;
            }

            // Clear any queued messages
            SubtitleQueue.Clear();

            // Officer Dialogs first
            PlaySequence(officerSequence, Rage.Game.LocalPlayer.Character, "~y~You~w~:");

            // Has the player asked this question already?
            if (selectedItem.ForeColor == Color.Gray)
            {
                var line = new CommunicationElement(GetRepeatPrefixText(), 2500)
                {
                    PrefixText = $"~y~{SubjectPed.Persona.Forename}~w~:",
                    Speaker = SubjectPed.Ped
                };

                // Add line
                SubtitleQueue.Add(line);
            }

            // Add Ped response
            PlaySequence(pedSequence, SubjectPed, $"~y~{SubjectPed.Persona.Forename}~w~:");

            // Enable button and change font color to show its been clicked before
            selectedItem.Enabled = true;
            selectedItem.ForeColor = Color.Gray;

            // Fire off event
            OnPedResponse(this, question, response, pedSequence);
        }

        /// <summary>
        /// Plays the dialog
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="Speaker"></param>
        /// <param name="prefix"></param>
        private void PlaySequence(CommunicationSequence sequence, Ped Speaker, string prefix)
        {
            // Add Ped response
            int index = 0;
            int lastIndex = sequence.Elements.Length - 1;
            foreach (CommunicationElement line in sequence.Elements)
            {
                // Do we have a callback for "onFirstShown" ?
                if (index == 0)
                {
                    AttachCallbackAction_OnDisplayed(sequence.CallOnFirstShown, line);
                }

                // Check for last subtitle events
                if (index == lastIndex)
                {
                    // Do we have a callback for "onLastShown" ?
                    AttachCallbackAction_OnDisplayed(sequence.CallOnLastShown, line);

                    // Do we have a callback for "elapsed" ?
                    AttachedCallbackAction_OnElapsed(sequence.CallOnElapsed, line);
                }

                // Set ped in statement to allow animations
                line.Speaker = Speaker;
                line.PrefixText = prefix;
                line.Text = VariableExpression.ReplaceTokens(line.Text, Variables);

                // Add line
                SubtitleQueue.Add(line);
                index++;
            }
        }

        /// <summary>
        /// Adds the callback action to the <see cref="SubtitleEventHandler"/>
        /// </summary>
        /// <param name="callbackName">The name of the callback</param>
        /// <param name="subtitle">The <see cref="Subtitle"/></param>
        private void AttachCallbackAction_OnDisplayed(string callbackName, Subtitle subtitle)
        {
            // Do we have a callback for "elapsed" ?
            if (!String.IsNullOrEmpty(callbackName) && Callbacks.TryGetValue(callbackName, out Action action2))
            {
                void handler(Subtitle s)
                {
                    subtitle.Elapsed -= handler;
                    action2.Invoke();
                }

                subtitle.OnDisplayed += handler;
            }
        }

        /// <summary>
        /// Adds the callback action to the <see cref="SubtitleEventHandler"/>
        /// </summary>
        /// <param name="callbackName">The name of the callback</param>
        /// <param name="subtitle">The <see cref="Subtitle"/></param>
        private void AttachedCallbackAction_OnElapsed(string callbackName, Subtitle subtitle)
        {
            // Do we have a callback for "elapsed" ?
            if (!String.IsNullOrEmpty(callbackName) && Callbacks.TryGetValue(callbackName, out Action action))
            {
                void handler(Subtitle s)
                {
                    subtitle.Elapsed -= handler;
                    action.Invoke();
                }

                subtitle.Elapsed += handler;
            }
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

                foreach (var menu in MenusById)
                {
                    menu.Value.Clear();
                }

                MenusById.Clear();
                MenusById = null;

                HiddenMenuItems.Clear();
                SubjectPed = null;

                Callbacks.Clear();
                Callbacks = null;

                Questions.Clear();
                Questions = null;

                Variables.Clear();
                Variables = null;
            }
        }
    }
}
