using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze
{
    internal class AnalyzerForInput : AnalyzerBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        public AnalyzerForInput()
        {

        }
        public override Priority ExecutePriority { get => base.ExecutePriority; set => base.ExecutePriority = value; }

        public override string[] SupportGuiTypes => base.SupportGuiTypes;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
        public override bool TryExecuteAnalyzer(JObject aSelectorJO,
            ActionParamModel anActionParam,
            ElementCacheProvider anElementCacheProvider,
            out List<ExecuteAction> recommendActionExecutions,
            out Dictionary<string, string> recommendActionExecutionParams)
        {
            return TryExecuteAnalyzer(anActionParam,
                out recommendActionExecutions,
                out recommendActionExecutionParams);
        }
        public override bool TryExecuteAnalyzer(
            ActionParamModel actionParamModel,
            out List<ExecuteAction> recommendActionExecutions,
            out Dictionary<string, string> recommendActionExecutionParams)
        {
            recommendActionExecutions = null;
            recommendActionExecutionParams = null;
            if (!string.IsNullOrEmpty(actionParamModel.Attributes) && !string.IsNullOrEmpty(actionParamModel.ControlType))
            {
                actionParamModel.ControlType = $"{actionParamModel.ControlType}/{actionParamModel.Attributes}";
            }
            var elementTag = WinAutomation.CommonUtility.TargrtXmlHelper.CreateInstance().GetTranslatedNameByCode(actionParamModel.ControlType);
            if (elementTag == "文本框_Edit" || elementTag == "输入框" || elementTag == "文本框")
            {
                recommendActionExecutionParams = new Dictionary<string, string>();
                string edit_text = null;
                try
                {
                    edit_text = actionParamModel.CurrentGuiNode?.GetText();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
                if (!string.IsNullOrEmpty(edit_text))
                {
                    recommendActionExecutionParams.Add("args0.text", $"\"{edit_text}\"");
                }
                else
                {
                    recommendActionExecutionParams.Add("args0.text", $"\"\"");
                }
                if (LocalDataModels.SpecialElementActions.TryGetValue("native.input",
                    out List<ElementAction> actions))
                {
                    recommendActionExecutions = actions.First().Actions;
                }
                else
                {
                    logger.LogError("can not get native.input");
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
