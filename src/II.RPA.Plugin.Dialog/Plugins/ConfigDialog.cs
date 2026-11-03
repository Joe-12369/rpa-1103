using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.Dialog.Helpers;
using II.RPA.Plugin.Dialog.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace II.RPA.Plugin.Dialog.Plugins
{
    public class ConfigDialog : StaDialogAbilityBase<ParamConfigDialogModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ConfigDialog;
        public ConfigDialog(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override JMessage Execute(ParamConfigDialogModel paramModel, PluginExecutionContext context)
        {
        
            JMessage msg = new JMessage();
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                DialogOutPut result = new DialogOutPut() { Code = "2" };
                try
                {
                    ConfigSetWnd csw = new ConfigSetWnd(
                        paramModel.Info,
                        paramModel.FolderPath,
                        paramModel.Title,
                        paramModel.ExeMode == "2" ? ExeMode.Pre : ExeMode.Exe,
                        paramModel.AutoOut);
                    logger.LogInformation($"自定义对话框初始化:{stopwatch.ElapsedMilliseconds}");
                    if (csw.ShowDialog() == true)
                    {
                        result.Info = csw.outJson;
                    }
                    else
                    {
                        result.Code = "1";
                    }

                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"自定义对话框线程异常");
                    result.Code = "0";
                    result.ErrorMsg = "对话框显示异常，请检查配置信息是否正确！".I18n("ConfigDialog_3249D9BF");

                    if (ex is RPAException RpaEx)
                    {
                        result.ErrorMsg = RpaEx.ErrorMessage;
                    }
                }
                finally
                {
                    stopwatch.Stop();
                }

                msg.Info = JsonConvert.SerializeObject(result);
                if (result.Code == "0" && !string.IsNullOrEmpty(result.ErrorMsg))
                {
                    MessageBox.Show(result.ErrorMsg);
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ParamsError, ref msg);
            }
            finally
            {
                GC.Collect();
            }
            return msg;
        }

        protected override ParamConfigDialogModel DeserializeParameter(string jsonParam)
        {
            ParamConfigDialogModel paramModel = null;
            if (!ParamVerify(jsonParam,ref paramModel, out var ParamAnalyEx))
            {
                string paramExceptionDesc = AnalyJsonParamException(jsonParam);
                logger.LogError(paramExceptionDesc);
                throw new RPAException(eErrorMessageType.Error_ParamsError, eComponentType.ConfigDialog, paramExceptionDesc);
            }
            return paramModel;
        }

        protected override void ValidateParameter(ParamConfigDialogModel paramModel)
        {
        }

        public bool ParamVerify(string jsonParam, ref ParamConfigDialogModel paramModel,out Exception ParamAnalyEx)
        {
            try
            {
                ParamAnalyEx = null;
                paramModel = jsonParam.ToObject<ParamConfigDialogModel>();
                return true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"ConfigDialog 参数格式不符合要求：{jsonParam}");
                ParamAnalyEx = ex;
                return false;
            }
        }

        /// <summary>
        /// ParamConfigDialogSubModel
        /// </summary>
        /// <param name="jsonParam"></param>
        /// <returns></returns>
        public string AnalyJsonParamException(string jsonParam)
        {
            List<string> strExceptionDescList = new List<string>();
            try
            {
                ParamConfigDialogSubModel paramModel = jsonParam.ToObject<ParamConfigDialogSubModel>();
                
                for(int i =0; i< paramModel.Info.Count;i++)
                {
                    if (paramModel.Info[i] == null)
                    {
                        strExceptionDescList.Add("检测到空模板参数".I18n("ConfigDialog_5FCB61E"));
                        continue;
                    }

                    try
                    {
                        paramModel.Info[i].ToJson().ToObject<InputModel>();
                    }
                    catch (Exception ex)
                    {
                        string strTemp = string.Empty;
                        string strPath = string.Empty;
                        string strPathType = string.Empty;

                        if (ex is JsonReaderException ex1 && ex1?.Path != null)
                        {
                            strPath = ex1.Path;
                        }
                        else if (ex is JsonSerializationException ex2 && ex2?.Path != null)
                        {
                            strPath = ex2.Path;
                        }
                        else
                        {
                            strPath = "UnKnow";
                        }

                        JObject jsonObject = JObject.Parse(paramModel.Info[i].ToJson());
                        if (jsonObject.ContainsKey("label"))
                        {
                            JToken labelValue = jsonObject["label"];
                            if (labelValue.Type == JTokenType.String)
                            {
                                strTemp = labelValue.ToString();
                            }
                        }

                        if (strPath != "UnKonw")
                        {
                            strPathType = GetPropertyType(strPath,ref strPath);
                        }
                        /*
                        $"[{ComponentMessageConfig.GetInstance().GetComponentTextByCode(eComponentType.ConfigDialog)}]" +
                        strTemp = string.Format("自定义对话框组件的第{0}个控件出现参数设置错误，控件名称:{1}，错误字段名:{2}，该字段所需参数类型:{3}，具体错误信息:{4}",
                            i + 1, strTemp ?? "UnKnow", strPath, strPathType ?? "UnKnow", ex.Message);*/
                        strTemp = string.Format("自定义对话框的第{0}个控件出现参数设置错误，控件名称:{1}，错误字段名:{2}，该字段所需参数类型:{3}".I18n("ConfigDialog_1F9CB5B"),
                            i + 1, strTemp ?? "UnKnow", strPath, strPathType ?? "UnKnow");
                        logger.LogError($"参数异常:{ex.Message}");
                        strExceptionDescList.Add(strTemp ?? ex.Message);
                    }
                }

                return string.Join("\r\n", strExceptionDescList);
            }
            catch (Exception ex)
            {
                return $"{eErrorMessageType.Error_ParamsError}:{ex.Message}"; 
            }
        }


        public string GetPropertyType(string strPropertyName,ref string strPropertyAnnotation)
        {
            Type type = typeof(InputModel);

            var propertyInfos = type.GetProperties();

            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.Name.ToLower() == strPropertyName.ToLower())
                {
                    var attributes = propertyInfo.GetCustomAttributes(typeof(PropertyAnnotationAttribute), false);
                    if (attributes.Length > 0)
                    {
                        var annotationAttribute = (PropertyAnnotationAttribute)attributes[0];
                        strPropertyAnnotation = annotationAttribute.Annotation;                      
                    }

                    Type fieldType = propertyInfo.PropertyType;
                    if (fieldType.Name.Contains("[]"))
                    {
                        return "数组";
                    }

                    return fieldType.Name;
                }
            }
            return null;
        }
    }
}

