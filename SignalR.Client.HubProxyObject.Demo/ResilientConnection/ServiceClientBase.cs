﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.Demo.ResilientConnection
{
    class ServiceClientBase
    {
        private readonly IConnectionProvider _connectionProvider;

        protected ServiceClientBase(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        protected IObservable<T> GetResilientStream<T>(Func<IConnection, IObservable<T>> streamFactory, TimeSpan connectionTimeout)
        {
            var activeConnections = (from connection in _connectionProvider.GetActiveConnection()
                                     from status in connection.StatusStream
                                     where status.Status == ConnectionInfo.ConnectionStatus.Connected ||
                                        status.Status == ConnectionInfo.ConnectionStatus.Reconnected
                                     select connection)
                .Publish()
                .RefCount();

            // get the first connection
            var firstConnection = activeConnections.Take(1).Timeout(connectionTimeout);

            // 1 - notifies when the first connection gets disconnected
            var firstDisconnection = from connection in firstConnection
                                     from status in connection.StatusStream
                                     where status.Status == ConnectionInfo.ConnectionStatus.Reconnecting ||
                                        status.Status == ConnectionInfo.ConnectionStatus.Closed
                                     select Unit.Default;

            // 2- connection provider created a new connection it means the active one has droped
            var subsequentConnection = activeConnections.Skip(1).Select(_ => Unit.Default).Take(1);

            // OnError when we get 1 or 2
            var disconnected = firstDisconnection.Merge(subsequentConnection)
                .Select(_ => Notification.CreateOnError<T>(new Exception("Connection was closed.")))
                .Dematerialize();

            // create a stream which will OnError as soon as the connection drops
            return (from connection in firstConnection
                    from t in streamFactory(connection)
                    select t)
                .Merge(disconnected)
                .Publish()
                .RefCount();
        }
    }
}
