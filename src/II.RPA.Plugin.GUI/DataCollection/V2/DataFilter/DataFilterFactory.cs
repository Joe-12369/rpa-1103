using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataFilter
{
    internal class DataFilterFactory
    {
        internal static bool Filter(string content, List<FilterConfig> configs)
        {
            IDataFilter dataFilter = null;
            Dictionary<FilterConfig, IDataFilter> processingDic = new Dictionary<FilterConfig, IDataFilter>();
            var filterRet = true;
            foreach (var config in configs)
            {
                switch (config.Pattern)
                {
                    case PatternType.Equel:
                        dataFilter = EquelFilter.Instance;
                        break;
                    case PatternType.Unequal:
                        dataFilter = UnequalFilter.Instance;
                        break;
                    case PatternType.GreaterThan:
                        dataFilter = GreaterThanFilter.Instance;
                        break;
                    case PatternType.LessThan:
                        dataFilter = LessThanFilter.Instance;
                        break;
                    case PatternType.GreaterEquel:
                        dataFilter = GreaterEquelFilter.Instance;
                        break;
                    case PatternType.LessEquel:
                        dataFilter = LessEquelFilter.Instance;
                        break;
                    case PatternType.Contain:
                        dataFilter = ContainFilter.Instance;
                        break;
                    case PatternType.NotContain:
                        dataFilter = NotContainFilter.Instance;
                        break;
                    case PatternType.IsEmpty:
                        dataFilter = IsEmptyFilter.Instance;
                        break;
                    case PatternType.NotEmpty:
                        dataFilter = NotEmptyFilter.Instance;
                        break;
                    case PatternType.StartWith:
                        dataFilter = StartWithFilter.Instance;
                        break;
                    case PatternType.NotStartWith:
                        dataFilter = NotStartWithFilter.Instance;
                        break;
                    case PatternType.EndWith:
                        dataFilter = EndWithFilter.Instance;
                        break;
                    case PatternType.NotEndWith:
                        dataFilter = NotEndWithFilter.Instance;
                        break;
                    case PatternType.DateBefore:
                        dataFilter = DateBeforeFilter.Instance;
                        break;
                    case PatternType.DateAfter:
                        dataFilter = DateAfterFilter.Instance;
                        break;
                    case PatternType.DateBetween:
                        dataFilter = DateBetweenFilter.Instance;
                        break;
                    case PatternType.RegularTest:
                        dataFilter = RegularTestFilter.Instance;
                        break;
                    case PatternType.None:
                    default:
                        continue;
                }
                filterRet = dataFilter.Execute(content, config);
                if (filterRet == true && config.Logical == DataFilterLogicalType.Or)
                {
                    break;
                }
                if (filterRet == false && config.Logical == DataFilterLogicalType.And)
                {
                    break;
                }
            }
            return filterRet;
        }
    }
}
