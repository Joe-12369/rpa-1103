using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows.CompareRule
{
    public interface ICompareRule<T>
    {
       bool Compare(T fromObj,T toObj);
    }
}
