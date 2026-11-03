@echo off
@echo // +Stop
@echo // +Start Build C# Project
call build.bat
@echo // +Stop
@echo // +Start Confuse
call Reactor.bat
@echo // +Stop
@echo // +Start Add Digital Signature
call Signature.bat
@echo // +Stop
@echo // +Start Delete exe
del ..\bin\II.RPA\II.RPA.Test.exe
del ..\bin\II.RPA\II.RPA.Test.exe.config
@echo // +Stop
