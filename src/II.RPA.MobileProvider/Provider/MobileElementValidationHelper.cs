using II.RPA.MobileProvider.CV;
using II.RPA.MobileProvider.Params;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace II.RPA.MobileProvider.Provider
{
    internal static class MobileElementValidationHelper
    {
        private const float ElementImageSimilarityThreshold = 0.95f;
        private const int CurrentPageAdbTimeoutMilliseconds = 5000;

        public static void ValidatePage(AndroidDriver<AndroidElement> driver, ElementDetail elementDetail, MobileElementValidationOptions options, ILogger logger)
        {
            if (options?.IsVerifyPage != true)
            {
                return;
            }

            if (elementDetail == null || string.IsNullOrWhiteSpace(elementDetail.MobilePackageName))
            {
                logger?.LogError("手机界面校验失败，元素未记录手机包名，无法执行界面校验");
                throw new RPAException(eErrorMessageType.Error_MobilePageNotInvalid, "元素未记录手机界面信息，请重新拾取元素");
            }

            var current = GetCurrentPage(driver, logger);
            if (!StringEquals(current.PackageName, elementDetail.MobilePackageName))
            {
                logger?.LogError($"手机界面校验失败，当前包名为[{current.PackageName}]，期望包名为[{elementDetail.MobilePackageName}]");
                throw new RPAException(eErrorMessageType.Error_MobilePageNotInvalid, "未检测到页面信息，请检查后重试");
            }

            if (!string.IsNullOrWhiteSpace(elementDetail.MobileActivityName) &&
                !StringEquals(NormalizeActivity(current.PackageName, current.ActivityName), NormalizeActivity(elementDetail.MobilePackageName, elementDetail.MobileActivityName)))
            {
                logger?.LogError($"手机界面校验失败，当前Activity为[{current.ActivityName}]，期望Activity为[{elementDetail.MobileActivityName}]");
                throw new RPAException(eErrorMessageType.Error_MobilePageNotInvalid, "未检测到页面信息，请检查后重试");
            }
        }

        public static void ValidateElementImage(AndroidDriver<AndroidElement> driver, AppiumWebElement element, CommonProps commonProps, MobileElementValidationOptions options, ILogger logger)
        {
            if (options?.IsVerifyElementImage != true)
            {
                return;
            }

            string templatePath = ResolveTemplatePath(commonProps, options);
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                logger?.LogError("手机元素图像校验失败，模板图片路径为空或无法解析");
                throw new RPAException(eErrorMessageType.Error_MobileElementImageNotInvalid, "无法获取元素校验图像，请检查后重试");
            }
            if (!File.Exists(templatePath))
            {
                logger?.LogError($"手机元素图像校验失败，模板图片不存在，路径[{templatePath}]");
                throw new RPAException(eErrorMessageType.Error_MobileElementImageNotInvalid, "元素校验图像不存在，请重新拾取元素");
            }

            var rect = element.Rect;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                throw new ArgumentException("元素图像校验区域无效");
            }

            string tempSourcePath = string.Empty;
            try
            {
                tempSourcePath = Path.Combine(Path.GetTempPath(), $"II.RPA.MobileProvider-{Guid.NewGuid():N}.png");
                File.WriteAllBytes(tempSourcePath, driver.GetScreenshot().AsByteArray);

                var param = new ImageMatchingParams
                {
                    Source = tempSourcePath,
                    Template = templatePath,
                    Thr = ElementImageSimilarityThreshold,
                    DomainArea = new List<int> { rect.X, rect.Y, rect.Width, rect.Height },
                    OriginalPix = null
                };

                string imageRectResult = new CVImageHelper().InspectImageByCV(param);
                var rectsData = JsonConvert.DeserializeObject<List<List<List<int>>>>(imageRectResult);
                logger?.LogInformation($"手机元素图像校验匹配结果数量：{rectsData?.Count ?? 0}，阈值：{ElementImageSimilarityThreshold:F4}");
                if (rectsData == null || rectsData.Count == 0)
                {
                    logger?.LogError($"手机元素图像校验失败，未在元素区域内匹配到目标图像，阈值[{ElementImageSimilarityThreshold:F2}]");

                    throw new RPAException(eErrorMessageType.Error_MobileElementImageNotInvalid, "未找到元素图像，请检查后重试");
                }
            }
            catch (Exception ex) when (!(ex is RPAException))
            {
                logger?.LogError($"手机元素图像校验失败，未在元素区域内匹配到目标图像，阈值[{ElementImageSimilarityThreshold:F2}]。{ex.Message}");
                throw new RPAException(eErrorMessageType.Error_MobileElementImageNotInvalid, "未找到元素图像，请检查后重试");
            }
            finally
            {
                TryDeleteFile(tempSourcePath, logger);
            }
        }

        public static (string PackageName, string ActivityName) GetCurrentPage(AndroidDriver<AndroidElement> driver, ILogger logger)
        {
            string output = string.Empty;
            try
            {
                string udid = driver.Capabilities["udid"]?.ToString();
                if (!string.IsNullOrWhiteSpace(udid))
                {
                    output = AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync($"-s {udid} shell dumpsys window", CurrentPageAdbTimeoutMilliseconds).GetAwaiter().GetResult();
                    var parsed = ParseFocusedApp(output);
                    if (!string.IsNullOrWhiteSpace(parsed.PackageName))
                    {
                        return parsed;
                    }
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Failed to get current mobile page by ADB.");
                }
            }

            return (string.Empty, string.Empty);
        }

        private static (string PackageName, string ActivityName) ParseFocusedApp(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return (string.Empty, string.Empty);
            }

            foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.IndexOf("mCurrentFocus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            x.IndexOf("mFocusedApp", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var match = Regex.Match(line, @"\s([A-Za-z0-9_.$]+)/([A-Za-z0-9_.$]+)");
                if (match.Success)
                {
                    string packageName = match.Groups[1].Value;
                    string activityName = NormalizeActivity(packageName, match.Groups[2].Value);
                    return (packageName, activityName);
                }
            }

            return (string.Empty, string.Empty);
        }

        private static string ResolveTemplatePath(CommonProps commonProps, MobileElementValidationOptions options)
        {
            if (commonProps == null || string.IsNullOrWhiteSpace(commonProps.filePath))
            {
                return string.Empty;
            }
            if (Path.IsPathRooted(commonProps.filePath))
            {
                return commonProps.filePath;
            }
            if (options == null || string.IsNullOrWhiteSpace(options.ApplicationPath))
            {
                return string.Empty;
            }
            return Path.Combine(options.ApplicationPath, commonProps.filePath);
        }

        private static void TryDeleteFile(string filePath, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"Failed to delete temporary mobile screenshot: {filePath}");
                }
            }
        }

        private static string NormalizeActivity(string packageName, string activityName)
        {
            if (string.IsNullOrWhiteSpace(activityName))
            {
                return string.Empty;
            }
            return activityName.StartsWith(".") ? $"{packageName}{activityName}" : activityName;
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
