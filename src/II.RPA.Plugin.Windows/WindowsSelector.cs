using II.RPA.WinAutomation.CommonUtility;
using FlaUI.Core.AutomationElements;
using System;
using System.Collections.Generic;
using System.Linq;
using II.RPA.Plugin.GUI.Commons;

namespace II.RPA.Plugin.Windows
{
    public class WindowsSelector:Selector
    {
        public WindowsSelector(string json) : base(json) { }

        public WindowsSelector(AutomationElement element, WindowsSelector anchor)
        {
            AutomationElement root = null;
            AutomationElement baseElement = null;
            var pathToRoot = new List<AutomationElement>();
            while(element!=null)
            {
                try
                {
                    //遍历父元素获得整个控件链
                    if(element.Parent!=null)
                    {
                        pathToRoot.Add(element);
                    }
                    else
                    {
                        root = element;
                    }
                }
                catch(Exception)
                {
                    root = element;
                }
                try
                {
                    element = element.Parent;
                }
                catch(Exception ex)
                {
                    NLogger.Default.Error(ex);
                }
            }
            pathToRoot.Reverse();
            if(pathToRoot.Count==0)
            {
                NLogger.Default.Info("Element has no parent");
                return;
            }
            WindowsSelectorItem item; 
            baseElement = pathToRoot.First();
            element = pathToRoot.Last();
            Clear();
            if(anchor==null)
            {
                item = new WindowsSelectorItem(baseElement, true);   //根元素
                item.Enabled = true;
                this.Add(item);
            }
            bool isStartmenu = false;
            for (int i=0;i<pathToRoot.Count();i++)
            {
                var o = pathToRoot[i];
                int indexInParent = -1;
                if(o.Parent!=null&&i>0)
                {
                    var c = o.Parent.FindAllChildren();
                    for(int x=0;x<c.Count();x++)
                    {
                        if(o.Equals(c[x]))
                        {
                            indexInParent = x;
                        }
                    }
                }
                item = new WindowsSelectorItem(o, false, indexInParent);
                var idx = item.Properties.Where(x => x.Name == ConstFields.SELECTOR_INDEX_IN_PARENT).FirstOrDefault();
                if(idx!=null)
                {
                    idx.Enabled = false;
                }
                if(i==0||i==(pathToRoot.Count()-1))
                {
                    item.CanDisable = false;    //如果是首节点或者为尾节点，则不能隐藏
                }
                foreach(var p in item.Properties)
                {
                    int idx1 = p.Value.IndexOf(".");
                    if(p.Name== ConstFields.SELECTOR_CLASS_NAME&&idx1>-1)
                    {
                        int idx2 = p.Value.IndexOf(".", idx1 + 1);
                        if(idx2>idx1)
                        {
                            p.Enabled = false;   //对于此类的className会动态变化，不能用来做标识；
                        }
                    }
                    if(p.Name== ConstFields.SELECTOR_CLASS_NAME&&p.Value.ToLower()== "shelldll_defview")
                    {
                        item.Enabled = false;
                    }

                    if (p.Name == ConstFields.SELECTOR_CLASS_NAME && (p.Value.ToLower() == "dv2vontrolhost" || p.Value.ToLower() == "desktopprogramsmfu"))
                    {
                        isStartmenu = true;
                    }
                    if(p.Name== ConstFields.SELECTOR_CONTROL_TYPE&&p.Value.ToLower()=="listitem"&&isStartmenu)
                    {
                        p.Enabled = false;
                    }
                }
                var hassyslistview32 = item.Properties.Where(p => p.Name == ConstFields.SELECTOR_CLASS_NAME && p.Value.ToLower() == "syslistview32").ToList();
                if (hassyslistview32.Count > 0)
                {
                    var hasControlType = item.Properties.Where(p => p.Name == ConstFields.SELECTOR_CONTROL_TYPE).ToList();
                    if (hasControlType.Count > 0) { hasControlType[0].Enabled = false; }
                }
                this.Add(item);
            }
        }
    }
}
