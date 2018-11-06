using System.Collections.Generic;

namespace TupleSpace {

    public class TupleSpace {
        List<Tuple> tuples = new List<Tuple>();

        public void Add(string tuplestring) {
            Tuple tuple = new Tuple(tuplestring);
            tuples.Add(tuple);
        }

        public Tuple Read(string tuplestring) {
            Tuple tuple = new Tuple(tuplestring);
            return null;
        }

        public Tuple Take(string tuplestring) {
            Tuple tuple = new Tuple(tuplestring);
            return null;
        }

        private List<Tuple> SearchTuple(Tuple search_tuple) {
            //lock tuple list in order to don't add or take any tuple that might compromise the result.
            //lock(tuples)
            List<Tuple> result = new List<Tuple>();
            foreach(Tuple tuple in tuples) {
                if (tuple.Match(search_tuple)) {
                    result.Add(tuple);
                }
            }
            //unlock(tuples)
            return result;
        }
    }
}


