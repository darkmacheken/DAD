using System.Text.RegularExpressions;

namespace TupleSpace {

    public static class StringUtils {

        public static bool MatchString(string s,string search) {
            string parsedSearch = "^" + search.Replace("*", ".*") + "$";
            Regex regex = new Regex(@parsedSearch);
            Match match = regex.Match(s);

            return match.Success;
        }
    }
}