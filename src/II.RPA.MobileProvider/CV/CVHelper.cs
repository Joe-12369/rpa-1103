
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;


namespace II.RPA.MobileProvider.CV
{
    public class CVHelper
    {
        private readonly string _base64Image;
        private readonly string _cvUrl;
        private readonly List<CVItem> _cvResults = new List<CVItem>();
        //private static ILogger logger = Logger.OfContext();

        public CVHelper(string base64Image, string cvUrl)
        {
            _base64Image = base64Image;
            _cvUrl = cvUrl;
        }

        public bool CVParse()
        {
            return ShowWaitingWnd(() =>
            {
                string result = InspectByCV(_base64Image);
                ParseCVInfo(result);
            });
        }
        private string InspectByCV(string base64Image)
        {
            try
            {
                HttpItem item = new HttpItem
                {
                    URL = _cvUrl,
                    Method = "POST"
                };
                Payload pl = new Payload
                {
                    image = base64Image,
                    app = "mobbile"
                };
                string js = JsonHelper.JsonByNewtonJson(pl);
                item.Postdata = js;
#if Trace
                logger.LogDebug(js);
#endif  
                var result = GetHtml(item);
#if Trace
                logger.LogDebug(result.Html);
#endif  
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                   // logger.LogDebug("result.Html:" + result.Html);
                    return result.Html;
                }
                else
                {
                   // logger.LogError($"CV解析服务返回异常，请联系相关人员.{result?.Html}");
                    throw new MobileException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture);
                }
            }
            catch (MobileException cvEx)
            {
                throw;
            }
            catch (Exception ex)
            {
               // Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "CV解析服务返回异常，请联系相关人员.");
                throw new MobileException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture, ex);
            }
        }

        public HttpResult GetHtml(HttpItem item)
        {
            item.Header.Add("CLIENT-USER", GlobleClintInfo.ClientUser);
            item.Header.Add("CLIENT-MAC", GlobleClintInfo.ClientMac);
            return new HttpHelper().GetHtml(item);
        }
        private void ParseCVInfo(string result)
        {
            //logger.LogInformation($"CV元素解析服务返回的数据：{result}");
            _cvResults.Clear();
            try
            {
                CVMessage cvm = JsonHelper.JsonDesByNewtonJson<CVMessage>(result);
                if (cvm.status == "success")
                {
                    JArray items = cvm.result as JArray;
                    if (items != null)
                    {
                        foreach (var item in items.Children())
                        {
                            CVItem cv = JsonHelper.JsonDesByNewtonJson<CVItem>(item.ToString());
                            _cvResults.Add(cv);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               // Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "CV元素解析服务返回的数据");
                throw new MobileException(eErrorMessageType.Error_CVElementServiceDataUseError, eComponentType.Capture, ex);
            }
        }
        /// <summary>
        /// 弹出等待窗口 ，并开启一个线程执行  输入的方法 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private bool ShowWaitingWnd(Action action)
        {
            //var waitWnd = new CVWaiting();
            //bool bResult = false;
            //Exception exception = null;
            //Thread thread = new Thread(() =>
            //{
            //    try
            //    {
            //        action?.Invoke();
            //        bResult = true;
            //    }
            //    catch (Exception ex)
            //    {
            //        logger.LogError(ex);
            //        bResult = false;
            //        exception = ex;
            //    }
            //    finally
            //    {
            //        waitWnd.Dispatcher.Invoke(() =>
            //        {
            //            waitWnd.Close();
            //        });
            //    }
            //})
            //{
            //    IsBackground = true
            //};
            //thread.Start();
            //waitWnd.ShowDialog();
            //if (exception != null)
            //{
            //    throw exception;
            //}
            //return bResult;
            return true;
        }

        private CVSelectorItem GetCVElementSelector(CVItem cVItem)
        {
            //得到最终 操作前需要回传服务端解析做需要的 结构体信息 JSON
            string elementObjStr = JsonHelper.JsonByNewtonJson(cVItem);
            //获取 用户选择的元素对应锚点(也是一个元素)的返回结构体信息
            CVItem anchorObj = _cvResults.Where(item => item.result_id == cVItem.result_value.anchor).FirstOrDefault(); ;
            string anchorObjStr = anchorObj == null ? "" : JsonHelper.JsonByNewtonJson(anchorObj);
            CVSelectorItem cvselectorItem = new CVSelectorItem(elementObjStr, anchorObjStr);
            return cvselectorItem;
        }

        private CVMobileElement CreateCVMobileElement(CVItem item)
        {
            CVMobileElement cVMobileElement = new CVMobileElement();
            cVMobileElement.SetBoundRectangle(item.left_top, item.left_bottom, item.right_top, item.right_bottom);
            cVMobileElement.SetSelectorItems(GetCVElementSelector(item));
            return cVMobileElement;
        }

        public List<CVMobileElement> GetCVMobileElements()
        {
            List<CVMobileElement> cVMobileElements = new List<CVMobileElement>();
            foreach (var item in _cvResults)
            {
                cVMobileElements.Add(CreateCVMobileElement(item));
            }
            return cVMobileElements;
        }

        public List<int[]> OperationMatchingByCVService(string element_obj, string anchor_obj)
        {
            try
            {
                List<int[]> locations = new List<int[]>();//要返回的坐标4点信息
                string result = string.Empty;//接收服务端返回值
                string imgStr = string.Empty; //base64 image
                {
                    //弹出等待窗口，并传入 去服务端调用 元素匹配拾取 服务
                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    ShowWaitingWnd(() =>
                    //    {
                    //        //http 调用获取 元素匹配拾取服务提供 的匹配信息
                    //        result = OperationMatchingByCVService_HttpRequest(_base64Image, _cvUrl, element_obj, anchor_obj);
                    //        logger.LogInformation(result);
                    //    });
                    //});

                    try
                    {
                        result = OperationMatchingByCVService_HttpRequest(_base64Image, _cvUrl, element_obj, anchor_obj);
                        CVMessage cvm = JsonHelper.JsonDesByNewtonJson<CVMessage>(result);
                        if (cvm.status == "success")
                        {
                            locations = JsonHelper.JsonDesByNewtonJson<List<int[]>>(cvm.result);
                        }
                        else
                        {
                            throw new MobileException(eErrorMessageType.Error_CVNotMatchingElementError);
                        }
                    }
                    catch (MobileException e)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        throw new MobileException(eErrorMessageType.Error_CVElementServiceDataUseError, e);
                    }
                }
                return locations;
            }
            catch (MobileException cvEx)
            {
               // Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, cvEx, "通过锚点读取元素信息时异常");
                throw;
            }
            catch (Exception ex)
            {
               // Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "通过锚点读取元素信息时异常");
                throw;
            }
        }

        /// <summary>
        /// 发起Http 请求，进行操作前的元素匹配拾取  CV 服务器解析，返回服务器提供的数据
        /// </summary>
        /// <param name="imgStr"></param>
        /// <param name="element_obj"></param>
        /// <param name="anchor_obj"></param>
        /// <returns></returns>
        private string OperationMatchingByCVService_HttpRequest(string imgStr, string cvUrl, string element_obj, string anchor_obj)
        {
            try
            {
                //logger.LogDebug("element_obj:" + element_obj);
                //logger.LogDebug("anchor_obj" + anchor_obj);
                HttpItem item = new HttpItem
                {
                    URL = cvUrl,
                    Method = "POST"
                };
                Dictionary<string, object> dic = new Dictionary<string, object>
                {
                    ["image"] = imgStr,
                    ["element_obj"] = imgStr
                };
                if (string.IsNullOrEmpty(element_obj))
                {
                    dic["element_obj"] = "";
                }
                else
                {
                    dic["element_obj"] = JsonHelper.JsonDesByNewtonJson<CVItem>(element_obj);

                }
                if (string.IsNullOrEmpty(anchor_obj))
                {
                    dic["anchor_obj"] = "";
                }
                else
                {
                    dic["anchor_obj"] = JsonHelper.JsonDesByNewtonJson<CVItem>(anchor_obj);

                }
                string js = JsonHelper.JsonByNewtonJson(dic);
                item.Postdata = js;

                var result = GetHtml(item);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //logger.LogDebug("result.Html:" + result.Html);
                    return result.Html;
                }
                else
                {
                   // logger.LogError($"CV元素匹配拾取服务调用返回异常，请联系相关人员.{result?.Html}");
                    throw new MobileException(eErrorMessageType.Error_CVElementMatchingServiceError);
                }
            }
            catch (MobileException cvEx)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MobileException(eErrorMessageType.Error_CVElementMatchingServiceError, ex);
            }
        }
    }
}


