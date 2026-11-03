using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData.Binding;
using Observable = System.Reactive.Linq.Observable;

namespace II.RPA.Observable.Reactives
{
    public static class Extensions
    {
        public static IObservable<TValue> Watch<TSource, TValue>(
                     this TSource source,
                     Expression<Func<TSource, TValue>> property,
                     bool withInit = true)
            where TSource : INotifyPropertyChanged
        {
            if (!(property.Body is MemberExpression memberExpression))
            {
                throw new ArgumentException("仅支持直接的成员访问");
            }

            string propertyName = memberExpression.Member.Name;

            var accessor = property.Compile();

            var result = System.Reactive.Linq.Observable
                .FromEventPattern<PropertyChangedEventArgs>(source, "PropertyChanged")
                .Where(x => x.EventArgs.PropertyName == propertyName)
                .Select(x => accessor(source));

            return withInit ? result.StartWith(accessor(source)) : result;
        }

        public static IObservable<KeyValuePair<string, object>> Watch<TSource>(
                     this TSource source,
                     params Expression<Func<TSource, object>>[] properties)
            where TSource : INotifyPropertyChanged
        {
            var names = new List<string>();

            foreach (var property in properties)
            {
                if (!(property.Body is MemberExpression memberExpression))
                {
                    throw new ArgumentException("仅支持直接的成员访问");
                }

                names.Add(memberExpression.Member.Name);
            }

            return System.Reactive.Linq.Observable
                .FromEventPattern<PropertyChangedEventArgs>(source, "PropertyChanged")
                .Select(x => x.EventArgs?.PropertyName)
                .Where(x => !string.IsNullOrEmpty(x))
                .Where(x => names.Contains(x) || names?.Any() != true)
                .Select(x => new KeyValuePair<string, object>(x, source.ReadProperty(x)));
        }

        public static IObservable<IList<T>> Watch<T>(
                     this INotifyCollectionChanged source,
                     bool withInit = false)
        {
            var result = System.Reactive.Linq.Observable
                .FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    hander => (s, e) => hander(e),
                    (NotifyCollectionChangedEventHandler cHnalder) => source.CollectionChanged += cHnalder,
                    (NotifyCollectionChangedEventHandler cHnalder) => source.CollectionChanged -= cHnalder)
                .Select(x => (source as IEnumerable<T>)?.ToList() ?? new List<T>());

            return withInit ?
                result.StartWith((source as IEnumerable<T>)?.ToList() ?? new List<T>()) :
                result;
        }

        public static IObservable<IList<T>> WatchAdd<T>(
                     this INotifyCollectionChanged source,
                     bool withInit = false)
        {
            var result = System.Reactive.Linq.Observable
                .FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    hander => (s, e) => hander(e),
                    (NotifyCollectionChangedEventHandler cHnalder) => source.CollectionChanged += cHnalder,
                    (NotifyCollectionChangedEventHandler cHnalder) => source.CollectionChanged -= cHnalder)
                .Where(x => x.Action == NotifyCollectionChangedAction.Add)
                .Select(x => (source as IEnumerable<T>)?.ToList() ?? new List<T>());

            return withInit ?
                result.StartWith((source as IEnumerable<T>)?.ToList() ?? new List<T>()) :
                result;
        }

        public static IObservable<T> WatchRemove<T>(
                     this IEnumerable<T> source)
            where T : INotifyCollectionChanged
        {
            return System.Reactive.Linq.Observable
                .FromEventPattern<CollectionChangeEventArgs>(source, "CollectionChanged")
                .Where(x => x.EventArgs.Action == CollectionChangeAction.Remove)
                .Select(x => x.EventArgs.Element)
                .OfType<T>();
        }
    }
}
