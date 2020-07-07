using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Extensions
{
    public static class ColorExtensions
    {
        public static Color WithA(this Color color, byte newA) => Color.FromArgb(newA, color);
    }
}
