using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.OcrOffline.Plugins;
using II.RPA.Plugin.OcrOffline.Utils;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;

namespace II.RPA.Plugin.OcrOffline
{
    [Serializable]
    public class PayloadV2
    {
        public string image { get; set; }
    }

    internal class OcrHelper
    {
        private static ILogger logger = Logger.OfContext();

        internal static string GetOCRResult(Bitmap img, string server)
        {
            try
            {
                string imgStr = ImageHelper.Bitmap2Base64(img);
                HttpItem item = new HttpItem();
                item.URL = server;
                item.Method = "POST";
                //PayloadV2 pl = new PayloadV2();
                //pl.image = imgStr;
                Dictionary<string, object> dicParam = new Dictionary<string, object>();
                dicParam["image"] = imgStr;
                string js = JsonHelper.JsonByNewtonJson(dicParam);
                item.Postdata = js;
                //var result = new HttpHelper().GetHtml(item);
                var result = GetHtml(item, dicParam);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:" + result.Html);
                    return result.Html;
                }
                else
                {
                    logger.LogError($"Ocr离线解析服务返回异常，请联系相关人员.{result?.Html}");
                    RPAException cVException = new RPAException(eErrorMessageType.Error_OCROffline_NotMatchingElementError, eComponentType.ExecuteCV);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (Exception ex)
            {
                RPAException rpaException = new RPAException(eErrorMessageType.Error_OCROffline_ServiceError, eComponentType.None, ex);
                throw rpaException;//向上层抛出异常
            }
        }


        /// <summary>
        /// 拾取返回结果的解析函数
        /// </summary>
        /// <param name="_ImageData"></param>
        /// <param name="server"></param>
        /// <param name="_Result"></param>
        /// <param name="_ErrMsg"></param>
        /// <returns></returns>
        internal static bool GetOCRTableResult(ImageData _ImageData, string server, ref string _Result, ref JMessage _ErrMsg)
        {
            try
            {
                string imgStr = _ImageData?.Base64?.ToString();//ImageHelper.Bitmap2Base64(img);
                List<List<int>> pointList = new List<List<int>> { new List<int> { _ImageData.StartPoint.X, _ImageData.StartPoint.Y }, new List<int> { _ImageData.EndPoint.X, _ImageData.EndPoint.Y } };

                HttpItem item = new HttpItem();
                item.URL = server;
                item.Method = "POST";

                Dictionary<string, object> dicParam = new Dictionary<string, object>();
                dicParam["image"] = imgStr;
                dicParam["points"] = pointList;
                string js = JsonHelper.JsonByNewtonJson(dicParam);
                //logger.LogDebug($"入参：{js}");
                item.Postdata = js;
                var result = GetHtml(item, dicParam);
              
                // File.WriteAllText("OcrHtml.txt", result.Html);
                logger.LogInformation($"获取服务器返回result.Html:{result.Html.ToString()}");
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug($"result.Html:{result.Html}");
                    string ErrMsg = string.Empty;
                    if (AlgJsonAnaylse.HasData(result.Html, ref ErrMsg))
                    {
                        _Result = result.Html;
                        return true;
                    }
                    else
                    {
                        _ErrMsg = OcrTableError.GetErrMessage(eErrorMessageType.Error_GetTableDataEmpty, "拾取表格失败:获取表格数据为空".I18n("OcrHelper_354E2D9E"));
                    }
                }
                else
                {
                    logger.LogError($"Ocr离线解析服务返回异常:{result?.Html}");
                    _ErrMsg = OcrTableError.GetErrMessage(eErrorMessageType.Error_GetTableDataFail);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "算法返回值解析失败");
                _ErrMsg = OcrTableError.GetErrMessage(eErrorMessageType.Error_GetNoTable);
            }
            return false;
        }

