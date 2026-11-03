using II.RPA.Logging;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;

namespace II.RPA.Plugin.GUI
{
    class DataCollectingCaptureTask : OrdinaryCaptureTask<DataCollectingCaptureTask>
    {
        private static ILogger logger = Logger.OfContext();
        protected override string Name => "数据采集普通元素拾取任务";//用于日志

        /// <summary>
        /// 查找相似元素需要的Selector
        /// </summary>
        internal string Selector { get; set; }

        internal override void BackToUIThread(Action action)
        {
            DataCollectingManager.dataCollectOverlayWnd.Dispatcher.Invoke(() =>
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

        }
        /// <summary>
        /// 拾取窗口后动作（每次）
        /// </summary>
        /// <returns>返回false 则本次不拾取</returns>
        internal override bool CaptureTask_GUICapturpWindowBefore()
        {
            return DataCollectingManager.dataCollectOverlayWnd.Dispatcher.Invoke(() =>
            {
                var point = System.Windows.Forms.Cursor.Position;
                bool isNonCapturePostion = DataCollectingManager.IsNonCapturePosition(point);
                //logger.LogDebug("CaptureTask_GUICapturpWindowBefore DataCollect");
                if (isNonCapturePostion)
                {
                    DataCollectingManager.dataCollectOverlayWnd.CurrentHighlightElement = null;
                    return false;
                }
                return true;
            });
        }
        internal override void CaptureTask_TickOnceCallback(IGuiNode guiNode)
        {
            logger.LogDebug("CaptureTask_TickOnceCallback DataCollection");
            if (!this.IsStart)
            {
                return;
            }
            if (guiNode == null)
            {
                DataCollectingManager.dataCollectOverlayWnd.CurrentHighlightElement = null;
                return;
            }

            var rect = guiNode.BoundingRectangle;
            if (rect == Rectangle.Empty)
            {
                logger.LogDebug("得到一个无效元素");
                DataCollectingManager.dataCollectOverlayWnd.CurrentHighlightElement = null;
            }
            else
            {
                DataCollectingManager.dataCollectOverlayWnd.CurrentHighlightElement = guiNode;
            }
        }
        internal override void CaptureTask_CaptureTaskThreadException(Exception ex)
        {
            if (ex is RPAException)
            {
                var rpaex = (RPAException)ex;
                rpaex.ComponentType = eComponentType.Capture;
                DataCollectingManager.dataCollectOverlayWnd.CaptureError = rpaex.Message;
            }
            DataCollectingManager.dataCollectOverlayWnd.CaptureException = ex;
            //Java Chrome 插件未安装异常导致拾取退出 
            DataCollectingManager.dataCollectOverlayWnd.StopHighlight();
        }
    }
}
