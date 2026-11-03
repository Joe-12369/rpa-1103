using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Xunit;

namespace II.RPA.Plugin.Core.Tests;

public sealed class PluginAbilityPipelineTests
{
    private static readonly PluginExecutionContext Context = new(
        new PluginRoute("Test", "Record"),
        "{\"componentLabel\":\"label-1\",\"value\":\"input\",\"runParam\":{\"attempt\":2}}",
        "invocation-1");

    [Fact]
    public void PluginParameterBase_ContainsStableCommonParameters()
    {
        var parameter = new TestParameter
        {
            ComponentLabel = "label-1",
            RunParam = new RunParamModel { CurrentAttempt = 2 }
        };

        Assert.Equal("label-1", parameter.ComponentLabel);
        Assert.Equal(2, parameter.RunParam.CurrentAttempt);
    }

    [Fact]
    public async Task RunAsync_UsesFixedPipelineOrderAndCopiesComponentLabel()
    {
        var ability = new RecordingSyncAbility();
        var result = Deserialize(await ability.RunAsync(Context));

        Assert.Equal(0, result.Code);
        Assert.Equal("Deserialize,Validate,Normalize,Prepare,Execute,Handle", string.Join(',', ability.Stages));
        Assert.Equal("label-1", ability.LabelObservedDuringExecute);
        Assert.Equal(2, ability.AttemptObservedDuringExecute);
        Assert.Equal("normalized", result.Info);
    }

    [Fact]
    public async Task RunAsync_ClearsComponentLabelAfterEveryInvocation()
    {
        var ability = new RecordingSyncAbility();
        await ability.RunAsync(Context);
        Assert.Null(ability.CurrentComponentLabel);
    }

    [Fact]
    public async Task RunAsync_DoesNotReuseComponentLabelAcrossInvocations()
    {
        var ability = new RecordingSyncAbility();
        await ability.RunAsync(Context);
        ability.Failure = PipelineFailure.Execute;
        var secondContext = new PluginExecutionContext(
            Context.Route,
            "{\"value\":\"second\"}",
            Context.InvocationId);

        await ability.RunAsync(secondContext);

        Assert.Equal(string.Empty, ability.LabelObservedDuringError);
        Assert.Null(ability.CurrentComponentLabel);
    }

    [Theory]
    [InlineData(PipelineFailure.Validate, "Deserialize,Validate,Handle")]
    [InlineData(PipelineFailure.Normalize, "Deserialize,Validate,Normalize,Handle")]
    [InlineData(PipelineFailure.Prepare, "Deserialize,Validate,Normalize,Prepare,Handle")]
    public async Task RunAsync_StopsPipelineAfterFailure(PipelineFailure failure, string expectedStages)
    {
        var ability = new RecordingSyncAbility(failure);
        var result = Deserialize(await ability.RunAsync(Context));

        Assert.NotEqual(0, result.Code);
        Assert.Equal(expectedStages, string.Join(',', ability.Stages));
        Assert.Null(ability.CurrentComponentLabel);
    }

    [Fact]
    public async Task RunAsync_MapsDeserializeFailureToParameterError()
    {
        var ability = new RecordingSyncAbility();
        var context = new PluginExecutionContext(Context.Route, "{invalid-json", Context.InvocationId);
        var result = Deserialize(await ability.RunAsync(context));

        Assert.NotEqual(0, result.Code);
        Assert.Equal(eErrorMessageType.Error_ParamsError, ability.LastErrorType);
        Assert.Equal("Deserialize,Handle", string.Join(',', ability.Stages));
    }

    [Fact]
    public async Task RunAsync_PreservesRpaExceptionWhenConvertingError()
    {
        var expected = new RPAException(eErrorMessageType.Error_NotSupport, eComponentType.None);
        var ability = new RecordingSyncAbility(PipelineFailure.Execute, expected);
        var result = Deserialize(await ability.RunAsync(Context));

        Assert.NotEqual(0, result.Code);
        Assert.Same(expected, ability.LastException);
        var actual = Assert.IsType<RPAException>(ability.LastException);
        Assert.Equal(eErrorMessageType.Error_NotSupport, actual.ErrorMessageType);
    }

    [Fact]
    public async Task RunAsync_MapsOrdinaryExceptionToUnknownError()
    {
        var ability = new RecordingSyncAbility(PipelineFailure.Execute, new InvalidOperationException("boom"));
        var result = Deserialize(await ability.RunAsync(Context));

        Assert.NotEqual(0, result.Code);
        Assert.Equal(eErrorMessageType.Error_UNKOWN, ability.LastErrorType);
    }

