using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Windows.Config;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows.Core
{
    internal class WindowElement
    {
        public WindowElement(IntPtr hWnd)
        {
            this.WindowHandle = hWnd;
        }
        public WindowElement(AutomationElement winElement)
        {
            this.UIAElement = winElement;
        }
        public IntPtr WindowHandle { get; set; }
        public string Title { get; set; }
        public string ClassName { get; set; }
        public AutomationElement UIAElement { get; set; }
        /// <summary>
        /// 通过扫描元素树，获取指定位置的元素
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public List<AutomationElement> FromPointByScanElementTree(Point point)
        {
            return FromPointByScanElementTree(point, this.UIAElement, 0);
        }
        /// <summary>
        /// 通过扫描元素树，获取指定位置的元素
        /// </summary>
        /// <param name="point"></param>
        /// <param name="parentElement"></param>
        /// <param name="scanLevel"></param>
        /// <returns></returns>
        public List<AutomationElement> FromPointByScanElementTree(Point point, AutomationElement parentElement, int scanLevel)
        {
            //获取所有符合 坐标点 的 子元素
            var eleChildrens = parentElement.GetChildrenByRawView();
            eleChildrens = eleChildrens.Where(ele => ele.BoundingRectangle.Contains(point)).ToArray();

            List<AutomationElement> eleList = null;
            if (eleChildrens.Length > 0)
            {
                var smallerElements = from c in eleChildrens
                                      where c.BoundingRectangle.Contains(point)
                                      orderby c.BoundingRectangle.X ascending, c.BoundingRectangle.Y ascending
                                      select c;
                if (smallerElements.Count() > 0)
                {
                    eleList = new List<AutomationElement>();
                    foreach (var item in smallerElements)
                    {
                        item.ScanLevel = scanLevel;
                        item.ScanCachedParent = parentElement;
                        var scanChildenList = FromPointByScanElementTree(point, item, scanLevel + 1);
                        if (scanChildenList?.Count() > 0)
                        {
                            eleList.AddRange(scanChildenList);
                        }
                        else
                        {
                            eleList.Add(item);
                        }
                    }
                }
            }
            return eleList;
        }

    }
}
