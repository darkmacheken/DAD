using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TupleSpace.Exceptions;

namespace TupleSpace {

    public class TupleSpace {
        public List<Tuple> Tuples { get; set; }
        public ConcurrentDictionary<System.Tuple<string,int>, List<Tuple>> LockedTuples {get; set; }

        public TupleSpace() {
            this.Tuples = new List<Tuple>();
            this.LockedTuples = new ConcurrentDictionary<System.Tuple<string, int>, List<Tuple>>();
        }

        public TupleSpace(List<Tuple> tuples) {
            this.Tuples = new List<Tuple>(tuples);
            this.LockedTuples = new ConcurrentDictionary<System.Tuple<string, int>, List<Tuple>>();
        }

        public void Add(string tupleString) {
            Tuple tuple = new Tuple(tupleString);
            this.Tuples.Add(tuple);
            Console.WriteLine("Added Tuple: " + tuple);
        }

        public Tuple Read(string tupleString) {
            Tuple tuple = new Tuple(tupleString);
            Console.WriteLine("\nRead Tuple: " + tuple);
            List<Tuple> matches = this.SearchTuples(tuple);
            Console.WriteLine("Matches: -------------------------- ");
            foreach (Tuple match in matches) {
                Console.WriteLine(match);
            }
            Console.WriteLine("-----------------------------------");
            return matches[0];
        }

        public Tuple Take(string tupleString) {
            Tuple tuple = new Tuple(tupleString);
            Console.WriteLine("\nTake Tuple: " + tuple);
            List<Tuple> matches = this.SearchTuples(tuple);
            Console.WriteLine("Matches: -------------------------- ");
            foreach (Tuple match in matches) {
                Console.WriteLine(match);
            }
            Console.WriteLine("-----------------------------------");
            Tuple removed = matches[0];
            this.Tuples.Remove(removed);
            return removed;
        }

        public List<Tuple> GetAndLock(string clientId, int requestNumber, string tupleString) {
            Tuple searchTuple = new Tuple(tupleString);
            List<Tuple> lockedTuples = new List<Tuple>();
            bool refuse = false;

            /* only one thread can acquire locks at each time */
            lock (this.Tuples) {
                try {
                    lockedTuples = this.SearchAndLockTuples(searchTuple);
                } catch(Exception e) {
                    refuse |= e is UnableToLockException;
                }
            }

            /* unlock previous locked tuples if refused locks */
            if(refuse) {
                foreach(Tuple tuple in lockedTuples) {
                    tuple.Locked = false;
                }
            }

            else if (!this.LockedTuples.TryAdd(new System.Tuple<string, int>(clientId, requestNumber), lockedTuples)){
                throw new RequestAlreadyHasLocks();
            }
            return lockedTuples;
        }

        public void Unlock(string clientId, int requestNumber) {
            List<Tuple> lockedTuples = this.GetLockedTuplesByClientRequest(clientId, requestNumber);
            /* unlock all matches */    
            foreach (Tuple tuple in lockedTuples) {
                tuple.Locked = false;
            }
            if(!this.LockedTuples.TryRemove(new System.Tuple<string, int>(clientId, requestNumber), 
                                            out List<Tuple> tupleValues)) {
                throw new RequestDontHaveLocks();
            }
        }

        public void UnlockAndTake(string clientId, int requestNumber, Tuple tupleToRemove) {
            /* unlock and take are "atomic" */
            lock (this.Tuples) {
                this.Unlock(clientId, requestNumber);
                this.Tuples.Remove(tupleToRemove);
            }
        }

        private List<Tuple> GetLockedTuplesByClientRequest(string clientId, int requestNumber) {
            return this.LockedTuples[new System.Tuple<string, int>(clientId, requestNumber)];
        }

        private List<Tuple> SearchAndLockTuples(Tuple searchTuple) {
            List<Tuple> result = new List<Tuple>();
            foreach (Tuple tuple in this.Tuples) {
                if (!tuple.Match(searchTuple)) {
                    continue;
                }
                lock(tuple) {
                    if (tuple.Locked) {
                        throw new UnableToLockException();
                    }
                    tuple.Locked = true;
                }
                result.Add(tuple);
            }
            return result;
        }

        private List<Tuple> SearchTuples(Tuple searchTuple) {
            List<Tuple> result = new List<Tuple>();
            foreach (Tuple tuple in this.Tuples) {
                if (tuple.Match(searchTuple)) {
                    result.Add(tuple);
                }
            }
            return result;
        }
    }
}