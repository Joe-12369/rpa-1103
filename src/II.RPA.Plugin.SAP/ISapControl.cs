using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.SAP
{
    interface ISapControl
    {
        Rectangle GetRectangle(ApplicationContext context);
        void Execute(ApplicationContext context);

        string GetText(ApplicationContext context);

        void SetText(ApplicationContext context, string txt);

        
    }
   public class ApplicationContext
    {
        public GuiSession session;
        public string path;
        public string text;
        public bool opened;
        public GuiComponent component; //对于之前已经转换过一次的，就不需要重复查找。
        public string ext; //主要用于sap gui tree 中节点信息的传递  由nodeText/nodeText/…… 组成
        public string subId;   //对树控件而言，需要额外的信息来定位树列表的列等；
        public string inspectId;  //拾取的时候辅助标签
        
    }
}
