using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.Logging;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Logger = II.RPA.Logging.Logger;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze
{
    internal class AnalyzerForCv : AnalyzerBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        public AnalyzerForCv()
        {
        }
        public override Priority ExecutePriority { get => base.ExecutePriority; set => base.ExecutePriority = value; }

        public override string[] SupportGuiTypes => new string[] 
        {
             
        };
        public int[] ConditionClass = new int[] 
        {
            30,31,32
        };
        public string[] ExceptElementTypes = new string[]
        {
            "listitem"
        };
        public string[] ExceptProcessNames = new string[]
        {
            "explorer"
        };
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public async Task<AnalyzeResult?> TryExecuteAnalyzerAsAddition(ActionParamModel actionParamModel)
        {
            try
            {
                var elementClass = await ElementClass(actionParamModel.ElementShotBase64);
                if (LocalDataModels.CvElementActions.TryGetValue(elementClass.ToString(), out List<ElementAction> actions))
                {
                    var recommendActionExecutions = actions.Last().Actions;
                    if (recommendActionExecutions.Any())
                    {
                        var isExceptElementType = ExceptElementTypes.Contains(actionParamModel.ControlType);
                        var isExceptProcessName = ExceptProcessNames.Contains(actionParamModel.ProcessName);

                        bool anIsMatchCondition = true;
                        if (isExceptElementType || isExceptProcessName)
                        {
                            anIsMatchCondition = false;
                        }
                        else
                        {
                            anIsMatchCondition = ConditionClass.Contains(elementClass);
                        }
                        var anElementClass = actions.Last().ElementType;
                        return new AnalyzeResult
                        {
                            ElementClass = anElementClass,
                            IsMatchCondition = anIsMatchCondition,
                            RecommendActionExecutions = recommendActionExecutions,
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return new AnalyzeResult
            {
                ElementClass = "其他图标".I18n("AnalyzerForCv_FAFA0698"),
            };
        }
        public async Task<AnalyzeResult?> TryExecuteAnalyzer(JObject aSelectorJO,
            ActionParamModel actionParamModel)
        {
            //默认其他图标
            int elementClass = 19;
            try
            {
                //logger.LogDebug($"cvdata:{aSelectorJO.ToJson()}");
                JArray props = aSelectorJO["props"] as JArray;
                var elementobj = props.FirstOrDefault(p => p["name"].ToString() == "elementobj");
                if (elementobj != null)
                {
                    logger.LogDebug($"find elementobj");
                    var valueObject = JsonConvert.DeserializeObject<JObject>(elementobj["value"].ToString());
                    var cvtext = valueObject["result_value"]["text_contents"];
                    var cvimageBase64 = valueObject["result_value"]["template_image"];
                    if (cvtext == null || string.IsNullOrEmpty(cvtext.ToString()))
                    {
                        string imagebase64 = cvimageBase64.ToString();
                        if (string.IsNullOrEmpty(imagebase64))
                        {
                            using (Bitmap shot = new Bitmap(actionParamModel.PicturePath))
                            {
                                //shot.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"test.png"));
                                imagebase64 = ImageHelper.Bitmap2Base64(shot);
                            }
                        }
                        if (string.IsNullOrEmpty(imagebase64))
                        {
                            logger.LogError($"element_class:imagebase64 is empty!");
                        }
                        elementClass = await ElementClass(imagebase64);
                        if (elementClass == 0)
                        {
                            logger.LogWarning($"element_class:take the image as text.Fix manaually");
                            elementClass = 19;
                        }
                    }
                    else
                    {
                        elementClass = 0;
                        logger.LogDebug($"element_class:{0}");
                    }
                }
                else
                {
                    throw new Exception("elementobj is null");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("fail to call cv element class", ex);
            }
            if (LocalDataModels.CvElementActions.TryGetValue(elementClass.ToString(), out List<ElementAction> actions))
            {
                var recommendActionExecutions = actions.First().Actions;
                if (recommendActionExecutions.Any())
                {
                    var anElementClass = actions.First().ElementType;
                    if (!string.IsNullOrEmpty(actionParamModel.ProcessName)
                                && (actionParamModel.ProcessName.ToLower().Equals("excel")
                                || actionParamModel.ProcessName.ToLower().Equals("wps")
                                || actionParamModel.ProcessName.ToLower().Equals("et")))
                    {
                        if (LocalDataModels.SpecialElementActions.TryGetValue("excel.record", out List<ElementAction> excelRecordActions))
                        {
                            if (excelRecordActions != null && excelRecordActions.Any())
                            {
                                if (!recommendActionExecutions.Any(a => a.ActionId == "excel.record"))
                                {
                                    logger.LogDebug("Insert excel.record!");
                                    recommendActionExecutions.Insert(0, excelRecordActions.First().Actions.First());
                                }
                                else
                                {
                                    logger.LogWarning("Duplicate excel.record!");
                                }
                            }
                        }
                    }
                    return new AnalyzeResult
                    {
                        ElementClass = anElementClass,
                        RecommendActionExecutions = recommendActionExecutions,
                    };
                }
            }
            return new AnalyzeResult
            {
                ElementClass = "其他图标".I18n("AnalyzerForCv_FAFA0698"),
            };
        }
        private async Task<int> ElementClass(string aBitmapBase64)
        {
            try
            {
                Dictionary<string, object> requestObject = new Dictionary<string, object>
                {
                    {"image",aBitmapBase64},
                    ///...
                };
                logger.LogDebug($"start element_class");
                var responseString = await AnalyzerContainer.ZbrainServiceInstance.ElementClass(requestObject.ToJson()) ;
                int elementClass = JObject.Parse(responseString).GetValue<int>("result");
                logger.LogDebug($"element_class result:{responseString}");
                return elementClass;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ElementClass Error");
                return 19;
            }

        }
    }
}

