using II.RPA.Logging;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Commons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using II.RPA.Plugin.GUI.Plugins;
using II.RPA.Plugin.GUI.Plugins.Chatrpa;
using II.RPA.Plugin.GUI.Plugins.DataCollection;

namespace II.RPA.Plugin.GUI
{
    public sealed class PluginPackage : PluginPackageBase
    {
        private static readonly ILogger Logger = II.RPA.Logging.Logger.OfContext();

        public override string PackageName => PluginProtocol.PackageName;

        protected override void Configure(PluginPackageBuilder builder)
        {
            RegisterAbilities(builder);
            var elementDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
            var runtimeDirectory = elementDirectory == null ? null : Directory.GetParent(elementDirectory)?.FullName;
            if (string.IsNullOrWhiteSpace(runtimeDirectory)) throw new DirectoryNotFoundException("无法确定 Element runtime 目录。");

            var moduleNames = Configurations.Info.Category
                .Where(category => !GuiElement.IsWebSelectorType(category.Name.Replace("GUI", string.Empty)))
                .OrderBy(category => category.Sort)
                .Select(category => category.Name)
                .ToArray();

            new GuiModuleLoader().ConfigureModules(runtimeDirectory, moduleNames, new GuiModuleBuilder(builder.Services));
            builder.Services.AddSingleton<GuiRuntimeServices>();
            builder.Services.AddSingleton<IStaDispatcher, StaDispatcher>();
            builder.Services.AddSingleton<GuiHighlightService>();
        }

