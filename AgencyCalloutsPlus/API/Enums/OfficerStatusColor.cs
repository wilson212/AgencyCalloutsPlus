using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.API
{
    internal static class OfficerStatusColor
    {
        public static Color Available = Color.White;

        public static Color Dispatched = Color.Yellow;

        public static Color OnScene = Color.Green;

        public static Color OnBreak = Color.Aqua;

        public static Color Panic = Color.Red;

        public static Color Dead = Color.Black;
    }
}
