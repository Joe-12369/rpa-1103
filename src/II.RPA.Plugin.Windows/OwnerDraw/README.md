# OwnerDraw adapter layer

This directory contains the framework-neutral managed layer for PowerBuilder
DataWindow and VCL DBGrid virtual cells. It is intentionally independent from
`UIANode` and `IGuiNode`; the existing Windows plugin only needs a thin virtual
node wrapper and two calls into `OwnerDrawAdapterRegistry`.

## Integration contract

- Interactive pick: create the normal UIA/MSAA node first, call
  `OwnerDrawAdapterRegistry.Shared.TryPick`, and replace the normal node only
  when the result is `Success`. `NotApplicable`, failures and exceptions retain
  the original node.
- Selector replay: decode the final virtual leaf and find the host HWND with the
  original Windows selector. Then call `DecodeAndResolve`. Once an owner-draw
  marker is recognized, every failure is final; never return the host Pane as a
  successful match.
- Actions: call `GetText` or `GetBounds` immediately before the action. Clicks
  use the newly resolved rectangle, not pick-time geometry.

`OwnerDrawSelectorCodec.Encode` returns neutral property records. The node
wrapper maps each record to a `WinSelectorItemProperty`, preserving its
`Accurate` flag. Cell text is diagnostic (`Accurate=false`) unless a user
explicitly promotes it to an exact guard.

## Protocol coverage

PowerBuilder (`pbdwNN`, MfcGdiAccBridge ABI 5):

- `REFRESH`
- `GET_TEXT_AT`
- `RESOLVE_CELL index=N`
- visual-index selector, current text and current screen rectangle

VCL logical DBGrid (VclGridAccBridge ABI 8):

- `GET_CELL semantic=1`
- `RESOLVE_CELL row=N col=N field64=...`
- `READ_CELL`
- `GET_GRID_INFO`
- logical row/column/field selector, current text and current screen rectangle

The VCL adapter accepts a picked/resolved cell only when the bridge returns
`semantic=1`, valid `logicalRow/logicalCol`, a cell model and
`coord=physical-screen-v2`. It never turns viewport row/column into a persistent
selector.

## Lifecycle and architecture

Bridge files are resolved below the managed plugin's `OwnerDraw` directory:

- `MfcGdiAccBridge.x86.dll` / `MfcGdiAccBridge.x64.dll`
- `VclGridAccBridge.x86.dll` / `VclGridAccBridge.x64.dll`
- `OwnerDrawInjectorBroker.x86.exe` (64-bit package only)

Same-bitness targets are injected directly. The only cross-bitness route is a
64-bit RPA controller operating an x86 PowerBuilder target: the controller
starts the signed x86 broker for the bounded injection/lifecycle operation,
then communicates with `MfcGdiAccBridge.x86.dll` over the existing named-pipe
protocol. `StartWork` receives the long-lived RPA controller PID, not the
short-lived broker PID. This keeps pipe authentication valid after the broker
exits. x64-to-x86 VCL and every x86-to-x64 route remain rejected.

The supported matrix is therefore:

| RPA | Target | PowerBuilder | VCL |
| --- | --- | --- | --- |
| x86 | x86 | direct | direct |
| x64 | x64 | direct | direct |
| x64 | x86 | x86 broker | rejected |
| x86 | x64 | rejected | rejected |

An x86 product package keeps the original x86 PB/VCL Bridges and the pinned
Delphi helper dependencies. An x64 package contains the x64 PB/VCL Bridges plus
only the x86 PB Bridge and x86 broker needed for the supported cross-bitness
route; it does not contain the x86 VCL Bridge, `DelphiRttiBridge.dll`, or
`rtl160.bpl`.

Cached sessions include adapter, PID, target architecture and process start
time to prevent PID reuse. A protocol failure invalidates the cache, calls
`StartWork` again and retries once. PING is checked against the expected target
architecture so a stale/wrong-architecture pipe cannot be accepted. Stop and
start/resume share the manager lifecycle lock. Stop invokes `EndWork` but never
`FreeLibrary`; replacing a resident bridge requires restarting the target.

PowerBuilder activation also validates `BridgeBuildId` from the mapped image,
in addition to the packaged/resident file hash and ABI. This covers the case
where an installer replaces a DLL at the same path while an older image remains
mapped: hashing `module.FileName` would only see the new disk file. A missing or
different in-memory BuildId fails closed before `StartWork`/`EndWork` and asks
for a target restart; neither the direct route nor the broker ever calls
`FreeLibrary`. If the same verified build is already resident, `StartWork` is
called again to transfer pipe ownership to the current RPA controller. If that
controller exits without an explicit Stop, the PB Bridge watchdog reversibly
stops capture and pipe work but leaves the DLL resident for a later verified
resume.

`BridgeBuildId` is a release identity, not a protocol version. Every production
change to the PB Bridge binary, hooks, pipe behaviour, or lifecycle handling
must increment it in both `MfcGdiAccBridge.cpp` and
`NativeOwnerDrawBridgeActivator.cs`; changing only one side intentionally makes
activation fail closed. A graceful product shutdown may call `Stop`, but process
exit is also covered by the watchdog. Neither path hot-unloads the target DLL.

The product also checks for the architecture-neutral module names used by the
WeChatPicker validation build (`MfcGdiAccBridge.dll` and
`VclGridAccBridge.dll`). If either matching legacy module is already resident,
start, stop and status fail closed with `LegacyConflict`: the product does not
load a second Bridge or call an unknown legacy ABI. Restart the target process
before using the architecture-suffixed product Bridge. If `StartWork` succeeds
but the subsequent PING fails, the manager makes one best-effort resident
`EndWork` rollback and never creates a managed session.

Product lifecycle calls are available through `OwnerDrawBridgeControl`:

- `Resume(hwnd)`
- `Stop(hwnd)`
- `GetStatus(hwnd)`

## Kill switches

All switches default to enabled. Set a value to `false`, `0`, `off` or `no` and
restart the RPA plugin process:

- `II_RPA_OWNER_DRAW_ENABLED`
- `II_RPA_OWNER_DRAW_PB_ENABLED`
- `II_RPA_OWNER_DRAW_VCL_ENABLED`

When disabled, interactive pick is `NotApplicable` (normal UIA/MSAA remains),
while explicit owner-draw selector replay fails closed.

## Identity boundary

The PowerBuilder selector is the visual index implemented by the validated GDI
bridge. It supports pick/replay/text/click for the current captured layout, but
sorting, filtering or inserting rows can assign the index to another cell. Text
is not made exact by default so a legitimate value change does not make
`GetText` fail.

The VCL selector is an authoritative logical row/column identity, with an
optional field guard. On x86 it may be backed by the pinned Delphi helper. On
x64 it is available only for targets where the Bridge can prove a logical cell
from the guarded visible-header/record-scroll model. This is not a claim of
complete DataSet metadata or off-screen table access, and no managed viewport
fallback is permitted.
