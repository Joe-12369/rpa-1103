using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiNode_GetElementSelectExtension: IGuiNode
    {
        /// <summary>
        /// 获取下拉选项
        /// </summary>
        /// <param name="selectMode">0：获取当前 1: 获取全部</param>
        /// <param name="selectType"> 0：按索引（按顺序）1：按文本 2: 按 value (针对浏览器)</param>
        /// <returns></returns>
        string GetElementSelectEx(string strJson);
    }
}
