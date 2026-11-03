using II.RPA.WinAutomation.CommonUtility;
using RobotLiteControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobotLiteControlsNew
{
    public class LiteSdkNew
    {
        AutoResetEvent eventReset;
        public LiteSdkNew()
        {
            eventReset = new AutoResetEvent(false);
        }
        public string GetConfig(string defaultParametersJson, string folderPath, string title = "")
        {
            string result = string.Empty;
            NLogger.Default.Trace(string.Format("入参1：{0},入参2：{1}", defaultParametersJson, folderPath));

            Thread thread = new Thread(() =>
             {
                 ConfigSetWnd csw = new ConfigSetWnd(defaultParametersJson, folderPath, title);
                 if (csw.ShowDialog() == true)
                 {
                     result = csw.outJson;
                 }
                 else
                 {
                     result = "1";
                 }
                 eventReset.Set();
             });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            eventReset.WaitOne();
            return result;

        }


        public string GetConfig585(string defaultParametersJson, string folderPath, string var, string title = "")
        {
            string result = string.Empty;
            NLogger.Default.Trace(string.Format("入参1：{0},入参2：{1}", defaultParametersJson, folderPath));

            Thread thread = new Thread(() =>
            {
                ConfigSetWnd csw = new ConfigSetWnd(defaultParametersJson, folderPath, title, var);
                if (csw.ShowDialog() == true)
                {
                    result = csw.outJson;
                }
                else
                {
                    result = "1";
                }
                eventReset.Set();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            eventReset.WaitOne();
            return result;
        }

        public string GetConfig593(string defaultParametersJson, string folderPath, string var, string title = "")
        {
            string result = string.Empty;
            string threadMessage = string.Empty;
            try
            {
                NLogger.Default.Trace(string.Format("入参1：{0},入参2：{1}", defaultParametersJson, folderPath));
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        ConfigSetWnd csw = new ConfigSetWnd(defaultParametersJson, folderPath, title, var);
                        if (csw.ShowDialog() == true)
                        {
                            result = csw.outJson;
                        }
                        else
                        {
                            result = "1";
                        }
                        eventReset.Set();
                    }
                    catch (Exception ex)
                    {
                        NLogger.Default.Error($"自定义对话框线程异常:{ex.Message}");
                        threadMessage = ex.Message;
                        eventReset.Set();
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                eventReset.WaitOne();
            }
            catch (Exception ex)
            {
                NLogger.Default.Error($"自定义对话框显示异常:{ex.Message}");
                result = 0 + ex.Message;//为兼容老的版本如此设计
            }
            if (!string.IsNullOrEmpty(threadMessage))
            {
                return 0 + threadMessage;
            }
            return result;
        }
    }
}
