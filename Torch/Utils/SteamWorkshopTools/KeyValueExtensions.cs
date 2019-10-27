using System;
using System.ComponentModel;
using SteamKit2;

namespace Torch.Utils.SteamWorkshopTools
{
    public static class KeyValueExtensions
    {
        public static T GetValueOrDefault<T>(this KeyValue kv, string key)
        {
            kv.TryGetValueOrDefault<T>(key, out T result);
            return result;
        }
        public static bool TryGetValueOrDefault<T>(this KeyValue kv, string key, out T typedValue)
        {
            var match = kv.Children?.Find(item => item.Name == key);
            object result = default(T);
            if (match == null)
            {
                typedValue = (T) result;
                return false;
            }

            var value = match.Value ?? "";

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                result = converter.ConvertFromString(value);
                typedValue = (T)result;
                return true;
            }
            catch (NotSupportedException)
            {
                throw new Exception($"Unexpected Type '{typeof(T)}'!");
            }
        }
    }
}
