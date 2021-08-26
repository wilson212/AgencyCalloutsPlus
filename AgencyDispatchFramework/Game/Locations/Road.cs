using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;

namespace AgencyDispatchFramework.Game.Locations
{
    public class Road : WorldLocation
    {
        public override LocationTypeCode LocationType => LocationTypeCode.Road;

        public Road(Vector3 coordinates) : base(coordinates)
        {

        }
    }
}
