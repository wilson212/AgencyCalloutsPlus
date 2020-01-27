using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.API
{
    public class PriorityCallDescription
    {
        /// <summary>
        /// Gets the description text for the <see cref="PriorityCall"/>
        /// </summary>
        public string Text { get; protected set; }

        /// <summary>
        /// Gets the description abbreviation text for the <see cref="PriorityCall"/>
        /// </summary>
        public string Source { get; protected set; }

        public PriorityCallDescription(string description, string source)
        {
            Text = description ?? throw new ArgumentNullException(nameof(description));
            Source = source ?? String.Empty;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
