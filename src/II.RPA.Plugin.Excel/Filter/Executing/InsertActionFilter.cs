using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Excel.Utils;
using System.Collections.Generic;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    internal class InsertActionFilter : ActionFilter
    {
        public InsertActionFilter(FilterType filterType, int order = 0) : base(filterType, order)
        {
        }

        public override void OnExecution(ActionContext context)
        {
            if (!context.ActionName.StartsWith("Insert")) return;
            int _count = ExcelUtils.ConvertDynamicToDefaultValue(typeof(int), context["count"]);
            if (context.ActionName.Equals("InsertRow"))
            {
                var rowIndex = ExcelUtils.ConvertNegativeRow(context.ActionParams["index"]);

                if (!ExcelUtils.ValidateRow(rowIndex + _count))
                    throw new ExcelException(Localize($"插入数量已超出 Excel 最大上限", "InsertActionFilter_4C143D3C"));
                context["index"] = rowIndex;

            }
            if (context.ActionName.Equals("InsertColumn"))
            {
                var columnIndex = ExcelUtils.ColumnLetterToIndex(ExcelUtils.ConvertNegativeColumn(context.ActionParams["index"]));

                if (!ExcelUtils.ValidateColumn(column: ExcelUtils.ColumnIndexToLetter(columnIndex + int.Parse(context.ActionParams["count"].ToString()))))
                    throw new ExcelException(Localize($"插入数量已超出 Excel 最大上限", "InsertActionFilter_4C143D3C"));
                context["index"] = columnIndex;
            }
        }
    }
}