        /// <summary>
        /// 本地图片解析或者拾取解析的返回结果解析函数
        /// </summary>
        /// <param name="_ImageData"></param>
        /// <param name="server"></param>
        /// <param name="_Result"></param>
        /// <param name="_ErrMsg"></param>
        /// <returns></returns>
        internal static bool GetOCRTableResultEx(ImageDataEx _ImageData, string server, ref string _Result, ref JMessage _ErrMsg)
        {
            try
            {
                string imgStr = _ImageData?.Base64?.ToString();
                string _strTableAlgInfo = _ImageData?.TableAlgInfo?.ToString();
                List<List<int>> pointList = new List<List<int>> { new List<int> { _ImageData.StartPoint.X, _ImageData.StartPoint.Y }, new List<int> { _ImageData.EndPoint.X, _ImageData.EndPoint.Y } };

                HttpItem item = new HttpItem();
                item.URL = server;
                item.Method = "POST";

                Dictionary<string, object> dicParam = new Dictionary<string, object>();
                dicParam["image"] = imgStr;
                dicParam["points"] = pointList;
                dicParam["table_algo_info"] = _strTableAlgInfo;
                string js = JsonHelper.JsonByNewtonJson(dicParam);
                //logger.LogDebug($"入参：{js}");
                item.Postdata = js;
                var result = GetHtml(item, dicParam);
                logger.LogInformation($"GetOcrTableData 表格解析执行 接口返回：{result}");
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:" + result.Html);
                    string ErrMsg = string.Empty;
                    if (AlgJsonAnaylse.HasData(result.Html, ref ErrMsg))
                    {
                        _Result = result.Html;
                        return true;
                    }
                    else
                    {
                        _ErrMsg = OcrTableError.GetErrMessage(eErrorMessageType.Error_GetTableDataEmptyExcute, "拾取表格失败:获取表格数据为空".I18n("OcrHelper_354E2D9E"));
                    }
                }
                else
                {
                    logger.LogError($"Ocr离线解析服务返回异常:{result?.Html}");
                    _ErrMsg = OcrTableError.GetErrMessage(eErrorMessageType.Error_GetTableDataFailExcute);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"算法返回值解析失败");
                _ErrMsg = OcrTableError.GetErrMessage(eErrorMessageType.Error_GetNoTable);
            }
            return false;
        }

        #region 原返回结果解析函数
        /// <summary>
        /// 原拾取返回结果解析函数
        /// </summary>
        /// <param name="_ImageData"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        internal static string GetOCRTableResult(ImageData _ImageData, string server)
        {
            try
            {
                string imgStr = _ImageData?.Base64?.ToString();//ImageHelper.Bitmap2Base64(img);
                List<List<int>> pointList = new List<List<int>> { new List<int> { _ImageData.StartPoint.X, _ImageData.StartPoint.Y }, new List<int> { _ImageData.EndPoint.X, _ImageData.EndPoint.Y } };
                HttpItem item = new HttpItem();
                item.URL = server;
                item.Method = "POST";

                Dictionary<string, object> dicParam = new Dictionary<string, object>();
                dicParam["image"] = imgStr;
                dicParam["points"] = pointList;
                string js = JsonHelper.JsonByNewtonJson(dicParam);
                logger.LogDebug($"入参：{js}");
                item.Postdata = js;
                //var result = new HttpHelper().GetHtml(item);
                var result = GetHtml(item, dicParam);
                //result.StatusCode = System.Net.HttpStatusCode.OK;//固定返回状态
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:" + result.Html);
                    return result.Html;
                }
                else
                {
                    logger.LogError($"Ocr离线解析服务返回异常，请联系相关人员.{result?.Html}");
                    RPAException cVException = new RPAException(eErrorMessageType.Error_OCROffline_NotMatchingElementError, eComponentType.ExecuteCV);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (Exception ex)
            {
                RPAException rpaException = new RPAException(eErrorMessageType.Error_OCROffline_ServiceError, eComponentType.None, ex);
                throw rpaException;//向上层抛出异常
            }
        }

        /// <summary>
        /// 原表格解析执行的返回值解析
        /// </summary>
        /// <param name="_ImageData"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        internal static string GetOCRTableResult(ImageDataEx _ImageData, string server)
        {
            try
            {
                string _imgStr = _ImageData?.Base64?.ToString();
                string _strTableAlgInfo = _ImageData?.TableAlgInfo?.ToString();
                List<List<int>> pointList = new List<List<int>> { new List<int> { _ImageData.StartPoint.X, _ImageData.StartPoint.Y }, new List<int> { _ImageData.EndPoint.X, _ImageData.EndPoint.Y } };

                HttpItem item = new HttpItem();
                item.URL = server;
                item.Method = "POST";

                Dictionary<string, object> dicParam = new Dictionary<string, object>();
                dicParam["image"] = _imgStr;
                dicParam["points"] = pointList;
                dicParam["table_algo_info"] = _strTableAlgInfo;
                string js = JsonHelper.JsonByNewtonJson(dicParam);
                logger.LogInformation($"启动Ocr离线解析服务");
                item.Postdata = js;
                logger.LogDebug($"入参：{js}");
                var result = GetHtml(item, dicParam);
                logger.LogInformation($"Ocr离线解析服务调用结束");
                logger.LogDebug("result.Html:" + result.Html);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:" + result.Html);
                    return result.Html;
                }
                else
                {
                    logger.LogError($"Ocr离线解析服务返回异常，请联系相关人员.{result?.Html}");
                    RPAException cVException = new RPAException(eErrorMessageType.Error_OCROffline_NotMatchingElementError, eComponentType.ExecuteCV);
                    throw cVException;//向上层抛出异常
                }

            }
            catch (Exception ex)
            {

                RPAException rpaException = new RPAException(eErrorMessageType.Error_OCROffline_ServiceError, eComponentType.None, ex);
                throw rpaException;//向上层抛出异常
            }
        }
        #endregion