    [Fact]
    public async Task RunAsync_ReturnsPreparationFailureWithoutExecutingOrRebuildingTheMessage()
    {
        var failure = new JMessage
        {
            Code = 123456,
            UnifyCode = "U-123456",
            Message = "prepare failed",
            Info = "original info",
            Extend = "{\"source\":\"prepare\"}",
            AllowRetry = "0"
        };
        var ability = new RecordingSyncAbility { PreparationResult = failure };

        var result = Deserialize(await ability.RunAsync(Context));

        Assert.Equal("Deserialize,Validate,Normalize,Prepare,Handle", string.Join(',', ability.Stages));
        Assert.Equal(failure.Code, result.Code);
        Assert.Equal(failure.UnifyCode, result.UnifyCode);
        Assert.Equal(failure.Message, result.Message);
        Assert.Equal(failure.Info, result.Info);
        Assert.Equal(failure.Extend, result.Extend);
        Assert.Equal(failure.AllowRetry, result.AllowRetry);
    }

    [Fact]
    public async Task AsyncAbility_AwaitsExecuteAsyncThroughSameContract()
    {
        IPluginAbility ability = new RecordingAsyncAbility();
        var result = Deserialize(await ability.RunAsync(Context));

        Assert.Equal(0, result.Code);
        Assert.Equal("async", result.Info);
    }

    private static JMessage Deserialize(string json) => JsonConvert.DeserializeObject<JMessage>(json)!;

    public enum PipelineFailure
    {
        None,
        Validate,
        Normalize,
        Prepare,
        Execute
    }

    private sealed class TestParameter : PluginParameterBase
    {
        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class RecordingSyncAbility : PluginAbilityBase<TestParameter>
    {
        private PipelineFailure _failure;
        private readonly Exception? _executionException;

        public RecordingSyncAbility(PipelineFailure failure = PipelineFailure.None, Exception? executionException = null)
        {
            _failure = failure;
            _executionException = executionException;
        }

        public List<string> Stages { get; } = new();
        public string? LabelObservedDuringExecute { get; private set; }
        public int AttemptObservedDuringExecute { get; private set; }
        public string? CurrentComponentLabel => ComponentLabel;
        public Exception? LastException { get; private set; }
        public eErrorMessageType? LastErrorType { get; private set; }
        public string? LabelObservedDuringError { get; private set; }
        public PipelineFailure Failure { set => _failure = value; }
        public JMessage? PreparationResult { get; set; }

        protected override eComponentType ComponentType => eComponentType.None;

        protected override TestParameter DeserializeParameter(string jsonParam)
        {
            Stages.Add("Deserialize");
            return base.DeserializeParameter(jsonParam);
        }

        protected override void ValidateParameter(TestParameter parameter)
        {
            Stages.Add("Validate");
            ThrowIf(PipelineFailure.Validate);
        }

        protected override void NormalizeParameter(TestParameter parameter)
        {
            Stages.Add("Normalize");
            ThrowIf(PipelineFailure.Normalize);
            parameter.Value = "normalized";
        }

        protected override Task<JMessage?> PrepareExecutionAsync(TestParameter parameter, PluginExecutionContext context)
        {
            Stages.Add("Prepare");
            ThrowIf(PipelineFailure.Prepare);
            return Task.FromResult(PreparationResult);
        }

        protected override JMessage Execute(TestParameter parameter, PluginExecutionContext context)
        {
            Stages.Add("Execute");
            ThrowIf(PipelineFailure.Execute);
            LabelObservedDuringExecute = ComponentLabel;
            AttemptObservedDuringExecute = parameter.RunParam.CurrentAttempt;
            return new JMessage { Info = parameter.Value };
        }

        protected override string HandlingResultMessage(TestParameter? parameter, JMessage resultMsg)
        {
            Stages.Add("Handle");
            return base.HandlingResultMessage(parameter, resultMsg);
        }

        protected override void Exception(Exception? exception, eGuiType guiType, eErrorMessageType errorMessageType, bool simpleMessage, ref JMessage message)
        {
            LastException = exception;
            LastErrorType = errorMessageType;
            LabelObservedDuringError = ComponentLabel;
            base.Exception(exception, guiType, errorMessageType, simpleMessage, ref message);
        }

        private void ThrowIf(PipelineFailure stage)
        {
            if (_failure == stage)
            {
                throw _executionException ?? new InvalidOperationException(stage.ToString());
            }
        }
    }

    private sealed class RecordingAsyncAbility : AsyncPluginAbilityBase<TestParameter>
    {
        protected override eComponentType ComponentType => eComponentType.None;

        protected override void ValidateParameter(TestParameter parameter)
        {
        }

        protected override async Task<JMessage> ExecuteAsync(TestParameter parameter, PluginExecutionContext context)
        {
            await Task.Yield();
            return new JMessage { Info = "async" };
        }
    }
}
