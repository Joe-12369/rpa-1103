using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract
{
    public interface IAnalyzer
    {
        string AnalyzerName
        {
            get;
        }
        Type AnalyzerType
        {
            get;
        }
        Priority ExecutePriority
        {
            get;
            set;
        }
        bool IsSupportCv
        {
            get;
        }
        string[] SupportGuiTypes
        {
            get;
        }
        bool TryExecuteAnalyzer(
            JObject aSelectorJO,
            ActionParamModel anActionParam,
            ElementCacheProvider anElementCacheProvider,
            out List<ExecuteAction> recommendActionExecutions, 
            out Dictionary<string, string> recommendActionExecutionParams);


        bool TryExecuteAnalyzer(
           JObject aSelectorJO,
           ActionParamModel anActionParam,
           IEnumerable<string> aCurrentComponents,
           out List<ExecuteAction> recommendActionExecutions);

        bool TryExecuteAnalyzer(
            JObject aSelectorJO,
            ActionParamModel anActionParam,
            out List<ExecuteAction> recommendActionExecutions,
            out Dictionary<string, string> recommendActionExecutionParams,
            out string anElementClass);

        bool TryExecuteAnalyzerAsAddition(
            ActionParamModel anActionParam, 
            out List<ExecuteAction> recommendActionExecutions,
            out Dictionary<string, string> recommendActionExecutionParams,
            out string anElementClass,
            out bool anIsMatchCondition);

        bool TryExecuteAnalyzer(
            ActionParamModel anActionParam, 
            out List<ExecuteAction> recommendActionExecutions,
            out Dictionary<string, string> recommendActionExecutionParams);
    }
    /// <summary>
    /// 无需selector的分析器接口、明天重构
    /// </summary>
    public interface ISlimAnalyzer
    {
        string AnalyzerName
        {
            get;
        }
        Priority ExecutePriority
        {
            get;
            set;
        }
        bool IsSupportCv
        {
            get;
        }
        string[] SupportGuiTypes
        {
            get;
        }
        bool TryExecuteAnalyzer(ActionParamModel anAactionParamModel,
            out List<ExecuteAction> recommendActionExecutions,
            out Dictionary<string, string> recommendActionExecutionParams);
    }
    public enum Priority
    {
        //默认优先级、相同优先级会按照注册的顺序执行
        Default = 2,
        //优先级高于default
        High = 1,
        //优先级低于default
        Low = 3,
        //优先级最高、只能有一个、设置多个Highest时、只有第一个生效、后续的会被降级为High
        Highest = 0,
        //优先级最低、只能有一个、设置多个Lowest时、只有第一个生效、后续的会被升级为Low
        Lowest = 9
    }
}
