using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject
{
    public static class Dictionary_Extensions
    {
        public static TValue GetOrSet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueGetter, bool setIfNull = true)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));
            if (valueGetter == null)
                throw new ArgumentNullException(nameof(valueGetter));
            if (dictionary.TryGetValue(key, out TValue value))
                return value;
            value = valueGetter();
            if (setIfNull || value != null)
                dictionary.Add(key, value);
            return value;
        }
    }
}
