using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamIsSimilarElementExistModel : ParamBase
    {
        public bool isHighlighting { get; set; } = false;
        public string cvExecutionServiceURL { get; set; } = "";
        public int timeout { get; set; } = 10000;
        public ElementRelativeType RelativeType { get; set; } = ElementRelativeType.Similar;
        public int UseAdvanced { get; set; } = 10000;
        public string Num { get; set; } = string.Empty;

    }
}
