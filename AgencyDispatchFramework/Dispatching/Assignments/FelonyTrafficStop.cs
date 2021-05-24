using AgencyDispatchFramework.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Dispatching.Assignments
{
    public class FelonyTrafficStop : BaseAssignment
    {
        public FelonyTrafficStop()
        {
            Description = "Felony Traffic Stop";
            Priority = CallPriority.Emergency;
            Location = Rage.Game.LocalPlayer.Character.Position;
        }
    }
}
