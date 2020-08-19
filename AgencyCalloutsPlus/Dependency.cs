using System;
using System.IO;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// Represents a dependancy that this mod relies on
    /// </summary>
    internal class Dependancy
    {
        public string FilePath;
        public Version MinimumVersion;

        public Dependancy(string fileName, Version minimumVersion)
        {
            this.FilePath = fileName.Replace('/', Path.DirectorySeparatorChar);
            this.MinimumVersion = minimumVersion;
        }
    }
}
