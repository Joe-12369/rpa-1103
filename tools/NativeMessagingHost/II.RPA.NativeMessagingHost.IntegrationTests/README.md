# NMH black-box integration tests

These net48 tests launch the real NMH executable, simulate CRX Native Messaging through redirected stdin/stdout, and drive its real native Named Pipe endpoint. They do not launch a browser and only terminate processes they create.

```powershell
MSBuild.exe tools\NativeMessagingHost\NativeMessagingHost.sln /t:Rebuild /p:Configuration=Debug "/p:Platform=Any CPU" /m
dotnet test tools\NativeMessagingHost\II.RPA.NativeMessagingHost.IntegrationTests\II.RPA.NativeMessagingHost.IntegrationTests.csproj -c Debug
```
