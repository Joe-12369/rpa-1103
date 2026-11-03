using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract
{
    internal struct AnalyzeResult
    {
        public List<ExecuteAction> RecommendActionExecutions
        {
            get;
            set;
        }
        public Dictionary<string, string> RecommendActionExecutionParams
        {
            get;
            set;
        }
        public string ElementClass
        {
            get;
            set;
        }
        public bool IsMatchCondition
        {
            get;
            set;
        }
    }
}
