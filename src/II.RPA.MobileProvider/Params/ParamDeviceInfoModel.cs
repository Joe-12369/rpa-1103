using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamConnectInfo
    {
        public ConnectType connectType { get; set; }
        public List<ParamDeviceInfoModel> devices { get; set; }

        public bool isAutoUnlock { get; set; }
        public UnlockType unlockType { get; set; }
        public string unlockPw { get; set; }
    }
    public enum ConnectType
    {
        /// <summary>
        /// 指定手机
        /// </summary>
        Refer,
        /// <summary>
        /// 运行前手动选择
        /// </summary>
        ManualSelect,
        /// <summary>
        /// 任意连接的单台手机
        /// </summary>
        AnyOne,
        /// <summary>
        /// 所有手机
        /// </summary>
        All
    }
    /// <summary>
    /// 解锁类型枚举
    /// </summary>
    public enum UnlockType
    {
        /// <summary>
        /// 图案解锁
        /// </summary>
        Pattern,
        /// <summary>
        /// PIN码解锁（纯数字）
        /// </summary>
        Pin,
        /// <summary>
        /// 字母数字混合解锁
        /// </summary>
        Alphanumeric
    }
    /// <summary>
    /// 本类的字段大部分需要传递给driver初始化，不是driver默认的capability的话，需要在属性上加[DescriptionAttribute("ignore")]，否则会出错
    /// </summary>
    public class ParamDeviceInfoModel
    {
        #region 自定义的字段,不能直接用来初始化Driver
        /// <summary>
        /// 自定义名称
        /// </summary>
        [DescriptionAttribute("ignore")]
        public string customDeviceName { get; set; }
        /// <summary>
        /// 同步频率
        /// </summary>
        [DescriptionAttribute("ignore")]
        public int syncFrequency { get; set; }

        #endregion

        #region 系统自带的配置字段
        public string deviceName { get; set; }
        /// <summary>
        /// Appium 地址
        /// </summary>
        public string appiumAddress { get; set; }
        /// <summary>
        /// 系统名称，Android or iOS or FirefoxOS
        /// </summary>
        public string platformName { get; set; } = "android";
        /// <summary>
        /// 系统版本，主要是Android 版本
        /// </summary>
        public string platformVersion { get; set; }
        public int timeout { get; set; } = 30000;

        /// <summary>
        /// Automation 版本
        /// Appium (default), or UiAutomator2, Espresso, or UiAutomator1 for Android, 
        /// or XCUITest or Instruments for iOS
        /// </summary>
        public string automationName { get; set; }

        #region 能力扩展
        public string app { get; set; }
        public string otherApps { get; set; }
        /// <summary>
        /// Name of mobile web browser to automate. Should be an empty string if automating an app instead
        /// </summary>
        public string browserName { get; set; }
        /// <summary>
        /// How long (in seconds) Appium will wait for a new command from the client before assuming the client quit and ending the session
        /// </summary>
        public int newCommandTimeout { get; set; }
        /// <summary>
        /// Language to set for iOS (XCUITest driver only) and Android
        /// </summary>
        public string language { get; set; }

        public string orientation { get; set; }

        public bool autoWebview { get; set; }
        /// <summary>
        /// Don't reset app state before this session
        /// </summary>
        public bool noReset { get; set; }
        /// <summary>
        /// Perform a complete reset
        /// </summary>
        public bool fullReset { get; set; }

        public string appActivity { get; set; }

        public string appPackage { get; set; }

        public string appWaitActivity { get; set; }

        public string appWaitPackage { get; set; }
        /// <summary>
        /// Timeout in milliseconds used to wait for the appWaitActivity to launch (default 20000)
        /// </summary>
        public int appWaitDuration { get; set; }
        /// <summary>
        /// Timeout in seconds while waiting for device to become ready
        /// </summary>
        public int deviceReadyTimeout { get; set; }
        /// <summary>
        /// Port used to connect to the ADB server (default 5037)
        /// </summary>
        public int adbPort { get; set; }

        #endregion

        #endregion
    }
}
