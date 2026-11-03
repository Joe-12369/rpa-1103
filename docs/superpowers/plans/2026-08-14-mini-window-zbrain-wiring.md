# 小窗运行缺失链路补全（子会话 ZBrain 接线）实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在新 Talker 架构上补齐"小窗模式下任务跑在子会话 ZBrain 上"执行链的 4 项缺失（子代理启停、插件包继承、消息路由、ExitCode=5 自动管理员重启）。

**Architecture:** 在现有接点恢复参考实现语义：子会话 z_brain 注册（`TalkerManager.ConfigureTalkerFromWorker`）触发 6442 子代理启动与插件包继承；`HostTalker` 的 zbrain 请求分支按 `IsMiniModeActive` 选择子/主会话 ZBrainTalker；`ZDesktopProcess` 在 ExitCode=5 时复用 `BaseProcess` 自动重启框架。新增 `IZBrainChildProxyLauncher` 接口（Abstractions 程序集）解耦 Provider 与 Talker 的项目依赖（Provider → Core → Talker 单向，Talker 不能反向引用 Provider）。

**Tech Stack:** C# / .NET 10（windows）、Unity 容器（container.config）、xUnit（`II.RPA.Core.Talker.Tests`，已配 `InternalsVisibleTo`）、gRPC Frame 通信（`II.RPA.Core.Contracts`）。

**设计文档:** `docs/superpowers/specs/2026-08-14-mini-window-zbrain-wiring-design.md`

---

## 文件结构

| 文件 | 变更 | 职责 |
|---|---|---|
| `src/II.RPA.Core.Abstractions/Runtime/IZBrainChildProxyLauncher.cs` | 新建 | 子会话 ZBrain 代理生命周期接口（Abstractions 默认通配编译项，自动纳入） |
| `src/II.RPA.Core/II.RPA.Core.Provider/ZBrainProxyProvider/ZBrainProxyProvider.cs` | 修改 | 实现接口，委托现有 `StartZdesktopProxy/StopZdesktopProxy` |
| `src/II.RPA.Core/II.RPA.Core.Server/container.config` | 修改 | 注册接口映射（typeAlias + register） |
| `src/II.RPA.Core.Talker/ITalkerManager.cs` | 修改 | 增加 `StartChildZBrainProxy/StopChildZBrainProxy` 与 predicate 版 `WaitTalker<T>` |
| `src/II.RPA.Core.Talker/TalkerManager.cs` | 修改 | 注入 launcher、实现新接口成员、注册触发代理启动、注册完成后继承插件包、`WorkerRegistrationMeta`/`ConfigureTalkerFromWorker`/`InheritMainZbrainPackageAsync` 内部化 |
| `src/II.RPA.Core.Talker/Talkers/ZBrainTalker.cs` | 修改 | `LatestPackagePath` setter → internal；`LoadPlugin`、两个 `ProxyTalk` 重载 → virtual（测试接缝） |
| `src/II.RPA.Core.Talker/Talkers/HostTalker.cs` | 修改 | zbrain 请求分支按 `IsMiniModeActive` 等待并选择对应 ZBrainTalker |
| `src/II.RPA.Core.Talker/Talkers/ZDeskTopTalker.cs` | 修改 | Stopped 分支调用 `StopChildZBrainProxy`；`HandleZdesktopProcessStatusChangedAsync` → internal |
| `src/II.RPA.Core.Talker/Processes/ZDesktopProcess.cs` | 修改 | ExitCode=5 非主动退出时走 base 自动重启；暴露 `internal WithAdministrator` |
| `src/II.RPA.Core.Talker.Tests/KernelRequestDispatcherTests.cs` | 修改 | 既有 `FakeTalkerManager` 补 3 个新接口成员（编译修复） |
| `src/II.RPA.Core.Talker.Tests/TestRuntimeContext.cs` | 新建 | `IRpaRuntimeContext` 测试假实现（轻量 TalkerManager 所需） |
| `src/II.RPA.Core.Talker.Tests/ChildZBrainProxyLauncherTests.cs` | 新建 | ① 的测试：代理启停委托、注册触发、WaitTalker predicate |
| `src/II.RPA.Core.Talker.Tests/ZDeskTopTalkerTests.cs` | 新建 | ① 的测试：小窗退出停止代理 |
| `src/II.RPA.Core.Talker.Tests/PackageInheritanceTests.cs` | 新建 | ② 的测试：插件包继承三条路径 |
| `src/II.RPA.Core.Talker.Tests/ZBrainRoutingTests.cs` | 新建 | ③ 的测试：HostTalker 按小窗模式路由 |
| `src/II.RPA.Core.Talker.Tests/ZDesktopProcessTests.cs` | 新建 | ④ 的测试：ExitCode=5 自动重启 |

---

## Task 0: 基线验证

- [ ] **Step 1: 确认当前测试基线为绿**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q`
Expected: 通过（4 个既有 KernelRequestDispatcherTests 用例）

- [ ] **Step 2: 确认受影响项目可编译**

Run: `dotnet build c:/Works/ii.rpa/src/II.RPA.Core.Talker/II.RPA.Core.Talker.csproj -v q`
Expected: 0 Error

## Task 1: 新增 IZBrainChildProxyLauncher 接口与实现、容器注册

**Files:**
- Create: `src/II.RPA.Core.Abstractions/Runtime/IZBrainChildProxyLauncher.cs`
- Modify: `src/II.RPA.Core/II.RPA.Core.Provider/ZBrainProxyProvider/ZBrainProxyProvider.cs:1-14`
- Modify: `src/II.RPA.Core/II.RPA.Core.Server/container.config`

- [ ] **Step 1: 新建接口文件**

```csharp
namespace II.RPA.Core.Runtime;

/// <summary>
/// 子会话 ZBrain HTTP 代理（ChildStartup，固定 6442 端口）的生命周期入口。
/// 由主 CoreServer 在子会话 z_brain 注册时启动，小窗(ZDesktop)退出时停止。
/// </summary>
public interface IZBrainChildProxyLauncher
{
    void StartChildProxy();

