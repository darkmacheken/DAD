using System.Collections.Generic;
using System;
using System.Threading;

namespace TupleSpace {

    public class TupleSpace 
    {
        public List<Tuple> Tuples { get; set; }

        public TupleSpace() {
            Tuples = new List<Tuple>();
        }

        public TupleSpace(List<Tuple> tuples) {
            Tuples = new List<Tuple>(tuples);
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

        public List<Tuple> GetAndLock(int clientid, int requestnumber, string tuplestring) {
            Tuple search_tuple = new Tuple(tuplestring);
            List<Tuple> matches = new List<Tuple>();
            List<Tuple> locked_tuples = new List<Tuple>();
            bool refuse = false;
            /* only one thread can aquire locks at each time */
            lock (this.Tuples) {
                matches = SearchTuples(search_tuple);
                /* lock all matches */
                foreach (Tuple tuple in matches) {
                    /* if lock cannot be acquired */
                    if(!Monitor.TryEnter(tuple)) {
                        refuse = true;
                        break;
                    }
                    locked_tuples.Add(tuple);
                }
                /* unlock previous locked tuples */
                if(refuse) {
                    foreach(Tuple tuple in locked_tuples) {
                        Monitor.Exit(tuple);
                    }
                    return null;
                }
                else {
                    //add to tuple set with clientid and requestnumber
                }
            }
            return matches;
        }

        public void Unlock(int clientid, int requestnumber) {
            List<Tuple> locked_tuples = GetTuplesByClientRequest(clientid, requestnumber);
            /* lock all matches */
            foreach (Tuple tuple in locked_tuples) {
                Monitor.Exit(tuple);
            }
        }

        public void UnlockAndTake(int clientid, int requestnumber, Tuple tupletoremove) {
            List<Tuple> locked_tuples = GetTuplesByClientRequest(clientid, requestnumber);

            /* unlock and take are atomic */
            lock (this.Tuples) {
                Unlock(clientid, requestnumber);
                this.Tuples.Remove(tupletoremove);
            }
        }

        private List<Tuple> GetTuplesByClientRequest(int clientid, int requestnumber) {
            //todo implementation of list? of lists? tuples
            return null;
        }

        List<Tuple> SearchTuples(Tuple search_tuple) {
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