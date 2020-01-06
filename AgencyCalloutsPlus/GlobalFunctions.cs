using LSPD_First_Response.Mod.API;
using System;
using System.Reflection;

namespace AgencyCalloutsPlus
{
    internal static class GlobalFunctions
    {
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
