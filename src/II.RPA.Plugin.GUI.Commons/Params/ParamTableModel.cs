using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public class ParamTableModel
    {
        //0：all，1：RowOnly，2：ColumnOnly，3：RowColumn
        public int DataScope { get; set; }
        public string RowNo { get; set; }
        public string ColumnNo { get; set; }
        public int ContainHeader { get; set; }

    }
    public enum TableDataScope
    {
        ALL,
        RowOnly,
        ColumnOnly,
        RowColumn
    }
    public class SapClickModel
    {
        public string RowNo { get; set; }
        public string ColumnNo { get; set; }
        //0:单击，1：双击
        public int ClickType { get; set; }
    }

    public class ScopeRowColumn
    {
        public ScopeRowColumn() { }
        public ScopeRowColumn(int rowNo, int columnNo, Scope rowScope, Scope columnScope)
        {
            RowNo = rowNo;
            ColumnNo = columnNo;
            RowScope = rowScope;
            ColumnScope = columnScope;
        }
        public int RowNo { get; set; }
        public int ColumnNo { get; set; }
        public Scope RowScope { get; set; }
        public Scope ColumnScope { get; set; }

    }
    public class Scope
    {
        public Scope() { }
        public Scope(int startNo, int endNo)
        {
            StartNo = startNo;
            EndNo = endNo;
        }
        public int StartNo { get; set; }
        public int EndNo { get; set; }
    }
}
