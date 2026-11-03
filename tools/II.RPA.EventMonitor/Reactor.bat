set b=%cd%\II.RPA.EventMonitor
dotNET_Reactor -file "%b%\bin\II.RPA.EventMonitor\II.RPA.EventMonitor.exe" -targetfile "%b%\bin\SecureBin\<AssemblyFileName>"
copy %b%\bin\SecureBin\*.exe %b%\bin\II.RPA.EventMonitor
del %b%\bin\SecureBin\*.exe