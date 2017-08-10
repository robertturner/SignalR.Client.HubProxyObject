using Microsoft.AspNet.SignalR;
using Newtonsoft.Json.Linq;
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
        public class DictionaryChangedEventArgs<TKey> : EventArgs
        {
            public DictionaryChangedEventArgs(ListChangedType listChangedType, TKey key = default(TKey), PropertyDescriptor propDesc = null, object property = null)
            {
                ListChangedType = listChangedType; Key = key; PropertyDescriptor = propDesc; Property = property;
            }

            public ListChangedType ListChangedType { get; private set; }
            public TKey Key { get; private set; }
            public PropertyDescriptor PropertyDescriptor { get; private set; }
            public object Property { get; private set; }
        }

        public delegate void DictionaryChangedEventHandler<TKey>(object sender, DictionaryChangedEventArgs<TKey> e);

        public interface IBindingReadonlyDictionaryProxy<TKey, TData>
            : IReadOnlyDictionary<TKey, TData>, IDisposable
        {
            event DictionaryChangedEventHandler<TKey> DictionaryChanged;
        }

        class BindingReadonlyDictionaryProxy<TKey, TData> : Dictionary<TKey, TData>, IBindingReadonlyDictionaryProxy<TKey, TData>
        {
            Action disposer;
            public BindingReadonlyDictionaryProxy(Dictionary<TKey, TData> init, Action disposer) : base(init) { this.disposer = disposer; }
            public BindingReadonlyDictionaryProxy() { }

            public void RaiseListChanged(DictionaryChangedEventArgs<TKey> args) { DictionaryChanged?.Invoke(this, args); }

            public void Dispose()
            {
                disposer?.Invoke();
                disposer = null;
            }

            public void ProxyAdd(TKey key, TData value) { this[key] = value; }
            public void ProxyDelete(TKey key) { Remove(key); }
            public void ProxyClear() { Clear(); }

            public event DictionaryChangedEventHandler<TKey> DictionaryChanged;
        }

        public static async Task<IBindingReadonlyDictionaryProxy<TDataKey, TList>> GetProxy<TDataKey, TList>(
            Func<Task<Dictionary<TDataKey, TList>>> getAllCaller,
            HubSignal<(TDataKey Key, ListChangedType Change, string Property, object Data)> itemChanged)
        {
            var dictT = getAllCaller();
            var props = TypeDescriptor.GetProperties(typeof(TList));
            BindingReadonlyDictionaryProxy<TDataKey, TList> bl = null;
            Action<(TDataKey Key, ListChangedType Change, string Property, object Data)> onChange = args =>
            {
                var data = args.Data;
                var jObj = data as JObject;

                switch (args.Change)
                {
                    case ListChangedType.ItemChanged:
                        {
                            PropertyDescriptor pd = null;
                            var val = args.Data;
                            if (!string.IsNullOrEmpty(args.Property))
                            {
                                pd = props.Find(args.Property, true);
                                if (pd != null && args.Data != null)
                                {
                                    try
                                    {
                                        if (jObj != null)
                                            val = jObj.ToObject(pd.PropertyType);
                                    }
                                    catch (Exception ex)
                                    { }
                                    try
                                    {
                                        if (!pd.IsReadOnly)
                                            pd.SetValue(bl[args.Key], val);
                                    }
                                    catch (InvalidCastException) // Unlikely but necessary
                                    { }
                                }
                            }
                            bl.RaiseListChanged(new DictionaryChangedEventArgs<TDataKey>(args.Change, args.Key, pd, val));
                        }
                        break;
                    case ListChangedType.ItemAdded:
                        {
                            TList newItem = default(TList);
                            if (jObj != null)
                            {
                                try
                                {
                                    newItem = jObj.ToObject<TList>();
                                }
                                catch (Exception ex)
                                { }
                            }
                            bl.ProxyAdd(args.Key, newItem);
                            bl.RaiseListChanged(new DictionaryChangedEventArgs<TDataKey>(args.Change, args.Key, null, newItem));
                        }
                        break;
                    case ListChangedType.ItemDeleted:
                        bl.ProxyDelete(args.Key);
                        bl.RaiseListChanged(new DictionaryChangedEventArgs<TDataKey>(args.Change, args.Key));
                        break;
                    case ListChangedType.Reset:
                        bl.ProxyClear();
                        bl.RaiseListChanged(new DictionaryChangedEventArgs<TDataKey>(args.Change));
                        break;
                }
            };
            bl = new BindingReadonlyDictionaryProxy<TDataKey, TList>(await dictT, () => itemChanged.On -= onChange);
            itemChanged.On += onChange;
            return bl;
        }


        public static (Func<Dictionary<TDataKey, TList>> GetAll, HubSignal<(TDataKey Key, ListChangedType Change, string Property, object Data)> Updater) 
            GetHubEntries<TList, TDataKey>(Hub hub, BindingList<TList> sourceList, Func<TList, TDataKey> keyProvider, string updaterSignalName)
        {
            Func<Dictionary<TDataKey, TList>> getAll = () => sourceList.ToDictionary(v => keyProvider(v), v => v);
            var objProperties = typeof(TList).GetProperties(System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p);

            var hubSignal = HubSignal<(TDataKey Key, ListChangedType Change, string Property, object Data)>.Create(hub, updaterSignalName);
            sourceList.ListChanged += (sender, args) =>
            {
                var newItem = (args.NewIndex >= 0) ? sourceList[args.NewIndex] : default(TList);
                var oldItem = (args.OldIndex >= 0) ? sourceList[args.OldIndex] : default(TList);
                switch (args.ListChangedType)
                {
                    case ListChangedType.ItemAdded:
                        hubSignal.All((keyProvider(newItem), args.ListChangedType, null, newItem));
                        break;
                    case ListChangedType.ItemChanged:
                        hubSignal.All((keyProvider(newItem), args.ListChangedType, args.PropertyDescriptor.Name, objProperties.GetValueOrDefault(args.PropertyDescriptor.Name).GetMethod.Invoke(newItem, new object[0])));
                        break;
                    case ListChangedType.ItemDeleted:
                        hubSignal.All((keyProvider(oldItem), args.ListChangedType, null, null));
                        break;
                    case ListChangedType.Reset:
                        hubSignal.All((default(TDataKey), args.ListChangedType, null, null));
                        break;
                    default:
                        throw new ArgumentException("Unhandled/unimplemented/unsupported change type: " + args.ListChangedType.ToString());
                }
            };

            return (getAll, hubSignal);
        }

    }
}
