using System.Drawing;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Provides an enumeration of <see cref="Rage.Blip"/> colors
    /// for <see cref="OfficerUnit"/> entities based on status
    /// </summary>
    internal static class OfficerStatusColor
    {
        public static Color Available = Color.White;

        public static Color DispatchedCode2 = Color.DodgerBlue;

        public static Color DispatchedCode3 = Color.Orange;

        public static Color OnScene = Color.LimeGreen;

        public static Color OnBreak = Color.Aqua;

        public static Color OffDuty = Color.Silver;

        public static Color Panic = Color.Red;

        public static Color Dead = Color.Black;
    }
}
