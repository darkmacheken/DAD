using System.Collections.Generic;
using System;

namespace TupleSpace {

    public class TupleSpace
    {
        public List<Tuple> Tuples { get; set; }

        public TupleSpace()
        {
            Tuples = new List<Tuple>();
        }

        public TupleSpace(List<Tuple> tuples)
        {
            Tuples = new List<Tuple>(tuples);
        }

        public void Add(string tuplestring)
        {
            Tuple tuple = new Tuple(tuplestring);
            Tuples.Add(tuple);
            Console.WriteLine("Added Tuple: " + tuple);
        }

        public Tuple Read(string tuplestring)
        {
            Tuple tuple = new Tuple(tuplestring);
            Console.WriteLine("\nRead Tuple: " + tuple);
            List<Tuple> matches = SearchTuples(tuple);
            Console.WriteLine("Matches: -------------------------- ");
            foreach (Tuple match in matches)
            {
                Console.WriteLine(match);
            }
            Console.WriteLine("-----------------------------------");
            return matches[0];
        }

        public Tuple Take(string tuplestring)
        {
            Tuple tuple = new Tuple(tuplestring);
            Console.WriteLine("\nTake Tuple: " + tuple);
            List<Tuple> matches = SearchTuples(tuple);
            Console.WriteLine("Matches: -------------------------- ");
            foreach (Tuple match in matches)
            {
                Console.WriteLine(match);
            }
            Console.WriteLine("-----------------------------------");
            Tuple removed = matches[0];
            Tuples.Remove(removed);
            return removed;
        }

        List<Tuple> SearchTuples(Tuple search_tuple)
        {
            //lock tuple list in order to don't add or take any tuple that might compromise the result.
            //lock(tuples)
            List<Tuple> result = new List<Tuple>();
            foreach (Tuple tuple in Tuples)
            {
                if (tuple.Match(search_tuple))
                {
                    result.Add(tuple);
                }
            }
            //unlock(tuples)
            return result;
        }
    }
}


