using II.RPA.Logging;
using II.RPA.Plugin.GUI.ElementHighlight;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using static II.RPA.Plugin.GUI.ElementHighlight.HighlightUtils;

namespace II.RPA.Plugin.GUI.HighLight
{
    public class HighLightManager
    {
        private static ILogger logger = Logger.OfContext();

        #region 核心构造函数
        public HighLightManager()
        {
            HighLightDispatcher = Dispatcher.CurrentDispatcher;
        }

        Dispatcher HighLightDispatcher;

        public void SetDispatcher(Dispatcher _Dispatcher)
        {
            HighLightDispatcher = _Dispatcher;
        }

        //HighLightWnd _HighLightWnd = null;
        //HighLightAgentWnd _HighLightWnd = null;
        IHighlightWindow _HighLightWnd = null;
        #endregion

        #region 初始化 Init()
        public void Init()
        {
            InitTopmostDispatcher();
        }
        #endregion

        #region 反初始化 Deint()
        public void Deint()
        {
            DisposeTopmostDispatcher();
        }
        #endregion

        /// <summary>
        /// 高亮函数（创建或者移动窗口） 核心功能
        /// </summary>
        /// <param name="_HighLightParam"></param>
        #region HighLight(HighLightParam _HighLightParam)

        bool IsBusy = false;
        double factor = 0.0;
        public void HighLight(HighLightParam _HighLightParam)
        {
            // 显示等待提示
            if (IsOnHide)
            {
                return;
            }

            IsBusy = true;
            if (LastHighLightParam.Equal(_HighLightParam))
            {
                //logger.LogInformation("重复渲染 忽略");
                return;
            }

            LastHighLightParam = _HighLightParam;

            NeedStopAiLoading(_HighLightParam.m_Rectangle);

            try
            {
                try
                {
                    // 在 UI 线程上更新 UI
                    HighLightDispatcher.InvokeAsync((Action)delegate ()
                    {
                        Dpi? _Dpi = GuiElement.overlayForm?.inspectorStatus?.GetWPFDpi();
                        if (_Dpi != null)
                        {
                            factor = ((Dpi)_Dpi).X / 96.0;
                        }

                        if (factor != 0)
                        {
                            _HighLightParam.m_Rectangle = GetVisiualRect(_HighLightParam.m_Rectangle, factor, _HighLightParam.UseTightBorder);
                        }

                        if (_HighLightWnd == null)
                        {
                            if (GuiElement.overlayForm.IsAgentCapture)
                                _HighLightWnd = new HighLightAgentWnd();
                            else
                                _HighLightWnd = new HighLightWnd();
                            _HighLightWnd.SetPosionAndBorderSize(_HighLightParam.m_Rectangle);
                            _HighLightWnd.Show();
                            Init();
                        }
                        else
                        {
                            //_HighLightWnd.RefreshStatus(_HighLightParam);
                            _HighLightWnd.SetPosionAndBorderSize(_HighLightParam.m_Rectangle);
                            ShowWnd();
                        }

                        if (factor != 0)
                        {
                            Rectangle borderRect = new Rectangle(), tagRect = new Rectangle();
                            _HighLightWnd.GetBorderRectAndTagRect(_HighLightParam, ref borderRect, ref tagRect);

                            //Rectangle rectangle = new Rectangle((int)(_HighLightWnd.Left), (int)(_HighLightWnd.Top), (int)_HighLightWnd.ActualWidth, (int)_HighLightWnd.ActualHeight);
                            //CurrentBorderRectangle = HighlightUtils.ResizeRetangle(borderRect, 3);
                            CurrentBorderRectangle = HighlightUtils.UpScale(borderRect, factor);
                            CurrentTagRectangle = HighlightUtils.UpScale(tagRect, factor);

                            //logger.LogDebug($"CV抠图 Border:{CurrentBorderRectangle.ToString()} Tag:{CurrentTagRectangle.ToString()}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString());
                }
            }
            finally
            {
                // 隐藏等待提示
                IsBusy = false;
            }
        }

        public void Flash(Rectangle rectangle, bool useTightBorder = false)
        {
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    FlashOneTime(rectangle, useTightBorder);
                    Task.Delay(200).Wait();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }


        public void FlashOneTime(Rectangle rectangle, bool useTightBorder = false)
        {
            HighLightDispatcher.Invoke((Action)delegate ()
            {
                Dpi? _Dpi = GuiElement.overlayForm?.inspectorStatus?.GetWPFDpi();
                if (_Dpi != null)
                {
                    factor = ((Dpi)_Dpi).X / 96.0;
                }

                if (factor != 0)
                {
                    rectangle = GetVisiualRect(rectangle, factor, useTightBorder);
                }

                if (_HighLightWnd == null)
                {
                    if (GuiElement.overlayForm.IsAgentCapture)
                        _HighLightWnd = new HighLightAgentWnd();
                    else
                        _HighLightWnd = new HighLightWnd();
                    _HighLightWnd.SetBorderBorderBrush(ElementType.None);
                    _HighLightWnd.SetPosionAndBorderSize(rectangle, true);
                    _HighLightWnd.Show();
                }
                else
                {
                    _HighLightWnd.SetPosionAndBorderSize(rectangle, true);
                    ShowWnd();
                }

                Task.Delay(100).Wait();

                _HighLightWnd.Close();
                _HighLightWnd = null;
            });
        }


        public void SetTagContent(string strTagName)
        {
            HighLightDispatcher.InvokeAsync(() =>
            {
                _HighLightWnd?.SetTagContent(strTagName);
            });
        }

        public void SetTagIcon(ElementType _ElementType, eGuiType _eGuiType)
        {
            string strTagIcon = HighLightIconTraitor.GetIconString(_ElementType, _eGuiType);
            HighLightDispatcher.InvokeAsync(() =>
            {
                _HighLightWnd?.SetTagIconCode(strTagIcon);
            });
        }
        public void SetBorderBorderBrush(ElementType _ElementType)
        {
            HighLightDispatcher.InvokeAsync(() =>
            {
                _HighLightWnd?.SetBorderBorderBrush(_ElementType);
            });
        }
        /// <summary>
        /// 修正提供的框体大小（抵消缩放影响）
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        private Rectangle GetVisiualRect(Rectangle rect, double factor = 1.0, bool useTightBorder = false)
        {
            Rectangle rectangle2 = rect.Downscale(factor);

            if (useTightBorder)
            {
                return rectangle2;
            }

            //logger.LogDebug($"校验 RePositionAndResize{rectangle2.ToJson()} 缩放{factor}");
            if ((double)(rectangle2.Width + 4) <= SystemParameters.WorkArea.Width)
            {
                rectangle2.Inflate(4, 0);
            }
            if ((double)(rectangle2.Height + 4) <= SystemParameters.WorkArea.Height)
            {
                rectangle2.Inflate(0, 4);
            }

            return rectangle2;
        }
        #endregion

        /// <summary>
        /// 设置window背景色 透明/不透明
        /// </summary>
        /// <param name="anIsTranparent"></param>
        #region Ctrl高亮 （蓝色）
        public void SetBackgroundActive(bool IsTranparent)
        {
            //HighLightDispatcher.InvokeAsync(() =>
            //HighLightDispatcher.Invoke(() =>
            //{
            //    this.CHLWndViewModel.SetBackgroundActive(!IsTranparent);
            //    //this.CHLWndViewModel.BorderBackground = new SolidColorBrush(Colors.Green);
            //});

            HighLightDispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (_HighLightWnd == null)
                    {
                        return;
                    }
                    else
                    {
                        _HighLightWnd.SetBackgroundActive(IsTranparent);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"HighLightWnd SetBackgroundActive ex:{ex.ToString()}");
                }
            });
        }
        #endregion

