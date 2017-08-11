using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;

namespace SignalR.Client.HubProxyObject.ResilientConnection
{
    public interface IConnection<T>
    {
            IObservable<ConnectionInfo> StatusStream { get; }
            IObservable<Unit> Initialize();
            string Address { get; }

            T HubProxies { get; }
    }
}
