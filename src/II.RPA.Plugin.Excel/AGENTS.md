# II.RPA.Plugin.Excel 架构说明

本文档用于给后续 AI 或开发者快速理解 `II.RPA.Plugin.Excel` 的整体设计、模块边界和常见改动入口。当前项目是一个 .NET Framework 4.8 的 RPA Excel 插件库，输出 `II.RPA.Plugin.Excel.dll`，由插件加载器通过 `Config/plugin.json` 和 `Config/config.json` 识别并调用。

## 项目定位

`II.RPA.Plugin.Excel` 提供 Excel/WPS 文件操作能力，支持两类执行模式：

- 使用软件模式：通过 Microsoft Excel 或 WPS 的 COM 自动化操作真实桌面应用，入口实现是 `Actions/Software/SoftwareExcel`。
- 不使用软件模式：通过 DevExpress Spreadsheet 在后台直接读写工作簿文件，入口实现是 `Actions/Non-Software/NonSoftware`。

两类模式共同实现 `IExcel` 接口。运行时由 `ExcelPlugin` 根据动作名、参数和全局配置选择具体实例，并通过反射调用 `IExcel` 中同名方法。

## 核心执行链路

主要调用链：

1. 插件加载器调用 `Plugins/Run.cs` 中的 `Run : ComponentBase<ParamRunModel>`。
2. `Run.Excute` 将外部 JSON 参数反序列化为 `ActionContext`：
   - `ActionName` 来自 `ParamRunModel.Method`。
   - `ActionParams` 来自 `ParamRunModel.Paras`。
   - `SingleRun` 用于单次运行场景。
3. `ExcelPlugin.Instance.ExecuteWithTimeout(context)` 执行统一流程：
   - 执行 `BeforeExecuteFilter`，补齐参数、选择 Excel 实例、设置超时等。
   - 按 `context.ActionName` 在 `IExcel` 上查找同名方法。
   - 使用 `ExcelUtils.ConvertDynamicToDefaultValue` 将动态参数转成目标方法参数类型。
   - 反射调用 `context.ExcelInstance` 上的实现方法。
   - 执行 `ExecutedFilter`，保存实例缓存、自动保存等。
   - 出错时执行 `ExceptionFilter`，统一包装异常。
4. 结果写入 `ActionContext.Result`，通常返回 JSON 字符串。少量动作通过 `Raw_String` 返回原始字符串。

关键入口文件：

- `Plugins/Run.cs`：通用 Excel 动作执行入口。
- `Excel.cs`：`ExcelPlugin` 单例、实例缓存、过滤器注册、反射调用、超时执行。
- `IExcel.cs`：所有可执行 Excel 动作的统一契约。
- `Models/ActionContext.cs`：一次组件执行的上下文、参数、结果和异常容器。

## 目录职责

### 根目录

- `II.RPA.Plugin.Excel.csproj`：C# 项目文件，目标框架为 .NET Framework 4.8，输出类型为类库。包含 WPF 页面、资源、配置复制和 AfterBuild 部署目标。
- `II.RPA.Plugin.Excel.sln`：解决方案文件。
- `Excel.cs`：插件运行时核心。维护 `ExcelSet` 工作簿到 `IExcel` 实例的映射、`TimeoutCache` 超时恢复缓存、过滤器链和反射执行逻辑。
- `IExcel.cs`：动作接口。新增普通 Excel 动作通常必须先在这里声明方法。
- `ExcelException.cs`：插件内部业务异常类型，异常过滤器会将底层异常包装成该类型。
- `app.config`、`packages.config`：运行时绑定和 NuGet 包配置。
- `AGENTS.md`：面向 AI/开发者的项目说明文件。

### `Plugins/`

插件组件对外入口，继承 `II.RPA.Plugin.Core.ComponentBase<T>`。

- `Run.cs`：最重要的入口。执行绝大多数 Excel 动作，参数模型是 `ParamRunModel`。
- `GetExcelRule.cs`：获取 Excel 规则/设计器可视化配置。部分场景会打开临时副本并使用 `SoftwareExcel` 获取规则。
- `GetSystemFont.cs`：返回系统字体列表，用于格式相关 UI。
- `SaveInstance.cs`：流程结束或超时场景下保存/关闭已打开实例，并将 `TimeoutCache` 写入 `excel_timeout.cache`。

