using System.Text.RegularExpressions;

namespace TupleSpace {

    public static class Utils {

        public static bool MatchString(string s,string search) {
            string parsed_search = "^" + search.Replace("*", ".*") + "$";
            Regex regex = new Regex(@parsed_search);
            Match match = regex.Match(s);

            return match.Success;
        }
    }
}