        private static void RegisterAbilities(PluginPackageBuilder builder)
        {
            Register<CancelCapture>(builder, PluginProtocol.AbilityNames.CancelCapture, concurrencyMode: AbilityConcurrencyMode.Parallel);
            Register<CaptureForAgent>(builder, PluginProtocol.AbilityNames.CaptureForAgent);
            Register<CaptureCV>(builder, PluginProtocol.AbilityNames.CaptureCV);
            Register<Capture>(builder, PluginProtocol.AbilityNames.Capture);
            Register<CaptureByRegion>(builder, PluginProtocol.AbilityNames.CaptureByRegion);
            Register<CaptureCVForOcrTable>(builder, PluginProtocol.AbilityNames.CaptureCVForOcrTable);
            Register<CapturePosition>(builder, PluginProtocol.AbilityNames.CapturePosition);
            Register<CaptureRenovate>(builder, PluginProtocol.AbilityNames.CaptureRenovate);
            Register<CaptureScreenForOcrTable>(builder, PluginProtocol.AbilityNames.CaptureScreenForOcrTable);
            Register<CaptureWindow>(builder, PluginProtocol.AbilityNames.CaptureWindow);
            Register<CheckElementsExist>(builder, PluginProtocol.AbilityNames.CheckElementsExist);
            Register<ClearChatRpaCacheElement>(builder, PluginProtocol.AbilityNames.ClearChatRpaCacheElement);
            Register<CollectRawPageData>(builder, PluginProtocol.AbilityNames.CollectRawPageData);
            Register<CyclicSimilarElementConfig>(builder, PluginProtocol.AbilityNames.CyclicSimilarElementConfig, AbilityLifetime.RuntimeSingleton);
            Register<DataCollectConfig>(builder, PluginProtocol.AbilityNames.DataCollectConfig, AbilityLifetime.RuntimeSingleton);
            Register<ExtractWinndowInfo>(builder, PluginProtocol.AbilityNames.ExtractWinndowInfo, AbilityLifetime.RuntimeSingleton);
            Register<GetElementSelector>(builder, PluginProtocol.AbilityNames.GetElementSelector);
            Register<GetMemoryElementSelector>(builder, PluginProtocol.AbilityNames.GetMemoryElementSelector);
            Register<GetTargetElementInfo>(builder, PluginProtocol.AbilityNames.GetTargetElementInfo, AbilityLifetime.RuntimeSingleton);
            Register<ScreenshotCapture>(builder, PluginProtocol.AbilityNames.ScreenshotCapture, concurrencyMode: AbilityConcurrencyMode.Parallel);
            Register<SmartSelector>(builder, PluginProtocol.AbilityNames.SmartSelector);
            Register<GetLineAndBarChartData>(builder, PluginProtocol.AbilityNames.GetLineAndBarChartData);
            Register<GetPieChartData>(builder, PluginProtocol.AbilityNames.GetPieChartData);
            Register<ReleaseGuiResources>(builder, PluginProtocol.AbilityNames.ReleaseGuiResources);
            Register<CheckImage>(builder, PluginProtocol.AbilityNames.CheckImage, AbilityLifetime.RuntimeSingleton);
            Register<DeepCapture>(builder, PluginProtocol.AbilityNames.DeepCapture, AbilityLifetime.RuntimeSingleton);
            Register<DataCollect_v2>(builder, PluginProtocol.AbilityNames.DataCollect_v2);
            Register<DataCollectCapture>(builder, PluginProtocol.AbilityNames.DataCollectCapture, concurrencyMode: AbilityConcurrencyMode.Parallel);
            Register<Downloader>(builder, PluginProtocol.AbilityNames.Downloader);
            Register<FileUploader>(builder, PluginProtocol.AbilityNames.FileUploader);
            Register<GetElementInfo>(builder, PluginProtocol.AbilityNames.GetElementInfo);
            Register<GetElement_V2>(builder, PluginProtocol.AbilityNames.GetElement_V2);
            Register<GetElementSelect>(builder, PluginProtocol.AbilityNames.GetElementSelect);
            Register<Download>(builder, PluginProtocol.AbilityNames.Download);
            Register<ElementExecutejs>(builder, PluginProtocol.AbilityNames.ElementExecutejs);
            Register<GetElement>(builder, PluginProtocol.AbilityNames.GetElement);
            Register<GetSelectorByPoint>(builder, PluginProtocol.AbilityNames.GetSelectorByPoint);
            Register<GetSimilarElementList>(builder, PluginProtocol.AbilityNames.GetSimilarElementList);
            Register<HighlightScriptElement>(builder, PluginProtocol.AbilityNames.HighlightScriptElement);
            Register<ScreenShotExecutor>(builder, PluginProtocol.AbilityNames.ScreenShotExecutor);
            Register<Plugins.ProcessCreator>(builder, PluginProtocol.AbilityNames.ProcessCreator, AbilityLifetime.RuntimeSingleton);
            Register<ClickSapTableCell>(builder, PluginProtocol.AbilityNames.ClickSapTableCell);
            Register<DataCollect>(builder, PluginProtocol.AbilityNames.DataCollect);
            Register<DataCollectConfiguration>(builder, PluginProtocol.AbilityNames.DataCollectConfiguration);
            Register<ElementScreenshot>(builder, PluginProtocol.AbilityNames.ElementScreenshot);
            Register<FindRelativeElementsInfo>(builder, PluginProtocol.AbilityNames.FindRelativeElementsInfo);
            Register<GetAttribute>(builder, PluginProtocol.AbilityNames.GetAttribute);
            Register<GetButtonEnable>(builder, PluginProtocol.AbilityNames.GetButtonEnable);
            Register<GetCVPriceAmount>(builder, PluginProtocol.AbilityNames.GetCVPriceAmount);
            Register<GetCVText>(builder, PluginProtocol.AbilityNames.GetCVText);
            Register<GetElementCheck>(builder, PluginProtocol.AbilityNames.GetElementCheck);
            Register<GetElementForOcrTable>(builder, PluginProtocol.AbilityNames.GetElementForOcrTable);
            Register<GetElementSelectEx>(builder, PluginProtocol.AbilityNames.GetElementSelectEx);
            Register<GetElementSelectorAll>(builder, PluginProtocol.AbilityNames.GetElementSelectorAll);
            Register<GetHtml>(builder, PluginProtocol.AbilityNames.GetHtml);
            Register<GetImageByCaptureScreenOnApp>(builder, PluginProtocol.AbilityNames.GetImageByCaptureScreenOnApp);
            Register<GetRegion>(builder, PluginProtocol.AbilityNames.GetRegion);
            Register<GetSapTableData>(builder, PluginProtocol.AbilityNames.GetSapTableData);
            Register<GetSiblingsElements>(builder, PluginProtocol.AbilityNames.GetSiblingsElements);
            Register<GetParent>(builder, PluginProtocol.AbilityNames.GetParent);
            Register<GetSubElements>(builder, PluginProtocol.AbilityNames.GetSubElements);
            Register<GetText>(builder, PluginProtocol.AbilityNames.GetText);
            Register<IsElementExist>(builder, PluginProtocol.AbilityNames.IsElementExist);
            Register<IsSimilarElementExist>(builder, PluginProtocol.AbilityNames.IsSimilarElementExist);
            Register<MouseClick>(builder, PluginProtocol.AbilityNames.MouseClick);
            Register<MouseHover>(builder, PluginProtocol.AbilityNames.MouseHover);
            Register<StopCapture>(builder, PluginProtocol.AbilityNames.StopCapture);
            Register<RemoteCapture>(builder, PluginProtocol.AbilityNames.RemoteCapture);
            Register<CaptureScreen>(builder, PluginProtocol.AbilityNames.CaptureScreen, AbilityLifetime.RuntimeSingleton);
            Register<Plugins.ScreenShot>(builder, PluginProtocol.AbilityNames.ScreenShot, AbilityLifetime.RuntimeSingleton);
            Register<ScreenShotV2>(builder, PluginProtocol.AbilityNames.ScreenShotV2, AbilityLifetime.RuntimeSingleton);
            Register<ScrollIntoViewIfNeeded>(builder, PluginProtocol.AbilityNames.ScrollIntoViewIfNeeded);
            Register<SetAttribute>(builder, PluginProtocol.AbilityNames.SetAttribute);
            Register<SetElementCheck>(builder, PluginProtocol.AbilityNames.SetElementCheck);
            Register<SetElementSelect_V2>(builder, PluginProtocol.AbilityNames.SetElementSelect_V2);
            Register<SetElementSelect>(builder, PluginProtocol.AbilityNames.SetElementSelect);
            Register<Test>(builder, PluginProtocol.AbilityNames.Test);
            Register<SetText>(builder, PluginProtocol.AbilityNames.SetText);
            Register<CaptureSimilar>(builder, PluginProtocol.AbilityNames.CaptureSimilar);
            Register<SharpUIProvider>(builder, PluginProtocol.AbilityNames.SharpUIProvider, AbilityLifetime.RuntimeSingleton);
        }

