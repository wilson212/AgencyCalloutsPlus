using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AgencyDispatchFramework.Extensions
{
    public static class RegexExtensions
    {
        public static string ReplaceTokens(this Regex re, string input, IDictionary<string, object> args)
        {
            return re.Replace(input, match => 
            {
                string varName = match.Groups[1].Value;
                return (args.ContainsKey(varName)) ? args[varName].ToString() : match.Value;
            });
        }
    }
}
