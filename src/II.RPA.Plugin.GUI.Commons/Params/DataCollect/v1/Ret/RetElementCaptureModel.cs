using II.RPA.Plugin.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public class RetElementCaptureModel
    {
        /// <summary>
        /// 拾取结果，0代表拾取成功
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// 拾取操作相关提示信息，比如异常信息等
        /// </summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>
        /// 初始状态的Selector（与普通的元素结构规则相同）
        /// </summary>
        public List<SelectorProps> OriginalSelector { get; set; } = new List<SelectorProps>();
        /// <summary>
        /// 加工处理过的Selector（根据“数据采集元素定位规则”处理之后的Selector）
        /// </summary>
        public List<SelectorProps> ProcessedSelector { get; set; } = new List<SelectorProps>();

        public string PageUrl { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public string GuiType { get; set; } = string.Empty;
        public string ScreenShotPath { get; set; } = string.Empty;

        public List<SelectorProps> NextPageBtnSelector { get; set; } = new List<SelectorProps>();
        public string NextPageBtnScreenShotPath { get; set; } = string.Empty;

        public bool IsTable { get; set; } = false;

    }
}
