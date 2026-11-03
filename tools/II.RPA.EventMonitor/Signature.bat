@echo off
setlocal

if "%SIGN_CODE%"=="" (
    echo Skip signing because SIGN_CODE is empty
    endlocal & exit /b 0
)

if not defined RPA_SIGN_CLI (
    echo RPA_SIGN_CLI is required
    endlocal & exit /b 16
)
if not defined RPA_SIGN_CLI_SCRIPT (
    echo RPA_SIGN_CLI_SCRIPT is required
    endlocal & exit /b 16
)

set "PIPELINE_ID=%CI_PIPELINE_ID%"
if not defined PIPELINE_ID set "PIPELINE_ID=%BUILD_NUMBER%"
if not defined PIPELINE_ID set "PIPELINE_ID=manual"
set "OUTPUT_DIR=%~dp0II.RPA.EventMonitor\bin\II.RPA.EventMonitor"

"%RPA_SIGN_CLI%" "%RPA_SIGN_CLI_SCRIPT%" /file "%OUTPUT_DIR%\*.dll" "%OUTPUT_DIR%\*.exe" /kind ii-rpa-binaries /request-id "%PIPELINE_ID%:ii-rpa-binaries:event-monitor"
set "SIGN_EXIT_CODE=%ERRORLEVEL%"

endlocal & exit /b %SIGN_EXIT_CODE%
