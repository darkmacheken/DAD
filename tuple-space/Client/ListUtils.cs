using System.Collections.Generic;
using System.Linq;

namespace Client {
    public static class ListUtils {
        public static List<string> IntersectLists(List<List<string>> lists) {
            List<List<string>> listsNotEmpty = lists.Where(list => list.Count != 0).ToList();

            if (listsNotEmpty.Count == 0) {
                return new List<string>();
            }

            List<string> intersection = listsNotEmpty
              .Skip(1)
              .Aggregate(
                  new HashSet<string>(lists.First()),
                  (h, e) => { h.IntersectWith(e); return h; }
              ).ToList();
            return intersection;
        }
    }
}
