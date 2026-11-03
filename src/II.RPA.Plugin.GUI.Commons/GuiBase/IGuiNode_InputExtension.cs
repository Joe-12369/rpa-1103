using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiNode_InputExtension : IGuiNode
    {
        /// <summary>
        /// 文本输入
        /// </summary>
        /// <param name="text"></param>
        /// <param name="isReplace"></param>
        /// <param name="inputType">输入类型</param>
        void SetText(string text, int isReplace, int inputType, int interval); //, int interval
    }
}
