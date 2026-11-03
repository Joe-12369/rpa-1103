using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze
{
    internal class AnalyzerSlimDefault : AnalyzerBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        public AnalyzerSlimDefault()
        {
            
        }
        public override Priority ExecutePriority 
        { 
            get => base.ExecutePriority; 
            set => base.ExecutePriority = value;
        }

        public override bool TryExecuteAnalyzer(
            ActionParamModel actionParamModel,
            out List<ExecuteAction> recommendActionExecutions, 
            out Dictionary<string, string> recommendActionExecutionParams)
        {
            try
            {
                string guitype = actionParamModel.GuiType.ToString().ToLower();
                List<ElementAction> actions;

                if (LocalDataModels.DefaultActions.ContainsKey(guitype))
                {
                    actions = LocalDataModels.DefaultActions[guitype];
                }
                else
                {
                    actions = LocalDataModels.DefaultActions["windows"];
                }

                recommendActionExecutions = actions.First().Actions;
                recommendActionExecutionParams = new Dictionary<string, string>();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                recommendActionExecutions = null;
                recommendActionExecutionParams = null;
                return false;
            }
        }
    }
}
