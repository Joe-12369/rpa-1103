using System;
using II.RPA.Plugin.GUI.Commons.Params;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace II.RPA.Plugin.GUI.Commons
{
    /// <summary>
    /// 后续拓展功能，不需要新增接口类，直接在此接口类中新增行为
    /// </summary>
    public interface IGuiNode_6_6_0 : IGuiNode_6_5_0
    {
        /// <summary>
        /// 元素所在进程ID 用来判断元素是否为系统资源元素
        /// </summary>
        int ProcessID { get; set; }
        string GetSelector(bool innerTextFirst);
        IGuiNode[] QueryTargetSelectorAll(ParamQueryTargetSelectorAllModel param);
        string GetElementAttribute_V1(string name, bool isIntact, string apiVersion);

        List<string> GetSelectorAll(List<IGuiNode> guiNodes, string sourceSelector);
        List<Rectangle> GetRegionAll(List<IGuiNode> guiNodes);
        List<string> GetTextAll(List<IGuiNode> guiNodes);
        List<string> GetHtmlAll(List<IGuiNode> guiNodes);
        List<string> GetAttributeAll(List<IGuiNode> guiNodes, string attributeName, bool isIntact, string apiVersion);

    }
}
