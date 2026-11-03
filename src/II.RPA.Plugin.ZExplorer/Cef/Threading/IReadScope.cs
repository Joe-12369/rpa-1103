using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.ZExplorer.Cef.Threading
{
    public interface IReadScope<T> : IDisposable
    {
        T Value { get; }
    }
}
