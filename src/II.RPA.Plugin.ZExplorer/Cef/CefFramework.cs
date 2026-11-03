using System;
using System.ComponentModel;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public enum CefFramework
    {
        [Description("Generic Embedded Chromium")]
        Generic,
        [Description("Cef-Sharp")]
        CefSharp,
        [Description("DotNetBrowser")]
        DotNetBrowser,
        [Description("Electron")]
        Electron,
        [Description("NW.js")]
        NWJs,
        [Description("OpenFin")]
        OpenFin,
        [Description("QtWebEngine")]
        QtWebEngine,
        [Description("JxBrowser")]
        JxBrowser,
        [Description("JCEF")]
        JCEF,
        [Description("WebView2")]
        WebView2
    }
}
