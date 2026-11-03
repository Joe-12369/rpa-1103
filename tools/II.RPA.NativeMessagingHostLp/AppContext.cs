using Newtonsoft.Json;
using ScreenCapture;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.NativeMessagingHostLp
{
    class AppContext : ApplicationContext
    {
        private static Stopwatch stopwatch2 = new Stopwatch();
        private static Stopwatch stopwatch = new Stopwatch();
        private static string heartKey;
        public AppContext() : base()
        {
            using (Mutex mutex = new Mutex(true, "Global\\II.RPA.NativeMessagingHostLp", out bool ret))
            {
                if (!ret)
                {
                    Environment.Exit(0);//退出程序
                }
                Thread thread = new Thread(() =>
                {
                    using (System.Threading.Timer t2 = new System.Threading.Timer(mainBack, null, 0, 2 * 1000))
                    {
                        stopwatch2.Start();
                        while (true)
                        {
                            try
                            {
                                // DebugHelper.WriteLine("管道二等待连接！");

                                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("480294ec-d8da-4efe-90c0-e9f9b2fd0937", PipeDirection.InOut, 1))
                                {
                                    using (StreamReader sr = new StreamReader(pipeServer))
                                    {
                                        pipeServer.WaitForConnection();
                                        string input = sr.ReadLine();
                                        // DebugHelper.WriteLine("管道二：" + input);

                                        MethodOptions methodOptions = JsonConvert.DeserializeObject<MethodOptions>(input);
                                        if (methodOptions != null)
                                        {
                                            if (string.IsNullOrEmpty(heartKey))
                                            {
                                                heartKey = methodOptions.Params;
                                            }
                                            if (heartKey == methodOptions.Params)
                                            {
                                                stopwatch2.Restart();
                                            }
                                            else
                                            {
                                                DebugHelper.WriteLine("管道二心跳参数不匹配！");
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                DebugHelper.WriteError($"管道二连接出现异常" + ex.Message);
                            }
                        }
                    }
                });
                thread.Start();
                using (System.Threading.Timer t = new System.Threading.Timer(bak, null, 0, 2 * 1000))
                {
                    stopwatch.Start();
                    while (true)
                    {
                        try
                        {
                            DebugHelper.WriteLine("管道一等待连接！");

                            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("aa3f7b1a-c221-4d19-a646-77cd6c58f815", PipeDirection.InOut, 1))
                            {
                                using (StreamReader sr = new StreamReader(pipeServer))
                                {
                                    pipeServer.WaitForConnection();
                                    string input = sr.ReadLine();
                                    // DebugHelper.WriteLine($"管道一：{input}");
                                    MethodOptions methodOptions = JsonConvert.DeserializeObject<MethodOptions>(input);
                                    if (methodOptions != null)
                                    {
                                        if (methodOptions.MethodName == "Heartbeat")
                                        {
                                            stopwatch.Restart();
                                            continue;
                                        }
                                        DebugHelper.WriteLine(methodOptions.MethodName);

                                        Task.Run(() => LuPingService(methodOptions));
                                    }
                                    else
                                    {
                                        DebugHelper.WriteLine("参数格式有误！");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugHelper.WriteError($"连接出现异常" + ex.Message);
                        }
                        Console.WriteLine("");
                    }
                }
            }
        }

        private static void bak(object state)
        {
            // DebugHelper.WriteLine($"定时器一当前心跳时刻 " + stopwatch.ElapsedMilliseconds);
            if (stopwatch.ElapsedMilliseconds >= 1000 * 9)
            {
                // DebugHelper.WriteError($"定时器一超时结束！");
                Task.Run(() => ScreenHelper.GetSingleton().DoStopScreenCapture());
            }
        }
        private static void mainBack(object state)
        {
            // DebugHelper.WriteLine($"定时器二当前心跳时刻 " + stopwatch2.ElapsedMilliseconds);
            if (stopwatch2.ElapsedMilliseconds >= 1000 * 9)
            {
                // DebugHelper.WriteError($"定时器二超时退出！");
                Task.Run(() => ScreenHelper.GetSingleton().DoStopScreenCapture());
                Thread.Sleep(3000);
                Environment.Exit(1);//退出程序
            }
        }

        public static void LuPingService(MethodOptions methodOptions)
        {
            switch (methodOptions.MethodName)
            {
                case "DoStartScreenCapture":
                    stopwatch.Restart();
                    ScreenHelper.GetRecorderWrapper(methodOptions.Params).Start();
                    break;
                case "DoStopScreenCapture":
                    ScreenHelper.StopWrapper();
                    break;
                case "CutVideo":
                    ScreenHelper.GetSingleton().CutVideo(methodOptions.Params);
                    break;
                case "LastCutVideo":
                    ScreenHelper.GetSingleton().LastCutVideo(methodOptions.Params);
                    break;
                case "MergeVideo":
                    ScreenHelper.GetSingleton().MergeVideo(methodOptions.Params);
                    break;
                default:
                    throw new Exception($"未知的方法{methodOptions.MethodName}");
            }
        }

    }
}