        #region 蛮吉代码
        private static HttpResult GetHtml(HttpItem item, Dictionary<string, object> dicParams)
        {
            string saltStr = EncryptUtils.MD5Encrypt("indeedRpaCVOfflineSalt" + GlobleConfigurations.UserToken);
            DateTime DateNow = DateTime.Now;
            long t = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            dicParams.Add("t", t);
            if (!dicParams.ContainsKey("timeout"))
            {
                dicParams.Add("timeout", item.Timeout);
            }
            var dicParams_sort = (from k in dicParams
                                  orderby k.Key
                                  select k).ToDictionary(kv => kv.Key, kv => kv.Value);
            StringBuilder sb_salt = new StringBuilder();
            List<KeyValuePair<string, object>> sgin_params = new List<KeyValuePair<string, object>>();
            string signSourceStr = "{0}&{1}&{2}";
            Uri uri = new Uri(item.URL);

            foreach (var p in dicParams_sort)
            {
                sb_salt.Append(saltStr);
                if (p.Value == null)
                {
                    dicParams_sort[p.Key] = "";
                    //sgin_params.Add(p);
                    //continue;
                }
                if (p.Value.GetType().IsArray)
                {
                    continue;
                }
                if (!(p.Value is string) && p.Value.GetType().IsClass)
                {
                    continue;
                }
                sgin_params.Add(p);
                logger.LogDebug($"参与签名参数：【{p.Key.ToUpper()}】{(p.Value.ToString().Length > 20 ? p.Value.ToString().Substring(0, 20) : p.Value.ToString())}");
            }
            logger.LogDebug(sb_salt.ToString());
            string paramsStr = string.Join("&", sgin_params.Select(o => $"{o.Key.ToUpper()}={(o.Value ?? "")}"));


            signSourceStr = string.Format(signSourceStr, uri.AbsolutePath, sb_salt.ToString(), paramsStr);
            string sign = EncryptUtils.MD5Encrypt(signSourceStr);

            item.URL = $"{item.URL}{(item.URL.IndexOf("?") > 0 ? "&" : "?")}sign={sign}&t={t}";
            logger.LogDebug($"CV URL: {item.URL}");
            return GetHtml(item);
        }
        private static HttpResult GetHtml(HttpItem item)
        {
            item.Header.Add("CLIENT-USER", GlobleConfigurations.ClientUser);
            item.Header.Add("CLIENT-MAC", GlobleConfigurations.ClientMac);
            item.Header.Add("engineCode", GlobleConfigurations.EngineCode);
            return new HttpHelper().GetHtml(item);
        }
        #endregion
    }
}

