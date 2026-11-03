using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.HighLight
{

    /// <summary>
    /// 元素高亮参数
    /// </summary>
    public class HighLightParam
    {
        public Rectangle m_Rectangle = new Rectangle();//元素高亮框体
        public string m_TagContent = string.Empty;//元素标签名称
        public ElementType m_ElementType = ElementType.None;//元素类型
        public eGuiType m_eGuiType = eGuiType.None;//GUI元素类型（决定标签logo）
        public bool UseTightBorder = false;//是否使用贴边高亮框，不额外外扩边距

        private string _iconString = string.Empty;
        public string m_IconString
        {
            get
            {
                if (string.IsNullOrEmpty(_iconString))
                {
                    _iconString = HighLightIconTraitor.GetIconString(m_ElementType, m_eGuiType);
                    return _iconString;
                }
                return _iconString;
            }

            set
            {
                _iconString = value;
            }
        }

        public void InitIconString()
        {
            m_IconString = HighLightIconTraitor.GetIconString(m_ElementType, m_eGuiType);
        }

        public HighLightParam(Rectangle _Rectangle, string _TagContent, ElementType _ElementType, eGuiType _eGuiType)
        {
            m_Rectangle = _Rectangle;
            m_TagContent = _TagContent;
            m_ElementType = _ElementType;
            m_eGuiType = _eGuiType;
            InitIconString();
        }

        public HighLightParam(Rectangle _Rectangle, ElementType _ElementType)
        {
            m_Rectangle = _Rectangle;
            m_ElementType = _ElementType;
        }

        public HighLightParam(Rectangle _Rectangle, string _TagContent, string _IconString)
        {
            m_Rectangle = _Rectangle;
            m_TagContent = _TagContent;
            m_IconString = _IconString;
        }

        public HighLightParam() { }

        //public HighlightPolicy CHighlighterPolicy = HighlightPolicy.ProcessEdit;//不清楚
        //public bool IsExeplorer = false;//是否是资源管理器（判断是否为windows系统类型）
        //public int DurationInMs = 0;//不清楚
        //public HighLighterFlashParam CHighLighterFlashParam = new HighLighterFlashParam();

        public bool Equal(HighLightParam _HighLightParam)
        {
            if (this.m_Rectangle.Equals(_HighLightParam.m_Rectangle)
                && this.m_TagContent.Equals(_HighLightParam.m_TagContent)
                && this.m_ElementType.Equals(_HighLightParam.m_ElementType)
                && this.m_eGuiType.Equals(_HighLightParam.m_eGuiType)
                && this.UseTightBorder.Equals(_HighLightParam.UseTightBorder)
                && this.m_IconString.Equals(_HighLightParam.m_IconString)
                )
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 高亮闪烁参数
    /// </summary>
    public class HighLightFlashParam
    {
        public bool IsFlashEnable = false;//是否闪烁
        public int FlashTimes = 3;//闪烁册数
        public int FlashInterval = 150;//闪烁间隔（毫秒）
        public string BorderBrushCode = HighLightIconTraitor.NATIVE_BORDER_COLOR;
    }

    public class HighLightIconTraitor
    {
        public const string NATIVE_BORDER_COLOR = "#FE5402";
        public const string AIREPAIRE_BORDER_COLOR = "#5570EC";
        private const string EXEPLORE_ICON = "\ue72d";
        public const string WINDOWS_ICON = "\ue722";
        private const string WEB_ICON = "\ue72e";
        private const string CV_ICON = "\ue72c";
        private const string INSTEAD_ICON = "\ue6ed";

        public static string GetIconString(ElementType _ElementType, eGuiType _eGuiType)
        {
            string IconString = WINDOWS_ICON;

            if (_ElementType == ElementType.None || _eGuiType == eGuiType.None)
            {
                return IconString;// string.Empty;
            }

            if (_ElementType == ElementType.Ordinary)
            {
                switch (_eGuiType)
                {
                    case eGuiType.Windows:
                    case eGuiType.Java:
                    case eGuiType.SAP:
                    case eGuiType.WdtErp:
                        IconString = WINDOWS_ICON;
                        break;
                    case eGuiType.IE:
                    case eGuiType.Edge:
                    case eGuiType.Chrome:
                    case eGuiType.FireFox:
                    case eGuiType.Sogou:
                    case eGuiType.Web360se:
                    case eGuiType.ZExplorer:
                    case eGuiType.Web360X:
                    case eGuiType.SuperBrowser:
                    case eGuiType.Chromium:
                        IconString = WEB_ICON;
                        break;
                    default:
                        IconString = WINDOWS_ICON;
                        break;
                }
            }
            else
            {
                IconString = CV_ICON;
            }

            return IconString;
        }
    }
}
