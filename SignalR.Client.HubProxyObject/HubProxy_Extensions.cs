using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject
{
    public static class HubProxy_Extensions
    {
        public static T CreateProxy<T>(this HubConnection connection, string hubName) where T : class
        {
            return (T)new HubObjectProxy(connection, hubName, typeof(T)).Proxy;
        }
    }
}
