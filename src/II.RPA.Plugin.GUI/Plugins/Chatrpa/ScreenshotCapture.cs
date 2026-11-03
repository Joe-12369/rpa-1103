using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.DataCollection.V2.Windows;
using II.RPA.Plugin.GUI.DataCollection.V2;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.ScreenShot;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core.Entities;
using Newtonsoft.Json.Linq;
using II.RPA.Plugin.GUI.Utils;
using FlaUI.Core.Tools;
using System.IO;
using SharpVectors.Dom;
using System.Windows.Interop;

namespace II.RPA.Plugin.GUI.Plugins.DataCollection
{
    public class ScreenshotCapture : ElementAbilityBase<ParamScreenshotCaptureModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ScreenshotCapture;

        public ScreenshotCapture()
        {
        }

        protected override void ValidateParameter(ParamScreenshotCaptureModel paramModel) {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override void NormalizeParameter(ParamScreenshotCaptureModel paramModel)
        {
            if (string.IsNullOrEmpty(paramModel.ActionCmd))
            {
                paramModel.ActionCmd = "stop";
            }
            if (string.IsNullOrEmpty(paramModel.FilePath))
            {
                var pictureRandomName = $"{new Random().Next(1, 99999)}-{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}.png".Trim().Replace(" ", string.Empty);
                paramModel.FilePath = Path.Combine("res", pictureRandomName);
            }
        }

        protected override JMessage Execute(ParamScreenshotCaptureModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage captureMessage = new JMessage();
            try
            {
                logger.LogDebug($"开始拾取：Capture.DataCollectCapture");
                GlobleConfigurations.CaptureUniqueID = DateTime.Now.ToLongTimeStamp();
                if (paramModel.ActionCmd.EqualIgnoreCase("Start"))  //只有开始拾取功能，注释start 判断
                {
                    if (paramModel.MemoryInfo != null)
                        NotifyZBrainScreenShot();
                    ElementProvider.Instance.StartScreenshotCapture(paramModel).ConfigureAwait(false);
                }
                else
                {
                    ElementProvider.Instance.StopScreenshotCapture().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref captureMessage);
            }
            finally
            {
            }
            return captureMessage;
        }


        private void NotifyZBrainScreenShot()
        {
            var _desktopWindowHandle = NativeMethods.GetDesktopWindow();
            NativeMethods.GetWindowRect(_desktopWindowHandle, out var _desktopWindowRect);
            var desktopWindowWidth = _desktopWindowRect.Right - _desktopWindowRect.Left;
            var desktopWindowHeight = _desktopWindowRect.Bottom - _desktopWindowRect.Top;
            ZbrainAPIService.NotifyScreenShot(new Rectangle(_desktopWindowRect.Left, _desktopWindowRect.Top, desktopWindowWidth, desktopWindowHeight));
        }
    }
}

