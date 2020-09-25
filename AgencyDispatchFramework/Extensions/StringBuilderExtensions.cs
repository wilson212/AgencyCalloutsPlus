using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Extensions
{
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Clears the current contents of this StringBuilder
        /// </summary>
        /// <param name="builder"></param>
        public static void Clear(this StringBuilder builder)
        {
            builder.Length = 0;
        }

        /// <summary>
        /// Appends an Object to this string builder if the <paramref name="condition"/> is true.
        /// </summary>
        /// <param name="condition">Indicates whether we append this object to the end of this StringBuilder</param>
        /// <param name="value">The value to append</param>
        public static StringBuilder AppendIf(this StringBuilder builder, bool condition, object value)
        {
            if (condition)
                builder.Append(value);
            return builder;
        }
    }
}
