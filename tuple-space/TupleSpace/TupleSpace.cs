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
            lock(this.Tuples) {
                this.Tuples.Add(tuple);
            }
            Console.WriteLine("Added Tuple: " + tuple);
        }

        /// <summary>
        /// This method returns the first tuple found that matches the tupleString.
        /// If no match was found, it returns null.
        /// </summary>
        public Tuple Read(string tupleString) {
            Tuple tuple = new Tuple(tupleString);
            List<Tuple> matches = new List<Tuple>();
            lock (this.Tuples) {
                matches = this.SearchTuples(tuple);
            }
            if (matches.Count > 0) {
                Console.WriteLine("\nRead Tuple: " + matches[0]);
                return matches[0];
            }
            Console.WriteLine("\nRead: No Tuple was found.");
            return null;
        }

        /// <summary>
        /// This method removes and returns the first tuple found that matches the tupleString.
        /// If no match was found, it returns null.
        /// </summary>
        /// <param name="tupleString">Tuple string.</param>
        public Tuple Take(string tupleString) {
            Tuple tuple = new Tuple(tupleString);
            List<Tuple> matches = new List<Tuple>();
            lock (this.Tuples) {
                matches = this.SearchTuples(tuple);
                if (matches.Count > 0) {
                    Tuple removed = matches[0];
                    this.Tuples.Remove(removed);
                    Console.WriteLine("\nTake Tuple: " + removed);
                    return removed;
                }
            }
            Console.WriteLine("\nTake: No Tuple was found.");
            return null;
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
            lock(this.Tuples) {
                foreach (Tuple tuple in lockedTuples) {
                    tuple.Locked = false;
                }
            }
            if(!this.LockedTuples.TryRemove(new System.Tuple<string, int>(clientId, requestNumber), 
                                            out List<Tuple> tupleValues)) {
                throw new RequestDontHaveLocks();
            }
        }

        public void UnlockAndTake(string clientId, int requestNumber, Tuple tupleToRemove) {
            /* unlock and take are "atomic" */
            List<Tuple> lockedTuples = this.GetLockedTuplesByClientRequest(clientId, requestNumber);
            lock (this.Tuples) {
                /* unlock all matches */
                foreach (Tuple tuple in lockedTuples)  {
                    tuple.Locked = false;
                }
                this.Tuples.Remove(tupleToRemove);
            }
            if (!this.LockedTuples.TryRemove(new System.Tuple<string, int>(clientId, requestNumber),
                                            out List<Tuple> tupleValues)) {
                throw new RequestDontHaveLocks();
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