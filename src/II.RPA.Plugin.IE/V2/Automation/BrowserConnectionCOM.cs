using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.IE.V2.Automation
{
    public class BrowserConnectionCOM : IBrowserConnectionNew
    {
        private object _instance = null;
        private Type _type = null;
        public BrowserConnectionCOM()
        {
            string className = "IEAutomation";
            _type = Type.GetType(className);
            _instance = Activator.CreateInstance(_type);
        }
        public PipeResponse PipeResponse => null;

        public bool IsConnected { get; set; } = true;

        InitScripeStateType IBrowserConnectionNew.InitScripeState { get; set; } = InitScripeStateType.Complete;

        public PipeResponse Send(eGuiType eGuiType, string command, JObject @params, int timeout = 5000)
        {
            return null;
        }

        public void Start()
        {

        }

        public void Stop()
        {

        }

        public bool TestConnection()
        {
            return true;
        }

        public bool TestConnection(eGuiType guiType)
        {
            return TestConnection();
        }
    }
}
