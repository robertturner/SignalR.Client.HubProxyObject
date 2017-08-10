using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.Demo.ResilientConnection
{
    public interface IConnectionProvider<T>
    {
        IObservable<IConnection<T>> GetActiveConnection();
    }

}
