using System.Text.RegularExpressions;

namespace TupleSpace {

    public static class Utils {

        public static bool CompareArrays(string[] a, string[] b) {
            if (a.Length != b.Length) { 
                return false; 
            }

            for (int i = 0; i < a.Length; i++) {
                if (!(a[i].Equals(b[i]))) {
                    return false; 
                }
            }
            return true;
        }

        public static bool MatchString(string s,string search) {
            string parsed_search = "^" + search.Replace("*", ".*") + "$";
            Regex regex = new Regex(@parsed_search);
            Match match = regex.Match(s);

            return match.Success;
        }
    }
}