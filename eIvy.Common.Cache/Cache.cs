using System;
using System.Collections.Generic;

namespace eIvy.Common
{
    /// <summary>
    /// 一个实现简单的内存缓存器。
    /// </summary>
    public sealed class Cache
    {
        private Cache() { }

        private static Cache _cache;
        /// <summary>
        /// 获得缓存器的实例对象。
        /// </summary>
        public static Cache Instance
        {
            get
            {
                if (_cache == null)
                {
                    _cache = new Cache();

                    _cachedObjects = new Dictionary<string, CachedObject>();
                }
                return _cache;
            }
        }

        private class CachedObject
        {
            public object CachingObject
            {
                get;
                set;
            }

            public CacheExpiredPolicy ExpiredPolicy
            {
                get;
                set;
            }
        }

        private static Dictionary<string, CachedObject> _cachedObjects;

        /// <summary>
        /// 将对象放入缓存区。
        /// </summary>
        /// <param name="key">用于之后检索出缓存对象的键名，必需是全局唯一的。</param>
        /// <param name="cachingObject">待放入缓存区的对象。</param>
        /// <param name="expiredPolicy">缓存到期策略。</param>
        public void Put(string key, object cachingObject, CacheExpiredPolicy expiredPolicy)
        {
            _cachedObjects[key] = new CachedObject()
            {
                CachingObject = cachingObject,
                ExpiredPolicy = expiredPolicy
            };
        }
        /// <summary>
        /// 将对象放入缓存区。
        /// </summary>
        /// <param name="key">用于之后检索出缓存对象的键名，必需是全局唯一的。</param>
        /// <param name="cachingObject">待放入缓存区的对象。</param>
        public void Put(string key, object cachingObject)
        {
            this.Put(key, cachingObject, new CacheExpiredTimeoutPolicy());
        }
        /// <summary>
        /// 从缓存区中获取指定键名的缓存对象。
        /// </summary>
        /// <param name="key">键名。</param>
        /// <returns>缓存对象，如果未检索到缓存对象，或者到期了则返回 null。</returns>
        public object Get(string key)
        {
            object v = null;

            if (_cachedObjects.ContainsKey(key))
            {
                var co = _cachedObjects[key];

                if (co.ExpiredPolicy.IsExpired())
                {
                    _cachedObjects.Remove(key);
                }
                else
                {
                    v = co.CachingObject;
                }
            }

            return v;
        }

        /// <summary>
        /// 从缓存区移除缓存对象。
        /// </summary>
        /// <param name="key">缓存对象在缓存区的键名。</param>
        public void Remove(string key)
        {
            _cachedObjects.Remove(key);
        }

        /// <summary>
        /// 从缓存区获得需要的数据，如果没有则通过 func 获得需要缓存的数据，并将其放入缓存区。
        /// </summary>
        /// <typeparam name="T">需要缓存数据的类型。</typeparam>
        /// <param name="key">用于在缓存区寻找缓存对象的唯一键名。</param>
        /// <param name="func">用于获取需要缓存的数据。</param>
        /// <returns>缓存的数据。</returns>
        public T GetValue<T>(string key, Func<T> func)
        {
            return GetValue<T>(key, func, new CacheExpiredTimeoutPolicy());
        }

        /// <summary>
        /// 从缓存区获得需要的数据，如果没有则通过 func 获得需要缓存的数据，并将其放入缓存区。
        /// </summary>
        /// <typeparam name="T">需要缓存数据的类型。</typeparam>
        /// <param name="key">用于在缓存区寻找缓存对象的唯一键名。</param>
        /// <param name="func">用于获取需要缓存的数据。</param>
        /// <param name="expiredPolicy">缓存到期策略。</param>
        /// <returns>缓存的数据。</returns>
        /// <remarks>
        /// 通过该方法可以省掉获取与缓存数据的逻辑代码，只需要指定获取需要缓存数据的逻辑。
        /// <example>
        /// 例如：下面的例子中，将 1 + 2 算的整数值放入缓存区，并从缓存区返回该计算的整数值。
        /// 这样不用每次去计算 1 + 2 这个表达式，直到缓存到期策略判断缓存到期，才重新去计算并存入缓存区。
        /// <code>
        /// int x = Cache.Instance.GetValue&lt;int&gt;("somekey", () =&gt; {
        ///     return 1 + 2;
        /// });
        /// </code>
        /// </example>
        /// </remarks>
        public T GetValue<T>(string key, Func<T> func, CacheExpiredPolicy expiredPolicy)
        {
            var cv = this.Get(key);

            if (cv != null) return (T)cv;

            var v = func();

            this.Put(key, v, expiredPolicy);

            return v;
        }
    }

