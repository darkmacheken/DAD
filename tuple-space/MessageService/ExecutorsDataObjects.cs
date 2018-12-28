using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using MessageService.Serializable;

namespace MessageService {
    public interface IExecutorVisitor {
        void ExecuteAdd(AddExecutor addExecutor);

        void ExecuteTake(TakeExecutor takeExecutor);

        void ExecuteRead(ReadExecutor readExecutor);
    }

    public interface IExecutorXLVisitor : IExecutorVisitor{
        void ExecuteGetAndLock(GetAndLockExecutor getAndLockExecutor);

        void ExecuteUnlock(UnlockExecutor unlockExecutor);
    }

    [Serializable]
    public abstract class Executor : ClientResponse {
        [NonSerialized] private AutoResetEvent executed;

        public string ClientId { get; set; }
        public string Tuple { get; set; }
        public new int RequestNumber { get; set; }
        public int OpNumber { get; set; }
        public AutoResetEvent Executed {
            get => this.executed;
            set => this.executed = value;
        }
        public bool AddedToQueue { get; set; }

        protected Executor(ClientRequest clientRequest) {
            this.ClientId = clientRequest.ClientId;
            this.Tuple = clientRequest.Tuple;
            this.RequestNumber = clientRequest.RequestNumber;
            this.Executed = new AutoResetEvent(false);
            this.AddedToQueue = false;
        }

        protected Executor(ClientRequest clientRequest, int opNumber) {
            this.ClientId = clientRequest.ClientId;
            this.Tuple = clientRequest.Tuple;
            this.RequestNumber = clientRequest.RequestNumber;
            this.OpNumber = opNumber;
            this.Executed = new AutoResetEvent(false);
            this.AddedToQueue = false;
        }

        public abstract void Execute(IExecutorVisitor visitor);
    }

    [Serializable]
    public class AddExecutor : Executor {
        public AddExecutor(ClientRequest clientRequest) : base(clientRequest) { }

        public AddExecutor(ClientRequest clientRequest, int opNumber) 
            : base(clientRequest, opNumber) { }

        public override void Execute(IExecutorVisitor visitor) {
            visitor.ExecuteAdd(this);
        }
    }

    [Serializable]
    public class TakeExecutor : Executor {
        public TakeExecutor(ClientRequest clientRequest) : base(clientRequest) { }

        public TakeExecutor(ClientRequest clientRequest, int opNumber) 
            : base(clientRequest, opNumber) { }

        public override void Execute(IExecutorVisitor visitor) {
            visitor.ExecuteTake(this);
        }
    }

    [Serializable]
    public class ReadExecutor : Executor {
        public ReadExecutor(ClientRequest clientRequest) : base(clientRequest) { }

        public ReadExecutor(ClientRequest clientRequest, int opNumber) : base(clientRequest, opNumber) { }

        public override void Execute(IExecutorVisitor visitor) {
            visitor.ExecuteRead(this);
        }
    }

    public static class ExecutorFactory {
        private static readonly ConcurrentDictionary<ClientRequest, Executor> Executors = new ConcurrentDictionary<ClientRequest, Executor>();

        public static Executor Factory(ClientRequest clientRequest, int opNumber) {
            Executor clientExecutor;
            lock (clientRequest) {
                if (Executors.TryGetValue(clientRequest, out clientExecutor)) {
                    return clientExecutor;
                }
            }

            if (clientRequest is AddRequest) {
                clientExecutor = new AddExecutor(clientRequest, opNumber);
            } else if (clientRequest is TakeRequest) {
                clientExecutor = new TakeExecutor(clientRequest, opNumber);
            } else if (clientRequest is ReadRequest) {
                clientExecutor = new ReadExecutor(clientRequest, opNumber);
            }

            Executors.TryAdd(clientRequest, clientExecutor);
            return clientExecutor;
        }
    }

    // XL --------------------------------------------------------------------------------
    [Serializable]
    public abstract class ExecutorXL : Executor {
        protected ExecutorXL(ClientRequest clientRequest) 
            : base(clientRequest) { }
    }

    [Serializable]
    public class GetAndLockExecutor : Executor {
        public GetAndLockExecutor(ClientRequest clientRequest) : base(clientRequest) { }

        public override void Execute(IExecutorVisitor visitor) {
            (visitor as IExecutorXLVisitor)?.ExecuteGetAndLock(this);

        }
    }

    [Serializable]
    public class UnlockExecutor : Executor {
        public UnlockExecutor(ClientRequest clientRequest) : base(clientRequest) { }

        public override void Execute(IExecutorVisitor visitor) {
            (visitor as IExecutorXLVisitor)?.ExecuteUnlock(this);
        }
    }
}