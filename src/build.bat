@echo off
setlocal

if /i "%ARCH%"=="x64" (
    set PLATFORM=x64
) else if /i "%ARCH%"=="x86" (
    set PLATFORM=x86
) else (
    REM Preserve the 7.3.6sp1 build contract: an unset ARCH builds x86.
    set PLATFORM=x86
)

dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [Build] ERROR: .NET SDK is unavailable.
    exit /b 2
)

set "BRANDING_LOG_SCRIPT=%~dp0Build.Branding.Log.ps1"
set "BRANDING_BUILD_LOG=%TEMP%\ii.rpa-branding-build.log"

"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%BRANDING_LOG_SCRIPT%" -Action reset -LogPath "%BRANDING_BUILD_LOG%"

if exist ..\bin rd /s /Q ..\bin

REM Keep x86 scheduling unchanged while testing serial x64 builds.
set "BUILD_PARALLEL_ARGS="
if /i "%PLATFORM%"=="x64" set "BUILD_PARALLEL_ARGS=-m:1"
set "BUILD_LOG_DIR=E:\jenkins\workspace\rpa\rpa-monorepo\rpa-build\logs"
if not exist "%BUILD_LOG_DIR%" mkdir "%BUILD_LOG_DIR%"
if not exist "%BUILD_LOG_DIR%" exit /b 2

echo [Build] Platform=%PLATFORM% MSBuildArgs=%BUILD_PARALLEL_ARGS%
echo [Build] Binary logs: %BUILD_LOG_DIR%
dotnet --version

REM Complete all cleaning before any build can generate shared references.
dotnet clean II.RPA.sln -c Release -p:Platform=%PLATFORM% %BUILD_PARALLEL_ARGS% "-bl:%BUILD_LOG_DIR%\II.RPA-Clean-Release-%PLATFORM%.binlog"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%
dotnet clean II.RPA.ZDeskTop\II.RPA.ZDeskTop.sln -c Release -p:Platform=%PLATFORM% %BUILD_PARALLEL_ARGS% "-bl:%BUILD_LOG_DIR%\II.RPA.ZDeskTop-Clean-Release-%PLATFORM%.binlog"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

dotnet build II.RPA.sln -c Release -p:Platform=%PLATFORM% %BUILD_PARALLEL_ARGS% "-bl:%BUILD_LOG_DIR%\II.RPA-Release-%PLATFORM%.binlog"
set "BUILD_EXIT_CODE=%ERRORLEVEL%"
if exist "%BUILD_LOG_DIR%\II.RPA-Release-%PLATFORM%.binlog" (
    for %%F in ("%BUILD_LOG_DIR%\II.RPA-Release-%PLATFORM%.binlog") do echo [Build] Binary log: %%~fF [%%~zF bytes]
) else (
    echo [Build] WARNING: Binary log missing: %BUILD_LOG_DIR%\II.RPA-Release-%PLATFORM%.binlog
)
if %BUILD_EXIT_CODE% neq 0 exit /b %BUILD_EXIT_CODE%
dotnet build II.RPA.ZDeskTop\II.RPA.ZDeskTop.sln -c Release -p:Platform=%PLATFORM% %BUILD_PARALLEL_ARGS% "-bl:%BUILD_LOG_DIR%\II.RPA.ZDeskTop-Release-%PLATFORM%.binlog"
set "BUILD_EXIT_CODE=%ERRORLEVEL%"
if exist "%BUILD_LOG_DIR%\II.RPA.ZDeskTop-Release-%PLATFORM%.binlog" (
    for %%F in ("%BUILD_LOG_DIR%\II.RPA.ZDeskTop-Release-%PLATFORM%.binlog") do echo [Build] Binary log: %%~fF [%%~zF bytes]
) else (
    echo [Build] WARNING: Binary log missing: %BUILD_LOG_DIR%\II.RPA.ZDeskTop-Release-%PLATFORM%.binlog
)
if %BUILD_EXIT_CODE% neq 0 exit /b %BUILD_EXIT_CODE%

echo.
echo [Branding] ===== Dynamic branding summary =====
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%BRANDING_LOG_SCRIPT%" -Action summary -LogPath "%BRANDING_BUILD_LOG%"
echo [Branding] ===== End dynamic branding summary =====
echo.
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%BRANDING_LOG_SCRIPT%" -Action clear -LogPath "%BRANDING_BUILD_LOG%"

del ..\bin\II.RPA.PluginLoader\*.pdb
del ..\bin\II.RPA.ZDeskTop\*.pdb

set "DIR=..\bin\II.RPA.PluginLoader\runtime"
if exist "%DIR%" (
    for /r "%DIR%" %%f in (*.pdb) do del /q "%%f"
)
