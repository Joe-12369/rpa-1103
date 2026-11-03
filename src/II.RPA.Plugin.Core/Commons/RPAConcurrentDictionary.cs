using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core
{
    public class RPAConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    {
		public RPAConcurrentDictionary()
		{
		}

		public RPAConcurrentDictionary(IEqualityComparer<TKey> comparer)
			: base(comparer)
		{

		}
	}
}
