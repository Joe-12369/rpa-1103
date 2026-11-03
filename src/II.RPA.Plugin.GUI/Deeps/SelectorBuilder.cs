using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Deeps
{
    public class SelectorBuilder
    {
        private readonly AutomationElement _automationElement;
        private static readonly ILogger _logger = Logger.OfContext();
        private static readonly CaptureProvider _captureProvider = new CaptureProvider();

        private const int _maxTraversalDepth = 50;
        public SelectorBuilder(AutomationElement automationElement)
        {
            _automationElement = automationElement;
        }
        public string Build()
        {
            GlobleConfigurations.CaptureUniqueID = RandomHelper.RandomString(10);
            if (_automationElement == null || !_automationElement.IsAvailable)
            {
                throw new InvalidOperationException("目标元素已失效".I18n("controlType_SelectorBuilder_57BEE49E"));
            }
            var guiNode = GuiManager.CreateWindowsGuiNode(_automationElement);
            var guiElement = new GuiElement(guiNode);
            AddElementShot(guiElement, _automationElement);
            var selector = OnBuildSelector(guiElement);
            return selector;
        }
        

        private string OnBuildSelector(GuiElement element)
        {
            if (element == null || string.IsNullOrWhiteSpace(element.Selector))
            {
                _logger.LogWarning("OnBuildSelector: element 或 element.Selector 为空");
                return string.Empty;
            }

            return AddFileAttrToSelector(element.Selector, element.screenShotPath);
        }

        private string AddFileAttrToSelector(string orgJson, string filePath)
        {
            if (string.IsNullOrWhiteSpace(orgJson))
            {
                _logger.LogWarning("AddFileAttrToSelector: orgJson 为空");
                return string.Empty;
            }

            try
            {
                var common = JsonConvert.DeserializeObject<RPACommon>(orgJson);
                if (common == null)
                {
                    _logger.LogWarning("AddFileAttrToSelector: 反序列化结果为 null");
                    return orgJson;
                }

                if (common.CommonProps == null)
                    common.CommonProps = new CommonProps();

                if (common.ElementDetail == null)
                    common.ElementDetail = new ElementDetail();

                common.CommonProps.ModifyTime = DateTime.Now.ToLongTimeStamp();
                common.CommonProps.filePath = filePath ?? string.Empty;
                common.CommonProps.captureMode = (int)CaptureModeType.Ordinary;
                common.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
                common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
                common.CommonProps.isSmartControl = "false";
                common.CommonProps.DiscernHidden = "false";

                try
                {
                    _logger.LogDebug("开始处理 ico");

                    if (!string.Equals(common.ElementDetail.CategoryName, "Windows桌面", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(common.ElementDetail.CategoryIconFile) &&
                        File.Exists(common.ElementDetail.CategoryIconFile))
                    {
                        using (var icon = Icon.ExtractAssociatedIcon(common.ElementDetail.CategoryIconFile))
                        {
                            if (icon != null)
                            {
                                var bmp = icon.ToBitmap();
                                var parentDir = Path.GetDirectoryName(filePath);
                                if (!string.IsNullOrEmpty(parentDir))
                                {
                                    var baseDir = Path.GetDirectoryName(parentDir); 
                                    if (!string.IsNullOrEmpty(baseDir))
                                    {
                                        var iconfileName = Path.GetFileNameWithoutExtension(common.ElementDetail.CategoryIconFile);
                                        var iconPath = Path.Combine(baseDir, iconfileName + ".ico");

                                        bmp.Save(iconPath, System.Drawing.Imaging.ImageFormat.Icon);

                                        common.ElementDetail.CategoryIcon = iconfileName + ".png";
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        common.ElementDetail.CategoryIcon = string.Empty;
                    }

                    common.ElementDetail.ElementId = GlobleConfigurations.CaptureUniqueID;
                    _logger.LogDebug("处理 ico 结束");
                }
                catch (Exception e)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, e, "处理 ico 出错");
                    common.ElementDetail.CategoryIcon = string.Empty;
                    common.ElementDetail.CategoryIconFile = string.Empty;
                    common.ElementDetail.CategoryName = string.Empty;
                    common.ElementDetail.WindowName = string.Empty;
                }
                var originalSelector = JsonConvert.SerializeObject(common);
                SelectorFormat(originalSelector, out var selector);
                _logger.LogInformation("build selector:");
                _logger.LogInformation(selector);
                return selector;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "AddFileAttrToSelector 出错");
                return orgJson;
            }
        }
        
        private void AddElementShot(GuiElement guiElement, AutomationElement automationElement)
        {
            guiElement.screenShotPath = Utils.GetElementShotPath();
            var window = GetElementRootWindowHandle(automationElement);
            var rect = automationElement.Properties.BoundingRectangle.ValueOrDefault;
            var bitmap = ElementHandleShot(window, rect);
            if (bitmap != null)
            {
                string dir = Path.GetDirectoryName(guiElement.screenShotPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                bitmap.Save(guiElement.screenShotPath);
                ConfigAIRepair(window, guiElement.screenShotPath, rect);
                bitmap.Dispose();
            }
            else
            {
                _logger.LogError("截图失败");
            }
        }

        private Bitmap ElementHandleShot(IntPtr windowHandle, Rectangle rectangle)
        {
            if (!_captureProvider.Capture(windowHandle, out Bitmap bitmap))
            {
                return null;
            }

            if (rectangle.IsEmpty || rectangle.Width <= 0 || rectangle.Height <= 0)
            {
                _logger.LogWarning($"元素区域无效:{rectangle}");
                return null;
            }

            _logger.LogInformation($"元素原始坐标:({rectangle.X},{rectangle.Y})");

            NativeMethods.GetWindowRect(windowHandle, out NativeMethods.RECT windowRect);

            int relativeX = rectangle.X - windowRect.Left;
            int relativeY = rectangle.Y - windowRect.Top;

            _logger.LogInformation($"元素相对坐标:({relativeX},{relativeY})");

            Bitmap elementBitmap = new Bitmap(rectangle.Width, rectangle.Height);
            using (Graphics g = Graphics.FromImage(elementBitmap))
            {
                g.DrawImage(bitmap,
                    new Rectangle(0, 0, rectangle.Width, rectangle.Height),
                    new Rectangle(relativeX, relativeY, rectangle.Width, rectangle.Height),
                    GraphicsUnit.Pixel);
            }

            return elementBitmap;
        }


        public static IntPtr GetElementRootWindowHandle(AutomationElement element)
        {
            if (element == null)
            {
                return IntPtr.Zero;
            }

            try
            {
                var currentElement = element;
                int traversalDepth = 0;

                while (currentElement != null && traversalDepth < _maxTraversalDepth)
                {
                    try
                    {
                        if (currentElement.Properties.NativeWindowHandle.IsSupported 
                            || (currentElement.Properties.ControlType.IsSupported 
                                && currentElement.Properties.ControlType.ValueOrDefault == ControlType.Window))
                        {
                            var windowHandle = currentElement.Properties.NativeWindowHandle.ValueOrDefault;
                            if (windowHandle != IntPtr.Zero)
                            {
                                return windowHandle;
                            }
                        }
                        var parentElement = currentElement.Parent;
                        if (parentElement == null || parentElement.Equals(currentElement))
                        {
                            break;
                        }
                        currentElement = parentElement;
                        traversalDepth++;
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"获取父元素时发生异常: {ex.Message}");
                        break;
                    }
                }

                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"查找窗口句柄时发生异常: {ex.Message}");
                return IntPtr.Zero;
            }
        }
        private void ConfigAIRepair(IntPtr rootHwnd, string picturePath, Rectangle rectangle)
        {
            _logger.LogDebug("SaveAPPFaceImg Start");
            try
            {
                Rectangle anElementRect = rectangle;
                if (anElementRect != null && anElementRect.IsEmpty)
                {
                    return;
                }
                var rootwindow = UiCommon.UIAAutomation.FromHandle(rootHwnd);
                if (rootwindow == null)
                {
                    _logger.LogError($"SaveAPPFaceImg rootwindow == null");
                }
                anElementRect.X -= rootwindow.BoundingRectangle.X;
                anElementRect.Y -= rootwindow.BoundingRectangle.Y;

                var file = new FileInfo(picturePath);
                {
                    string filePath = Path.Combine(GlobleConfigurations.PackageDirectory, ConstFields.RES_STATICFOLDER_NAME, GlobleConfigurations.CaptureUniqueID);
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    string fileFullName = Path.Combine(filePath, ConstFields.RES_SELECTOR_APPFACEIMG_NAME);
                    file.CopyTo(fileFullName);
                }
                JObject jObj = new JObject();
                JObject elementDataObj = new JObject
                {
                    new JProperty("x", anElementRect.X),
                    new JProperty("y", anElementRect.Y),
                    new JProperty("width", anElementRect.Width),
                    new JProperty("height", anElementRect.Height)
                };
                jObj.Add(new JProperty(ConstFields.RES_SELECTOR_STATICDATA_ELEMENTDATA, elementDataObj));
                UiCommon.SaveStaticSelectorData(GlobleConfigurations.CaptureUniqueID, ConstFields.RES_SELECTOR_STATICFILE_NAME, jObj.ToString());
                Dictionary<string, object> wndInfo = new Dictionary<string, object>
                {
                    { "handle", (int)rootwindow.Properties.NativeWindowHandle.ValueOrDefault },
                    { "area", new[] { anElementRect.X, anElementRect.Y, anElementRect.Width, anElementRect.Height } }
                };
                ZbrainAPIService.SendUIDToZBrain(wndInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
            _logger.LogDebug("SaveAPPFaceImg End");

        }
        private bool SelectorFormat(string aSelectorOriginal, out string aSelectorFormated)
        {
            string _picturePath;
            aSelectorFormated = aSelectorOriginal;
            if (string.IsNullOrEmpty(aSelectorOriginal))
            {
                return false;
            }
            try
            {
                JObject uiSelector = JToken.Parse(aSelectorOriginal) as JObject;
                if (uiSelector["commonProps"]["filePath"] != null)
                {
                    var picturePath = uiSelector["commonProps"]["filePath"].ToString();
                    _picturePath = Path.Combine("res", GlobleConfigurations.CaptureUniqueID, Path.GetFileName(picturePath));
                    uiSelector["commonProps"]["filePath"] = _picturePath;
                }
                if (uiSelector["elementDetail"] != null)
                {
                    var icon_name = uiSelector["elementDetail"]["categoryIcon"];
                    try
                    {
                        if (icon_name != null && !string.IsNullOrEmpty(icon_name.ToString()))
                        {
                            uiSelector["elementDetail"]["categoryIcon"] = Path.Combine("res", icon_name.ToString());
                        }
                        else
                        {
                            uiSelector["elementDetail"]["categoryIcon"] = icon_name?.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex);
                        aSelectorFormated = string.Empty;
                        return false;
                    }
                }
                aSelectorFormated = uiSelector.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
            return true;
        }
    }
}


