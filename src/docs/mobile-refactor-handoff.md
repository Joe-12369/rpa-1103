# Mobile Module Refactor Handoff

This document is intended for Claude Code to understand the current state of the mobile automation refactoring, the decisions made so far, and the remaining work.

## Purpose

Continue the mobile module stability and maintainability refactoring without restarting the analysis from zero.

## Refactoring Progress Log

> Each committed refactor batch appends an entry here (newest on top) so any tool can quickly resume from the current state. Record only adopted (committed) changes; decisions/conclusions that were adopted without code go into "Confirmed Decisions and Constraints".

- `2026-08-26` — `b0fbb6539a` — **Shared selector XPath construction**. Regular single-element lookup and collection-style similar lookup now share the same structural-path versus accurate-predicate XPath builder while preserving their separate single-result/multi-result driver calls.
- `2026-08-26` — `566aa54efe` — **Unified similar-element capture lookup**. `CheckSimilarElement` now reuses `BuildVerifiedSelector` and `FindElementCore` instead of maintaining an independent ancestor-ID/XPath lookup implementation. Similar capture keeps one explicit policy difference: the target node itself cannot be used as the unique anchor; stable ancestor `id`/`description`/`text` anchors and full-index fallback are shared with regular capture.
- `2026-08-26` — `1daf006e24` — **Provider-owned Appium process and port management**. Removed global `node.exe`/`netstat` discovery and the fallback `taskkill` of an unknown process occupying port 4723. The Provider now trusts only the Appium process it starts, selects a free candidate port by socket binding, logs initialization failures, and terminates its owned Appium process during Provider shutdown.
- `2026-08-26` — `77e6439c77` — **Prevent stale session recovery after operation timeout**. Added per-device operation generations captured when a timeout-protected operation is created. A timed-out health probe or automatic reconnect can no longer publish a newly created session after the caller has already timed out. `IsSessionValid` also rechecks timeout/recovery state after the Appium probe completes.
- `2026-08-26` — `7137512113` — **Preserve current capture selector lookup after legacy cleanup**. Capture-time node arrays are explicitly wrapped as the current internal `RPACommon` form and passed to `FindElementCore`; capture does not run execution-time page/image validation.
- `2026-08-26` — `47a7090fbe` — **Removed obsolete Android selector compatibility**. Deleted `AndroidJsonItem`, `isOldSelector`, `IsStructuredSelector`, and the legacy conversion branches. Current persisted elements use `RPACommon`; the bare node array used during capture remains a current internal capture protocol, not a historical persisted selector.
- `2026-08-26` — `07c2882b54` — **Bound management calls with session timeouts**. Management gRPC operations use real timeout boundaries and quarantine timed-out sessions.

- `2026-08-26` — `c2c76361b9` — **Timeout isolation and connection publication stability (P0)**. Replaced ineffective cancellation-only timeouts with real `Task.WhenAny` boundaries; timed-out Appium sessions are quarantined until the background command ends and then require recovery. Added per-device connection generations so timed-out or superseded connection attempts cannot publish stale sessions. Added a dedicated 7-second PageSource provider timeout and routed the management gRPC PageSource call through it.

- `2026-08-22` — `ea3a32beca` — **相似元素/常规查找路径 selector 构建对齐（P1）**. 常规元素查找（`FindElementCore`）与相似元素查找（`FindSimilarElementCore`/`CheckSimilarElementCore`）中重复的 XPath 拼接块收敛为 3 个纯函数，并删除一处 330 行死代码。
  - 新增纯函数：`MapXPathAttributeName`（props.name→XPath 属性名映射）、`BuildStructuralSegment`（class+index 结构片段，`useIndexZeroWhenMissing` 控制 index 缺失时输出 `[@index=0]` 或省略，`onIndexMark` 回调供 CheckSimilar 标记 accurate）、`BuildPredicatePath`（多节点精确属性谓词部分路径）。
  - 收敛点：`FindElementCore`/`FindSimilarElementCore` 两处完全相同的 ~50 行 XPath 谓词块（含 type 属性名映射 if 链）→ `BuildPredicatePath`；三方法四处 class+index 结构路径循环 → `BuildStructuralSegment`（FindSimilar 用省略-index 模式，CheckSimilar 用 `[@index=0]`+accurate 标记模式，FindElement 用 `[@index=0]` 模式）。
  - **删除死代码 `FindElement_old`（约 330 行）**：全项目零调用方、无项目直接引用 MobileProvider（独立 gRPC 进程），是 `FindElementCore` 的旧版全量重复（含前导零 index 特殊处理）。该批次当时仍保留旧 selector 分支；该分支已在 `47a7090fbe` 中正式删除。
  - 净减约 392 行；构建 0 警告 0 错误；普通 selector 输出逐字不变。
  - `II.RPA.Plugin.Mobile` is the retired mobile module and remains outside this refactor scope; do not use it as the reference implementation.
