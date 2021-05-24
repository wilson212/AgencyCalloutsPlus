using AgencyDispatchFramework.Dispatching;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Dispatching.Assignments
{
    public sealed class StandardTrafficStop : BaseAssignment
    {
        public StandardTrafficStop()
        {
            Description = "Traffic Stop";
            Priority = CallPriority.Expedited;
            Location = Rage.Game.LocalPlayer.Character.Position;
        }
    }
}