    /// <summary>
    /// 缓存到期策略。
    /// </summary>
    public abstract class CacheExpiredPolicy
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CacheExpiredPolicy()
        {

        }

        /// <summary>
        /// 检测指定键的缓存对象是否到期了。
        /// </summary>
        /// <returns>如果到期返回 true，否则返回 false。</returns>
        public abstract bool IsExpired();
    }

    /// <summary>
    /// 以时间到期方式处理缓存到期。
    /// </summary>
    public class CacheExpiredTimeoutPolicy : CacheExpiredPolicy
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CacheExpiredTimeoutPolicy()
        {
            this.CachedTime = DateTime.Now;

            this.Timeout = 30 * 60;
        }
        /// <summary>
        /// 获得缓存对象放入缓存的时间。
        /// </summary>
        public DateTime CachedTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取与设置过期时间，以秒为单位，默认为 30 分钟。
        /// </summary>
        public int Timeout
        {
            get;
            set;
        }

        /// <summary>
        /// 检测指定键的缓存对象是否到期了。
        /// </summary>
        /// <returns>如果到期返回 true，否则返回 false。</returns>
        public override bool IsExpired()
        {
            return (this.CachedTime.AddSeconds(this.Timeout) < DateTime.Now);
        }
    }

    /// <summary>
    /// 比较值缓存到期策略。
    /// </summary>
    /// <remarks>
    /// 通过比较新值与旧值来判定缓存对象是否过期。
    /// <example>
    /// 例如：下面的例子中 1 + 2 的表达式只在 i 被 5 整除的情况下重新计算，如果包括首次缓存的话，共执行 3 次。
    /// <code>
    /// string key = "somekey";
    /// int s = 1, i = 1;
    /// while(i &lt;= 10)
    /// {
    ///     var p = CacheCompareValueExpiredPolicy.GetPolicy(key, s).SetNewValue(s);
    ///     var x = Cache.Instance.GetValue&lt;int&gt;(key, () =&gt;
    ///     {
    ///         return 1 + 2;
    ///     }, p);
    ///     if(i % 5 == 0)
    ///     {
    ///         s = i;
    ///     }
    ///     i++;
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public class CacheCompareValueExpiredPolicy : CacheExpiredPolicy
    {
        private CacheCompareValueExpiredPolicy(string key, object compareValue)
        {
            this.Key = key;

            _compareValueKey = string.Format("{0}_compareValue", key);

            this.SourceValue = Cache.Instance.GetValue<object>(_compareValueKey, () =>
            {
                return compareValue;
            });
        }

        private string _compareValueKey;

        /// <summary>
        /// 获得依赖此缓存过期策略的缓存项的键名。
        /// </summary>
        public string Key
        {
            get;
            private set;
        }
        /// <summary>
        /// 获得需要比较的原始值。
        /// </summary>
        public object SourceValue
        {
            get;
            private set;
        }
        /// <summary>
        /// 获取与设置需要比较的新值。
        /// </summary>
        public object NewValue
        {
            get;
            set;
        }
        /// <summary>
        /// 设置需要比较的新值。
        /// </summary>
        /// <param name="newValue">需要比较的新值。</param>
        /// <returns>当前缓存策略。</returns>
        public CacheCompareValueExpiredPolicy SetNewValue(object newValue)
        {
            this.NewValue = newValue;
            return this;
        }
        /// <summary>
        /// 如果新值与旧值不相同的话，则判定为过期，否则判定不过期。
        /// </summary>
        /// <returns>如果新值与旧值不相同的话，则判定为过期，否则判定不过期。</returns>
        public override bool IsExpired()
        {
            var v = this.SourceValue;

            var nv = this.NewValue;

            bool r = (v != nv);

            if (r)
            {
                Cache.Instance.Remove(_compareValueKey);

                this.SourceValue = Cache.Instance.GetValue<object>(_compareValueKey, () =>
                {
                    return nv;
                });
            }

            return r;
        }
        /// <summary>
        /// 获得一个缓存策略对象。
        /// </summary>
        /// <param name="key">依赖于此缓存策略的缓存对象的键名。</param>
        /// <param name="compareValue">需要比较的数值。</param>
        /// <returns>缓存策略对象。</returns>
        public static CacheCompareValueExpiredPolicy GetPolicy(string key, object compareValue)
        {
            var v = Cache.Instance.GetValue<CacheCompareValueExpiredPolicy>(key + typeof(CacheCompareValueExpiredPolicy).Name, () =>
            {
                return new CacheCompareValueExpiredPolicy(key, compareValue);
            });

            return v;
        }
    }
}