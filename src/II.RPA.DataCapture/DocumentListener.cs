using MSHTML;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.DataCapture
{
    //Test 654321
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class DocumentEventListenerUp
    {
        [DispId(0)]
        // The "target" parameter is an implementation detail.
        public void MethodForEventListener()
        {
            PublicConfig.LogTrace(string.Format("Execute MethodForEventListener"));
            PublicConfig.gUpMutex.WaitOne();
            PublicConfig.LogTrace(string.Format("Execute MethodForEventListener EX"));
            try
            {
                if (PublicConfig.ghtmlUpDoc != null)
                {
                
                    string strText = "";
                    IHTMLElement htmlElementUpOut = PublicConfig.ghtmlUpDoc.getElementById("SHI_ZAI_MESSAGE_ELEMENT_OUT");
                    if (htmlElementUpOut != null)
                    {
                        strText = htmlElementUpOut.innerText == null ? "" : htmlElementUpOut.innerText;
                    }

                    PublicConfig.LogTrace(string.Format("Execute MethodForEventListener strText:{0}", strText));

                    if (strText != "")
                    {
                        ResolveUpMessage nExecutor = new ResolveUpMessage();
                        nExecutor.ResolveMessage(strText);
                    }
                }
            }
            catch (Exception EX)
            {

            }
            PublicConfig.gUpMutex.ReleaseMutex();
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class DocumentEventListenerDown
    {
        [DispId(0)]
        // The "target" parameter is an implementation detail.
        public void MethodForEventListener()
        {
            PublicConfig.LogTrace(string.Format("Configure MethodForEventListener"));
            PublicConfig.gDownMutex.WaitOne();
            PublicConfig.LogTrace(string.Format("Configure MethodForEventListener EX"));
            try
            {
                if (PublicConfig.ghtmlDownDoc != null)
                {
                    string strText = "";
                    IHTMLElement htmlElementDownOut = PublicConfig.ghtmlDownDoc.getElementById("SHI_ZAI_MESSAGE_ELEMENT_OUT");
                    if (htmlElementDownOut != null)
                    {
                        strText = htmlElementDownOut.innerText == null ? "" : htmlElementDownOut.innerText;
                    }

                    PublicConfig.LogTrace(string.Format("Configure MethodForEventListener strText:{0}", strText));

                    if (strText != "")
                    {
                        ResolveDownMessage nConfigure = new ResolveDownMessage();
                        nConfigure.ResolveMessage(strText);
                    }
                }
            }
            catch (Exception EX)
            {

            }
            PublicConfig.gDownMutex.ReleaseMutex();
        }
    }
}
