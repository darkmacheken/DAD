using System;
using System.Collections.Generic;
using System.Threading;
using TupleSpace.Exceptions;

namespace TupleSpace {

    public class TupleSpace
    {
        public List<Tuple> Tuples { get; set; }
        public Dictionary<Tuple<string,int>, List<Tuple>> LockedTuples {get; set; }

        public TupleSpace() {
            Tuples = new List<Tuple>();
            LockedTuples = new Dictionary<Tuple<string, int>, List<Tuple>>();
        }

        public TupleSpace(List<Tuple> tuples) {
            Tuples = new List<Tuple>(tuples);
            LockedTuples = new Dictionary<Tuple<string, int>, List<Tuple>>();
        }

        public void Add(string tuplestring) {
            Tuple tuple = new Tuple(tuplestring);
            this.Tuples.Add(tuple);
            Console.WriteLine("Added Tuple: " + tuple);
        }

        public Tuple Read(string tuplestring) {
            Tuple tuple = new Tuple(tuplestring);
            Console.WriteLine("\nRead Tuple: " + tuple);
            List<Tuple> matches = SearchTuples(tuple);
            Console.WriteLine("Matches: -------------------------- ");
            foreach (Tuple match in matches) {
                Console.WriteLine(match);
            }
            Console.WriteLine("-----------------------------------");
            return matches[0];
        }

        public Tuple Take(string tuplestring) {
            Tuple tuple = new Tuple(tuplestring);
            Console.WriteLine("\nTake Tuple: " + tuple);
            List<Tuple> matches = SearchTuples(tuple);
            Console.WriteLine("Matches: -------------------------- ");
            foreach (Tuple match in matches) {
                Console.WriteLine(match);
            }
            Console.WriteLine("-----------------------------------");
            Tuple removed = matches[0];
            Tuples.Remove(removed);
            return removed;
        }

        public List<Tuple> GetAndLock(string clientid, int requestnumber, string tuplestring) {
            Tuple search_tuple = new Tuple(tuplestring);
            List<Tuple> locked_tuples = new List<Tuple>();
            bool refuse = false;

            /* only one thread can aquire locks at each time */
            lock (this.Tuples) {
                try {
                    locked_tuples = SearchAndLockTuples(search_tuple);
                } catch(Exception e) {
                    refuse |= e is UnableToLockException;
                }
            }

            /* unlock previous locked tuples if refused locks */
            if(refuse) {
                foreach(Tuple tuple in locked_tuples) {
                    Monitor.Exit(tuple);
                }
                return null;
            }
            else {
                this.LockedTuples.Add(new Tuple<string, int>(clientid, requestnumber),locked_tuples);
            }
            return locked_tuples;
        }

        public void Unlock(string clientid, int requestnumber) {
            List<Tuple> locked_tuples = GetTuplesByClientRequest(clientid, requestnumber);
            /* unlock all matches */
            foreach (Tuple tuple in locked_tuples) {
                Monitor.Exit(tuple);
            }
            this.LockedTuples.Remove(new Tuple<string, int>(clientid, requestnumber));
        }

        public void UnlockAndTake(string clientid, int requestnumber, Tuple tupletoremove) {
            List<Tuple> locked_tuples = GetTuplesByClientRequest(clientid, requestnumber);

            /* unlock and take are "atomic" */
            lock (this.Tuples) {
                Unlock(clientid, requestnumber);
                this.Tuples.Remove(tupletoremove);
            }
        }

        private List<Tuple> GetTuplesByClientRequest(string clientid, int requestnumber) {
            return this.LockedTuples[new Tuple<string, int>(clientid, requestnumber)];
        }

        private List<Tuple> SearchAndLockTuples(Tuple search_tuple) {
            List<Tuple> result = new List<Tuple>();
            foreach (Tuple tuple in this.Tuples) {
                if (tuple.Match(search_tuple)) {
                    if (!Monitor.TryEnter(tuple)) {
                        throw new UnableToLockException();
                    }
                    result.Add(tuple);
                }
            }
            return result;
        }

        private List<Tuple> SearchTuples(Tuple search_tuple)
        {
            List<Tuple> result = new List<Tuple>();
            foreach (Tuple tuple in this.Tuples) {
                if (tuple.Match(search_tuple)) {
                    result.Add(tuple);
                }
            }
            return result;
        }
    }
}