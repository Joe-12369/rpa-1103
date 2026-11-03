using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.JavaBridgeHost.CompareRule
{
    public interface ICompareRule<T>
    {
       bool Compare(T fromObj,T toObj);
    }
}
