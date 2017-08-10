using Microsoft.AspNet.SignalR;
using SignalR.Client.HubProxyObject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject
{
    public static class BindingListHubWrapper
    {
        public interface IBindingReadonlyDictionaryProxy<TKey, TData>
            : IReadOnlyDictionary<TKey, TData>
        {
            event ListChangedEventHandler ListChanged;
        }

        class BindingReadonlyDictionaryProxy<TKey, TData> : Dictionary<TKey, TData>, IBindingReadonlyDictionaryProxy<TKey, TData>
        {
            public BindingReadonlyDictionaryProxy(Dictionary<TKey, TData> init)
                : base(init)
            { }

            public void RaiseListChanged(ListChangedEventArgs args)
            {
                ListChanged?.Invoke(this, args);
            }
            public event ListChangedEventHandler ListChanged;
        }

        public static async Task<IBindingReadonlyDictionaryProxy<TDataKey, TList>> GetProxy<TDataKey, TList>(
            Func<Task<Dictionary<TDataKey, object>>> getAllCaller,
            HubSignal<(TDataKey Key, ListChangedType Change, object Data)> itemChanged)
        {

            var 

            itemChanged.On += args =>
            {

            };
            var dict = await getAllCaller();

            //throw new NotImplementedException();
            return new BindingReadonlyDictionaryProxy<TDataKey, TList>();
        }


        public static (Func<Dictionary<TDataKey, object>> GetAll, HubSignal<(TDataKey Key, ListChangedType Change, object Data)> Updater) 
            GetHubEntries<TList, TDataKey>(Hub hub, BindingList<TList> sourceList, Func<TList, TDataKey> keyProvider, string updaterSignalName)
        {
            Func<Dictionary<TDataKey, object>> getAll = () => sourceList.ToDictionary(v => keyProvider(v), v => (object)v);

            var hubSignal = HubSignal<(TDataKey Key, ListChangedType Change, object Data)>.Create(hub, updaterSignalName);
            sourceList.ListChanged += (sender, args) =>
            {
                var newItem = (args.NewIndex >= 0) ? sourceList[args.NewIndex] : default(TList);
                var oldItem = (args.OldIndex >= 0) ? sourceList[args.OldIndex] : default(TList);
                switch (args.ListChangedType)
                {
                    case ListChangedType.ItemChanged:
                    case ListChangedType.ItemAdded:
                        hubSignal.All((keyProvider(newItem), args.ListChangedType, newItem));
                        break;
                    case ListChangedType.ItemDeleted:
                        hubSignal.All((keyProvider(oldItem), args.ListChangedType, oldItem));
                        break;
                }
            };

            return (getAll, hubSignal);
        }

    }
}
