using II.RPA.Plugin.Excel.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    /// <summary>
    /// 转换负的列名索引，行号
    /// </summary>
    internal class NegativeColumnRowFilter : ActionFilter
    {
        public NegativeColumnRowFilter(FilterType filterType, int order = 0) : base(filterType, order) { }
        public override void OnExecution(ActionContext context)
        {
            if ("ShowTableStyle".Contains(context.ActionName))
            {
                return;
            }
            if (context.ActionParams.Keys.Any(k => k.IndexOf("row", StringComparison.OrdinalIgnoreCase) >= 0 || k.IndexOf("column", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                //行列有效性校验、行列支持负数、列名支持索引处理
                Dictionary<string, object> changedParam = new Dictionary<string, object>();

                foreach (var param in context.ActionParams)
                {
                    if (IsRowParam(param.Key))
                    {
                        changedParam[param.Key] = TryGetRangeStartParam(context, param.Key, "row", out var startRow)
                            ? ExcelUtils.ConvertNegativeRangeEndRow(startRow, param.Value)
                            : ExcelUtils.ConvertNegativeRow(param.Value);
                        continue;
                    }
                    if (IsColumnParam(param.Key))
                    {
                        changedParam[param.Key] = TryGetRangeStartParam(context, param.Key, "column", out var startColumn)
                            ? ExcelUtils.ConvertNegativeRangeEndColumn(startColumn, param.Value)
                            : ExcelUtils.ConvertNegativeColumn(param.Value);
                    }
                }

                foreach (var _column in changedParam)
                {
                    context[_column.Key] = _column.Value?.ToString();
                }

            }
        }

        private static bool IsRowParam(string key)
        {
            return key.IndexOf("row", StringComparison.OrdinalIgnoreCase) >= 0
                && key.IndexOf("rows", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool IsColumnParam(string key)
        {
            return key.IndexOf("column", StringComparison.OrdinalIgnoreCase) >= 0
                && key.IndexOf("columns", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool TryGetRangeStartParam(ActionContext context, string key, string axis, out object value)
        {
            value = null;
            var pattern = $"(last|end)({axis})";
            if (!Regex.IsMatch(key, pattern, RegexOptions.IgnoreCase))
            {
                return false;
            }

            var startKey = Regex.Replace(key, pattern, "start$2", RegexOptions.IgnoreCase);
            foreach (var param in context.ActionParams)
            {
                if (param.Key.Equals(startKey, StringComparison.OrdinalIgnoreCase))
                {
                    value = param.Value;
                    return true;
                }
            }

            return false;
        }
    }
}
