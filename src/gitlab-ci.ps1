echo "============================== begin nuget build =============================="
dotnet restore .\II.RPA.sln
dotnet restore .\II.RPA.ZDeskTop\II.RPA.ZDeskTop.sln
echo "============================== begin executeAll.bat =============================="
if (Test-Path ..\bin) { Remove-Item -Force -Recurse ..\bin\II.RPA\bin }

./build-scripts.ps1
if ($LASTEXITCODE -ne 0 -or $?) {
    # If build-scripts threw an error, script shouldn't reach here if ErrorActionPreference is Stop,
    # but keeping a check makes it robust.
    if (-not $?) { Throw 'build-browser-scripts execution failed.' }
}


./check-error-code-conflicts.ps1
if ($LASTEXITCODE -ne 0) {
    Throw 'check-error-code-conflicts.ps1 execution failed with conflicts or errors. Stopping CI flow.'
}
cmd /c ".\build.bat"
if ($LASTEXITCODE -ne 0) {
    Throw 'build.bat execution failed.'
}
./plugin.json.bat

# Protect/obfuscate binaries only for release builds.
# QA builds should skip Virbox/Reactor to keep CI fast/stable.
$rawBuildOption = $env:BUILD_OPTION
if ([string]::IsNullOrWhiteSpace($rawBuildOption)) { $rawBuildOption = "" }
$buildOption = $rawBuildOption.ToLowerInvariant()
#if (!($env:ENABLE_GREEN -eq "true" -or $env:DISABLE_PROTECTCODE -eq "true") ) {
python .\virbox.py $env:PROTECT_TOOL
if ($LASTEXITCODE -ne 0) {
  Throw 'Protect code failed.'
}
#}


if ($env:BUILD_OPTION -eq "release") {
  python .\sign.py
  if ($LASTEXITCODE -ne 0) {
    Throw 'Sign failed.'
  }
}
if (Test-Path ..\bin\II.RPA\II.RPA.Test.exe) { Remove-Item -Force ..\bin\II.RPA\II.RPA.Test.exe }
if (Test-Path ..\bin\II.RPA\II.RPA.Test.exe.config) { Remove-Item -Force ..\bin\II.RPA\II.RPA.Test.exe.config }
Get-ChildItem ..\bin\ -Include *.pdb -Recurse | Remove-Item -Verbose
Start-Sleep -Seconds 5
Move-Item ..\bin ..\dist-temp