    void StopChildProxy();
}
```

- [ ] **Step 2: ZBrainProxyProvider 实现接口**

在 [ZBrainProxyProvider.cs](src/II.RPA.Core/II.RPA.Core.Provider/ZBrainProxyProvider/ZBrainProxyProvider.cs) 中：
文件顶部 using 区追加 `using II.RPA.Core.Runtime;`；
类声明 `public class ZBrainProxyProvider : IDisposable`（:14）改为 `public class ZBrainProxyProvider : IDisposable, IZBrainChildProxyLauncher`；
在 `StopZdesktopProxy()` 方法（:94-98）之后新增：

```csharp
public void StartChildProxy() => StartZdesktopProxy();

public void StopChildProxy() => StopZdesktopProxy();
```

- [ ] **Step 3: container.config 注册接口映射**

在 [container.config](src/II.RPA.Core/II.RPA.Core.Server/container.config) 中：
typeAlias 区（约 :33，`ZBrainProxyProvider` 别名之后）追加：

```xml
<add alias="IZBrainChildProxyLauncher" type="II.RPA.Core.Runtime.IZBrainChildProxyLauncher,II.RPA.Core.Abstractions" />
```

register 区（约 :128-141 的 `<!-- ZBrainProxyProvider -->` 注册块之后、`EngineProxyProvider` 注册块之前）追加：

```xml
<register type="IZBrainChildProxyLauncher" mapTo="ZBrainProxyProvider">
  <lifetime type="ContainerControlled" />
</register>
```

- [ ] **Step 4: 验证编译与配置格式**

Run: `dotnet build c:/Works/ii.rpa/src/II.RPA.Core/II.RPA.Core.Provider/II.RPA.Core.Provider.csproj -v q`
Expected: 0 Error（Abstractions 经 II.RPA.Core 传递引用，接口可编译）

Run: `powershell -NoProfile -Command "$null = [xml](Get-Content -Raw 'c:/Works/ii.rpa/src/II.RPA.Core/II.RPA.Core.Server/container.config'); 'XML OK'"`
Expected: `XML OK`

- [ ] **Step 5: 提交**

```bash
cd c:/Works/ii.rpa && git add src/II.RPA.Core.Abstractions/Runtime/IZBrainChildProxyLauncher.cs src/II.RPA.Core/II.RPA.Core.Provider/ZBrainProxyProvider/ZBrainProxyProvider.cs src/II.RPA.Core/II.RPA.Core.Server/container.config && git commit -m "feat: 新增IZBrainChildProxyLauncher接口并注册子会话ZBrain代理

