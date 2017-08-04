using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.Demo
{
    public class MyHub : Hub, IMyHub
    {
        public MyHub()
        {
            HubSignal.ImplementSignals(this, GlobalHost.ConnectionManager.GetHubContext<MyHub>().Clients);
        }

        public HubSignal<(string Arg1, int Arg2)> ASignal { get; private set; } // auto populated

        public Task<string> AsyncMethod(string arg1)
        {
            return Task.FromResult("AsyncMethod called with arg1: " + arg1);
        }

        public Task MethodWithNoReturn(int arg1, string arg2)
        {
            return Task.CompletedTask;
        }

        public async void SyncMethodNoArgs()
        {
            await ASignal.All(("bob", 55));
        }
    }
}
