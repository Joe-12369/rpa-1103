using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using Gma.UserActivityMonitor;
using Newtonsoft.Json.Linq;
using II.RPA.WinAutomation.CommonUtility;
using FlaUI.Core.WindowsAPI;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Video2Process
{
    //654321
    public class Video2ProcessImp
    {
        [DllImport("./ScreenCapture.dll", EntryPoint = "DoStartScreenCapture", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool DoStartScreenCapture(int nF, Byte[] encodedBytes, int nLevel);
        [DllImport("./ScreenCapture.dll", EntryPoint = "DoStopScreenCapture", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool DoStopScreenCapture();
        [DllImport("./Element.dll", EntryPoint = "DoCaptureElementPicture", CharSet = CharSet.Auto)]
        public static extern IntPtr DoCaptureElementPicture(Byte[] encodedBytes);
        [DllImport("./Element.dll", EntryPoint = "DoGoWaitEX", CharSet = CharSet.Auto)]
        public static extern IntPtr DoGoWaitEX(int hWnd, Byte[] encodedBytes1, int nType);
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, StringBuilder lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr WindowFromPoint(int xPoint, int yPoint);
        [DllImport("User32.dll", EntryPoint = "FindWindowA")]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short GetKeyState(int nVirtKey);

        private static ILogger logger = Logger.OfContext();

        public bool StartRecord(string strVideoFilePath, string strLogFilePath)
        {
            PublicVideo2Process.gWidth = GetSystemMetrics(0);  //屏幕宽度    
            PublicVideo2Process.gHeight = GetSystemMetrics(1); //屏幕高度
            logger.LogDebug("StartRecord");
            PublicVideo2Process.gMutex.WaitOne();
            PublicVideo2Process.gProcess = 0;
            PublicVideo2Process.gVideoFilePath = strVideoFilePath;
            PublicVideo2Process.gLogFilePath = strLogFilePath;
            PublicVideo2Process.gbCapture = true;
            PublicVideo2Process.gJObjectLog = new JArray();
            PublicVideo2Process.gTSInterval = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            Byte[] encodedBytes = Encoding.UTF8.GetBytes(strVideoFilePath);
            bool bRet = DoStartScreenCapture(15, encodedBytes, 3);
            Video2ProcessImp.InstallHook();
            HookManager.MouseMove += Video2ProcessImp.HookManager_MouseMove;
            PublicVideo2Process.gMutex.ReleaseMutex();
            logger.LogDebug("StartRecord End");
            return bRet;
        }
        public string RecordOnce(string strElementPicPath)
        {
            logger.LogDebug(string.Format("RecordOnce 1 Process {0} {1}", PublicVideo2Process.gProcess, PublicVideo2Process.gbCapture));
            PublicVideo2Process.gMutex.WaitOne();
            if (PublicVideo2Process.gProcess != 0 || PublicVideo2Process.gbCapture == false)
            {
                PublicVideo2Process.gMutex.ReleaseMutex();
                return "ERROR100";
            }

            logger.LogDebug("RecordOnce 2 ");
            PublicVideo2Process.gFileNameMutex.WaitOne();
            PublicVideo2Process.gstrElementPicFileName = strElementPicPath;
            PublicVideo2Process.gbCapture = false;
            PublicVideo2Process.gFileNameMutex.ReleaseMutex();
            PublicVideo2Process.gMutex.ReleaseMutex();
            Task t1 = Task.Run(() =>
            {
                long nRecordOnce = 0;
                do
                {
                    if (PublicVideo2Process.gbCapture == false && PublicVideo2Process.gProcess == 0)
                    {
                        nRecordOnce++;
                        if (nRecordOnce % 300 == 0)
                        {
                            logger.LogDebug("RecordOnce 3");
                        }
                        Thread.Sleep(10);
                    }
                    else
                    {
                        break;
                    }
                } while (true);
            });
            t1.Wait();

            logger.LogDebug($"RecordOnce 4 {PublicVideo2Process.gstrElementPicFileName}");
            if (PublicVideo2Process.gstrElementPicFileName != "ERROR101" && PublicVideo2Process.gstrElementPicFileName != ""
                && PublicVideo2Process.gstrElementPicFileName != strElementPicPath)
            {
                try
                {
                    JObject obj = new JObject();
                    PublicVideo2Process.gFileNameMutex.WaitOne();
                    obj = JObject.Parse(PublicVideo2Process.gstrElementPicFileName);
                    PublicVideo2Process.gFileNameMutex.ReleaseMutex();
                    logger.LogDebug("RecordOnce 5");
                    return obj["pic_path"].ToString();
                }
                catch (Exception ex)
                {
                    PublicVideo2Process.gFileNameMutex.ReleaseMutex();
                    logger.LogDebug("RecordOnce 6");
                    return "ERROR101";
                }
            }
            else
            {
                logger.LogDebug("RecordOnce 7");
                return "ERROR101";
            }
        }
        public bool PauseRecord()
        {
            logger.LogDebug("PauseRecord");
            PublicVideo2Process.gMutex.WaitOne();
            PublicVideo2Process.gProcess = 1;
            Video2ProcessImp.UninstallHook();
            PublicVideo2Process.gMutex.ReleaseMutex();
            return true;
        }
        public bool RestartRecord()
        {
            logger.LogDebug("RestartRecord");
            PublicVideo2Process.gMutex.WaitOne();
            PublicVideo2Process.gProcess = 0;
            Video2ProcessImp.InstallHook();
            PublicVideo2Process.gMutex.ReleaseMutex();
            return true;
        }
        public bool StopRecord()
        {
            logger.LogDebug("StopRecord 1");
            PublicVideo2Process.gMutex.WaitOne();
            bool bRet = false;
            try
            {
                logger.LogDebug("StopRecord 2");
                PublicVideo2Process.gProcess = 2;
                PublicVideo2Process.gbCapture = true;
                try
                {
                    Video2ProcessImp.UninstallHook();
                    HookManager.MouseMove -= Video2ProcessImp.HookManager_MouseMove;
                }
                catch (Exception ex1)
                {
                    logger.LogDebug(ex1);
                }
                bRet = DoStopScreenCapture();
                if (bRet == true)
                {
                    logger.LogDebug("停止录屏成功");
                }
                logger.LogDebug("StopRecord 3");
                string strLog = PublicVideo2Process.gJObjectLog.ToString();
                string strLogT = Regex.Replace(strLog, @"[\r\n]", "");
                StreamWriter sw = new StreamWriter(PublicVideo2Process.gLogFilePath, false, Encoding.Default);
                sw.WriteLine(strLogT);
                sw.Close();
                logger.LogDebug("保存鼠标键盘数据文件成功");
                PublicVideo2Process.gJObjectLog.Clear();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex);
                bRet = false;
            }

            return bRet;
        }
        static public void InstallHook()
        {
            Video2ProcessImp.UninstallHook();
            HookManager.MouseDown += Video2ProcessImp.HookManager_MouseDown;
            HookManager.KeyDown += Video2ProcessImp.HookManager_KeyDown;
            HookManager.MouseUp += Video2ProcessImp.HookManager_MouseUp;
            HookManager.KeyUp += Video2ProcessImp.HookManager_KeyUp;
            HookManager.MouseWheel += Video2ProcessImp.HookManager_MouseWheel;
        }
        static public void UninstallHook()
        {
            HookManager.MouseDown -= Video2ProcessImp.HookManager_MouseDown;
            HookManager.KeyDown -= Video2ProcessImp.HookManager_KeyDown;
            HookManager.MouseUp -= Video2ProcessImp.HookManager_MouseUp;
            HookManager.KeyUp -= Video2ProcessImp.HookManager_KeyUp;
            HookManager.MouseWheel -= Video2ProcessImp.HookManager_MouseWheel;
        }
        public static void HookManager_MouseWheel(object sender, MouseEventArgs e)
        {
            PublicVideo2Process.gMutex.WaitOne();
            if (PublicVideo2Process.gProcess != 0)
            {
                PublicVideo2Process.gMutex.ReleaseMutex();
                return;
            }

            JObject obj = new JObject();
            obj["event_id"] = 522;
            obj["event_type"] = 3;
            obj["value"] = e.Delta;
            JObject action = new JObject();
            action["action"] = obj;
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0) - PublicVideo2Process.gTSInterval;
            long lTS = Convert.ToInt64(ts.TotalMilliseconds);
            action["interval"] = lTS;
            PublicVideo2Process.gJObjectLog.Add(action);
            PublicVideo2Process.gMutex.ReleaseMutex();
        }
        public static void HookManager_MouseMove(object sender, MouseEventArgs e)
        {
            POINT pt;
            GetCursorPos(out pt);
            int nPosX = pt.X;
            int nPosY = pt.Y;
            //logger.LogDebug(string.Format("MouseMove {0} {1}", nPosX, nPosY));
            IntPtr hWnd = FindWindow(null, "***智能视频转RPA浮动提示***");
            RECT rect;
            GetWindowRect(hWnd, out rect);
            //rect.left = 0;
            //rect.right = 400;
            //rect.top = 0;
            //rect.bottom = 400;
            if (hWnd != IntPtr.Zero && rect.left < nPosX && rect.right > nPosX && rect.top < nPosY && rect.bottom > nPosY)
            {
                //logger.LogDebug(string.Format("left:{0} top:{1} right:{2} bottom:{3} X:{4} Y:{5}", 
                //    rect.left, rect.top, rect.right, rect.bottom, nPosX, nPosY));
                if (PublicVideo2Process.gProcess == 0)
                {
                    logger.LogDebug("PauseRecord");
                    PublicVideo2Process.gMutex.WaitOne();
                    PublicVideo2Process.gProcess = 1;
                    PublicVideo2Process.gbCapture = false;
                    //Video2ProcessImp.UninstallHook();
                    PublicVideo2Process.gMutex.ReleaseMutex();
                }
            }
            else if (hWnd != IntPtr.Zero)
            {
                if (PublicVideo2Process.gProcess == 1)
                {
                    logger.LogDebug("RestartRecord");
                    PublicVideo2Process.gMutex.WaitOne();
                    PublicVideo2Process.gProcess = 0;
                    PublicVideo2Process.gbCapture = true;
                    //Video2ProcessImp.InstallHook();
                    PublicVideo2Process.gMutex.ReleaseMutex();
                }
            }
        }
        public static void HookManager_KeyDown(object sender, KeyEventArgs e)
        {
            PublicVideo2Process.gMutex.WaitOne();
            if (PublicVideo2Process.gProcess != 0)
            {
                PublicVideo2Process.gMutex.ReleaseMutex();
                return;
            }

            JObject obj = new JObject();
            obj["event_type"] = 2;
            obj["key"] = (int)e.KeyCode;
            obj["down_or_up"] = 1;
            obj["alt"] = 0;
            obj["shift"] = 0;
            obj["ctrl"] = 0;
            int bCtrl = GetKeyState(0x11) & 0x8000;
            int bShift = GetKeyState(0x10) & 0x8000;
            int bAlt = GetKeyState(0x12) & 0x8000;
            obj["shift"] = bShift;
            obj["alt"] = bAlt;
            obj["ctrl"] = bCtrl;
            JObject action = new JObject();
            action["action"] = obj;
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0) - PublicVideo2Process.gTSInterval;
            long lTS = Convert.ToInt64(ts.TotalMilliseconds);
            action["interval"] = lTS;
            PublicVideo2Process.gJObjectLog.Add(action);
            PublicVideo2Process.gMutex.ReleaseMutex();
        }
        public static void HookManager_KeyUp(object sender, KeyEventArgs e)
        {
            PublicVideo2Process.gMutex.WaitOne();
            if (PublicVideo2Process.gProcess != 0)
            {
                PublicVideo2Process.gMutex.ReleaseMutex();
                return;
            }

            JObject obj = new JObject();
            obj["event_type"] = 2;
            obj["key"] = (int)e.KeyCode;
            obj["down_or_up"] = 2;
            obj["alt"] = 0;
            obj["shift"] = 0;
            obj["ctrl"] = 0;
            int bCtrl = GetKeyState(0x11) & 0x8000;
            int bShift = GetKeyState(0x10) & 0x8000;
            int bAlt = GetKeyState(0x12) & 0x8000;
            obj["shift"] = bShift;
            obj["alt"] = bAlt;
            obj["ctrl"] = bCtrl;
            JObject action = new JObject();
            action["action"] = obj;
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0) - PublicVideo2Process.gTSInterval;
            long lTS = Convert.ToInt64(ts.TotalMilliseconds);
            action["interval"] = lTS;
            PublicVideo2Process.gJObjectLog.Add(action);
            PublicVideo2Process.gMutex.ReleaseMutex();
        }
        public static void HookManager_MouseDown(object sender, MouseEventArgs e)
        {
            logger.LogDebug(string.Format("MouseButtons.Left 1 "));
            PublicVideo2Process.gMutex.WaitOne();
            if (PublicVideo2Process.gProcess != 0 || PublicVideo2Process.gbCapture == true)
            {
                PublicVideo2Process.gMutex.ReleaseMutex();
                return;
            }

            switch (e.Button)
            {
                case MouseButtons.Left:
                    {
                        logger.LogDebug(string.Format("MouseButtons.Left 2"));
                        JObject obj = new JObject();
                        obj["event_type"] = 1;
                        obj["event_id"] = 513;
                        JObject action = new JObject();
                        action["action"] = obj;
                        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0) - PublicVideo2Process.gTSInterval;
                        long lTS = Convert.ToInt64(ts.TotalMilliseconds);
                        action["interval"] = lTS;
                        JObject location = new JObject();
                        location["x"] = e.X;
                        location["y"] = e.Y;
                        action["location"] = location;
                        PublicVideo2Process.gJObjectLog.Add(action);

                        PublicVideo2Process.gFileNameMutex.WaitOne();
                        try
                        {
                            logger.LogDebug(PublicVideo2Process.gstrElementPicFileName);
                            bool bJson = false;
                            try
                            {
                                JObject obj1 = JObject.Parse(PublicVideo2Process.gstrElementPicFileName);
                                bJson = true;
                            }
                            catch (Exception ex)
                            {
                                char[] szIndexAny = { '{', '}', '\n', '\r' };
                                if (PublicVideo2Process.gstrElementPicFileName.IndexOfAny(szIndexAny) >= 0)
                                {
                                    bJson = true;
                                }
                            }

                            if (PublicVideo2Process.gbCapture == false && PublicVideo2Process.gstrElementPicFileName != ""
                                && PublicVideo2Process.gstrElementPicFileName != "ERROR101" && bJson == false)
                            {
                                Byte[] encodedBytes = new Byte[256];
                                Buffer.BlockCopy(Encoding.GetEncoding("gb2312").GetBytes(PublicVideo2Process.gstrElementPicFileName), 0, encodedBytes,
                                    0, Encoding.GetEncoding("gb2312").GetBytes(PublicVideo2Process.gstrElementPicFileName).Length);
                                try
                                {
                                    IntPtr pFileName = DoCaptureElementPicture(encodedBytes);
                                    PublicVideo2Process.gstrElementPicFileName = Marshal.PtrToStringAnsi(pFileName);
                                    if (PublicVideo2Process.gstrElementPicFileName != "" && PublicVideo2Process.gstrElementPicFileName != "ERROR101")
                                    {
                                        try
                                        {
                                            JObject objRet = JObject.Parse(PublicVideo2Process.gstrElementPicFileName);
                                            if (objRet["type"].ToString() == "1")
                                            {
                                                int nIterExists = 0;
                                                do
                                                {
                                                    if (File.Exists(objRet["pic_path"].ToString()))
                                                    {
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        Thread.Sleep(100);
                                                        nIterExists++;
                                                    }

                                                    if (nIterExists > 30)
                                                        break;
                                                } while (true);

                                                PublicVideo2Process.gbCapture = true;

                                                //string strTitle = "";
                                                //Byte[] bytesTitle = new Byte[256];
                                                //Buffer.BlockCopy(Encoding.GetEncoding("gb2312").GetBytes(strTitle), 0, bytesTitle,
                                                //    0, Encoding.GetEncoding("gb2312").GetBytes(strTitle).Length);
                                                //DoGoWaitEX(0, bytesTitle, 2);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            PublicVideo2Process.gbCapture = true;
                                        }
                                    }
                                    PublicVideo2Process.gbCapture = true;
                                }
                                catch (Exception ex1)
                                {
                                    PublicVideo2Process.gstrElementPicFileName = "ERROR101";
                                    PublicVideo2Process.gbCapture = true;
                                }
                            }
                            else
                            {
                                PublicVideo2Process.gstrElementPicFileName = "ERROR101";
                                PublicVideo2Process.gbCapture = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Thread.Sleep(250);
                            PublicVideo2Process.gbCapture = true;
                        }
                        PublicVideo2Process.gFileNameMutex.ReleaseMutex();
                    }
                    break;
                case MouseButtons.Middle:
                    {
                        JObject obj = new JObject();
                        obj["event_type"] = 1;
                        obj["event_id"] = 519;
                        JObject action = new JObject();
                        action["action"] = obj;
                        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0) - PublicVideo2Process.gTSInterval;
                        long lTS = Convert.ToInt64(ts.TotalMilliseconds);
                        action["interval"] = lTS;
                        JObject location = new JObject();
                        location["x"] = e.X;
                        location["y"] = e.Y;
                        action["location"] = location;
                        PublicVideo2Process.gJObjectLog.Add(action);
                    }
                    break;
                case MouseButtons.Right:
                    {
                        JObject obj = new JObject();
                        obj["event_type"] = 1;
                        obj["event_id"] = 516;
                        JObject action = new JObject();
                        action["action"] = obj;
                        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0) - PublicVideo2Process.gTSInterval;
                        long lTS = Convert.ToInt64(ts.TotalMilliseconds);
                        action["interval"] = lTS;
                        JObject location = new JObject();
                        location["x"] = e.X;
                        location["y"] = e.Y;
                        action["location"] = location;
                        PublicVideo2Process.gJObjectLog.Add(action);
                    }
                    break;
                default:
                    break;
            }
            PublicVideo2Process.gMutex.ReleaseMutex();
        }
        public static void HookManager_MouseUp(object sender, MouseEventArgs e)
        {
            PublicVideo2Process.gMutex.WaitOne();
            if (PublicVideo2Process.gProcess != 0)
            {
                PublicVideo2Process.gMutex.ReleaseMutex();
                return;
            }

            switch (e.Button)
            {
                case MouseButtons.Left:
                    {
                        JObject obj = new JObject();
                        obj["event_type"] = 1;
                        obj["event_id"] = 514;
                        JObject action = new JObject();
                        action["action"] = obj;
                        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0) - PublicVideo2Process.gTSInterval;
                        long lTS = Convert.ToInt64(ts.TotalMilliseconds);
                        action["interval"] = lTS;
                        JObject location = new JObject();
                        location["x"] = e.X;
                        location["y"] = e.Y;
                        action["location"] = location;
                        PublicVideo2Process.gJObjectLog.Add(action);
                    }
                    break;
                case MouseButtons.Middle:
                    {
                        JObject obj = new JObject();
                        obj["event_type"] = 1;
                        obj["event_id"] = 520;
                        JObject action = new JObject();
                        action["action"] = obj;
                        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0) - PublicVideo2Process.gTSInterval;
                        long lTS = Convert.ToInt64(ts.TotalMilliseconds);
                        action["interval"] = lTS;
                        JObject location = new JObject();
                        location["x"] = e.X;
                        location["y"] = e.Y;
                        action["location"] = location;
                        PublicVideo2Process.gJObjectLog.Add(action);
                    }
                    break;
                case MouseButtons.Right:
                    {
                        JObject obj = new JObject();
                        obj["event_type"] = 1;
                        obj["event_id"] = 517;
                        JObject action = new JObject();
                        action["action"] = obj;
                        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0) - PublicVideo2Process.gTSInterval;
                        long lTS = Convert.ToInt64(ts.TotalMilliseconds);
                        action["interval"] = lTS;
                        JObject location = new JObject();
                        location["x"] = e.X;
                        location["y"] = e.Y;
                        action["location"] = location;
                        PublicVideo2Process.gJObjectLog.Add(action);
                    }
                    break;
                default:
                    break;
            }
            PublicVideo2Process.gMutex.ReleaseMutex();
        }
    }
}
