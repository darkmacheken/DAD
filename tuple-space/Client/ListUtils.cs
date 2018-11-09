using System.Collections.Generic;
using System.Linq;

namespace Client {
    public static class ListUtils {
        public static List<string> IntersectLists(List<List<string>> lists) {
            List<string> intersection = lists
              .Skip(1)
              .Aggregate(
                  new HashSet<string>(lists.First()),
                  (h, e) => { h.IntersectWith(e); return h; }
              ).ToList();
            return intersection;
        }
    }
}
