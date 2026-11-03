using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.OcrOffline.Params;
using II.RPA.Plugin.OcrOffline.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.OcrOffline.Plugins
{
    /// <summary>
    /// 根据selector 传递全屏截图与修正分辨率更改后的坐标 传递给算法解析并透传表格数据
    /// </summary>
    public class GetOcrTableDataBySelectorAILimit: OcrOfflineComponentBase<ParamGetOcrTableDataBySelector>
    {
        public GetOcrTableDataBySelectorAILimit(II.RPA.Plugin.Abstractions.IPluginInvoker pluginInvoker) : base(pluginInvoker) { }
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.OcrTableAnalyse;

        protected override void ValidateParameter(ParamGetOcrTableDataBySelector paramModel)
        {

            //paramModel = new ParamGetOcrTableDataBySelector();

            try
            {
                logger.LogDebug($"GetOcrTableDataBySelectorAILimit  ParamIn:{paramModel.ToJson()} Version:2");
                logger.LogDebug($"GetOcrTableDataBySelectorAILimit  ParamOut:{paramModel.ToJson()} Version:2");

                if (string.IsNullOrEmpty(paramModel.IsFromSelector.ToString()) || paramModel.IsFromSelector == false)
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }

                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"ExecuteCV OCR表格解析AILimit参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        /// <summary>
        /// 根据纯selector查找窗体元素并置顶 同时获取窗体元素的Rect信息 用来后续截图
        /// </summary>
        /// <param name="strSelectorJson"></param>
        internal async Task<FindElementResult> FindElementAndGetRectAsync(string strSelectorJson, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            FindElementResult _FindElementResult = new FindElementResult();
            var item = new JObject
                    {{"selector", strSelectorJson}}.ToString();

            ScreenShootStruct _ScreenShootStruct = new ScreenShootStruct();
            logger.LogDebug($"FindElementAndGetRect Param:{item}");
            string _strResult = await PluginInvoker.InvokeAsync(
                new II.RPA.Plugin.Abstractions.PluginRoute("Element", "GetElementForOcrTable"), item).ConfigureAwait(false);
            logger.LogInformation($"GetElementForOcrTable 接口返回:{_strResult}");
            var JsonObject = _strResult.ToObject<JObject>();
            string strJsonRectangle = string.Empty;

            if (JsonObject?["code"] != null && JsonObject?["message"] != null && JsonObject?["info"] != null)
            {
                if (Convert.ToInt32(JsonObject?["code"].ToString()) == 0)
                {
                    strJsonRectangle = JsonObject?["info"].ToString();

                    _FindElementResult = strJsonRectangle.ToObject<FindElementResult>();
                    return _FindElementResult;
                }
            }
            return _FindElementResult;//失败返回空对象
        }

        /// <summary>
        /// 解析engine传入Json 
        /// </summary>
        /// <param name="strJson"></param>
        /// <param name="_ScreenShootStruct"></param>
        /// <returns></returns>
        internal async Task<(ScreenShootStruct ScreenShoot, FindElementResult FindElement)> GetJsonDataAsync(string strJson, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            logger.LogDebug($"解析selector：{strJson}");
            ScreenShootStruct _ScreenShootStruct = new ScreenShootStruct();
            FindElementResult _FindElementResult = await FindElementAndGetRectAsync(strJson, context).ConfigureAwait(false);
            if (!_FindElementResult.ElementFound)
            {
                logger.LogError($"OCR表格解析执行失败 目标元素窗体查找失败");
                throw new Exception("OCR表格解析未获取目标窗体".I18n("controlType_GetOcrTableDataBySelectorAILimit_117DB36C"));
            }

            int x = 0, y = 0, width = 0, height = 0, screenWidth = 0, screenHeight = 0;
            double scaleX = 1.0, scaleY = 1.0;

            JToken _jTotalToken = JToken.Parse(strJson);
            JArray LocationArray = null;
            JObject obj = null;

            string JsonCommon = _jTotalToken["commonProps"].ToString();
            var _CommonProps = JsonConvert.DeserializeObject<CommonProps>(JsonCommon);
            _ScreenShootStruct.ResDir = _CommonProps.ResDirPath;
            string JsonProps = _jTotalToken["selector"].ToString();
            JToken token2 = JToken.Parse(JsonProps);
            int PointJsonIndex = token2.Count();
            LocationArray = JArray.Parse(token2[PointJsonIndex - 1]["props"].ToString());
            foreach (var item in LocationArray)
            {
                if (item["name"].ToString() == "x")
                {
                    x = Convert.ToInt32(item["value"].ToString().Replace("\\", "").Replace("\"", ""));
                }
                else if (item["name"].ToString() == "y")
                {
                    y = Convert.ToInt32(item["value"].ToString().Replace("\\", "").Replace("\"", ""));
                }
                else if (item["name"].ToString() == "width")
                {
                    width = Math.Abs(Convert.ToInt32(item["value"].ToString().Replace("\\", "").Replace("\"", "")));
                }
                else if (item["name"].ToString() == "height")
                {
                    height = Math.Abs(Convert.ToInt32(item["value"].ToString().Replace("\\", "").Replace("\"", "")));
                }
                else if (item["name"].ToString() == "scaleX")
                {
                    scaleX = Convert.ToDouble(item["value"].ToString().Replace("\\", "").Replace("\"", ""));
                }
                else if (item["name"].ToString() == "scaleY")
                {
                    scaleY = Convert.ToDouble(item["value"].ToString().Replace("\\", "").Replace("\"", ""));
                }
                else if (item["name"].ToString() == "screenWidth")
                {
                    screenWidth = Math.Abs(Convert.ToInt32(item["value"].ToString().Replace("\\", "").Replace("\"", "")));
                }
                else if (item["name"].ToString() == "screenHeight")
                {
                    screenHeight = Math.Abs(Convert.ToInt32(item["value"].ToString().Replace("\\", "").Replace("\"", "")));
                }
            }

            _ScreenShootStruct.StartX = x;
            _ScreenShootStruct.StartY = y;
            _ScreenShootStruct.Width = width;
            _ScreenShootStruct.Height = height;
            _ScreenShootStruct.ScaleX = scaleX;
            _ScreenShootStruct.ScaleY = scaleY;
            _ScreenShootStruct.ScreenHeight = screenHeight;
            _ScreenShootStruct.ScreenWidth = screenWidth;

            logger.LogInformation($"解析selector正常");
            return (_ScreenShootStruct, _FindElementResult);
        }

        #region  Excute 执行函数

        protected override async Task<JMessage> ExecuteAsync(ParamGetOcrTableDataBySelector paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            logger.LogInformation($"GetOcrTableDataBySelector Start:{paramModel.Selector} Version:1.12");
            try
            {
                #region 解析selector
                ScreenShootStruct CScreenShootStruct = new ScreenShootStruct();
                FindElementResult _FindElementResult = new FindElementResult();
                var selectorData = await GetJsonDataAsync(paramModel.Selector2.ToJson(), context).ConfigureAwait(false);
                CScreenShootStruct = selectorData.ScreenShoot;
                _FindElementResult = selectorData.FindElement;
                string strTableAlgInfoJson = string.Empty;
                ReadCVData(CScreenShootStruct.ResDir, out strTableAlgInfoJson);
                CScreenShootStruct.TableAlgInfo = strTableAlgInfoJson;

                int iStartX = 0, iStartY = 0, iFrameworkWidth = 0, iFrameworkHeight = 0, iScreenWidth = 0, iScreenHeight = 0;
                double dScaleX = 1.0, dScaleY = 1.0;//Selector缩放
                string strTableAlgInfo = string.Empty;//Selector表格附件信息

                if (CScreenShootStruct != null)
                {
                    iStartX = CScreenShootStruct.StartX;
                    iStartY = CScreenShootStruct.StartY;
                    iFrameworkWidth = CScreenShootStruct.Width;//Math.Abs((int)obj.GetValue("width"));
                    iFrameworkHeight = CScreenShootStruct.Height;//Math.Abs((int)obj.GetValue("height"));
                    iScreenWidth = CScreenShootStruct.ScreenWidth;
                    iScreenHeight = CScreenShootStruct.ScreenHeight;
                    dScaleX = CScreenShootStruct.ScaleX;
                    dScaleY = CScreenShootStruct.ScaleY;
                    strTableAlgInfo = CScreenShootStruct.TableAlgInfo;
                }
                else
                {
                    logger.LogError($"OCR Table Selector Invalid:{paramModel.Selector}");
                    return OcrTableError.GetErrMessage(eErrorMessageType.Error_GetNoTable, $"OCR Table Selector Invalid:{paramModel.Selector}");
                }
                #endregion

                ImageDataEx CImageData = new ImageDataEx();

                using (Bitmap bitmap = UiCommon.GetDesktop())
                {
                    //#if Trace
                    string fileFullName = Path.Combine(GlobleConfigurations.PackageDirectory, CScreenShootStruct.ResDir, $"{DateTime.Now.ToString("yyyyMMddhhmmss")}.jpg");
                    bitmap.Save(fileFullName);
                    //#endif
                    logger.LogInformation($"窗体Rect:{_FindElementResult.WindowX},{_FindElementResult.WindowY},{_FindElementResult.WindowWidth},{_FindElementResult.WindowHeight}");

                    var bitmapOfWindow = ImageHelper.CaptureImage(bitmap, new System.Drawing.Rectangle(_FindElementResult.WindowX, _FindElementResult.WindowY, _FindElementResult.WindowWidth, _FindElementResult.WindowHeight));
                    CImageData.Base64 = ImageHelper.Bitmap2Base64(bitmapOfWindow);
                }

                #region 根据selector记录的信息重新计算当前截图坐标
                logger.LogDebug($"修正前坐标：X:{iStartX} Y:{iStartY} Width:{iFrameworkWidth} Height:{iFrameworkHeight}");

                int newStartX = (int)(iStartX * NativeMethods.GetScreenSize().X / iScreenWidth * NativeMethods.ScaleX / dScaleX);
                int newStartY = (int)(iStartY * NativeMethods.GetScreenSize().Y / iScreenHeight * NativeMethods.ScaleX / dScaleY);
                int newWidth = (int)(iFrameworkWidth * NativeMethods.ScaleX / dScaleX);
                int newHeight = (int)(iFrameworkHeight * NativeMethods.ScaleY / dScaleY);

                CImageData.StartPoint = new Point(newStartX, newStartY);
                CImageData.EndPoint = new Point(newStartX + newWidth, newStartY + newHeight);

                logger.LogDebug($"修正后坐标：X:{newStartX} Y:{newStartY} Width:{newWidth} Height:{newHeight}");
                #endregion

                CImageData.TableAlgInfo = strTableAlgInfo;
                logger.LogDebug($"表格附件信息：{ CImageData.TableAlgInfo}");
                string cvResults = string.Empty;
                CursorHelper.SetCursorBusy(true);
                try
                {
                    logger.LogInformation($"GetOcrTableDataBySelector 表格拾取执行调用接口开始");

                    CommonHttpParam _CommonHttpParam = new CommonHttpParam();
                    SaasParam _SaasParam = new SaasParam("rpa-table.table_ocr_service", "1.0", paramModel.Token);

                    string imgStr = CImageData?.Base64?.ToString();

                    #region 测试保存实际截图
                    if (false)
                    {
                        PictureHelper CPictureHelper = new PictureHelper();
                        string ImageDir = CPictureHelper.GetCurrentPackageResDirPath();
                        string ImageName = Localize($"执行截图{DateTime.Now.ToString("yyyyMMddHHmmss")}", "GetOcrTableDataBySelectorAILimit_3BDFCF67");
                        PictureHelper.Base64ToImage(imgStr, ImageDir, ImageName);
                    }

                    #endregion

                    string _strTableAlgInfo = CImageData?.TableAlgInfo?.ToString();
                    List<List<int>> pointList = new List<List<int>> { new List<int> { CImageData.StartPoint.X, CImageData.StartPoint.Y },
                            new List<int> { CImageData.EndPoint.X, CImageData.EndPoint.Y } };

                    _CommonHttpParam.ParamCollection.Add("image", imgStr);
                    _CommonHttpParam.ParamCollection.Add("points", pointList);
                    _CommonHttpParam.ParamCollection.Add("table_algo_info", _strTableAlgInfo);

                    msg.Info = JsonConvert.SerializeObject(_CommonHttpParam.ParamCollection);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "表格解析执行发生异常");
                    return OcrTableError.GetErrMessage(eErrorMessageType.Error_GetNoTable, Localize($"表格解析执行发生异常，详情请观察日志或者联系技术人员处理", "GetOcrTableDataBySelectorAILimit_825DE9E1"));
                }
                finally
                {
                    CursorHelper.SetCursorBusy(false);
                }

            }
            catch (Exception ex)
            {
                if (ex.Message == "OCR表格解析未获取目标窗体")
                {
                    return OcrTableError.GetErrMessage(eErrorMessageType.Error_NotFoundElement, Localize($"OCR表格解析未获取目标窗体", "GetOcrTableDataBySelectorAILimit_AF153EBE"));
                }
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetOcrTableDataBySelector 执行异常");
                return OcrTableError.GetErrMessage(eErrorMessageType.Error_GetNoTable, Localize($"表格解析拾取执行异常:{ex}", "GetOcrTableDataBySelectorAILimit_CCE8B230"));
            }
            return msg;
        }
        #endregion

        internal bool ReadCVData(string resDirPath, out string strTableAlgInfoJson)
        {
            strTableAlgInfoJson = string.Empty;
            string text = UiCommon.ReadStaticSelectorData(resDirPath, "OcrTableAlgInfoData.json");
            JObject jObj = JObject.Parse(text);
            jObj.TryGetValue("CVData", StringComparison.OrdinalIgnoreCase, out var val);
            if (val != null)
            {
                strTableAlgInfoJson = val.ToString();
                return true;
            }
            return false;
        }

    }
}

