# 小窗运行缺失链路补全设计（子会话 ZBrain 接线）

日期：2026-08-14
分支：feat-dotnet-upgrade-net8.0
参考实现：`C:\Works\Backup\ii.rpa`（develop-7.3.7）

## 背景

II.RPA 在 .NET 升级分支上完成了 Talker 体系重构（TalkerProvider → `II.RPA.Core.Talker`）。
对比参考实现（7.3.7）盘点发现，"小窗运行"的窗口 UI 与进程骨架迁移完整，但
"小窗模式下任务跑在子会话 ZBrain 上"的执行链有 3 处接线丢失，另有 1 处行为差异：

1. 子会话 ZBrain 代理（6442 端口，ChildStartup）的启动/停止接线丢失
   —— 当前 `ZBrainProxyProvider.StartZdesktopProxy/StopZdesktopProxy` 全仓库零调用；
   参考实现由 WorkerGRPCService（z_brain 子会话注册时）与 HostTalker（小窗退出时）调用。
2. 子会话 zbrain 插件包继承丢失
   —— 参考实现注册子会话 z_brain 时继承主 zbrain 的 `LatestPackagePath` 并 `LoadPlugin`；
   当前 `ConfigureTalkerFromWorker` 只设置 `IsChildSession`。
3. zbrain 消息路由不再按小窗模式选择 zbrain
   —— 参考实现 `HostTalker.StartZbrainPureProxy` 按 `Global.IsMiniModeActive` 选择
   子会话/主会话 ZBrainTalker 并等待 Loaded/Online 状态；当前 `HostTalker` 直接
   `GetTalker<ZBrainTalker>()`（取注册最早者，无筛选、无状态等待）。
4. ExitCode=5 不再自动以管理员权限重启
   —— 参考实现 ZdesktopGuard 在 ExitCode=5 时立即以管理员重启；当前只记录
   `_withAdministrator` 供下次显式启动使用。用户已确认恢复自动重启行为。

## 范围

修复上述 4 项，不改动其他行为。以下不在范围内（两侧共同的既有状态，保持现状）：

- `ChildSessionClientFacade` 死代码、`StartZDeskTop` RPC 无调用方（两侧一致）
- `ControlService` 的 `subscribe.start/stop` 注释与 `restore.on` 未实现（两侧一致）
- 两个 childsession proto 版本字段号错位（两侧一致）
- `IsMiniModeActive` 仓库内无写入方（依赖前端下发 settings，两侧一致）

## 方案 A：在新架构自然接点恢复备份语义

### ① 子会话 ZBrain 代理（6442）启停接线

新接口（程序集 `II.RPA.Core.Abstractions`，命名空间 `II.RPA.Core.Runtime`，与
`IZBrainController` 同域）：

```csharp
public interface IZBrainChildProxyLauncher
{
    void StartChildProxy();
    void StopChildProxy();
}
```

- `ZBrainProxyProvider` 实现该接口，内部委托现有 `StartZdesktopProxy()` /
  `StopZdesktopProxy()`（`ZBrainProxyProvider.cs:84-98`），零行为变化。
- `container.config` 增加类型别名与注册：
  `<register type="IZBrainChildProxyLauncher" mapTo="ZBrainProxyProvider">`
  （ContainerControlled）。注意：Unity 生命周期按注册键缓存，接口注册与
  `ZBrainProxyProvider` 自身注册是**两个独立实例**——接口实例是子代理（6442）
  生命周期的唯一入口，具体类型实例仅供 Program.cs 调用 `Start()` 管理根代理，
  两者职责分离、无共享状态（m_ClientKind 均来自同一 RuntimeContext，行为一致）。
- `TalkerManager` 增加第 4 个可选构造参数 `IZBrainChildProxyLauncher? childProxyLauncher`
  （沿用现有 `IAdbStartupSignal? startupSignal` 的 null 安全模式）；CoreTalkerManager 注册带显式
  `<constructor>`（Unity 按精确参数个数匹配构造），必须在其中显式追加 `childZBrainProxyLauncher`
  参数（dependencyType=IZBrainChildProxyLauncher），否则 4 参构造不被选用、launcher 在生产恒为 null。