        private static void Register<TAbility>(
            PluginPackageBuilder builder,
            string abilityName,
            AbilityLifetime lifetime = AbilityLifetime.Transient,
            AbilityConcurrencyMode concurrencyMode = AbilityConcurrencyMode.RejectWhenBusy)
            where TAbility : class, IPluginAbility
        {
            builder.AddAbility<TAbility>(
                abilityName,
                lifetime,
                concurrencyMode);
        }

        protected override async Task InitializePackageAsync(IPluginInvoker pluginInvoker)
        {
            var guiRuntime = GetRequiredPackageService<GuiRuntimeServices>();
            GuiRuntime.Initialize(guiRuntime);
            ConfigureOperatorPriorities(guiRuntime.Operators, Configurations.Info.Category, Configurations.Info.JavaMode);

            GuiManager.Initialize(guiRuntime);
            GlobleConfigurations.CurrentSelectorVersion = "1.6";

            var staDispatcher = GetRequiredPackageService<IStaDispatcher>();
            await staDispatcher.InvokeAsync(WarmUpInspector).ConfigureAwait(false);
            Logger.LogInformation($"Element 应用程序域:{AppDomain.CurrentDomain.FriendlyName}");
            Logger.LogInformation($"Element 应用程序域寻址:{AppDomain.CurrentDomain.BaseDirectory}");
        }

        internal static void ConfigureOperatorPriorities(
            System.Collections.Generic.IEnumerable<IGuiOperator> operators,
            System.Collections.Generic.IEnumerable<GUICategory> categories,
            string javaMode)
        {
            foreach (var category in categories)
            {
                var guiTypeName = category.Name.Replace("GUI", string.Empty);
                // JavaAlter is the module identity; its selector/GUI identity is Swing.
                if (string.Equals(guiTypeName, "JavaAlter", StringComparison.OrdinalIgnoreCase))
                    guiTypeName = "Swing";
                var guiOperator = operators.FirstOrDefault(value =>
                    string.Equals(value.GuiType.ToString(), guiTypeName, StringComparison.OrdinalIgnoreCase));
                if (guiOperator != null) guiOperator.Sort = category.Sort;
            }

            var javaOperator = operators.FirstOrDefault(value =>
                string.Equals(value.GuiType.ToString(), "Swing", StringComparison.OrdinalIgnoreCase));
            if (javaOperator != null)
            {
                javaOperator.Sort *= javaMode.Equals("agent") ? 1 : -1;
            }
        }

