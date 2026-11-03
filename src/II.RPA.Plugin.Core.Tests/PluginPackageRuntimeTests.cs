using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace II.RPA.Plugin.Core.Tests;

public sealed class PluginPackageRuntimeTests
{
    private static PluginExecutionContext Context(string ability = "Echo", string json = "{}") =>
        new(new PluginRoute("Test", ability), json, Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Package_InitializesAndRunsRegisteredAbility()
    {
        var package = new TestPackage(builder => builder.AddAbility<EchoAbility>("Echo", AbilityLifetime.Transient, AbilityConcurrencyMode.Parallel));

        await package.InitializeAsync(new NullInvoker());
        var response = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context()))!;

        Assert.Equal(0, response.Code);
        Assert.Equal("echo", response.Info);
    }

    [Fact]
    public async Task Package_RejectsRunBeforeInitialization()
    {
        var package = new TestPackage(builder => builder.AddAbility<EchoAbility>("Echo", AbilityLifetime.Transient, AbilityConcurrencyMode.Parallel));

        var response = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context()))!;

        Assert.NotEqual(0, response.Code);
        Assert.Equal(eErrorMessageType.Error_PluginRun_ServerStatusUninit, package.LastErrorType);
    }

    [Fact]
    public async Task Same_type_can_be_registered_for_multiple_routes_and_preserves_requested_route()
    {
        var package = new TestPackage(builder => builder
            .AddAbility<RouteEchoAbility>("Echo", AbilityLifetime.Transient, AbilityConcurrencyMode.Parallel)
            .AddAbility<RouteEchoAbility>("Alias", AbilityLifetime.Transient, AbilityConcurrencyMode.Parallel));

        await package.InitializeAsync(new NullInvoker());
        var response = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context("alias")))!;

        Assert.Equal("Test.alias", response.Info);
    }

    [Fact]
    public async Task RoutedAbilities_use_full_route_and_keep_route_identity()
    {
        var package = new MultiRoutePackage(builder => builder
            .AddAbility<MouseRouteAbility>("Mouse", "SameName", AbilityLifetime.Transient, AbilityConcurrencyMode.Parallel)
            .AddAbility<InputMethodRouteAbility>("InputMethod", "SameName", AbilityLifetime.Transient, AbilityConcurrencyMode.Parallel));

        await package.InitializeAsync(new NullInvoker());

        var mouse = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(RouteContext("Mouse", "SameName")))!;
        var inputMethod = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(RouteContext("InputMethod", "SameName")))!;

        Assert.Equal("Mouse.SameName", mouse.Info);
        Assert.Equal("InputMethod.SameName", inputMethod.Info);
    }

    [Fact]
    public async Task Same_type_across_domains_uses_the_same_singleton_registration_and_preserves_requested_route()
    {
        var package = new MultiRoutePackage(builder => builder
            .AddAbility<RouteCountingAbility>("Mouse", "SameName", AbilityLifetime.RuntimeSingleton, AbilityConcurrencyMode.RejectWhenBusy)
            .AddAbility<RouteCountingAbility>("InputMethod", "SameName", AbilityLifetime.RuntimeSingleton, AbilityConcurrencyMode.RejectWhenBusy));

        await package.InitializeAsync(new NullInvoker());

        Assert.Equal("Mouse.SameName:1", JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(RouteContext("Mouse", "SameName")))!.Info);
        Assert.Equal("InputMethod.SameName:2", JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(RouteContext("InputMethod", "SameName")))!.Info);
    }

    [Fact]
    public void RoutedAbility_rejects_undeclared_domain()
    {
        var undeclared = new MultiRoutePackage(builder => builder
            .AddAbility<MouseRouteAbility>("Other", "Move", AbilityLifetime.Transient, AbilityConcurrencyMode.Parallel));
        Assert.Throws<ArgumentException>(() => undeclared.InitializeAsync(new NullInvoker()).GetAwaiter().GetResult());
    }

    [Fact]
    public void Same_ability_type_rejects_conflicting_registration_policies()
    {
        var package = new MultiRoutePackage(builder => builder
            .AddAbility<CountingAbility>("Mouse", "First", AbilityLifetime.RuntimeSingleton, AbilityConcurrencyMode.RejectWhenBusy)
            .AddAbility<CountingAbility>("InputMethod", "Second", AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy));

        Assert.Throws<ArgumentException>(() => package.InitializeAsync(new NullInvoker()).GetAwaiter().GetResult());
    }

    [Fact]
    public async Task Transient_CreatesNewAbilityPerInvocation()
    {
        var package = new TestPackage(builder => builder.AddAbility<CountingAbility>("Echo", AbilityLifetime.Transient, AbilityConcurrencyMode.Parallel));
        await package.InitializeAsync(new NullInvoker());

        var first = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context()))!;
        var second = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context()))!;

        Assert.Equal("1", first.Info);
        Assert.Equal("1", second.Info);
    }

    [Fact]
    public async Task RuntimeSingleton_ReusesAbilityInstance()
    {
        var package = new TestPackage(builder => builder.AddAbility<CountingAbility>("Echo", AbilityLifetime.RuntimeSingleton, AbilityConcurrencyMode.RejectWhenBusy));
        await package.InitializeAsync(new NullInvoker());

        var first = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context()))!;
        var second = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context()))!;

        Assert.Equal("1", first.Info);
        Assert.Equal("2", second.Info);
    }

    [Fact]
    public async Task Eager_ResolvesAbilityDuringInitialization()
    {
        ConstructCountingAbility.ConstructionCount = 0;
        var package = new TestPackage(builder => builder.AddAbility<ConstructCountingAbility>("Echo", AbilityLifetime.RuntimeSingleton, AbilityConcurrencyMode.RejectWhenBusy, AbilityActivationMode.Eager));

        await package.InitializeAsync(new NullInvoker());

        Assert.Equal(1, ConstructCountingAbility.ConstructionCount);
    }

    [Fact]
    public async Task PackageInitializationHook_runs_after_eager_activation_and_receives_invoker()
    {
        var events = new List<string>();
        ConstructCountingAbility.ConstructionCount = 0;
        ConstructCountingAbility.OnConstruct = () => events.Add("Eager");
        var invoker = new NullInvoker();
        var package = new TestPackage(
            builder => builder.AddAbility<ConstructCountingAbility>("Echo", AbilityLifetime.RuntimeSingleton, AbilityConcurrencyMode.RejectWhenBusy, AbilityActivationMode.Eager),
            value => { Assert.Same(invoker, value); events.Add("Package"); return Task.CompletedTask; });

        try
        {
            await package.InitializeAsync(invoker);
            Assert.Equal(new[] { "Eager", "Package" }, events);
        }
        finally
        {
            ConstructCountingAbility.OnConstruct = null;
        }
    }

    [Fact]
    public async Task PackageInitializationHook_failure_propagates_and_package_stays_uninitialized()
    {
        var package = new TestPackage(
            _ => { },
            _ => Task.FromException(new InvalidOperationException("failed")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => package.InitializeAsync(new NullInvoker()));
        var response = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context()))!;

        Assert.NotEqual(0, response.Code);
    }

    [Fact]
    public async Task PackageInitializationHook_can_resolve_private_package_services_without_exposing_container()
    {
        var dependency = new object();
        var package = new ResolvingPackage(dependency);

        await package.InitializeAsync(new NullInvoker());

        Assert.Same(dependency, package.ResolvedDependency);
        Assert.DoesNotContain(typeof(PluginPackageBase).GetMethods(), method => method.IsPublic && method.Name.Contains("Service", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisteredAbility_can_receive_package_invoker_through_constructor_injection()
    {
        var invoker = new NullInvoker();
        var package = new TestPackage(builder => builder.AddAbility<InvokerAbility>("Echo", AbilityLifetime.Transient, AbilityConcurrencyMode.Parallel));

        await package.InitializeAsync(invoker);
        var response = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context()))!;

        Assert.Same(invoker, InvokerAbility.LastInvoker);
        Assert.Equal(0, response.Code);
    }

    [Fact]
    public async Task RejectWhenBusy_ReturnsImmediatelyAndReleasesAfterFailure()
    {
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var package = new TestPackage(builder => builder.AddAbility<BlockingAbility>("Echo", AbilityLifetime.Transient, AbilityConcurrencyMode.RejectWhenBusy));
        BlockingAbility.Gate = gate.Task;
        await package.InitializeAsync(new NullInvoker());

        var first = Task.Run(() => package.RunAsync(Context()));
        await BlockingAbility.Started.Task;
        var rejected = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context()))!;
        gate.SetResult(true);
        await first;
        var afterRelease = JsonConvert.DeserializeObject<JMessage>(await package.RunAsync(Context()))!;

        Assert.Equal(400000966, rejected.Code);
        Assert.Equal("010400100000966", rejected.UnifyCode);
        Assert.Contains("正在运行中的功能", rejected.Message);
        Assert.Equal(0, afterRelease.Code);
    }

    [Fact]
    public void InvalidRegistrationCombinationsAreRejected()
    {
        var builder = new PluginPackageBuilder("Test", new[] { "Test" });

        Assert.Throws<ArgumentException>(() => builder.AddAbility<EchoAbility>("Echo", AbilityLifetime.RuntimeSingleton, AbilityConcurrencyMode.Parallel));
        Assert.Throws<ArgumentException>(() => builder.AddAbility<EchoAbility>("Echo", AbilityLifetime.Transient, AbilityConcurrencyMode.Parallel, AbilityActivationMode.Eager));
    }

    [Fact]
    public void ContainerIsNotExposedByPackagePublicSurface()
    {
        Assert.DoesNotContain(typeof(PluginPackageBase).GetProperties(), property => typeof(IServiceProvider).IsAssignableFrom(property.PropertyType));
        Assert.DoesNotContain(typeof(PluginPackageBase).GetMethods(), method => method.Name.Contains("Service", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class NullInvoker : IPluginInvoker
    {
        public Task<string> InvokeAsync(PluginRoute targetRoute, string jsonParam) => Task.FromResult("{}");
    }

    private sealed class TestPackage : PluginPackageBase
    {
        private readonly Action<PluginPackageBuilder> _configure;
        private readonly Func<IPluginInvoker, Task>? _initialize;
        public eErrorMessageType? LastErrorType { get; private set; }
        public TestPackage(Action<PluginPackageBuilder> configure, Func<IPluginInvoker, Task>? initialize = null) { _configure = configure; _initialize = initialize; }
        public override string PackageName => "Test";
        protected override void Configure(PluginPackageBuilder builder) => _configure(builder);
        protected override Task InitializePackageAsync(IPluginInvoker pluginInvoker) => _initialize?.Invoke(pluginInvoker) ?? Task.CompletedTask;
        protected override void OnError(eErrorMessageType errorType) => LastErrorType = errorType;
    }

    private sealed class MultiRoutePackage : PluginPackageBase
    {
        private readonly Action<PluginPackageBuilder> _configure;
        public MultiRoutePackage(Action<PluginPackageBuilder> configure) => _configure = configure;
        public override string PackageName => "DesktopInput";
        public override IReadOnlyCollection<string> RoutePackageNames => new[] { "Mouse", "InputMethod" };
        protected override void Configure(PluginPackageBuilder builder) => _configure(builder);
    }

    private sealed class ResolvingPackage : PluginPackageBase
    {
        private readonly object _dependency;
        public ResolvingPackage(object dependency) => _dependency = dependency;
        public override string PackageName => "Test";
        public object? ResolvedDependency { get; private set; }
        protected override void Configure(PluginPackageBuilder builder) => builder.Services.AddSingleton(_dependency);
        protected override Task InitializePackageAsync(IPluginInvoker pluginInvoker)
        {
            ResolvedDependency = GetRequiredPackageService<object>();
            return Task.CompletedTask;
        }
    }

    private sealed class EchoAbility : PluginAbilityBase<TestParameter>
    {
        protected override eComponentType ComponentType => eComponentType.None;
        protected override void ValidateParameter(TestParameter parameter) { }
        protected override JMessage Execute(TestParameter parameter, PluginExecutionContext context) => new() { Info = "echo" };
    }

    private class RouteEchoAbility : PluginAbilityBase<TestParameter>
    {
        protected override eComponentType ComponentType => eComponentType.None;
        protected override void ValidateParameter(TestParameter parameter) { }
        protected override JMessage Execute(TestParameter parameter, PluginExecutionContext context) => new() { Info = context.Route.ToString() };
    }

    private sealed class MouseRouteAbility : RouteEchoAbility
    {
    }

    private sealed class InputMethodRouteAbility : RouteEchoAbility
    {
    }

    private sealed class CountingAbility : PluginAbilityBase<TestParameter>
    {
        private int _count;
        protected override eComponentType ComponentType => eComponentType.None;
        protected override void ValidateParameter(TestParameter parameter) { }
        protected override JMessage Execute(TestParameter parameter, PluginExecutionContext context) => new() { Info = (++_count).ToString() };
    }

    private sealed class RouteCountingAbility : PluginAbilityBase<TestParameter>
    {
        private int _count;
        protected override eComponentType ComponentType => eComponentType.None;
        protected override void ValidateParameter(TestParameter parameter) { }
        protected override JMessage Execute(TestParameter parameter, PluginExecutionContext context) => new() { Info = $"{context.Route}:{++_count}" };
    }

    private sealed class ConstructCountingAbility : PluginAbilityBase<TestParameter>
    {
        public static int ConstructionCount;
        public static Action? OnConstruct;
        public ConstructCountingAbility() { Interlocked.Increment(ref ConstructionCount); OnConstruct?.Invoke(); }
        protected override eComponentType ComponentType => eComponentType.None;
        protected override void ValidateParameter(TestParameter parameter) { }
        protected override JMessage Execute(TestParameter parameter, PluginExecutionContext context) => new();
    }

    private sealed class BlockingAbility : PluginAbilityBase<TestParameter>
    {
        public static Task Gate = Task.CompletedTask;
        public static TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override eComponentType ComponentType => eComponentType.None;
        protected override void ValidateParameter(TestParameter parameter) { }
        protected override JMessage Execute(TestParameter parameter, PluginExecutionContext context)
        {
            Started.TrySetResult(true);
            Gate.GetAwaiter().GetResult();
            return new();
        }
    }

    private sealed class InvokerAbility : PluginAbilityBase<TestParameter>
    {
        public static IPluginInvoker? LastInvoker { get; private set; }

        public InvokerAbility(IPluginInvoker invoker) => LastInvoker = invoker;

        protected override eComponentType ComponentType => eComponentType.None;
        protected override void ValidateParameter(TestParameter parameter) { }
        protected override JMessage Execute(TestParameter parameter, PluginExecutionContext context) => new();
    }

    private sealed class TestParameter : PluginParameterBase
    {
    }


    private static PluginExecutionContext RouteContext(string packageName, string abilityName) =>
        new(new PluginRoute(packageName, abilityName), "{}", Guid.NewGuid().ToString("N"));
}
