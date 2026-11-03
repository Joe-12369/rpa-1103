using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using System.Reflection;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Plugins;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Xunit;

namespace II.RPA.Plugin.GUI.Tests;

public sealed class GuiComponentBaseTests
{
    [Fact]
    public void Element_package_registers_all_compiled_abilities_with_explicit_concurrency()
    {
        var builder = new PluginPackageBuilder();
        var method = typeof(PluginPackage).GetMethod("RegisterAbilities", BindingFlags.NonPublic | BindingFlags.Static)!;

        method.Invoke(null, new object[] { builder });
        var registrationsProperty = typeof(PluginPackageBuilder).GetProperty("Registrations", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var registrations = (IReadOnlyDictionary<string, AbilityRegistration>)registrationsProperty.GetValue(builder)!;

        Assert.Equal(80, registrations.Count);
        Assert.Equal(typeof(global::II.RPA.Plugin.GUI.Capture), registrations["Capture"].AbilityType);
        Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registrations["Capture"].ConcurrencyMode);
        Assert.Equal(AbilityConcurrencyMode.Parallel, registrations["CancelCapture"].ConcurrencyMode);
        Assert.Equal(3, registrations.Values.Count(value => value.ConcurrencyMode == AbilityConcurrencyMode.Parallel));
        Assert.Equal(AbilityLifetime.RuntimeSingleton, registrations["CaptureScreen"].Lifetime);
        Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registrations["CaptureScreen"].ConcurrencyMode);
        Assert.Equal(AbilityLifetime.RuntimeSingleton, registrations["ProcessCreator"].Lifetime);
        Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registrations["ProcessCreator"].ConcurrencyMode);
        Assert.Equal(AbilityLifetime.RuntimeSingleton, registrations["DeepCapture"].Lifetime);
        Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registrations["DeepCapture"].ConcurrencyMode);
        var longLivedThreadAbilities = new[]
        {
            "CheckImage",
            "CyclicSimilarElementConfig",
            "DataCollectConfig",
            "ExtractWinndowInfo",
            "GetTargetElementInfo",
            "ScreenShot",
            "ScreenShotV2",
            "SharpUIProvider",
        };
        Assert.All(
            longLivedThreadAbilities,
            abilityName =>
            {
                Assert.Equal(AbilityLifetime.RuntimeSingleton, registrations[abilityName].Lifetime);
                Assert.Equal(AbilityConcurrencyMode.RejectWhenBusy, registrations[abilityName].ConcurrencyMode);
            });
    }

    [Fact]
    public void Element_abilities_use_typed_validation_and_execute_contracts_only()
    {
        var registrations = GetRegistrations();

        Assert.All(registrations.Values, registration =>
        {
            var methods = registration.AbilityType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.DoesNotContain(methods, method => method.Name == "ParamVerification");
            Assert.DoesNotContain(methods, method => method.Name == "Excute");
        });
        Assert.DoesNotContain(
            typeof(ElementAbilityBase<>).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic),
            method => method.Name == "ParamVerification" || method.Name == "Excute");
    }

    private static IReadOnlyDictionary<string, AbilityRegistration> GetRegistrations()
    {
        var builder = new PluginPackageBuilder();
        var method = typeof(PluginPackage).GetMethod("RegisterAbilities", BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, new object[] { builder });
        var registrationsProperty = typeof(PluginPackageBuilder).GetProperty("Registrations", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (IReadOnlyDictionary<string, AbilityRegistration>)registrationsProperty.GetValue(builder)!;
    }

    [Fact]
    public async Task RunAsync_uses_typed_validation_preparation_and_execution_pipeline()
    {
        var ability = new TestGuiAbility();
        var context = new PluginExecutionContext(
            new PluginRoute("Element", "Test"),
            "{\"selector\":\"selector\",\"componentLabel\":\"label\"}",
            "invocation");

        var response = JsonConvert.DeserializeObject<JMessage>(await ability.RunAsync(context))!;

        Assert.Equal(0, response.Code);
        Assert.Equal(new[] { "Validate", "Normalize", "GuiPrepare", "AbilityPrepare", "Execute" }, ability.Events);
        Assert.Equal("selector-normalized", response.Info);
    }

    [Fact]
    public async Task RunAsync_preserves_gui_preparation_failure_and_skips_execute()
    {
        var failure = new JMessage
        {
            Code = 765432,
            UnifyCode = "GUI-765432",
            Message = "browser activation failed",
            Extend = "{\"browser\":\"failed\"}"
        };
        var ability = new TestGuiAbility { PreparationResult = failure };
        var context = new PluginExecutionContext(
            new PluginRoute("Element", "Test"),
            "{\"selector\":\"selector\"}",
            "invocation");

        var response = JsonConvert.DeserializeObject<JMessage>(await ability.RunAsync(context))!;

        Assert.Equal(new[] { "Validate", "Normalize", "GuiPrepare" }, ability.Events);
        Assert.Equal(failure.Code, response.Code);
        Assert.Equal(failure.UnifyCode, response.UnifyCode);
        Assert.Equal(failure.Message, response.Message);
        Assert.Equal(failure.Extend, response.Extend);
    }

    private sealed class TestGuiAbility : GuiComponentBase<TestParameter>
    {
        public List<string> Events { get; } = new();
        public JMessage? PreparationResult { get; set; }
        protected override eComponentType ComponentType => eComponentType.None;
        protected override void ValidateParameter(TestParameter parameter)
        {
            Events.Add("Validate");
            if (string.IsNullOrEmpty(parameter.Selector)) throw new InvalidOperationException("selector required");
        }
        protected override void NormalizeParameter(TestParameter parameter)
        {
            Events.Add("Normalize");
            parameter.Selector += "-normalized";
        }
        protected override Task<JMessage?> PrepareGuiExecutionAsync(TestParameter parameter, PluginExecutionContext context)
        {
            Events.Add("GuiPrepare");
            return Task.FromResult(PreparationResult);
        }
        protected override Task<JMessage?> PrepareAbilityExecutionAsync(TestParameter parameter, PluginExecutionContext context)
        {
            Events.Add("AbilityPrepare");
            return Task.FromResult<JMessage?>(null);
        }
        protected override JMessage Execute(TestParameter parameter, PluginExecutionContext context)
        {
            Events.Add("Execute");
            return new JMessage { Info = parameter.Selector };
        }
    }

    private sealed class TestParameter : ParamBase
    {
    }

    [Fact]
    public void Data_collect_configuration_validation_does_not_apply_defaults()
    {
        var ability = new TestDataCollectConfiguration();
        var parameter = new ParamDataCollectConfigurationModel { WebData = null };

        ability.Validate(parameter);

        Assert.Null(parameter.WebData);
    }

    [Fact]
    public void Data_collect_configuration_normalization_applies_empty_web_data_default()
    {
        var ability = new TestDataCollectConfiguration();
        var parameter = new ParamDataCollectConfigurationModel { WebData = null };

        ability.Normalize(parameter);

        Assert.Equal("{}", parameter.WebData);
    }

    private sealed class TestDataCollectConfiguration : DataCollectConfiguration
    {
        public TestDataCollectConfiguration() : base(new InlineStaDispatcher())
        {
        }

        public void Validate(ParamDataCollectConfigurationModel parameter) => ValidateParameter(parameter);

        public void Normalize(ParamDataCollectConfigurationModel parameter) => NormalizeParameter(parameter);
    }

    [Fact]
    public void Relative_element_validation_does_not_rewrite_advanced_filter()
    {
        const string filter = "root:child[2]";
        var cases = CreateRelativeElementCases(filter);

        foreach (var testCase in cases)
        {
            testCase.Validate();
            Assert.Equal(filter, testCase.GetFilter());
        }
    }

    [Fact]
    public void Relative_element_normalization_rewrites_advanced_filter()
    {
        const string expected = "[{\"targetType\":\"child\",\"index\":\"2\"}]";
        var cases = CreateRelativeElementCases("root:child[2]");

        foreach (var testCase in cases)
        {
            testCase.Normalize();
            Assert.Equal(expected, testCase.GetFilter());
        }
    }

    private static RelativeElementCase[] CreateRelativeElementCases(string filter)
    {
        var parent = new ParamGetParentModel { Selector = "selector", UseAdvanced = 1, Filter = filter };
        var siblings = new ParamGetSiblingsElementsModel { Selector = "selector", UseAdvanced = 1, Filter = filter };
        var children = new ParamGetSubElementsModel { Selector = "selector", UseAdvanced = 1, Filter = filter };
        var similar = new ParamIsSimilarElementExistModel { Selector = "selector", UseAdvanced = 1, Filter = filter };
        var parentAbility = new TestGetParent();
        var siblingsAbility = new TestGetSiblingsElements();
        var childrenAbility = new TestGetSubElements();
        var similarAbility = new TestIsSimilarElementExist();
        return new[]
        {
            new RelativeElementCase(() => parentAbility.Validate(parent), () => parentAbility.Normalize(parent), () => parent.Filter),
            new RelativeElementCase(() => siblingsAbility.Validate(siblings), () => siblingsAbility.Normalize(siblings), () => siblings.Filter),
            new RelativeElementCase(() => childrenAbility.Validate(children), () => childrenAbility.Normalize(children), () => children.Filter),
            new RelativeElementCase(() => similarAbility.Validate(similar), () => similarAbility.Normalize(similar), () => similar.Filter)
        };
    }

    private sealed record RelativeElementCase(Action Validate, Action Normalize, Func<string> GetFilter);

    private sealed class TestGetParent : GetParent
    {
        public void Validate(ParamGetParentModel parameter) => ValidateParameter(parameter);
        public void Normalize(ParamGetParentModel parameter) => NormalizeParameter(parameter);
    }

    private sealed class TestGetSiblingsElements : GetSiblingsElements
    {
        public void Validate(ParamGetSiblingsElementsModel parameter) => ValidateParameter(parameter);
        public void Normalize(ParamGetSiblingsElementsModel parameter) => NormalizeParameter(parameter);
    }

    private sealed class TestGetSubElements : GetSubElements
    {
        public void Validate(ParamGetSubElementsModel parameter) => ValidateParameter(parameter);
        public void Normalize(ParamGetSubElementsModel parameter) => NormalizeParameter(parameter);
    }

    private sealed class TestIsSimilarElementExist : IsSimilarElementExist
    {
        public TestIsSimilarElementExist()
            : base(new global::II.RPA.Plugin.GUI.GuiHighlightService(new InlineStaDispatcher()))
        {
        }

        public void Validate(ParamIsSimilarElementExistModel parameter) => ValidateParameter(parameter);
        public void Normalize(ParamIsSimilarElementExistModel parameter) => NormalizeParameter(parameter);
    }

    [Fact]
    public void Screenshot_capture_validation_does_not_generate_defaults()
    {
        var ability = new TestScreenshotCapture();
        var parameter = new ParamScreenshotCaptureModel();

        ability.Validate(parameter);

        Assert.Null(parameter.ActionCmd);
        Assert.Null(parameter.FilePath);
    }

    [Fact]
    public void Screenshot_capture_normalization_generates_legacy_defaults()
    {
        var ability = new TestScreenshotCapture();
        var parameter = new ParamScreenshotCaptureModel();

        ability.Normalize(parameter);

        Assert.Equal("stop", parameter.ActionCmd);
        Assert.Equal("res", Path.GetDirectoryName(parameter.FilePath));
        Assert.Equal(".png", Path.GetExtension(parameter.FilePath));
    }

    private sealed class TestScreenshotCapture : global::II.RPA.Plugin.GUI.Plugins.DataCollection.ScreenshotCapture
    {
        public void Validate(ParamScreenshotCaptureModel parameter) => ValidateParameter(parameter);
        public void Normalize(ParamScreenshotCaptureModel parameter) => NormalizeParameter(parameter);
    }

    [Fact]
    public async Task Highlight_script_element_invokes_same_package_abilities_through_invoker()
    {
        var invoker = new RecordingPluginInvoker(
            new JMessage { Info = "resolved-selector" }.ToJson(),
            new JMessage { Code = 0, Info = "highlighted", Message = "ok" }.ToJson());
        var ability = new HighlightScriptElement(invoker);
        var context = new PluginExecutionContext(
            new PluginRoute("Element", "HighlightScriptElement"),
            "{\"webScript\":\"script\"}",
            "invocation-123");

        var response = JsonConvert.DeserializeObject<JMessage>(await ability.RunAsync(context))!;

        Assert.Equal(new[] { "Element.GetElement", "Element.IsSimilarElementExist" }, invoker.Routes);
        Assert.Contains("resolved-selector", invoker.JsonParameters[1]);
        Assert.Contains("highlighted", response.Info);
    }

    private sealed class RecordingPluginInvoker : IPluginInvoker
    {
        private readonly Queue<string> _responses;
        public RecordingPluginInvoker(params string[] responses) => _responses = new Queue<string>(responses);
        public List<string> Routes { get; } = new();
        public List<string> JsonParameters { get; } = new();
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam)
        {
            Routes.Add(targetRoute.ToString());
            JsonParameters.Add(jsonParam);
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class InlineStaDispatcher : IStaDispatcher
    {
        public Task InvokeAsync(Action operation)
        {
            operation();
            return Task.CompletedTask;
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> operation) =>
            Task.FromResult(operation());

        public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> operation) => operation();
    }

    [Fact]
    public async Task Data_collect_preserves_web_browser_activation_failure_from_invoker()
    {
        var failure = new JMessage
        {
            Code = 24680,
            UnifyCode = "WEB-24680",
            Message = "activation failed",
            Info = "failure-info",
            Extend = "{\"source\":\"browser\"}"
        };
        var invoker = new RecordingPluginInvoker(failure.ToJson());
        var ability = new global::II.RPA.Plugin.GUI.Plugins.DataCollect(invoker);
        var context = new PluginExecutionContext(
            new PluginRoute("Element", "DataCollect"),
            "{\"collectType\":\"web\",\"webData\":\"{\\\"type\\\":\\\"chrome\\\",\\\"chrome\\\":\\\"tab\\\"}\"}",
            "invocation-data-collect");

        var result = JsonConvert.DeserializeObject<JMessage>(await ability.RunAsync(context))!;

        Assert.Equal(new[] { "WebBrowser.Active" }, invoker.Routes);
        Assert.Equal(failure.Code, result.Code);
        Assert.Equal(failure.UnifyCode, result.UnifyCode);
        Assert.Equal(failure.Message, result.Message);
        Assert.Equal(failure.Info, result.Info);
        Assert.Equal(failure.Extend, result.Extend);
    }

    [Fact]
    public async Task Check_elements_exist_resolves_application_path_through_invoker()
    {
        var response = new JMessage
        {
            Info = "{\"InstallLocation\":\"Z:\\\\missing\\\\WeChat.exe\"}"
        };
        var invoker = new RecordingPluginInvoker(response.ToJson());
        var ability = new global::II.RPA.Plugin.GUI.Plugins.Chatrpa.CheckElementsExist(invoker);
        var context = new PluginExecutionContext(
            new PluginRoute("Element", "CheckElementsExist"),
            "{}",
            "invocation-check-elements");

        var version = await ability.GetAppVersionByProcessNameAsync("WeChat", context);

        Assert.Equal(string.Empty, version);
        Assert.Equal(new[] { "RPASystem.GetApplicationPath" }, invoker.Routes);
        Assert.Contains("WeChat", invoker.JsonParameters.Single());
    }

    [Fact]
    public async Task Set_text_sets_requested_input_method_through_invoker_before_element_lookup()
    {
        var invoker = new RecordingPluginInvoker(new JMessage().ToJson());
        var ability = new TestSetText(invoker);
        var context = new PluginExecutionContext(
            new PluginRoute("Element", "SetText"),
            "{}",
            "invocation-set-text");

        await ability.ExecuteForTestAsync(
            new ParamSetTextModel
            {
                Selector = "missing-selector",
                txt = "value",
                inputState = "en_us"
            },
            context);

        Assert.Equal(new[] { "DesktopInput.SetState" }, invoker.Routes);
        Assert.Contains("en_us", invoker.JsonParameters.Single());
    }

    private sealed class TestSetText : SetText
    {
        public TestSetText(IPluginInvoker pluginInvoker) : base(pluginInvoker)
        {
        }

        public Task<JMessage> ExecuteForTestAsync(ParamSetTextModel parameter, PluginExecutionContext context) =>
            ExecuteAsync(parameter, context);
    }

    [Fact]
    public async Task Data_collect_v2_page_click_invokes_mouse_click_with_current_context()
    {
        var invoker = new RecordingPluginInvoker(new JMessage().ToJson());
        var context = new PluginExecutionContext(
            new PluginRoute("Element", "DataCollect_v2"),
            "{}",
            "invocation-data-collect-v2");
        var turningPage = new II.RPA.Plugin.GUI.Commons.Params.DataCollect.TurningPageConfig
        {
            Selector = "selector",
            SimulateClick = 1
        };
        var managerType = typeof(PluginPackage).Assembly.GetType(
            "II.RPA.Plugin.GUI.DataCollection.V2.DataCollectingManagerV2",
            throwOnError: true)!;
        var method = managerType.GetMethod(
            "ElementMouseClickAsync",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var clickState = await (Task<bool>)method!.Invoke(
            null,
            new object[] { turningPage, invoker, context })!;

        Assert.True(clickState);
        Assert.Equal(new[] { "Element.MouseClick" }, invoker.Routes);
        Assert.Contains("selector", invoker.JsonParameters.Single());
    }

    [Fact]
    public async Task Data_collect_v1_page_click_invokes_mouse_click_with_current_context()
    {
        var invoker = new RecordingPluginInvoker(new JMessage().ToJson());
        var context = new PluginExecutionContext(
            new PluginRoute("Element", "DataCollect"),
            "{}",
            "invocation-data-collect-v1");
        var managerType = typeof(PluginPackage).Assembly.GetType(
            "II.RPA.Plugin.GUI.DataCollectingManager",
            throwOnError: true)!;
        var method = managerType.GetMethod(
            "ElementMouseClickAsync",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var clicked = await (Task<bool>)method!.Invoke(
            null,
            new object[]
            {
                new List<II.RPA.Plugin.Core.Commons.SelectorProps>(),
                "windows",
                "1.5",
                invoker,
                context
            })!;

        Assert.True(clicked);
        Assert.Equal(new[] { "Element.MouseClick" }, invoker.Routes);
    }

    [Fact]
    public void Data_collect_v1_exposes_async_runtime_collection_without_changing_preview_entry()
    {
        var managerType = typeof(PluginPackage).Assembly.GetType(
            "II.RPA.Plugin.GUI.DataCollectingManager",
            throwOnError: true)!;

        Assert.NotNull(managerType.GetMethod(
            "CollectDataBySelectorAsync",
            BindingFlags.Static | BindingFlags.NonPublic));
        Assert.True(typeof(AsyncPluginAbilityBase<ParamDataCollectModel>)
            .IsAssignableFrom(typeof(global::II.RPA.Plugin.GUI.Plugins.DataCollect)));
    }

    [Fact]
    public void Process_creator_explicitly_accepts_invoker_for_popup_session_calls()
    {
        Assert.NotNull(typeof(global::II.RPA.Plugin.GUI.Plugins.ProcessCreator)
            .GetConstructor(new[] { typeof(IPluginInvoker) }));

        var popupType = typeof(PluginPackage).Assembly.GetType(
            "II.RPA.Plugin.GUI.ProcessCreator.ActionProvider.ActionPopupViewModel",
            throwOnError: true)!;
        Assert.NotNull(popupType.GetMethod(
            "BindPluginInvocation",
            BindingFlags.Instance | BindingFlags.NonPublic));
    }
}