### `Params/`

插件入口参数模型和复杂动作参数模型。

- `ParamRunModel.cs`：通用执行参数，包含 `Method`、`Paras`、`SingleRun`。
- `ParamRuleModel.cs`：获取 Excel 规则相关参数。
- `ParamSaveInstance.cs`、`ParamGetSystemFont.cs`：对应插件入口参数。
- `CellFormatParams.cs`：单元格格式 JSON 的强类型模型，包含边框、字体、对齐、数字格式枚举等。
- `ColumnSplitParam.cs`：分列动作参数模型。

### `Config/`

插件元数据和运行配置。

- `plugin.json`：插件包元信息，包括名称、版本、依赖、说明等。
- `config.json`：全局配置和每个 Action 的名称、超时、延时等设置。注意该文件包含注释，按普通严格 JSON 解析工具处理时可能失败，但项目配置体系可读取。
- `ExcelConfigurations.cs`：配置模型，继承 `ConfigurationBase<ExcelConfigurations>`。提供：
  - `GlobalSetting`：全局超时、批大小、WPS ProgID、是否保存实例、打开模式等。
  - `ActionSetting`：按动作名配置名称、超时、延时等。
  - `GetActionName`：将 `ReadRangeV2` 这类版本化动作回退到基础动作配置。
  - `GetSetting<T>`：读取动作配置。

### `Actions/Software/`

使用 Excel/WPS 软件自动化的实现，核心类为 `SoftwareExcel : IExcel`，用 partial class 分文件组织。

整体特点：

- 通过 COM 自动化访问 Microsoft Excel 或 WPS。
- 支持附加到已打开进程、获取前台活动工作簿、最大化窗口、执行宏、透视表、筛选、图片导出等更依赖桌面软件的能力。
- 维护 `Workbooks`、`Applications`、`ExcelApplication`、`WpsApplication`、`RootApplication` 等 COM 对象缓存。
- 使用 `ComScope` 和显式 `Marshal.ReleaseComObject/FinalReleaseComObject` 管理 COM 生命周期。
- 对用户手动关闭工作簿有 `BeforeClose` 事件绑定，关闭时从缓存中移除实例。

主要文件：

- `SoftwareExcel.cs`：应用级对象、COM 应用创建、附加到运行中 Excel/WPS、获取工作簿/工作表、释放 COM 引用。
- `Workbook.cs`：新建、打开、获取已打开、保存、另存、关闭、导出 PDF、加密、获取路径等工作簿级动作。
- `Worksheet.cs`：插入、获取名称、重命名、复制、合并、激活、删除、移动 Sheet。
- `SheetInfo.cs`：读取单元格、区域、行列数、已编辑区域、空白单元格等信息类动作。
- `SheetEdit.cs`：写入、删除、插入、区域操作、格式、筛选、排序、透视表、查找替换、合并单元格等编辑类动作。
- `SoftwareExcel.ExportRangeAsPicture.cs`：导出区域为图片。
- `ExcelObject.cs`、`ExcelExtension.cs`：COM 属性/方法访问、辅助扩展、规则读取等能力。
- `ExcelWindow.cs`：通过窗口句柄查找 Excel 子窗口和 COM 对象。
- `ComScope.cs`：局部 COM 对象跟踪与释放工具。

### `Actions/Non-Software/`

不使用 Excel/WPS 软件的后台文件实现，核心类为 `NonSoftware : IExcel`，基于 DevExpress Spreadsheet。

整体特点：

- 通过 `DevExpress.Spreadsheet.Workbook` 读写文件，无需启动 Excel/WPS。
- 更适合后台批量读写、普通 Sheet 操作和格式处理。
- 某些依赖桌面应用的能力不可用或行为受限，例如获取激活 Excel、部分粘贴/宏/可视化规则能力。
- 打开文件时可能创建 `sz~!_` 前缀临时副本，保存时再覆盖回原文件。

