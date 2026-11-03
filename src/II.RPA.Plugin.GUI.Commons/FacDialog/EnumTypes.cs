using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.FacDialog
{
    public enum WindowDialogType
    {
        info = 0,
        error = 1,
        success = 2,
        warning = 3
    }
    public enum ButtonType
    {
        WindowClose = 0,
        [ButtonProperty(Text = "取消")]
        UseTableDialog_Cancel = 1,
        [ButtonProperty(Colored = false, Text = "采集单元素")]
        UseTableDialog_CollectElement = 2,
        [ButtonProperty(Colored = true, Text = "采集表格")]
        UseTableDialog_CollectTable = 3
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class ButtonPropertyAttribute : Attribute
    {
        public bool Colored { get; set; } = false;
        public string Text { get; set; } = string.Empty;
    }
}
