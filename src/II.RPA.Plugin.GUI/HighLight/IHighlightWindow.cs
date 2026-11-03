using II.RPA.Plugin.GUI.Params;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.HighLight
{
    public interface IHighlightWindow
    {
        void SetPosionAndBorderSize(Rectangle CRectangle, bool _ElementExistFlash = false, int delta = 1);
        void Show();
        bool GetBorderRectAndTagRect(HighLightParam _HighLightParam, ref Rectangle BorderRect, ref Rectangle TagRect);
        void SetBorderBorderBrush(ElementType _ElementType);
        void Close();
        void SetBackgroundActive(bool IsActive);
        void SetWndPenetrate(bool IsActive);
        void SetWndState(bool IsActive);
        bool IsActive { get; }
        bool Topmost { get; set; }
        void SetTopMost();
        void SetTagContent(string strTagContent = "");
        void SetTagIconCode(string strIconString);
        void SetToolPanelVisible();
        void StartAILoading();
        void StopAILoading();
        void SetZoomControlsShowStatus(bool isEnbale);
    }
}
