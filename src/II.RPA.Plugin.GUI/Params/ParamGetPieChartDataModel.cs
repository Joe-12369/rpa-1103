using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetPieChartDataModel : ParamBase
    {
        public bool isRemoveWspaceChar { get; set; }
        public bool isPinChartElement { get; set; }
        public float radiusRatio { get; set; }
        public float horOffset { get; set; }
        public float verOffset { get; set; }
        public MoveSpeed moveSpeed { get; set; }
    }
}
