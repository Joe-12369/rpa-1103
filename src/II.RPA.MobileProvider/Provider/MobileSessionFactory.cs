using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using System;
using System.Collections.Generic;
using System.Threading;

namespace II.RPA.MobileProvider.Provider
{
    internal class MobileSessionFactory
    {
        private const int CreateSessionRetryCount = 3;
        private const int CreateSessionRetryDelayMilliseconds = 1000;

        public static string AppiumUrl => "";

        public static MobileSession CreateSession(MobileInfo mobileInfo)
        {
            Exception lastException = null;
            bool deviceAutomationRecoveryAttempted = false;
            for (int attempt = 1; attempt <= CreateSessionRetryCount; attempt++)
            {
                try
                {
                    int systemPort = MobileSystemPortAllocator.GetOrAllocate(mobileInfo.Udid);
                    long sessionGeneration = MobileAutomationStateStore.NextSessionGeneration(mobileInfo.Udid);
                    AppiumOptions desiredCapabilities = new AppiumOptions();
                    desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.PlatformName, mobileInfo.PlatformName);
                    desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.PlatformVersion, mobileInfo.PlatformVersion);
                    desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.AutomationName, "UiAutomator2");
                    desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.NewCommandTimeout, 300);
                    desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.NoReset, true);
                    desiredCapabilities.AddAdditionalCapability("disableWindowAnimation", true);
                    desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.Udid, mobileInfo.Udid);
                    if (mobileInfo.AdditionalCapabilities != null)
                    {
                        foreach (var item in mobileInfo.AdditionalCapabilities)
                        {
                            desiredCapabilities.AddAdditionalCapability(item.Key, item.Value);
                        }
                    }
                    desiredCapabilities.AddAdditionalCapability("systemPort", systemPort);

                    var androidDriver = new AndroidDriver<AndroidElement>(
                        new Uri($"http://127.0.0.1:{GlobleConfig.AppiumServerPort}/wd/hub"),
                        desiredCapabilities,
                        TimeSpan.FromSeconds(60));

                    androidDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
                    androidDriver.Settings = new Dictionary<string, object>()
                    {
                        ["waitForIdleTimeout"] = 100
                    };

                    MobileSession mobileSession = new MobileSession(androidDriver, mobileInfo.Udid, sessionGeneration, systemPort);
                    if (string.IsNullOrEmpty(mobileInfo.Udid))
                    {
                        mobileInfo.Udid = mobileSession.Udid;
                    }
                    MobileAutomationStateStore.MarkSessionCreated(mobileInfo.Udid, sessionGeneration);
                    return mobileSession;
                }
                catch (Exception ex) when (IsRetryableCreateSessionException(ex) && attempt < CreateSessionRetryCount)
                {
                    lastException = ex;
                    if (!deviceAutomationRecoveryAttempted &&
                        !string.IsNullOrWhiteSpace(mobileInfo.Udid) &&
                        MobileSessionRecovery.ShouldResetAfterCreateFailure(ex))
                    {
                        MobileSessionRecovery.ResetDetachedSession(
                            mobileInfo.Udid,
                            null,
                            $"CreateSessionFailed-Attempt{attempt}");
                        deviceAutomationRecoveryAttempted = true;
                    }
                    Thread.Sleep(CreateSessionRetryDelayMilliseconds);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    break;
                }
            }

            throw lastException ?? new Exception("Create mobile session failed");
        }

        private static bool IsRetryableCreateSessionException(Exception ex)
        {
            if (ex == null)
            {
                return false;
            }

            if (MobileSessionRecovery.ShouldResetAfterCreateFailure(ex))
            {
                return true;
            }

            string text = ex.ToString();
            return text.Contains("SocketException")
                || text.Contains("Connection refused")
                || text.Contains("ECONNREFUSED")
                || text.Contains("Unable to connect")
                || text.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
