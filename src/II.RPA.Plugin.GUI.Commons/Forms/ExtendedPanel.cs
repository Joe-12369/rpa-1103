using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Plugin.GUI.Commons
{
    /// <summary>
    /// 设置可穿透的Panel，以便标记框上的鼠标能选择下方窗体
    /// </summary>
    public class ExtendedPanel : Panel
    {
        private const int WM_NCHITTEST = 0x84;
        private const int HTTRANSPARENT = -1;
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == (int)WM_NCHITTEST)
                message.Result = (IntPtr)HTTRANSPARENT;
            else
                base.WndProc(ref message);
        }
    }
}
