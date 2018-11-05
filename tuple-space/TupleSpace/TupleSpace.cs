using System.Collections.Generic;

namespace TupleSpace {

    public class TupleSpace {
        List<Tuple> tuples = new List<Tuple>();

        public void Add(string tuplestring) {
            Tuple tuple = Tuple.ParseTuple(tuplestring);
        }

        public Tuple Read(string tuplestring) {
            Tuple tuple = Tuple.ParseTuple(tuplestring);
            return tuple;
        }

        public Tuple Take(string tuplestring) {
            Tuple tuple = Tuple.ParseTuple(tuplestring);
            return tuple;
        }

    }


}


