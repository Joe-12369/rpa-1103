using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public class ParamGetChartDataModel
    {
        public bool isRemoveWspaceChar { get; set; }
        public bool isPinChartElement { get; set; }
        public string parentElementId { get; set; }
        public int moveDirection { get; set; }
        public int moveSpeed { get; set; }
        public int moveModel { get; set; }
        public float offset { get; set; }

        //以下饼图使用
        public float radiusRatio { get; set; }
        public float horOffset { get; set; }
        public float verOffset { get; set; }
    }
}
