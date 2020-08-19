using System;

namespace AgencyCalloutsPlus.API
{
    public class PriorityCallDescription : ISpawnable
    {
        /// <summary>
        /// Gets the description text for the <see cref="PriorityCall"/>
        /// </summary>
        public string Text { get; protected set; }

        /// <summary>
        /// Gets the description abbreviation text for the <see cref="PriorityCall"/>
        /// </summary>
        public string Source { get; protected set; }

        public int Probability { get; }

        public PriorityCallDescription(int probabilty, string description, string source)
        {
            Probability = probabilty;
            Text = description ?? throw new ArgumentNullException(nameof(description));
            Source = source ?? String.Empty;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
