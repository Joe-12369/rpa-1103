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
    /// 根据表格图片路径 获取图片数据并 调用OCR表格拾取接口获取表格数据
    /// </summary>
    public class GetOcrTableDataByImg : OcrOfflineComponentBase<ParamGetOcrTableDataByImg>
    {
        public GetOcrTableDataByImg(II.RPA.Plugin.Abstractions.IPluginInvoker pluginInvoker) : base(pluginInvoker) { }
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ExecuteCV;

        protected override void ValidateParameter(ParamGetOcrTableDataByImg paramModel)
        {
            try
            {
                logger.LogDebug($"GetOcrTableDataByImg  ParamIn:{paramModel.ToJson()} Version:2");
                logger.LogDebug($"GetOcrTableDataByImg  ParamOut:{paramModel.ToJson()} Version:2");
                if (string.IsNullOrEmpty(paramModel.ImagePath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }

                //if (string.IsNullOrEmpty(paramModel.Server))
                //{
                //    return false;
                //}

                //if (string.IsNullOrEmpty(paramModel.Token))
                //{
                //bool bMark = false;
                //#if Trace
                //bMark = true;
                //#endif
                //return bMark;
                //}
            }
            catch (Exception ex)
            {
                logger.LogError($"ExecuteCV OCR表格解析参数格式不符合要求：{paramModel.ToJson()} Exception:{ex.ToString()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        #region 原始取接口执行函数 弃用
        /*
        protected override JMessage Excute(ParamGetOcrTableDataByImg paramModel)
        {
            JMessage msg = new JMessage();
            try
            {
                #region 解析selector
                int x = 0, y = 0, width = 0, height = 0;
                string _Path = string.Empty;
                JToken _JTokenPath, _JTokenX, _JTokenY, _JTokenWidth, _JTokenPathHeight;

                JArray _JArray = JArray.Parse(paramModel.Selector);
                var obj = (JObject)_JArray.Last();

                if (obj.TryGetValue("path", out _JTokenPath)
                    && obj.TryGetValue("x", out _JTokenX)
                    && obj.TryGetValue("y", out _JTokenY)
                    && obj.TryGetValue("width", out _JTokenWidth)
                    && obj.TryGetValue("height", out _JTokenPathHeight))
                {
                    _Path = _JTokenPath.ToString();
                    x = (int)_JTokenX;
                    y = (int)_JTokenY;
                    width = (int)_JTokenWidth;//Math.Abs((int)obj.GetValue("width"));
                    height = (int)_JTokenPathHeight;//Math.Abs((int)obj.GetValue("height"));
                }
                else
                {
                    logger.LogError($"OCR Table Selector Invalid:{paramModel.Selector}");
                    Exception(null, eGuiType.CV, eErrorMessageType.Error_DataCaptureError, ref msg);
                    throw new Exception("");
                }
                #endregion

                ImageData CImageData = new ImageData();
                if (string.IsNullOrWhiteSpace(_Path))//图片路径为空 则进行屏幕截图
                {
                    using (Bitmap bitmap = UiCommon.GetDesktop())
                    {
                        #if Trace
                        bitmap.Save("11.jpg");
                        #endif
                        CImageData.Base64 = ImageHelper.Bitmap2Base64(bitmap);
                    }
                    CImageData.StartPoint=new Point(x, y);
                    CImageData.EndPoint = new Point(x + width, y + height);
                }
                else
                {
                    if(IsValideImage(_Path, ref msg))
                    {
                        PictureHelper cPictureHelper = new PictureHelper();
                        if (!cPictureHelper.GetImageDataFromFile(_Path, ref CImageData))
                        {
                            Exception(null, eGuiType.CV, eErrorMessageType.Error_PictureOpenWrong, ref msg);
                            throw new Exception("");
                        }
                    }
                }

                string cvResults = OcrHelper.GetOCRTableResult(CImageData, paramModel.Server);
               
                //string cvResults = "";
                #if Trace
                using (FileStream fs = File.Open("result.txt", FileMode.OpenOrCreate))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(cvResults);
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                    //cvResults = Encoding.UTF8.GetString(bytes);
                }
                #endif
                string _ResultContent = string.Empty;
                string _ErrorMsg = string.Empty;
                if(GetTableStructureJson( cvResults, ref _ResultContent, ref _ErrorMsg))
                {
                    msg.Info = _ResultContent;//GetAnalyseJson(cvResults); 
                }
                else
                {
                    logger.LogError($"Failed to Get Table Data:{_ErrorMsg}");
                    msg.Message = _ErrorMsg;
                    Exception(null, eGuiType.CV, eErrorMessageType.Error_GetNoTable, ref msg);
                    throw new Exception(_ErrorMsg);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"OCR Get TableInfo Ex:{ex.ToString()}");
                Exception(null, eGuiType.CV, eErrorMessageType.Error_GetTableDataError, ref msg);
                throw new Exception("");
            }
            return msg;
        }
        */
        #endregion

        protected override JMessage Execute(ParamGetOcrTableDataByImg paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();

            try
            {
                string _strImagePath = paramModel.ImagePath;//本地截图保存路径
                ImageData CImageData = new ImageData();
                if (string.IsNullOrWhiteSpace(_strImagePath))
                {
                    /* //图片路径为空 则进行屏幕截图
                    using (Bitmap bitmap = UiCommon.GetDesktop())
                    {
                        #if Trace
                        bitmap.Save("11.jpg");
                        #endif
                        CImageData.Base64 = ImageHelper.Bitmap2Base64(bitmap);
                        CImageData.StartPoint = new Point(0, 0);
                        CImageData.EndPoint = new Point(bitmap.Width, bitmap.Height);
                    }
                    */
                    return OcrTableError.GetErrMessage(eErrorMessageType.Error_PictureNotFound, "Picture Path is Empty");
                }
                else
                {
                    if (IsValideImage(_strImagePath, ref msg))//检查文件是否存在 以及图片扩展名是否有效
                    {
                        PictureHelper cPictureHelper = new PictureHelper();
                        if (!cPictureHelper.GetImageDataFromFile(_strImagePath, ref CImageData))
                        {
                            return OcrTableError.GetErrMessage(eErrorMessageType.Error_PictureOpenWrong, "图片读取失败".I18n("GetOcrTableDataByImg_A6D74F05"));
                        }
                    }
                    else
                    {
                        return msg;
                    }
                }

                ImageDataEx CImageDataEx = new ImageDataEx(CImageData);
                string cvResults = string.Empty;

                CursorHelper.SetCursorBusy(true);
                try
                {
                    logger.LogInformation($"GetOcrTableDataByImg 调用算法接口服务");

                    if (InternetStateHelper.IsSaasServiceAvailable() && InternetStateHelper.IsTokenAvailable(paramModel.Token))
                    {
                        logger.LogInformation("运行模式:进入Saas模式");
                        CommonHttpParam _CommonHttpParam = new CommonHttpParam();
                        SaasParam _SaasParam = new SaasParam("rpa-table.table_ocr_service", "1.0", paramModel.Token);

                        string imgStr = CImageDataEx?.Base64?.ToString();
                        string _strTableAlgInfo = CImageDataEx?.TableAlgInfo?.ToString();
                        List<List<int>> pointList = new List<List<int>> { new List<int> { CImageDataEx.StartPoint.X, CImageDataEx.StartPoint.Y },
                            new List<int> { CImageDataEx.EndPoint.X, CImageDataEx.EndPoint.Y } };

                        _CommonHttpParam.ParamCollection.Add("image", imgStr);
                        _CommonHttpParam.ParamCollection.Add("points", pointList);
                        _CommonHttpParam.ParamCollection.Add("table_algo_info", _strTableAlgInfo);

                        string ResultMessage = string.Empty;
                        string ErrorMessage = string.Empty;
                        if (SaasAuthenticationHelper.RequestSaasService(_CommonHttpParam, _SaasParam, ref ResultMessage, ref ErrorMessage))
                        {
                            cvResults = ResultMessage;
                            logger.LogInformation($"本地图片表格解析返回结果 {cvResults}");
                            if (InternetStateHelper.IsResponceTokenErr(ResultMessage))
                            {
                                logger.LogInformation("检测到saas鉴权失败 运行模式:进入离线模式");
                                paramModel.Server = InternetStateHelper.LocalAlgOcrAnalyseUrl;
                                if (!OcrHelper.GetOCRTableResultEx(CImageDataEx, paramModel.Server, ref cvResults, ref msg))
                                {
                                    return msg;
                                }
                            }
                        }
                        else
                        {
                            if (InternetStateHelper.IsResponceTokenErr(ResultMessage) || InternetStateHelper.IsResponceTokenErr(ErrorMessage))
                            {
                                logger.LogInformation("检测到saas鉴权失败 运行模式:进入离线模式");
                                paramModel.Server = InternetStateHelper.LocalAlgOcrAnalyseUrl;
                                if (!OcrHelper.GetOCRTableResultEx(CImageDataEx, paramModel.Server, ref cvResults, ref msg))
                                {
                                    return msg;
                                }
                            }
                            else
                            {
                                logger.LogError($"本地图片表格解析失败:{ErrorMessage}");
                                msg = OcrTableError.GetErrMessage(eErrorMessageType.Error_GetTableDataFailExcute);
                                return msg;
                            }
                        }
                    }
                    else
                    {
                        logger.LogInformation("运行模式:进入离线模式");

                        paramModel.Server = InternetStateHelper.LocalAlgOcrAnalyseUrl;
                        if (!OcrHelper.GetOCRTableResultEx(CImageDataEx, paramModel.Server, ref cvResults, ref msg))
                        {
                            return msg;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"本地图片表格解析发生异常");
                }
                finally
                {
                    CursorHelper.SetCursorBusy(false);
                }

                //string cvResults = OcrHelper.GetOCRTableResult(CImageDataEx, paramModel.Server);
#if Trace
                using (FileStream fs = File.Open("result.txt", FileMode.OpenOrCreate))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(cvResults);
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                }
#endif
                string _ResultContent = string.Empty;
                string _ErrorMsg = string.Empty;
                if (GetTableResultJson(cvResults, ref _ResultContent, ref _ErrorMsg))
                {
                    msg.Info = _ResultContent;
                }
                else
                {
                    return OcrTableError.GetErrMessage(eErrorMessageType.Error_GetNoTable, Localize($"本地图片解析处理返回值失败:{_ErrorMsg}", "GetOcrTableDataByImg_C001B0B5"));
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"GetOcrTableDataByImg 执行异常");
                return OcrTableError.GetErrMessage(eErrorMessageType.Error_GetNoTable, Localize($"本地图片解析异常:{ex.ToString()}", "GetOcrTableDataByImg_B760B05B"));
            }
            return msg;
        }

        internal bool IsValideImage(string _strPath, ref JMessage msg)
        {
            if (!File.Exists(_strPath))
            {
                msg = OcrTableError.GetErrMessage(eErrorMessageType.Error_PictureNotFound, "Picture Path is Empty");
                return false;
            }
            else
            {
                FileInfo fileInfo = new FileInfo(_strPath);
                if (fileInfo.Extension != ".jpg" && fileInfo.Extension != ".png" && fileInfo.Extension != ".jpeg")
                {
                    msg = OcrTableError.GetErrMessage(eErrorMessageType.Error_ImageFormatNotSupported, "Image Format Not Supported");
                    return false;
                }
            }
            return true;
        }

        internal bool GetTableResultJson(string _strJsonResultFromServer, ref string _strResult, ref string _strErrorMsg)
        {
            _strResult = string.Empty;
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

                object objResult = JsonConvert.DeserializeObject<object>(obj?["result"]?.ToString());

                //if (string.IsNullOrEmpty(objResult?["table_structure"]?.ToString()))
                //{
                //    logger.LogError($"OCR Table Result Get No Value");
                //    _strErrorMsg = "Get No Value For Table Structure";
                //    return false;
                //}

                //_strResult = JsonConvert.SerializeObject((object)(objResult?["table_structure"]));

                _strResult = JsonConvert.SerializeObject(objResult);
                bRet = true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetOcrTableDataByImg GetTableResultJson执行异常");
                _strErrorMsg = $"Get Table Data EX:{ex.Message}";
            }

            return bRet;
        }


        #region 解析OCR表格的表格内容 此步骤交由engine不再使用
        internal string GetAnalyseJson(string strJsonResultFromServer)
        {
            string strResult = string.Empty;
            string strErr = string.Empty;
            List<List<List<string>>> _listTableInfo = GetTableItem(strJsonResultFromServer, ref strErr);
            strResult = JsonHelper.JsonByNewtonJson(_listTableInfo);
            strResult = string.IsNullOrEmpty(strResult) ? strErr : strResult;
            return strResult;
        }

        internal static List<List<List<string>>> GetTableItem(string strTableInfo, ref string errMsg)
        {
            List<List<List<string>>> _TableList = new List<List<List<string>>>();

            bool bIsSuccess = false;
            string strErrorMsg = string.Empty;

            try
            {
                var obj = JsonConvert.DeserializeObject<JToken>(strTableInfo);

                if (!string.IsNullOrEmpty(obj?["status"]?.ToString()))
                {
                    bIsSuccess = (obj?["status"]?.ToString().ToLower() == ("success"));
                }

                if (!bIsSuccess)
                {
                    if (!string.IsNullOrEmpty(obj?["message"]?.ToString()))
                    {
                        strErrorMsg = obj?["message"]?.ToString();
                    }

                    Console.WriteLine($"表格解析失败:{strErrorMsg}");
                    return _TableList;
                }

                if (!string.IsNullOrEmpty(obj?["result"]?.ToString()))
                {
                    JToken objResult = JsonConvert.DeserializeObject<JToken>(obj?["result"]?.ToString());

                    if (string.IsNullOrEmpty(objResult?["table_structure"]?.ToString()))
                    {
                        return _TableList;
                    }

                    JToken table_structure = JsonConvert.DeserializeObject<JToken>(objResult?["table_structure"]?.ToString());

                    if (!string.IsNullOrEmpty(table_structure?["table_list"]?.ToString()))
                    {
                        //JToken objTableList = table_structure?["table_list"];

                        JArray ja = JArray.Parse(table_structure?["table_list"].ToString());
                        foreach (var item in ja)
                        {
                            if (string.IsNullOrEmpty(item?["row_size"]?.ToString()) || string.IsNullOrEmpty(item?["col_size"]?.ToString()))
                            {
                                continue;
                            }
                            int RowCount = Convert.ToInt32(item?["row_size"]?.ToString());
                            int ColCount = Convert.ToInt32(item?["col_size"]?.ToString());

                            string[,] TableData = new string[RowCount, ColCount];

                            if (string.IsNullOrEmpty(item?["cell_list"]?.ToString()))
                            {
                                continue;
                            }

                            JArray jarray = JArray.Parse(item?["cell_list"]?.ToString());
                            foreach (JObject _CellStruct in jarray)
                            {
                                //"start_row":8,
                                //"end_row":8,
                                //"start_col":4,
                                //"end_col":4,
                                //"text":"",

                                if (!_CellStruct.ContainsKey("start_row") ||
                                    !_CellStruct.ContainsKey("end_row") ||
                                    !_CellStruct.ContainsKey("start_col") ||
                                    !_CellStruct.ContainsKey("end_col") ||
                                    !_CellStruct.ContainsKey("text"))
                                {
                                    continue;
                                }

                                int _Start_Row = Convert.ToInt32(_CellStruct.Value<string>("start_row").ToString());
                                int _End_row = Convert.ToInt32(_CellStruct.Value<string>("end_row").ToString());
                                int _Start_col = Convert.ToInt32(_CellStruct.Value<string>("start_col").ToString());
                                int _End_col = Convert.ToInt32(_CellStruct.Value<string>("end_col").ToString());
                                string _strCellValue = _CellStruct.Value<string>("text").ToString();// item?["text"]?.ToString();
                                for (int i = _Start_Row; i <= _End_row; i++)
                                {
                                    for (int j = _Start_col; j <= _End_col; j++)
                                    {
                                        TableData[i, j] = _strCellValue;
                                    }
                                }
                            }

                            List<List<string>> CellValueList = new List<List<string>>();
                            for (int i = 0; i < RowCount; i++)
                            {
                                List<string> CellValueRow = new List<string>();
                                for (int j = 0; j < ColCount; j++)
                                {
                                    CellValueRow.Add(TableData[i, j]);
                                }
                                CellValueList.Add(CellValueRow);
                            }

                            _TableList.Add(CellValueList);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "ExecuteCV OCR表格解析失败");
                errMsg = Localize($"ExecuteCV OCR表格解析失败:{ex}", "GetOcrTableDataByImg_B0413B8F");
            }
            return _TableList;
        }
        #endregion
    }
}

