using AgencyCalloutsPlus.API;
using LSPD_First_Response.Engine.Scripting;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace AgencyCalloutsPlus
{
    internal static class GlobalFunctions
    {
        /// <summary>
        /// Loads an xml file and returns the XML document back as an object
        /// </summary>
        /// <param name="calloutType"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        internal static XmlDocument LoadScenarioFile(Type calloutType, params string[] paths)
        {
            // Convert namespace to string
            string name = calloutType.Namespace;
            string[] parts = name.Split('.');
            int length = parts.Length;

            // If this is NOT a callout!
            if (parts.Length != 3 || !String.Equals(parts[1], "Callouts"))
            {
                Game.LogTrivial("[WARN] AgencyCalloutsPlus: Invalid Callout class. Cannot load Scenario file!");
                throw new ArgumentException("Invalid Callout class. Cannot load Scenario file!", "calloutType");
            }

            // Get type and name of the callout
            string calloutTypeString = parts[length - 1];
            string calloutName = calloutType.BaseType.Name;

            // Create file path
            string path = Path.Combine(Main.PluginFolderPath, "Callouts", calloutTypeString, calloutName);
            foreach (string p in paths)
                path = Path.Combine(path, p);

            // Load XML document
            XmlDocument document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            return document;
        }

        internal static bool IsLSPDFRPluginRunning(string Plugin, Version minversion = null)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName an = assembly.GetName();
                if (an.Name.ToLower() == Plugin.ToLower())
                {
                    if (minversion == null || an.Version.CompareTo(minversion) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool IsLSPDFRPluginRunning(string Plugin, out Version version)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName an = assembly.GetName();
                if (an.Name.ToLower() == Plugin.ToLower())
                {
                    version = an.Version;
                    return true;
                }
            }

            version = default(Version);
            return false;
        }

        internal static bool IsLSPDFRPluginRunning(string Plugin, Version minversion, out Version version)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName an = assembly.GetName();
                if (an.Name.ToLower() == Plugin.ToLower())
                {
                    if (minversion == null || an.Version.CompareTo(minversion) >= 0)
                    {
                        version = an.Version;
                        return true;
                    }
                }
            }

            version = default(Version);
            return false;
        }
    }
}