- `ITalkerManager`（`II.RPA.Core.Talker` 项目内接口）增加：
  `void StartChildZBrainProxy();` / `void StopChildZBrainProxy();`
  `TalkerManager` 实现为委托注入的 launcher，未注入时记日志 no-op。

触发点：

- **启动**：`TalkerManager.ConfigureTalkerFromWorker`（TalkerManager.cs:625-642），
  检测到 `zBrainTalker.IsChildSession == true` 时调用 `StartChildZBrainProxy()`。
  幂等性由现有实现保证（先 StopProxy 再 StartProxy，天然支持子会话换代）。
- **停止**：`ZDeskTopTalker.HandleZdesktopProcessStatusChangedAsync` 的 Stopped 分支
  （ZDeskTopTalker.cs:158-162，停 ChildCoreServerTalkers 处）调用 `StopChildZBrainProxy()`。

### ② 子会话 zbrain 插件包继承

在 `TalkerManager.RegisterTalker` 中 `registrationTask = talker.RegisterTalker(...)` 创建
之后**立即 fire-and-forget 触发**（`_ = InheritMainZbrainPackageAsync(...)`）：

- 时序依据：`BaseTalker.RegisterTalker` 先**同步**完成 `EstablishConnection()`（连接建立、
  talker 已 Online、流已赋值），随后 `await StartReadStream()` 读循环直到 worker 断开——
  即 `registrationTask` 只在断开时才完成。因此触发点必须在注册任务创建之后立即执行，
  与参考实现（WorkerGRPCService 注册握手时直接调用 LoadPlugin）一致；若放在
  `await registrationTask` 之后，继承会发生在断开时刻且因 `_onlineCompletion` 已重置而挂起。
- 触发条件：`talker is ZBrainTalker childZBrain && childZBrain.IsChildSession`。
- `InheritMainZbrainPackageAsync` 内部：
  - 取主 zbrain：复用 `ZBrainController.SelectTalker(childSession: false)`（非子会话、按 IsReady 降序、非终止优先，与 OWIN 通道同一选择策略）。
  - 主 zbrain 的 `LatestPackagePath` 非空则 `await childZBrain.LoadPlugin(path)`，
    配 30 秒 `CancellationTokenSource` 超时保护，杜绝任何路径的无限等待；
  - 整体 try/catch + 日志（含 childZBrain.Id），失败不阻断注册流程。
- 保持备份语义：仅在注册时继承一次；运行中切换包仍走既有
  `/workers/zbrain/switch` OWIN 通道（`ZBrainController.SwitchPackageAsync`）。

### ③ zbrain 消息路由恢复小窗模式选择

- `ITalkerManager` / `TalkerManager` 增加重载
  `Task<T> WaitTalker<T>(Predicate<T> predicate, CancellationToken cancellationToken = default)`
  —— 50ms 轮询、无超时上限（对齐参考实现 `WaitTalker<T>(Predicate<T>)` 的等待语义），
  由调用方用 `.WaitAsync(timeout)` 限界。
- `HostTalker` zbrain 请求分支（HostTalker.cs:209-217）改为：

```csharp
var wantChildSession = _runtimeContext.IsMiniModeActive;
var zbrainTalker = await _talkerManager.WaitTalker<ZBrainTalker>(
    talker => talker.IsChildSession == wantChildSession,
    CancellationToken).ConfigureAwait(false)
    .WaitAsync(ZBrainRequestTimeout);           // 30s，protected virtual，测试可覆写
var reply = await zbrainTalker.ProxyTalk(new FrameRequest
{
    Id = zbrainRequest.Id,          // 信封完整复制：BaseTalker 读循环按 ReplyId 关联应答，
    ReplyId = zbrainRequest.ReplyId,// 远端请求方以原 Id 等待；FrameType 默认 PING=0，
    Type = zbrainRequest.Type,      // 必须复制 Type 避免帧类型错误
    Code = zbrainRequest.Code,
    Message = zbrainRequest.Message,
    Buffer = zbrainRequest.Buffer,
    Payload = zbrainRequest.Payload
}, CancellationToken).ConfigureAwait(false)
    .WaitAsync(ZBrainRequestTimeout);
await Notify(reply).ConfigureAwait(false);
```