- `2026-08-22` — `573a1aa267` — **XPath/UiAutomator 选择器纯函数提取（P1）**. `MobileSession` 中 2 组近乎重复的 UiAutomator 选择器拼接块 + 3 组 XPath 谓词 if/else 矩阵收敛为 6 个纯函数，并补齐此前缺失的值转义。
  - 新增纯函数：`EscapeUiAutomatorLiteral`（转义 `\` 与 `"`）、`EscapeUiAutomatorRegex`（仅转义 `"`，保留正则语义）、`BuildUiAutomatorTextSelectorFragment`、`BuildUiAutomatorSelector`、`ToXPathLiteral`（值含单/双引号自动选引号，双引号都有时用 `concat()` 兜底）、`BuildXPathPredicate`。
  - UiAutomator 侧：4 处重复拼接（单属性 text/description 各 2 处 + 多属性 StringBuilder 块 2 处）统一走 `BuildUiAutomatorSelector`/`BuildUiAutomatorTextSelectorFragment`；`className`/`resourceId`/`text`/`description`/`index` 值统一转义。
  - XPath 侧：3 组谓词矩阵（regular/contain/begin/end/等于 → matches/contains/starts-with/ends-with/`@x='v'`）收敛为 `BuildXPathPredicate`；全部 30 处单引号值 + 5 处 anchor 双引号值（`@resource-id`/`@content-desc`/`@text`/`@anchorType`）改用 `ToXPathLiteral`。普通值输出与旧代码逐字一致，仅修复含引号值此前会生成非法 XPath/选择器的问题。
  - 净减约 230 行；构建 0 警告 0 错误，`git diff --check` 干净。
  - **已知边界（本批未处理）**：含 `\` 的正则值（`textMatches`/`descriptionMatches`）仍不转义——`\` 在正则中有含义，旧代码同样未处理，本批保持行为不变；结构化路径拼接随后已继续收口；`CheckSimilarElementCore` 的独立构建链路最终在 `566aa54efe` 中删除。
- `2026-08-22` — `6cefa4760c` — **错误处理统一（P1）**. MobileProvider 与 MobileDeviceManager 全模块 80+ 处 `catch` 内 `throw ex;`/`throw cvEx;`/`throw rpaEx;`/`throw e;` 改为裸 `throw;`（保留原始堆栈，便于定位根因）。
  - `MobileSession` 4 处 `throw new NotFoundException();` 改为 `throw new NotFoundException(ex.Message, ex);` —— 原代码吞掉原始异常，调用方收到的 NotFoundException 无消息、无内因。
  - 注释里残留的 `// throw ex;` 等保持不变（不做无谓改动）。
  - **验证点**：元素查找失败抛出的 NotFoundException 带真实 Message 与 InnerException，日志/错误上报含原始堆栈。
- `2026-08-22` — `00d939af3b` — **CacheList 并发安全（P1）**. `MobileSession.CacheList` 是无锁 `List<MobileElement>`，多线程（元素查找/验证/页面解析/拾取）访问时存在 Clear/Add 与枚举、Count 的集合竞争。
  - 新增 `_cacheListLock` 锁与三个锁内辅助方法：`GetCachedElementsSnapshot`（`Where(...).ToList()` 快照）、`CountCachedElements`、`FindCachedElement`。
  - 全部 4 个写点（3 处 `CacheList.Clear()` + 1 处 `CacheList.Add`）与所有 `Count`/`FirstOrDefault` 读点改为锁内执行；两处延迟 LINQ `Where`（GetMobileElementPath / similar 元素验证）改为锁内 ToList 快照，消除枚举期间的集合变更风险。
  - **验证点**：并发元素查找 + 页面解析/拾取时无 `Collection was modified during enumeration` 异常。
