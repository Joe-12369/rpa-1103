using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Common
{
    public class ObjectRef<T>
    {
        public T Object { get; set; }

        public T Get(Func<T> factory = null)
        {
            if (factory == null)
            {
                factory = () => Activator.CreateInstance<T>();
            }

            lock (this)
            {

                return Object = Object != null ? Object : factory();
            }
        }

        public T Update(Func<T> factory)
        {
            lock (this)
            {
                return Object = factory();
            }
        }
    }
}
