using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game.Locations;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    public class CalloutMetaFile : XmlFileBase
    {
        /// <summary>
        /// Gets whether this file has been loaded
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// Contains a list Scenarios by name
        /// </summary>
        public List<CalloutScenarioInfo> Scenarios { get; set; }

        /// <summary>
        /// Gets the class type that controls the scenarios
        /// </summary>
        public Type CalloutType { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="CalloutMetaFile"/> using the specified
        /// file
        /// </summary>
        /// <param name="filePath"></param>
        public CalloutMetaFile(string filePath) : base(filePath)
        {
            // === Document loaded successfully by base class if we are here === //
            // 
            Scenarios = new List<CalloutScenarioInfo>();
        }

        /// <summary>
        /// Parses the XML in the callout meta
        /// </summary>
        /// <param name="assembly">The assembly containing the callout class type for the contained scenarios</param>
        public void Parse(Assembly assembly, bool yieldFiber)
        {
            // Setup some vars
            var calloutDirName = Path.GetDirectoryName(FilePath);
            var agencies = new List<AgencyType>(6);
            ProbabilityGenerator<PriorityCallDescription> descriptions = null;

            // Ensure proper format at the top
            var rootElement = Document.SelectSingleNode("CalloutMeta");
            if (rootElement == null)
            {
                throw new Exception($"CalloutMetaFile.Parse(): Unable to load CalloutMeta data in directory '{calloutDirName}' for Assembly: '{assembly.FullName}'");
            }

            // Get callout type name
            var typeName = rootElement.SelectSingleNode("Controller")?.InnerText;
            if (String.IsNullOrWhiteSpace(typeName))
            {
                throw new Exception($"CalloutMetaFile.Parse(): Unable to extract Controller value in CalloutMeta.xml in directory '{calloutDirName}' for Assembly: '{assembly.FullName}'");
            }

            // Get callout type name
            CalloutType = assembly.GetType(typeName);
            if (CalloutType == null)
            {
                throw new Exception($"CalloutMetaFile.Parse(): Unable to find Callout class '{typeName}' in Assembly '{assembly.FullName}'");
            }

            // Get official callout name
            string calloutName = CalloutType.GetAttributeValue((CalloutInfoAttribute attr) => attr.Name);
            if (String.IsNullOrWhiteSpace(calloutName))
            {
                throw new Exception($"CalloutMetaFile.Parse(): Callout class '{typeName}' in Assembly '{assembly.FullName}' is missing the CalloutInfoAttribute!");
            }

            // Yield fiber?
            if (yieldFiber) GameFiber.Yield();

            // Process the XML scenarios
            foreach (XmlNode scenarioNode in rootElement.SelectSingleNode("Scenarios")?.ChildNodes)
            {
                // Skip all but elements
                if (scenarioNode.NodeType == XmlNodeType.Comment) continue;

                // Grab the Callout Catagory
                XmlNode catagoryNode = scenarioNode.SelectSingleNode("Category");
                if (!Enum.TryParse(catagoryNode?.InnerText, out CallCategory crimeType))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract Callout Category value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                    );
                    continue;
                }

                // Extract simulation time
                int min = 0, max = 0;
                XmlNode childNode = scenarioNode.SelectSingleNode("Simulation")?.SelectSingleNode("CallTime");
                if (childNode == null)
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract Simulation->CallTime element for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                    );
                    continue;
                }
                else if (childNode.Attributes == null || !Int32.TryParse(childNode.Attributes["min"]?.Value, out min) || !Int32.TryParse(childNode.Attributes["max"]?.Value, out max))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract CallTime[min] or CallTime[max] attribute for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                    );
                    continue;
                }

                // ============================================== //
                // Grab probabilities
                // ============================================== //
                XmlNode probNode = scenarioNode.SelectSingleNode("Probabilities");
                if (probNode == null || !probNode.HasChildNodes)
                {
                    Log.Error($"CalloutMetaFile.Parse(): Unable to load probabilities in CalloutMeta for '{calloutDirName}'");
                    continue;
                }

                // Create world state probabilities
                WorldStateMultipliers multipliers = null;
                try
                {
                    multipliers = XmlExtractor.GetWorldStateMultipliers(probNode);
                }
                catch (Exception e)
                {
                    Log.Warning("CalloutMetaFile.Parse(): " + e.Message);
                    continue;
                }

                // Grab the Location info
                childNode = scenarioNode.SelectSingleNode("Location");
                if (childNode == null)
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract Location node for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                    );
                    continue;
                }

                // Parse location type
                string type = childNode.SelectSingleNode("Type")?.InnerText ?? "";
                if (!Enum.TryParse(type, out LocationTypeCode locationType))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract Location Type value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                    );
                    continue;
                }

                // Extract location flags
                var filter = new FlagFilterGroup();
                childNode = childNode.SelectSingleNode("RequiredFlags");
                if (childNode != null && childNode.HasChildNodes)
                {
                    // Fetch filter type
                    if (!Enum.TryParse(childNode.GetAttribute("mode"), out SelectionOperator filterType))
                    {
                        filterType = SelectionOperator.All;
                    }
                    filter.Mode = filterType;

                    // Get child requirements
                    var nodes = childNode.SelectNodes("Requirement"); 
                    foreach (XmlNode n in nodes)
                    {
                        // Fetch filter type
                        if (!Enum.TryParse(n.GetAttribute("mode"), out SelectionOperator fType))
                        {
                            fType = SelectionOperator.All;
                        }

                        // Fetch inverse mode?
                        if (!bool.TryParse(n.GetAttribute("inverse"), out bool inverse))
                        {
                            inverse = false;
                        }

                        // Parse flags
                        int[] flags = GetFlagCodesFromLocationType(locationType, n, out Type t);
                        filter.Requirements.Add(new Requirement(t)
                        {
                            Flags = flags,
                            Inverse = inverse,
                            Mode = fType
                        });
                    }
                }

                // Yield fiber?
                if (yieldFiber) GameFiber.Yield();

                // Get the Dispatch Node
                XmlNode dispatchNode = scenarioNode.SelectSingleNode("Dispatch");
                if (dispatchNode == null)
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract scenario Dispatch node for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                    );
                    continue;
                }

                // Grab agency list
                XmlNode agenciesNode = dispatchNode.SelectSingleNode("Agencies");
                if (agenciesNode == null)
                {
                    Log.Error($"CalloutMetaFile.Parse(): Unable to load agency data in CalloutMeta for '{calloutDirName}'");
                    continue;
                }

                // No data?
                if (!agenciesNode.HasChildNodes)
                {
                    Log.Error($"CalloutMetaFile.Parse(): Unable to load any agencies data in CalloutMeta for '{calloutDirName}'");
                    continue;
                }

                // Itterate through items
                agencies.Clear();
                foreach (XmlNode n in agenciesNode.SelectNodes("Agency"))
                {
                    // Try and extract type value
                    if (!Enum.TryParse(n.InnerText, out AgencyType agencyType))
                    {
                        Log.Warning(
                            $"CalloutMetaFile.Parse(): Unable to parse AgencyType value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                        );
                        continue;
                    }

                    agencies.Add(agencyType);
                }

                // Try and extract probability value
                childNode = dispatchNode.SelectSingleNode("Target");
                if (!Enum.TryParse(childNode.InnerText, out CallTarget callTarget))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract scenario dispatch target value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                    );
                    continue;
                }

                // Try and extract probability value
                childNode = dispatchNode.SelectSingleNode("Priority");
                if (!Enum.TryParse(childNode.InnerText, out CallPriority priority))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract scenario priority value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                    );
                    continue;
                }

                // Try and extract Code value
                childNode = dispatchNode.SelectSingleNode("Response");
                if (!Enum.TryParse(childNode.InnerText, out ResponseCode code))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract scenario response code value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                    );
                    continue;
                }

                // Try and extract Code value
                childNode = dispatchNode.SelectSingleNode("UnitCount");
                if (!Int32.TryParse(childNode.InnerText, out int unitCount))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract scenario unit count value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                    );
                    continue;
                }

                // Grab the Scanner data
                string scanner = String.Empty;
                var scannerNode = dispatchNode.SelectSingleNode("Scanner");
                childNode = scannerNode.SelectSingleNode("AudioString");
                if (String.IsNullOrWhiteSpace(childNode.InnerText))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract ScannerAudioString value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}' -> Scanner"
                    );
                    continue;
                }
                else
                {
                    scanner = childNode.InnerText;
                }

                // Try to extract scanner prefix and suffix information
                childNode = scannerNode.SelectSingleNode("PrefixCallSign");
                bool.TryParse(childNode?.InnerText, out bool prefix);

                childNode = scannerNode.SelectSingleNode("UsePosition");
                bool.TryParse(childNode?.InnerText, out bool suffix);

                // Try and extract descriptions
                XmlNode cadNode = dispatchNode.SelectSingleNode("CAD");
                if (cadNode == null || !cadNode.HasChildNodes)
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract scenario CAD values for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                    );
                    continue;
                }

                // Yield fiber?
                if (yieldFiber) GameFiber.Yield();

                // Try and extract descriptions
                childNode = cadNode.SelectSingleNode("Descriptions");
                if (childNode == null || !childNode.HasChildNodes)
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract scenario descriptions values for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                    );
                    continue;
                }
                else
                {
                    // Clear old descriptions
                    descriptions = new ProbabilityGenerator<PriorityCallDescription>();
                    foreach (XmlNode descNode in childNode.SelectNodes("Description"))
                    {
                        // Ensure we have attributes
                        if (descNode.Attributes == null || !int.TryParse(descNode.Attributes["probability"]?.Value, out int prob))
                        {
                            Log.Warning(
                                $"CalloutMetaFile.Parse(): Unable to extract probability value for Description in '{calloutDirName}->Scenarios->{scenarioNode.Name}'->Dispatch->Descriptions"
                            );
                            continue;
                        }

                        // Extract call source value
                        var srcNode = descNode.SelectSingleNode("Source");
                        if (srcNode == null)
                        {
                            Log.Warning(
                                $"CalloutMetaFile.Parse(): Unable to extract Source value for Description in '{calloutDirName}->Scenarios->{scenarioNode.Name}'->Dispatch->Descriptions"
                            );
                            continue;
                        }

                        // Extract desc text value
                        var descTextNode = descNode.SelectSingleNode("Text");
                        if (descTextNode == null)
                        {
                            Log.Warning(
                                $"CalloutMetaFile.Parse(): Unable to extract Text value for Description in '{calloutDirName}->Scenarios->{scenarioNode.Name}'->Dispatch->Descriptions"
                            );
                            continue;
                        }

                        descriptions.Add(new PriorityCallDescription(prob, descTextNode.InnerText.Trim(), srcNode.InnerText));
                    }

                    // If we have no descriptions, we failed
                    if (descriptions.ItemCount == 0)
                    {
                        Log.Warning(
                            $"CalloutMetaFile.Parse(): Scenario has no Descriptions '{calloutDirName}->Scenarios->{scenarioNode.Name}'->Dispatch"
                        );
                        continue;
                    }
                }

                // Grab the CAD Texture
                childNode = cadNode.SelectSingleNode("Texture");
                if (String.IsNullOrWhiteSpace(childNode.InnerText))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract CADTexture value for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                    );
                    continue;
                }
                else if (childNode.Attributes == null || String.IsNullOrWhiteSpace(childNode.Attributes["dictionary"]?.Value))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract CADTexture[dictionary] attribute for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                    );
                    continue;
                }

                string textName = childNode.InnerText;
                string textDict = childNode.Attributes["dictionary"].Value;

                // Grab the Incident
                childNode = cadNode.SelectSingleNode("IncidentType");
                if (String.IsNullOrWhiteSpace(childNode.InnerText))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract IncidentType value for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                    );
                    continue;
                }
                else if (childNode.Attributes == null || childNode.Attributes["abbreviation"]?.Value == null)
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract Incident abbreviation attribute for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                    );
                    continue;
                }

                // Create scenario
                var scene = new CalloutScenarioInfo()
                {
                    Name = scenarioNode.Name,
                    CalloutName = calloutName,
                    Category = crimeType,
                    Targets = callTarget,
                    ProbabilityMultipliers = multipliers,
                    Priority = priority,
                    ResponseCode = code,
                    UnitCount = unitCount,
                    LocationTypeCode = locationType,
                    LocationFilters = filter,
                    ScannerAudioString = scanner,
                    ScannerPrefixCallSign = prefix,
                    ScannerUsePosition = suffix,
                    Descriptions = descriptions,
                    IncidentText = childNode.InnerText,
                    IncidentAbbreviation = childNode.Attributes["abbreviation"].Value,
                    CADSpriteName = textName,
                    CADSpriteTextureDict = textDict,
                    SimulationTime = new Range<int>(min, max),
                    AgencyTypes = agencies.ToArray()
                };

                // Add scenario to the pools
                Scenarios.Add(scene);

                // Yield fiber?
                if (yieldFiber) GameFiber.Yield();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="innerText"></param>
        /// <returns></returns>
        private int[] GetFlagCodesFromLocationType(LocationTypeCode locationType, XmlNode node, out Type type)
        {
            type = default(Type);
            if (String.IsNullOrEmpty(node.InnerText))
            {
                return new int[0];
            }

            string[] vals = node.InnerText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            int[] items;

            switch (locationType)
            {
                case LocationTypeCode.Residence:
                    type = typeof(ResidenceFlags);
                    items = ParseFlags<ResidenceFlags>(vals, node);
                    break;
                case LocationTypeCode.RoadShoulder:
                    type = typeof(RoadFlags);
                    items = ParseFlags<RoadFlags>(vals, node);
                    break;
                case LocationTypeCode.Intersection:
                    type = typeof(IntersectionFlags);
                    items = ParseFlags<IntersectionFlags>(vals, node);
                    break;
                default:
                    throw new Exception($"Cannot parse flags from {type}");
            }

            return items;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="vals"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private int[] ParseFlags<T>(string[] vals, XmlNode node) where T : struct
        {
            var items = new List<int>(vals.Length);
            foreach (string input in vals)
            {
                if (Enum.TryParse(input.Trim(), out T flag))
                {
                    int value = Convert.ToInt32(flag);
                    items.Add(value);
                }
                else
                {
                    Log.Debug($"Unable to parse enum value of '{input}' for type '{typeof(T).Name}' in CalloutMeta.xml ->  {node.GetFullPath()}");
                }
            }

            return items.ToArray();
        }
    }
}
