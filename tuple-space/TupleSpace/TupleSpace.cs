using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TupleSpace.Exceptions;

namespace TupleSpace {
    [Serializable]
    public class TupleSpace {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(TupleSpace));

        public List<Tuple> Tuples { get; set; }
        public ConcurrentDictionary<string, List<Tuple>> LockedTuples {get; set; }

        public TupleSpace() {
            this.Tuples = new List<Tuple>();
            this.LockedTuples = new ConcurrentDictionary<string, List<Tuple>>();
        }

        public TupleSpace(List<Tuple> tuples) {
            this.Tuples = new List<Tuple>(tuples);
            this.LockedTuples = new ConcurrentDictionary<string, List<Tuple>>();
        }

        public void Add(string tupleString) {
            Tuple tuple = new Tuple(tupleString);
            lock(this.Tuples) {
                this.Tuples.Add(tuple);
            }
            Log.Info($"Added Tuple: {tuple}");
        }

        /// <summary>
        /// This method returns the first tuple found that matches the tupleString.
        /// If a match isn't found, it returns null.
        /// </summary>
        public Tuple Read(string tupleString) {
            Tuple tuple = new Tuple(tupleString);
            List<Tuple> matches = new List<Tuple>();
            lock (this.Tuples) {
                matches = this.SearchTuples(tuple);
            }
            if (matches.Count > 0) {
                Log.Info($"Read Tuple: {matches[0]}");
                return matches[0];
            }
            Log.Warn($"Read: Tuple {tupleString} was not found.");
            return null;
        }

        /// <summary>
        /// This method removes and returns the first tuple found that matches the tupleString.
        /// If a match isn't found, it returns null.
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
                    Log.Info($"Take Tuple: {removed}");

                    return removed;
                }
            }
            Log.Warn($"Take: Tuple {tupleString} was not found.");
            return null;
        }

        public List<string> GetAndLock(string clientId, string tupleString) {
            Tuple searchTuple = new Tuple(tupleString);
            List<Tuple> lockedTuples = new List<Tuple>();
            bool refuse = false;

            if (this.GetLockedTuplesByClient(clientId, out lockedTuples)) {
                return lockedTuples.Select(s => s.ToString()).ToList();
            }

            /* only one thread can acquire locks at each time */
            lock (this.Tuples) {
                try {
                    lockedTuples = this.SearchAndLockTuples(searchTuple);
                } catch(Exception e) {
                    refuse |= e is UnableToLockException;
                }
            }

            /* unlock previous locked tuples if refused locks */
            if(refuse || !this.LockedTuples.TryAdd(clientId, lockedTuples)) {
                Log.Debug($"Request from client {clientId} got lock refuse for tuple {tupleString}. Starting process of unlocking.");
                foreach(Tuple tuple in lockedTuples) {
                    tuple.Locked = false;
                }

                return null;
            } 

            List<string> lockedTuplesString = lockedTuples.Select(s => s.ToString()).ToList();
            return lockedTuplesString;
        }

        public bool Unlock(string clientId) {
            if (!this.GetLockedTuplesByClient(clientId, out List<Tuple> lockedTuples)) {
                return false;
            }

            /* unlock all matches */
            lock (this.Tuples) {
                foreach (Tuple tuple in lockedTuples) {
                    tuple.Locked = false;
                }
            }
            if(!this.LockedTuples.TryRemove(clientId, out List<Tuple> _)) {
                return false;
            }

            return true;
        }

        public bool UnlockAndTake(string clientId, string tupleString) {
            /* unlock and take are "atomic" */
            if (!this.GetLockedTuplesByClient(clientId, out List<Tuple> lockedTuples)) {
                return false;
            }
            
            lock (this.Tuples) {
                /* unlock all matches */
                foreach (Tuple tuple in lockedTuples) {
                    tuple.Locked = false;
                }
                this.Tuples.Remove(new Tuple(tupleString));
            }
            if (!this.LockedTuples.TryRemove(clientId, out List<Tuple> _)) {
                return false;
            }

            return true;
        }

        private bool GetLockedTuplesByClient(string clientId, out List<Tuple> lockedTuples) {
            return this.LockedTuples.TryGetValue(clientId, out lockedTuples);
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

        public string Status() {
            StringBuilder status = new StringBuilder();
            lock (this.Tuples) {
                foreach (Tuple tuple in this.Tuples) {
                    status.Append($"{tuple} {Environment.NewLine}");
                }
            }

            return status.ToString();
        }
    }
}