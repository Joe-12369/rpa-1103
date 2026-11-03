@echo off
@echo // +Start Git Pull Code
call PullCode
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
pause
