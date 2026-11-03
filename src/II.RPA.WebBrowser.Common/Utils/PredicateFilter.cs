using II.RPA.WebBrowser.Common.Basic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Utils
{
    public class PredicateFilter
    {
        public static PredicateFilter<T> NewFilter<T>(Predicate<T> predicate)
        {
            return new PredicateFilter<T>(predicate);
        }
    }
    public class PredicateFilter<T>
    {
        private LinkedList<Predicate<T>> filterList = null;
        public PredicateFilter()
        {
            filterList = new LinkedList<Predicate<T>>();
        }
        public PredicateFilter(Predicate<T> predicate) : this()
        {
            AddLast(predicate);
        }
        public void AddLast(Predicate<T> predicate)
        {
            filterList.AddLast(predicate);
        }
        public void RemoveLast()
        {
            filterList.RemoveLast();
        }
        public void Remove(Predicate<T> predicate)
        {
            filterList.Remove(predicate);
        }
        public bool Run(T objT)
        {
            foreach (var filter in filterList)
            {
                if (filter != null && !filter.Invoke(objT))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
