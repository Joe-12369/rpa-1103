using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Common
{
    public static class Extensions
    {
        public static IObservable<TValue> Watch<TSource, TValue>(this TSource source, Expression<Func<TSource, TValue>> property, bool withInit = true) where TSource : INotifyPropertyChanged
        {
            if (!(property.Body is MemberExpression memberExpression))
            {
                throw new ArgumentException("仅支持直接的成员访问".I18n("controlType_Extensions_C2CD7DDA"));
            }
            string propertyName = memberExpression.Member.Name;
            Func<TSource, TValue> accessor = property.Compile();
            IObservable<TValue> result = from x in Observable.FromEventPattern<PropertyChangedEventArgs>(source, "PropertyChanged")
                                         where x.EventArgs.PropertyName == propertyName
                                         select accessor(source);
            return withInit ? result.StartWith(accessor(source)) : result;
        }

        public static IObservable<KeyValuePair<string, object>> Watch<TSource>(this TSource source, params Expression<Func<TSource, object>>[] properties) where TSource : INotifyPropertyChanged
        {
            List<string> names = new List<string>();
            foreach (Expression<Func<TSource, object>> property in properties)
            {
                if (!(property.Body is MemberExpression memberExpression))
                {
                    throw new ArgumentException("仅支持直接的成员访问".I18n("controlType_Extensions_C2CD7DDA"));
                }
                names.Add(memberExpression.Member.Name);
            }
            return from x in (from x in Observable.FromEventPattern<PropertyChangedEventArgs>(source, "PropertyChanged")
                              select x.EventArgs?.PropertyName into x
                              where !string.IsNullOrEmpty(x)
                              select x).Where((string x) =>
                              {
                                  int result;
                                  if (!names.Contains(x))
                                  {
                                      List<string> list = names;
                                      result = ((list == null || !list.Any()) ? 1 : 0);
                                  }
                                  else
                                  {
                                      result = 1;
                                  }
                                  return (byte)result != 0;
                              })
                   select new KeyValuePair<string, object>(x, source.ReadProperty(x));
        }
        public static object ReadProperty(this object source, string name)
        {
            var propOrField = source.GetType().GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public);

            if (propOrField == null)
            {
                throw new InvalidOperationException(Localize($"{name}未在{source.GetType()}上定义", "controlType_Extensions_C7A23AFF"));
            }

            return propOrField.GetValue(source);
        }

        public static IObservable<IList<T>> Watch<T>(this INotifyCollectionChanged source, bool withInit = false)
        {
            IObservable<List<T>> result = from x in Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>((Action<NotifyCollectionChangedEventArgs> hander) => (object s, NotifyCollectionChangedEventArgs e) =>
            {
                hander(e);
            }, delegate (NotifyCollectionChangedEventHandler cHnalder)
            {
                source.CollectionChanged += cHnalder;
            }, delegate (NotifyCollectionChangedEventHandler cHnalder)
            {
                source.CollectionChanged -= cHnalder;
            })
                                          select (source as IEnumerable<T>)?.ToList() ?? new List<T>();
            return withInit ? result.StartWith((source as IEnumerable<T>)?.ToList() ?? new List<T>()) : result;
        }

        public static IObservable<IList<T>> WatchAdd<T>(this INotifyCollectionChanged source, bool withInit = false)
        {
            IObservable<List<T>> result = from x in Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>((Action<NotifyCollectionChangedEventArgs> hander) => (object s, NotifyCollectionChangedEventArgs e) =>
            {
                hander(e);
            }, delegate (NotifyCollectionChangedEventHandler cHnalder)
            {
                source.CollectionChanged += cHnalder;
            }, delegate (NotifyCollectionChangedEventHandler cHnalder)
            {
                source.CollectionChanged -= cHnalder;
            })
                                          where x.Action == NotifyCollectionChangedAction.Add
                                          select (source as IEnumerable<T>)?.ToList() ?? new List<T>();
            return withInit ? result.StartWith((source as IEnumerable<T>)?.ToList() ?? new List<T>()) : result;
        }

        public static IObservable<T> WatchRemove<T>(this IEnumerable<T> source) where T : INotifyCollectionChanged
        {
            return (from x in Observable.FromEventPattern<CollectionChangeEventArgs>(source, "CollectionChanged")
                    where x.EventArgs.Action == CollectionChangeAction.Remove
                    select x.EventArgs.Element).OfType<T>();
        }
    }
}
