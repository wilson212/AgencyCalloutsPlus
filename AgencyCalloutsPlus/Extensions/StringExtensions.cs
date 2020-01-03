using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<int> ToIntList(this string str)
        {
            if (String.IsNullOrEmpty(str))
                yield break;

            foreach (var s in str.Split(','))
            {
                if (int.TryParse(s, out int num))
                    yield return num;
            }
        }
    }
}
