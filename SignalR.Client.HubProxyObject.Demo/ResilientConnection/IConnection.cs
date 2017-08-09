using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;

namespace SignalR.Client.HubProxyObject.Demo.ResilientConnection
{
    public interface IConnection
    {
            IObservable<ConnectionInfo> StatusStream { get; }
            IObservable<Unit> Initialize();
            string Address { get; }
            //void SetAuthToken(string authToken);
            //IHubProxy TickerHubProxy { get; }

        
    }
}
