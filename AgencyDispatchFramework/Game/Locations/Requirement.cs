using AgencyDispatchFramework.Extensions;
using System;
using System.Text;

namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// Contains information regarding location filtering when the system chooses a location for a callout
    /// </summary>
    public class Requirement
    {
        /// <summary>
        /// Containts the converted integer flags required
        /// </summary>
        public int[] Flags { get; set; }

        /// <summary>
        /// Indicates whether the result should be inversed (true becomes false and vise versa)
        /// </summary>
        public bool Inverse { get; set; }

        /// <summary>
        /// Indicates whether we need all the flags, or just any of them
        /// </summary>
        public SelectionOperator Mode { get; set; }

        /// <summary>
        /// Gets the enum class type that the <see cref="Flags"/> parse from
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="Requirement"/>
        /// </summary>
        /// <param name="type"></param>
        public Requirement(Type type)
        {
            // Must be an enum
            if (!type.IsEnum)
                throw new ArgumentException("Passed type must be an enum");

            Type = type;
        }

        /// <summary>
        /// Pretty printing for the Game.log
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(Mode.ToString() + "(");
            foreach (int flag in Flags)
            {
                sb.Append(Enum.GetName(Type, flag));
            }

            sb.Append(")");
            return sb.ToString();
        }
    }
}