- `2026-08-22` — `55ea2e2ab` — **Appium process management** (global analysis, step 2). Fixes the documented production crash root cause (node.exe killed by OS).
  - Root cause: `StartMobileServer` called `BeginErrorReadLine`/`BeginOutputReadLine` with **no** `OutputDataReceived`/`ErrorDataReceived` handlers — stdout/stderr buffers filled, node.exe's log write blocked, OS terminated it. Now both events are subscribed and drained to the logger (`LogDebug` output / `LogWarning` stderr).
  - Subscribed `process.Exited`: on unexpected exit (not shutting down) it logs and auto-restarts the Appium server — single-flight via `Interlocked` guard, skips if the port is already listening, delays 1s so the OS releases the port. Eager restart pre-warms the server; session reconnect (autoReConnect) still re-creates the Appium sessions.
  - Added `MobileServerManager.MarkShuttingDown()`; `Program.ServerEnding()` now calls it first so `Exited` can distinguish planned shutdown from crash and skip the restart. `_isShuttingDown` is `volatile`.
  - `IsAppiumPortListening` made `static` (shared by restart logic); fixed the instance-call at `MobileServer.cs:64`.
  - **Design note:** the eager restart reuses `GlobleConfig.AppiumServerPort` and is skipped if the port is already listening (guards against double-start with the lazy restart in `CreateConnectSessionCoreAsync` → `InitMobileServer`).
  - **Remaining (deferred):** port/PID conflict handling fragility (`GetAvailablePort` regex + `taskkill` 4723 occupier), sync-over-async `connectTask.Wait(...)`, `InitMobileServer` silent swallow — separate hardening pass.
  - **Real-device verify:** kill the node.exe process mid-run → observe auto-restart + session auto-reconnect; normal provider exit → no spurious restart log.
- `2026-08-22` — `623251629` — **Fix P0 correctness defects** (global analysis, step 1). Five independent low-risk fixes in `MobileSession.cs`:
  - `ClickElementCore` retry loop `while (!eleEnabled || tryCount < 3)` → `&&`: when element is already enabled this caused 3×500ms useless delay; when it never becomes enabled → infinite loop (holding `driverCommandLock`). Confirmed by two independent scans.
  - `DialCore` dial switch only mapped '0'-'9'; `+`/`*`/`#` fell through to `key=7` (i.e. '0'). Added `+`→KEYCODE_PLUS(81), `*`→KEYCODE_STAR(17), `#`→KEYCODE_POUND(18). The unconditional 20× DEL before dialing was intentionally **kept as-is** (deliberate "clear stale dial-pad digits" heuristic).
  - `HuaWeiDialCore` `contract?[0].Click()`: the `?` guarded the indexer, not the call → NRE when the tab-icon list is empty. Now guarded by `if (contract != null && contract.Count > 0)`.
  - `GetMobileElementText` used `eComponentType.GetMobileElementScreenshot` (copied from the screenshot method); changed both catch clauses to the correct `eComponentType.GetText`.
  - `GetImageRects` created a temp `Guid.png` that was never deleted (comment even said "记得执行完删除"). Body extracted to `GetImageRectsCore` and wrapped in try/finally that `File.Delete`s the temp file (leak confirmed — method is live, called at :1822/:1864).
  - **Next (global analysis step 2):** Appium process management — subscribe stdout/stderr consumption, `process.Exited` + auto-restart, `_isShuttingDown`; plus make `RunWithTimeOutAsync` genuinely cancellable (Hang Up mechanism).
