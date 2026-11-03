$RegistryRepoUrl = "ssh://git@code.ii-ai.tech:2222/rpa/rpa-monorepo/rpa-cs-utils/rpa-errors-code.git"
$RegistryRepoPath = Join-Path ([System.IO.Path]::GetTempPath()) ("rpa_enum_registry_{0}" -f [guid]::NewGuid().ToString("N"))
$ProjectName = "II.RPA"
$EnumsPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\src\II.RPA.WinAutomation.CommonUtility\enums"))

$exitCode = 0
$registryLocationPushed = $false
try {
    Write-Host "=== 检查并推送错误码 ===" -ForegroundColor Cyan
    Write-Host "项目错误码枚举目录: $EnumsPath" -ForegroundColor Gray
    
    $env:GIT_SSH_COMMAND = "ssh -o BatchMode=yes -o StrictHostKeyChecking=accept-new"
    Write-Host "拉取远程错误码..." -ForegroundColor Yellow
    git clone --depth 1 $RegistryRepoUrl $RegistryRepoPath
    
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $RegistryRepoPath)) {
        throw "无法克隆错误码仓库，请检查SSH密钥/known_hosts配置"
    }

    Push-Location $RegistryRepoPath
    $registryLocationPushed = $true
    $RegistryBranch = (git symbolic-ref --short HEAD) 2>$null
    
    # 获取项目当前所在的分支
    Push-Location $PSScriptRoot
    $CurrentBranch = (git symbolic-ref --short HEAD) 2>$null
    if (-not $CurrentBranch) { $CurrentBranch = "unknown" }
    Pop-Location
    
    if (-not $RegistryBranch) {
        throw "无法确定默认分支，请检查远端仓库设置"
    }
    
    function Get-FileMd5 {
        param([string]$FilePath)
        $hash = Get-FileHash $FilePath -Algorithm MD5
        return $hash.Hash
    }

    function Get-ErrorCodeValuesFromFile {
        param([string]$FilePath)
        $content = Get-Content $FilePath
        $values = @()
        foreach ($line in $content) {
            if ($line -match "^\s*(\w+)\s*=\s*([0-9]+)") {
                $values += [pscustomobject]@{ 
                    key  = $matches[1]
                    code = [int]$matches[2]
                }
            }
        }
        return $values
    }
    
    $enumFiles = Get-ChildItem -Path $EnumsPath -Filter *.cs -Recurse | Where-Object { $_.Name -cmatch "^e" }
    $conflicts = @()
    $hasUpdates = $false
    
    foreach ($enumFile in $enumFiles) {
        $enumName = $enumFile.Name
        $jsonPath = Join-Path $RegistryRepoPath ("{0}_{1}.json" -f $ProjectName, $enumName)
        
        Write-Host "检查: $enumName" -ForegroundColor Yellow
        
        $localHash = Get-FileMd5 $enumFile.FullName
        $registry = $null
        
        if (Test-Path $jsonPath) {
            $registry = Get-Content $jsonPath -Raw | ConvertFrom-Json
            if ($registry.Hash -eq $localHash) {
                Write-Host "  Hash 值相同，文件未改动，跳过检查。" -ForegroundColor Gray
                continue
            }
        }
        else {
            $registry = [pscustomobject]@{ 
                Name       = $enumName
                Path       = $enumFile.FullName
                Hash       = ""
                UpdateTime = ""
                Items      = @()
            }
            Write-Host "  发现新枚举文件" -ForegroundColor Gray
        }
        
        $localValues = Get-ErrorCodeValuesFromFile $enumFile.FullName
        
        $registryCodes = @{}
        foreach ($item in $registry.Items) {
            $registryCodes[$item.code] = $item.key
        }
        
        foreach ($lv in $localValues) {
            if ($registryCodes.ContainsKey($lv.code) -and $registryCodes[$lv.code] -ne $lv.key) {
                $conflicts += "  ${enumName}: 值 $($lv.code) ($($lv.key)) 冲突！已被注册为 '$($registryCodes[$lv.code])'"
            }
        }
        
        # 没有冲突才能更新内存对象
        if ($conflicts.Count -eq 0) {
            # 更新已合并的项
            $newItems = @()
            foreach ($lv in $localValues) {
                # 假设简单的全覆盖提取
                $newItems += [pscustomobject]@{
                    key    = $lv.key
                    code   = $lv.code
                    branch = $CurrentBranch
                }
            }
            # 合并旧项中不存在于当前代码的：可以选择保留或按需清理，为了不冲突默认保留
            foreach ($oldItem in $registry.Items) {
                $exists = $false
                foreach ($ni in $newItems) {
                    if ($ni.code -eq $oldItem.code) { $exists = $true; break }
                }
                if (-not $exists) { $newItems += $oldItem }
            }

            $registry.Items = $newItems
            $registry.Hash = $localHash
            $registry.UpdateTime = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
            $registry.Path = $enumFile.FullName
            
            $registry | ConvertTo-Json -Depth 5 | Set-Content $jsonPath -Encoding UTF8
            $hasUpdates = $true
            Write-Host "  已更新本地仓库 JSON 进行待推送..." -ForegroundColor Green
        }
    }
    
    if ($conflicts.Count -gt 0) {
        Write-Host "
❌ 发现枚举值冲突:" -ForegroundColor Red
        $conflicts | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        throw "发现枚举值冲突"
    }
    
    if ($hasUpdates) {
        Write-Host "
所有文件检查通过，提交变更到远程仓库..." -ForegroundColor Green
        git config user.name "RPA Bot" | Out-Null
        git config user.email "bot@rpa.com" | Out-Null
        git add .
        $commitMessage = "Update enum registry. Sync enumerations."
        git commit -m $commitMessage | Out-Null
        
        Write-Host "同步线上最新进展 (避免推送冲突)..." -ForegroundColor Yellow
        git pull --rebase origin $RegistryBranch | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "同步错误码仓库失败，请查看上方 Git 报错" }

        Write-Host "推送更改（覆盖线上）..." -ForegroundColor Green
        git push origin $RegistryBranch
        if ($LASTEXITCODE -ne 0) {
            throw "推送失败，请检查网络或是否遇到提交冲突（考虑使用 git pull --rebase）"
        }
        Write-Host "✅ 成功检查并同步错误码！" -ForegroundColor Green
    }
    else {
        Write-Host "
✅ 未检测到任何变化，检查通过。" -ForegroundColor Green
    }
}
catch {
    Write-Host "❌ 脚本执行失败: $_" -ForegroundColor Red
    $exitCode = 1
}
finally {
    if ($registryLocationPushed) { Pop-Location }
    if (Test-Path -LiteralPath $RegistryRepoPath) {
        try {
            Remove-Item -LiteralPath $RegistryRepoPath -Recurse -Force -ErrorAction Stop
        }
        catch {
            Write-Warning "清理临时错误码仓库失败: $_"
            $exitCode = 1
        }
    }
}
exit $exitCode
