using II.RPA.WinAutomation.CommonUtility;
using MSHTML;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.DataCapture
{
    //123456
    public class ReadExecutorJS
    {
        public string ReadJSCode(string strJSFileName)
        {
            StreamReader sr = new StreamReader(strJSFileName);
            string strContent = string.Format("");
            strContent = sr.ReadToEnd();
            return strContent;
        }
    }

    public class DealExecutorJS
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        public IHTMLDocument2 _htmlDoc2;
        public DealExecutorJS()
        {
        }
        public bool GetWebBrowser()
        {
            IHTMLDocument2 htmlDoc2;
            bool bRet = PublicConfig.GetDocument2(out htmlDoc2);
            _htmlDoc2 = htmlDoc2;
            return bRet;
        }
        public bool GetDocExecute(bool bFirst)
        {
            if (bFirst == false)
            {
                return false;
            }

            PublicConfig.LogTrace("Document Execute Object 3");
            PublicConfig.gUpMutexInit.WaitOne();
            PublicConfig.RemoveIEExecuteDocAndListener(2);
            PublicConfig.ghtmlUpDoc = _htmlDoc2 as HTMLDocument;
            PublicConfig.ghtmlUpDoc2 = _htmlDoc2 as IHTMLDocument2;
            if (PublicConfig.ghtmlUpDoc != null)
            {
                PublicConfig.gdocEventListenerUp = new DocumentEventListenerUp();
                try
                {
                    PublicConfig.LogTrace(string.Format("Execute addEventListener 1"));
                    PublicConfig.ghtmlUpDoc.addEventListener(PublicConfig.gstrShiZaiEventOut, PublicConfig.gdocEventListenerUp, false);
                    PublicConfig.gUpMutexInit.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    PublicConfig.gUpMutexInit.ReleaseMutex();
                    //PublicConfig.ReturnErrorInfo(string.Format("采集页加载显示有误，请确保该目标采集页存在，未关闭并且为IE8及以上内核!"), 1);
                    return false;
                }

                bool retReadyState = PublicConfig.GetReadyStateComplete(PublicConfig.ghtmlUpDoc2);
                if (retReadyState == false)
                    return false;

                if (PublicConfig.ghtmlUpDoc != null)
                {

                }
                else
                {
                    PublicConfig.ReturnErrorInfo(string.Format("采集页加载显示有误，请确保该目标采集页存在，未关闭!"), 2);
                    return false;
                }
            }
            else
            {
                PublicConfig.gUpMutexInit.ReleaseMutex();
            }

            return true;
        }
        public void IEExecutorJS(int nType)
        {
            Task.Run(() =>
            {
                PublicConfig.LogTrace(string.Format("ExecutorJS Enter {0}", PublicConfig.gbExecBeginAnd));
                bool bFirst = true;
                PublicConfig.gbExecBeginAnd = 1;
                int nIterDoc = 0;
                int nIterFindScriptElement = 0;
                do
                {
                    if (true == GetWebBrowser() && PublicConfig.gbExecBeginAnd == 1)
                    {
                        nIterDoc = 0;
                        IHTMLElement htmlElementScript = null;
                        if (_htmlDoc2 != null)
                        {
                            bool retReadyState = PublicConfig.GetReadyStateComplete(_htmlDoc2);
                            if (retReadyState == false)
                            {
                                Thread.Sleep(5);
                                continue;
                            }

                            try
                            {
                                HTMLDocument htmlDoc = _htmlDoc2 as HTMLDocument;
                                htmlElementScript = htmlDoc.getElementById("SHI_ZAI_SCRIPT_ELEMENT_IN");
                            }
                            catch (Exception EX)
                            {

                            }
                        }
                        else if (PublicConfig.gbExecJS == false)
                        {
                            break;
                        }
                        else
                        {
                            Thread.Sleep(5);
                            continue;
                        }

                        if (htmlElementScript == null)
                        {
                            bool bRet = GetDocExecute(true);
                            if (bRet == false)
                            {
                                Thread.Sleep(5);
                                continue;
                            }

                            int nIter = 0;
                            do
                            {
                                if (++nIter > 3 || PublicConfig.gbExecJS == false)
                                {
                                    if (nIter > 3)
                                    {
                                        PublicConfig.ReturnErrorInfo(string.Format("执行JS失败，考虑IE内核不兼容或JS函数语法有误!"), 22);
                                    }

                                    break;
                                }

                                ReadExecutorJS nJS = new ReadExecutorJS();
                                string strContent = nJS.ReadJSCode(PublicConfig.gstrJSFileName);
                                string strJSCode = "function() {\r\n";
                                strJSCode += string.Format("{0}\r\n", strContent);
                                strJSCode += "      };";
                                JavaScriptExecutor sdk = new JavaScriptExecutor();
                                string strRet = sdk.DoExecJavaScript("", "", strJSCode, "");
                                PublicConfig.LogTrace(string.Format("Document Execute {0} Exec JS{1}", nType, strRet));
                                try
                                {
                                    JObject obj = JObject.Parse(strRet);
                                    if (obj["retCode"].ToString() == "SUCCEED")
                                    {
                                        if (PublicConfig.gformCapture._typeOperation == 1)
                                        {
                                            TransferEngineMessage nTransferToExecutor = new TransferEngineMessage();
                                            nTransferToExecutor.IEFromEngineToExecutorMessageByStart();
                                            bFirst = false;
                                            PublicConfig.LogTrace(string.Format("ExecutorJS Exist 2"));
                                            PublicConfig.gbExecBeginAnd = 2;
                                            return;
                                        }
                                        else if (PublicConfig.gformCapture._typeOperation == 2)
                                        {
                                            TransferEngineMessage nTransferToExecutor = new TransferEngineMessage();
                                            nTransferToExecutor.IEFromEngineToExecutorMessageByStart();
                                            bFirst = false;
                                        }

                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    continue;
                                }
                            } while (true);
                        }
                        else
                        {
                            if (bFirst == true)
                            {
                                bool bRet = GetDocExecute(true);
                                if (bRet == false)
                                {
                                    Thread.Sleep(5);
                                    continue;
                                }

                                TransferEngineMessage nTransferToExecutor = new TransferEngineMessage();
                                bool bStartRet = nTransferToExecutor.IEFromEngineToExecutorMessageByStart();
                                if (bStartRet == true)
                                {
                                    bFirst = false;
                                }
                                else
                                {
                                    Thread.Sleep(5);
                                    continue;
                                }
                            }
                            else
                            {

                            }
                        }

                        nIterFindScriptElement++;
                        if (nIterFindScriptElement > 150)
                        {
                            nIterFindScriptElement = 0;
                            PublicConfig.LogTrace(string.Format("HTMLScriptElement Has Been Found"));
                        }
                        Thread.Sleep(5);
                    }
                    else
                    {
                        PublicConfig.gUpMutexInit.WaitOne();
                        PublicConfig.RemoveIEExecuteDocAndListener(1);
                        PublicConfig.LogTrace("Document Execute NULL 3");
                        PublicConfig.ghtmlUpDoc2 = null;
                        PublicConfig.ghtmlUpDoc = null;
                        PublicConfig.gdocEventListenerUp = null;
                        PublicConfig.gUpMutexInit.ReleaseMutex();

                        nIterDoc++;
                        if (nIterDoc >= 3 && false == (PublicConfig.gformCapture._typeOperation == 1 && PublicConfig.gbRulesGet == true))
                        {
                            PublicConfig.ReturnErrorInfo(string.Format("采集页加载显示有误，请确保该目标采集页存在，未关闭!"), 16);
                            break;
                        }
                    }

                    Thread.Sleep(5);
                } while (PublicConfig.gbExecJS);

                PublicConfig.LogTrace(string.Format("ExecutorJS Exist 1"));
                PublicConfig.gbExecBeginAnd = 2;
            });
        }
    }
}
