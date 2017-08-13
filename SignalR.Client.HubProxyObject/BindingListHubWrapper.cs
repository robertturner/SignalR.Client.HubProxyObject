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

        class BindingReadonlyDictionaryProxy<TKey, TValue> : Dictionary<TKey, TValue>, IBindingReadonlyDictionaryProxy<TKey, TValue>
        {
            public Tuple<TKey> SuppressKey = null;
            Action disposer;
            public BindingReadonlyDictionaryProxy(Dictionary<TKey, TValue> init, Action disposer = null) : base(init) { this.disposer = disposer; }
            public BindingReadonlyDictionaryProxy() { }

            public void RaiseListChanged(DictionaryChangedEventArgs<TKey> args) { DictionaryChanged?.Invoke(this, args); }

            public void Dispose()
            {
                disposer?.Invoke();
                disposer = null;
            }

            public void ProxyAdd(TKey key, TValue value) { this[key] = value; }
            public void ProxyDelete(TKey key) { Remove(key); }
            public void ProxyClear() { Clear(); }

            public event DictionaryChangedEventHandler<TKey> DictionaryChanged;
        }

        class ValContainer<TKey>
        {
            public Tuple<TKey> Key = null;
        }

        public delegate void ItemUpdateHandler<TKey>((TKey Key, ListChangedType Change, string Property, object Data) Arg);

        public static async Task<(IBindingReadonlyDictionaryProxy<TKey, TValue> BindingDict, ItemUpdateHandler<TKey> Caller)> 
            GetProxy<TKey, TValue>(
                Func<Tuple<TKey, string, object>, Task<Dictionary<TKey, TValue>>> getAllOrUpdater,
                Func<TValue, TKey> keyProvider)
        {
            if (getAllOrUpdater == null)
                throw new ArgumentNullException(nameof(getAllOrUpdater));
            if (keyProvider == null)
                throw new ArgumentNullException(nameof(keyProvider));

            var dictT = getAllOrUpdater(null);
            var dcea = new DictionaryChangedEventArgs<TKey>(ListChangedType.ItemAdded, default(TKey));
            var props = TypeDescriptor.GetProperties(typeof(TValue));
            BindingReadonlyDictionaryProxy<TKey, TValue> bl = null;

            PropertyChangedEventHandler itemChangedLocally = (sender, propChangedArgs) =>
            {
                if (sender is TValue value)
                {
                    var key = keyProvider(value);
                    if (bl.SuppressKey == null || !bl.SuppressKey.Item1.Equals(key))
                    {
                        var val = props.Find(propChangedArgs.PropertyName, false).GetValue(sender);
                        getAllOrUpdater(new Tuple<TKey, string, object>(key, propChangedArgs.PropertyName, val)); // fire it off and not worry about task
                    }
                }
            };

            ItemUpdateHandler<TKey> onChange = args =>
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
                                        {
                                            var item = bl[args.Key];
                                            lock (item)
                                            {
                                                bl.SuppressKey = new Tuple<TKey>(args.Key);
                                                try
                                                {
                                                    pd.SetValue(item, val);
                                                }
                                                finally
                                                {
                                                    bl.SuppressKey = null;
                                                }
                                            }
                                        }
                                    }
                                    catch (InvalidCastException) // Unlikely but necessary
                                    { }
                                }
                            }
                            bl.RaiseListChanged(new DictionaryChangedEventArgs<TKey>(args.Change, args.Key, pd, val));
                        }
                        break;
                    case ListChangedType.ItemAdded:
                        {
                            TValue newItem = default(TValue);
                            if (jObj != null)
                            {
                                try
                                {
                                    newItem = jObj.ToObject<TValue>();
                                }
                                catch (Exception ex)
                                { }
                            }
                            if (newItem is INotifyPropertyChanged npc)
                                npc.PropertyChanged += itemChangedLocally;
                            bl.ProxyAdd(args.Key, newItem);
                            bl.RaiseListChanged(new DictionaryChangedEventArgs<TKey>(args.Change, args.Key, null, newItem));
                        }
                        break;
                    case ListChangedType.ItemDeleted:
                        if (typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(TValue)))
                            ((INotifyPropertyChanged)bl[args.Key]).PropertyChanged -= itemChangedLocally;
                        bl.ProxyDelete(args.Key);
                        bl.RaiseListChanged(new DictionaryChangedEventArgs<TKey>(args.Change, args.Key));
                        break;
                    case ListChangedType.Reset:
                        if (typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(TValue)))
                        {
                            foreach (var kvp in bl)
                                ((INotifyPropertyChanged)bl).PropertyChanged -= itemChangedLocally;
                        }
                        bl.ProxyClear();
                        bl.RaiseListChanged(new DictionaryChangedEventArgs<TKey>(args.Change));
                        break;
                }
            };
            var initDict = await dictT;
            if (typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(TValue)))
            {
                foreach (var kvp in initDict)
                    ((INotifyPropertyChanged)kvp.Value).PropertyChanged += itemChangedLocally;
            }
            bl = new BindingReadonlyDictionaryProxy<TKey, TValue>(initDict);
            return (bl, onChange);
        }

        public static Func<Tuple<TKey, string, object>, Dictionary<TKey, TValue>>
            GetHubEntries<TKey, TValue>(
                BindingList<TValue> sourceList, 
                Func<TValue, TKey> keyProvider, 
                ItemUpdateHandler<TKey> Updater)
        {
            var props = TypeDescriptor.GetProperties(typeof(TValue));
            ValContainer<TKey> suppressFromKey = new ValContainer<TKey>();
            Func<Tuple<TKey, string, object>, Dictionary<TKey, TValue>> getAll = arg =>
            {
                if (arg == null)
                    return sourceList.ToDictionary(v => keyProvider(v), v => v);
                else
                {
                    // Value updated. Need to suppress callback before updating
                    TValue value;
                    lock (sourceList)
                    {
                        var match = sourceList.Where(item => keyProvider(item).Equals(arg.Item1));
                        if (match.Any() && !match.Skip(1).Any())
                            value = match.First();
                        else
                            return null;
                    }
                    var propDesc = props.Find(arg.Item2, false);
                    if (propDesc != null)
                    {
                        var key = keyProvider(value);
                        var propVal = arg.Item3;
                        if (propVal is JObject jObj)
                            propVal = jObj.ToObject(propDesc.PropertyType);
                        lock (value)
                        {
                            suppressFromKey.Key = new Tuple<TKey>(key);
                            try
                            {
                                propDesc.SetValue(value, propVal);
                            }
                            finally
                            {
                                suppressFromKey.Key = null;
                            }
                        }
                    }
                    return null;
                }
            };

            ListChangedEventHandler listChangedHandler = (sender, args) =>
            {
                var newItem = (args.NewIndex >= 0) ? sourceList[args.NewIndex] : default(TValue);
                var oldItem = (args.OldIndex >= 0) ? sourceList[args.OldIndex] : default(TValue);
                switch (args.ListChangedType)
                {
                    case ListChangedType.ItemAdded:
                        Updater((keyProvider(newItem), args.ListChangedType, null, newItem));
                        break;
                    case ListChangedType.ItemChanged:
                        var key = keyProvider(newItem);
                        if (suppressFromKey.Key == null || !suppressFromKey.Key.Item1.Equals(key))
                        {
                            if (args.PropertyDescriptor != null)
                                Updater((key, args.ListChangedType, args.PropertyDescriptor.Name, args.PropertyDescriptor.GetValue(newItem)));
                            else
                                Updater((keyProvider(newItem), args.ListChangedType, null, null));
                        }
                        break;
                    case ListChangedType.ItemDeleted:
                        Updater((keyProvider(oldItem), args.ListChangedType, null, null));
                        break;
                    case ListChangedType.Reset:
                        Updater((default(TKey), args.ListChangedType, null, null));
                        break;
                    default:
                        throw new ArgumentException("Unhandled/unimplemented/unsupported change type: " + args.ListChangedType.ToString());
                }
            };
            sourceList.ListChanged += listChangedHandler;
            return getAll;
        }

    }
}
