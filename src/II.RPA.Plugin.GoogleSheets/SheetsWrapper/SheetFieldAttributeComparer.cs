using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.SheetsWrapper
{
    public class SheetFieldAttributeComparer : IComparer<SheetFieldAttribute>
    {
        public int Compare(SheetFieldAttribute x, SheetFieldAttribute y)
        {
            if ((x != null) && (y != null))
            {
                return x.ColumnID.CompareTo(y.ColumnID);
            }

            return 0;
        }
    }
}
