using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebExtensionInstall
{
    internal enum WebExtensionStatue
    {
        ChromeExtensionUnInstall = 0,
        ChromeExtensionInstallSuccess = 1,
        ChromeUnInstall = 2,// 界面展示  如需安装Chrome插件，请先安装Chrome浏览器 "https://www.google.cn/chrome"，再点击确定。
        ChromeNotClose = 3,//面展示 如需安装Chrome插件，请先关闭Chrome浏览器，再点击确定。
        FileCopyFail = 4,//界面展示 文件复制失败，查看原因。
        RegeditWriteFail = 5, //界面展示 注册表写入失败，查看原因。
        FireFoxExtensionUnInstall = 6,
        FireFoxUnInstall = 7,
        FireFoxNotClose = 8,
        FireFoxExtensionInstallSuccess = 9,
        WebbrowserLowversion = 10,

        CloseBrowserFail = 11,
        CloseBrowserSuccess = 12,

        ServiceNotFound = 13,
        BrowserNotFound = 14,

        SougouExtension = 15,//12版本的搜狗浏览器，请按照安装说明，手动完成插件拖入浏览器操作

        
        ChromiumExtensionInstallSuccess = 17,
        UnsupportedBrowser = 18,
        DetectChromiumCompleted = 19,
        IsChromiumBrowser = 20,
        NotChromiumBrowser = 21,
        Unknown = 99 //未知异常
    }
    internal enum ReturnStatus
    {
        Cancel = -1,//取消
        Fail = 0,//失败
        Success = 1,//成功
    }
}
