using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Services;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.DataCollection.V2;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Windows;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class CaptureWindow : StaElementAbilityBase<ParamCaptureWindowModel>
    {
        private static ILogger logger = Logger.OfContext();
        GuiElement elementCache = null;
        JMessage captureMessage = null;
        string globalSelector = null;
        CommonProps originalCommonProps = null;
        protected override eComponentType ComponentType => throw new NotImplementedException();

        public CaptureWindow(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override JMessage Execute(ParamCaptureWindowModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            captureMessage = new JMessage();
            elementCache = null;
            globalSelector = string.Empty;
            try
            {
                logger.LogDebug($"开始拾取：Capture.CaptureWindow");

                GuiElement.OnSelected = UIElement_OnSelected;
                GuiElement.OnCanceled = UiElement_OnCancel;

                WindowCaptureManager.Instance.StartCaptureWindow(paramModel);
                WindowCaptureManager.Instance.WindowHighLightWnd.ShowDialog();


                captureMessage.Info = globalSelector;
            }
            catch (COMException ex)
            {
                logger.LogError(ex);
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

        protected override void ValidateParameter(ParamCaptureWindowModel paramModel) {
            try
            {
                GlobleConfigurations.CaptureUniqueID = RandomHelper.RandomString(10);
                if (string.IsNullOrEmpty(paramModel.filePath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "图片保存地址为空".I18n("controlType_CaptureWindow_4CB9ECDF"));
                }
                else
                {
                    string path = Path.GetDirectoryName(paramModel.filePath);
                    string fileName = Path.GetFileName(paramModel.filePath);
                    paramModel.filePath = Path.Combine(path, GlobleConfigurations.CaptureUniqueID, fileName);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "CaptureWindow 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        private string UIElement_OnSelected(GuiElement element)
        {
            elementCache = element;
            globalSelector = AddFileAttrToSelector(element.Selector, element.screenShotPath);
            return globalSelector;
        }
        private string AddFileAttrToSelector(string orgJson, string filePath)
        {
            try
            {

                //JArray jArray = JArray.Parse(orgJson);
                var common = JsonConvert.DeserializeObject<RPACommon>(orgJson);
                //var item = jArray?.FirstOrDefault();
                //if (item != null)
                //{
                string fileContent = filePath;
                //如果是远程拾取服务端，传递图标的base64
                if (GlobleConfigurations.IsRemoteCaptureServer)
                {
                    if (File.Exists(filePath))
                    {
                        using (var img = new Bitmap(filePath))
                        {
                            fileContent = ImageHelper.Bitmap2Base64(img);
                        }
                    }
                }
                common.CommonProps.ModifyTime = DateTime.Now.ToLongTimeStamp();
                common.CommonProps.filePath = fileContent;
                common.CommonProps.captureMode = (int)GuiElement.overlayForm.CurrentCaptureMode;
                //埋点数据
                //GuiElement.overlayForm.CaptureOperationData.pick_mode = GuiElement.overlayForm.CurrentCaptureMode.ToString();
                //GuiElement.overlayForm.CaptureOperationData.app_name = common.CommonProps.ProcessName;
                //GuiElement.overlayForm.CaptureOperationData.element_type = common.CommonProps.GUIType;

                //(item as JObject).Add("filePath", fileContent);
                //(item as JObject).Add("captureMode", (int)GuiElement.overlayForm.CurrentCaptureMode);
                common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
                // ExtendedTags 是 Flags 枚举，保留元素原有的扩展标记并追加窗口类型标记。
                common.CommonProps.ExtendedTags |= ExtendedTags.WindowSelectorType;
                if (originalCommonProps != null)
                {
                    common.CommonProps.isFindbyIndex = originalCommonProps.isFindbyIndex;
                    common.CommonProps.isSmartControl = originalCommonProps.isSmartControl;
                }
                try
                {
                    logger.LogDebug($"开始处理ico");
                    if (common.ElementDetail.CategoryName != "Windows桌面")
                    {
                        var icon = System.Drawing.Icon.ExtractAssociatedIcon(common.ElementDetail.CategoryIconFile);
                        Bitmap bmp = icon.ToBitmap();
                        var iconfile = Path.GetDirectoryName(Path.GetDirectoryName(filePath));
                        var iconfileName = Path.GetFileNameWithoutExtension(common.ElementDetail.CategoryIconFile);
                        iconfile = Path.Combine(iconfile, iconfileName + ".ico");
                        bmp.Save(iconfile, System.Drawing.Imaging.ImageFormat.Icon);
                        common.ElementDetail.CategoryIcon = iconfileName + ".ico";
                    }
                    else
                    {
                        common.ElementDetail.CategoryIcon = "";
                    }
                    common.ElementDetail.ElementId = GlobleConfigurations.CaptureUniqueID;
                    logger.LogDebug($"处理ico结束");
                }
                catch (Exception e)
                {
                    common.ElementDetail.CategoryIcon = "";
                    common.ElementDetail.CategoryIconFile = "";
                    common.ElementDetail.CategoryName = "";
                    common.ElementDetail.WindowName = "";
                }
                if (GuiElement.overlayForm.UseCaptureMode == CaptureModeType.FloatWnd)
                {
                    common.CommonProps.Flag = "NoForeground";
                    //(item as JObject).Add("Flag", "NoForeground");
                }
                //}
                return JsonConvert.SerializeObject(common);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return orgJson;
            }
        }
        private void UiElement_OnCancel()
        {
            if (captureMessage != null)
            {
                captureMessage.Code = 1;
                captureMessage.UnifyCode = "1";
                captureMessage.Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage();
            }
            globalSelector = string.Empty;
        }
    }
}

