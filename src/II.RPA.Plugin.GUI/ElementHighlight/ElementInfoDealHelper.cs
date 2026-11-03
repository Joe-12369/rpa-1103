using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ElementHighlight
{
    internal class ElementInfoDealHelper
    {
        private static ILogger logger = Logger.OfContext();

        bool IsEnable = false;//暂时弃用系统进程检测

        #region 刷新当前系统进程集合的id集合
        /// <summary>
        /// 定时器 刷新当前系统开启的explorer进程们的id集合
        /// </summary>
        System.Timers.Timer CRefreshExplorerPIDTimer = null;

        /// <summary>
        /// 开始刷新
        /// </summary>
        public void StartExplorerPIDTimers()
        {
            if(!IsEnable)
            {
                return;
            }

            try
            {
                CRefreshExplorerPIDTimer.Enabled = true;
                CRefreshExplorerPIDTimer?.Start();
            }
            catch (Exception ex)
            {
                logger.LogError($"StartExplorerPIDTimers Alert:{ex.ToString()}");
            }
        }

        /// <summary>
        /// 开始刷新
        /// </summary>
        public void StopExplorerPIDTimers()
        {
            if (!IsEnable)
            {
                return;
            }

            try
            {
                if (CRefreshExplorerPIDTimer != null)
                {
                    CRefreshExplorerPIDTimer.Enabled = false;
                    CRefreshExplorerPIDTimer?.Stop();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"StopExplorerPIDTimers Alert:{ex.ToString()}");
            }
        }

        /// <summary>
        /// 回收
        /// </summary>
        public void DisposeExplorerPIDTimers()
        {
            if (!IsEnable)
            {
                return;
            }

            try
            {
                StopExplorerPIDTimers();
                CRefreshExplorerPIDTimer?.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError($"DisposeExplorerPIDTimers Alert:{ex.ToString()}");
            }
        }


        public void InitExplorerPIDTimer()
        {
            if (!IsEnable)
            {
                return;
            }

            try
            {
                if (CRefreshExplorerPIDTimer == null)
                {
                    CRefreshExplorerPIDTimer = new System.Timers.Timer(100);
                }
                CRefreshExplorerPIDTimer.Enabled = false;
                CRefreshExplorerPIDTimer.Elapsed += CRefreshExplorerPIDTimer_Elapsed;
            }
            catch (Exception ex)
            {
                logger.LogError($"InitExplorerPIDTimer Alert:{ex.ToString()}");
            }
        }
        private void CRefreshExplorerPIDTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RefreshExplorerPIDs();
        }
        #endregion

        #region 当前explorer 资源系统进程

        public List<int> m_ExplorerPIDs = new List<int>();

        /// <summary>
        /// 刷新资源管理器进程id集合
        /// </summary>
        public void RefreshExplorerPIDs()
        {
            try
            {
                //m_ExplorerPIDs.Clear();
                var processes = Process.GetProcessesByName("explorer");
                if (processes != null)
                {
                    var currentprocesses = processes.Where(p => p.SessionId == Process.GetCurrentProcess().SessionId);
                    if (currentprocesses != null && currentprocesses.Any())
                    {

                        currentprocesses.ForEach(cp =>
                        {
                            if (!m_ExplorerPIDs.Contains(cp.Id))
                            {
                                m_ExplorerPIDs.Add(cp.Id);
                            }
                        });
                        //logger.LogError($"explorerob:m_ExplorerPIDs is {string.Join(";", m_ExplorerPIDs)}");
                    }
                    else
                    {
                        //logger.LogError($"explorerob:currentprocess is null");
                    }
                }
                else
                {
                    //logger.LogError($"explorerob:explorer process is null");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"异常:{ex.ToString()}");
            }
        }

        /// <summary>
        /// 判断目标进程是否为windows进程（资源管理器进程）
        /// </summary>
        /// <param name="aElementToHighlight"></param>
        /// <returns></returns>
        public bool IsExplorer(IGuiNode aElementToHighlight)
        {
            if (!IsEnable)
            {
                return false;
            }

            if (!(aElementToHighlight is IGuiCVNode))
            {
                try
                {
                    //if (CurrentAutomationElement.Properties.ProcessId.ValueOrDefault
                    //    == Process.GetCurrentProcess().Id)
                    //{
                    //    logger.LogError("ElementHighlight Error:Highlight myself");
                    //    return false;
                    //}
                    
                    //var _CurrentAutomationElement = GetCurrentAutomationElement(aElementToHighlight);

                    //UiCommon.GetWindowThreadProcessId(aElementToHighlight.WindowHandle, out var processId1);//实测取不到windowHandle


                    //if (m_ExplorerPIDs.Contains(_CurrentAutomationElement?.Properties.ProcessId))
                    //{
                    //    return true;
                    //}


                    if (aElementToHighlight is IGuiNode_6_6_0 && m_ExplorerPIDs.Contains(((IGuiNode_6_6_0)aElementToHighlight).ProcessID))
                    {
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError($"IsExplorer:{ex.ToString()}");
                    return false;
                }
            }
            return false;
        }

        public AutomationElement GetCurrentAutomationElement(IGuiNode _ElementToHighlight)
        {
            var point = _ElementToHighlight.GetClickablePoint();
            GetPointAndWndInfo(point, out AutomationElement rootElement, out AutomationElement pointElement);
            return pointElement;
        }

        /// <summary>
        /// 调用flaUI获取元素
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="rootElement"></param>
        /// <param name="pointElement"></param>
        private void GetPointAndWndInfo(Point screenPoint,
            out AutomationElement rootElement,
            out AutomationElement pointElement)
        {
            rootElement = null;
            pointElement = null;

            try
            {
                var hWndList = UiCommon.GetRootWindowList(screenPoint);
                //hWndList = hWndList?.Where(h=> UiCommon.UIAAutomation.FromHandle(h)?.AutomationId != "WAOverlayWindow").ToList();
                if (hWndList.Count > 0)
                {
                    foreach (IntPtr hWnd in hWndList)
                    {
                        if (hWnd != IntPtr.Zero)
                        {
                            UiCommon.GetWindowThreadProcessId(hWnd, out var processId1);
                            if (processId1 != GlobleConfigurations.CurrentProcessId)
                            {
                                rootElement = UiCommon.UIAAutomation.FromHandle(hWnd);
                            }
                        }
                        if (rootElement == null)
                        {
                            //logger.LogDebug($"RootElement is null.{hWndList.Count}");
                            break;
                        }

                        if (rootElement.Properties.BoundingRectangle.IsSupported
                            && rootElement.BoundingRectangle.Contains(screenPoint))
                        {
                            //logger.LogWarning("find rootElement");
                            break;
                        }
                    }
                    pointElement = UiCommon.UIAAutomation.FromHandle(hWndList.Last());
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                logger.LogError($"获取屏幕元素时发生一次异常，将忽略本次异常继续拾取");
            }
        }

        #endregion

        /// <summary>
        /// 获取元素标签对应的中文名称
        /// </summary>
        /// <param name="_TargetName"></param>
        /// <returns></returns>
        public string GetCNLabelForElement(string _TargetName)
        {
            if (!string.IsNullOrEmpty(_TargetName))
            {
                string _CNLabelName = II.RPA.WinAutomation.CommonUtility.TargrtXmlHelper.CreateInstance().GetTranslatedNameByCode(_TargetName);
                if (!string.IsNullOrEmpty(_CNLabelName))
                {
                    _TargetName = _CNLabelName;
                }
            }

            return _TargetName;
        }

        public ElementHighLighterParam GetCurrentElementHighLighterParam(IGuiNode _CurrentHighLightElement)
        {
            eGuiType _aGuiType = GuiManager.GetGuiType(_CurrentHighLightElement);
            bool _IsExeplorer = IsExplorer(_CurrentHighLightElement);//暂时不处理系统判断逻辑
            System.Drawing.Rectangle _Rectangle = _CurrentHighLightElement.BoundingRectangle;
            string _TargetName = GetCNLabelForElement(_CurrentHighLightElement.GuiNodeControlName());
            ElementHighLighterParam _ElementHighLighterParam = new ElementHighLighterParam();
            _ElementHighLighterParam.CRectangle = _Rectangle;
            _ElementHighLighterParam.CTagName = _TargetName;
            _ElementHighLighterParam.EGuiType = _aGuiType;
            _ElementHighLighterParam.IsExeplorer = _IsExeplorer;
            return _ElementHighLighterParam;
        }

    }
}