- `2026-08-21` — `d8d43286b` — **Close the Similar-lookup concurrency gap (Similar analysis P0)**. Wrapped the last 4 bare `_driver` calls in the Similar/condition family with `ExecuteDriverCommand`: `CheckSimilarElement`, `FindSimilarElement`, `GetSimilarElementListByCondition`, `GetElementByCondition`. `CheckSimilarElement`'s `out` param handled via a captured local. Cleanups: removed shared mutable field `buildSelectorJson` (→ local), removed dead `isUsedXpath` flag. Completes serialization coverage of all remaining high-risk `_driver` operations. **Subsequent status:** exception handling and XPath builder consolidation were completed in later commits; similar capture now shares the regular selector-building and lookup core.
- `2026-08-21` — `6daa78f9d` — **Serialize high-risk driver operations with `ExecuteDriverCommand`** (completes handoff work-item #1). Wrapped 15 `_driver` operations: click/touch (`ClickElement`, `ClickScreen`, `ClickScreenByImagePoint`), input/key (`InputText`, `ClickKeyButton`, `Dial`, `HuaWeiDial`, `ClickDailNum`), slide (`TouchSlid_WithSelector`, `TouchSlid`, `TouchSlid_V2`), app (`ActivateApp`, `CloseApp`), clipboard write (`SetClipboard`), orientation write (`SetOriention`).
  - Pattern: public method delegates to a private `XxxCore` method inside `ExecuteDriverCommand`; body preserved verbatim (no re-indent); reentrant `lock` handles nested `FindElement` → `RefreshPageSource` calls.
  - **Scale decision (adopted)**: serialize only "multi-step atomic sequences + state-changing operations"; pure single reads intentionally left unwrapped (`GetFromClipboard`, `GetOriention`, `GetConnectDeviceDetail`, `PushFileToMobile`/`GetFileFromMobile`, `FileTransfer`).
- `2026-08-21` — Baseline before the later .NET 8 rebase. Previous refactoring batches (PageSource refresh strategy, connection/session stability, state isolation, element lookup stability, screen mirroring) are captured in the "Completed Changes" and current "Commit Map" sections below.

## Deferred Verification Checklist

> The user will run ONE unified real-device verification pass after the refactoring is substantially complete, walking the optimization points below one by one. Do NOT verify per-batch during refactoring. Newest batch on top; append here after each committed refactor batch.

- `ea3a32beca` 相似元素/常规查找路径 selector 构建对齐:
  - 常规元素查找（单属性/多属性/多节点精确/全路径 class+index 四种分支）→ 结果与重构前一致
  - 相似元素查找 CheckSimilarElement/FindSimilarElement → 结果与重构前一致；拾取返回的 buildSelecter 中 index 节点仍被标记 accurate
  - 老结构 selector（AndroidJsonItem 格式）→ 仍按 nth-of-type `[n]` 路径正确定位
  - （FindElement_old 已删除：若存在依赖该方法的自定义插件/外部调用，需改为调用 FindElement——架构上 provider 只暴露 gRPC，理论上无直接调用方）
- `573a1aa267` XPath/UiAutomator 选择器纯函数提取:
  - 元素 text/resource-id/description 值含 `"` 或 `'`（如文本 `"确定"`、`O'Brien`）→ XPath 查找仍能找到正确元素，不抛 XPath 解析错误
  - 元素 text/description 含反斜杠 `\` 且 pattern 为 regular/begin/end（走 `matches` 正则）→ 行为与重构前一致（已知边界，只求不回归）
  - UiAutomator 选择器：多属性定位、`id`+`class`、index 定位 → 常规元素查找定位到正确元素
  - 常规无特殊字符值 → 与重构前查找结果逐字一致
- `6cefa4760c` 错误处理统一:
  - 触发元素查找失败（元素不存在/超时）→ 抛出的 NotFoundException 带真实 Message 与 InnerException，日志/错误上报含原始异常堆栈
- `00d939af3b` CacheList 并发安全:
  - 并发执行元素查找（FindElement/CheckElement/相似查找）+ 页面切换/解析（ParseXml 的 Clear+Add）不抛 `Collection was modified during enumeration`，查找结果与串行一致
- `55ea2e2ab` Appium 进程管理:
  - 运行中杀掉 node.exe → 日志出现"异常退出，正在端口重启"，~1s 后 Appium 自动拉起，会话自动重连、流程能继续
  - 正常退出 provider → 只出现"已按计划关闭"，无"异常退出/重启"日志
  - 日志中出现 Appium 输出/错误输出行（证明 stdout/stderr 已被消费，不再有缓冲溢出）
- `623251629` P0 正确性修复:
  - 点击元素：元素禁用→可用切换时行为正确（无 1.5s 空转、无死循环）
  - 取元素文本 GetMobileElementText：返回正确文本；异常时日志组件类型为 GetText
  - 图像点击 GetImageRects：执行后保存目录无残留 Guid.png
  - （拨号相关按用户决定冻结，不验证）
- `d8d43286b` Similar/条件查找串行化:
  - 并发执行常规元素查找 + 相似元素查找不交错、不抛集合异常
- `6daa78f9d` 15 个高风险操作串行化:
  - 并发访问下 click/input/key/slide/剪贴板/app 激活/屏幕方向 行为一致
- 更早批次（PageSource 双端策略、连接/会话稳定、状态隔离、镜像）:
  - 页面切换、元素拾取/执行、PageSource 失败恢复、scrcpy 嵌入/缩放、长时间断开重连

## Project Context

- Repository root: `D:\Work-Dir\RPA\ii.rpa\src`
- Main projects:
  - `II.RPA.MobileDeviceManager`: desktop-side mobile screen rendering and capture/UI management.
  - `II.RPA.MobileProvider`: standalone mobile automation service that talks to Appium.
  - `II.RPA.MobileDeviceManager.GrpcService`: gRPC contracts between desktop manager and provider.
- The current rebased branch contains the complete mobile refactoring series; use the current hashes in the "Commit Map" section instead of pre-rebase hashes.

## Overall Goals

- Improve mobile connection stability and recovery.
- Reduce Appium/Node service crashes caused by frequent or heavy `PageSource` requests.
- Improve element lookup and capture stability.
- Unify duplicated element lookup and verification logic.
- Improve scrcpy window embedding stability.
- Improve code maintainability and reduce shared-state risks.

## Completed Changes

### 1. PageSource Refresh Strategy

- Replaced unconditional periodic refresh with event-driven dirty marking and active refresh windows.
- Added single-flight refresh control, short-interval force-refresh deduplication, and failure circuit breaking.
- `PageSourceRefreshService` now:
  - increments `_dirtyVersion` when marking dirty;
  - prevents stale refresh results from clearing newer dirty state;
  - returns `ShouldRefreshForCapture()` based on dirty state or active refresh window.
- Capture path now prefers the existing snapshot and only refreshes when no snapshot exists or the page is dirty.
- `DeviceScreenView` no longer forces a PageSource refresh at capture entry.
- `MobileSession` keeps its own PageSource TTL and only performs a controlled forced refresh/retry during element lookup when needed.

### 2. Connection and Session Stability

- Improved Appium/Node startup checks, readiness waits, and session creation/restart flow.
- Added session-level synchronization so the same device does not create or close sessions concurrently.
- `MobileSessionCache`:
  - resolves sessions under a per-`udid` gate;
  - uses direct concurrent dictionary access instead of linear enumeration;
  - has a per-session health-check cache to avoid probing Appium on every session access.
- `MobileSession.IsSessionValid(bool forceProbe = false)` caches the healthy state for 2 seconds by default.
- `ExecuteDriverCommand` serializes Appium commands using a normal reentrant `lock`, avoiding the previous `SemaphoreSlim + AsyncLocal` complexity.

### 3. State Isolation and Encapsulation

- `MobileSession.CacheList` is per-session instance state, not shared.
- `CVHelper.CVResults` is instance state.
- `MobileDeviceManager` public mutable fields were tightened:
  - `CacheList` is exposed as read-only;
  - `CurrentElement` has a private setter;
  - selector builder field was renamed and better encapsulated.
- `MobileElementRepository.Elements` exposes a read-only list.

### 4. Element Lookup Stability

- Element lookup now favors stable anchors:
  - `id` and `description` can act as target or ancestor anchors.
  - `text` can act as an ancestor context anchor but is not preferred as the final target-only anchor.
  - `index` is used as structural fallback, not as a strong unique locator.
- `FindElement` uses a snapshot-first strategy and retries once with a forced PageSource refresh on lookup failure.
- `CheckElement` now delegates its actual element lookup to `FindElement`.
- `BuildVerifiedSelector` is responsible only for marking `accurate` flags and producing the capture-time selector JSON.
- Old selector compatibility is retained and intentionally not being refactored at this time.

### 5. Screen Mirroring Stability

- Extracted mobile screen mirroring into `MobileScreenMirrorService`.
- Improved startup, embedding, parent window handling, and resize logic.
- Added retry and settle logic for parent handle and window size operations.

## Current State

- `II.RPA.MobileProvider` and `II.RPA.MobileDeviceManager` compile successfully on the current rebased .NET 8 branch.
- The latest mobile refactor commit is `b0fbb6539a`.
- Persisted mobile elements use the current `RPACommon` structure; obsolete `AndroidJsonItem` compatibility has been removed.
- Capture-time bare node arrays remain an internal protocol only. `CheckElement` wraps them and directly reuses `FindElementCore`; execution-time page/image validation is intentionally skipped because capture passes no validation options.
- Regular capture and similar-element capture now share stable-anchor selection, structural fallback, and the core element lookup implementation.
- Timeout-protected operations prevent stale automatic reconnects from publishing sessions after the caller has timed out.
- Appium process ownership is explicit: the Provider manages only the process it starts.
## Remaining / Next Work

Prioritized follow-up work:

1. Perform focused real-device regression for the newly completed stability batches:
   - repeated connect/disconnect/reconnect;
   - timeout during session health probing and automatic reconnect;
   - Appium crash/restart and Provider shutdown/restart;
   - ports 4723/4725 already occupied by unrelated processes.

2. Verify collection-style similar-element lookup (`FindSimilarElement`) after sharing XPath construction with regular lookup; keep its multi-result driver semantics separate.

3. Validate PageSource behavior on known slow or problematic pages:
   - single-flight and five-second background throttling;
   - circuit opening after repeated failures and recovery after the cool-down;
   - capture using the existing snapshot first and forcing one refresh only when necessary.

4. Run end-to-end validation for optional execution-time page and image checks. These validations run only when the client supplies non-null options and enables the corresponding flag; capture-time lookup must continue to bypass them.

5. Remove remaining unused state/dead code only after the above runtime behavior is verified, and add focused automated tests where dependencies can be isolated from a real Appium server.
## Key Files

- `II.RPA.MobileProvider/Provider/MobileSession.cs`
- `II.RPA.MobileProvider/Provider/MobileSessionCache.cs`
- `II.RPA.MobileProvider/Provider/MobileSessionFactory.cs`
- `II.RPA.MobileProvider/Service/MobileServer.cs`
- `II.RPA.MobileProvider/Service/MobileServerManager.cs`
- `II.RPA.MobileProvider/Service/MobileAbilityServer.cs`
- `II.RPA.MobileProvider/Service/MobilePluginGRPCService.cs`
- `II.RPA.MobileDeviceManager/MobileDeviceManager.cs`
- `II.RPA.MobileDeviceManager/Services/PageSourceRefreshService.cs`
- `II.RPA.MobileDeviceManager/Services/MobileElementRepository.cs`
- `II.RPA.MobileDeviceManager/Services/MobileScreenMirrorService.cs`
- `II.RPA.MobileDeviceManager/Views/DeviceScreenView.xaml.cs`
- `II.RPA.MobileDeviceManager/ViewModels/MainViewModel.cs`
- `II.RPA.MobileProvider/CV/CVHelper.cs`

## Design Principles

- Stability first; prefer small, incremental, low-risk changes.
- `MobileDeviceManager` remains a single-device capture/UI-management component.
- `MobileProvider` may hold multiple sessions, but one device should resolve to one valid session.
- Avoid unconditional PageSource refresh during capture or execution.
- Prefer stable anchors over weak locators.
- Do not restore obsolete persisted-selector compatibility. Current capture node arrays are a separate internal protocol and must remain supported.
- Lock at actual Appium command boundaries or outer operation boundaries, not around every high-frequency lookup method.
- Compile after each change and prefer small commits with clear scope.

## Verification

Build commands:

```powershell
dotnet build II.RPA.MobileProvider/II.RPA.MobileProvider.csproj -v:minimal /p:AllowUnsafeBlocks=true
dotnet build II.RPA.MobileDeviceManager/II.RPA.MobileDeviceManager.csproj -v:minimal /p:AllowUnsafeBlocks=true
```

Why `AllowUnsafeBlocks=true`: the build currently requires it because of existing unsafe code in `II.RPA.Plugin.Core`, unrelated to the mobile refactor.

Additional checks:

```powershell
git diff --check
```

Manual verification should include real-device connection/reconnection, page switching, element capture/execution, PageSource failure recovery, and scrcpy embedding/resizing.
## Architecture and Responsibilities

- `II.RPA.MobileDeviceManager`
  - Runs in the desktop client.
  - Renders the mobile screen through scrcpy into a WPF window.
  - Handles capture interactions, element highlighting, and desktop-side UI state.
  - Is effectively a single-device capture/UI manager; only one device is managed at a time during capture.

- `II.RPA.MobileProvider`
  - Runs as a separate mobile automation service.
  - Owns Appium driver sessions and mobile operation commands.
  - Can hold multiple device sessions, but the same `udid` should resolve to one valid `MobileSession`.

- `II.RPA.MobileDeviceManager.GrpcService`
  - Defines the gRPC contract between desktop manager and mobile provider.

## Important Runtime Model

- The mobile screen is rendered with `scrcpy + Appium`, not by sending every desktop click through `MobileDeviceManager.Click/Swipe/PressKey`.
- Mouse clicks and input on the rendered screen are synchronized to the phone through the scrcpy input path.
- During capture, normal clicks still work; when capture mode is active, the capture click is intercepted and should not be forwarded as a real phone click.
- `IsCaptureMode` and `captureModeCount` are used to guard capture mode state. `captureModeCount` is changed with `Interlocked` operations so capture state remains thread-safe even though only one capture is expected at a time.

## Current Key Flows

### PageSource Refresh Flow

- Device-side:
  - Page-changing operations or active refresh windows call `MarkPageSourceDirty`.
  - `RequestPageSourceRefresh(force, reason)` calls `PageSourceRefreshService.TryRefresh`.
  - `TryRefresh` applies deduplication, single-flight, circuit breaking, and dirty-version checks.
  - On success, `UpdateMobileRotation` updates rotation and `MobileElementRepository.ReplaceXmlSnapshot` replaces the cached XML.

- Provider-side:
  - `MobileSession.RefreshPageSource` has its own TTL and `_pageSourceVersion`.
  - `FindElement` first reuses the current snapshot; it forces a refresh and retries only when necessary.
  - `ParseXml` rebuilds `CacheList` only when the PageSource version changes.

### Capture / Element Pick Flow

1. Client calls `CaptureMobile`.
2. `MobileDeviceManager.Instance` sets capture callbacks and enters capture mode.
3. User clicks a point on the rendered phone screen.
4. `ClickElementWithPoint` decides whether to refresh before capture:
   - refresh if there is no snapshot or the page is marked dirty;
   - otherwise reuse the current snapshot first.
5. `GetSelectedElementFromPoint` finds the desktop-side element.
6. If lookup fails and no refresh happened, retry once after a forced refresh.
7. The selected element is sent to the provider through `MobileCheckElementUnaryAsync`.
8. Provider-side `CheckElement` wraps the capture node array, uses `FindElementCore` for lookup, and uses `BuildVerifiedSelector` for the final selector JSON.
9. Desktop side stores `BuildSelectorJson`, crops the element image, and returns the selector.

### Session Resolution Flow

- `MobileSessionCache.WithSessionLockAsync` serializes higher-level connection/session operations by `udid`.
- `MobileSessionCache.GetValidSession` resolves under a per-`udid` gate.
- `IsSessionValid` caches a healthy state for 2 seconds by default to reduce Appium probing.
- If invalid and `autoReConnect` is true, a new session is created only for the operation generation captured before execution; timeout invalidation prevents stale publication.

## Confirmed Decisions and Constraints

- Do not upgrade Appium to 2.x at this time.
- Obsolete persisted-selector compatibility has been removed. Do not conflate it with the current capture-time bare node-array protocol.
- Keep `MobileDeviceManager` as a single-device capture/UI manager.
- Keep `MobileProvider` as a multi-session service with per-device session isolation.
- Avoid unconditional PageSource refresh during normal capture/execution.
- Prefer stable anchors:
  - `id` and `description` can be target or ancestor anchors.
  - `text` may be used as an ancestor context anchor, but not as the preferred final target-only anchor.
  - `class + index` is structural fallback, not a strong unique locator.
- Do not wrap `FindElement` in a broad command lock; operations are already serial in normal use.
- `ExecuteDriverCommand` is synchronous and uses a normal reentrant `lock`, not `SemaphoreSlim + AsyncLocal`.
- Keep changes incremental and compile after each step.

## Known Pitfalls

- Some pages produce very large or slow PageSource XML. Avoid polling PageSource too frequently.
- A failed PageSource operation can be expensive or can affect the Appium/Node process, so circuit breaking and deduplication are important.
- The desktop manager and provider both have PageSource/snapshot concepts. Do not add another hard refresh path without checking both sides.
- `BuildVerifiedSelector` contains capture-specific `accurate` flag logic. Regular and similar capture share it, but collection-style similar lookup still has distinct multi-result semantics.
- `MobileSessionCache.session_Lock` is currently present but not used by the active implementation; avoid assuming it protects session resolution.
- Normal `dotnet build` for MobileProvider may fail on existing unsafe code in `II.RPA.Plugin.Core`; use `AllowUnsafeBlocks=true` for verification.

## Commit Map

Recent commits after the .NET 8 rebase:

- `b0fbb6539a` - share structural/predicate XPath construction between regular and similar lookup.
- `566aa54efe` - unify similar-element capture selector construction and lookup.
- `1daf006e24` - manage only the Provider-owned Appium process and select ports without killing unknown processes.
- `77e6439c77` - prevent timed-out automatic reconnects from publishing stale sessions.
- `7137512113` - preserve capture-time node-array lookup after legacy selector cleanup.
- `47a7090fbe` - remove obsolete persisted Android selector compatibility.
- `07c2882b54` - add timeout boundaries to management operations.
- `c2c76361b9` - isolate timed-out sessions, add connection generations, and protect PageSource calls.
- `e8f0b8026c` - strengthen Appium restart recovery and PageSource/lookup stability.
- `ea3a32beca` - align regular/similar selector builders and remove duplicated lookup code.
- `573a1aa267` - extract selector escaping and XPath/UiAutomator builder helpers.
- `6cefa4760c` - preserve exception stacks and root causes.
- `00d939af3b` - protect provider-side element cache reads/writes.
- `55ea2e2ab` - consume Appium output and add abnormal-exit restart handling.
- `623251629` - fix the first global-analysis P0 correctness defects.
- `d8d43286b` - serialize the remaining high-risk similar/condition driver calls.
- `c1f61c9f1` - serialize high-risk driver operations at command boundaries.
- `b440f0081` - simplify `ExecuteDriverCommand` to a synchronous reentrant lock.
- `29a11c0cc` - optimize capture-time PageSource refresh strategy.
- `0945f9024` - encapsulate PageSource refresh, lookup state, and capture state.
- `a7d8618fc` - connection stability refactor across server/factory/cache/gRPC.
- `5b12445d9` - PageSource single-flight/circuit-breaker/dirty marking and scrcpy embedding stability.
- `258f9a4c6` - first mobile refactor batch.

## How to Continue

1. Begin with the deferred real-device verification list rather than changing the core lookup path again.
2. Reproduce timeout during health probing/reconnect and confirm no new session is published after the caller receives a timeout.
3. Verify Appium startup when 4723 is occupied by an unrelated service, abnormal Appium exit/restart, and Provider shutdown cleanup.
4. Verify regular capture, two-step similar capture, regular execution lookup, and similar multi-result lookup on the same device/pages.
5. Exercise slow PageSource pages until the failure threshold is reached, then verify circuit cool-down and recovery.
6. After runtime verification, continue only with low-risk selector parsing/dead-state cleanup as separate commits.
