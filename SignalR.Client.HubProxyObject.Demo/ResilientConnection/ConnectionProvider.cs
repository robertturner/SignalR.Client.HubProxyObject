using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.Demo.ResilientConnection
{
    public class ConnectionProvider<T> : IConnectionProvider<T>, IDisposable
    {
        private readonly SingleAssignmentDisposable disposable = new SingleAssignmentDisposable();
        private readonly IObservable<IConnection<T>> connectionSequence;
        private readonly string server;
        private readonly Func<HubConnection, T> proxyInitialiser;

        public ConnectionProvider(string server, Func<HubConnection, T> proxyInitialiser)
        {

            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.proxyInitialiser = proxyInitialiser ?? throw new ArgumentNullException(nameof(proxyInitialiser));
            connectionSequence = CreateConnectionSequence();
        }

        public IObservable<IConnection<T>> GetActiveConnection()
        {
            return connectionSequence;
        }

        public void Dispose()
        {
            disposable.Dispose();
        }

        private IObservable<IConnection<T>> CreateConnectionSequence()
        {
            return Observable.Create<IConnection<T>>(o =>
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

        private IConnection<T> GetNextConnection()
        {
            return new Connection<T>(server, proxyInitialiser);
        }
    }
}
