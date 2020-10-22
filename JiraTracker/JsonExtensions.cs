using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace JiraTracker
{
    public static class JsonExtensions
    {
        public static IEnumerable<JToken> AsTokenList(this JToken token)
        {
            if (token is JArray array)
            {
                foreach (var child in array.Children())
                    yield return child;
            }
        }

        public static string ConvertDate(string input)
        {
            if (input == "<null>" || input == "-") return "-";
            return ParseDate(input).ToString(CultureInfo.InvariantCulture);
        }

        public static DateTime ParseDate(this string input)
        {
            if (input == "<null>" || input == "-") return DateTime.MinValue;

            // Safety first
            return DateTime.Parse(input, CultureInfo.InvariantCulture);
        }
    }
}
