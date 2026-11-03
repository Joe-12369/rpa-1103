
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using II.RPA.WinAutomation.CommonUtility.Parse;
using II.RPA.MobileProvider.Selecter;

namespace II.RPA.MobileProvider.CV
{
    public class CVMobileElement : MobileElement
    {
        private List<CVSelectorItem> CVSelectorItemList;
        //private static ILogger logger = Logger.OfContext();

        public CVMobileElement()
        {
        }
        public void SetBoundRectangle(float[] left_top, float[] left_bottom, float[] right_top, float[] right_bottom)
        {
            float minx = Math.Min(left_top[0], left_bottom[0]);
            float maxx = Math.Max(right_top[0], right_bottom[0]);
            float miny = Math.Min(left_top[1], right_top[1]);
            float maxy = Math.Max(left_bottom[1], right_bottom[1]);
            float width = maxx - minx;
            float height = maxy - miny;
            Area = Math.Ceiling(width * height);
            BoundRectangle = new Rectangle((int)Math.Ceiling(minx), (int)Math.Ceiling(miny), (int)Math.Ceiling(width), (int)Math.Ceiling(maxy - miny));
        }
        public void SetSelectorItems(CVSelectorItem item)
        {
            CVSelectorItemList = new List<CVSelectorItem>();

            CVSelectorItem item1 = new CVSelectorItem();
            item1.Properties.Add(new SelectorItemProperty("selector", "CV"));
            CVSelectorItemList.Add(item1);

            CVSelectorItem item2 = new CVSelectorItem();
            item2.Properties.Add(new SelectorItemProperty("Name", "Mobile"));
            item2.Properties.Add(new SelectorItemProperty("ClassName", "Mobile"));
            CVSelectorItemList.Add(item2);

            CVSelectorItemList.Add(item);
        }
        public override string GetSelector()
        {
            //var results = new List<JObject>();
            //foreach (var item in CVSelectorItemList)
            //{
            //    results.Add(item.json);
            //}
            //return new JArray(results).ToString();


            List<SelectorProps> Jselectors = new List<SelectorProps>();
            foreach (var item in CVSelectorItemList)
            {
                var selector = new SelectorProps();
                //selector.Name = "";
                var Props = new List<WinAutomation.CommonUtility.Parse.Props>();
                foreach (var pro in item.Properties)
                {
                    var prop = new WinAutomation.CommonUtility.Parse.Props();
                    prop.Name = pro.Name == "ClassName" ? "class" : pro.Name;
                    prop.Value = pro.Value;
                    prop.Pattern = "equal";
                    prop.Accurate = "true";
                    Props.Add(prop);
                }
                selector.Props = Props;
                selector.Accurate = Props.Any(prop =>
                    string.Equals(prop.Accurate, "true", StringComparison.OrdinalIgnoreCase))
                    ? "true"
                    : "false";
                Jselectors.Add(selector);
            }
            var jsonSetting = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
            return JsonHelper.JsonByNewtonJson(Jselectors.ToArray(), jsonSetting);
        }

        public CVMobileElement(string selector)
        {
            try
            {
                CVSelectorItemList = new List<CVSelectorItem>();
                string selectorType, version;
                ExtractViewData.SelectCommonProps(selector, out selectorType, out version);
                if (version.Equals("1.0") && selectorType != "None")
                {
                    List<MobileItem> secList = new List<MobileItem>();
                    RPACommon selectorAll = JsonHelper.ToObject<RPACommon>(selector);
                    secList = JsonHelper.ToObject<List<MobileItem>>(selectorAll.Selector.ToString());
                    foreach (var sec in secList)
                    {
                        var cvSelectorItem = new CVSelectorItem();
                        cvSelectorItem.Properties = new List<SelectorItemProperty>();
                        foreach (var item in sec.props)
                        {
                            cvSelectorItem.Properties.Add(new SelectorItemProperty(item.name, item.value));
                        }
                        CVSelectorItemList.Add(cvSelectorItem);
                    }
                }
                else
                {
                    JArray json = JArray.Parse(selector);
                    foreach (JObject o in json)
                    {
                        var item = new CVSelectorItem(o);
                        CVSelectorItemList.Add(item);
                    }
                }
                if (CVSelectorItemList.Count < 3)
                {
                    throw new Exception(Localize($"手机CV通过selector转化元素：selector有误", "controlType_CVMobileElement_5618EB83"));
                }
            }
            catch (Exception ex)
            {
                //Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"手机CV通过selector转化元素失败, selector:{selector}");
                throw new Exception(Localize($"手机CV通过selector转化元素失败{ex.Message}", "controlType_CVMobileElement_B7A55434"));
            }
        }
        public void LoadNewBoundRectangle(CVHelper cVHelper)
        {
            try
            {
                //获取CVNode中操作元素的对象中的 识别结构体数据 和  锚点元素识别结构体数据
                string elementObjStr = CVSelectorItemList[2].Properties.Where(x => x.Name == "ElementObj").First().Value.ToString();
                string anchor_obj = CVSelectorItemList[2].Properties.Where(x => x.Name == "AnchorObj").First().Value.ToString(); ;
                //CV元素操作前在服务端重新解析图片拾取操作员最新位置,如果拾取失败返回空集合
                List<int[]> locations = cVHelper.OperationMatchingByCVService(elementObjStr, anchor_obj);
                if (locations.Count() == 4)
                {
                    //获取 元素操作前在服务端重新解析 之后  得到的 返回信息
                    //OperationMatchingByCVService_Result matchingReulst = JsonHelper.JsonDesByNewtonJson<OperationMatchingByCVService_Result>(result);
                    int locationX = locations[0][0];
                    int locationY = locations[0][1];
                    int width = locations[1][0] - locations[0][0];
                    int height = locations[2][1] - locations[1][1];
                    //最终以BoundingRectangle属性体现
                    BoundRectangle = new Rectangle(locationX, locationY, width, height);
                }
                else
                {
                    //logger.LogInformation($"CV元素匹配元素location异常，仅获取到{locations.Count}个点");
                    throw new MobileException(eErrorMessageType.Error_CVNotMatchingElementError);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Point GetElementCenter()
        {
            Point point = new Point
            {
                X = BoundRectangle.X + BoundRectangle.Width / 2,
                Y = BoundRectangle.Y + BoundRectangle.Height / 2
            };
            return point;
        }
        //public void SetCurrentImage()
        //{ 
        //若将图片都保存则太耗内存不该保存
        //}
        public void SaveSelectImage(byte[] Bytes)
        {
            Bitmap bitmap = BytesToBitmap(Bytes);
            Bitmap subBitmap = new Bitmap(BoundRectangle.Width, BoundRectangle.Height);
            Graphics g = Graphics.FromImage(subBitmap);
            g.DrawImage(bitmap, 0, 0, BoundRectangle, GraphicsUnit.Pixel);
            currentInformativeScreenshot = subBitmap;
        }

        private static Bitmap BytesToBitmap(byte[] Bytes)
        {
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Bytes);
                return new Bitmap(new Bitmap(stream));
            }
            catch (ArgumentNullException ex)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                throw;
            }
            finally
            {
                stream.Close();
            }
        }

    }
}

