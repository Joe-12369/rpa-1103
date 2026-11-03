using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ElementHighlight
{
    /// <summary>
    /// 元素高亮参数
    /// </summary>
    public class ElementHighLighterParam
    {
        public Rectangle CRectangle = new Rectangle();//元素框体
        public eGuiType EGuiType = eGuiType.None;//GUI元素类型（决定标签logo）
        public string CTagName = string.Empty;//元素标签名称
        public ElementType CElementType = ElementType.None;//元素类型
        public HighlightPolicy CHighlighterPolicy = HighlightPolicy.ProcessEdit;//不清楚
        public bool IsExeplorer = false;//是否是资源管理器（判断是否为windows系统类型）
        public int DurationInMs = 0;//不清楚
        public HighLighterFlashParam CHighLighterFlashParam = new HighLighterFlashParam();
    }

    /// <summary>
    /// 高亮闪烁参数
    /// </summary>
    public class HighLighterFlashParam
    {
        public bool IsFlashEnable = false;//是否闪烁
        public int FlashTimes = 3;//闪烁册数
        public int FlashInterval = 150;//闪烁间隔（毫秒）
        public string BorderBrushCode = "#FE5402";
        public bool UseTightBorder = false;//是否使用贴边高亮框，不额外外扩边距
    }
}
