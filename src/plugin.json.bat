@echo off
setlocal enabledelayedexpansion

set SCRIPT_DIR=%~dp0
set I18N_FILE=%SCRIPT_DIR%..\plugin.json.csv
set PLUGINS_DIR=%SCRIPT_DIR%..\bin\plugins
REM -----------------------------------

if not exist "%I18N_FILE%" (
  echo ERROR: i18n.csv not found: %I18N_FILE%
  exit /b 1
)
if not exist "%PLUGINS_DIR%" (
  echo ERROR: plugins folder not found: %PLUGINS_DIR%
  exit /b 1
)

echo ============================================================
echo === 开始生成国际化 plugin.json 文件 ========================
echo ============================================================

powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -Command ^
  "$i18n = Import-Csv -Path '%I18N_FILE%'; " ^
  "$langs = ($i18n | Get-Member -MemberType NoteProperty).Name | Where-Object { $_ -ne 'key' -and $_ -ne 'zh-CN' }; " ^
  "$total = 0; $ok = 0; $fail = 0; $genCount = 0; " ^
  "Get-ChildItem -Path '%PLUGINS_DIR%' -Filter 'plugin.json' -Recurse | ForEach-Object { " ^
  "  $total++; $file = $_.FullName; Write-Host '--- Processing' $file; " ^
  "  try { $content = Get-Content -Raw -Path $file -Encoding UTF8; $json = $content | ConvertFrom-Json } catch { Write-Host ('[FAIL] parse ' + $file + ' → ' + $_.Exception.Message); $fail++; return } ;" ^
  "  $name = $json.name; if (-not $name) { Write-Host ('[FAIL] no name in ' + $file); $fail++; return } ;" ^
  "  $titleKey = $name + '_title'; $introKey = $name + '_introduction'; " ^
  "  foreach ($lang in $langs) { " ^
  "    $titleRow = $i18n | Where-Object { $_.key -eq $titleKey }; " ^
  "    $introRow = $i18n | Where-Object { $_.key -eq $introKey }; " ^
  "    $title = if ($titleRow) { $titleRow.$lang } else { $null }; " ^
  "    $intro = if ($introRow) { $introRow.$lang } else { $null }; " ^
  "    $missing = @(); " ^
  "    if ([string]::IsNullOrWhiteSpace($title)) { $title = $json.title; $missing += 'title' } " ^
  "    if ([string]::IsNullOrWhiteSpace($intro)) { $intro = $json.introduction; $missing += 'introduction' } " ^
  "    $out = $json.PSObject.Copy(); $out.title = $title; $out.introduction = $intro; $out.description = ''; " ^
  "    $outJson = $out | ConvertTo-Json -Depth 20; " ^
  "    $outPath = Join-Path -Path $_.DirectoryName -ChildPath ('plugin_' + $lang + '.json'); " ^
  "    try { " ^
  "      $outJson | Out-File -FilePath $outPath -Encoding UTF8; $genCount++; " ^
  "      if ($missing.Count -gt 0) { " ^
  "        Write-Host ('[OK] ' + $outPath + ' (fallback used: ' + ($missing -join ', ') + ')') " ^
  "      } else { " ^
  "        Write-Host ('[OK] ' + $outPath + ' (all translated)') " ^
  "      } " ^
  "    } catch { Write-Host ('[FAIL] write ' + $outPath + ' → ' + $_.Exception.Message) } " ^
  "  } " ^
  "  $ok++; " ^
  "} ;" ^
  "Write-Host '============================================================'; " ^
  "Write-Host ('Total plugins:     ' + $total); " ^
  "Write-Host ('Success plugins:   ' + $ok); " ^
  "Write-Host ('Failed plugins:    ' + $fail); " ^
  "Write-Host ('Generated files:   ' + $genCount); " ^
  "Write-Host '============================================================' "

echo Done.
