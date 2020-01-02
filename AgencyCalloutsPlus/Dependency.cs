using System;
using System.IO;

namespace AgencyCalloutsPlus
{
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