主要文件：

- `NonSoftwareExcel.cs`：后台实现核心、工作簿缓存、范围获取、批量读取序列化、临时副本、DevExpress 粘贴模式转换。
- `Workbook.cs`：新建、打开、保存、另存、关闭、导出 PDF、加密、获取路径。
- `Worksheet.cs`：Sheet 插入、获取名称、重命名、复制、合并、激活、删除、移动。
- `SheetInfo.cs`：读取单元格/区域/行列/已编辑范围等。
- `SheetEdit.cs`：写入、删除、插入、区域处理、格式、筛选排序等。

### `Filter/`

统一执行管线过滤器。所有过滤器继承 `ActionFilter`，通过 `FilterType` 区分执行阶段。`ExcelPlugin.RegisterFilters()` 注册顺序中 `Order` 越大越先执行。

通用基类：

- `ActionFilter.cs`：过滤器基类，包含 `FilterType`、`Order`、`CancelAction` 和 `OnExecution`。
- `FilterType.cs`：过滤器阶段枚举。

`Filter/Executing/`：执行前过滤器。

- `InsertActionFilter.cs`：插入行/列等旧新版动作参数兼容处理。
- `MacroActionFilter.cs`：宏执行相关参数/脚本处理。
- `NegativeColumnRowFilter.cs`：行列参数负数或边界处理。
- `WriteContentFilter.cs`、`ReadContentFilter.cs`：读写内容类动作重定向或批量内容处理。
- `TimoutFilter.cs`：读取动作/全局超时与执行前延时配置。
- `ExcelInstanceFilter.cs`：选择 `SoftwareExcel` 或 `NonSoftware` 实例。`New/Open` 根据 `visible` 或全局 `OpenMode` 决定，普通动作从 `ExcelPlugin.ExcelSet` 获取已打开实例。
- `ActionParamsFilter.cs`：参数标准化、动作重定向标记、共享路径和复制区域标记。
- `DisposeFilter.cs`：释放/GC 相关处理。
- `SaveInstanceFilter.cs`：维护超时恢复所需实例信息。
- `CloseFilter.cs`：关闭动作前置处理。

`Filter/Executed/`：执行后过滤器。

- `AddinFilter.cs`：Excel Addin/规则交互相关后处理。
- `SaveActionFilter.cs`：当参数 `save` 为 `是` 时自动保存当前或目标工作簿。
- `SaveWorkbookFilter.cs`：动作结果包含 `workbook` 时，将工作簿路径写入 `ExcelPlugin.ExcelSet`。
- `TimeoutFilter.cs`：执行后超时缓存/恢复相关处理。

`Filter/Exception/`：

- `ExceptionFilter.cs`：记录异常，将 COM/HRESULT 等底层异常包装为更适合用户展示的 `ExcelException`。

### `Models/`

运行时数据结构和动作规则模型。

- `ActionContext.cs`：执行上下文。通过索引器访问 `ActionParams`，通过 `AddResult` 写结果。内部的 `ExcelEditRange` 会延迟调用 `GetEditRange` 获取当前已编辑范围。
- `ExcelRange.cs`：Excel 规则/区域模型。
- `FilterRule.cs`：筛选规则模型。
- `PivotTableRule.cs`：数据透视表规则模型。
- `SortRule.cs`：排序规则模型。
- `CellFormat.cs`：单元格格式内部模型。
- `RangeMode.cs`：区域模式枚举。
- `SerializeData.cs`：序列化数据包装模型。

### `Forms/`

WPF 可视化规则配置界面和资源。

- `ExcelForm.xaml`、`ExcelForm.xaml.cs`：Excel 规则选择/可视化配置窗口。
- `ExcelFormContext.cs`：窗口上下文。
- `Range.png`、`PivotTable.png`、`CellFormat.png`、`Sort.png`、`default.png`：表单显示资源，作为 WPF Resource 编译进程序集。

### `Utils/`

工具类集合。

