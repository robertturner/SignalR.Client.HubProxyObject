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
        protected IHubConnectionContext<dynamic> callContext;

        internal HubSignalBase(IHubConnectionContext<dynamic> callContext, string signalName = "")
        {
            SignalName = signalName;
            this.callContext = callContext;
        }

        internal abstract object GetCaller();
    }
}
