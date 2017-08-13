using Microsoft.AspNet.SignalR;
using SignalR.Client.HubProxyObject.DemoUICommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalR.Client.HubProxyObject.DemoUIServer
{
    [HubName("demoHub")]
    public class DemoHub : Hub, IDemoHub
    {
        Func<Tuple<string, string, object>, Dictionary<string, ADataItem>> allSourcesGetter;

        public DemoHub()
        {
        }

        public event Action<string> ASig;

        public event Action<(string Key, ListChangedType Change, string Property, object Data)> ItemChangeCallback;

        public Task<Dictionary<string, ADataItem>> GetOrUpdateItem(Tuple<string, string, object> updateOrAll = null)
        {
            return Task.FromResult(allSourcesGetter(updateOrAll));
        }
    }
}
