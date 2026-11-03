using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI
{
    /// <summary>
    /// 老流程元素场景截图恢复。该能力为元素查找的旁路逻辑，任何异常均不得影响元素执行。
    /// </summary>
    internal static class ElementSceneRecoveryService
    {
        private static readonly ILogger logger = Logger.OfContext();
        private static readonly ConcurrentDictionary<string, byte> runningElements = new ConcurrentDictionary<string, byte>();
        private static readonly ConcurrentDictionary<string, byte> confirmedElements = new ConcurrentDictionary<string, byte>();

        internal static Task TryRecoverAsync(string selector, GuiElement element)
        {
            if (!GlobleConfigurations.EnableAiEnhancedScreenshot || element == null || string.IsNullOrWhiteSpace(selector))
            {
                return Task.CompletedTask;
            }

            return Task.Run(() => TryRecover(selector, element));
        }

        private static void TryRecover(string selector, GuiElement element)
        {
            logger.LogDebug("进入判断是否元素截图恢复操作");
            string recoveryKey = string.Empty;
            try
            {
                JObject selectorObj = JObject.Parse(selector);
                RPACommon common = selectorObj.ToObject<RPACommon>();

                string uid = GetResourceUid(common.CommonProps.ResDirPath);
                if (string.IsNullOrWhiteSpace(uid))
                {
                    logger.LogDebug("元素截图恢复跳过：未获取到资源UID");
                    return;
                }

                string resourceRoot = Path.GetFullPath(Path.Combine(GlobleConfigurations.PackageDirectory, ConstFields.RES_STATICFOLDER_NAME));
                string resourceDirectory = Path.GetFullPath(Path.Combine(resourceRoot, uid));
                if (!resourceDirectory.StartsWith(resourceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning($"元素截图恢复跳过：资源路径不合法，uid={uid}");
                    return;
                }

                string scenePath = Path.Combine(resourceDirectory, ConstFields.RES_SELECTOR_APPFACEIMG_NAME);
                string sceneImageGlobalPath = Path.Combine(resourceDirectory, ConstFields.RES_SELECTOR_IMAGE_GLOBAL);
                string sceneImagePartPath = Path.Combine(resourceDirectory, ConstFields.RES_SELECTOR_IMAGE_PART);
                string elementDescPath = Path.Combine(resourceDirectory, ConstFields.RES_SELECTOR_ELEDESC_NAME);

                if ((IsValidFile(sceneImageGlobalPath) && IsValidFile(elementDescPath)) || IsValidFile(elementDescPath))
                {
                    return;
                }

                string elementId = common.ElementDetail.ElementId ?? string.Empty;
                if (string.IsNullOrWhiteSpace(elementId)
                    || !ZbrainAPIService.TryGetPackageInfo(out System.Collections.Generic.Dictionary<string, object> packageInfo))
                {
                    logger.LogWarning($"元素截图恢复跳过：未获取到流程包信息或元素ID，elementId={elementId}");
                    return;
                }

                string packageInnerId = packageInfo["package_inner_id"].ToString();
                string packageVersion = packageInfo["package_version"].ToString();
                recoveryKey = $"{packageInnerId}|{packageVersion}|{elementId}|{uid}";
                if (confirmedElements.ContainsKey(recoveryKey) || !runningElements.TryAdd(recoveryKey, 0))
                {
                    return;
                }

                // 进入并发保护后再次检查，避免重复截图。
                if ((IsValidFile(sceneImageGlobalPath) && IsValidFile(elementDescPath)) || IsValidFile(elementDescPath))
                {
                    return;
                }

                bool? needScreenshot = ZbrainAPIService.QueryExistingElementImage(packageInfo, elementId);
                if (needScreenshot == false)
                {
                    confirmedElements.TryAdd(recoveryKey, 0);
                    return;
                }
                // 查询明确要求截图，或查询失败/超时，均继续执行截图上传。

                AutomationElement rootElement = ResolveRootElement(element);
                if (rootElement == null)
                {
                    logger.LogWarning($"元素截图恢复失败：未找到元素所在窗口，elementId={elementId}");
                    return;
                }

                Rectangle rootRect = rootElement.BoundingRectangle;
                Rectangle elementBounding = element.BoundingRectangle;
                elementBounding.Offset(-rootRect.X, -rootRect.Y);

                Directory.CreateDirectory(resourceDirectory);
                using (Bitmap sceneImage = rootElement.Capture())
                {
                    if (sceneImage == null)
                    {
                        return;
                    }
                    sceneImage.Save(scenePath);
                }


                if (ZbrainAPIService.UploadExistingElementImage(packageInfo, elementId, scenePath, elementBounding))
                {
                    confirmedElements.TryAdd(recoveryKey, 0);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "元素AI增强截图恢复异常，不影响元素执行");
            }
            finally
            {
                if (!string.IsNullOrEmpty(recoveryKey))
                {
                    runningElements.TryRemove(recoveryKey, out _);
                }
            }
        }

        private static AutomationElement ResolveRootElement(GuiElement element)
        {
            Rectangle elementRect = element.BoundingRectangle;
            if (elementRect.IsEmpty)
            {
                return null;
            }

            Point center = new Point(elementRect.Left + elementRect.Width / 2, elementRect.Top + elementRect.Height / 2);
            foreach (IntPtr hWnd in UiCommon.GetRootWindowList(center))
            {
                if (hWnd == IntPtr.Zero)
                {
                    continue;
                }

                AutomationElement rootElement = UiCommon.UIAAutomation.FromHandle(hWnd);
                if (rootElement != null && rootElement.Properties.BoundingRectangle.IsSupported && rootElement.BoundingRectangle.Contains(center))
                {
                    return rootElement;
                }
            }
            return null;
        }

        private static string GetResourceUid(string resDirPath)
        {
            if (string.IsNullOrWhiteSpace(resDirPath))
            {
                return string.Empty;
            }
            return Path.GetFileName(resDirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        private static bool IsValidFile(string path)
        {
            try
            {
                return File.Exists(path) && new FileInfo(path).Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
