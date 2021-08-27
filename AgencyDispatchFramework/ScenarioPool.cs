using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Scripting;
using AgencyDispatchFramework.Xml;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// Provides an interface to load and store <see cref="CalloutScenarioInfo"/> instances
    /// </summary>
    public static class ScenarioPool
    {
        /// <summary>
        /// Contains a list Scenarios by name
        /// </summary>
        internal static Dictionary<string, CalloutScenarioInfo> ScenariosByName { get; set; }

        /// <summary>
        /// Contains a list Scenarios by name
        /// </summary>
        internal static Dictionary<string, List<CalloutScenarioInfo>> ScenariosByAssembly { get; set; }

        /// <summary>
        /// Contains a list Scenarios seperated by CalloutType that will be used
        /// to populate the calls board
        /// </summary>
        internal static Dictionary<CallCategory, WorldStateProbabilityGenerator<CalloutScenarioInfo>> ScenariosByCalloutType { get; set; }

        /// <summary>
        /// Contains a list of scenario's by callout name
        /// </summary>
        internal static Dictionary<string, WorldStateProbabilityGenerator<CalloutScenarioInfo>> ScenariosByCalloutName { get; set; }

        /// <summary>
        /// Event called when a callout scenario is added to the list
        /// </summary>
        public static event ScenarioListUpdateHandler OnScenarioAdded;

        /// <summary>
        /// Event called when a callout is added and registered with LSPDFR through this <see cref="ScenarioPool"/> class.
        /// </summary>
        public static event CalloutListUpdateHandler OnCalloutRegistered;

        /// <summary>
        /// Event called when a call to the method <see cref="RegisterCalloutsFromPath(string, Assembly)"/>
        /// has completed adding callouts and scenarios to the pool.
        /// </summary>
        public static event CalloutPackLoadedHandler OnCalloutPackLoaded;

        /// <summary>
        /// Static method called the first time this class is referenced anywhere
        /// </summary>
        static ScenarioPool()
        {
            // Initialize callout types
            ScenariosByName = new Dictionary<string, CalloutScenarioInfo>();
            ScenariosByAssembly = new Dictionary<string, List<CalloutScenarioInfo>>();
            ScenariosByCalloutName = new Dictionary<string, WorldStateProbabilityGenerator<CalloutScenarioInfo>>();
            ScenariosByCalloutType = new Dictionary<CallCategory, WorldStateProbabilityGenerator<CalloutScenarioInfo>>();
            foreach (CallCategory type in Enum.GetValues(typeof(CallCategory)))
            {
                ScenariosByCalloutType.Add(type, new WorldStateProbabilityGenerator<CalloutScenarioInfo>());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootPath">The full directory path to the callout pack root folder</param>
        /// <param name="assembly">The calling assembly that contains the callout scripts</param>
        /// <returns></returns>
        public static int RegisterCalloutsFromPath(string rootPath, Assembly assembly, bool yieldFiber = true)
        {
            // Load directory
            var directory = new DirectoryInfo(rootPath);
            if (!directory.Exists)
            {
                Log.Error($"ScenarioPool.RegisterCalloutsFromPath(): Callouts directory is missing: {rootPath}");
                return 0;
            }

            // Initialize vars
            int itemsAdded = 0;

            // Load callout scripts
            foreach (var calloutDirectory in directory.GetDirectories())
            {
                // Get callout name and type
                string calloutDirName = calloutDirectory.Name;

                // ensure CalloutMeta.xml exists
                string path = Path.Combine(calloutDirectory.FullName, "CalloutMeta.xml");
                if (!File.Exists(path))
                {
                    Log.Warning($"ScenarioPool.RegisterCalloutsFromPath(): Directory does not contain a CalloutMeta.xml: {path}");
                    continue;
                }

                // Wrap in a try/catch. Exceptions are thrown in here
                try
                {
                    // Add assembly
                    var name = assembly.GetName().Name;
                    if (!ScenariosByAssembly.ContainsKey(name))
                    {
                        ScenariosByAssembly.Add(name, new List<CalloutScenarioInfo>());
                    }

                    // Load meta file
                    using (var metaFile = new CalloutMetaFile(path))
                    {
                        // Parse file
                        metaFile.Parse(assembly, yieldFiber);

                        // Yield fiber?
                        if (yieldFiber) GameFiber.Yield();

                        // Add each scenario
                        foreach (var scenario in metaFile.Scenarios.OrderBy(x => x.Name))
                        {
                            // Create entry if not already
                            if (!ScenariosByCalloutName.ContainsKey(scenario.CalloutName))
                            {
                                ScenariosByCalloutName.Add(scenario.CalloutName, new WorldStateProbabilityGenerator<CalloutScenarioInfo>());
                            }

                            // Add scenario to the pools
                            ScenariosByName.Add(scenario.Name, scenario);
                            ScenariosByAssembly[name].Add(scenario);
                            ScenariosByCalloutName[scenario.CalloutName].Add(scenario, scenario.ProbabilityMultipliers);
                            ScenariosByCalloutType[scenario.Category].Add(scenario, scenario.ProbabilityMultipliers);

                            // Statistics trackins
                            itemsAdded++;

                            // Call event
                            OnScenarioAdded?.Invoke(scenario);

                            // Yield fiber?
                            if (yieldFiber) GameFiber.Yield();
                        }

                        // Register the callout
                        Functions.RegisterCallout(metaFile.CalloutType);

                        // Call event
                        OnCalloutRegistered?.Invoke(metaFile.CalloutType);
                    }

                }
                catch (FileNotFoundException)
                {
                    Log.Error($"ScenarioPool.RegisterCalloutsFromPath(): Missing CalloutMeta.xml in directory '{calloutDirName}' for Assembly: '{assembly.FullName}'");
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // Call events
            OnCalloutPackLoaded?.Invoke(rootPath, assembly, itemsAdded);

            // Log and return
            Log.Debug($"Added {itemsAdded} scenarios to the ScenarioPool from {assembly.FullName}");
            return itemsAdded;
        }

        /// <summary>
        /// Clears the current list of scenario's from the pool
        /// </summary>
        internal static void Reset()
        {
            ScenariosByName.Clear();
            ScenariosByCalloutName.Clear();
            foreach (CallCategory type in Enum.GetValues(typeof(CallCategory)))
            {
                ScenariosByCalloutType[type].Clear();
            }
        }
    }
}
