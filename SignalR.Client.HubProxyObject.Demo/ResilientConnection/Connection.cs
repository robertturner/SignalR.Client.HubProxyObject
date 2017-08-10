using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.Demo.ResilientConnection
{
    public class Connection<T> : IConnection<T>
    {
        private readonly ISubject<ConnectionInfo> _statusStream;
        private readonly HubConnection hubConnection;

        private bool _initialized;
        //private static readonly ILog log = LogManager.GetLogger(typeof(Connection));

        public Connection(string address, Func<HubConnection, T> proxyInitialiser)
        {
            _statusStream = new BehaviorSubject<ConnectionInfo>(
                new ConnectionInfo(ConnectionInfo.ConnectionStatus.Uninitialized, address));
            Address = address;
            hubConnection = new HubConnection(address);
            //hubConnection.Headers.Add(ServiceConstants.Server.UsernameHeader, username);
            CreateStatus().Subscribe(
                s => _statusStream.OnNext(new ConnectionInfo(s, address)),
                _statusStream.OnError,
                _statusStream.OnCompleted);
            hubConnection.Error += exception =>
            {
                //log.Error("There was a connection error with " + address, exception);
            };

            try
            {
                HubProxies = proxyInitialiser(hubConnection);
            }
            catch (Exception ex) { }
            //TickerHubProxy = hubConnection.CreateHubProxy(ServiceConstants.Server.TickerHub);

        }

        public IObservable<Unit> Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException("Connection has already been initialized");
            }
            _initialized = true;

            return Observable.Create<Unit>(async observer =>
            {
                _statusStream.OnNext(new ConnectionInfo(ConnectionInfo.ConnectionStatus.Connecting, Address));

                try
                {
                    //log.InfoFormat("Connecting to {0}", Address);
                    await hubConnection.Start();
                    _statusStream.OnNext(new ConnectionInfo(ConnectionInfo.ConnectionStatus.Connected, Address));
                    observer.OnNext(Unit.Default);
                }
                catch (Exception e)
                {
                    //log.Error("An error occurred when starting SignalR connection", e);
                    observer.OnError(e);
                }

                return Disposable.Create(() =>
                {
                    try
                    {
                        //log.Info("Stoping connection...");
                        hubConnection.Stop();
                        //log.Info("Connection stopped");
                    }
                    catch (Exception e)
                    {
                        // we must never throw in a disposable
                        //log.Error("An error occurred while stoping connection", e);
                    }
                });
            })
            .Publish()
            .RefCount();
        }

        private IObservable<ConnectionInfo.ConnectionStatus> CreateStatus()
        {
            var closed = Observable.FromEvent(h => hubConnection.Closed += h,
                h => hubConnection.Closed -= h).Select(_ => ConnectionInfo.ConnectionStatus.Closed);
            var connectionSlow = Observable.FromEvent(h => hubConnection.ConnectionSlow += h,
                h => hubConnection.ConnectionSlow -= h).Select(_ => ConnectionInfo.ConnectionStatus.ConnectionSlow);
            var reconnected = Observable.FromEvent(h => hubConnection.Reconnected += h,
                h => hubConnection.Reconnected -= h).Select(_ => ConnectionInfo.ConnectionStatus.Reconnected);
            var reconnecting = Observable.FromEvent(h => hubConnection.Reconnecting += h,
                h => hubConnection.Reconnecting -= h).Select(_ => ConnectionInfo.ConnectionStatus.Reconnecting);
            return Observable.Merge(closed, connectionSlow, reconnected, reconnecting)
                .TakeUntilInclusive(status => status == ConnectionInfo.ConnectionStatus.Closed);
            // complete when the connection is closed (it's terminal, SignalR will not attempt to reconnect anymore)
        }

        public IObservable<ConnectionInfo> StatusStream
        {
            get { return _statusStream; }
        }

        public string Address { get; private set; }

        public T HubProxies { get; private set; }

        //public IHubProxy TickerHubProxy { get; private set; }

        /*public void SetAuthToken(string authToken)
        {
            //hubConnection.Headers[AuthTokenProvider.AuthTokenKey] = authToken;
        }*/

        public override string ToString()
        {
            return string.Format("Address: {0}", Address);
        }
    }
}
