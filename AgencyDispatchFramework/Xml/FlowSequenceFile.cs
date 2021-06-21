using AgencyDispatchFramework.Conversation;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.NativeUI;
using LSPD_First_Response.Engine.Scripting.Entities;
using RAGENativeUI;
using System;
using System.Collections.Generic;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
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
        /// Parses the XML in the flow sequence xml file
        /// </summary>
        /// <param name="sequenceId">The name of this flow sequence</param>
        /// <param name="outcome">The <see cref="FlowOutcome"/> selected for this sequence</param>
        /// <param name="ped">The ped that will hold the answers in this sequence</param>
        /// <param name="parser">The <see cref="ExpressionParser"/> use for condition statements in the XML</param>
        /// <returns></returns>
        public FlowSequence Parse(string sequenceId, FlowOutcome outcome, GamePed ped, ExpressionParser parser)
        {
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
                    Log.Warning($"FlowSequence.LoadXml(): Menu node has no 'id' attribute");
                    continue;
                }

                // Create
                string menuId = n.Attributes["id"].Value;
                var menu = new UIMenu("Ped Interaction", $"~y~{ped.Persona.FullName}");

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
                    if (sequence.OfficerDialogs.ContainsKey(buttonId))
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
                        sequence.OfficerDialogs.Add(buttonId, lines.ToArray());
                    }
                    else
                    {
                        // Log
                        Log.Warning("FlowSequence.LoadXml(): Menu -> MenuItem has no Line nodes");

                        // Save default line
                        sequence.OfficerDialogs.Add(buttonId, new Subtitle[] { new Subtitle() { Text = text, Duration = 3000 } });
                    }

                    // Create button
                    var item = new MyUIMenuItem<string>(text);
                    item.Tag = buttonId;
                    item.Parent = menu; // !! Important !!

                    // Add button
                    sequence.AddMenuButton(buttonId, item, isVisible);
                }

                // Add menu
                sequence.AllMenus.Add(menu);
                sequence.FlowReturnMenus.Add(menuId, menu);

                if (i == 0)
                {
                    sequence.InitialMenuName = menuId;
                }

                i++;
            }

            // Call menu refresh
            sequence.AllMenus.RefreshIndex();

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
                if (sequence.FlowReturnMenus.ContainsKey(menuName))
                {
                    sequence.InitialMenuName = menuName;
                }
                else
                {
                    // Log as warning and continue
                    Log.Warning($"FlowSequence.LoadXml(): Specified initial menu for FlowOutcome '{outcome.Id}' does not exist!");
                }
            }
            else
            {
                // Log as warning and continue
                Log.Warning($"FlowSequence.LoadXml(): FlowOutcome '{outcome.Id}' does not have an 'initialMenu' attribute!");
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

            // Add items within the Inventory node
            if (invNode != null && invNode.HasChildNodes)
            {
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
                        Log.Warning($"FlowSequence.LoadXml(): Unable to parse FlowOutcome['{outcome.Id}']->Ped->Inventory->Contraband type attribute");
                        continue;
                    }

                    // Grab items
                    var subItems = n.SelectNodes("Item");
                    if (subItems.Count == 0)
                    {
                        // Log as warning and continue
                        Log.Warning($"FlowSequence.LoadXml(): FlowOutcome['{outcome.Id}']->Ped->Inventory->Contraband has no Item nodes");
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
                            Log.Warning($"FlowSequence.LoadXml(): Unable to parse FlowOutcome['{outcome.Id}']->Ped->Inventory->Contraband->Item probability attribute value");
                            continue;
                        }

                        // See if we have an IF statement
                        if (!String.IsNullOrEmpty(item.Attributes["if"]?.Value))
                        {
                            // Do we have a parser?
                            if (parser == null)
                            {
                                Log.Warning($"FlowSequence.LoadXml(): Statement from FlowOutcome['{outcome.Id}']->Ped->Inventory->Contraband->Item has 'if' statement but no ExpressionParser is defined... Skipping");
                                continue;
                            }

                            // parse expression
                            if (!parser.Evaluate<bool>(item.Attributes["if"].Value))
                            {
                                // If we failed to evaluate to true, skip
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
                        Log.Warning($"FlowSequence.LoadXml(): FlowOutcome['{outcome.Id}']->Ped->Inventory->Contraband has no Item nodes");
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
                            Log.Warning($"FlowSequence.LoadXml(): Unable to parse FlowOutcome['{outcome.Id}']->Ped->Inventory->Weapon->Item probability attribute value");
                            continue;
                        }

                        // See if we have an IF statement
                        if (!String.IsNullOrEmpty(item.Attributes["if"]?.Value))
                        {
                            // Do we have a parser?
                            if (parser == null)
                            {
                                Log.Warning($"FlowSequence.LoadXml(): Statement from FlowOutcome['{outcome.Id}']->Ped->Inventory->Weapon->Item has 'if' statement but no ExpressionParser is defined... Skipping");
                                continue;
                            }

                            // parse expression
                            if (!parser.Evaluate<bool>(item.Attributes["if"].Value))
                            {
                                // If we failed to evaluate to true, skip
                                continue;
                            }
                        }

                        // See if we have an IF statement
                        if (String.IsNullOrEmpty(item.Attributes["id"]?.Value))
                        {
                            Log.Warning($"FlowSequence.LoadXml(): Empty or missing FlowOutcome['{outcome.Id}']->Ped->Inventory->Weapon->Item id attribute value");
                            continue;
                        }

                        // Skip if we cant parse the value
                        if (!short.TryParse(item.Attributes["ammo"]?.Value, out short ammo))
                        {
                            // Log as warning and continue
                            Log.Warning($"FlowSequence.LoadXml(): Unable to parse FlowOutcome['{outcome.Id}']->Ped->Inventory->Weapon->Item ammo attribute value");
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
                ped.Demeanor = PedDemeanor.Calm;

                // Log as warning and continue
                Log.Warning($"FlowSequence.LoadXml(): FlowOutcome '{outcome.Id}' does not have a Ped->Presentation node");
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
                        Log.Warning($"FlowSequence.LoadXml(): Unable to parse FlowOutcome['{outcome.Id}'] Ped->Presentation->Demeanor node value");
                        continue;
                    }

                    // Skip if we cant parse the value
                    if (!Int32.TryParse(item.Attributes["probability"]?.Value, out int prob))
                    {
                        // Log as warning and continue
                        Log.Warning($"FlowSequence.LoadXml(): Unable to parse FlowOutcome['{outcome.Id}'] Ped->Presentation->Demeanor probability attribute value");
                        continue;
                    }

                    // See if we have an IF statement
                    if (!String.IsNullOrEmpty(item.Attributes["if"]?.Value))
                    {
                        // Do we have a parser?
                        if (parser == null)
                        {
                            Log.Warning($"FlowSequence.LoadXml(): Statement from FlowOutcome['{outcome.Id}']->Ped->Presentation->Demeanor has 'if' statement but no ExpressionParser is defined... Skipping");
                            continue;
                        }

                        // parse expression
                        if (!parser.Evaluate<bool>(item.Attributes["if"].Value))
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
                    ped.Demeanor = gen.Spawn().Value;
                    Log.Debug($"Setting Ped demeanor for {ped} to {ped.Demeanor}");
                }
            }

            // Load flow outcome
            sequence.PedResponses = new ResponseSet(outcome.Id, catagoryNode.SelectSingleNode("Responses"), parser);

            // return
            return sequence;
        }
    }
}
