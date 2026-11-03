using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;

namespace II.Framework.Utility;

public static class SchedulerTaskHelper
{
    public const string CHILD_SESSION_CoreServer_TASK = "CoreServerStartTask";
    public const string CHILD_SESSION_Unlock_TASK = "CHILD_SESSION_Unlock_TASK";
    public static void CreateTask(string aTaskName, 
        string anAppPath, 
        string anAuthor, 
        int aDelay, 
        string aParam = null,
        bool injectDotnetRoot = false)
    {
        string fullTaskName = aTaskName;
        string appPath = anAppPath;
        using TaskService taskService = new();
        TaskDefinition taskDefinition = taskService.NewTask();
        taskDefinition.RegistrationInfo.Author = anAuthor;
        taskDefinition.RegistrationInfo.Description = "For II.RPA.ZDeskTop";
        taskDefinition.Settings.DisallowStartIfOnBatteries = false;
        taskDefinition.Settings.StopIfGoingOnBatteries = false;

        if (EnvironmentHelper.IsAdmin)
        {
            taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
        }
        LogonTrigger logonTrigger = new()
        {
            UserId = WindowsIdentity.GetCurrent().User.Value,
            Delay = TimeSpan.FromSeconds(aDelay)
        };
        //var bt = new BootTrigger { Delay = new TimeSpan(0, 0, 1) };
        taskDefinition.Triggers.Add(logonTrigger);
        if (injectDotnetRoot)
        {
            // 任务计划程序创建的进程不会继承注册者的进程级环境变量，DOTNET_ROOT 会丢失，
            // 导致新会话中的 CoreServer 找不到 bundled 运行时。
            // 任务动作改为 wscript + VBS：wscript 是 GUI 子系统进程（无控制台窗口），
            // VBS 内部以隐藏方式（窗口样式 0）运行 cmd 包装，注入 DOTNET_ROOT 后启动 CoreServer，
            // 避免子会话里出现黑色 cmd 窗口。
            var dotnetRoot = ResolveDotnetRoot();
            if (!string.IsNullOrWhiteSpace(dotnetRoot))
            {
                var wscriptPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wscript.exe");
                var vbsPath = Path.Combine(Path.GetTempPath(), "zdt_launch_child_core.vbs");
                // VBScript 不认 UTF-8 BOM，使用 UTF-16（BOM）写入，兼容含中文的路径。
                File.WriteAllText(vbsPath, BuildVbsLauncher(appPath, aParam, dotnetRoot), Encoding.Unicode);
                taskDefinition.Actions.Add(new ExecAction(wscriptPath, "\"" + vbsPath + "\"", null));
                taskService.RootFolder.RegisterTaskDefinition(fullTaskName, taskDefinition);
                return;
            }
        }

        taskDefinition.Actions.Add(new ExecAction(appPath, aParam, null));
        taskService.RootFolder.RegisterTaskDefinition(fullTaskName, taskDefinition);
    }

    /// <summary>
    /// 解析 bundled 运行时目录：优先取当前进程的 DOTNET_ROOT 环境变量（launcher 注入），
    /// 取不到时从程序所在目录向上查找 runtime\win-&lt;arch&gt; / win-&lt;arch&gt;。
    /// </summary>
    private static string ResolveDotnetRoot()
    {
        var archVariable = Environment.Is64BitProcess ? "DOTNET_ROOT_X64" : "DOTNET_ROOT_X86";
        foreach (var name in new[] { "DOTNET_ROOT", archVariable })
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(Path.Combine(value, "host", "fxr")))
            {
                return value;
            }
        }

        var arch = Environment.Is64BitProcess ? "x64" : "x86";
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(dir.FullName, "runtime", "win-" + arch),
                Path.Combine(dir.FullName, "win-" + arch),
            })
            {
                if (Directory.Exists(Path.Combine(candidate, "host", "fxr")))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 生成 wscript 启动脚本：以隐藏方式运行 cmd 包装（注入 DOTNET_ROOT 后启动目标程序并重定向输出）。
    /// </summary>
    private static string BuildVbsLauncher(string appPath, string aParam, string dotnetRoot)
    {
        var command = new StringBuilder();
        command.Append("cmd /d /s /c \"set \"DOTNET_ROOT=").Append(dotnetRoot).Append('"');
        if (Environment.Is64BitProcess)
        {
            command.Append(" && set \"DOTNET_ROOT_X64=").Append(dotnetRoot).Append('"');
        }
        else
        {
            command.Append(" && set \"DOTNET_ROOT_X86=").Append(dotnetRoot).Append('"');
        }

            command.Append(" && \"").Append(appPath).Append('"');
        if (!string.IsNullOrWhiteSpace(aParam))
        {
            command.Append(' ').Append(aParam);
        }

        // 捕获子会话 CoreServer 的启动输出（含 apphost/hostfxr 报错），便于定位启动失败原因。
        command.Append(" > \"").Append(Path.Combine(Path.GetTempPath(), "zdt_child_core.log")).Append("\" 2>&1");
        command.Append('"');
        var escaped = command.ToString().Replace("\"", "\"\"");
        return "Set sh = CreateObject(\"WScript.Shell\")\r\nsh.Run \"" + escaped + "\", 0, False\r\n";
    }
    public static void DeleteTask(string aTaskName)
    {
        try
        {
            using TaskService taskService = new();
            taskService.RootFolder.DeleteTask(aTaskName, false);
        }
        catch
        {
            //ignore
        }
    }

    public static IEnumerable<string> EnumerateTask(int aMax)
    {
        using TaskService taskService = new();
        try
        {
            var tasks = taskService.RootFolder.Tasks;
            if (tasks.Any())
            {
                if (tasks.Count() > aMax)
                {
                    return tasks.Take(aMax).Select(task => $"{task.Name};{task.NextRunTime}");
                }
                else
                {
                    return tasks.Select(task => $"{task.Name};{task.NextRunTime}");
                }
            }
            return Enumerable.Empty<string>();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }
    public static bool CanRegisterTask()
    {
        try
        {
            var testName = "CanRegisterTask";
            CreateTask(testName, Assembly.GetExecutingAssembly().FullName, "Zdesktoptest", 1);
            Console.WriteLine($"Create {testName}");
            DeleteTask(testName);
            Console.WriteLine($"Delete {testName}");

            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"CanRegisterTask Error: {ex}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
}
