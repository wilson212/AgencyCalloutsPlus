using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.API
{
    public class GameLocation
    {
        /// <summary>
        /// Gets the <see cref="Vector3"/> position of this location
        /// </summary>
        public Vector3 Position { get; protected set; }

        public string Address { get; set; }

        public GameLocation(Vector3 position)
        {
            this.Position = position;
        }
    }
}
