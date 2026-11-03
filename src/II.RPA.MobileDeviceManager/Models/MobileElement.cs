using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Parse;
using Microsoft.Extensions.Logging;

namespace II.RPA.MobileDeviceManager.Models
{
    public class MobileElement
    {
        public delegate void GuiElementSelectedEventHandler(MobileElement element);
        public static GuiElementSelectedEventHandler OnSelected;
        public static Action OnCancelSelected;
        public static Action<List<Rectangle>> OnCheckElement;
        public static Action<System.Windows.Point> OnGetMousePointSelected;
        private static ILogger logger = Logger.OfContext();

        private XmlElement automationElement;
        public string ControlType { get; set; }
        public string ClassName { get; set; }
        public string Id { get; set; }
        public string Text { get; set; }
        public string Desc { get; set; }
        public Rectangle BoundRectangle { get; set; }
        public int Depth;
        public MobileElement ParentElement;
        public List<MobileElement> Children;
        public double Area { get; set; }
        public string filepath { get; set; }
        public Bitmap currentInformativeScreenshot { get; set; }
        public string SourceProvider { get; internal set; } = "Appium";
        public bool IsFromDegradedPageSource { get; internal set; }
        public long SourceGeneration { get; internal set; }
        public long SourceSnapshotVersion { get; internal set; }
        internal string CapturedSimilarSelector { get; set; }

