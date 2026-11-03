using AForge;
using AForge.Imaging;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.ElementHighlight;
using Newtonsoft.Json.Linq;
using II.RPA.WinAutomation.CommonUtility.Utils;

namespace II.RPA.Plugin.GUI
{
    class CVCaptureTask : CaptureTask<CVCaptureTask, IGuiNode>
    {
        private static ILogger logger = Logger.OfContext();
        private bool _isSwitchApp = false;
        private Rectangle _curWndArea = Rectangle.Empty;
        private Bitmap _appShotImg = null;
        private DateTime _lastCapturedTime = DateTime.Now;

        private bool ZBrainAvailableStatus = false;
        protected override string Name => "CV鍏冪礌鎷惧彇浠诲姟";
        internal bool IsCVParsing { get; set; } = false;
        internal bool IsRefrush { get; set; } = false;
        internal IntPtr CurrentWndHendler { get; set; } = IntPtr.Zero;

        private Bitmap AppShotImg
        {
            set
            {
                _appShotImg = value;
            }
            get
            {
                return _appShotImg;
            }
        }

        internal CVCaptureTask() : base()
        {
            this.Tick = CVCaptureTask_Tick;
            this.TickOnceCallback = CVCaptureTask_TickOnceCallback;
            this.Interval = 200;
        }
        internal override void Start()
        {
            //if (this.IsStart)
            //{
            //    return;
            //}
            if (!GuiElement.overlayForm.IsCvSupport)
            {
                return;
            }
            ZBrainAvailableStatus = false;
            base.Start();
            CVElementProvider.Instance.Start();
        }
        internal override void Abort()
        {
            try
            {
                //this.Tick -= CVCaptureTask_Tick;
                //this.TickOnceCallback -= CVCaptureTask_TickOnceCallback;
                CVElementProvider.Instance.Stop();
                base.Abort();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"{Name}:缁堟寮傚父");
            }
        }
        public override void Pause()
        {
            base.Pause();
            CVElementProvider.Instance.Pause();
        }

        public override void Resume()
        {
            base.Resume();
            CVElementProvider.Instance.Resume();
        }
        public override void Init()
        {
            base.Init();
            CVElementProvider.Instance.Init();
        }
        bool IsFirstCVParse = true;

        AutomationElement m_DesktopElement = null;
        private AutomationElement GetDeskTopElement()
        {
            if (m_DesktopElement == null)
            {
                m_DesktopElement = UiCommon.UIAAutomation.GetDesktop();
            }
            return m_DesktopElement;
        }

