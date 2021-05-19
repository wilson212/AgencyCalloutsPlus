using AgencyDispatchFramework.Extensions;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// Contains information used for filtering out locations when choosing a callout location
    /// </summary>
    public class FlagFilterGroup
    {
        /// <summary>
        /// Contains an empty (default) condition
        /// </summary>
        public static FlagFilterGroup Default { get => new FlagFilterGroup(); }

        public List<Requirement> Requirements { get; set; }

        public SelectionOperator Mode { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="FlagFilterGroup"/>
        /// </summary>
        public FlagFilterGroup()
        {
            Requirements = new List<Requirement>();
        }

        public override string ToString()
        {
            return Mode.ToString() + "<< " + String.Join(", ", Requirements) + " >>";
        }
    }
}