- `ExcelUtils.cs`：最核心工具类。包含路径处理、列号/列名转换、动态参数转换、范围解析、数据序列化、读写批处理、软件类型识别、临时目录等大量通用逻辑。
- `NativeUtils.cs`：Win32 P/Invoke、窗口、进程、输入、屏幕和系统 API 定义。
- `ProcessUtils.cs`：进程父子关系辅助。
- `HookManager.cs`：鼠标/键盘钩子辅助。
- `StringCipher.cs`：字符串加解密工具。

### `Addin/`

- `MacroArgs.cs`：宏执行或 Addin 交互时使用的参数模型。

### `VBScript/`

- `宏1.bas`：随插件复制到输出目录的宏脚本资源。项目文件中配置为 `CopyToOutputDirectory=Always`，AfterBuild 部署时会复制到插件运行时的 `vb/` 目录。

### `Properties/`

- `AssemblyInfo.cs`：程序集元数据。

### `bin/` 和 `obj/`

构建输出和中间文件，不应作为源码架构修改入口。AI 开发时通常不要编辑这两个目录。

### `.agents/`

Codex/AI 工作区内部目录，不属于插件业务源码。

## 实例与缓存设计

`ExcelPlugin` 维护两个重要缓存：

- `ExcelSet: Dictionary<string, IExcel>`：工作簿完整路径到执行实例的映射。打开或新建成功后，`SaveWorkbookFilter` 根据结果中的 `workbook` 写入缓存。后续动作通过 `workbook` 参数找到同一个 `IExcel` 实例。
- `TimeoutCache: Dictionary<string, object>`：用于超时或流程恢复时保存已打开实例参数。`SaveInstance` 会将其写入 `excel_timeout.cache`。

`SoftwareExcel` 另有：

- `Workbooks: Dictionary<string, dynamic>`：工作簿路径到 COM Workbook 的映射。
- `Applications: Dictionary<int, dynamic>`：进程 ID 到 COM Application 的映射。
- `ExcelApplication/WpsApplication/RootApplication`：当前 Excel/WPS 应用对象。

`NonSoftware` 另有：

- `Workbooks: Dictionary<string, Workbook>`：工作簿路径到 DevExpress Workbook 的映射。
- `Encryptions` 和 `EncryptedWorkbook`：加密/另存相关状态。

## 配置与构建

项目构建要点：

- 目标框架：`.NET Framework 4.8`。
- C# 语言版本：`7.3`。
- 输出：`bin/II.RPA.Plugin.Excel/II.RPA.Plugin.Excel.dll`。
- 主要依赖：
  - `II.RPA.Plugin.Core`
  - `II.RPA.Core.Provider`
  - `II.RPA.Logging`
  - `II.RPA.WinAutomation.CommonUtility`
  - DevExpress 22.1 Spreadsheet/Office/Printing 等组件
  - Newtonsoft.Json
  - ExcelDataReader
- `Config/config.json`、`Config/plugin.json` 会复制到输出目录。
- `VBScript/宏1.bas` 会复制到输出目录。

`II.RPA.Plugin.Excel.csproj` 中的 `DeployExcelPlugin` AfterBuild 目标会将主程序集、配置和 VBScript 复制到：

```text
$(SolutionDir)..\bin\II.RPA.PluginLoader\runtime\Excel\
```

其中 VBScript 会进入：

```text
$(SolutionDir)..\bin\II.RPA.PluginLoader\runtime\Excel\vb\
```

## 新增或修改 Action 的常见步骤

新增一个普通 Excel 动作时，通常按以下顺序修改：

