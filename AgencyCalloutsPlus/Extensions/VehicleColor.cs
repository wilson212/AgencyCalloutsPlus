using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Extensions
{
    /// <summary>
    /// Credits to Albo1125: https://github.com/Albo1125/Albo1125-Common/blob/master/Albo1125.Common/CommonLibrary/ExtensionMethods.cs#L298
    /// </summary>
    public struct VehicleColor
    {
        /// <summary>
        /// The primary color paint index 
        /// </summary>
        public EPaint PrimaryColor { get; set; }

        /// <summary>
        /// The secondary color paint index 
        /// </summary>
        public EPaint SecondaryColor { get; set; }

        /// <summary>
        /// Gets the primary color name
        /// </summary>
        public string PrimaryColorName
        {
            get { return GetColorName(PrimaryColor); }
        }
        /// <summary>
        /// Gets the secondary color name
        /// </summary>
        public string SecondaryColorName
        {
            get { return GetColorName(SecondaryColor); }
        }

        /// <summary>
        /// Gets the color name
        /// </summary>
        /// <param name="paint">Color to get the name from</param>
        /// <returns></returns>
        public string GetColorName(EPaint paint)
        {
            String name = Enum.GetName(typeof(EPaint), paint);
            return name.Replace("_", " ");
        }
    }
}
