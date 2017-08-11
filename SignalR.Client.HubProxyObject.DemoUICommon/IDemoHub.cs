using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.DemoUICommon
{
    public interface IDemoHub
    {
        Task<Dictionary<string, ADataItem>> GetOrUpdateItem(Tuple<string, string, object> updateOrAll = null);
        //HubSignal<(string Key, ListChangedType Change, string Property, object Data)> ItemChangeCallback { get; }
        event Action<(string Key, ListChangedType Change, string Property, object Data)> ItemChangeCallback;
    }
}
