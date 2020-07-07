using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AgencyCalloutsPlus.Extensions
{
    public static class RegexExtensions
    {
        public static string ReplaceTokens(this Regex re, string input, IDictionary<string, object> args)
        {
            return re.Replace(input, match => args[match.Groups[1].Value].ToString());
        }
    }
}
