using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI
{
    class OverlayFormCaptureTask : OrdinaryCaptureTask<OverlayFormCaptureTask>
    {
        private static ILogger logger = Logger.OfContext();
        protected override string Name => "普通元素拾取任务";

        //private RemoteCaptureClient client
        //{
        //    get
        //    {
        //        //return OverlayForm.client;
        //        return OverlayForm.CurrentRemoteCaptureClient;
        //    }
        //}
        private bool IsStartRemoteCapture
        {
            get
            {
                return OverlayForm.isStartRemoteCapture;
            }
            set
            {
                OverlayForm.isStartRemoteCapture = value;
            }
        }
        private string address = string.Empty;
        private bool isSocket
        {
            get
            {
                return GuiElement.overlayForm.isSocket;
            }
            set
            {
                GuiElement.overlayForm.isSocket = value;
            }
        }
        internal override void BackToUIThread(Action action)
        {
            GuiElement.overlayForm.Invoke(() =>
            {
                action();
            });
        }
        /// <summary>
        /// 在拾取过程中，GUI类型变化时 执行，虚方法 无具体动作
        /// </summary>
        /// <param name="guiType"></param>
        internal override void CaptureTask_GuiTypeChanged(eGuiType guiType)
        {
            GuiElement.overlayForm.SetWindowType(guiType.ToString());
        }
        /// <summary>
        /// 拾取窗口后动作（每次）
        /// </summary>
        /// <returns>返回false 则本次不拾取</returns>
        internal override bool CaptureTask_GUICapturpWindowBefore()
        {
            if (GuiElement.overlayForm.CancellationForCapture.IsCancellationRequested)
            {
                Task.Delay(200).Wait();
                logger.LogDebug("========= Cancel OverlayFormCaptureTask ==========");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 拾取窗口后动作（每次）
        /// </summary>
        /// <returns>返回false 则本次不拾取</returns>
        internal override bool CaptureTask_GUICapturpWindowAfter()
        {
            //if (Configurations.Info.EnableRemoteCapture && RemoteCapture(this.CaptureRootWnd)) //远程拾取
            //{
            //    if (GuiElement.overlayForm.CurrentCaptureMode == Params.CaptureModeType.Intelligence)
            //    {
            //        CVCaptureTask.Instance.StopAfterExecute();
            //    }
            //    GuiElement.overlayForm.ElementCaptureNode = null;
            //    return false;
            //}
            //else
            //{
            //    if (GuiElement.overlayForm.CurrentCaptureMode == Params.CaptureModeType.Intelligence)
            //    {
            //        CVCaptureTask.Instance.Start();
            //    }
            //}
            return true;
        }
        internal override void CaptureTask_TickOnceCallback(IGuiNode guiNode)
        {
            //logger.LogDebug("CaptureTask_TickOnceCallback OverlayFormCapture");
            if (!this.IsStart)
            {
                return;
            }
            if (guiNode == null)
            {
                return;
            }

            try
            {
                // 在拾取的过程中，用户可能会执行关闭操作，从而导致属性访问失效
                var rect = guiNode.BoundingRectangle;
                if (rect == Rectangle.Empty)
                {
                    logger.LogDebug("得到一个无效元素");
                }
                else
                {
                    GuiElement.overlayForm.ElementCaptureNode = guiNode;
                }
            }
            catch
            {
            }
        }
        internal override void CaptureTask_CaptureTaskThreadException(Exception ex)
        {
            GuiElement.overlayForm.CaptureException = ex;
            GuiElement.overlayForm.StopHighlight();
        }

        //private bool RemoteCapture(AutomationElement rootElement)
        //{
        //    string rootElementName = rootElement.Name;
        //    bool isRemoteCapture = false;
        //    if (rootElementName.Contains("远程桌面连接") || rootElementName.StartsWith("FreeRD"))
        //    {
        //        isRemoteCapture = true;
        //        try
        //        {
        //            var m = Regex.Match(rootElementName, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
        //            if (!m.Success)
        //            {
        //                return true;
        //            }
        //            //var ip = rootElement.Name.Split('-')[0].Trim();
        //            var ip = m.Value;
        //            var port = Configurations.Info.RemotePort;
        //            var key = ip + "," + port.ToString();
        //            string address = key;

        //            if (!isSocket && !client.IsConnected)
        //            {
        //                isSocket = true;
        //                client.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
        //            }
        //            GuiElement.overlayForm.CurrentHighlightElement = null;
        //            if (!IsStartRemoteCapture)
        //            {
        //                if (client.IsConnected)
        //                {
        //                    IsStartRemoteCapture = true;
        //                    client.DataReceived -= GuiElement.overlayForm.Client_DataReceived;
        //                    client.StartRemoteCapture(key);
        //                    return true;
        //                }
        //            }
        //            else
        //            {
        //                return true;
        //            }
        //        }
        //        catch (Exception)
        //        {

        //        }

        //    }
        //    else
        //    {
        //        try
        //        {
        //            if (IsStartRemoteCapture && client.IsConnected)
        //            {
        //                IsStartRemoteCapture = false;
        //                client.EndRemoteCapture();
        //            }
        //        }
        //        catch (Exception)
        //        {
        //        }
        //    }
        //    return isRemoteCapture;
        //}
    }
}