说明：
- `ProxyTalk(FrameRequest)` 内置 `EnsureOnlineAsync`（State ∈ Online/Loading/Loaded
  直接返回，否则等 `_onlineCompletion`），等价参考实现"等待 Loaded/Online 后再转发"。
- 有界化与错误应答：等待与请求均以 30s 限界、以连接令牌（`BaseTalker.CancellationToken`）
  可取消；超时或 null 应答时向远端回传 `Code=504` 的错误 REPLY（ReplyId=原请求 Id），
  保证远端按 ReplyId 关联的应答不会悬空；宿主连接关闭时请求随连接丢弃。子会话 zbrain
  连接断开（取消）时同样回传 504（宿主连接关闭除外）。这是对参考实现
  （无限等待）的有意增强——当前架构中该分支运行在 Host 连接读循环热路径上，无限挂起
  会卡死同连接的所有上游帧。

### ④ ExitCode=5 自动管理员重启

`ZDesktopProcess.ProcessExitHandle`（ZDesktopProcess.cs:45-60）改为：

```csharp
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
        // 指数退避（500ms 起）、最多 5 次熔断；重启时 GetChildProcess 已读
        // _withAdministrator，以 runas 启动。
        base.ProcessExitHandle(isInitiativeStop, exitCode);
    }
    // 其他异常退出保持现状：ZDesktop 由用户主动拉起，不做守护式自动重启。
}
```

与参考实现的差异（有意保留）：参考实现 ExitCode=5 无限递归重启，若管理员权限下
仍退出 5 会死循环；本方案复用 `BaseProcess.ScheduleAutomaticRestart` 的熔断机制。

## 错误处理

- launcher 未注入（如接口映射注册遗漏）：`StartChildZBrainProxy/StopChildZBrainProxy`
  no-op + 日志，不抛异常。
- 插件继承失败（子 zbrain 不可用、加载失败）：日志记录，注册流程不受影响。
- 代理启动失败：现有 `StartProxy` 内部已处理端口绑定重试与日志，不新增处理。
- `WaitTalker` predicate 永不满足：与参考实现一致地无限等待（由外层订阅生命周期控制，
  与现有 HostTalker 无取消令牌的上下文一致）。

## 测试计划（II.RPA.Core.Talker.Tests，先测试后实现）

1. `TalkerManager`：子会话 zbrain 注册（`ConfigureTalkerFromWorker`）触发 launcher
   `StartChildProxy`；未注入 launcher 时不抛异常。
2. `ZDeskTopTalker` 退出处理：Stopped 时调用 `StopChildZBrainProxy`。
3. 插件继承：子 zbrain 注册完成且主 zbrain `LatestPackagePath` 非空 → 对子 zbrain
   调用 `LoadPlugin`；为空 → 不调用。
4. `WaitTalker<T>(Predicate<T>)`：满足即返回；不满足持续等待；取消令牌生效。
5. `HostTalker` zbrain 路由：`IsMiniModeActive=true` → 选择 `IsChildSession` 的 talker；
   `false` → 选择非子会话 talker。
6. `ZDesktopProcess`：ExitCode=5 非主动退出 → 触发一次自动重启，且重启进程启动参数
   使用管理员方式（`_withAdministrator=true`）；其他退出码不触发自动重启。

## 验收标准

- `dotnet build` 全解决方案通过（含 x86/x64 Release 配置不变）。
- 新增单测全绿。
- 行为对照：小窗模式（setmini 生效）下，子会话 z_brain 注册后主 CoreServer 的
  6442 子代理被启动；小窗退出后停止；子会话 zbrain 自动加载主 zbrain 当前流程包；
  zbrain 代理请求按小窗模式路由到子会话 zbrain。
- 小窗进程因权限不足以 ExitCode=5 退出时，自动以管理员权限重启。
