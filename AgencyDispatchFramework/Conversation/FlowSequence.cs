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
using System.Xml;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a conversation flow for a <see cref="Rage.Ped"/> based on menu input by the player
    /// </summary>
    public class FlowSequence
    {
        /// <summary>
        /// Contains the regular expression to replace variables within Ped responses
        /// </summary>
        private static Regex VariableExpression = new Regex(@"\$(\w+)\$", RegexOptions.Compiled);

        /// <summary>
        /// Gets the sequence ID for this conversation event
        /// </summary>
        public string SequenceId { get; protected set; }

        /// <summary>
        /// Gets the <see cref="Rage.Ped"/> this <see cref="FlowSequence"/> is attacthed to
        /// </summary>
        public GamePed SubjectPed { get; private set; }

        /// <summary>
        /// Contains the main menu string name
        /// </summary>
        protected string InitialMenuName { get; set; }

        /// <summary>
        /// A <see cref="MenuPool"/> that contains all of this <see cref="FlowSequence"/> <see cref="UIMenu"/>s
        /// </summary>
        internal MenuPool AllMenus { get; set; }

        /// <summary>
        /// Contains a list of flow return menu's by name
        /// </summary>
        protected Dictionary<string, UIMenu> FlowReturnMenus { get; set; }

        /// <summary>
        /// Contains a list of <see cref="UIMenuItem"/>s by id
        /// that it belongs to
        /// </summary>
        protected Dictionary<string, UIMenuItem> MenuButtonsById { get; set; }

        /// <summary>
        /// Contains a list of flow return menu items that are initially no visible
        /// </summary>
        protected Dictionary<string, UIMenuItem> HiddenMenuItems { get; set; }

        /// <summary>
        /// Contains our <see cref="ResponseSet"/> for this <see cref="FlowSequence"/>
        /// </summary>
        internal ResponseSet PedResponses { get; set; }

        /// <summary>
        /// Contains officer dialogs attached to each menu option
        /// </summary>
        protected Dictionary<string, Subtitle[]> OfficerDialogs { get; set; }

        /// <summary>
        /// Contains string replacements in the Output strings
        /// </summary>
        protected Dictionary<string, object> Variables { get; set; }

        /// <summary>
        /// Contains the expression parser used on the "if" attribuutes
        /// </summary>
        protected ExpressionParser Parser { get; set; }

        /// <summary>
        /// Gets the FlowOutcome name selected for this conversation event
        /// </summary>
        public string FlowOutcomeId { get; protected set; }

        /// <summary>
        /// Delegate to handle a <see cref="FlowSequenceEvent"/>
        /// </summary>
        /// <param name="sender">The <see cref="FlowSequence"/> that triggered the <see cref="PedResponse"/></param>
        /// <param name="e">The <see cref="PedResponse"/> instance</param>
        /// <param name="l">The displayed <see cref="Statement"/> to the player in game</param>
        public delegate void PedResponseEventHandler(FlowSequence sender, PedResponse e, Statement l);

        /// <summary>
        /// Event fired whenever a <see cref="PedResponse"/> is displayed
        /// </summary>
        public event PedResponseEventHandler OnPedResponse;

        /// <summary>
        /// Creates a new <see cref="FlowSequence"/> instance
        /// </summary>
        /// <param name="ped">The conversation subject ped</param>
        /// <param name="outcomeId">The selected outcome <see cref="ResponseSet"/></param>
        /// <param name="document">The XML document containing the <see cref="FlowSequence"/> XML data</param>
        /// <param name="parser">An <see cref="ExpressionParser" /> containing parameters to parse "if" statements in the XML</param>
        public FlowSequence(string sequenceId, Ped ped, FlowOutcome outcome, XmlDocument document, ExpressionParser parser)
        {
            // Set internal vars
            SequenceId = sequenceId;
            FlowOutcomeId = outcome.Id;
            SubjectPed = new GamePed(ped);
            Parser = parser;
            Variables = new Dictionary<string, object>();
            HiddenMenuItems = new Dictionary<string, UIMenuItem>();
            MenuButtonsById = new Dictionary<string, UIMenuItem>();

            // Parse XML document
            LoadXml(document, outcome.Id);
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
        /// Dispose method to clear memory
        /// </summary>
        public void Dispose()
        {
            if (AllMenus != null)
            {
                AllMenus.CloseAllMenus();
                AllMenus.Clear();
                AllMenus = null;
            }

            foreach (var menu in FlowReturnMenus)
            {
                menu.Value.Clear();
            }

            FlowReturnMenus.Clear();
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
            foreach (Subtitle line in statement.Subtitles)
            {
                // Set ped in statement to allow animations
                line.Speaker = SubjectPed.Ped;
                line.PrefixText = $"~y~{SubjectPed.Persona.Forename}~w~:";
                line.Text = VariableExpression.ReplaceTokens(line.Text, Variables);

                // Add line
                SubtitleQueue.Add(line);
            }

            // Enable button and change font color to show its been clicked before
            selectedItem.Enabled = true;
            selectedItem.ForeColor = Color.Gray;

            // Fire off event
            OnPedResponse(this, response, statement);
        }

        #region XML loading methods

        private void LoadXml(XmlDocument document, string outcomeId)
        {
            var documentRoot = document.SelectSingleNode("FlowSequence");
            AllMenus = new MenuPool();
            FlowReturnMenus = new Dictionary<string, UIMenu>();
            OfficerDialogs = new Dictionary<string, Subtitle[]>();

            // ====================================================
            // Load input flow return menus
            // ====================================================
            XmlNode catagoryNode = documentRoot.SelectSingleNode("Input");
            if (catagoryNode == null || !catagoryNode.HasChildNodes)
            {
                throw new ArgumentNullException("Input");
            }

            // Load FlowReturn items
            int i = 0;
            foreach (XmlNode n in catagoryNode.SelectNodes("Menu"))
            {
                // Validate and extract attributes
                if (n.Attributes == null || n.Attributes["id"]?.Value == null)
                {
                    Log.Warning($"FlowSequence.LoadXml(): Menu node has no 'id' attribute");
                    continue;
                }

                // Create
                string menuId = n.Attributes["id"].Value;
                var menu = new UIMenu("Ped Interaction", $"~y~{SubjectPed.Persona.FullName}");

                // Extract menu items
                foreach (XmlNode menuItemNode in n.SelectNodes("MenuItem"))
                {
                    // Extract attributes
                    if (menuItemNode.Attributes == null)
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no attributes");
                        continue;
                    }

                    // Extract id attriibute
                    string buttonId = menuItemNode.Attributes["id"]?.Value;
                    if (String.IsNullOrWhiteSpace(buttonId))
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no or empty 'id' attribute");
                        continue;
                    }

                    // Ensure ID is unique
                    if (OfficerDialogs.ContainsKey(buttonId))
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem 'id' attribute is not unique");
                        continue;
                    }

                    // Extract button text attriibute
                    string text = menuItemNode.Attributes["buttonText"]?.Value;
                    if (String.IsNullOrWhiteSpace(text))
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no or empty 'buttonText' attribute");
                        continue;
                    }

                    // Extract visible attriibute
                    bool isVisible = true;
                    string val = menuItemNode.Attributes["visible"]?.Value;
                    if (!String.IsNullOrWhiteSpace(text))
                    {
                        if (!bool.TryParse(val, out isVisible))
                        {
                            isVisible = true;
                            Log.Warning($"FlowSequence.LoadXml(): Menu -> MenuItem attribute 'visible' attribute is not correctly formatted");
                        }
                    }

                    // Ensure we have lines to read
                    if (!menuItemNode.HasChildNodes)
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no Line child nodes");
                        continue;
                    }

                    // Load dialog for officer
                    var subNodes = menuItemNode.SelectNodes("Subtitle");
                    if (subNodes != null && subNodes.Count > 0)
                    {
                        List<Subtitle> lines = new List<Subtitle>(subNodes.Count);
                        foreach (XmlNode lNode in subNodes)
                        {
                            // Validate and extract attributes
                            if (lNode.Attributes == null || !Int32.TryParse(lNode.Attributes["time"]?.Value, out int time))
                            {
                                time = 3000;
                            }

                            lines.Add(new Subtitle() { Text = lNode.InnerText, Duration = time });
                        }

                        // Save lines
                        OfficerDialogs.Add(buttonId, lines.ToArray());
                    }
                    else
                    {
                        // Log
                        Log.Warning("FlowSequence.LoadXml(): Menu -> MenuItem has no Line nodes");

                        // Save default line
                        OfficerDialogs.Add(buttonId, new Subtitle[] { new Subtitle() { Text = text, Duration = 3000 } });
                    }

                    // Create button
                    var item = new MyUIMenuItem<string>(text);
                    item.Tag = buttonId;
                    item.Parent = menu; // !! Important !!
                    item.Activated += On_ItemActivated;

                    // Add named item to our hash table
                    if (!MenuButtonsById.ContainsKey(buttonId))
                    {
                        MenuButtonsById.Add(buttonId, item);
                    }

                    // Handle item visibility
                    if (!isVisible)
                    {
                        // Store for later
                        HiddenMenuItems.Add(buttonId, item);
                    }
                    else
                    {
                        menu.AddItem(item);
                    }
                }

                // Add menu
                AllMenus.Add(menu);
                FlowReturnMenus.Add(menuId, menu);

                if (i == 0)
                {
                    InitialMenuName = menuId;
                }

                i++;
            }

            // Call menu refresh
            AllMenus.RefreshIndex();

            // ====================================================
            // Load response sequences
            // ====================================================
            catagoryNode = documentRoot.SelectSingleNode($"Output/FlowOutcome[@id = '{outcomeId}']");
            if (catagoryNode == null || !catagoryNode.HasChildNodes)
            {
                throw new ArgumentNullException("Output", $"Scenario FlowOutcome does not exist '{outcomeId}'");
            }

            // Validate and extract attributes
            if (!String.IsNullOrWhiteSpace(catagoryNode.Attributes["initialMenu"]?.Value))
            {
                // Ensure menu exists
                var menuName = catagoryNode.Attributes["initialMenu"].Value;
                if (FlowReturnMenus.ContainsKey(menuName))
                {
                    InitialMenuName = menuName;
                }
                else
                {
                    // Log as warning and continue
                    Log.Warning($"FlowSequence.LoadXml(): Specified initial menu for FlowOutcome '{FlowOutcomeId}' does not exist!");
                }
            }
            else
            {
                // Log as warning and continue
                Log.Warning($"FlowSequence.LoadXml(): FlowOutcome '{FlowOutcomeId}' does not have an 'initialMenu' attribute!");
            }

            // Extract Ped data
            var subNode = catagoryNode.SelectSingleNode("Ped");
            if (subNode == null || !subNode.HasChildNodes)
            {
                throw new ArgumentNullException("Output", $"Scenario FlowOutcome '{outcomeId}' does not contain a Ped node");
            }

            // Extract drunk attribute
            var random = new CryptoRandom();
            if (Int32.TryParse(catagoryNode.Attributes["drunkChance"]?.Value, out int drunkChance))
            {
                // Set ped to drunk?
                if (drunkChance.InRange(1, 100) && random.Next(1, 100) <= drunkChance)
                {
                    SubjectPed.IsDrunk = true;
                }
            }

            // Extract high attribute
            if (Int32.TryParse(catagoryNode.Attributes["highChance"]?.Value, out drunkChance))
            {
                // Set ped to drunk?
                if (drunkChance.InRange(1, 100) && random.Next(1, 100) <= drunkChance)
                {
                    SubjectPed.IsHigh = true;
                }
            }

            // ====================================================
            // @TODO :: Extract inventory items
            // ====================================================

            // ====================================================
            // Get SubjectPed Demeanor
            // ====================================================
            subNode = subNode.SelectSingleNode("Presentation");
            var items = subNode.SelectNodes("Demeanor");
            if (subNode == null || items.Count == 0)
            {
                SubjectPed.Demeanor = PedDemeanor.Calm;

                // Log as warning and continue
                Log.Warning($"FlowSequence.LoadXml(): FlowOutcome '{FlowOutcomeId}' does not have a Ped->Presentation node");
            }
            else
            {
                var gen = new ProbabilityGenerator<Spawnable<PedDemeanor>>();
                foreach (XmlNode item in items)
                {
                    // Skip if we cant parse the value
                    if (!Enum.TryParse(item.InnerText, out PedDemeanor demeanor))
                    {
                        // Log as warning and continue
                        Log.Warning($"FlowSequence.LoadXml(): Unable to parse FlowOutcome['{FlowOutcomeId}'] Ped->Presentation->Demeanor node value");
                        continue;
                    }                    

                    // Skip if we cant parse the value
                    if (!Int32.TryParse(item.Attributes["probability"]?.Value, out int prob))
                    {
                        // Log as warning and continue
                        Log.Warning($"FlowSequence.LoadXml(): Unable to parse FlowOutcome['{FlowOutcomeId}'] Ped->Presentation->Demeanor probability attribute value");
                        continue;
                    }

                    // See if we have an IF statement
                    if (!String.IsNullOrEmpty(item.Attributes["if"]?.Value))
                    {
                        // Do we have a parser?
                        if (Parser == null)
                        {
                            Log.Warning($"FlowSequence.LoadXml(): Statement from FlowOutcome['{FlowOutcomeId}']->Ped->Presentation->Demeanor has 'if' statement but no ExpressionParser is defined... Skipping");
                            continue;
                        }

                        // parse expression
                        if (!Parser.Evaluate<bool>(item.Attributes["if"].Value))
                        {
                            // If we failed to evaluate to true, skip
                            continue;
                        }
                    }

                    gen.Add(new Spawnable<PedDemeanor>(prob, demeanor));
                }

                // If we extracted anything
                if (gen.ItemCount > 0)
                {
                    SubjectPed.Demeanor = gen.Spawn().Value;
                }
            }

            // Load flow outcome
            PedResponses = new ResponseSet(outcomeId, catagoryNode.SelectSingleNode("Responses"), Parser);
        }

        #endregion XML loading methods
    }
}
