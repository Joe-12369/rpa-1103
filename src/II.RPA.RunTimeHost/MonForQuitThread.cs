using System;
using System.Windows;
using System.Windows.Interop; // Required for ComponentDispatcher
using System.Windows.Threading; // Required for Dispatcher
using System.Threading;
using System.Runtime.InteropServices;

public partial class MonForQuitThread
{
    // P/Invoke and message definition (as shown above)
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint RegisterWindowMessage(string lpString);

    public static readonly uint WM_MY_CUSTOM_MESSAGE_WPF__MonForQuitThread = RegisterWindowMessage("MyWpfCustomMessage_MonForQuitThread");

    private static void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled)
    {
        if (msg.message == WM_MY_CUSTOM_MESSAGE_WPF__MonForQuitThread && msg.hwnd == IntPtr.Zero)
        {
            Environment.Exit(0); // 0表示成功
            handled = true; // 标记消息已处理
        }
    }

    public static Thread StartMonForQuit()
    {
        Thread newWindowThread = new Thread(new ThreadStart(() =>
        {
            // 挂钩消息循环
            ComponentDispatcher.ThreadFilterMessage += ComponentDispatcher_ThreadFilterMessage;

            // 启动该线程的Dispatcher消息循环
            // 这将阻塞，直到InvokeShutdown()被调用
            System.Windows.Threading.Dispatcher.Run();

            // Cleanup after Dispatcher.Run exits (due to InvokeShutdown)
            ComponentDispatcher.ThreadFilterMessage -= ComponentDispatcher_ThreadFilterMessage;
        }));

        // 必须将新线程设置为STA，WPF UI才能工作
        newWindowThread.SetApartmentState(ApartmentState.STA);
        // 可选：将其设置为后台线程，这样当主应用程序退出时它也会自动退出
        newWindowThread.IsBackground = true;
        newWindowThread.Start();

        return newWindowThread;
    }
}