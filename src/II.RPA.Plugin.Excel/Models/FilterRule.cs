
namespace II.RPA.Plugin.Excel.Models
{
    /// <summary>
    /// 过滤规则
    /// </summary>
    public class FilterRule
    {
        public string Criteria1 { get; set; }
        public string Criteria2 { get; set; }
        public bool On { get; set; }
        public int Operator { get; set; }
        public string Column { get; set; }
    }
    /// <summary>
    /// Excel 自动筛选规则
    /// </summary>
    public class AutoFilterRule
    {
        public dynamic Range { get; set; }
        public dynamic StartColumn { get; set; }
        public dynamic EndColumn { get; set; }
        public int StartRow { get; set; }
        public int EndRow { get; set; }
        public dynamic SortMethod { get; set; }
        public dynamic FilterMode { get; set; }
        // 0: 非表格区域筛选  1：表格区域筛选
        public int InTable { get; set; }
        public ExcelFilters[] Filters { get; set; }

    }

    public class ExcelFilters
    {
        public dynamic Criteria1 { get; set; }
        public dynamic Criteria2 { get; set; }
        public dynamic On { get; set; }
        //https://learn.microsoft.com/en-us/dotnet/api/microsoft.office.interop.excel.xlautofilteroperator?view=excel-pia
        public dynamic Operator { get; set; }
    }
}
