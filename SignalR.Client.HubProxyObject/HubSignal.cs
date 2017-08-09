using Fasterflect;
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
        internal HubSignal(Hub hub, string signalName = "") : base(hub, signalName) { }

        public static HubSignal ForClient(IHubProxy proxy) { return new HubSignal(null); }

        public Task All(Hub hub = null)
        {
            var cp = hub.Clients.All;
            return cp.Invoke(SignalName);
        }
        public Task Other(Hub hub = null)
        {
            var cp = hub.Clients.Others;
            return cp.Invoke(SignalName);
        }
        public Task Caller(Hub hub = null)
        {
            var cp = hub.Clients.Caller;
            return cp.Invoke(SignalName);
        }
        public Task Group(string group)
        {
            var cp = hub.Clients.Group(group);
            return cp.Invoke(SignalName);
        }
        public Task OthersInGroup(string group)
        {
            var cp = hub.Clients.OthersInGroup(group);
            return cp.Invoke(SignalName);
        }

        public event Action On;
        internal override object GetCaller() { return (Action)(() => On?.Invoke()); }

        public static void ImplementSignals(Hub hub)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));
            var type = hub.GetType();
            var allProps = type.GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);
            var props = allProps.Where(p => typeof(HubSignalBase).IsAssignableFrom(p.PropertyType)).ToArray();

            foreach (var prop in props)
            {
                if (!prop.CanWrite)
                    throw new ArgumentException($"HubSignal property {prop.Name} does not support writing.");
                if (prop.GetMethod.Invoke(hub, new object[0]) != null)
                    continue;

                var argType = prop.PropertyType.IsGenericType ? prop.PropertyType.GetGenericArguments()[0] : typeof(void);
                HubSignalBase hubSig;
                if (!prop.PropertyType.IsGenericType)
                    hubSig = new HubSignal(hub, prop.Name);
                else
                {
                    var genSig = typeof(HubSignal<>).MakeGenericType(argType);
                    var ctor = genSig.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
                    hubSig = (HubSignalBase)ctor[0].Invoke(new object[] { hub, prop.Name });
                }

                prop.SetMethod.Invoke(hub, new object[] { hubSig });
            }

        }
    }

    public class HubSignal<T> : HubSignalBase
    {
        internal HubSignal(Hub hub, string signalName = "") : base(hub, signalName) { }
        public Task All(T arg)
        {
            var cp = hub.Clients.All;
            return cp.Invoke(SignalName, arg);
        }
        public Task Others(T arg)
        {
            var cp = hub.Clients.Others;
            return cp.Invoke(SignalName, arg);
        }
        public Task Caller(T arg)
        {
            var cp = hub.Clients.Caller;
            return cp.Invoke(SignalName, arg);
        }
        public Task Group(string group, T arg)
        {
            var cp = hub.Clients.Group(group);
            return cp.Invoke(SignalName, arg);
        }
        public Task OthersInGroup(string group, T arg)
        {
            var cp = hub.Clients.OthersInGroup(group);
            return cp.Invoke(SignalName, arg);
        }

        public event Action<T> On;
        internal override object GetCaller() { return (Action <T>)(arg => On?.Invoke((T)arg)); }

        public static HubSignal<T> Create(Hub hub, string signalName)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));
            if (string.IsNullOrEmpty(signalName))
                throw new ArgumentNullException(nameof(signalName));
            return new HubSignal<T>(hub, signalName);
        }
    }

}
