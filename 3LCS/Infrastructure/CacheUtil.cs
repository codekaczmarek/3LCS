using System;
using System.Runtime.Caching;

namespace ThreeLCS.Infrastructure
{
    public static class CacheUtil
    {
        public static void Add(string key, object value)
        {
            if (value == null) return;
            MemoryCache.Default.Add(key, value, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddDays(30) });
        }

        public static T? Get<T>(string key) where T : class =>
            MemoryCache.Default.Get(key) as T;
    }
}
