using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Observable
{
    public static class ObjectExtension
    {
        public static TResult Apply<TSource, TResult>(
            this TSource source,
            Func<TSource, TResult> applyer)
        {
            if (source == null)
            {
                return default(TResult);
            }

            return applyer.Invoke(source);
        }

        public static object ApplyGet<TSource>(
            this object source,
            Func<TSource, object> applyer)
        {
            if (source is TSource tObj)
            {
                return Apply(tObj, applyer);
            }

            return default(object);
        }

        public static void Apply<TSource>(
            this object source,
            Action<TSource> applyer)
        {
            if (source is TSource tSource)
            {
                applyer.Invoke(tSource);
            }
        }

        public static TResult ApplyGet<TSource, TResult>(
            this object source,
            Func<TSource, TResult> applyer)
        {
            try
            {
                if (source is TSource tSource)
                {
                    return applyer.Invoke(tSource);
                }
            }
            catch
            {
            }

            return default(TResult);
        }

        public static object ReadProperty(this object source, string name)
        {
            var propOrField = source.GetType().GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public);

            if (propOrField == null)
            {
                throw new InvalidOperationException($"{name}未在{source.GetType()}上定义");
            }

            return propOrField.GetValue(source);
        }

        public static bool TrySetProperty(
            this object source,
            string name,
            Func<object> valueFactory)
        {
            var propOrField = source.GetType().GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public);

            if (propOrField == null)
            {
                return false;
            }

            propOrField.SetValue(source, valueFactory());

            return true;
        }

        public static T ReadProperty<T>(this object source, string name)
        {
            return (T)ReadProperty(source, name);
        }

        public static T Promise<T>(
            this T source,
            string message = null,
            [CallerMemberName] string callerName = null)
        {
            if (source == null)
            {
                message = message == null ? $"成员{callerName}承诺是非空的，但是实际为空" : message;
                throw new Exception(message);
            }

            return source;
        }

        private static T Fallback<T>(
            this T source,
            Func<T> factory)
        {
            return source == null ? factory() : source;
        }

        public static string Safe(
            this string source,
            Func<string> factory = null)
        {
            factory = factory == null ? () => string.Empty : factory;
            return Fallback(source, factory);
        }

        public static T1 Promise<T1>(T1 t1)
        {
            return t1.Promise();
        }

        public static (T1, T2) Promise<T1, T2>(T1 t1, T2 t2)
        {
            return (t1.Promise(), t2.Promise());
        }

        public static (T1, T2, T3) Promise<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
        {
            return (t1.Promise(), t2.Promise(), t3.Promise());
        }
    }
}