        public string IndexInParent
        {
            get
            {
                if (this.ParentElement != null)
                {
                    if (this.ParentElement.Children.Count <= 1)
                    {
                        return null;
                    }
                    var index = this.ParentElement.Children.IndexOf(this);
                    var indexed = index;
                    for (var i = 0; i < index; i++)
                    {
                        if (this.ParentElement.Children[i].ClassName != this.ClassName)
                        {
                            indexed--;
                        }
                    }
                    if (index == -1)
                    {
                        return null;
                    }
                    return indexed.ToString();
                }
                return null;
            }
        }
        public string NthChildIndex { get; set; }
        public bool HasValidBounds => BoundRectangle.Width > 0 && BoundRectangle.Height > 0;
        public int StablePropertyCount
        {
            get
            {
                int count = 0;
                if (!string.IsNullOrEmpty(Id)) count++;
                if (!string.IsNullOrEmpty(Desc)) count++;
                return count;
            }
        }
        public MobileElement() { }
        public MobileElement(XmlNode element, int depth, MobileElement parentElement)
        {
            this.automationElement = (XmlElement)element;
            ParentElement = parentElement;
            this.Depth = depth;
            Children = new List<MobileElement>();
            ParseXml();
        }
        public void AddChild(MobileElement child)
        {
            Children.Add(child);
        }
        private void ParseXml()
        {
            string className = GetNonEmptyAttribute("class");
            string tagName = automationElement?.Name;
            string resolvedType = className;
            if (string.IsNullOrWhiteSpace(resolvedType)
                && !string.Equals(tagName, "node", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(tagName, "hierarchy", StringComparison.OrdinalIgnoreCase))
            {
                resolvedType = tagName;
            }

            ControlType = NullIfWhiteSpace(resolvedType);
            ClassName = ControlType;
            Text = GetNonEmptyAttribute("text", preserveWhitespace: true);
            Id = GetNonEmptyAttribute("resource-id");
            Desc = GetNonEmptyAttribute("content-desc", preserveWhitespace: true);
            NthChildIndex = GetNonEmptyAttribute("index");
            BoundRectangle = ParseBounds(GetNonEmptyAttribute("bounds"));
            Area = HasValidBounds ? (double)BoundRectangle.Width * BoundRectangle.Height : 0;
        }

        private string GetNonEmptyAttribute(string name, bool preserveWhitespace = false)
        {
            if (automationElement == null || !automationElement.HasAttribute(name)) return null;
            string value = automationElement.GetAttribute(name);
            // Display attributes must retain the exact value used by Appium XPath.
            return preserveWhitespace ? (string.IsNullOrEmpty(value) ? null : value) : NullIfWhiteSpace(value);
        }

        private static string NullIfWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static Rectangle ParseBounds(string bounds)
        {
            if (string.IsNullOrWhiteSpace(bounds)) return Rectangle.Empty;
            Match match = Regex.Match(bounds, @"^\s*\[(-?\d+),(-?\d+)\]\[(-?\d+),(-?\d+)\]\s*$");
            if (!match.Success) return Rectangle.Empty;

            int x1;
            int y1;
            int x2;
            int y2;
            if (!int.TryParse(match.Groups[1].Value, out x1)
                || !int.TryParse(match.Groups[2].Value, out y1)
                || !int.TryParse(match.Groups[3].Value, out x2)
                || !int.TryParse(match.Groups[4].Value, out y2)
                || x2 <= x1
                || y2 <= y1)
            {
                return Rectangle.Empty;
            }
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        public bool ContainPoint(Point point)
        {
            if (point.X >= BoundRectangle.X && point.X <= (BoundRectangle.X + BoundRectangle.Width) && point.Y >= BoundRectangle.Y && (point.Y <= BoundRectangle.Y + BoundRectangle.Height))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual string GetSelector_old()
        {
            List<MobileElement> pathToRoot = new List<MobileElement>();
            MobileElement element = this;
            while (element != null)
            {
                pathToRoot.Add(element);
                element = element.ParentElement;
            }
            pathToRoot.Reverse();
            List<AndroidSelectorItem> selectors = new List<AndroidSelectorItem>();

            foreach (var item in pathToRoot)
            {
                AndroidSelectorItem selectItem = new AndroidSelectorItem(item);
                selectors.Add(selectItem);
            }
            //json序列化刷掉值为null的属性
            var jsonSetting = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
            return JsonHelper.JsonByNewtonJson(selectors.ToArray(), jsonSetting);
        }
        /// <summary>
        /// 需改造后的selector结构
        /// </summary>
        /// <returns></returns>
        public virtual string GetSelector()
        {
            List<MobileElement> pathToRoot = new List<MobileElement>();
            MobileElement element = this;
            while (element != null)
            {
                pathToRoot.Add(element);
                element = element.ParentElement;
            }
            pathToRoot.Reverse();

            List<AndroidSelectorItem> selectors = new List<AndroidSelectorItem>();

            foreach (var item in pathToRoot)
            {
                AndroidSelectorItem selectItem = new AndroidSelectorItem(item);
                selectors.Add(selectItem);
            }


            List<SelectorProps> Jselectors = new List<SelectorProps>();
            foreach (var item in selectors)
            {
                var selector = new SelectorProps();
                selector.Name = item.Name;
                // 初始 Selector 尚未选定任何定位属性，节点本身也不应标记为启用。
                // 后续由 Provider 根据最终启用的属性统一回算节点 accurate。
                selector.Accurate = "false";
                var Props = new List<II.RPA.WinAutomation.CommonUtility.Parse.Props>();
                PropertyInfo[] propertys = item.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                foreach (var pro in propertys)
                {
                    if (pro.GetValue(item) != null && pro.GetValue(item).ToString() != "")
                    {
                        var prop = new WinAutomation.CommonUtility.Parse.Props();
                        #region 属性节点改为小写,为了不改变类的属性名，在这里直接转换
                        if (pro.Name == "ID")
                        {
                            prop.Name = "id";
                        }
                        if (pro.Name == "Name")
                        {
                            prop.Name = "name";
                        }
                        if (pro.Name == "ClassName")
                        {
                            prop.Name = "class";
                        }
                        if (pro.Name == "Text")
                        {
                            prop.Name = "text";
                        }
                        if (pro.Name == "Description")
                        {
                            prop.Name = "description";
                        }
                        if (pro.Name == "Index")
                        {
                            prop.Name = "index";
                        }
                        #endregion
                        prop.Value = pro.GetValue(item).ToString();
                        prop.Pattern = "equal";
                        //if (pro.Name == "Index" || item == selectors.Last())
                        //    prop.Accurate = "true";
                        //else
                        prop.Accurate = "false";
                        Props.Add(prop);
                    }
                }
                //foreach (var pro in item.Properties)
                //{
                //    var prop = new Core.Commons.Props();
                //    prop.Name = pro.Name;
                //    prop.Value = pro.Value;
                //    prop.Pattern = "equal";
                //    prop.Accurate = "true";
                //    Props.Add(prop);
                //}
                selector.Props = Props;
                Jselectors.Add(selector);
            }
            //json序列化刷掉值为null的属性
            var jsonSetting = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
            return JsonHelper.JsonByNewtonJson(Jselectors.ToArray(), jsonSetting);
        }
        private Bitmap DrawInformativeOnUiNode(Bitmap currentDesktopScreenshot)
        {
            //var ratio = NativeMethods.ScaleX;
            Rectangle cropRect = new Rectangle(this.BoundRectangle.X, this.BoundRectangle.Y, this.BoundRectangle.Width, this.BoundRectangle.Height);
            using (Graphics g = Graphics.FromImage(currentDesktopScreenshot))
            {
                //最终截图的红色标记边框
                Pen pen = new Pen(Color.Red, 2);
                g.DrawRectangle(pen, cropRect);
            }
            //this.boundingRectangle = uiNode.BoundingRectangle;
            cropRect.Inflate(5, 5);
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(currentDesktopScreenshot, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }

            return target;
        }
        public Bitmap CaptureInformativeScreenshot(string filepath, Bitmap bitmap)
        {
            Bitmap target = null;
            try
            {
                string dirName = Path.GetDirectoryName(filepath);

                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                Bitmap currentDesktopScreenshot = bitmap;

                //if (uiNode.BoundingRectangle.IsEmpty)
                //{
                //    //QQ截取时会出现为空的情况
                //    return null;
                //}
                target = DrawInformativeOnUiNode(currentDesktopScreenshot);
                try
                {
                    target.Save(filepath, System.Drawing.Imaging.ImageFormat.Bmp);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "snap screen shot fail");
            }
            return target;
        }
    }
}