Co-Authored-By: Claude <noreply@anthropic.com>"
```

## Task 2: TalkerManager 支持代理启停与按条件等待 Talker

**Files:**
- Modify: `src/II.RPA.Core.Talker/ITalkerManager.cs:63-75`
- Modify: `src/II.RPA.Core.Talker/TalkerManager.cs:28-96, 434-475`
- Modify: `src/II.RPA.Core.Talker.Tests/KernelRequestDispatcherTests.cs:151-235`
- Test: `src/II.RPA.Core.Talker.Tests/ChildZBrainProxyLauncherTests.cs`

- [ ] **Step 1: 写失败测试（接口成员尚不存在，编译失败即预期失败）**

新建 `src/II.RPA.Core.Talker.Tests/ChildZBrainProxyLauncherTests.cs`：

```csharp
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Talkers;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class ChildZBrainProxyLauncherTests
{
    [Fact]
    public async Task WaitTalkerWithPredicateReturnsMatchingTalker()
    {
        var manager = CreateManager();
        var child = new ZBrainTalker(manager, NullLogger.Instance, new TestRuntimeContext()) { IsChildSession = true };
        manager.AddTalker(child);

        var result = await manager
            .WaitTalker<ZBrainTalker>(talker => talker.IsChildSession, CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(child, result);
    }

    [Fact]
    public async Task WaitTalkerWithPredicateWaitsUntilMatchAppears()
    {
        var manager = CreateManager();
        var child = new ZBrainTalker(manager, NullLogger.Instance, new TestRuntimeContext()) { IsChildSession = true };

        var waitTask = manager.WaitTalker<ZBrainTalker>(talker => talker.IsChildSession, CancellationToken.None);
        await Task.Delay(200);
        Assert.False(waitTask.IsCompleted);

        manager.AddTalker(child);
        var result = await waitTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Same(child, result);
    }

    [Fact]
    public async Task WaitTalkerWithPredicateHonorsCancellation()
    {
        var manager = CreateManager();
        using var cts = new CancellationTokenSource();
        var waitTask = manager.WaitTalker<ZBrainTalker>(talker => true, cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
    }

    [Fact]
    public void StartChildZBrainProxyWithoutLauncherDoesNotThrow()
    {
        var manager = CreateManager();

        var exception = Record.Exception(() => manager.StartChildZBrainProxy());

        Assert.Null(exception);
    }

    [Fact]
    public void ChildZBrainProxyStartAndStopDelegateToLauncher()
    {
        var launcher = new FakeChildProxyLauncher();
        var manager = CreateManager(launcher);

        manager.StartChildZBrainProxy();
        manager.StopChildZBrainProxy();

        Assert.Equal(1, launcher.StartCount);
        Assert.Equal(1, launcher.StopCount);
    }

    private static TalkerManager CreateManager(IZBrainChildProxyLauncher? launcher = null)
        => new(NullLogger.Instance, new TestRuntimeContext(), null, launcher);

    private sealed class FakeChildProxyLauncher : IZBrainChildProxyLauncher
    {
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }

        public void StartChildProxy() => StartCount++;

        public void StopChildProxy() => StopCount++;
    }
}
```

同时新建测试辅助 `src/II.RPA.Core.Talker.Tests/TestRuntimeContext.cs`：

```csharp
using II.RPA.Core.Runtime;

namespace II.RPA.Core.Talker.Tests;

/// <summary>
/// IRpaRuntimeContext 的轻量测试实现。
/// ClientKind=Unknown 使 TalkerManager 不创建任何 worker talker；
/// GetConfiguration 恒返回 "false" 以禁用 Mobile/ADB/配置数量覆盖。
/// </summary>
internal sealed class TestRuntimeContext : IRpaRuntimeContext
{
    public RpaClientKind ClientKind { get; set; } = RpaClientKind.Unknown;
    public string ClientId { get; set; } = string.Empty;
    public string ProducedClientId { get; set; } = string.Empty;
    public string RootClientId { get; set; } = string.Empty;
    public int CurrentProcessId { get; set; } = Environment.ProcessId;
    public int SessionId { get; set; } = Environment.ProcessId;
    public int ServerPort { get; set; }
    public int RootServerPort { get; set; }
    public bool LoopAdb { get; set; }
    public bool IsMiniModeActive { get; set; }
    public bool ForceRestartHost { get; set; }
    public string EngineExecutablePath { get; set; } = string.Empty;
    public string PythonEnvironmentVersion { get; set; } = string.Empty;
    public string? UserInfoParameter { get; set; }

    public string BuildUserInfoParameter() => string.Empty;
    public string FixZBrainUriForZDesktop(string parameter) => parameter;
    public void SetSetting(string key, object value) { }
    public string? GetConfiguration(string commandLineName, string configName) => "false";
    public bool IsMobileProject(string packagePath) => false;
}
```

- [ ] **Step 2: 运行测试确认失败（编译失败）**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q`
Expected: 编译错误（`ITalkerManager` 无 `WaitTalker<T>(Predicate<T>,...)`；`TalkerManager` 无 4 参构造与 `StartChildZBrainProxy`）

- [ ] **Step 3: ITalkerManager 增加接口成员**

在 [ITalkerManager.cs:69](src/II.RPA.Core.Talker/ITalkerManager.cs#L69) 的 `WaitTalker<T>(TimeSpan, ...)` 之后追加：

```csharp
Task<T> WaitTalker<T>(Predicate<T> predicate, CancellationToken cancellationToken = default) where T : ITalker;

void StartChildZBrainProxy();

void StopChildZBrainProxy();
```

- [ ] **Step 4: TalkerManager 实现（构造注入 + 新成员）**

[TalkerManager.cs:28-44](src/II.RPA.Core.Talker/TalkerManager.cs#L28-L44) 字段与构造函数改为（新增 `_childZBrainProxyLauncher` 字段，原 2/3 参构造改为链式调用 4 参构造，原构造体移入 4 参构造）：

```csharp
private readonly ILogger _logger;
private readonly IRpaRuntimeContext _runtimeContext;
private readonly IZBrainChildProxyLauncher? _childZBrainProxyLauncher;
private int _initializationStarted;
private int _stopped;

public TalkerManager(ILogger logger, IRpaRuntimeContext runtimeContext)
    : this(logger, runtimeContext, null, null)
{
}

public TalkerManager(
    ILogger logger,
    IRpaRuntimeContext runtimeContext,
    IAdbStartupSignal? startupSignal)
    : this(logger, runtimeContext, startupSignal, null)
{
}

public TalkerManager(
    ILogger logger,
    IRpaRuntimeContext runtimeContext,
    IAdbStartupSignal? startupSignal,
    IZBrainChildProxyLauncher? childZBrainProxyLauncher)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
    _childZBrainProxyLauncher = childZBrainProxyLauncher;
    // ……原 3 参构造体其余内容原样保留……
}
```

（`II.RPA.Core.Runtime` 已在文件顶部引用，无需新增 using）

在 [TalkerManager.cs:475](src/II.RPA.Core.Talker/TalkerManager.cs#L475)（现有 `WaitTalker<T>(TimeSpan, ...)` 方法之后）新增：

```csharp
public async Task<T> WaitTalker<T>(Predicate<T> predicate, CancellationToken cancellationToken = default) where T : ITalker
{
    ArgumentNullException.ThrowIfNull(predicate);
    while (true)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var talker = GetTalkers<T>(readyOnly: false).FirstOrDefault(candidate => predicate(candidate));
        if (talker != null)
        {
            return talker;
        }

        await Task.Delay(50, cancellationToken).ConfigureAwait(false);
    }
}

public void StartChildZBrainProxy()
{
    if (_childZBrainProxyLauncher == null)
    {
        _logger.LogWarning("未注入 IZBrainChildProxyLauncher，跳过子会话ZBrain代理启动");
        return;
    }

    _logger.LogInfo("启动子会话ZBrain代理");
    _childZBrainProxyLauncher.StartChildProxy();
}

public void StopChildZBrainProxy()
{
    if (_childZBrainProxyLauncher == null)
    {
        _logger.LogWarning("未注入 IZBrainChildProxyLauncher，跳过子会话ZBrain代理停止");
        return;
    }

    _logger.LogInfo("停止子会话ZBrain代理");
    _childZBrainProxyLauncher.StopChildProxy();
}
```

- [ ] **Step 5: 修复既有 FakeTalkerManager 编译**

在 [KernelRequestDispatcherTests.cs:215](src/II.RPA.Core.Talker.Tests/KernelRequestDispatcherTests.cs#L215) 的 `WaitTalker<T>(TimeSpan, ...)` 实现之后追加：

```csharp
public Task<T> WaitTalker<T>(Predicate<T> predicate, CancellationToken cancellationToken = default) where T : ITalker => throw new NotSupportedException();
public void StartChildZBrainProxy() { }
public void StopChildZBrainProxy() { }
```

- [ ] **Step 6: 运行测试确认通过**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q`
Expected: 全部通过（既有 4 个 + 新增 5 个）

- [ ] **Step 7: 提交**

```bash
cd c:/Works/ii.rpa && git add src/II.RPA.Core.Talker/ITalkerManager.cs src/II.RPA.Core.Talker/TalkerManager.cs src/II.RPA.Core.Talker.Tests/ && git commit -m "feat: TalkerManager支持子会话ZBrain代理启停与按条件等待Talker

Co-Authored-By: Claude <noreply@anthropic.com>"
```

## Task 3: 子会话 z_brain 注册触发代理启动，小窗退出触发停止

**Files:**
- Modify: `src/II.RPA.Core.Talker/TalkerManager.cs:625-642, 816-823`
- Modify: `src/II.RPA.Core.Talker/Talkers/ZDeskTopTalker.cs:147-202`
- Test: `src/II.RPA.Core.Talker.Tests/ChildZBrainProxyLauncherTests.cs`（追加）、`src/II.RPA.Core.Talker.Tests/ZDeskTopTalkerTests.cs`（新建）

- [ ] **Step 1: 写失败测试**

在 `ChildZBrainProxyLauncherTests.cs` 末尾（类内）追加：

```csharp
[Fact]
public void ConfigureTalkerFromWorkerStartsChildProxyForChildZBrain()
{
    var launcher = new FakeChildProxyLauncher();
    var manager = CreateManager(launcher);
    var child = new ZBrainTalker(manager, NullLogger.Instance, new TestRuntimeContext());
    manager.AddTalker(child);

    manager.ConfigureTalkerFromWorker(child, new WorkerRegistrationMeta(
        Pid: Environment.ProcessId,
        TalkerId: Guid.NewGuid(),
        Role: "z_brain",
        Name: "z_brain",
        Shadow: false,
        Temporary: false,
        IsChild: true));

    Assert.True(child.IsChildSession);
    Assert.Equal(1, launcher.StartCount);
}

[Fact]
public void ConfigureTalkerFromWorkerDoesNotStartProxyForMainZBrain()
{
    var launcher = new FakeChildProxyLauncher();
    var manager = CreateManager(launcher);
    var main = new ZBrainTalker(manager, NullLogger.Instance, new TestRuntimeContext());
    manager.AddTalker(main);

    manager.ConfigureTalkerFromWorker(main, new WorkerRegistrationMeta(
        Pid: Environment.ProcessId,
        TalkerId: Guid.NewGuid(),
        Role: "z_brain",
        Name: "z_brain",
        Shadow: false,
        Temporary: false,
        IsChild: false));

    Assert.False(main.IsChildSession);
    Assert.Equal(0, launcher.StartCount);
}
```

新建 `src/II.RPA.Core.Talker.Tests/ZDeskTopTalkerTests.cs`：

```csharp
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Talker.Models;
using II.RPA.Core.Talker.Talkers;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class ZDeskTopTalkerTests
{
    [Fact]
    public async Task ZDeskTopExitStopsChildZBrainProxy()
    {
        var manager = new RecordingTalkerManager();
        var zdesktop = new ZDeskTopTalker(manager, "--m agent", NullLogger.Instance, new TestRuntimeContext());

        await zdesktop.HandleZdesktopProcessStatusChangedAsync(ProcessStatus.Stopped);

        Assert.Equal(1, manager.StopChildZBrainProxyCount);
    }

    [Fact]
    public async Task ZDeskTopStartedDoesNotStopChildZBrainProxy()
    {
        var manager = new RecordingTalkerManager();
        var zdesktop = new ZDeskTopTalker(manager, "--m agent", NullLogger.Instance, new TestRuntimeContext());

        await zdesktop.HandleZdesktopProcessStatusChangedAsync(ProcessStatus.Started);

        Assert.Equal(0, manager.StopChildZBrainProxyCount);
    }

    private sealed class RecordingTalkerManager : ITalkerManager
    {
        public int StopChildZBrainProxyCount { get; private set; }
        private readonly TestHostTalker _hostTalker = new();

        public T GetTalker<T>() where T : ITalker
            => _hostTalker as T ?? throw new NotSupportedException();

        public IReadOnlyCollection<T> GetTalkers<T>(bool readyOnly = true) where T : ITalker => Array.Empty<T>();

        public void StopChildZBrainProxy() => StopChildZBrainProxyCount++;

        public void SetShare(string key, JObject value) { }
        public bool TryGetShare(string key, out JObject value) { value = null!; return false; }
        public Task<JObject> WaitShareAsync(string key, string? role = null, string? name = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IDisposable SubscribeShare(string? key, string? role, string? name, Action<ShareChange> callback) => throw new NotSupportedException();
        public JObject? RemoveShare(string key) => null;
        public IReadOnlyCollection<string> GetShareKeys() => Array.Empty<string>();
        public bool ContainsShare(string key) => false;
        public void WriteShareChannel(string key, JObject value) { }
        public JObject? ReadShareChannel(string key) => null;
        public int CountShareChannel(string key) => 0;
        public ValueTask<JObject> ReadShareChannelAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Initialization() { }
        public Task InitializationAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public (bool, int) CheckZDesktopAllow() => (true, 0);
        public Task RegisterTalker(Grpc.Core.IAsyncStreamReader<FrameStream> requestStream, Grpc.Core.IServerStreamWriter<FrameStream> responseStream, Grpc.Core.ServerCallContext context) => Task.CompletedTask;
        public bool TryGetTalker<T>(out T talker) where T : ITalker { talker = default!; return false; }
        public Task<T> WaitTalker<T>(TimeSpan timeout, CancellationToken cancellationToken = default) where T : ITalker => throw new NotSupportedException();
        public Task<T> WaitTalker<T>(Predicate<T> predicate, CancellationToken cancellationToken = default) where T : ITalker => throw new NotSupportedException();
        public void StartChildZBrainProxy() { }
        public void AddTalker(ITalker talker) { }
        public bool RemoveTalker(ITalker talker) => false;
        public Task StopAsync() => Task.CompletedTask;
    }

    private sealed class TestHostTalker : HostTalker
    {
        public TestHostTalker() : base(new UnusedManager(), NullLogger.Instance, new TestRuntimeContext()) { }
        public override Task Notify(FrameMessage message) => Task.CompletedTask;
    }

    private sealed class UnusedManager : ITalkerManager
    {
        public void SetShare(string key, JObject value) { }
        public bool TryGetShare(string key, out JObject value) { value = null!; return false; }
        public Task<JObject> WaitShareAsync(string key, string? role = null, string? name = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IDisposable SubscribeShare(string? key, string? role, string? name, Action<ShareChange> callback) => throw new NotSupportedException();
        public JObject? RemoveShare(string key) => null;
        public IReadOnlyCollection<string> GetShareKeys() => Array.Empty<string>();
        public bool ContainsShare(string key) => false;
        public void WriteShareChannel(string key, JObject value) { }
        public JObject? ReadShareChannel(string key) => null;
        public int CountShareChannel(string key) => 0;
        public ValueTask<JObject> ReadShareChannelAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Initialization() { }
        public Task InitializationAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public (bool, int) CheckZDesktopAllow() => (true, 0);
        public Task RegisterTalker(Grpc.Core.IAsyncStreamReader<FrameStream> requestStream, Grpc.Core.IServerStreamWriter<FrameStream> responseStream, Grpc.Core.ServerCallContext context) => Task.CompletedTask;
        public T GetTalker<T>() where T : ITalker => throw new NotSupportedException();
        public bool TryGetTalker<T>(out T talker) where T : ITalker { talker = default!; return false; }
        public IReadOnlyCollection<T> GetTalkers<T>(bool readyOnly = true) where T : ITalker => Array.Empty<T>();
        public Task<T> WaitTalker<T>(TimeSpan timeout, CancellationToken cancellationToken = default) where T : ITalker => throw new NotSupportedException();
        public Task<T> WaitTalker<T>(Predicate<T> predicate, CancellationToken cancellationToken = default) where T : ITalker => throw new NotSupportedException();
        public void StartChildZBrainProxy() { }
        public void StopChildZBrainProxy() { }
        public void AddTalker(ITalker talker) { }
        public bool RemoveTalker(ITalker talker) => false;
        public Task StopAsync() => Task.CompletedTask;
    }
}
```

注：`TestHostTalker` 持有一个空实现的 `UnusedManager` 仅为满足 HostTalker 构造签名；HostTalker 构造函数本身不访问 manager 成员（仅存字段），因此安全。文件顶部需补 `using Newtonsoft.Json.Linq;`（JObject）。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q`
Expected: 编译错误（`WorkerRegistrationMeta` 不可访问、`ConfigureTalkerFromWorker` 为 private、`HandleZdesktopProcessStatusChangedAsync` 为 private）

- [ ] **Step 3: TalkerManager 注册触发代理启动**

[TalkerManager.cs:625-642](src/II.RPA.Core.Talker/TalkerManager.cs#L625-L642) 的 `ConfigureTalkerFromWorker` 改为（`private` → `internal`，末尾追加启动逻辑）：

```csharp
internal void ConfigureTalkerFromWorker(ITalker talker, WorkerRegistrationMeta meta)
{
    if (talker is not ZBrainTalker zBrainTalker || meta.Pid <= 0)
    {
        return;
    }

    try
    {
        using var process = Process.GetProcessById(meta.Pid);
        zBrainTalker.IsChildSession = meta.IsChild || _runtimeContext.SessionId != process.SessionId;
    }
    catch (Exception ex)
    {
        zBrainTalker.IsChildSession = meta.IsChild;
        _logger.LogDebug($"读取 ZBrain[{meta.Pid}] Session 失败", ex);
    }

    if (zBrainTalker.IsChildSession)
    {
        StartChildZBrainProxy();
    }
}
```

[TalkerManager.cs:816-823](src/II.RPA.Core.Talker/TalkerManager.cs#L816-L823) 的 `WorkerRegistrationMeta` 记录 `private sealed record` → `internal sealed record`。

- [ ] **Step 4: ZDeskTopTalker 小窗退出停止代理**

[ZDeskTopTalker.cs:147](src/II.RPA.Core.Talker/Talkers/ZDeskTopTalker.cs#L147) 的 `HandleZdesktopProcessStatusChangedAsync` `private` → `internal`；
在 [ZDeskTopTalker.cs:158-162](src/II.RPA.Core.Talker/Talkers/ZDeskTopTalker.cs#L158-L162) 的 childTalker 停止循环结束之后（`}` 之后）插入：

```csharp
_talkerManager.StopChildZBrainProxy();
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q`
Expected: 全部通过

- [ ] **Step 6: 提交**

```bash
cd c:/Works/ii.rpa && git add src/II.RPA.Core.Talker/TalkerManager.cs src/II.RPA.Core.Talker/Talkers/ZDeskTopTalker.cs src/II.RPA.Core.Talker.Tests/ && git commit -m "feat: 子会话z_brain注册时启动子代理，小窗退出时停止

Co-Authored-By: Claude <noreply@anthropic.com>"
```

## Task 4: 子会话 ZBrain 注册后继承主 ZBrain 插件包

**Files:**
- Modify: `src/II.RPA.Core.Talker/Talkers/ZBrainTalker.cs:40, 164-226`
- Modify: `src/II.RPA.Core.Talker/TalkerManager.cs:383-400`
- Test: `src/II.RPA.Core.Talker.Tests/PackageInheritanceTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `src/II.RPA.Core.Talker.Tests/PackageInheritanceTests.cs`：

```csharp
using II.RPA.Core.Talker.Talkers;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class PackageInheritanceTests
{
    [Fact]
    public async Task ChildZBrainInheritsMainZBrainPackage()
    {
        var manager = new TalkerManager(NullLogger.Instance, new TestRuntimeContext(), null, null);
        var main = new ZBrainTalker(manager, NullLogger.Instance, new TestRuntimeContext());
        main.LatestPackagePath = @"C:\packages\flow.zip";
        manager.AddTalker(main);
        var child = new RecordingZBrainTalker(manager) { IsChildSession = true };
        manager.AddTalker(child);

        await manager.InheritMainZbrainPackageAsync(child);

        Assert.Equal(new[] { @"C:\packages\flow.zip" }, child.LoadedPackages);
    }

    [Fact]
    public async Task ChildZBrainSkipsInheritanceWhenMainHasNoPackage()
    {
        var manager = new TalkerManager(NullLogger.Instance, new TestRuntimeContext(), null, null);
        var main = new ZBrainTalker(manager, NullLogger.Instance, new TestRuntimeContext());
        manager.AddTalker(main);
        var child = new RecordingZBrainTalker(manager) { IsChildSession = true };
        manager.AddTalker(child);

        await manager.InheritMainZbrainPackageAsync(child);

        Assert.Empty(child.LoadedPackages);
    }

    [Fact]
    public async Task InheritMainZbrainPackageSwallowsLoadFailure()
    {
        var manager = new TalkerManager(NullLogger.Instance, new TestRuntimeContext(), null, null);
        var main = new ZBrainTalker(manager, NullLogger.Instance, new TestRuntimeContext());
        main.LatestPackagePath = @"C:\packages\flow.zip";
        manager.AddTalker(main);
        var child = new FailingZBrainTalker(manager) { IsChildSession = true };
        manager.AddTalker(child);

        var exception = await Record.ExceptionAsync(() => manager.InheritMainZbrainPackageAsync(child));

        Assert.Null(exception);
    }

    private sealed class RecordingZBrainTalker : ZBrainTalker
    {
        public RecordingZBrainTalker(ITalkerManager manager)
            : base(manager, NullLogger.Instance, new TestRuntimeContext()) { }

        public List<string> LoadedPackages { get; } = new();

        public override Task<(bool IsSuccess, string Body)> LoadPlugin(
            string packagePath,
            bool scan = false,
            CancellationToken cancellationToken = default)
        {
            LoadedPackages.Add(packagePath);
            return Task.FromResult((true, string.Empty));
        }
    }

    private sealed class FailingZBrainTalker : ZBrainTalker
    {
        public FailingZBrainTalker(ITalkerManager manager)
            : base(manager, NullLogger.Instance, new TestRuntimeContext()) { }

        public override Task<(bool IsSuccess, string Body)> LoadPlugin(
            string packagePath,
            bool scan = false,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("z_brain 不可用");
    }
}
```

- [ ] **Step 2: 运行测试确认失败（编译失败）**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q`
Expected: 编译错误（`LatestPackagePath` setter 私有、`LoadPlugin` 非 virtual、`InheritMainZbrainPackageAsync` 不存在）

- [ ] **Step 3: ZBrainTalker 打开测试接缝（行为不变）**

[ZBrainTalker.cs:40](src/II.RPA.Core.Talker/Talkers/ZBrainTalker.cs#L40)：`public string? LatestPackagePath { get; private set; }` → `public string? LatestPackagePath { get; internal set; }`

[ZBrainTalker.cs:164](src/II.RPA.Core.Talker/Talkers/ZBrainTalker.cs#L164) 的 `LoadPlugin` 与 [ZBrainTalker.cs:212, 219](src/II.RPA.Core.Talker/Talkers/ZBrainTalker.cs#L212-L219) 的两个 `ProxyTalk` 重载：方法声明加 `virtual`（`public async Task` → `public virtual async Task`）。

- [ ] **Step 4: TalkerManager 增加继承逻辑**

在 [TalkerManager.cs:640](src/II.RPA.Core.Talker/TalkerManager.cs#L640) 的 `ConfigureTalkerFromWorker` 之后新增：

```csharp
internal async Task InheritMainZbrainPackageAsync(ZBrainTalker childZBrain)
{
    try
    {
        var mainZBrain = GetTalkers<ZBrainTalker>(readyOnly: false)
            .Where(talker => !talker.IsChildSession)
            .OrderByDescending(talker => talker.IsReady)
            .FirstOrDefault();
        if (mainZBrain == null)
        {
            _logger.LogDebug("未找到主会话ZBrain，跳过子会话插件包继承");
            return;
        }

        var packagePath = mainZBrain.LatestPackagePath;
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            _logger.LogDebug("主会话ZBrain尚未加载插件包，跳过继承");
            return;
        }

        _logger.LogInfo($"子会话ZBrain[{childZBrain.Id}]继承主ZBrain插件包:{packagePath}");
        await childZBrain.LoadPlugin(packagePath).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        _logger.LogWarning("子会话ZBrain继承主ZBrain插件包失败", ex);
    }
}
```

在 [TalkerManager.cs:367](src/II.RPA.Core.Talker/TalkerManager.cs#L367) 的 `RegisterTalker` 中，`registrationTask = talker.RegisterTalker(requestStream, responseStream, context);` 赋值语句之后立即插入（fire-and-forget，`InheritMainZbrainPackageAsync` 内部全吞异常）：

```csharp
if (talker is ZBrainTalker zBrainTalker && zBrainTalker.IsChildSession)
{
    _ = InheritMainZbrainPackageAsync(zBrainTalker);
}
```

> 时序关键：`BaseTalker.RegisterTalker` 先同步完成 `EstablishConnection()`（连接建立、talker 已 Online），随后 `await StartReadStream()` 读循环直到 worker 断开——`registrationTask` 只在断开时才完成。因此触发点必须在注册任务创建之后立即执行（对齐参考实现 WorkerGRPCService 注册握手时直接调用 LoadPlugin 的做法），**不能**放在 `await registrationTask` 之后（否则继承发生在断开时刻，且断开后 `EnsureOnlineAsync` 会因 `_onlineCompletion` 已重置而无限挂起）。

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q`
Expected: 全部通过（新增 3 个用例）

- [ ] **Step 6: 提交**

```bash
cd c:/Works/ii.rpa && git add src/II.RPA.Core.Talker/Talkers/ZBrainTalker.cs src/II.RPA.Core.Talker/TalkerManager.cs src/II.RPA.Core.Talker.Tests/ && git commit -m "feat: 子会话ZBrain注册后继承主ZBrain插件包

Co-Authored-By: Claude <noreply@anthropic.com>"
```

## Task 5: HostTalker 按小窗模式路由 zbrain 请求

**Files:**
- Modify: `src/II.RPA.Core.Talker/Talkers/HostTalker.cs:207-218`
- Test: `src/II.RPA.Core.Talker.Tests/ZBrainRoutingTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `src/II.RPA.Core.Talker.Tests/ZBrainRoutingTests.cs`：

```csharp
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.ZBrain;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Talkers;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class ZBrainRoutingTests
{
    [Fact]
    public async Task MiniModeRoutesZBrainRequestToChildSessionTalker()
    {
        var context = new TestRuntimeContext { IsMiniModeActive = true };
        var child = new RoutingZBrainTalker() { IsChildSession = true };
        var main = new RoutingZBrainTalker() { IsChildSession = false };
        var manager = new RoutingTalkerManager(new ITalker[] { main, child });
        var host = new RoutingHostTalker(manager, context);

        await host.HandleIncomingFrameAsync(new FrameMessage<ZBrainRequest>
        {
            Type = FrameType.REQUEST,
            Payload = new ZBrainRequest()
        });

        Assert.Equal(1, child.ProxyTalkCount);
        Assert.Equal(0, main.ProxyTalkCount);
    }

    [Fact]
    public async Task NormalModeRoutesZBrainRequestToMainSessionTalker()
    {
        var context = new TestRuntimeContext { IsMiniModeActive = false };
        var child = new RoutingZBrainTalker() { IsChildSession = true };
        var main = new RoutingZBrainTalker() { IsChildSession = false };
        var manager = new RoutingTalkerManager(new ITalker[] { main, child });
        var host = new RoutingHostTalker(manager, context);

        await host.HandleIncomingFrameAsync(new FrameMessage<ZBrainRequest>
        {
            Type = FrameType.REQUEST,
            Payload = new ZBrainRequest()
        });

        Assert.Equal(1, main.ProxyTalkCount);
        Assert.Equal(0, child.ProxyTalkCount);
    }

    private sealed class RoutingHostTalker : HostTalker
    {
        public RoutingHostTalker(ITalkerManager manager, IRpaRuntimeContext context)
            : base(manager, NullLogger.Instance, context) { }

        public override Task Notify(FrameMessage message) => Task.CompletedTask;
    }

    private sealed class RoutingZBrainTalker : ZBrainTalker
    {
        public RoutingZBrainTalker()
            : base(new RoutingTalkerManager(new ITalker[] { this }), NullLogger.Instance, new TestRuntimeContext()) { }

        public int ProxyTalkCount { get; private set; }

        public override Task<FrameMessage<Reply<string>>?> ProxyTalk(
            FrameRequest request,
            CancellationToken cancellationToken = default)
        {
            ProxyTalkCount++;
            return Task.FromResult<FrameMessage<Reply<string>>?>(
                new FrameMessage<Reply<string>> { Payload = new Reply<string> { Body = "ok" } });
        }
    }

    private sealed class RoutingTalkerManager : ITalkerManager
    {
        private readonly ITalker[] _talkers;

        public RoutingTalkerManager(ITalker[] talkers) => _talkers = talkers;

        public Task<T> WaitTalker<T>(Predicate<T> predicate, CancellationToken cancellationToken = default) where T : ITalker
        {
            var match = _talkers.OfType<T>().FirstOrDefault(talker => predicate(talker));
            if (match == null)
            {
                throw new InvalidOperationException($"无匹配 {typeof(T).Name}");
            }

            return Task.FromResult(match);
        }

        public void SetShare(string key, JObject value) { }
        public bool TryGetShare(string key, out JObject value) { value = null!; return false; }
        public Task<JObject> WaitShareAsync(string key, string? role = null, string? name = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IDisposable SubscribeShare(string? key, string? role, string? name, Action<ShareChange> callback) => throw new NotSupportedException();
        public JObject? RemoveShare(string key) => null;
        public IReadOnlyCollection<string> GetShareKeys() => Array.Empty<string>();
        public bool ContainsShare(string key) => false;
        public void WriteShareChannel(string key, JObject value) { }
        public JObject? ReadShareChannel(string key) => null;
        public int CountShareChannel(string key) => 0;
        public ValueTask<JObject> ReadShareChannelAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Initialization() { }
        public Task InitializationAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public (bool, int) CheckZDesktopAllow() => (true, 0);
        public Task RegisterTalker(Grpc.Core.IAsyncStreamReader<FrameStream> requestStream, Grpc.Core.IServerStreamWriter<FrameStream> responseStream, Grpc.Core.ServerCallContext context) => Task.CompletedTask;
        public T GetTalker<T>() where T : ITalker => throw new NotSupportedException();
        public bool TryGetTalker<T>(out T talker) where T : ITalker { talker = default!; return false; }
        public IReadOnlyCollection<T> GetTalkers<T>(bool readyOnly = true) where T : ITalker => Array.Empty<T>();
        public Task<T> WaitTalker<T>(TimeSpan timeout, CancellationToken cancellationToken = default) where T : ITalker => throw new NotSupportedException();
        public void StartChildZBrainProxy() { }
        public void StopChildZBrainProxy() { }
        public void AddTalker(ITalker talker) { }
        public bool RemoveTalker(ITalker talker) => false;
        public Task StopAsync() => Task.CompletedTask;
    }
}
```

注：`RoutingZBrainTalker` 构造中的 `new RoutingTalkerManager(new ITalker[] { this })` 仅为满足 ZBrainTalker 构造签名，无实际行为。文件顶部需补 `using Newtonsoft.Json.Linq;`（JObject）。

- [ ] **Step 2: 运行测试确认失败（行为断言失败）**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q --filter ZBrainRoutingTests`
Expected: 编译通过但测试失败（当前实现恒取 `GetTalker<ZBrainTalker>()`，`RoutingTalkerManager.GetTalker` 抛 `NotSupportedException`，或路由断言不成立）

- [ ] **Step 3: 修改 HostTalker zbrain 请求分支**

[HostTalker.cs:209-217](src/II.RPA.Core.Talker/Talkers/HostTalker.cs#L209-L217) 改为：

```csharp
if (frameMessage is FrameMessage<ZBrainRequest> zbrainRequest)
{
    Logger.LogDebug($"Handle zbrain proxy message:{zbrainRequest?.Payload?.Path}");
    var wantChildSession = _runtimeContext.IsMiniModeActive;
    Logger.LogDebug($"等待 zbrain 初始化 minimode:{wantChildSession}");
    var zbrainTalker = await _talkerManager.WaitTalker<ZBrainTalker>(
        talker => talker.IsChildSession == wantChildSession,
        CancellationToken.None).ConfigureAwait(false);
    var reply = await zbrainTalker.ProxyTalk(new FrameRequest
    {
        Id = zbrainRequest.Id,
        ReplyId = zbrainRequest.ReplyId,
        Type = zbrainRequest.Type,
        Code = zbrainRequest.Code,
        Message = zbrainRequest.Message,
        Buffer = zbrainRequest.Buffer,
        Payload = zbrainRequest.Payload
    }, CancellationToken.None).ConfigureAwait(false);
    await Notify(reply).ConfigureAwait(false);
}
```

> 信封必须完整复制：`BaseTalker` 读循环按 `frameMessage.ReplyId` 关联应答（BaseTalker.cs:227），远端请求方以原请求 Id 等待匹配应答；且 `FrameType` 默认值为 `PING=0`（FrameType.cs:5），不复制 Type 会发出错误帧类型。`FrameMessage.Id` 默认 NewGuid，仅复制 Payload 同样会破坏应答关联。

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q`
Expected: 全部通过

- [ ] **Step 5: 提交**

```bash
cd c:/Works/ii.rpa && git add src/II.RPA.Core.Talker/Talkers/HostTalker.cs src/II.RPA.Core.Talker.Tests/ && git commit -m "feat: zbrain请求按小窗模式路由到子会话ZBrain

Co-Authored-By: Claude <noreply@anthropic.com>"
```

## Task 6: ZDesktop ExitCode=5 自动以管理员权限重启

**Files:**
- Modify: `src/II.RPA.Core.Talker/Processes/ZDesktopProcess.cs:45-60`
- Test: `src/II.RPA.Core.Talker.Tests/ZDesktopProcessTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `src/II.RPA.Core.Talker.Tests/ZDesktopProcessTests.cs`：

```csharp
using II.RPA.Core.Log;
using II.RPA.Core.Talker.Processes;
using System.Diagnostics;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class ZDesktopProcessTests
{
    [Fact]
    public async Task ExitCode5TriggersAutomaticRestartWithAdministrator()
    {
        var process = new TestZDesktopProcess(NullLogger.Instance, new TestRuntimeContext());

        process.TriggerExitHandle(isInitiativeStop: false, exitCode: 5);

        Assert.True(process.WithAdministrator);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (process.StartAttempts == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }

        Assert.True(process.StartAttempts >= 1, "ExitCode=5 非主动退出应触发自动重启");
    }

    [Fact]
    public async Task OtherExitCodesDoNotTriggerAutomaticRestart()
    {
        var process = new TestZDesktopProcess(NullLogger.Instance, new TestRuntimeContext());

        process.TriggerExitHandle(isInitiativeStop: false, exitCode: 3);

        Assert.False(process.WithAdministrator);
        await Task.Delay(1500);
        Assert.Equal(0, process.StartAttempts);
    }

    [Fact]
    public async Task InitiativeStopDoesNotTriggerRestart()
    {
        var process = new TestZDesktopProcess(NullLogger.Instance, new TestRuntimeContext());

        process.TriggerExitHandle(isInitiativeStop: true, exitCode: 5);

        Assert.True(process.WithAdministrator);
        await Task.Delay(1500);
        Assert.Equal(0, process.StartAttempts);
    }

    private sealed class TestZDesktopProcess : ZDesktopProcess
    {
        public TestZDesktopProcess(ILogger logger, II.RPA.Core.Runtime.IRpaRuntimeContext context)
            : base("--m agent", logger, context) { }

        public int StartAttempts { get; private set; }

        protected override Process GetChildProcess(ref bool addChildProcess)
        {
            StartAttempts++;
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Path.GetTempPath(), "ii-rpa-nonexistent-zdesktop.exe"),
                    UseShellExecute = false
                }
            };
        }

        internal void TriggerExitHandle(bool isInitiativeStop, int? exitCode)
            => ProcessExitHandle(isInitiativeStop, exitCode);
    }
}
```

说明：`GetChildProcess` 返回指向不存在文件的 Process，`Start()` 抛 `Win32Exception` 后被 `BaseProcess.StartCore` 捕获记录——不会真正启动进程，测试仅断言重启被触发。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q --filter ZDesktopProcessTests`
Expected: 编译失败（`WithAdministrator` 不存在）；补齐后行为断言失败（当前实现不触发自动重启）

- [ ] **Step 3: 修改 ZDesktopProcess**

[ZDesktopProcess.cs:45-60](src/II.RPA.Core.Talker/Processes/ZDesktopProcess.cs#L45-L60) 的 `ProcessExitHandle` 替换为：

```csharp
internal bool WithAdministrator => _withAdministrator;

protected override void ProcessExitHandle(bool isInitiativeStop, int? exitCode)
{
    _withAdministrator = exitCode == 5;
    if (_withAdministrator)
    {
        Logger.LogWarning("ExitCode=5，后续启动将改用管理员权限");
    }

    if (!isInitiativeStop && _withAdministrator)
    {
        // ExitCode=5（子会话启用需管理员权限）时沿用基础自动重启框架：
        // 指数退避（500ms 起）、最多 5 次熔断；重启时 GetChildProcess 已读取
        // _withAdministrator，以管理员(runas)方式启动。
        base.ProcessExitHandle(isInitiativeStop, exitCode);
    }

    // 其他异常退出保持现状：ZDesktop 由用户主动拉起，不做守护式自动重启。
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q`
Expected: 全部通过（重启用例约 1-2 秒，含 500ms 退避）

- [ ] **Step 5: 提交**

```bash
cd c:/Works/ii.rpa && git add src/II.RPA.Core.Talker/Processes/ZDesktopProcess.cs src/II.RPA.Core.Talker.Tests/ && git commit -m "feat: ZDesktop ExitCode=5时自动以管理员权限重启

Co-Authored-By: Claude <noreply@anthropic.com>"
```

## Task 7: 全量验证

- [ ] **Step 1: 受影响项目全量编译**

Run: `dotnet build c:/Works/ii.rpa/src/II.RPA.Core.Abstractions/II.RPA.Core.Abstractions.csproj -v q && dotnet build c:/Works/ii.rpa/src/II.RPA.Core/II.RPA.Core.Provider/II.RPA.Core.Provider.csproj -v q && dotnet build c:/Works/ii.rpa/src/II.RPA.Core.Talker/II.RPA.Core.Talker.csproj -v q`
Expected: 均 0 Error

- [ ] **Step 2: 全量测试**

Run: `dotnet test c:/Works/ii.rpa/src/II.RPA.Core.Talker.Tests/II.RPA.Core.Talker.Tests.csproj -v q`
Expected: 全部通过（既有 4 个 + 新增约 18 个）

- [ ] **Step 3: 主解决方案编译（验证整体兼容）**

Run: `dotnet build c:/Works/ii.rpa/src/II.RPA.sln -v q`
Expected: 0 Error。若出现与本任务无关的既有错误（如环境缺失 SDK），记录输出并与 Task 0 基线对比：不新增与本任务相关文件的错误即可。

- [ ] **Step 4: 检查工作区状态**

Run: `git -C c:/Works/ii.rpa status --short && git -C c:/Works/ii.rpa log --oneline -7`
Expected: 工作区干净；最近 7 条提交为 Task 0-6 的 6 个提交（+ 设计文档提交）

## 风险与备选

- **Unity 4 参构造解析**：CoreTalkerManager 注册带显式 `<constructor>`（Unity 对显式构造按精确参数个数匹配，不走贪婪选择），必须在其中显式追加 `childZBrainProxyLauncher` 参数（dependencyType=IZBrainChildProxyLauncher）并配一条 container.config 回归测试（断言该 param 存在），否则 launcher 在生产恒为 null、代理启停退化为静默 no-op。
- **`ZBrainProxyProvider` 被提前构造**（TalkerManager 解析时作为构造依赖）：其构造函数仅赋值字段（无启动副作用），提前构造安全；`Start()` 仍由 Program.cs:130-134 按 `Global.HostZBrain` 控制，行为不变。
- **继承触发点**：fire-and-forget 在 `registrationTask` 创建后立即触发（连接已同步建立、talker 已 Online），`LoadPlugin` 内部 `EnsureOnlineAsync` 立即返回；并配 30 秒 CancellationTokenSource 超时保护，杜绝任何路径的无限等待（与参考实现的注册握手期调用语义一致）。
