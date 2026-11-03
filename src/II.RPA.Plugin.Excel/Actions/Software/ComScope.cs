using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace II.RPA.Plugin.Excel.Actions.Software
{
    internal class ComScope : IDisposable
    {
        private List<object> _objects = new List<object>();

        public object Track(object obj)
        {
            if (obj != null && Marshal.IsComObject(obj))
            {
                _objects.Add(obj);
            }
            return obj;
        }

        public void Dispose()
        {
            foreach (var obj in _objects)
            {
                try
                {
                    if (obj != null && Marshal.IsComObject(obj))
                    {
                        Marshal.ReleaseComObject(obj);
                    }
                }
                catch { }
            }
            _objects.Clear();
        }
    }
}