1. 在 `IExcel.cs` 增加方法声明。方法名必须与 `ParamRunModel.Method` / `config.json` 中的 Action 名一致，因为 `ExcelPlugin.Execute` 通过反射按名称查找。
2. 在 `Actions/Software/` 对应 partial class 中实现 `void IExcel.Xxx(...)`，如果动作支持使用软件模式。
3. 在 `Actions/Non-Software/` 对应 partial class 中实现同名接口方法，或明确抛出 `ExcelException` 说明不支持不使用软件模式。
4. 在 `Config/config.json` 的 `ActionSetting` 增加动作配置，包括 `name`、`ExecuteTimeout`、必要的 `DelayBefore`。
5. 如果动作需要预处理参数、兼容旧参数、重定向、自动保存、特殊异常处理，则增加或修改 `Filter/Executing` 或 `Filter/Executed` 下的过滤器，并在 `ExcelPlugin.RegisterFilters()` 注册。
6. 如果动作需要复杂 JSON 参数，优先在 `Params/` 或 `Models/` 增加强类型模型，再由实现类中反序列化。
7. 如果动作返回结果，统一写入 `ActionContext.AddResult(...)`。默认返回 JSON；若必须返回非 JSON 原始字符串，需要在 `Excel.cs` 的 `RawStringMethod` 中加入动作名，并写入 `Raw_String`。
8. 如果动作打开或生成新工作簿，结果中应包含 `workbook`，以便 `SaveWorkbookFilter` 写入 `ExcelPlugin.ExcelSet`。

修改已有动作时重点检查：

- `IExcel.cs` 方法签名是否与外部传参名称一致。
- `ExcelUtils.ConvertDynamicToDefaultValue` 是否能正确转换新增参数类型。
- `SoftwareExcel` 和 `NonSoftware` 两种模式是否都需要同步修改。
- `config.json` 中基础动作和 `V2/V3` 动作的配置回退是否符合预期。
- 是否受 `save=是` 自动保存逻辑影响。
- 是否需要处理共享路径 `__IsShareFile`。
- 是否会破坏 `ExcelSet`、`Workbooks`、COM 释放或临时副本清理。

## 关键开发约束

- 不要直接绕过 `ExcelPlugin.ExecuteWithTimeout` 调用动作，否则会跳过参数过滤、实例选择、超时、自动保存和异常包装。
- 不要随意改变 `IExcel` 方法名。动作分发依赖方法名反射，改名会导致运行时“调用的方法不存在”。
- `ActionParams` 是动态字典，新增参数名必须与 `IExcel` 方法参数名一致。特殊参数名 `_override` 会从外部 `override` 映射。
- `Open/New/Load` 是实例建立动作，普通动作必须传入已缓存的 `workbook`。
- 使用软件模式处理 COM 对象时，应使用 `ComScope` 或现有释放方法，避免泄漏或释放正在缓存的 Workbook。
- 不使用软件模式下，DevExpress 与真实 Excel/WPS 的行为不完全一致；涉及宏、活动窗口、部分复制粘贴、加载已打开文件、可视化规则时优先检查 `SoftwareExcel` 实现。
- 文件路径通常要通过 `ExcelUtils.GetAbsolutePath` 或 `Path.GetFullPath` 标准化，除非 `__IsShareFile` 为真。
- 大范围读取使用 `ExcelConfigurations.Info.GlobalSetting.BatchSize` 分批，并通过 `ExcelUtils.SerializerDataToBinaryFile` 输出中间数据。
- `bin/`、`obj/` 是产物目录，不要手工维护。

## 常见排查入口

- 动作没有被调用：检查 `IExcel.cs` 是否有同名方法、`ParamRunModel.Method` 是否一致、`config.json` 是否配置。
- 找不到工作簿：检查打开动作是否返回 `workbook`、`SaveWorkbookFilter` 是否写入 `ExcelSet`、路径是否被标准化。
- 使用软件/不使用软件模式不符合预期：检查 `ExcelInstanceFilter`、`visible` 参数、`GlobalSetting.OpenMode`。
- 执行超时：检查 `config.json` 中 Action 的 `ExecuteTimeout` 和 `GlobalSetting.ExecuteTimeout`，同时排查 Excel/WPS 是否有弹窗或保护状态。
- 自动保存异常：检查参数 `save`、`SaveActionFilter`、目标工作簿 `destWorkbook`。
- COM 异常或 Excel 进程残留：检查 `SoftwareExcel` 的 `ComScope` 使用、`Close` 逻辑、`ReleaseAllComReferences` 和 `BeforeClose` 绑定。
- 后台模式保存后原文件没有更新：检查是否打开了 `sz~!_` 临时副本，以及 `NonSoftware.Save/Close` 的回写和清理逻辑。
