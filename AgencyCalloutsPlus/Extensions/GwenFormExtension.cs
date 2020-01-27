using Rage;
using Rage.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Extensions
{
    internal static class GwenFormExtension
    {
        internal static Point GetLaunchPosition(this GwenForm form)
        {
            return new Point(Game.Resolution.Width / 2 - form.Window.Width / 2, Game.Resolution.Height / 2 - form.Window.Height / 2);
        }
    }
}
