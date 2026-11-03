using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.PluginBase;
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
using II.RPA.Logging.Formatting;

namespace II.RPA.Plugin.OcrOffline.Plugins
{
    /// <summary>
    /// 根据selector 传递全屏截图与修正分辨率更改后的坐标 传递给算法解析并透传表格数据
    /// </summary>
    public class MakeOcrTableSelector : OcrOfflineComponentBase<ParamMakeOcrTableSelector>
    {
        public MakeOcrTableSelector(II.RPA.Plugin.Abstractions.IPluginInvoker pluginInvoker) : base(pluginInvoker) { }
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.OcrTableAnalyse;

        protected override void ValidateParameter(ParamMakeOcrTableSelector paramModel)
        {
            try
            {
                GlobleConfigurations.CaptureUniqueID = RandomHelper.RandomString(10);
                logger.LogDebug($"MakeOcrTableSelector  ParamIn:{paramModel.ToJson()} Version:2");
                logger.LogDebug($"MakeOcrTableSelector  ParamOut:{paramModel.ToJson()} Version:2");
               

                if (string.IsNullOrEmpty(paramModel.ImgPath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                else
                {
                    string path = Path.GetDirectoryName(paramModel.ImgPath);
                    string fileName = Path.GetFileName(paramModel.ImgPath);
                    paramModel.ImgPath = Path.Combine(path, GlobleConfigurations.CaptureUniqueID, fileName);
                }
                
            }
            catch (Exception ex)
            {
                logger.LogError($"ExecuteCV OCR表格解析参数格式不符合要求：{paramModel.ToJson()} Exception:{ex.ToString()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        #region
        protected override async Task<JMessage> ExecuteAsync(ParamMakeOcrTableSelector paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            logger.LogDebug($"MakeOcrTableSelector Run:{paramModel.ToJson()} Version:1.11");
            JMessage msg = new JMessage();
            ImageData CImageData = new ImageData();
            logger.LogDebug($"TakeScreenshot:{paramModel.ImgPath} Version:1.10");
            ScreenShootStruct _ScreenShootStruct = new ScreenShootStruct();
            string _ElementSelector = string.Empty;
            try
            {
                var screenshot = await TakeScreenshotAsync(paramModel.ImgPath, context);
                _ScreenShootStruct = screenshot.ScreenShoot;
                _ElementSelector = screenshot.Selector;
                _ElementSelector = ExtractViewData.RetrieveOriginSelector(_ElementSelector);
                logger.LogDebug($"strSelector:{_ElementSelector}");
            }
            catch (Exception ex)
            {
                logger.LogError($"TakeScreenshot EX:{ex}");
                if (ex.ToString().Contains(eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage()))
                {
                    return OcrTableError.GetErrMessage(eErrorMessageType.Error_USERCLOSE);
                }
                else
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"拾取发生异常A1");
                    return OcrTableError.GetErrMessage(eErrorMessageType.Error_GetTableDataFail, $"{ex}");
                }
            }

            CursorHelper.SetCursorBusy(true);
            string cvResults = string.Empty;
            try
            {
                if (_ScreenShootStruct.IsValid)
                {
                    CImageData.Base64 = _ScreenShootStruct.Base64;
                    CImageData.StartPoint = new Point(_ScreenShootStruct.StartX, _ScreenShootStruct.StartY);
                    CImageData.EndPoint = new Point(_ScreenShootStruct.StartX + _ScreenShootStruct.Width,
                        _ScreenShootStruct.StartY + _ScreenShootStruct.Height);
                }
                else
                {
                    return OcrTableError.GetErrMessage(eErrorMessageType.Error_GetTableDataFail, "截屏获取区域信息失败".I18n("MakeOcrTableSelector_356FCCE4"));
                }

                logger.LogInformation("表格拾取调用服务 MakeOcrTableSelector");
                if (InternetStateHelper.IsSaasServiceAvailable() && InternetStateHelper.IsTokenAvailable(paramModel.Token))
                {
                    logger.LogInformation("运行模式:进入Saas模式");
                    CommonHttpParam _CommonHttpParam = new CommonHttpParam();
                    SaasParam _SaasParam = new SaasParam("rpa-table.table_pickup", "1.0", paramModel.Token);
                    
                    string imgStr = CImageData?.Base64?.ToString();//ImageHelper.Bitmap2Base64(img);

                    #region 测试保存实际截图
                    if (false)
                    {
                        PictureHelper CPictureHelper = new PictureHelper();
                        string ImageDir = CPictureHelper.GetCurrentPackageResDirPath();
                        string ImageName = Localize($"拾取截图{DateTime.Now.ToString("yyyyMMddHHmmss")}", "MakeOcrTableSelector_73EB5CCA");
                        PictureHelper.Base64ToImage(imgStr, ImageDir, ImageName);
                    }
                    #endregion

                    List<List<int>> pointList = new List<List<int>> { new List<int> { CImageData.StartPoint.X, CImageData.StartPoint.Y },
                        new List<int> { CImageData.EndPoint.X, CImageData.EndPoint.Y } };

                    _CommonHttpParam.ParamCollection.Add("image", imgStr);
                    _CommonHttpParam.ParamCollection.Add("points", pointList);

                    string ResultMessage = string.Empty;
                    string ErrorMessage = string.Empty;
                    if (SaasAuthenticationHelper.RequestSaasService(_CommonHttpParam, _SaasParam, ref ResultMessage, ref ErrorMessage))
                    {
                        cvResults = ResultMessage;
                        logger.LogInformation($"表格拾取返回结果 {cvResults}");
                        if (InternetStateHelper.IsResponceTokenErr(ResultMessage))
                        {
                            logger.LogInformation("检测到saas鉴权失败 运行模式:进入离线模式");
                            paramModel.Server = InternetStateHelper.LocalAlgOcrPickUpUrl;
                            if (!OcrHelper.GetOCRTableResult(CImageData, paramModel.Server, ref cvResults, ref msg))
                            {
                                return msg;
                            }
                        }
                    }
                    else
                    {
                        //NLogger.Default.Error($"拾取执行表格解析失败:{ErrorMessage}");
                        //msg = OcrTableError.GetErrMessage(eErrorMessageType.Error_GetTableDataFailExcute);
                        //return msg;

                        if (InternetStateHelper.IsResponceTokenErr(ResultMessage)|| InternetStateHelper.IsResponceTokenErr(ErrorMessage))
                        {
                            logger.LogInformation("检测到saas鉴权失败 运行模式:进入离线模式");
                            paramModel.Server = InternetStateHelper.LocalAlgOcrPickUpUrl;
                            if (!OcrHelper.GetOCRTableResult(CImageData, paramModel.Server, ref cvResults, ref msg))
                            {
                                return msg;
                            }
                        }
                        else
                        {
                            logger.LogInformation($"拾取执行表格解析失败:{ErrorMessage}");
                            msg = OcrTableError.GetErrMessage(eErrorMessageType.Error_GetTableDataFailExcute);
                            return msg;
                        }
                    }
                }
                else
                {
                    logger.LogInformation("运行模式:进入离线模式");
                    paramModel.Server = InternetStateHelper.LocalAlgOcrPickUpUrl;
                    if (!OcrHelper.GetOCRTableResult(CImageData, paramModel.Server, ref cvResults, ref msg))
                    {
                        return msg;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取发生异常A2");
            }
            finally
            {
                CursorHelper.SetCursorBusy(false);
            }

#if Trace
            using (FileStream fs = File.Open("result.txt", FileMode.OpenOrCreate))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(cvResults);
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
                //cvResults = Encoding.UTF8.GetString(bytes);
            }
#endif
            object _TableAlgInfo = new object();
            string _ErrorMsg = string.Empty;

            if (GetTableAlgInfo(cvResults, ref _TableAlgInfo, ref _ErrorMsg))
            {
                msg.Info = MakeSelector(_ScreenShootStruct, _TableAlgInfo, _ElementSelector);
            }
            else
            {
                return OcrTableError.GetErrMessage(eErrorMessageType.Error_GetNoTable, Localize($"解析表格附加信息失败:{_ErrorMsg}", "MakeOcrTableSelector_B6487EF2"));
            }
            return msg;
        }
        #endregion 

        public string ImagePath { get; set; }

        internal async Task<(ScreenShootStruct ScreenShoot, string Selector)> TakeScreenshotAsync(string strImgPath, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            ImagePath = strImgPath;
            logger.LogDebug($"TakeScreenshot Param:{ImagePath}");
            //{ "uiSelector",uiSelector }
            var data = new
            {
                filePath = strImgPath
                //imgPath = strImgPath
            };

            var item = new JObject { { "filePath", strImgPath } }.ToString();
            ScreenShootStruct _ScreenShootStruct = new ScreenShootStruct();
            logger.LogDebug($"TakeScreenshot Param:{LogBlock.Object(data)}");
            logger.LogDebug($"TakeScreenshot filePath:{strImgPath}");
            string _strResult = await PluginInvoker.InvokeAsync(
                new II.RPA.Plugin.Abstractions.PluginRoute("Element", "CaptureCVForOcrTable"), item).ConfigureAwait(false);

            var JsonObject = _strResult.ToObject<JObject>();
            string strJsonRectangle = string.Empty;


            if (JsonObject?["code"] != null && JsonObject?["message"] != null && JsonObject?["info"] != null)
            {
                if (Convert.ToInt32(JsonObject?["code"].ToString()) == 0)
                {
                    strJsonRectangle = JsonObject?["info"].ToString();
                }
                else
                {
                    string strMsg = JsonObject?["message"].ToString();
                    logger.LogError($"OcrTable GetScreenShootFail:{strMsg}");
                    throw new Exception($"OcrTable GetScreenShootFail:{strMsg}");
                }
            }
            else
            {
                return (_ScreenShootStruct, string.Empty);
            }
            var CRectangle = strJsonRectangle.ToObject<Utils.Rectangle>();

            if (CRectangle != null)
            {
                _ScreenShootStruct.StartX = CRectangle.X;
                _ScreenShootStruct.StartY = CRectangle.Y;
                _ScreenShootStruct.Width = CRectangle.Width;
                _ScreenShootStruct.Height = CRectangle.Height;
            }
            else
            {
                return (_ScreenShootStruct, string.Empty);
            }

            string strSelector = CRectangle.Selector;

            _ScreenShootStruct.ScreenWidth = NativeMethods.GetScreenSize().X;
            _ScreenShootStruct.ScreenHeight = NativeMethods.GetScreenSize().Y;
            _ScreenShootStruct.ScaleX = NativeMethods.ScaleX;
            _ScreenShootStruct.ScaleY = NativeMethods.ScaleY;

            using (Bitmap bitmap = UiCommon.GetDesktop())
            {
#if Trace
                bitmap.Save("11.jpg");
#endif
                logger.LogDebug($"窗体Rect:{CRectangle.WindowsX},{CRectangle.WindowsY},{CRectangle.WindowsWidth},{CRectangle.WindowsHeight}");
                logger.LogDebug($"截图Rect:{CRectangle.X},{CRectangle.Y},{CRectangle.Width},{CRectangle.Height}");
                logger.LogDebug($"最后截图:{CRectangle.X + CRectangle.WindowsX},{CRectangle.Y + CRectangle.WindowsY},{CRectangle.Width},{CRectangle.Height}");
                var bitmapOfWindow = ImageHelper.CaptureImage(bitmap, new System.Drawing.Rectangle(CRectangle.WindowsX, CRectangle.WindowsY, CRectangle.WindowsWidth, CRectangle.WindowsHeight));
                _ScreenShootStruct.Base64 = ImageHelper.Bitmap2Base64(bitmapOfWindow);
            }

            _ScreenShootStruct.IsValid = true;
            return (_ScreenShootStruct, strSelector);
        }

        #region 获取表格返回数据中的算法附加信息
        internal bool GetTableAlgInfo(string _strJsonResultFromServer, ref object _TableAlgInfo, ref string _strErrorMsg)
        {
            _TableAlgInfo = string.Empty;
            bool bRet = false;
            bool bIsSuccess = false;
            _strErrorMsg = string.Empty;

            try
            {
                var obj = JsonConvert.DeserializeObject<JToken>(_strJsonResultFromServer);

                if (!string.IsNullOrEmpty(obj?["status"]?.ToString()))
                {
                    bIsSuccess = (obj?["status"]?.ToString().ToLower() == ("success"));
                }

                if (!bIsSuccess)
                {
                    if (!string.IsNullOrEmpty(obj?["message"]?.ToString()))
                    {
                        _strErrorMsg = obj?["message"]?.ToString();
                    }

                    logger.LogError($"OCR Table Result Analyse Fail:{_strErrorMsg}");
                    return false;
                }

                if (string.IsNullOrEmpty(obj?["result"]?.ToString()))
                {
                    logger.LogError($"OCR Table Result Get No Value");
                    _strErrorMsg = "Get No Value";
                    return false;
                }

                var objResult = JsonConvert.DeserializeObject<JToken>(obj?["result"]?.ToString());

                if (string.IsNullOrEmpty(objResult?["table_algo_info"]?.ToString()))
                {
                    logger.LogError($"OCR Table Result Get No Value");
                    _strErrorMsg = "Get No Value For Table Structure";

                    return false;
                }

                var jsonObject = (objResult?["table_algo_info"]?.ToString()).ToObject<object>();
                //_TableAlgInfo = jsonObject.ToJson();
                _TableAlgInfo = jsonObject;
                //_TableAlgInfo = JsonConvert.SerializeObject((object)(objResult?["table_algo_info"].ToString()));
                bRet = true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取发生异常A2:");
                _strErrorMsg = $"Get Table Data EX:{ex.Message}";
            }

            return bRet;
        }
        #endregion

        internal string MakeSelector(ScreenShootStruct _ScreenShootStruct, object _TableAlgInfo, string _strElementSelector)
        {
            //JArray jarray = new JArray();
            JArray jarray = JArray.Parse(_strElementSelector);
            JObject jobj = new JObject();
            jobj.Add("name", "cvelement");

            Props x = new Props()
            {
                Name = "x",
                Value = _ScreenShootStruct.StartX.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"

            };
            Props y = new Props()
            {
                Name = "y",
                Value = _ScreenShootStruct.StartY.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"

            };
            Props Width = new Props()
            {
                Name = "width",
                Value = _ScreenShootStruct.Width.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"

            };
            Props Height = new Props()
            {
                Name = "height",
                Value = _ScreenShootStruct.Height.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };
            Props ScaleX = new Props()
            {
                Name = "scaleX",
                Value = _ScreenShootStruct.ScaleX.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };
            Props ScaleY = new Props()
            {
                Name = "scaleY",
                Value = _ScreenShootStruct.ScaleY.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };
            Props ScreenWidth = new Props()
            {
                Name = "screenWidth",
                Value = _ScreenShootStruct.ScreenWidth.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };
            Props ScreenHeight = new Props()
            {
                Name = "screenHeight",
                Value = _ScreenShootStruct.ScreenHeight.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };

            JArray array = new JArray();
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(x)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(y)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Width)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Height)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ScaleX)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ScaleY)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ScreenWidth)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ScreenHeight)));
            jobj.Add("props", array);
            jarray.Add(jobj);

            string strSelector = GetSelector(jarray.ToJson(), "CV", ImagePath);

            SaveCVData(_TableAlgInfo);
            return strSelector;
        }

        internal string MakeSelector(ScreenShootStruct _ScreenShootStruct, string _TableAlgInfo)
        {
            JArray jarray = new JArray();
            JObject jobj = new JObject();
            jobj.Add("name", "cv");
            //var selectorProp = new
            //{
            //    Name = "cv",
            //    props = new List<Props>()
            //};
            Props x = new Props()
            {
                Name = "x",
                Value = _ScreenShootStruct.StartX.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"

            };
            Props y = new Props()
            {
                Name = "y",
                Value = _ScreenShootStruct.StartY.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"

            };
            Props Width = new Props()
            {
                Name = "width",
                Value = _ScreenShootStruct.Width.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"

            };
            Props Height = new Props()
            {
                Name = "height",
                Value = _ScreenShootStruct.Height.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };
            Props ScaleX = new Props()
            {
                Name = "scaleX",
                Value = _ScreenShootStruct.ScaleX.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };
            Props ScaleY = new Props()
            {
                Name = "scaleY",
                Value = _ScreenShootStruct.ScaleY.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };
            Props ScreenWidth = new Props()
            {
                Name = "screenWidth",
                Value = _ScreenShootStruct.ScreenWidth.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };
            Props ScreenHeight = new Props()
            {
                Name = "screenHeight",
                Value = _ScreenShootStruct.ScreenHeight.ToString(),
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };
            JArray array = new JArray();
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(x)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(y)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Width)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Height)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ScaleX)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ScaleY)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ScreenWidth)));
            array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ScreenHeight)));
            jobj.Add("props", array);
            jarray.Add(jobj);

            string strSelector = GetSelector(jarray.ToJson(), "cv", ImagePath);
            SaveCVData(_TableAlgInfo);
            return strSelector;
        }

        internal static string GetSelector(string selector, string type, string strImgPath)
        {
            RPACommon common = new RPACommon();
            common.CommonProps = new CommonProps();
            common.CommonProps.GUIType = type;
            common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
            common.CommonProps.filePath = strImgPath;
            common.Selector = string.IsNullOrEmpty(selector.ToString()) ? new JArray() : JArray.Parse(selector);
            common.ElementDetail.ElementId = GlobleConfigurations.CaptureUniqueID;
            return JsonConvert.SerializeObject(common);
        }

        /// <summary>
        /// 将表格附加信息保存到本地文件 以便于执行接口调用
        /// </summary>
        /// <param name="_TableAlgInfoData"></param>
        private void SaveCVData(object _TableAlgInfoData)
        {
            JObject jObj = new JObject();
            jObj.Add(new JProperty("CVData", _TableAlgInfoData.ToJson()));
            UiCommon.SaveStaticSelectorData(GlobleConfigurations.CaptureUniqueID, "OcrTableAlgInfoData.json", jObj.ToString());
        }

        private void SaveCVData(string strJson)
        {
            JObject jObj = new JObject();
            jObj.Add(new JProperty("CVData", strJson));
            UiCommon.SaveStaticSelectorData(GlobleConfigurations.CaptureUniqueID, "OcrTableAlgInfoData.json", jObj.ToString());
        }


    }
}

