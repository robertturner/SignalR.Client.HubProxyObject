using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject
{
    public static class Hub_Extensions
    {
        static Dictionary<Type, Dictionary<string, EventInfo>> eventsCache = new Dictionary<Type, Dictionary<string, EventInfo>>();

        static void CheckName<THub>(this THub hub, string signalName) where THub : Hub
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));
            if (signalName == null)
                throw new ArgumentNullException(nameof(signalName));
            var t = typeof(THub);
            var events = eventsCache.GetOrSet(t, () => t.GetEvents(BindingFlags.Instance | BindingFlags.Public).ToDictionary(e => e.Name, e => e));
            if (!events.TryGetValue(signalName, out EventInfo ei))
                throw new ArgumentException($"Unknown event name {signalName} in hub {t.Name}");
        }

        public static Task CallAll<THub>(this THub hub, string signalName, params object[] args) where THub : Hub
        {
            CheckName(hub, signalName);
            var cc = hub.Clients.All;
            return cc.Invoke(signalName, args);
        }
        public static Task CallOthers<THub>(this THub hub, string signalName, params object[] args) where THub : Hub
        {
            CheckName(hub, signalName);
            var cc = hub.Clients.Others;
            return cc.Invoke(signalName, args);
        }
        public static Task CallCaller<THub>(this THub hub, string signalName, params object[] args) where THub : Hub
        {
            CheckName(hub, signalName);
            var cc = hub.Clients.Caller;
            return cc.Invoke(signalName, args);
        }
        public static Task CallGroup<THub>(this THub hub, string signalName, string group, params object[] args) where THub : Hub
        {
            CheckName(hub, signalName);
            var cc = hub.Clients.Group(group);
            return cc.Invoke(signalName, args);
        }
        public static Task CallOthersInGroup<THub>(this THub hub, string signalName, string group, params object[] args) where THub : Hub
        {
            CheckName(hub, signalName);
            var cc = hub.Clients.OthersInGroup(group);
            return cc.Invoke(signalName, args);
        }
    }
}
