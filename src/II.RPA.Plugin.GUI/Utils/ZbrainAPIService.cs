using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace II.RPA.Plugin.GUI
{
    internal class ZbrainAPIService
    {
        private static ILogger logger = Logger.OfContext();

        internal string GetZBrainResult(Bitmap img, string server)
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
        internal static string SendAiRepairToZBrain_V2(string uid, string path, Bitmap img, CaptureImgSourceType sourceType, Dictionary<string, object> wndInfo, string eleVersion = "", string desc = "", string mode = "", bool useExistingElementLocate = false, bool isEnableSelfRepaire = false, string elementId = "")
        {
            try
            {
                if (!GlobleConfigurations.IsSyncProjectPathToZBrain)
                {
                    var ret = CongfigSyncToZBrain(ConstFields.ZBRAIN_CONFIGASYNC_PROJECT_PATH, GlobleConfigurations.PackageDirectory);
                    if (!ret)
                    {
                        return string.Empty;
                    }
                }
                string imgStr = ImageHelper.Bitmap2Base64(img);
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.Timeout = 20000;
                //item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.AI_INTELLIGENT_REPAIR_EXEC_BY_HWND;
                item.URL = uri.GetLeftPart(UriPartial.Authority) + (useExistingElementLocate ? CVRouteConstFields.AI_EXISTING_ELEMENT_LOCATE : CVRouteConstFields.AI_INTELLIGENT_REPAIR_EXEC);
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["uid"] = uid;
                dic["path"] = path;
                dic["image"] = imgStr;
                dic["captureModel"] = sourceType.ToString();
                dic["wnd_info"] = wndInfo;
                //ai增强定位需求新增下面参数
                dic["ele_version"] = eleVersion;   // ZBrain据此分流新/旧处理逻辑
                dic["desc"] = desc;
                dic["mode"] = mode;                // "runtime" | "validation"
                dic["client_tenantCode"] = GlobleConfigurations.ClientTenantCode;
                dic["tarsAgent_serviceUrl"] = GlobleConfigurations.TarsAgentServiceUrl;
                dic["tarsAgent_serviceToken"] = GlobleConfigurations.TarsAgentServiceToken;
                if (TryGetPackageInfo(out Dictionary<string, object> packageInfo))
                {
                    dic["package_info"] = packageInfo;
                }
                dic["flow_retrieve_enable"] = GlobleConfigurations.EnableAiEnhancedScreenshot;
                dic["element_retrieve_enable"] = isEnableSelfRepaire;
                dic["element_id"] = elementId;
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogInformation("SendAiRepairToZBrain result.Html:OK");
                    return result.Html;
                }
                else
                {
                    logger.LogError($"SendAiRepairToZBrain 服务返回异常，请联系相关人员.{result?.Html}");
                    RPAException cVException = new RPAException(eErrorMessageType.Error_OCROffline_NotMatchingElementError, eComponentType.ExecuteCV);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "SendAiRepairToZBrain 执行异常，请联系相关人员");
                //throw ex;//向上层抛出异常
                return string.Empty;
            }
        }
        internal static string SendAiRepairToZBrain(string uid, string path, Bitmap img, CaptureImgSourceType sourceType)
        {
            try
            {
                if (!GlobleConfigurations.IsSyncProjectPathToZBrain)
                {
                    var ret = CongfigSyncToZBrain(ConstFields.ZBRAIN_CONFIGASYNC_PROJECT_PATH, GlobleConfigurations.PackageDirectory);
                    if (!ret)
                    {
                        return string.Empty;
                    }
                }
                string imgStr = ImageHelper.Bitmap2Base64(img);
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.Timeout = 10000;
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.AI_INTELLIGENT_REPAIR_EXEC;
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                item.Timeout = 10000;
                dic["uid"] = uid;
                dic["path"] = path;
                dic["image"] = imgStr;
                dic["captureModel"] = sourceType.ToString();

                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogInformation("SendAiRepairToZBrain result.Html:OK");
                    return result.Html;
                }
                else
                {
                    logger.LogError($"SendAiRepairToZBrain 服务返回异常，请联系相关人员.{result?.Html}");
                    RPAException cVException = new RPAException(eErrorMessageType.Error_OCROffline_NotMatchingElementError, eComponentType.ExecuteCV);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "SendAiRepairToZBrain 执行异常，请联系相关人员");
                //throw ex;//向上层抛出异常
                return string.Empty;
            }
        }
        /// <summary>
        /// 获取上游写入的流程包信息，并校验 z-brain 必填字段。
        /// </summary>
        internal static bool TryGetPackageInfo(out Dictionary<string, object> packageInfo)
        {
            packageInfo = null;
            JObject source = GlobleConfigurations.PackageInfo as JObject;
            if (source == null)
            {
                return false;
            }
            packageInfo = new Dictionary<string, object>
            {
                ["package_name"] = source["packageName"]?.ToString() ?? string.Empty,
                ["package_version"] = source["packageVersion"]?.ToString() ?? string.Empty,
                ["package_inner_id"] = source["packageInnerId"]?.ToString() ?? string.Empty
            };
            return true;
        }
        /// <summary>
        /// 查询老流程元素是否需要补充截图。true=需要截图，false=无需截图，null=查询失败。
        /// </summary>
        internal static bool? QueryExistingElementImage(Dictionary<string, object> packageInfo, string elementId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GlobleConfigurations.ZBrainServicePath))
                {
                    return null;
                }

                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem
                {
                    URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.AI_EXISTING_ELEMENT_IMAGE_QUERY,
                    Method = "POST",
                    Timeout = 3000
                };
                Dictionary<string, object> dic = new Dictionary<string, object>
                {
                    ["package_info"] = packageInfo,
                    ["element_id"] = elementId
                };

                var result = GetHtml(item, dic);
                if (result.StatusCode != System.Net.HttpStatusCode.OK || string.IsNullOrWhiteSpace(result.Html))
                {
                    logger.LogWarning($"QueryExistingElementImage 服务返回异常:{result?.Html}");
                    return null;
                }

                JObject retObj = JObject.Parse(result.Html);
                if (!string.Equals(retObj.Value<string>("status"), "success", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning($"QueryExistingElementImage 查询失败:{result.Html}");
                    return null;
                }
                if (retObj.TryGetValueByPath("result.need_screenshot", out bool needScreenshot))
                {
                    return needScreenshot;
                }
                logger.LogWarning($"QueryExistingElementImage 返回值缺少need_screenshot:{result.Html}");
                return null;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "QueryExistingElementImage 执行异常");
                return null;
            }
        }

        /// <summary>
        /// 上传老流程元素截图。bounding 为元素相对截图左上角的矩形。
        /// </summary>
        internal static bool UploadExistingElementImage(Dictionary<string, object> packageInfo, string elementId, string imagePath, Rectangle bounding)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GlobleConfigurations.ZBrainServicePath))
                {
                    return false;
                }

                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem
                {
                    URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.AI_EXISTING_ELEMENT_IMAGE_UPLOAD,
                    Method = "POST",
                    Timeout = 3000
                };
                Dictionary<string, object> dic = new Dictionary<string, object>
                {
                    ["image_path"] = imagePath.Replace('\\', '/'),
                    ["bounding"] = new Dictionary<string, object>
                    {
                        ["x"] = bounding.X,
                        ["y"] = bounding.Y,
                        ["w"] = bounding.Width,
                        ["h"] = bounding.Height
                    },
                    ["package_info"] = packageInfo,
                    ["element_id"] = elementId
                };

                var result = GetHtml(item, dic);
                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogWarning($"UploadExistingElementImage 服务返回异常:{result?.Html}");
                    return false;
                }

                // 接口约定 C# 端无需等待业务处理结果，HTTP 请求接收成功即可。
                if (string.IsNullOrWhiteSpace(result.Html))
                {
                    return true;
                }
                JObject retObj = JObject.Parse(result.Html);
                string status = retObj.Value<string>("status");
                return string.IsNullOrWhiteSpace(status)
                    || string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "UploadExistingElementImage 执行异常");
                return false;
            }
        }

        internal static void SendUIDToZBrain(Dictionary<string, object> wndInfo)
        {
            var captureUniqueID = GlobleConfigurations.CaptureUniqueID;
            Action<string> action = (string _captureUniqueID) =>
            {
                try
                {
                    var uid = (_captureUniqueID == null ? string.Empty : _captureUniqueID.ToString());
                    if (!GlobleConfigurations.IsSyncProjectPathToZBrain)
                    {
                        var ret = CongfigSyncToZBrain(ConstFields.ZBRAIN_CONFIGASYNC_PROJECT_PATH, GlobleConfigurations.PackageDirectory);
                        if (!ret)
                        {
                            return;
                        }
                    }
                    Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                    HttpItem item = new HttpItem();
                    item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.AI_INTELLIGENT_REPAIR_TASK;
                    item.Method = "POST";
                    item.Timeout = 1000;
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    dic["uid"] = uid;
                    dic["path"] = Path.Combine(ConstFields.RES_STATICFOLDER_NAME, uid);
                    dic["wnd_info"] = wndInfo;
                    //item.Postdata = dic.ToJson();
                    var result = GetHtml(item, dic);
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        /*
                         {
                            "status":"success|fail",
                            "message":"错误信息，请求成功为空字符串",
                            "result":{
                                "taskId":"该修复拾取任务的任务id"
                                }
                        }

                         */
                        logger.LogInformation("SengUIDToZBrain result.Html:OK");
                        logger.LogDebug(result?.Html);
                        return;
                    }
                    else
                    {
                        logger.LogError($"SengUIDToZBrain 服务返回异常，请联系相关人员.{result?.Html}");
                        return;
                        //CVException cVException = new CVException(eErrorMessageType.Error_CVElementMatchingServiceError);
                        //throw cVException;//向上层抛出异常
                    }

                }
                catch (Exception ex)
                {
                    //Trace即可 
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "SengUIDToZBrain 服务返回异常，请联系相关人员");
                    return;
                }
            };
            Task.Run(() => action(captureUniqueID));
        }
        internal static bool CongfigSyncToZBrain(string key, object value)
        {
            try
            {
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.Timeout = 1000;

                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.CONFIG_SYNC_TO_ZBRAIN;
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["key"] = key;
                dic["value"] = value;
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogInformation($"CongfigSyncToZBrain {key}:{value} result.Html:OK");
                    JObject retObj = JObject.Parse(result.Html);
                    if (retObj.TryGetValueByPath("status", out string status))
                    {
                        if (status.Equals("success"))
                        {
                            GlobleConfigurations.IsSyncProjectPathToZBrain = true;
                        }
                    }
                    return true;
                }
                else
                {
                    logger.LogError($"SendAiRepairToZBrain{key}:{value}  服务返回异常，请联系相关人员.{result?.Html}");
                    return false;

                }

            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"SendAiRepairToZBrain {key}:{value}  执行异常，请联系相关人员");
                //throw ex;//向上层抛出异常
                return false;
            }
        }
        /// <summary>
        /// 图片相似度
        /// </summary>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <returns></returns>
        internal static float ImageSimilarity(Bitmap img1, Bitmap img2, IntPtr wndHandle, JObject areaInfo = null)
        {
            float sim = -9999;
            try
            {
                //检验CV图片对比度
                //string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempimg");
                //if (!Directory.Exists(path))
                //{
                //    Directory.CreateDirectory(path);
                //}
                //string name = DateTime.Now.Ticks.ToString();
                //img1.Save(Path.Combine(path, $@"{name}_eleShotImg.png"));
                //img2.Save(Path.Combine(path, $@"{name}_newAppShotImg.png"));


                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    logger.LogWarning("算法服务地址未同步，相似度返回1,排错时，可忽略");
                    return 1;
                }
                string img1Str = ImageHelper.Bitmap2Base64(img1);
                string img2Str = ImageHelper.Bitmap2Base64(img2);
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.IMAGE_SIMILARITY;
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["im1"] = img1Str;
                dic["im2"] = img2Str;
                dic["wndHandle"] = (int)wndHandle;
                dic["area_info"] = areaInfo;

                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("ImageSimilarity result.Html:OK");
                    JObject retObj = JObject.Parse(result.Html);

                    if (retObj.ContainsKey("status"))
                    {
                        if (retObj["status"].ToString().Equals("success"))
                        {
                            sim = retObj["result"].ToString().As<float>();
                        }
                        else
                        {
                            logger.LogInformation($"ImageSimilarity: {result?.Html}");
                        }
                    }
                }
                else
                {
                    logger.LogError($"ImageSimilarity 服务返回异常，请联系相关人员.{result?.Html}");
                }
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ImageSimilarity 执行异常，请联系相关人员");
            }
            return sim;

        }
        internal static bool NotifyGetWindowImg(IntPtr wndHandle)
        {
            try
            {
                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    logger.LogWarning("算法服务地址未同步，相似度返回1,排错时，可忽略");
                    return false;
                }
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.GET_WINDOW_IMG;
                item.Method = "POST";
                item.Timeout = 5000;
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["wndHandle"] = (int)wndHandle;
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("NotifyGetWindowImg result.Html:OK");
                    JObject retObj = JObject.Parse(result.Html);

                    if (retObj.ContainsKey("status"))
                    {
                        if (retObj["status"].ToString().Equals("success"))
                        {
                            return true;
                        }
                        else
                        {
                            logger.LogInformation($"NotifyGetWindowImg: {result?.Html}");
                            return false;
                        }
                    }
                }
                else
                {
                    logger.LogError($"NotifyGetWindowImg 服务返回异常，请联系相关人员.{result?.Html}");
                }
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "NotifyGetWindowImg 执行异常，请联系相关人员");
            }
            return false;

        }
        internal static bool NotifyGetWindowImg_ChatRpa(IntPtr wndHandle, string prevData, ref bool userLast)
        {
            return NotifyGetWindowImg_ChatRpa(wndHandle, prevData, 0, ref userLast);
        }
        internal static bool NotifyGetWindowImg_Agent(IntPtr wndHandle, string prevData, ref bool userLast)
        {
            return NotifyGetWindowImg_ChatRpa(wndHandle, prevData, 1, ref userLast);
        }
        internal static bool NotifyGetWindowImg_AiVision(IntPtr wndHandle, string prevData, ref bool userLast)
        {
            return NotifyGetWindowImg_ChatRpa(wndHandle, prevData, 2, ref userLast);
        }

        internal static bool NotifyGetWindowImg_ChatRpa(IntPtr wndHandle, string prevData, int getImgType, ref bool userLast)
        {
            string prevCVData = "[]";
            userLast = false;
            if (!string.IsNullOrEmpty(prevData))
            {
                prevCVData = prevData;
            }
            try
            {
                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    logger.LogWarning("算法服务地址未同步");
                    return false;
                }
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                string getImgRoute = getImgType == 0
                     ? CVRouteConstFields.GET_WINDOW_IMG_CHARRPA
                     : (getImgType == 1 ? CVRouteConstFields.GET_WINDOW_IMG_AGENT : CVRouteConstFields.GET_WINDOW_IMG_AI_VERSION);
                item.URL = uri.GetLeftPart(UriPartial.Authority) + getImgRoute;
                item.Method = "POST";
                item.Timeout = 5000;
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["wndHandle"] = (int)wndHandle;
                dic["prevCVData"] = prevCVData;
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("NotifyGetWindowImg result.Html:OK");
                    JObject retObj = JObject.Parse(result.Html);



                    if (retObj.TryGetValue("use_last", out var use_last)
                        && use_last.Value<bool>().Equals(true))
                    {
                        userLast = true;
                    }

                    if (retObj.TryGetValue("capture", out var capture)
                      && capture.Value<bool>().Equals(true))
                    {
                        return true;
                    }
                    else
                    {
                        logger.LogInformation($"NotifyGetWindowImg: {result?.Html}");
                        return false;
                    }
                }
                else
                {
                    logger.LogError($"NotifyGetWindowImg 服务返回异常，请联系相关人员.{result?.Html}");
                }
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "NotifyGetWindowImg 执行异常，请联系相关人员");
            }
            return false;

        }
        internal static int ElementClass(string aBitmapBase64)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic["image"] = aBitmapBase64;
            return Task.Run(async () =>
            {
                var m_ZbrainService = new ProcessCreator.ZbrainService(GlobleConfigurations.ZBrainServicePath);
                string responseString = await m_ZbrainService.ElementClass(dic.ToJson());
                int elementClass = JObject.Parse(responseString).GetValue<int>("result");
                return elementClass;
            }).Result;


            //try
            //{
            //    if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
            //    {
            //        logger.LogWarning("算法服务地址未同步，ElementClass 获取元素分类无法进行");
            //        return -1;
            //    }
            //    Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
            //    HttpItem item = new HttpItem();
            //    item.ContentType = "application/json";
            //    item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.ELEMENT_CLASS;
            //    item.Method = "POST";
            //    item.Timeout = 2000;
            //    Dictionary<string, object> dic = new Dictionary<string, object>();
            //    dic["image"] = aBitmapBase64;
            //    var result = GetHtml(item, dic);
            //    int elementClass = JObject.Parse(result.Html).GetValue<int>("result");
            //    return elementClass;
            //}
            //catch (Exception ex)
            //{
            //    //Trace即可 
            //    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ElementClass 执行异常，请联系相关人员");
            //}
            //return -1;

        }


        #region 元素记忆
        /// <summary>
        /// 桌面截图
        /// </summary>
        /// <param name="screenArea"></param>
        /// <returns></returns>
        internal static bool NotifyScreenShot(Rectangle screenArea)
        {
            try
            {
                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    logger.LogWarning("算法服务地址未同步");
                    return false;
                }
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.SCREEN_SHOT;
                item.Method = "POST";
                item.Timeout = 5000;
                Dictionary<string, object> dic = new Dictionary<string, object>();
                // screen_area:[x1,y1,x2,y2] # C#桌面截图结果
                dic["screen_area"] = new[] { screenArea.X, screenArea.Y, screenArea.X + screenArea.Width, screenArea.Y + screenArea.Height };
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("NotifyScreenShot result.Html:OK");
                    JObject retObj = JObject.Parse(result.Html);

                    if (retObj.ContainsKey("status"))
                    {
                        if (retObj["status"].ToString().Equals("success"))
                        {
                            return true;
                        }
                        else
                        {
                            logger.LogInformation($"NotifyScreenShot: {result?.Html}");
                            return false;
                        }
                    }
                }
                else
                {
                    logger.LogError($"NotifyGetWindowImg 服务返回异常，请联系相关人员.{result?.Html}");
                }
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "NotifyScreenShot 执行异常，请联系相关人员");
            }
            return false;
        }
        /// <summary>
        /// 元素记忆
        /// </summary>
        /// <param name="screenArea"></param>
        /// <returns></returns>
        internal static bool NotifyElementRecord(string appname, Rectangle elementArea, object memoryinfo)
        {
            try
            {
                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    logger.LogWarning("算法服务地址未同步");
                    return false;
                }
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.ELE_RECORD;
                item.Method = "POST";
                item.Timeout = 5000;
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["app_name"] = appname;
                dic["area_cor"] = new[] { elementArea.X, elementArea.Y, elementArea.X + elementArea.Width, elementArea.Y + elementArea.Height };
                dic["memoryinfo"] = memoryinfo;
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("NotifyScreenShot result.Html:OK");
                    JObject retObj = JObject.Parse(result.Html);

                    if (retObj.ContainsKey("status"))
                    {
                        if (retObj["status"].ToString().Equals("success"))
                        {
                            return true;
                        }
                        else
                        {
                            logger.LogInformation($"NotifyScreenShot: {result?.Html}");
                            return false;
                        }
                    }
                }
                else
                {
                    logger.LogError($"NotifyGetWindowImg 服务返回异常，请联系相关人员.{result?.Html}");
                }
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "NotifyScreenShot 执行异常，请联系相关人员");
            }
            return false;
        }

        /// <summary>
        /// 查询记忆元素
        /// </summary>
        /// <param name="screenArea"></param>
        /// <returns></returns>
        internal static bool QueryMemoryElementArea(WindowInfo windowInfo, object memoryinfo, out Rectangle area, out Rectangle remoteWndArea)
        {
            area = Rectangle.Empty;
            remoteWndArea = Rectangle.Empty;
            try
            {
                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    logger.LogWarning("算法服务地址未同步");
                    return false;
                }
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.ELE_INQUIRY;
                item.Method = "POST";
                item.Timeout = 5000;
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["wndHandle"] = windowInfo.WndHandle;
                dic["app_name"] = windowInfo.AppName;
                dic["memoryinfo"] = memoryinfo;
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("QueryMemoryElement result.Html:OK");
                    JObject retObj = JObject.Parse(result.Html);
                    if (retObj.ContainsKey("status"))
                    {
                        if (retObj["status"].ToString().Equals("success"))
                        {
                            var resultArr = retObj["result"].ToObject<int[]>();
                            area = new Rectangle(resultArr[0], resultArr[1], resultArr[2] - resultArr[0], resultArr[3] - resultArr[1]);
                            if (retObj.TryGetValueByPath("wnd_info.area", out int[] wndAreaArr))
                            {
                                remoteWndArea = RectHelper.FromAraeArr(wndAreaArr);
                            }
                            return true;
                        }
                        else
                        {
                            logger.LogInformation($"QueryMemoryElement: {result?.Html}");
                            return false;
                        }
                    }
                }
                else
                {
                    logger.LogWarning($"QueryMemoryElement 服务返回异常，请联系相关人员.{result?.Html}");
                }
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "QueryMemoryElement 执行异常，请联系相关人员");
            }
            return false;
        }
        #endregion

        #region 提取信息
        internal static string ExtractWinndowInfo(IntPtr hWnd, object query, string viewElementData)
        {
            try
            {
                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    logger.LogWarning("算法服务地址未同步");
                    return "";
                }
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.EXTRACT_INFO;
                item.Method = "POST";
                item.Timeout = 60 * 1000;
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["wndHandle"] = hWnd;
                dic["query"] = query;
                dic["web_elements"] = viewElementData;
                dic["client_tenantCode"] = GlobleConfigurations.ClientTenantCode;
                dic["tarsAgent_serviceUrl"] = GlobleConfigurations.TarsAgentServiceUrl;
                dic["tarsAgent_serviceToken"] = GlobleConfigurations.TarsAgentServiceToken;

                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("ExtractWinndowInfo result.Html:OK");
                    JObject retObj = JObject.Parse(result.Html);

                    if (retObj.ContainsKey("status"))
                    {
                        if (retObj.ContainsKey("message") && retObj["message"] != null)
                        {
                            if (!string.IsNullOrEmpty(retObj["message"].ToString()))
                            {
                                logger.LogInformation($"CV识别失败：{retObj["message"].ToString()}");
                                var cvException = new RPAException(eErrorMessageType.Error_CVElementServiceError);
                                cvException.CustomMessage = retObj["message"].ToString();
                                throw cvException;
                            }
                        }
                        if (retObj["status"].ToString().Equals("success"))
                        {
                            return retObj["result"].ToString();
                        }
                        else
                        {
                            logger.LogInformation($"ExtractWinndowInfo: {result?.Html}");
                            return "";
                        }
                    }
                }
                else
                {
                    logger.LogError($"ExtractWinndowInfo 服务返回异常，请联系相关人员.{result?.Html}");
                }
            }
            catch (RPAException rpaEx)
            {
                throw rpaEx;
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ExtractWinndowInfo 执行异常，请联系相关人员");
            }
            return "";
        }
        #endregion

        #region 获取数据采集列元素信息
        internal static string GetSimilarOrDataCollectElements(IntPtr hWnd, object taskInfo, string viewElementData, string actionName)
        {
            try
            {
                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    logger.LogWarning("算法服务地址未同步");
                    return "";
                }
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + actionName == "data_collect" ? CVRouteConstFields.DATA_COLLECT : CVRouteConstFields.CYCLE_ELES;
                item.Method = "POST";
                item.Timeout = 60 * 1000;
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["wndHandle"] = hWnd;
                dic["tsak_info"] = taskInfo;
                dic["web_elements"] = viewElementData;
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("GetSimilarOrDataCollectElements result.Html:OK");
                    JObject retObj = JObject.Parse(result.Html);

                    if (retObj.ContainsKey("status"))
                    {
                        if (retObj["status"].ToString().Equals("success"))
                        {
                            return retObj["result"].ToString();
                        }
                        else
                        {
                            logger.LogInformation($"DataCollectInfo: {result?.Html}");
                            return "";
                        }
                    }
                }
                else
                {
                    logger.LogError($"DataCollectInfo 服务返回异常，请联系相关人员.{result?.Html}");
                }
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "DataCollectInfo 执行异常，请联系相关人员");
            }
            return "";
        }
        #endregion

        #region 判断Zbrain服务是否可用
        internal static bool IsZBrainAvailable()
        {
            try
            {
                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    logger.LogWarning("算法服务地址未同步");
                    return false;
                }
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.PLUGIN_CONFIGURED_CHECK;
                item.Method = "POST";
                item.Timeout = 5000;
                Dictionary<string, object> dic = new Dictionary<string, object>();

                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("IsZBrainAvailable result.Html:OK");
                    JObject retObj = JObject.Parse(result.Html);

                    if (retObj.TryGetValueByPath<int>("code", out var code) && code == 0)
                    {
                        return true;
                    }
                }
                else
                {
                    logger.LogError($"IsZBrainAvailable 服务返回异常，请联系相关人员.{result?.Html}");
                }
            }
            catch (Exception ex)
            {
                //Trace即可 
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "IsZBrainAvailable 执行异常，请联系相关人员");
            }
            return false;
        }
        #endregion

        #region 提取有效属性

        /// <summary>
        /// 算法属性值筛选接口
        /// </summary>
        /// <param name="selector">元素属性字典列表</param>
        /// <param name="clientInfo">客户端鉴权信息（可选，如果为null则使用全局配置）</param>
        /// <returns>返回元素属性列表的JSON字符串，失败返回空字符串</returns>
        internal static List<Dictionary<string, object>> FilterAttributesByAlgorithm(List<Dictionary<string, object>> selector, string executeMode, Dictionary<string, object> clientInfo = null)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            try
            {
                // 提前返回：检查服务地址
                if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
                {
                    logger.LogWarning("算法服务地址未同步");
                    return result;
                }

                // 构建请求
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.SMART_ELEMENT_SELECTOR;
                item.Method = "POST";
                item.Timeout = 60 * 1000;

                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["seletor"] = selector;

                // 构建client_info，优先使用传入参数，否则使用全局配置
                Dictionary<string, object> clientInfoDict = clientInfo ?? new Dictionary<string, object>
                {
                    ["tarsAgent_serviceUrl"] = GlobleConfigurations.TarsAgentServiceUrl,
                    ["client_tenantCode"] = GlobleConfigurations.ClientTenantCode,
                    ["tarsAgent_serviceToken"] = GlobleConfigurations.TarsAgentServiceToken
                };
                dic["client_info"] = clientInfoDict;
                dic["api_version"] = "2.0";
                dic["execute_mode"] = executeMode;  // "auto" | "passive"

                // 发送请求
                var httpResult = GetHtml(item, dic);

                // 提前返回：检查HTTP状态码
                if (httpResult.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogError($"FilterAttributesByAlgorithm 服务返回异常，请联系相关人员.{httpResult?.Html}");
                    return result;
                }

                logger.LogDebug("FilterAttributesByAlgorithm result.Html:OK");
                JObject retObj = JObject.Parse(httpResult.Html);

                // 提前返回：检查code字段
                if (!retObj.ContainsKey("code"))
                {
                    logger.LogWarning($"FilterAttributesByAlgorithm: 返回结果格式异常，未找到code字段. {httpResult?.Html}");
                    return result;
                }

                string code = retObj["code"]?.ToString();
                string msg = retObj["msg"]?.ToString();

                // 提前返回：检查业务错误码
                if (code != "0200")
                {
                    // 业务错误
                    if (!string.IsNullOrEmpty(msg))
                    {
                        logger.LogInformation($"算法属性值筛选失败：{msg}");
                        eErrorMessageType errorType = GetErrorTypeByCode(code);
                        var rpaException = new RPAException(errorType);
                        rpaException.CustomMessage = msg;
                        throw rpaException;
                    }
                    logger.LogInformation($"FilterAttributesByAlgorithm: {httpResult?.Html}");
                    return result;
                }

                // 提前返回：检查data字段
                if (!retObj.ContainsKey("data") || retObj["data"] == null)
                {
                    logger.LogWarning("FilterAttributesByAlgorithm: 返回结果中未找到data字段");
                    return result;
                }

                JObject dataObj = retObj["data"] as JObject;

                // 提前返回：检查data对象和element_attributes字段
                if (dataObj == null || !dataObj.ContainsKey("element_attributes"))
                {
                    logger.LogWarning("FilterAttributesByAlgorithm: data中未找到element_attributes字段");
                    return result;
                }

                JToken elementAttributesToken = dataObj["element_attributes"];

                // 提前返回：检查element_attributes是否为数组
                if (elementAttributesToken == null || elementAttributesToken.Type != JTokenType.Array)
                {
                    logger.LogWarning("FilterAttributesByAlgorithm: element_attributes不是数组类型");
                    return result;
                }

                // 反序列化
                try
                {
                    result = elementAttributesToken.ToObject<List<Dictionary<string, object>>>() ?? new List<Dictionary<string, object>>();
                    logger.LogDebug($"FilterAttributesByAlgorithm: 成功解析{result.Count}个元素属性");
                    return result;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "FilterAttributesByAlgorithm: 反序列化element_attributes失败");
                    return result;
                }
            }
            catch (RPAException rpaEx)
            {
                throw rpaEx;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "FilterAttributesByAlgorithm 执行异常，请联系相关人员");
                return result;
            }
        }

        /// <summary>
        /// 根据返回的code值获取对应的错误类型
        /// </summary>
        /// <param name="code">错误码</param>
        /// <returns>对应的错误类型</returns>
        private static eErrorMessageType GetErrorTypeByCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return eErrorMessageType.Error_SmartSelector_BuildFail; // 默认错误类型
            }

            // TODO: 根据实际业务需求补充不同code对应的错误类型
            // 示例：
            switch (code)
            {

                case "0203":
                    return eErrorMessageType.Error_SmartSelector_Insufficient_Resource_Points; // 临时默认值，待补充
                case "0201":
                case "0202":
                case "0204":
                case "0205":
                    // TODO: 补充鉴权错误对应的类型
                    // return eErrorMessageType.Error_AuthenticationFailed;
                    return eErrorMessageType.Error_SmartSelector_BuildFail; // 临时默认值，待补充

                default:
                    // 默认返回CV元素服务错误
                    return eErrorMessageType.Error_SmartSelector_BuildFail;
            }
        }

        #endregion
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
            dicParams_sort.Remove("t");
            item.Postdata = dicParams_sort.ToJson();
            item.URL = $"{item.URL}{(item.URL.IndexOf("?") > 0 ? "&" : "?")}sign={sign}&t={t}";
            logger.LogDebug($"CV URL{item.URL}");
            return GetHtml(item);
        }
        private static HttpResult GetHtml(HttpItem item)
        {
            item.Header.Add("CLIENT-USER", GlobleConfigurations.ClientUser);
            item.Header.Add("CLIENT-MAC", GlobleConfigurations.ClientMac);
            item.Header.Add("engineCode", GlobleConfigurations.EngineCode);
            return new HttpHelper().GetHtml(item);
        }
    }
}

