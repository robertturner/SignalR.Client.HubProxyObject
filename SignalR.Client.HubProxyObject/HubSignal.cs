using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject
{
    public class HubSignal : HubSignalBase
    {
        internal HubSignal(IHubConnectionContext<dynamic> callContext, string signalName = "") : base(callContext, signalName) { }

        public static HubSignal ForClient(IHubProxy proxy)
        {
            return new HubSignal(null);
        }

        public Task All(Hub hub = null)
        {
            var cp = (hub != null) ? hub.Clients.All : callContext.All;
            return cp.Invoke(SignalName);
        }

        public event Action On;
        internal override object GetCaller() { return (Action)(() => On?.Invoke()); }

        public static void ImplementSignals(Hub hub, IHubConnectionContext<dynamic> context = null)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));
            var type = hub.GetType();
            var allProps = type.GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);
            var props = allProps.Where(p => typeof(HubSignalBase).IsAssignableFrom(p.PropertyType)).ToArray();
            IHubConnectionContext<dynamic> callContext = context ?? hub.Clients;
            foreach (var prop in props)
            {
                if (!prop.CanWrite)
                    throw new ArgumentException($"HubSignal property {prop.Name} does not support writing.");

                var argType = prop.PropertyType.IsGenericType ? prop.PropertyType.GetGenericArguments()[0] : typeof(void);
                HubSignalBase hubSig;
                if (!prop.PropertyType.IsGenericType)
                    hubSig = new HubSignal(callContext, prop.Name);
                else
                {
                    var genSig = typeof(HubSignal<>).MakeGenericType(argType);
                    var ctor = genSig.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
                    hubSig = (HubSignalBase)ctor[0].Invoke(new object[] { callContext, prop.Name });
                }

                prop.SetMethod.Invoke(hub, new object[] { hubSig });
            }

        }
    }

    public class HubSignal<T> : HubSignalBase
    {
        internal HubSignal(IHubConnectionContext<dynamic> callContext, string signalName = "") : base(callContext, signalName) { }
        public Task All(T arg, Hub hub = null)
        {
            var cp = (hub != null) ? hub.Clients.All : callContext.All;
            return cp.Invoke(SignalName, arg);
        }

        public event Action<T> On;
        internal override object GetCaller() { return (Action <T>)(arg => On?.Invoke((T)arg)); }

    }

}
