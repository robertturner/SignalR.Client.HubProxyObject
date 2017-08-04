using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject
{
    public abstract class HubSignalBase
    {
        protected readonly string SignalName;
        protected Hub hub;

        internal HubSignalBase(Hub hub, string signalName = "")
        {
            SignalName = signalName;
            this.hub = hub;
        }

        internal abstract object GetCaller();
    }
}