        internal void CVCaptureTask_Tick(object sender, EventArgs e)
        {
            if (GuiElement.overlayForm.CancellationForCapture.IsCancellationRequested)
            {
                Task.Delay(200).Wait();
                return;
            }

            if (!IsTaskEnable)
            {
                Task.Delay(200).Wait();
                return;
            }

            if (!ZBrainAvailableStatus)
            {
                var isAvailable = ZbrainAPIService.IsZBrainAvailable();
                if (!isAvailable)
                {
                    Task.Delay(1000).Wait();
                    return;
                }
                ZBrainAvailableStatus = true;
            }
            TimeTrackHelper timeTrackHelper = new TimeTrackHelper();

            try
            {
                timeTrackHelper.Start("CVCaptureTask_Tick", TimeTrackLogLevel.info);

                AutomationElement rootElement = GuiElement.overlayForm.CaptureRootWnd;

                if (rootElement == null)
                {
                    logger.LogDebug("CVCaptureTask_Tick 鏈娴嬪埌CaptureRootWnd");
                    return;
                }
                bool _isRefrush = false;
                IntPtr eleHandle = IntPtr.Zero;
                var cvRootElement = GuiManager.CreateCVGuiNode(rootElement);
                eleHandle = cvRootElement.WindowHandle;

                // if (rootElement.Properties.NativeWindowHandle.IsSupported)
                //{
                //    eleHandle = rootElement.Properties.NativeWindowHandle.Value;
                //}

                if (IsRefrush)
                {
                    _isRefrush = true;
                    this.IsRefrush = false;
                }
                else if (GuiElement.overlayForm.CvCaptureRootNode != null && !_isSwitchApp && AppShotImg != null)
                {
                    if (eleHandle != IntPtr.Zero && GuiElement.overlayForm.CvCaptureRootNode.WindowHandle == eleHandle)
                    {
                        timeTrackHelper.Pick($"CVCaptureTask_Tick DetectionSimilarity start", TimeTrackLogLevel.info);
                        var isRecapture = DetectionSimilarity(cvRootElement);
                        timeTrackHelper.Pick($"CVCaptureTask_Tick DetectionSimilarity End isRecapture:{isRecapture}", TimeTrackLogLevel.info);

                        if (!isRecapture)
                        {
                            return;
                        }
                    }
                    else
                    {
                        logger.LogDebug("鼠标切换了窗口，重新检测。");
                        Thread.Sleep(1);
                        Task.Delay(500).Wait();
                        //Thread.Sleep(500);
                        _isSwitchApp = true;
                        CVCaptureTask_Tick(sender, e);
                        return;
                    }
                }

                if (!IsFirstCVParse)
                {
                    Task.Delay(1000).Wait();
                }

                _isSwitchApp = false;
                timeTrackHelper.Pick($"CVCaptureTask_Tick ShotAppfaceImg start", TimeTrackLogLevel.info);
                var shotSuc = ShotAppfaceImg(cvRootElement);
                timeTrackHelper.Pick($"CVCaptureTask_Tick ShotAppfaceImg End shotSuc:{shotSuc}", TimeTrackLogLevel.info);
                if (!shotSuc)
                {
                    this.TickOnceResult = null;
                    return;
                }
                if (!this.IsStart)
                {
                    return;
                }

                GuiElement.overlayForm.inspectorStatus?.SetCVCaptureMsg("寮€濮嬫櫤鑳借瘑鍒?..");

                if (IsNeedAILoading(GuiElement.overlayForm.LastHighlightElement, cvRootElement, rootElement))
                {
                    GuiElement.overlayForm.CurrentElementHighlighterManager.StartAILoading();
                }

                ((IGuiCVNode590)cvRootElement).StopCVServerCall();
                //cvRootElement.CacheImage.Save(@"D:\123.png");
                if (((IGuiCVNode650)cvRootElement).CVParse(cvRootElement.CacheImage, eleHandle, _isRefrush)) //CV瑙ｆ瀽
                {
                    if (!this.IsStart)
                    {
                        return;
                    }
                    this.TickOnceResult = cvRootElement;
                    GuiElement.overlayForm.CurrentElementHighlighterManager.StopAILoading();
                }
                else
                {
                    this.TickOnceResult = null;
                }
                _lastCapturedTime = DateTime.Now;
                if (rootElement.Properties.NativeWindowHandle.IsSupported)
                {
                    this.CurrentWndHendler = rootElement.Properties.NativeWindowHandle;
                }
                else
                {
                    CurrentWndHendler = IntPtr.Zero;
                }
            }
            catch (RPAException ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "CV识别检测服务发生一次异常。");
                if (ex.ErrorMessageType == eErrorMessageType.Error_CVElementServiceError)
                {
                    this.ZBrainAvailableStatus = false;
                }
            }
            catch (System.ArgumentException ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "CV拾取程序发生了异常。");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "CV拾取程序发生了异常。");
            }
            finally
            {
                timeTrackHelper.Stop("CVCaptureTask_Tick");
                IsCVParsing = false;//瑙ｆ瀽瀹屾垚
                GuiElement.overlayForm.inspectorStatus?.SetCVCaptureMsg("");
                GuiElement.overlayForm.CurrentElementHighlighterManager.StopAILoading();
                //this.StopAfterExecute();
            }

        }

        /// <summary>
        /// 鏄惁鏈湴涓存椂瀛樺浘 娴嬭瘯CV鍙栧浘锛堟埅鍥俱€佹姞鍥撅級
        /// </summary>
        bool NeedTestImgCapture = false;

        private bool ShotAppfaceImg(IGuiCVNode cvRootElement)
        {
            if (GuiElement.overlayForm != null && GuiElement.overlayForm.inspectorStatus != null)
            {
                GuiElement.overlayForm.inspectorStatus.IsOnCVScreenshot = true;
            }
            TimeTrackHelper timeTrackHelper = new TimeTrackHelper();
            timeTrackHelper.Start("ShotAppfaceImg");

            logger.LogDebug("==============start SetWndHide");

            GuiElement.overlayForm?.CurrentElementHighlighterManager?.CheckCloseAilLoading();

            GuiElement.overlayForm?.inspectorStatus?.SetWndHide();

            GuiElement.overlayForm?.SetWndHide();


            logger.LogDebug("==============end SetWndHide");

            logger.LogDebug("==============start ElementToBitmap");

            if (GuiElement.overlayForm != null && GuiElement.overlayForm.inspectorStatus != null)
            {
                GuiElement.overlayForm.inspectorStatus.IsOnCVScreenshot = true;
            }

            timeTrackHelper.Pick("ShotAppfaceImg SetWndHide");
            ZbrainAPIService.NotifyGetWindowImg(cvRootElement.WindowHandle);
            timeTrackHelper.Pick("ShotAppfaceImg NotifyGetWindowImg");

            try
            {
                using (var rootEleImg = cvRootElement.ElementToBitmap())
                {
                    if (rootEleImg == null)
                    {
                        return false;
                    }

                    timeTrackHelper.Pick("ShotAppfaceImg ElementToBitmap");

                    if (NeedTestImgCapture)
                    {
                        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempimg1");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        string name = DateTime.Now.Ticks.ToString();
                        rootEleImg.Save(Path.Combine(path, $@"{name}_ElementToBitmap.png"));
                    }

                    var old_AppShotImg = AppShotImg;
                    var old_cvRootElement_CacheImage = cvRootElement.CacheImage;
                    AppShotImg = ImageHelper.CaptureImage(rootEleImg);
                    DisposeImage(old_AppShotImg);
                    cvRootElement.CacheImage = ImageHelper.CaptureImage(rootEleImg);
                    timeTrackHelper.Pick("ShotAppfaceImg CaptureImage");
                    DisposeImage(old_cvRootElement_CacheImage);
                }
                return true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "截取并创建图片副本异常。");
            }
            finally
            {
                #region 淇濆瓨瑕佽瘑鍒殑鍘熷浘锛屼笌绠楁硶鑱旇皟浣跨敤

                //try
                //{
                //    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempimg");
                //    if (!Directory.Exists(path))
                //    {
                //        Directory.CreateDirectory(path);
                //    }
                //    AppShotImg.Save(Path.Combine(path, DateTime.Now.Ticks.ToString() + ".png"));
                //}
                //catch (Exception)
                //{

                //}

                #endregion
                if (GuiElement.overlayForm != null && GuiElement.overlayForm.inspectorStatus != null)
                {
                    GuiElement.overlayForm.inspectorStatus.IsOnCVScreenshot = false;
                }
                logger.LogDebug("==============end ElementToBitmap");
                logger.LogDebug("==============start SetWndShow");

                GuiElement.overlayForm.inspectorStatus?.SetWndShow();


                GuiElement.overlayForm?.SetWndShow();
                logger.LogDebug("==============end SetWndShow");
                timeTrackHelper.Pick("ShotAppfaceImg SetWndShow");
                timeTrackHelper.Stop("ShotAppfaceImg");
            }
            return false;
        }

        private void DisposeImage(Bitmap img)
        {
            try
            {
                img?.Dispose();
            }
            catch
            {

            }
        }


        private bool IsNeedAILoading(IGuiNode guiNode, IGuiCVNode _CvCaptureRootNode, AutomationElement _CaptureRootWnd)
        {

            //logger.LogDebug($"IsNeedAILoading guiNode锛歿((IGuiNode_6_1_0)guiNode).GuiType} {((IGuiNode_6_1_0)guiNode).BoundingRectangle.ToJson()} {((IGuiNode_6_1_0)guiNode).ToJson()} " +
            //    $"_CvCaptureRootNode锛歿_CvCaptureRootNode.BoundingRectangle.ToJson()}{_CvCaptureRootNode.ToJson()}");

            try
            {
                //if (m_IsActionPopupOpen || !string.IsNullOrEmpty(m_SpecificTagText))
                //{
                //    return false;
                //}

                if (!(guiNode is IGuiCVNode))
                {
                    logger.LogDebug($"IsNeedAILoading IS IGuiCVNode");
                    if (!(guiNode is IGuiNode_6_1_0))
                    {
                        logger.LogDebug($"IsNeedAILoading IS Not IGuiNode_6_1_0");
                        return false;
                    }
                    logger.LogDebug($"IsNeedAILoading IS IGuiNode_6_1_0");
                    if (((IGuiNode_6_1_0)guiNode).GuiType != eGuiType.Windows)
                    {
                        logger.LogDebug($"IsNeedAILoading IS Not eGuiType.Windows");
                        return false;
                    }
                    logger.LogDebug($"IsNeedAILoading IS eGuiType.Windows");



                    //var currentProcessId = CurrentAutomationElement.Properties.ProcessId.ValueOrDefault;
                    //logger.LogInformation($"processexe:{currentProcessId}");
                    //if (m_ExplorerPIDs.Contains(currentProcessId))
                    //{
                    //    return false;
                    //}

                    //if (_CvCaptureRootNode != null
                    //    && _CvCaptureRootNode.WindowHandle != IntPtr.Zero
                    //    && _CvCaptureRootNode.WindowHandle == _CaptureRootWnd.Properties.NativeWindowHandle.ValueOrDefault)
                    //{
                    //    logger.LogDebug($"IsNeedAILoading _CvCaptureRootNode 闈炵┖");
                    //    return false;
                    //}

                    //logger.LogDebug($"IsNeedAILoading _CvCaptureRootNode 涓虹┖");
                    var currentarea = guiNode.BoundingRectangle.Width * guiNode.BoundingRectangle.Height;
                    var rootarea = _CvCaptureRootNode.BoundingRectangle.Width * _CvCaptureRootNode.BoundingRectangle.Height;
                    if (rootarea > 100 && currentarea > 0.95 * rootarea)
                    {
                        //logger.LogDebug($"IsNeedAILoading 闈㈢Н婊¤冻");
                        //logger.LogWarning($"{CaptureRootWnd.Properties.Name.ValueOrDefault}:{rootarea};{currentarea}");
                        if (Cursor.Position.X - _CvCaptureRootNode.BoundingRectangle.X > 80
                            && Cursor.Position.Y - _CvCaptureRootNode.BoundingRectangle.Y > 80)
                        {
                            var DeskTopElementRect = GetDeskTopElement().BoundingRectangle;
                            if (guiNode.BoundingRectangle == DeskTopElementRect
                                || _CaptureRootWnd.BoundingRectangle == DeskTopElementRect
                                || guiNode.BoundingRectangle.Width >= DeskTopElementRect.Width)
                            {
                                return false;
                            }

                            //logger.LogDebug($"IsNeedAILoading 璺濈婊¤冻");
                            //if (_CvCaptureRootNode == null
                            //    || _CvCaptureRootNode.WindowHandle == IntPtr.Zero
                            //    || _CvCaptureRootNode.WindowHandle != _CaptureRootWnd.Properties.NativeWindowHandle.ValueOrDefault)
                            {
                                //logger.LogDebug($"IsNeedAILoading _CvCaptureRootNode 涓虹┖");
                                logger.LogDebug($"IsNeedAILoading:{guiNode.GetType().FullName},{((IGuiNode_6_1_0)guiNode).GuiType}");
                                return true;
                            }
                            //logger.LogDebug($"IsNeedAILoading _CvCaptureRootNode 闈炵┖");
                        }
                    }
                }
                else
                {
                    return false;
                    //if (CurrentCvCaptureRootNode != null
                    //    && (CurrentCvCaptureRootNode.WindowHandle == IntPtr.Zero
                    //  || CurrentCvCaptureRootNode.WindowHandle != CaptureRootWnd.Properties.NativeWindowHandle.ValueOrDefault))
                    //{
                    //    //logger.LogInformation("IsNeedAILoading:cv element but root node is null");
                    //    return true;
                    //}
                }
                return false;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "IsNeedAILoading Error");
                return false;
            }
        }

        /// <summary>
        /// 妫€娴嬬浉浼煎害
        /// </summary>
        /// <param name="cvRootElement"></param>
        /// <returns>true 鐩镐技搴︿綆浜庨槇鍊硷紝闇€瑕侀噸鏂帮紝false 澶т簬绛変簬闃堝€?/returns>
        private bool DetectionSimilarity(IGuiCVNode cvRootElement)
        {
            // var rootElementBoundRect = UIAExcuteTask.Send(() =>
            //{
            //    return rootElement.Properties.BoundingRectangle.Value;
            //},0);
            JObject areaInfo = new JObject();
            var rootElementBoundRect = cvRootElement.BoundingRectangle;
            areaInfo.Add("cur_wnd_area", JToken.FromObject(new[] { rootElementBoundRect.X, rootElementBoundRect.Y, rootElementBoundRect.Width, rootElementBoundRect.Height }));
            //瀹介珮鐩稿悓
            if (_curWndArea.Width == rootElementBoundRect.Width && _curWndArea.Height == rootElementBoundRect.Height)
            {
                var span = (DateTime.Now - _lastCapturedTime).TotalMilliseconds;
                if (span < Configurations.Info.AppFaceProximityMonitorInterval)
                {
                    return false;
                }
                Bitmap eleShotImg_clear = null;
                Bitmap newAppShotIm_clear = null;
                Bitmap eleShotImg = null;
                try
                {
                    //UIAExcuteTask.Send(() =>
                    //{
                    //GuiElement.overlayForm?.SetWndHide();
                    //GuiElement.overlayForm?.inspectorStatus?.SetWndHide();
                    var beforScreentshotRect = GuiElement.overlayForm.LastHighlightRect;
                    var lastTagInfoRect = GuiElement.overlayForm.LastTagInfoRect;
                    //logger.LogDebug($"CV鎶犲浘 beforScreentshotRect:{beforScreentshotRect.ToString()} lastTagInfoRect:{lastTagInfoRect.ToString()}");
                    using (eleShotImg = cvRootElement.ElementToBitmap())
                    {
                        //}, 0);
                        //GuiElement.overlayForm?.SetWndShow();
                        //GuiElement.overlayForm?.inspectorStatus?.SetWndShow();
                        if (eleShotImg == null) return false;
                        var afterScreentshotRect = GuiElement.overlayForm.LastHighlightRect;
                        if (!beforScreentshotRect.Equals(afterScreentshotRect))
                        {
                            return false;//涓嶆槑纭埅鍥句腑鐨勯珮浜尯鍩熶綅缃?
                        }
                        //if (OverlayForm.ModifierKeys == Keys.Control)//鎸変笅Ctrl 閫忔槑搴?.9

                        if (KeyboardHelper.GetKeyPressState(VirtualKeyShort.CONTROL))
                        {
                            logger.LogDebug("按下 Ctrl，不对比界面变化。");
                            return false;
                        }
                        //eleShotImg.Save(@"D:\123.png");
                        Rectangle statusWndRect = GuiElement.overlayForm.inspectorStatus.GetWndRectangle();


                        beforScreentshotRect = new Rectangle
                        {
                            X = beforScreentshotRect.X - rootElementBoundRect.X,
                            Y = beforScreentshotRect.Y - rootElementBoundRect.Y,
                            Width = beforScreentshotRect.Width,
                            Height = beforScreentshotRect.Height
                        };
                        beforScreentshotRect.Inflate(-1, -1);

                        lastTagInfoRect.X = lastTagInfoRect.X - rootElementBoundRect.X;
                        lastTagInfoRect.Y = lastTagInfoRect.Y - rootElementBoundRect.Y;



                        using (var newAppShotImg = FillHighlightArea(beforScreentshotRect, lastTagInfoRect, AppShotImg, ref eleShotImg))
                        {
                            if (NeedTestImgCapture)
                            {
                                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempimg2");
                                if (!Directory.Exists(path))
                                {
                                    Directory.CreateDirectory(path);
                                }
                                string name = DateTime.Now.Ticks.ToString();
                                newAppShotImg.Save(Path.Combine(path, $@"{name}_newAppShotImg.png"));
                                eleShotImg.Save(Path.Combine(path, $@"{name}_eleShotImg.png"));

                            }
                            //bool isOverlap = IsOverlap(statusWndRect, rootElementBoundRect);
                            float sx = GetSX().Result;
                            statusWndRect = new Rectangle()
                            {
                                X = (int)(statusWndRect.X * sx),
                                Y = (int)(statusWndRect.Y * sx),
                                Width = (int)(statusWndRect.Width * sx),
                                Height = (int)(statusWndRect.Height * sx)
                            };
                            //logger.LogDebug($"statusWndRect锛歿statusWndRect}锛宺ootElementBoundRect锛歿rootElementBoundRect}");
                            Rectangle overlapRect = Rectangle.Intersect(statusWndRect, rootElementBoundRect);
                            if (overlapRect != Rectangle.Empty)
                            {
                                //logger.LogDebug($"overlapRect锛歿overlapRect}");
                                //float sx = PrimaryScreen.DpiX / 96F;
                                //float sy = PrimaryScreen.DpiY / 96F;


                                int x, y;
                                if (rootElementBoundRect.X <= 0 && rootElementBoundRect.Y <= 0)
                                {
                                    x = (int)(overlapRect.X - rootElementBoundRect.X);
                                    y = (int)(overlapRect.Y - rootElementBoundRect.Y);
                                }
                                else
                                {
                                    x = (int)((overlapRect.X - rootElementBoundRect.X));
                                    y = (int)((overlapRect.Y - rootElementBoundRect.Y));
                                }
                                overlapRect = new Rectangle
                                {
                                    X = x,
                                    Y = y,
                                    Width = (int)(overlapRect.Width),
                                    Height = (int)(overlapRect.Height)
                                };
                                //logger.LogDebug($"overlapRect锛歿overlapRect}");
                                eleShotImg_clear = ImageHelper.AreaClear(eleShotImg, overlapRect);
                                newAppShotIm_clear = ImageHelper.AreaClear(newAppShotImg, overlapRect);

                            }
                            //涓轰簡缂╃煭zbrain鎷垮埌鏁版嵁鐨勬椂闂村樊锛岃繖閲岄噸鏂拌幏鍙栦竴閬?
                            beforScreentshotRect = GuiElement.overlayForm.LastHighlightRect;
                            //lastTagInfoRect = GuiElement.overlayForm.LastTagInfoRect;
                            statusWndRect = GuiElement.overlayForm.inspectorStatus.GetWndRectangle();
                            areaInfo.Add("highlight_ele_area", JToken.FromObject(new[] { beforScreentshotRect.X, beforScreentshotRect.Y, beforScreentshotRect.Width, beforScreentshotRect.Height }));

                            lastTagInfoRect.X = lastTagInfoRect.X + rootElementBoundRect.X;
                            lastTagInfoRect.Y = lastTagInfoRect.Y + rootElementBoundRect.Y;

                            areaInfo.Add("ele_tag_area", JToken.FromObject(new[] { lastTagInfoRect.X, lastTagInfoRect.Y, lastTagInfoRect.Width, lastTagInfoRect.Height }));
                            //areaInfo.Add("ele_tag_area", JToken.FromObject(new[] { 0, 0, 0, 0 }));
                            areaInfo.Add("status_wnd_area", JToken.FromObject(new[] { statusWndRect.X, statusWndRect.Y, statusWndRect.Width, statusWndRect.Height }));
                            float sim = ZbrainAPIService.ImageSimilarity((eleShotImg_clear ?? eleShotImg), newAppShotIm_clear ?? newAppShotImg, cvRootElement.WindowHandle, areaInfo);
                            //logger.LogDebug($"鐣岄潰鐩镐技搴︽娴嬬粨鏋滐細{sim},闃堝€?{Configurations.Info.AppFaceProximity}");
                            if (sim == -9999 || sim >= Configurations.Info.AppFaceProximity)
                            {
                                return false;
                            }
                            #region AForge.Imaging 鍧楃┓涓炬硶瀵规瘮鐩镐技搴︼紝宸茶鏇挎崲

                            //using (var eleShotImg_new = this.To24bppRgb(eleShotImg_clear ?? eleShotImg))
                            //{
                            //    using (var newAppShotImg_24bppRgb = this.To24bppRgb(newAppShotImg))
                            //    {

                            //        if (eleShotImg_new == null) return false;
                            //        //鍒ゆ柇鏄惁鏄柊鐢婚潰锛堢浉浼煎害锛塻
                            //        bool isRecapture = this.IsNewAppImg(eleShotImg_new, newAppShotImg_24bppRgb);
                            //        if (!isRecapture)
                            //        {
                            //            return false;
                            //        }
                            //        else
                            //        {
                            //            //isNeedForceShowAppWnd = false;
                            //            logger.LogDebug("榧犳爣鎵€鍦ㄧ殑绐楀彛鐣岄潰鍙戠敓浜嗗彉鍖栵紝閲嶆柊妫€娴?);
                            //        }
                            //    }
                            //}

                            #endregion
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    eleShotImg_clear?.Dispose();
                    newAppShotIm_clear?.Dispose();
                }
            }
            else
            {
                //isNeedForceShowAppWnd = false;
                logger.LogDebug("鼠标所在窗口没有发生变化，但窗口大小发生了变化，重新检测。");
            }
            return true;
        }

        private async Task<float> GetSX()
        {
            float sx = await Task.Run(() =>
            {
                return PrimaryScreen.DpiX / 96F;
            });
            return sx;
        }
        /// <summary>
        /// 濉厖楂樹寒鍖哄煙
        /// </summary>
        /// <param name="screenshoteAreaRect"></param>
        /// <param name="oldface"></param>
        /// <param name="hasHighlightAreaImage"></param>
        /// <returns>澶勭悊鎴愬姛杩斿洖姊崇悊鐨勫師鐣岄潰锛屽惁鍒檔ull</returns>
        private Bitmap FillHighlightArea(Rectangle screenshoteAreaRect, Rectangle lastTagInfoRect, Bitmap oldface, ref Bitmap hasHighlightAreaImage)
        {
            Bitmap image = null;
            try
            {
                float sx = PrimaryScreen.DpiX / 96F;
                float sy = PrimaryScreen.DpiY / 96F;
                logger.LogDebug($"sx:{sx}, sy:{sy}");
                //screenshoteAreaRect = new Rectangle
                //{
                //    X = (int)(screenshoteAreaRect.X * sx),
                //    Y = (int)(screenshoteAreaRect.Y * sy),
                //    Width = (int)(screenshoteAreaRect.Width * sx),
                //    Height = (int)(screenshoteAreaRect.Height * sy)
                //};
                //lastTagInfoRect = new Rectangle
                //{
                //    X = (int)(lastTagInfoRect.X * sx),
                //    Y = (int)(lastTagInfoRect.Y * sy),
                //    Width = (int)(lastTagInfoRect.Width * sx),
                //    Height = (int)(lastTagInfoRect.Height * sy)
                //};

                //using (var ms = new MemoryStream())
                //{
                //    oldface.Save(ms, ImageFormat.Bmp);

                ///APPFace 锛堥殣钘忛粦妗嗕笌楂樹寒妗嗗悗鎷垮埌鐨勬埅鍥撅級
                using (var taget_img = ImageHelper.CaptureImage(oldface))
                {
                    image = new Bitmap(taget_img.Width, taget_img.Height);
                    using (Graphics g = Graphics.FromImage(image))
                    {
                        g.DrawImage(taget_img, 0, 0, taget_img.Width, taget_img.Height);
                        //using (Pen pen = new Pen(Color.White, 6))//2->6
                        using (Pen pen = new Pen(Color.White, 6))//2->6
                        {
                            g.DrawRectangle(pen, screenshoteAreaRect);
                        }
                        g.FillRectangle(new SolidBrush(Color.White), lastTagInfoRect);
                    }
                }
                //}

                ///褰撳墠鍏冪礌鎴浘鐨勬姞鍥?
                using (Graphics g = Graphics.FromImage(hasHighlightAreaImage))
                {
                    using (Pen pen = new Pen(Color.White, 6))
                    {
                        g.DrawRectangle(pen, screenshoteAreaRect);
                    }
                    g.FillRectangle(new SolidBrush(Color.White), lastTagInfoRect);
                }
                return image;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "澶勭悊妫€娴嬪浘鐗囨椂鍙戠敓寮傚父 FillHighlightArea");
                if (image != null)
                {
                    image.Dispose();
                }
                return null;
            }
        }
        private void ForceShowAppWnd(AutomationElement rootElement)
        {
            try
            {
                IntPtr eleHandle = IntPtr.Zero;
                if (!rootElement.Properties.NativeWindowHandle.IsSupported)
                {
                    return;
                }
                eleHandle = rootElement.Properties.NativeWindowHandle.Value;
                IntPtr curForegroundHWnd = UiCommon.GetForegroundWindow();
                if (eleHandle == curForegroundHWnd)
                {
                    return;
                }
                UiCommon.ForceShow(eleHandle);
            }
            catch (Exception ex)
            {
                //Trace
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "CV拾取前重置窗口异常，不影响整体操作。");
            }
        }

        private Bitmap To24bppRgb(Bitmap img)
        {
            try
            {

                var bnew = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
                using (Graphics g = Graphics.FromImage(bnew))
                {
                    g.DrawImage(img, 0, 0);
                    g.Dispose();
                }
                return bnew;
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// 鍖归厤缁撴灉
        /// </summary>
        /// <param name="src">鏉ユ簮</param>
        /// <param name="taget">妯℃澘</param>
        private bool IsNewAppImg(Bitmap src, Bitmap taget)
        {
            bool result = false;
            try
            {
                if (src == null || taget == null)
                {
                    return result;
                }
                //using (var ms = new MemoryStream())
                //{
                //    taget.Save(ms, ImageFormat.Bmp);
                //    using (var taget_img = new Bitmap(ms))
                //    {
                List<IntPoint> CurTemplateFeaturePoint = null;
                SusanCornersDetector scd = new SusanCornersDetector();
                if (CurTemplateFeaturePoint == null || CurTemplateFeaturePoint.Count == 0)
                {
                    CurTemplateFeaturePoint = scd.ProcessImage(src); //鑾峰彇妯℃澘鐩稿叧鐐?
                }
                ExhaustiveBlockMatching bm = new ExhaustiveBlockMatching(3, 3);
                bm.SimilarityThreshold = 0.98f;
                // 鎼滅储鍖归厤鐐?
                List<BlockMatch> matches = bm.ProcessImage(src, CurTemplateFeaturePoint, taget);
                int sourceCount = CurTemplateFeaturePoint.Count;
                int matchCount = matches.Count;
                double matchPercent = Math.Round(matchCount / (sourceCount + 0F), 8);
                if (matchPercent < Configurations.Info.AppFaceProximity)
                {
                    result = true;
                    logger.LogDebug($"检测到界面变化，相似度 {matchPercent} 小于 {Configurations.Info.AppFaceProximity}");
                }
                else
                {
                    logger.LogDebug($"鏈娴嬪埌鐣岄潰鍙樺寲锛岀浉浼煎害{matchPercent}");
                }
                //    }
                //}
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "鍥剧墖瀵规瘮寮傚父");
                //taget = null;
            }
            finally
            {
                //src.Dispose();
                //taget.Dispose();
            }
            return result;
        }

        private bool IsOverlap(Rectangle a, Rectangle b)
        {
            int num = Math.Max(a.X, b.X);
            int num2 = Math.Min(a.X + a.Width, b.X + b.Width);
            int num3 = Math.Max(a.Y, b.Y);
            int num4 = Math.Min(a.Y + a.Height, b.Y + b.Height);
            if (num2 >= num && num4 >= num3)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        internal override void StopAfterExecute()
        {
            try
            {
                CVElementProvider.Instance.Stop();
                base.StopAfterExecute();

                if (GuiElement.overlayForm.CvCaptureRootNode != null && GuiElement.overlayForm.CvCaptureRootNode.CacheImage != null)
                {
                    logger.LogDebug("=====Start CacheImage Dispose");
                    //GuiElement.overlayForm.CvCaptureRootNode.CacheImage.Dispose();
                    logger.LogDebug("=====End CacheImage Dispose");
                }
            }
            catch (Exception)
            {
                logger.LogError("鍋滄鎷惧彇CV鎷惧彇寮傚父");
            }
            //GuiElement.overlayForm.CvCaptureRootNode = null;
        }
        internal void CVCaptureTask_TickOnceCallback(IGuiNode guiNode)
        {
            //logger.LogDebug("CaptureTask_TickOnceCallback CV");
            if (!this.IsStart)
            {
                return;
            }
            if (guiNode == null)
            {
                return;
            }
            var old_CvCaptureRootNode = GuiElement.overlayForm.CvCaptureRootNode;
            GuiElement.overlayForm.CvCaptureRootNode = (IGuiCVNode)guiNode;
            _curWndArea = GuiElement.overlayForm.CvCaptureRootNode.BoundingRectangle;
            if (old_CvCaptureRootNode != null && old_CvCaptureRootNode.CacheImage != null)
            {
                logger.LogDebug("=====Start CacheImage Dispose");
                old_CvCaptureRootNode.CacheImage.Dispose();
                logger.LogDebug("=====End CacheImage Dispose");
            }
        }

        internal void ElementCaptureTask_CaptureTaskThreadException(Exception ex)
        {

        }
    }

    public class CVElementProvider
    {
        private readonly static Lazy<CVElementProvider> _instance = new Lazy<CVElementProvider>(() => new CVElementProvider());
        private ILogger logger = Logger.OfContext();
        public static CVElementProvider Instance => _instance.Value;

        private static AutoResetEvent _resetEvent;
        private bool IsStart = false;
        private bool isTaskEnable = false;
        public IGuiNode CurrentCVCaptureNode { get; set; }
        public int NormalContainsCVCount { get; set; }
        public CVElementProvider()
        {
            _resetEvent = new AutoResetEvent(false);
            Task.Factory.StartNew(async () =>
            {
                _resetEvent.WaitOne();
                while (true)
                {
                    while (IsStart)
                    {

                        InspectCVElement();
                        await Task.Delay(10);
                    }
                    this.ResetNodeData();
                    _resetEvent.WaitOne();
                }
            }, TaskCreationOptions.LongRunning);
        }
        public void Start()
        {
            IsStart = true;
            _resetEvent.Set();
        }
        public void Stop()
        {
            IsStart = false;
        }
        public void Init()
        {
            isTaskEnable = true;
        }

        public void Pause()
        {
            isTaskEnable = false;
        }

        public void Resume()
        {
            isTaskEnable = true;
        }
        /// <summary>
        /// InspectCVElement 鍖归厤绗﹀悎榧犳爣鐐圭殑CV鍏冪礌
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="parentNode"></param>
        /// <param name="curOrdinaryRect"></param>
        /// <param name="areaCount"></param>
        /// <returns></returns>
        #region InspectCVElement
        private void InspectCVElement()
        {
            try
            {
                if (!isTaskEnable)
                {
                    return;
                }
                var elementCaptureNode = GuiElement.overlayForm.ElementCaptureNode;
                var captureRootWnd = GuiElement.overlayForm.CaptureRootWnd;
                if (elementCaptureNode == null || captureRootWnd == null)
                {
                    return;
                }
                IGuiCVOperator650 guiCVOperator = GuiManager.CreateGuiCVOperator();
                Dictionary<string, object> extendParams = new Dictionary<string, object>();
                extendParams["areaRect"] = elementCaptureNode.BoundingRectangle;
                extendParams["areaCount"] = 0;
                if (GuiElement.overlayForm.CaptureRootWnd == null)
                {
                    return;
                }
                var screenPoint = System.Windows.Forms.Cursor.Position;
                var cvGuiNode = GuiManager.CreateCVGuiNode(captureRootWnd);
                bool result = guiCVOperator.InspectNode(screenPoint, cvGuiNode, "", extendParams, out IGuiNode inspectNode);
                logger.LogDebug($"areaCount=={extendParams["areaCount"]}");
                if (result == true)
                {
                    CurrentCVCaptureNode = inspectNode;
                    int.TryParse(extendParams["areaCount"].ToString(), out var areaCount);
                    this.NormalContainsCVCount = areaCount;
                }
                else
                {
                    this.ResetNodeData();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"鎷惧彇CV鍏冪礌閬囧埌寮傚父锛屽綋鍓嶆椂闂寸偣濂栦笉浣跨敤CV鍏冪礌");
                this.ResetNodeData();
            }
        }
        #endregion
        private void ResetNodeData()
        {
            this.CurrentCVCaptureNode = null;
            this.NormalContainsCVCount = 0;
        }
    }
}

