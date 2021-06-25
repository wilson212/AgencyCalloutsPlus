using AgencyDispatchFramework.Conversation;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.NativeUI;
using LSPD_First_Response.Engine.Scripting.Entities;
using RAGENativeUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    /// <summary>
    /// A class used to parse Flow Sequence XML files and create <see cref="FlowSequence"/> instances
    /// using the data in the file
    /// </summary>
    public class FlowSequenceFile : XmlFileBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="FlowSequenceFile"/> using the specified
        /// file path
        /// </summary>
        /// <param name="filePath">The full file path to the FlowSequence xml file</param>
        public FlowSequenceFile(string filePath) : base(filePath)
        {
            // === Document loaded successfully by base class if we are here === //
            // 
        }

        /// <summary>
        /// Parses the flow sequence xml file returns a new <see cref="FlowSequence"/>
        /// </summary>
        /// <param name="outcome">
        /// The <see cref="FlowOutcome"/> selected for this conversation sequence. Only this <see cref="FlowOutcome"/> will be parsed.
        /// </param>
        /// <param name="ped">The ped that will respond with the answers in this sequence</param>
        /// <param name="parser">The <see cref="ExpressionParser"/> used for evaluating expression strings in the XML "if" attributes</param>
        /// <returns>A <see cref="FlowSequence"/> instance</returns>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        public FlowSequence Parse(FlowOutcome outcome, GamePed ped, ExpressionParser parser)
        {
            string sequenceId = Path.GetFileNameWithoutExtension(FilePath);
            var sequence = new FlowSequence(sequenceId, ped, outcome);
            var documentRoot = Document.SelectSingleNode("FlowSequence");

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
                    Log.Warning($"FlowSequenceFile.Parse(): Menu node has no 'id' attribute");
                    continue;
                }

                // Create
                string menuId = n.Attributes["id"].Value;
                var menu = new UIMenu("Ped Interaction", $"~y~{ped.Persona.FullName}");

                // Extract Officer dialogs
                foreach (XmlNode questionNode in n.SelectNodes("Question"))
                {
                    // Extract attributes
                    if (questionNode.Attributes == null)
                    {
                        Log.Error($"FlowSequenceFile.Parse(): Menu -> MenuItem has no attributes");
                        continue;
                    }

                    // Extract id attriibute
                    string questionId = questionNode.Attributes["id"]?.Value;
                    if (String.IsNullOrWhiteSpace(questionId))
                    {
                        Log.Error($"FlowSequenceFile.Parse(): Menu -> MenuItem has no or empty 'id' attribute");
                        continue;
                    }

                    // Ensure ID is unique
                    if (sequence.Questions.ContainsKey(questionId))
                    {
                        Log.Error($"FlowSequenceFile.Parse(): Menu -> Question 'id' attribute is not unique");
                        continue;
                    }

                    // Extract button text attriibute
                    string text = questionNode.Attributes["buttonText"]?.Value;
                    if (String.IsNullOrWhiteSpace(text))
                    {
                        Log.Error($"FlowSequenceFile.Parse(): Menu -> MenuItem has no or empty 'buttonText' attribute");
                        continue;
                    }

                    // Extract visible attriibute
                    bool isVisible = true;
                    string val = questionNode.Attributes["visible"]?.Value;
                    if (!String.IsNullOrWhiteSpace(text))
                    {
                        if (!bool.TryParse(val, out isVisible))
                        {
                            isVisible = true;
                            Log.Warning($"FlowSequenceFile.Parse(): Menu -> MenuItem attribute 'visible' attribute is not correctly formatted");
                        }
                    }

                    // Ensure we have lines to read
                    if (!questionNode.HasChildNodes)
                    {
                        Log.Error($"FlowSequenceFile.Parse(): Menu -> MenuItem has no Line child nodes");
                        continue;
                    }

                    // Load dialog for officer
                    var question = new Question(questionId);
                    var subNodes = questionNode.SelectNodes("Dialog");
                    foreach (XmlNode dialogNode in subNodes)
                    {
                        if (TryParseDialogNode(dialogNode, parser, out Dialog dialog))
                        {
                            question.AddDialog(dialog);
                        }
                    }
                    
                    // If we failed to parse any dialog nodes, cancel this question
                    if (question.DialogCount == 0)
                    {
                        // Log
                        Log.Warning("FlowSequenceFile.Parse(): Menu -> Question node has no dialogs");
                        continue;
                    }

                    // Add button
                    sequence.AddQuestion(new UIMenuItem<Question>(text) { Tag = question, Parent = menu }, isVisible);
                }

                // Add menu
                sequence.AddQuestioningSubMenu(menuId, menu);

                // Ensure to set an initial opening menu
                if (i == 0)
                {
                    sequence.InitialMenuName = menuId;
                }

                i++;
            }

            // Load all flow outcome id's
            var mapping = new Dictionary<string, XmlNode>();
            foreach (XmlNode node in documentRoot.SelectNodes("Output/FlowOutcome"))
            {
                var ids = node.GetAttribute("id")?.Split(',');
                if (ids == null || ids.Length == 0)
                {
                    throw new ArgumentNullException("Output", $"Scenario FlowOutcome does not contain any id's");
                }

                foreach (string name in ids)
                {
                    mapping.Add(name, node);
                }
            }

            // Ensure flow outcome ID exists
            if (!mapping.ContainsKey(outcome.Id))
            {
                throw new ArgumentNullException("Output", $"Scenario FlowOutcome does not exist '{outcome.Id}'");
            }

            // ====================================================
            // Load response sequences
            // ====================================================
            catagoryNode = mapping[outcome.Id];
            if (catagoryNode == null || !catagoryNode.HasChildNodes)
            {
                throw new ArgumentNullException("Output", $"Scenario FlowOutcome does not exist '{outcome.Id}'");
            }

            // Validate and extract attributes
            if (!String.IsNullOrWhiteSpace(catagoryNode.Attributes["initialMenu"]?.Value))
            {
                // Ensure menu exists
                var menuName = catagoryNode.Attributes["initialMenu"].Value;
                if (sequence.MenusById.ContainsKey(menuName))
                {
                    sequence.InitialMenuName = menuName;
                }
                else
                {
                    // Log as warning and continue
                    Log.Warning($"FlowSequenceFile.Parse(): Specified initial menu for FlowOutcome '{outcome.Id}' does not exist!");
                }
            }
            else
            {
                // Log as warning and continue
                Log.Warning($"FlowSequenceFile.Parse(): FlowOutcome '{outcome.Id}' does not have an 'initialMenu' attribute!");
            }

            // Extract Ped data
            var subNode = catagoryNode.SelectSingleNode("Ped");
            if (subNode == null || !subNode.HasChildNodes)
            {
                throw new ArgumentNullException("Output", $"Scenario FlowOutcome '{outcome.Id}' does not contain a Ped node");
            }

            // Extract drunk attribute
            var random = new CryptoRandom();
            if (Int32.TryParse(subNode.Attributes["drunkChance"]?.Value, out int drunkChance))
            {
                // Set ped to drunk?
                if (drunkChance.InRange(1, 100) && random.Next(1, 100) <= drunkChance)
                {
                    Log.Debug($"Setting Ped {ped} as Drunk");
                    ped.IsDrunk = true;
                }
            }

            // Extract high attribute
            if (Int32.TryParse(subNode.Attributes["highChance"]?.Value, out int highChance))
            {
                // Set ped to drunk?
                if (highChance.InRange(1, 100) && random.Next(1, 100) <= highChance)
                {
                    Log.Debug($"Setting Ped {ped} as High");
                    ped.IsHigh = true;
                }
            }

            // ====================================================
            // Extract inventory items
            // ====================================================
            var invNode = subNode.SelectSingleNode("Inventory");
            var items = default(XmlNodeList);

            // Add items within the Inventory node
            if (invNode != null && invNode.HasChildNodes)
            {
                // Clear existing contraband?
                if (bool.TryParse(invNode.Attributes["clearContraband"]?.Value, out bool clearC))
                {
                    if (clearC)
                    {
                        Log.Debug($"Clearing contraband inventory for Ped {ped}");
                        ped.ClearContraband(true, true, true);
                    }
                }

                // Clear existing weapons?
                if (bool.TryParse(invNode.Attributes["clearWeapons"]?.Value, out bool clearW))
                {
                    if (clearW)
                    {
                        Log.Debug($"Clearing weapon inventory for Ped {ped}");
                        ped.Ped.Inventory.Weapons.Clear();
                    }
                }

                // Contraband
                items = invNode.SelectNodes("Contraband");
                foreach (XmlNode n in items)
                {
                    // Extract chance
                    if (Int32.TryParse(n.Attributes["chance"]?.Value, out int contChance))
                    {
                        // Dont parse this node if we fail the chance check
                        if (!contChance.InRange(1, 100) || random.Next(1, 100) > contChance)
                        {
                            continue;
                        }
                    }

                    // 
                    // If we are here, we passed the chance check. Finish extracting data
                    //

                    // Skip if we cant parse the value
                    if (!Enum.TryParse(n.Attributes["type"]?.Value, out ContrabandType type))
                    {
                        // Log as warning and continue
                        Log.Warning($"FlowSequenceFile.Parse(): Unable to parse FlowOutcome['{outcome.Id}']->Ped->Inventory->Contraband type attribute");
                        continue;
                    }

                    // Grab items
                    var subItems = n.SelectNodes("Item");
                    if (subItems.Count == 0)
                    {
                        // Log as warning and continue
                        Log.Warning($"FlowSequenceFile.Parse(): FlowOutcome['{outcome.Id}']->Ped->Inventory->Contraband has no Item nodes");
                        continue;
                    }

                    // Select a single item from the list based on probability
                    var gen = new ProbabilityGenerator<Spawnable<string>>();
                    foreach (XmlNode item in subItems)
                    {
                        // Skip if we cant parse the value
                        if (!Int32.TryParse(item.Attributes["probability"]?.Value, out int prob))
                        {
                            // Log as warning and continue
                            Log.Warning($"FlowSequenceFile.Parse(): Unable to parse FlowOutcome['{outcome.Id}']->Ped->Inventory->Contraband->Item probability attribute value");
                            continue;
                        }

                        // See if we have an IF statement
                        if (!String.IsNullOrEmpty(item.Attributes["if"]?.Value))
                        {
                            // Do we have a parser?
                            if (parser == null)
                            {
                                Log.Warning($"FlowSequenceFile.Parse(): Statement from FlowOutcome['{outcome.Id}']->Ped->Inventory->Contraband->Item has 'if' statement but no ExpressionParser is defined... Skipping");
                                continue;
                            }

                            // Execute the expression
                            var result = parser.Execute<bool>(item.Attributes["if"].Value);
                            if (!result.Success)
                            {
                                // If we failed to execute the expression, log the error
                                result.LogResult();
                                continue;
                            }
                            else if (!result.Value)
                            {
                                // We did not statisfy the condition
                                continue;
                            }
                        }

                        gen.Add(new Spawnable<string>(prob, item.InnerText));
                    }

                    // Add item to inventory
                    if (gen.TrySpawn(out Spawnable<string> spawned))
                    {
                        Log.Debug($"Adding contraband of type '{type}' with name '{spawned.Value}' to Ped {ped}");
                        ped.AddContrabandByType(spawned.Value, type);
                    }
                }

                // Weapons
                items = invNode.SelectNodes("Weapon");
                foreach (XmlNode n in items)
                {
                    // Extract chance
                    if (Int32.TryParse(n.Attributes["chance"]?.Value, out int contChance))
                    {
                        // Dont parse this node if we fail the chance check
                        if (!contChance.InRange(1, 100) || random.Next(1, 100) > contChance)
                        {
                            continue;
                        }
                    }

                    // 
                    // If we are here, we passed the chance check. Finish extracting data
                    //

                    // Grab items
                    var subItems = n.SelectNodes("Item");
                    if (subItems.Count == 0)
                    {
                        // Log as warning and continue
                        Log.Warning($"FlowSequenceFile.Parse(): FlowOutcome['{outcome.Id}']->Ped->Inventory->Contraband has no Item nodes");
                        continue;
                    }

                    // Select a single item from the list based on probability
                    var gen = new ProbabilityGenerator<Spawnable<Tuple<string, string, short>>>();
                    foreach (XmlNode item in subItems)
                    {
                        // Skip if we cant parse the value
                        if (!Int32.TryParse(item.Attributes["probability"]?.Value, out int prob))
                        {
                            // Log as warning and continue
                            Log.Warning($"FlowSequenceFile.Parse(): Unable to parse FlowOutcome['{outcome.Id}']->Ped->Inventory->Weapon->Item probability attribute value");
                            continue;
                        }

                        // See if we have an IF statement
                        if (!String.IsNullOrEmpty(item.Attributes["if"]?.Value))
                        {
                            // Do we have a parser?
                            if (parser == null)
                            {
                                Log.Warning($"FlowSequenceFile.Parse(): Statement from FlowOutcome['{outcome.Id}']->Ped->Inventory->Weapon->Item has 'if' statement but no ExpressionParser is defined... Skipping");
                                continue;
                            }

                            // Execute the expression
                            var result = parser.Execute<bool>(item.Attributes["if"].Value);
                            if (!result.Success)
                            {
                                // If we failed to execute the expression, log the error
                                result.LogResult();
                                continue;
                            }
                            else if (!result.Value)
                            {
                                // We did not statisfy the condition
                                continue;
                            }
                        }

                        // See if we have an IF statement
                        if (String.IsNullOrEmpty(item.Attributes["id"]?.Value))
                        {
                            Log.Warning($"FlowSequenceFile.Parse(): Empty or missing FlowOutcome['{outcome.Id}']->Ped->Inventory->Weapon->Item id attribute value");
                            continue;
                        }

                        // Skip if we cant parse the value
                        if (!short.TryParse(item.Attributes["ammo"]?.Value, out short ammo))
                        {
                            // Log as warning and continue
                            Log.Warning($"FlowSequenceFile.Parse(): Unable to parse FlowOutcome['{outcome.Id}']->Ped->Inventory->Weapon->Item ammo attribute value");
                            continue;
                        }

                        // Add weapon
                        gen.Add(new Spawnable<Tuple<string, string, short>>(prob, new Tuple<string, string, short>(item.Attributes["id"].Value, item.InnerText, ammo)));
                    }

                    // Add item to inventory
                    if (gen.TrySpawn(out Spawnable<Tuple<string, string, short>> tpl))
                    {
                        var weapon = tpl.Value;
                        Log.Debug($"Adding weapon id '{weapon.Item1}' with name '{weapon.Item2}' to Ped {ped}");
                        ped.AddWeapon(weapon.Item2, weapon.Item1, weapon.Item3, false);
                    }
                }

                // Save inventory changes
                ped.SaveContraband();
            }

            // ====================================================
            // Get SubjectPed Demeanor
            // ====================================================
            subNode = subNode.SelectSingleNode("Presentation");
            items = subNode.SelectNodes("Demeanor");
            if (subNode == null || items.Count == 0)
            {
                // Log as warning and continue
                Log.Debug($"FlowSequenceFile.Parse(): FlowOutcome '{outcome.Id}' does not have a Ped->Presentation node");
                ped.Demeanor = PedDemeanor.Calm;
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
                        Log.Warning($"FlowSequenceFile.Parse(): Unable to parse FlowOutcome['{outcome.Id}'] Ped->Presentation->Demeanor node value");
                        continue;
                    }

                    // Skip if we cant parse the value
                    if (!Int32.TryParse(item.Attributes["probability"]?.Value, out int prob))
                    {
                        // Log as warning and continue
                        Log.Warning($"FlowSequenceFile.Parse(): Unable to parse FlowOutcome['{outcome.Id}'] Ped->Presentation->Demeanor probability attribute value");
                        continue;
                    }

                    // See if we have an IF statement
                    if (!String.IsNullOrEmpty(item.Attributes["if"]?.Value))
                    {
                        // Do we have a parser?
                        if (parser == null)
                        {
                            Log.Warning($"FlowSequenceFile.Parse(): Statement from FlowOutcome['{outcome.Id}']->Ped->Presentation->Demeanor has 'if' statement but no ExpressionParser is defined... Skipping");
                            continue;
                        }

                        // Execute the expression
                        var result = parser.Execute<bool>(item.Attributes["if"].Value);
                        if (!result.Success)
                        {
                            // If we failed to execute the expression, log the error
                            result.LogResult();
                            continue;
                        }
                        else if (!result.Value)
                        {
                            // We did not statisfy the condition
                            continue;
                        }
                    }

                    gen.Add(new Spawnable<PedDemeanor>(prob, demeanor));
                }

                // If we extracted anything
                if (gen.ItemCount > 0)
                {
                    ped.Demeanor = gen.Spawn().Value;
                    Log.Debug($"Setting Ped demeanor for {ped} to {ped.Demeanor}");
                }
            }

            // Select all response nodes
            XmlNodeList responses = catagoryNode.SelectSingleNode("Responses")?.SelectNodes("Response");
            if (responses == null || responses.Count == 0)
            {
                throw new ArgumentNullException("Response", "FlowOutcome has no Response nodes.");
            }

            // Each Response Node
            foreach (XmlNode responseNode in responses)
            {
                // Validate and extract attributes
                if (responseNode.Attributes == null || responseNode.Attributes["to"]?.Value == null)
                {
                    Log.Error($"FlowSequenceFile.Parse(): Response has no 'to' attribute");
                    continue;
                }

                // Validate and extract attributes
                if (responseNode.Attributes["returnMenu"]?.Value == null)
                {
                    Log.Error($"FlowSequenceFile.Parse(): Response has no 'returnMenu' attribute");
                    continue;
                }

                // Check for line sets
                var dialogNodes = responseNode.SelectNodes("Dialog");
                if (dialogNodes == null || dialogNodes.Count == 0)
                {
                    Log.Error($"FlowSequenceFile.Parse(): Response has no Dialog child nodes");
                    continue;
                }

                // Create response object
                string questionId = responseNode.Attributes["to"].Value;
                var response = new PedResponse(questionId, responseNode.Attributes["returnMenu"].Value);

                // See if we have an hide statement to hide menuitems
                if (responseNode.Attributes["hide"]?.Value != null)
                {
                    var menuItems = responseNode.Attributes["hide"].Value.Split(',', ' ');
                    response.HideQuestionIds = menuItems;
                }
                else
                {
                    response.HideQuestionIds = new string[0];
                }

                // See if we have an unhide statement to hide menuitems
                if (responseNode.Attributes["show"]?.Value != null)
                {
                    var menuItems = responseNode.Attributes["show"].Value.Split(',', ' ');
                    response.ShowQuestionIds = menuItems;
                }
                else
                {
                    response.ShowQuestionIds = new string[0];
                }

                // Each LineSet
                foreach (XmlNode dialogNode in dialogNodes)
                {
                    if (TryParseDialogNode(dialogNode, parser, out Dialog statement))
                    {
                        response.AddDialog(statement);
                    }
                }

                // Add final response
                if (!sequence.AddPedResponse(questionId, response))
                {
                    Log.Warning($"FlowSequenceFile.Parse(): Duplicate Response node detected to Question id '{questionId}'");
                }
            }

            // return
            return sequence;
        }

        /// <summary>
        /// Parses a {Dialog} node within a FlowSquence xml file and returns the result
        /// </summary>
        /// <param name="dialogNode"></param>
        /// <param name="parser"></param>
        /// <param name="dialog"></param>
        /// <returns></returns>
        private static bool TryParseDialogNode(XmlNode dialogNode, ExpressionParser parser, out Dialog dialog)
        {
            // Set default value
            dialog = null;

            // Ensure we have lines to read
            if (!dialogNode.HasChildNodes)
            {
                Log.Error($"FlowSequenceFile.Parse(): Statement has no Sentance child nodes");
                return false;
            }

            // Validate and extract attributes
            if (dialogNode.Attributes == null || !Int32.TryParse(dialogNode.Attributes["probability"]?.Value, out int prob))
            {
                Log.Error($"FlowSequenceFile.Parse(): Statement has no attributes or cannot parse 'id' attribute");
                return false;
            }

            // See if we have an IF statement
            if (dialogNode.Attributes["if"]?.Value != null)
            {
                // Do we have a parser?
                if (parser == null)
                {
                    Log.Warning($"FlowSequenceFile.Parse(): Dialog from Response has 'if' statement but no ExpressionParser is defined... Skipping");
                    return false;
                }

                // Execute the expression
                var result = parser.Execute<bool>(dialogNode.Attributes["if"].Value);
                if (!result.Success)
                {
                    // If we failed to evaluate to true, skip
                    result.LogResult();
                    return false;
                }
                else if (!result.Value)
                {
                    // We did not statisfy the condition
                    return false;
                }
            }

            // Create LineSet
            dialog = new Dialog(prob);

            // See if we have an hide statement to hide menuitems
            if (dialogNode.Attributes["hide"]?.Value != null)
            {
                var menuItems = dialogNode.Attributes["hide"].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                dialog.HidesQuestionIds = menuItems;
            }
            else
            {
                dialog.HidesQuestionIds = new string[0];
            }

            // See if we have an unhide statement to hide menuitems
            if (dialogNode.Attributes["show"]?.Value != null)
            {
                var menuItems = dialogNode.Attributes["show"].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                dialog.ShowsQuestionIds = menuItems;
            }
            else
            {
                dialog.ShowsQuestionIds = new string[0];
            }

            // Load callbacks
            dialog.CallOnFirstShown = dialogNode.GetAttribute("onFirstShown");
            dialog.CallOnLastShown = dialogNode.GetAttribute("onLastShown");
            dialog.CallOnElapsed = dialogNode.GetAttribute("elapsed");

            // Fetch response lines
            List<Subtitle> lines = new List<Subtitle>(dialogNode.ChildNodes.Count);
            foreach (XmlNode subTitleNode in dialogNode.SelectNodes("Subtitle"))
            {
                // Validate and extract attributes
                if (!subTitleNode.TryGetAttribute("time", out int time))
                {
                    time = 3000;
                }

                // Does the sub title node have child nodes?
                var textNode = subTitleNode.SelectSingleNode("Text");
                if (textNode != null)
                {
                    // Check for animations
                    var subtitle = new Subtitle(textNode.InnerText, time);

                    // Do we play an animation
                    var sequenceNode = subTitleNode.SelectSingleNode("AnimationSequence");
                    var animationNodes = sequenceNode?.SelectNodes("Animation");
                    if (animationNodes != null)
                    {
                        foreach (XmlNode aNode in animationNodes)
                        {
                            // We must have a dictionary name
                            if (aNode.TryGetAttribute("dictionary", out string dic))
                            {
                                // Create
                                subtitle.AnimationSequence.Add(new PedAnimation()
                                {
                                    Dictionary = dic,
                                    Name = aNode.InnerText
                                });
                            }
                        }

                        // Check for loop attribute value
                        if (sequenceNode.TryGetAttribute("repeat", out bool loop))
                        {
                            subtitle.LoopAnimation = loop;
                        }

                        // Check for terminate attribute value
                        if (sequenceNode.TryGetAttribute("terminate", out bool terminate))
                        {
                            subtitle.TerminateAnimation = terminate;
                        }
                    }
                    // Add
                    lines.Add(subtitle);
                }
                else // The inner text is the text. No animation
                {
                    // Add
                    lines.Add(new Subtitle(subTitleNode.InnerText, time));
                }
            }

            // Save lines
            dialog.Subtitles = lines.ToArray();
            return true;
        } 
    }
}
