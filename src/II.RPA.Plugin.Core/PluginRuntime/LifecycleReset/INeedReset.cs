using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.PluginBase
{
    public interface INeedReset
    {
        void Reset(bool isSamePackage);
    }
}
