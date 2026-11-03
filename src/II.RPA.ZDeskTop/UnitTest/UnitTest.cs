using II.Framework.PInvoker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTest;

[TestClass]
public class UnitTest
{
    [TestMethod]
    public void WindowShotTest()
    {
        IntPtr hwnd = WindowPInvoker.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            hwnd = WindowPInvoker.GetDesktopWindow();
            Console.WriteLine("No active window found.Use desktop instead.");
        }
        WindowPInvoker win32Invoker = new WindowPInvoker(hwnd);
        win32Invoker.GetWindowText(out string text)
            .OutputToConsole(text)
            .CaptureWindowUsingBitBlt(false, out Bitmap shot);
        shot.Save($@".\{text}-{DateTime.Now:HH-mm-ss}.png");
    }

    [TestMethod]
    public void AnotherWindowShotTest()
    {
        IntPtr hwnd = WindowPInvoker.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            hwnd = WindowPInvoker.GetDesktopWindow();
            Console.WriteLine("No active window found.Use desktop instead.");
        }
        int length = WindowPInvoker.GetWindowTextLength(hwnd);
        StringBuilder title = new(length + 1);
        WindowPInvoker.GetWindowText(hwnd, title, title.Capacity);
        Console.WriteLine("Current active window title: " + title);
        using CaptureProvider captureProvider = new();
        if (captureProvider.Capture(hwnd, out Bitmap shot))
        {
            shot.Save($@".\{title}-{DateTime.Now:HH-mm-ss}.png");
        }
    }

    [TestMethod]
    public void ZDesktopWindowTest()
    {
        string _rootPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\zdesktop";
        if (Directory.Exists(_rootPath))
        {
            try
            {
                Directory.Delete(_rootPath, true);
                Directory.CreateDirectory(_rootPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail to operate _rootPath:{ex}");
            }
        }
        var filename = @"..\..\..\..\..\bin\II.RPA.ZDeskTop\II.RPA.ZDeskTop.exe";
        // var args = "--u indeedadmin";
        var args = "--u indeedadmin.windowonly --m agent --p 7181";
        ProcessStartInfo processStartInfo = new()
        {
            FileName = filename,
            Arguments = args,
            UseShellExecute = true,
        };
        var process = Process.Start(processStartInfo);
        process.WaitForInputIdle();
        Thread.Sleep(3000);
        nint hwnd = process.MainWindowHandle;
        if (hwnd == IntPtr.Zero)
        {
            Assert.Fail("hwnd == IntPtr.Zero");
        }

        int x = 0;
        int y = 0;
        int w = 100;
        int h = 100;

        WindowPInvoker win32Invoker = new WindowPInvoker(hwnd);
        win32Invoker.GetWindowText(out string text)
            .OutputToConsole(text)
            .Sleep(1000)
            .HideWindow()
            .Sleep(1000)
            .ShowWindow()
            .Sleep(1000)
            .CaptureWindow(out Bitmap bitmap)
            .Sleep(1000)
            .SetMiniWindow(x,y,w,h)
            .Sleep(1000)
            .CaptureWindow(out Bitmap bitmapmin)
            .Sleep(1000)
            .ResetWindow(x,y,w*2,h*2)
            .Sleep(1000)
            .CaptureWindow(out Bitmap bitmapreset)
            .Sleep(1000)
            .SetForeground()
            .ResetWindow(x, y, w * 5, h * 5)
            .Sleep(1000)
            .CaptureWindow(out Bitmap bitmapreset2)
            .Sleep(1000)
            .ResetWindow(-3000, -3000, 372, 213)
            .Sleep(1000)
            .CaptureWindow(out Bitmap bitmapreset3)
            .CloseWindow();

        SavePicture(bitmap, 50, nameof(bitmap));
        SavePicture(bitmapmin, 50, nameof(bitmapmin));
        SavePicture(bitmapreset, 50, nameof(bitmapreset));
        SavePicture(bitmapreset2, 50, nameof(bitmapreset2));
        SavePicture(bitmapreset3, 20, nameof(bitmapreset3));
    }
    [TestMethod]
    public async Task ZDesktopWindowAutoRestartTest()
    {
        string _rootPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\zdesktop";
        if (Directory.Exists(_rootPath))
        {
            try
            {
                Directory.Delete(_rootPath, true);
                Directory.CreateDirectory(_rootPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail to operate _rootPath:{ex}");
            }
        }
        var filename = @"..\..\..\..\..\bin\II.RPA.ZDeskTop\II.RPA.ZDeskTop.exe";
        var args = "--u indeedadmin --m agent --p 7181 --e 5";
        ProcessStartInfo processStartInfo = new()
        {
            FileName = filename,
            Arguments = args,
            UseShellExecute = true,
        };
        var process = Process.Start(processStartInfo);
        process.EnableRaisingEvents = true;
        process.Exited += (s,e) => 
        {
            Console.WriteLine($"Exited:{process.ExitTime - process.StartTime}-{process.ExitCode}");
        };
        var waitForExitTask = Task.Run(() => 
        {
           return process.WaitForExit(1000);
        });
        var waitForIdlTask = Task.Run(() =>
        {
            try
            {
                return process.WaitForInputIdle();
            }
            catch 
            { 
                return false; 
            }
        });
        var results = await Task.WhenAll(waitForExitTask, waitForIdlTask);
        Assert.IsTrue(results[0]);
        var newArgs = "--u indeedadmin --m agent --p 7181";
        processStartInfo = new()
        {
            FileName = filename,
            Arguments = newArgs,
            UseShellExecute = true,
            Verb = "runas"
        };
        var newProcess = Process.Start(processStartInfo);
        newProcess.EnableRaisingEvents = true;
        newProcess.Exited += (s, e) =>
        {
            Console.WriteLine($"Exited:{process.ExitTime - process.StartTime}-{process.ExitCode}");
        };
        var waitForExitTask2 = Task.Run(() =>
        {
            return newProcess.WaitForExit(1000);
        });
        var waitForIdlTask2 = Task.Run(() =>
        {
            try
            {
                return newProcess.WaitForInputIdle();
            }
            catch
            {
                return false;
            }
        });
        var results2 = await Task.WhenAll(waitForExitTask2, waitForIdlTask2);
        Assert.IsTrue(!results2[0] && results2[1]);
    }
    [TestMethod]
    public async Task ZDesktopWindowAdminTest()
    {
        string _rootPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\zdesktop";
        if (Directory.Exists(_rootPath))
        {
            try
            {
                Directory.Delete(_rootPath, true);
                Directory.CreateDirectory(_rootPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail to operate _rootPath:{ex}");
            }
        }
        var filename = @"..\..\..\..\..\bin\II.RPA.ZDeskTop\II.RPA.ZDeskTop.exe";
        var args = "--u indeedadmin --m agent --p 7181";
        ProcessStartInfo processStartInfo = new()
        {
            FileName = filename,
            Arguments = args,
            UseShellExecute = true,
            Verb = "runas"
        };
        var process = Process.Start(processStartInfo);
        process.EnableRaisingEvents = true;
        process.Exited += (s, e) =>
        {
            Console.WriteLine($"Exited:{process.ExitTime - process.StartTime}-{process.ExitCode}");
        };
        if (process.HasExited)
        {
            
        }

    }

    private void Process_Exited(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    [TestMethod]
    public void ZDesktopWindowPractice()
    {
        string _rootPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\zdesktop";
        if (Directory.Exists(_rootPath))
        {
            try
            {
                Directory.Delete(_rootPath, true);
                Directory.CreateDirectory(_rootPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail to operate _rootPath:{ex}");
            }
        }
        var filename = @"..\..\..\..\..\bin\II.RPA.ZDeskTop\II.RPA.ZDeskTop.exe";

        var args = "--u indeedadmin --m agent --p 7181";
        ProcessStartInfo processStartInfo = new()
        {
            FileName = filename,
            Arguments = args,
            UseShellExecute = true,
        };
        var process = Process.Start(processStartInfo);
        process.WaitForInputIdle();
        Thread.Sleep(3000);
        nint hwnd = process.MainWindowHandle;
        if (hwnd == IntPtr.Zero)
        {
            Assert.Fail("hwnd == IntPtr.Zero");
        }

        WindowPInvoker win32Invoker = new(hwnd);
        win32Invoker.GetWindowText(out string text)
            .OutputToConsole(text)
            .Sleep(1000);
    }
    private void SavePicture(Bitmap bitmap, int compressRatio = 50, string path = null)
    {
        Console.WriteLine(bitmap.RawFormat);
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap), "Bitmap cannot be null.");

        var time = $"{DateTime.Now:HH-mm-ss}";
        var sanitizedPath = string.IsNullOrWhiteSpace(path) ? "image" : path;

        var originalPath = $@".\{sanitizedPath}-{time}-original.jpg";
        bitmap.Dispose();
    }

}
