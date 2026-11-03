using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class FileUploader : GuiComponentBase<ParamFileUploaderModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.FileUploader;

        protected override JMessage Execute(ParamFileUploaderModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                GuiElement element = null;
                long operationTimeout = 0;
                try
                {
                    Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                    var param = new GetElementFromSelectorParams
                    {
                        Selector = paramModel.Selector,
                        SelectorType = paramModel.SelectorType,
                        ExtendParam = hashtable,
                        MatchType = paramModel.MatchType,
                        RunParam = paramModel.RunParam,
                        WindowInfo = paramModel.WindowInfo
                        //TabId = paramModel.TabId  //最终会根据元素成功新匹配到一个TabID 
                    };
                    element = GuiElement.FromSelector(param);
                }
                catch (Exception ex)
                {
                    Exception(ex, eErrorMessageType.Error_NotFoundElement, ref msg);
                    return msg;
                }
                try
                {
                    if (element == null)
                    {
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                    }
                    if (paramModel.IsRandomLocationClick)
                    {
                        logger.LogInformation("Downloader：启用了模拟人工随机位置点击");
                        paramModel.inputMode = 1;//模拟输入
                    }
                    //组件超时计算机制，保持和下载一致
                    operationTimeout = paramModel.RunParam.TotalTime + paramModel.timeout;
                    logger.LogInformation($"回传coreserver超时时间:{operationTimeout}ms.");
                    NotifyCoreServerElementFound(paramModel.RunUniqueID, operationTimeout);
                    //待实现的上传逻辑
                    element.ElementFileUploader(new Commons.Params.ParamFileUploaderModel
                    {
                        inputMode = paramModel.inputMode,
                        interval = paramModel.interval,
                        waitDialogTime = paramModel.waitDialogTime,
                        uploadType = (Commons.Params.UploadType)Enum.ToObject(typeof(Commons.Params.UploadType), (int)paramModel.uploadType),
                        filePath = paramModel.filePath
                    });
                }
                catch (RPAException rpaEx)
                {
                    Exception(rpaEx, rpaEx.ErrorMessageType, ref msg);
                    msg.AllowRetry = ((int)AllowRetryType.NotAllow).ToString();
                }
                return msg;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_FileUploadFail, ref msg);
                msg.AllowRetry = ((int)AllowRetryType.NotAllow).ToString();
            }
            return msg;
        }

        protected override void ValidateParameter(ParamFileUploaderModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.filePath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }

                return;
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"上传文件 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

