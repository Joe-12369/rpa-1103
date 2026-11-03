using System;
using System.Collections.Generic;
using System.Drawing;
using FlaUI.Core.AutomationElements;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiNode
    {
        //用户自定义ID，提供额外的标识信息，以辅助元素精确定位，主要在java节点使用
        string UserDefinedID { get; }
        //当节点无任何属性信息时，通过当前元素位置Idx(从0开始)来辅助快速定位，比如<Pane />这样的无属性节点
        string Idx { get; }
        string AutomationId { get; }
        string ClassName { get; }
        string ControlType { get; }
        string Name { get; }
        string Role { get; }
        string Description { get; }
        string ProcessName { get; }
        string ProcessFullPath { get; }
        string NodeSelector { get; }
        IntPtr WindowHandle { get; }
        IGuiNode Parent { get; }
        IGuiNode AutomationElementParent { get; }
        Rectangle BoundingRectangle { get; }

        List<IGuiNode> Children { get; }

        bool IsTopLevelWindow { get; }

        void IntoView();
        void MouseConterClick(int offsetX, int offsetY, UiElementClickParams clickParams = null);
        void MouseConterDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null);
        IGuiNode FindRelativeNode(int position, int offsetX, int offsetY);
        List<IGuiNode> FindAll(IGuiNode fromElement = null, int maxResult = 1, int milliseconds = 3000);
        void MouseClick(int offsetX, int offsetY, UiElementClickParams clickParams = null);
        void MouseDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null);
        void MouseRightClick(int offsetX, int offsetY, UiElementClickParams clickParams = null);
        void MouseRightDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null);
        void MouseHover(UiElementHoverParams hoverParams = null);
        void Focus();
        void SetForeground();
        Point GetClickablePoint();
        IGuiNode GetChildByIdx(int idx);
        //0 清空输入 1 追加输入
        void SetText(string text, int isReplace, bool isSimulate);
        string GetText();
        string GetTableData(string input);
        string ClickTableCell(string input);
        void SetElementAttribute(string name, string value);
        string GetElementAttribute(string name, string apiVersion);
        /// <summary>
        ///  获取下拉选项
        /// </summary>
        /// <param name="selectType">1：按文本 2: 按 value (针对浏览器)</param>
        /// <returns></returns>
        string GetElementSelect(int selectType = 1);
        ///// <summary>
        ///// 获取下拉选项
        ///// </summary>
        ///// <param name="selectMode">0：获取当前 1: 获取全部</param>
        ///// <param name="selectType"> 0：按索引（按顺序）1：按文本 2: 按 value (针对浏览器)</param>
        ///// <returns></returns>
        //string GetElementSelectEx(int selectMode =0, int selectType = 1);
        string GetEnableClick();
        /// <summary>
        /// 设置RadioButton选择
        /// </summary>
        /// <param name="selectType">0：按顺序，1：按文本 2: 按 value (针对浏览器)</param>
        /// <returns></returns>
        void SetElementSelect(int selectType, string setValue);
        void SetElementMultipleSelect(int selectType, string setValue);
        void SetElementSelectWithMatchModel(int selectType, int matchModel, string setValue);
        string GetElementCheck();
        /// <summary>
        /// 勾选
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        void SetElementCheck(int check);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="positionType">1 父元素相对位置 2 窗口相对位置 3 绝对位置</param>
        /// <returns></returns>
        Rectangle GetRegion(int positionType);
        string FindElementWithPicPath(string strPicPath);
        string FindRelativeElementsInfo(string strRelativeSelector, ref JArray context);
        Bitmap ElementToBitmap();
    }
}
