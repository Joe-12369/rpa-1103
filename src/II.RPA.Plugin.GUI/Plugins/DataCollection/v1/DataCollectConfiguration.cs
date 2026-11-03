using II.RPA.Plugin.GUI.DataCollection.Windows;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class DataCollectConfiguration : StaElementAbilityBase<ParamDataCollectConfigurationModel>
    {
        private static ILogger logger = Logger.OfContext();
        public DataCollectConfiguration(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override eComponentType ComponentType => eComponentType.DataCollectConfiguration;
        string globalSelector = null;
        JMessage captureMessage = null;
        protected override void ValidateParameter(ParamDataCollectConfigurationModel paramModel) {
            try
            {
                logger.LogDebug("Start DataCollectConfiguration");
                //if (string.IsNullOrEmpty(paramModel.filepath))
                //{
                //    throw new RPAException(eErrorMessageType.Error_ParamsError, "缺少文件地址参数");
                //}
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override void NormalizeParameter(ParamDataCollectConfigurationModel paramModel)
        {
            if (string.IsNullOrEmpty(paramModel.WebData))
            {
                paramModel.WebData = "{}";
            }
        }

        protected override JMessage Execute(ParamDataCollectConfigurationModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            globalSelector = null;
            captureMessage = new JMessage();
            DataCollectWnd dataCollectWnd = null;
            try
            {
                DataCollectingManager.InitDataCollectOverlayWnd();
                //以下为正常逻辑弹出数据采集窗口
                dataCollectWnd = new DataCollectWnd(paramModel);
                dataCollectWnd.ShowDialog();
                DataCollectingManager.ReleaseDataCollectOverlayWnd();
                if (GlobalData.GetInstance().VM_MainWindowViewModel.IsDataValid == false)
                {
                    captureMessage = GlobalData.GetInstance().VM_MainWindowViewModel.CaptureJMessage;
                    captureMessage.Extend = GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.ToJson();
                }
                else
                {
                    captureMessage.Info = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel?.ToJson(Formatting.None);
                    captureMessage.Extend = GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.ToJson();
                }
                globalSelector = string.Empty;
                //DataCollectingManager.ReleaseDataCollectOverlayWnd();
                GlobalData.GetInstance().VM_MainWindowViewModel = null;
                GuiElement.overlayForm = new OverlayForm();
                DataCollectingCaptureTask.Instance.StopAfterExecute();
            }
            catch (COMException ex)
            {
                logger.LogError(ex);
            }
            catch (Exception ex)
            {
                //if (dataCollectOverlayWnd?.Visibility == System.Windows.Visibility.Visible)
                //{
                //    dataCollectOverlayWnd.Close();
                //}
                Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref captureMessage);
            }
            return captureMessage;
        }



    }
}

