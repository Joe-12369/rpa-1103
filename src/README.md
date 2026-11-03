# src AssemblyInfo 动态注入说明

## 功能

当前版本只处理 `Properties/AssemblyInfo.cs`。

规则很简单：

1. `Branding.ManagedProjects.props` 里 `BrandingEnabled=true` 时才启用动态处理
2. 字段默认全部列出，但只有对应的 `*Enabled=true` 才参与覆盖
3. 字段启用后，有值就覆盖该值
4. 字段启用后，值为空就覆盖为空字符串
5. 字段未启用时，继续使用项目原始 `AssemblyInfo.cs` 中的值

## 相关文件

1. `build.bat`
   构建入口，只负责调用 MSBuild。
2. `Branding.ManagedProjects.props`
   动态处理总开关、匹配规则、字段覆盖配置。
3. `Directory.Build.targets`
   编译期入口，判断项目是否参与动态注入。
4. `Build.Branding.TransformAssemblyInfo.ps1`
   清洗原始 `AssemblyInfo.cs` 中被接管的字段。

## 配置方式

### 1. 决定哪些项目参与

在 `Branding.ManagedProjects.props` 中：

1. `BrandingProjectPattern`
   命中这些规则的项目参与动态处理。

这个值按前缀分组整理，便于阅读和维护。

`BrandingEnabled=true` 时，只会对命中 `BrandingProjectPattern` 的项目执行动态处理；没命中的项目保持原来的构建行为，不需要额外维护排除列表。

### 2. 配置需要覆盖的字段

字段默认全部列在 `Branding.ManagedProjects.props` 中：

1. `BrandingAssemblyCompany`
2. `BrandingAssemblyCopyright`
3. `BrandingAssemblyVersion`
4. `BrandingAssemblyFileVersion`
5. `BrandingAssemblyInformationalVersion`

每个字段都配有对应的启用开关：

1. `BrandingAssemblyCompanyEnabled`
2. `BrandingAssemblyCopyrightEnabled`
3. `BrandingAssemblyVersionEnabled`
4. `BrandingAssemblyFileVersionEnabled`
5. `BrandingAssemblyInformationalVersionEnabled`

字段行为：

1. `*Enabled=false`：该字段不参与覆盖，保留原值
2. `*Enabled=true` 且字段有值：构建时覆盖原值
3. `*Enabled=true` 且字段为空：构建时覆盖为空字符串

例如：

```xml
<BrandingAssemblyCompanyEnabled>true</BrandingAssemblyCompanyEnabled>
<BrandingAssemblyCompany>浙江实在智能科技有限公司</BrandingAssemblyCompany>

<BrandingAssemblyInformationalVersionEnabled>true</BrandingAssemblyInformationalVersionEnabled>
<BrandingAssemblyInformationalVersion></BrandingAssemblyInformationalVersion>
```

## 编译期执行过程

执行顺序如下：

1. 读取项目原始 `Properties/AssemblyInfo.cs`
2. 只移除那些 `*Enabled=true` 的属性
3. 保留其余原始字段不动
4. 生成新的 `ManagedAssemblyInfo.g.cs`
5. 编译时同时使用：
   保留原值的副本
   新生成的覆盖字段文件

这样可以保证：

1. 开启了的字段由 props 接管
2. 没开启的字段继续走原文件值

## build.bat 说明

`build.bat` 不再传入 `BrandingEnabled` 或任何 Assembly 字段值。

构建控制台会输出两类 Branding 日志：

1. 每个实际生效的项目都会输出一条 `[Branding] Apply <ProjectName> fields: ...`
2. 所有解决方案构建结束后，会输出已生效项目列表和总数

MSBuild 的选择规则：

1. 优先使用环境变量 `MSBUILD`
2. 如果 `MSBUILD` 没设置，则回退到 `MSBuild.exe`

是否启用动态处理，完全由 `Branding.ManagedProjects.props` 中的 `BrandingEnabled` 决定：

1. `false`：保持原来的构建行为，不动态处理 `AssemblyInfo.cs`
2. `true`：按项目规则命中和各字段 `*Enabled` 开关决定替换行为

`build.bat` 只保留以下环境变量：

1. `MSBUILD`
2. `BRANDING_DRY_RUN`

## 执行步骤

### 场景一：修改当前分支的 AssemblyInfo 覆盖值

1. 打开 `Branding.ManagedProjects.props`
2. 确认 `BrandingEnabled=true`
3. 在 `BrandingProjectPattern` 中维护目标项目
4. 打开需要覆盖字段对应的 `*Enabled`
5. 填入字段值，例如：

```xml
<BrandingAssemblyCompanyEnabled>true</BrandingAssemblyCompanyEnabled>
<BrandingAssemblyCompany>浙江实在智能科技有限公司</BrandingAssemblyCompany>

<BrandingAssemblyVersionEnabled>true</BrandingAssemblyVersionEnabled>
<BrandingAssemblyVersion>7.0.0.3</BrandingAssemblyVersion>
```

6. 需要覆盖为空字符串时，打开对应 `*Enabled`，字段值保持空，例如：

```xml
<BrandingAssemblyInformationalVersionEnabled>true</BrandingAssemblyInformationalVersionEnabled>
<BrandingAssemblyInformationalVersion></BrandingAssemblyInformationalVersion>
```

7. 不需要覆盖的字段保持 `*Enabled=false`
8. 执行 `build.bat`

### 场景二：先看当前会使用哪个 MSBuild

```powershell
Set-Location D:\projects\rpa\ii.rpa\src
$env:MSBUILD = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
$env:BRANDING_DRY_RUN = 'true'
cmd /c build.bat
```

### 场景三：真实构建

```powershell
Set-Location D:\projects\rpa\ii.rpa\src
$env:MSBUILD = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
cmd /c build.bat
```

## 验证建议

建议手动构建后检查：

1. `obj\...\Branding\ManagedAssemblyInfo.g.cs` 中是否只包含你启用的字段
2. `obj\...\Branding\SourceAssemblyInfo.g.cs` 中是否保留了未启用字段的原值
3. 启用且为空的字段是否被写成空字符串

## 注意事项

1. 不要再直接修改各项目 `AssemblyInfo.cs` 中已由 props 接管的字段。
2. 如果暂时不想动态处理，直接把 `BrandingEnabled` 改成 `false`。
3. 当前版本只处理 `AssemblyInfo.cs`。