        #region 高亮窗口穿透
        public void SetWndPenetrate(bool IsPenetrate)
        {
            HighLightDispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (_HighLightWnd == null)
                    {
                        return;
                    }
                    else
                    {
                        _HighLightWnd.SetWndPenetrate(IsPenetrate);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"HighLightWnd SetWndPenetrate ex:{ex.ToString()}");
                }
            });
        }
        #endregion

        #region 隐藏显示窗口(CV截图时)
        public bool IsOnHide = false;

        public void Hide()
        {
            IsOnHide = true;
            HighLightDispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (_HighLightWnd == null)
                    {
                        return;
                    }
                    else
                    {
                        _HighLightWnd.SetWndState(false);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"HighLightWnd Hide ex:{ex.ToString()}");
                }
            });
        }

        public void UndoHide()
        {
            IsOnHide = false;
            HighLightDispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (_HighLightWnd == null)
                    {
                        return;
                    }
                    else
                    {
                        _HighLightWnd.SetWndState(true);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"HighLightWnd UndoHide ex:{ex.ToString()}");
                }
            });
        }
        #endregion

        /// <summary>
        /// 显示并置顶窗口
        /// </summary>
        #region 显示并置顶窗口 ShowWnd()
        public void ShowWnd()
        {
            if (!_HighLightWnd.IsActive)
            {
                _HighLightWnd.Show();
            }

            if (!_HighLightWnd.Topmost)
            {
                _HighLightWnd.Topmost = true;
            }
        }
        #endregion

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="isFinal"></param>
        #region 关闭窗口 Close
        public void Close(bool isFinal = false)
        {
            HighLightDispatcher.InvokeAsync(() =>
            {
                if (isFinal)
                {
                    Deint();
                    _HighLightWnd?.Close();
                    _HighLightWnd = null;
                }
                else
                {
                    Deint();
                    Hide();
                }
            });
        }

        #endregion

        /// <summary>
        /// 选定基准元素后的拾取UI变化
        /// </summary>
        public void ShowTarsToolsPanel()
        {
            _HighLightWnd?.SetToolPanelVisible();
        }
        public void SetZoomControlsEnableStatus(bool isEnbale)
        {
            _HighLightWnd?.SetZoomControlsShowStatus(isEnbale);
        }

        #region Public 变量
        private Rectangle m_CurrentBorderRectangle = Rectangle.Empty;
        /// <summary>
        /// 当前高亮框 用于CV抠图判断
        /// </summary>
        public Rectangle CurrentBorderRectangle
        {
            get { return m_CurrentBorderRectangle; }
            set { m_CurrentBorderRectangle = value; }
        }

        private Rectangle m_CurrentTagRectangle = Rectangle.Empty;
        /// <summary>
        /// 高亮标签框体  用于CV抠图判断
        /// </summary>
        public Rectangle CurrentTagRectangle
        {
            get { return m_CurrentTagRectangle; }
            set { m_CurrentTagRectangle = value; }
        }

        private Rectangle m_LastRectangle = Rectangle.Empty;
        /// <summary>
        /// 存储最新的高亮框体 用于判断是否重复渲染
        /// </summary>
        public Rectangle LastRectangle
        {
            get { return m_LastRectangle; }
            set { m_LastRectangle = value; }
        }

        private string m_TagName = string.Empty;
        /// <summary>
        /// 判断当前高亮标签内容
        /// </summary>
        public string LastTagName
        {
            get { return m_TagName; }
            set { m_TagName = value; }
        }

        private HighLightParam m_HighLightParam = new HighLightParam();
        /// <summary>
        /// 判断当前高亮参数 用于比较区别防止重复渲染
        /// </summary>
        public HighLightParam LastHighLightParam
        {
            get { return m_HighLightParam; }
            set { m_HighLightParam = value; }
        }
        #endregion


        #region 置顶高亮窗体相关
        DispatcherTimer m_TopmostDP;

        private void InitTopmostDispatcher()
        {
            if (m_TopmostDP != null)
            {
                DisposeTopmostDispatcher();
            }

            m_TopmostDP = new DispatcherTimer();
            m_TopmostDP.Interval = TimeSpan.FromMilliseconds(2200);
            m_TopmostDP.Tick += M_TopmostDP_Tick;
            m_TopmostDP.Start();
            //logger.LogInformation("开始置顶线程");
        }

        private void DisposeTopmostDispatcher()
        {
            if (m_TopmostDP == null)
            {
                return;
            }

            m_TopmostDP.Tick -= M_TopmostDP_Tick;
            m_TopmostDP?.Stop();
            m_TopmostDP = null;
            //logger.LogInformation("关闭置顶线程");
        }

        private void M_TopmostDP_Tick(object sender, EventArgs e)
        {
            HighLightDispatcher.InvokeAsync(() =>
            {
                SetTopmost();
            });
        }

        public void SetTopmost()
        {
            HighLightDispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (_HighLightWnd == null)
                    {
                        return;
                    }
                    else
                    {
                        _HighLightWnd.SetTopMost();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }
        #endregion


        #region WPF窗体缩放计算
        public Dpi TransformToPixels(Visual aVisual)
        {
            Matrix matrix;
            var source = PresentationSource.FromVisual(aVisual);
            if (source != null)
            {
                matrix = source.CompositionTarget.TransformToDevice;
            }
            else
            {
                using (var src = new HwndSource(new HwndSourceParameters()))
                {
                    matrix = src.CompositionTarget.TransformToDevice;
                }
            }

            double pixelX = (int)(matrix.M11 * 96.0);
            double pixelY = (int)(matrix.M22 * 96.0);
            return new Dpi(pixelX, pixelY);
        }
        #endregion

        #region AI CV解析扫描动画调用

        AiLoadingRect CurrentAiLoadingRect = new AiLoadingRect();


        public void StartAILoading()
        {
            CurrentAiLoadingRect.IsAiLoadingRunning = true;
            HighLightDispatcher.InvokeAsync(() =>
            {
                _HighLightWnd?.StartAILoading();
            });
        }

        public void StopAILoading(bool IsAsync = true)
        {
            CurrentAiLoadingRect.Init();
            if (IsAsync)
            {
                HighLightDispatcher.InvokeAsync(() =>
                {
                    _HighLightWnd?.StopAILoading();
                });
            }
            else
            {
                HighLightDispatcher.Invoke(() =>
                {
                    _HighLightWnd?.StopAILoading();
                });
            }
        }

        public void CheckCloseAilLoading()
        {
            if (CurrentAiLoadingRect.IsAiLoadingRunning)
            {
                StopAILoading(false);
            }
        }

        public void NeedStopAiLoading(Rectangle _rectangle)
        {
            if (CurrentAiLoadingRect.IsAiLoadingRunning == true)
            {
                if (!CurrentAiLoadingRect.HasSetValue)
                {
                    CurrentAiLoadingRect.RecordCurrentRect(_rectangle);
                    return;
                }

                if (CurrentAiLoadingRect.DiffrentRect(_rectangle))
                {
                    StopAILoading();
                    return;
                }
            }
        }
        #endregion
    }
}
