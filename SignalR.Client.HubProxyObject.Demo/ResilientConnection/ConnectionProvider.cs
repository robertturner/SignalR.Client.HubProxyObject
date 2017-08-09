using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.Demo.ResilientConnection
{
    public class ConnectionProvider : IConnectionProvider, IDisposable
    {
        private readonly SingleAssignmentDisposable disposable = new SingleAssignmentDisposable();
        private readonly IObservable<IConnection> connectionSequence;
        private readonly string server;

        public ConnectionProvider(string server)
        {
            this.server = server;
            connectionSequence = CreateConnectionSequence();
        }

        public IObservable<IConnection> GetActiveConnection()
        {
            return connectionSequence;
        }

        public void Dispose()
        {
            disposable.Dispose();
        }

        private IObservable<IConnection> CreateConnectionSequence()
        {
            return Observable.Create<IConnection>(o =>
            {
                //log.Info("Creating new connection...");
                var connection = GetNextConnection();

                var statusSubscription = connection.StatusStream.Subscribe(
                    _ => { },
                    ex => o.OnCompleted(),
                    () =>
                    {
                        //log.Info("Status subscription completed");
                        o.OnCompleted();
                    });

                var connectionSubscription =
                    connection.Initialize().Subscribe(
                        _ => o.OnNext(connection),
                        ex => o.OnCompleted(),
                        o.OnCompleted);

                return new CompositeDisposable { statusSubscription, connectionSubscription };
            })
            .Repeat()
            .Replay(1)
            .LazilyConnect(disposable);
        }

        private IConnection GetNextConnection()
        {
            return new Connection(server);
        }
    }
}
