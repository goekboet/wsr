using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace App.WSr
{
    public static class ArgumentFunctions
    {
        public static Dictionary<string, string> ToOptionValuePairs(string[] args)
        {
            var pairs = new Dictionary<string, string>();

            for (int i = 0; i < args.Length - 1; i = i + 2)
            {
                pairs.Add(args[i], args[i + 1]);
            }

            return pairs;
        }

        public static int ParsePort(string[] args)
        {
            if (args.Length > 1 && int.TryParse(args[1], out int p)) return p; 
            return 80;
        }

        public static string ParseIP(string[] args)
        {
            if (args.Length > 0 && Regex.IsMatch(args[0], @"(\d{1,3}(\.|$)){4}")) return args[0];
            return "0.0.0.0";
        }
    }
}