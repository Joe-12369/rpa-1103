using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.WebBrowser.Common;
using Microsoft.Extensions.DependencyInjection;

namespace II.RPA.Plugin.WebBrowser
{
    public sealed class PluginPackage : PluginPackageBase
    {
        public override string PackageName => PluginProtocol.PackageName;

        protected override void Configure(PluginPackageBuilder builder)
        {
            RegisterAbilities(builder);
            builder.Services.AddSingleton<IStaDispatcher, StaDispatcher>();
        }

        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            Register<Plugins.Acquire>(builder, PluginProtocol.AbilityNames.Acquire);
            Register<Plugins.Active>(builder, PluginProtocol.AbilityNames.Active);
            Register<Plugins.UploadDialog>(builder, PluginProtocol.AbilityNames.UploadDialog);
            Register<Plugins.DownloadDialog>(builder, PluginProtocol.AbilityNames.DownloadDialog);
            Register<Plugins.EndInspectNetwork>(builder, PluginProtocol.AbilityNames.EndInspectNetwork);
            Register<Plugins.GetInspectNetwork>(builder, PluginProtocol.AbilityNames.GetInspectNetwork);
            Register<Plugins.GetScroll_V2>(builder, PluginProtocol.AbilityNames.GetScroll_V2);
            Register<Plugins.GetSupportedBrowserList>(builder, PluginProtocol.AbilityNames.GetSupportedBrowserList);
            Register<Plugins.MultipleClose>(builder, PluginProtocol.AbilityNames.MultipleClose);
            Register<Plugins.Scroll_V2>(builder, PluginProtocol.AbilityNames.Scroll_V2);
            Register<Plugins.StartInspectNetwork>(builder, PluginProtocol.AbilityNames.StartInspectNetwork);
            Register<Plugins.Close>(builder, PluginProtocol.AbilityNames.Close);
            Register<Plugins.CloseAll>(builder, PluginProtocol.AbilityNames.CloseAll);
            Register<Plugins.CaptureScreenshot>(builder, PluginProtocol.AbilityNames.CaptureScreenshot);
            Register<Plugins.StartCapture>(builder, PluginProtocol.AbilityNames.StartCapture);
            Register<Plugins.StartConfig>(builder, PluginProtocol.AbilityNames.StartConfig);
            Register<Plugins.Download>(builder, PluginProtocol.AbilityNames.Download);
            Register<Plugins.DownloadUrl>(builder, PluginProtocol.AbilityNames.DownloadUrl);
            Register<Plugins.ElementExecutejs>(builder, PluginProtocol.AbilityNames.ElementExecutejs);
            Register<Plugins.Executejs>(builder, PluginProtocol.AbilityNames.Executejs);
            Register<Plugins.GetCookies>(builder, PluginProtocol.AbilityNames.GetCookies);
            Register<Plugins.GetDocument>(builder, PluginProtocol.AbilityNames.GetDocument);
            Register<Plugins.GetScroll>(builder, PluginProtocol.AbilityNames.GetScroll);
            Register<Plugins.GetTabDetail>(builder, PluginProtocol.AbilityNames.GetTabDetail);
            Register<Plugins.GetTitle>(builder, PluginProtocol.AbilityNames.GetTitle);
            Register<Plugins.GetUrl>(builder, PluginProtocol.AbilityNames.GetUrl);
            Register<Plugins.GoBack>(builder, PluginProtocol.AbilityNames.GoBack);
            Register<Plugins.GoForward>(builder, PluginProtocol.AbilityNames.GoForward);
            Register<Plugins.IsLoaded>(builder, PluginProtocol.AbilityNames.IsLoaded);
            Register<Plugins.Maximized>(builder, PluginProtocol.AbilityNames.Maximized);
            Register<Plugins.Minimized>(builder, PluginProtocol.AbilityNames.Minimized);
            Register<Plugins.Navigate>(builder, PluginProtocol.AbilityNames.Navigate);
            Register<Plugins.ReLoad>(builder, PluginProtocol.AbilityNames.ReLoad);
            Register<Plugins.RemoveCookie>(builder, PluginProtocol.AbilityNames.RemoveCookie);
            Register<Plugins.Screenshot>(builder, PluginProtocol.AbilityNames.Screenshot);
            Register<Plugins.Scroll>(builder, PluginProtocol.AbilityNames.Scroll);
            Register<Plugins.SetCookie>(builder, PluginProtocol.AbilityNames.SetCookie);
            Register<Plugins.Stop>(builder, PluginProtocol.AbilityNames.Stop);
            Register<Plugins.Switch>(builder, PluginProtocol.AbilityNames.Switch);
            Register<Plugins.Wait>(builder, PluginProtocol.AbilityNames.Wait);
            Register<Plugins.Create>(builder, PluginProtocol.AbilityNames.Create);
        }

        private static void Register<TAbility>(PluginPackageBuilder builder, string abilityName)
            where TAbility : class, IPluginAbility
        {
            builder.AddAbility<TAbility>(
                abilityName,
                AbilityLifetime.Transient,
                AbilityConcurrencyMode.RejectWhenBusy);
        }
    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "WebBrowser";

        internal static class AbilityNames
        {
            internal const string Acquire = "Acquire";
            internal const string Active = "Active";
            internal const string CaptureScreenshot = "CaptureScreenshot";
            internal const string Close = "Close";
            internal const string CloseAll = "CloseAll";
            internal const string Create = "Create";
            internal const string Download = "Download";
            internal const string DownloadDialog = "DownloadDialog";
            internal const string DownloadUrl = "DownloadUrl";
            internal const string ElementExecutejs = "ElementExecutejs";
            internal const string EndInspectNetwork = "EndInspectNetwork";
            internal const string Executejs = "Executejs";
            internal const string GetCookies = "GetCookies";
            internal const string GetDocument = "GetDocument";
            internal const string GetInspectNetwork = "GetInspectNetwork";
            internal const string GetScroll = "GetScroll";
            internal const string GetScroll_V2 = "GetScroll_V2";
            internal const string GetSupportedBrowserList = "GetSupportedBrowserList";
            internal const string GetTabDetail = "GetTabDetail";
            internal const string GetTitle = "GetTitle";
            internal const string GetUrl = "GetUrl";
            internal const string GoBack = "GoBack";
            internal const string GoForward = "GoForward";
            internal const string IsLoaded = "IsLoaded";
            internal const string Maximized = "Maximized";
            internal const string Minimized = "Minimized";
            internal const string MultipleClose = "MultipleClose";
            internal const string Navigate = "Navigate";
            internal const string ReLoad = "ReLoad";
            internal const string RemoveCookie = "RemoveCookie";
            internal const string Screenshot = "Screenshot";
            internal const string Scroll = "Scroll";
            internal const string Scroll_V2 = "Scroll_V2";
            internal const string SetCookie = "SetCookie";
            internal const string StartCapture = "StartCapture";
            internal const string StartConfig = "StartConfig";
            internal const string StartInspectNetwork = "StartInspectNetwork";
            internal const string Stop = "Stop";
            internal const string Switch = "Switch";
            internal const string UploadDialog = "UploadDialog";
            internal const string Wait = "Wait";
        }
    }
}
