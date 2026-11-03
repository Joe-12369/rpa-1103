using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.Utility
{
    public class InterlockedEnum<T> where T : Enum
    {
        private volatile int value;

        public InterlockedEnum() { }
        public InterlockedEnum(T value)
        {
            this.value = Convert.ToInt32(value);
        }

        private int ToInt(T value) => Convert.ToInt32(value);

        public T Get()
        {
            return (T)Enum.ToObject(typeof(T), value);
        }

        public void Set(T value)
        {
            Interlocked.Exchange(ref this.value, ToInt(value));
        }

        public bool ChangeTo(T from, T to)
        {
            var intFrom = ToInt(from);
            var intTo = ToInt(to);

            if (intFrom == intTo)
            {
                return false;
            }

            return intFrom == Interlocked.CompareExchange(ref this.value, intTo, intFrom);
        }
    }
}
