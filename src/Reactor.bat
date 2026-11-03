dotNET_Reactor -file "..\bin\II.RPA\II.RPA.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*dll ..\bin\II.RPA
del ..\bin\SecureBin\*.dll

dotNET_Reactor -file "..\bin\II.RPA.AdminTransfer\II.RPA.AdminTransfer.exe" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.exe ..\bin\II.RPA.AdminTransfer
del ..\bin\SecureBin\*.exe


dotNET_Reactor -file "..\bin\II.RPA.PluginLoader\II.RPA.PluginLoader.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
dotNET_Reactor -file "..\bin\II.RPA.PluginLoader\II.RPA.Plugin.Core.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
dotNET_Reactor -file "..\bin\II.RPA.PluginLoader\II.RPA.WinAutomation.CommonUtility.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.dll ..\bin\II.RPA.PluginLoader
del ..\bin\SecureBin\*.dll

dotNET_Reactor -file "..\bin\plugins\Element\cs\II.RPA.Plugin.GUI.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.dll ..\bin\plugins\Element\cs
del ..\bin\SecureBin\*.dll

dotNET_Reactor -file "..\bin\plugins\GUIChrome\cs\II.RPA.Plugin.Chrome.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.dll ..\bin\plugins\GUIChrome\cs
del ..\bin\SecureBin\*.dll

dotNET_Reactor -file "..\bin\plugins\GUICV\cs\II.RPA.Plugin.CV.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.dll ..\bin\plugins\GUICV\cs
del ..\bin\SecureBin\*.dll

dotNET_Reactor -file "..\bin\plugins\GUIIE\cs\II.RPA.Plugin.IE.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.dll ..\bin\plugins\GUIIE\cs
del ..\bin\SecureBin\*.dll

dotNET_Reactor -file "..\bin\plugins\GUIJava\cs\II.RPA.Plugin.Java.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.dll ..\bin\plugins\GUIJava\cs
del ..\bin\SecureBin\*.dll

dotNET_Reactor -file "..\bin\plugins\GUISAP\cs\II.RPA.Plugin.SAP.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.dll ..\bin\plugins\GUISAP\cs
del ..\bin\SecureBin\*.dll

dotNET_Reactor -file "..\bin\plugins\GUIWindows\cs\II.RPA.Plugin.Windows.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.dll ..\bin\plugins\GUIWindows\cs
del ..\bin\SecureBin\*.dll

dotNET_Reactor -file "..\bin\plugins\GUIZExplorer\cs\II.RPA.Plugin.ZExplorer.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.dll ..\bin\plugins\GUIZExplorer\cs
del ..\bin\SecureBin\*.dll

dotNET_Reactor -file "..\bin\II.RPA.BrowserAutomation\II.RPA.BrowserAutomationServer.exe" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
copy ..\bin\SecureBin\*.exe ..\bin\II.RPA.BrowserAutomation
del ..\bin\SecureBin\II.RPA.BrowserAutomationServer.exe

for /d %%i in ("..\bin\plugins\*") do ( 
    echo ",WdtErp,Database,Mobile,Element,GUIChrome,GUICV,GUIIE,GUIJava,GUISAP,GUIWindows,GUIZExplorer,GUIFrieFox,"| findstr ",%%~ni," >nul && (
        echo %%~niÒÑ»ìÏý
    ) || (
        dotNET_Reactor -file "..\bin\plugins\%%~ni\cs\II.RPA.Plugin.%%~ni.dll" -targetfile "..\bin\SecureBin\<AssemblyFileName>"
        copy ..\bin\SecureBin\*.dll ..\bin\plugins\%%~ni\cs
        del ..\bin\SecureBin\*.dll
    )
)