        private static void WarmUpInspector()
        {
            GuiElement.InitializeOverlayDispatcher();
            Logger.LogInformation($"初始化GuiOperators：{string.Join(",", GuiManager.Info.GuiOperators.Select(value => value.GuiType.ToString()))}");
            Logger.LogInformation("开始初始化 InspectInfoWnd");
            var startTime = DateTime.Now;
            if (GuiElement.overlayForm == null) GuiElement.overlayForm = new OverlayForm();
            try
            {
                if (GuiElement.overlayForm.inspectorStatus == null) GuiElement.overlayForm.inspectorStatus = new InspectInfoWnd();
            }
            catch (Exception exception)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(Logger, exception, "预热 InspectInfoWnd 失败，降级为使用时再创建");
            }
            Logger.LogInformation($"结束初始化 InspectInfoWnd:{(DateTime.Now - startTime).TotalMilliseconds} ms");
        }

    }

    internal static class PluginProtocol
    {
        internal const string PackageName = "Element";

        internal static class AbilityNames
        {
            internal const string CancelCapture = "CancelCapture";
            internal const string Capture = "Capture";
            internal const string CaptureByRegion = "CaptureByRegion";
            internal const string CaptureCV = "CaptureCV";
            internal const string CaptureCVForOcrTable = "CaptureCVForOcrTable";
            internal const string CaptureForAgent = "CaptureForAgent";
            internal const string CapturePosition = "CapturePosition";
            internal const string CaptureRenovate = "CaptureRenovate";
            internal const string CaptureScreen = "CaptureScreen";
            internal const string CaptureScreenForOcrTable = "CaptureScreenForOcrTable";
            internal const string CaptureSimilar = "CaptureSimilar";
            internal const string CaptureWindow = "CaptureWindow";
            internal const string CheckElementsExist = "CheckElementsExist";
            internal const string CheckImage = "CheckImage";
            internal const string ClearChatRpaCacheElement = "ClearChatRpaCacheElement";
            internal const string ClickSapTableCell = "ClickSapTableCell";
            internal const string CollectRawPageData = "CollectRawPageData";
            internal const string CyclicSimilarElementConfig = "CyclicSimilarElementConfig";
            internal const string DataCollect = "DataCollect";
            internal const string DataCollect_v2 = "DataCollect_v2";
            internal const string DataCollectCapture = "DataCollectCapture";
            internal const string DataCollectConfig = "DataCollectConfig";
            internal const string DataCollectConfiguration = "DataCollectConfiguration";
            internal const string DeepCapture = "DeepCapture";
            internal const string Download = "Download";
            internal const string Downloader = "Downloader";
            internal const string ElementExecutejs = "ElementExecutejs";
            internal const string ElementScreenshot = "ElementScreenshot";
            internal const string ExtractWinndowInfo = "ExtractWinndowInfo";
            internal const string FileUploader = "FileUploader";
            internal const string FindRelativeElementsInfo = "FindRelativeElementsInfo";
            internal const string GetAttribute = "GetAttribute";
            internal const string GetButtonEnable = "GetButtonEnable";
            internal const string GetCVPriceAmount = "GetCVPriceAmount";
            internal const string GetCVText = "GetCVText";
            internal const string GetElement = "GetElement";
            internal const string GetElement_V2 = "GetElement_V2";
            internal const string GetElementCheck = "GetElementCheck";
            internal const string GetElementForOcrTable = "GetElementForOcrTable";
            internal const string GetElementInfo = "GetElementInfo";
            internal const string GetElementSelect = "GetElementSelect";
            internal const string GetElementSelectEx = "GetElementSelectEx";
            internal const string GetElementSelector = "GetElementSelector";
            internal const string GetElementSelectorAll = "GetElementSelectorAll";
            internal const string GetHtml = "GetHtml";
            internal const string GetImageByCaptureScreenOnApp = "GetImageByCaptureScreenOnApp";
            internal const string GetLineAndBarChartData = "GetLineAndBarChartData";
            internal const string GetMemoryElementSelector = "GetMemoryElementSelector";
            internal const string GetParent = "GetParent";
            internal const string GetPieChartData = "GetPieChartData";
            internal const string GetRegion = "GetRegion";
            internal const string GetSapTableData = "GetSapTableData";
            internal const string GetSelectorByPoint = "GetSelectorByPoint";
            internal const string GetSiblingsElements = "GetSiblingsElements";
            internal const string GetSimilarElementList = "GetSimilarElementList";
            internal const string GetSubElements = "GetSubElements";
            internal const string GetTargetElementInfo = "GetTargetElementInfo";
            internal const string GetText = "GetText";
            internal const string HighlightScriptElement = "HighlightScriptElement";
            internal const string IsElementExist = "IsElementExist";
            internal const string IsSimilarElementExist = "IsSimilarElementExist";
            internal const string MouseClick = "MouseClick";
            internal const string MouseHover = "MouseHover";
            internal const string ProcessCreator = "ProcessCreator";
            internal const string ReleaseGuiResources = "ReleaseGuiResources";
            internal const string RemoteCapture = "RemoteCapture";
            internal const string ScreenShot = "ScreenShot";
            internal const string ScreenshotCapture = "ScreenshotCapture";
            internal const string ScreenShotExecutor = "ScreenShotExecutor";
            internal const string ScreenShotV2 = "ScreenShotV2";
            internal const string ScrollIntoViewIfNeeded = "ScrollIntoViewIfNeeded";
            internal const string SetAttribute = "SetAttribute";
            internal const string SetElementCheck = "SetElementCheck";
            internal const string SetElementSelect = "SetElementSelect";
            internal const string SetElementSelect_V2 = "SetElementSelect_V2";
            internal const string SetText = "SetText";
            internal const string SharpUIProvider = "SharpUIProvider";
            internal const string SmartSelector = "SmartSelector";
            internal const string StopCapture = "StopCapture";
            internal const string Test = "Test";
        }
    }
}
