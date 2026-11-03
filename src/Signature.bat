@echo off
setlocal

pushd "%~dp0" || exit /b 1
python sign.py
set "SIGN_EXIT_CODE=%ERRORLEVEL%"
popd

endlocal & exit /b %SIGN_EXIT_CODE%
