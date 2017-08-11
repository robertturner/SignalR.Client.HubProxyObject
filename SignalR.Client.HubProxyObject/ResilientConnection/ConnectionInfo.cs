using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.ResilientConnection
{
    public class ConnectionInfo
    {
        public enum ConnectionStatus
        {
            Connecting,
            Connected,
            ConnectionSlow,
            Reconnecting,
            Reconnected,
            Closed,
            Uninitialized
        }

        public ConnectionStatus Status { get; private set; }
        public string Server { get; private set; }

        public ConnectionInfo(ConnectionStatus connectionStatus, string server)
        {
            Status = connectionStatus;
            Server = server;
        }

        public override string ToString()
        {
            return string.Format("ConnectionStatus: {0}, Server: {1}", Status, Server);
        }
    }
}
