using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.Plugin.GUI.ScreenShot;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class CaptureScreen : ElementAbilityBase<ParamCaptureScreenModel>
    {
        private static ILogger logger = Logger.OfContext();
        private readonly ConcurrencyItemContainer<Int32Rect> m_SnapResult = new ConcurrencyItemContainer<Int32Rect>();
        private readonly ScreenShoter m_ScreenShoter;
        public CaptureScreen()
        {
            m_ScreenShoter = new ScreenShoter();
            m_ScreenShoter.Snapped += ScreenShoter_Snapped;
        }

        protected override eComponentType ComponentType => eComponentType.CaptureScreen;
        protected override void ValidateParameter(ParamCaptureScreenModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.imgPath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "图片保存地址为空".I18n("controlType_CaptureScreen_4CB9ECDF"));
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "CaptureScreen 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        protected override JMessage Execute(ParamCaptureScreenModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                m_ScreenShoter.Start();
                m_SnapResult.WaitForData(-1);
                var rect = m_SnapResult.TakeData();
                if (rect != Int32Rect.Empty)
                {

                    if (ScreenShotWithRect(rect.X, rect.Y, rect.Width, rect.Height, paramModel.imgPath))
                    {
                        msg.Info = JsonConvert.SerializeObject(new
                        {
                            X = rect.X,
                            Y = rect.Y,
                            Width = rect.Width,
                            Height = rect.Height,
                        });

                        return msg;
                    }
                    else
                    {
                        Exception(eErrorMessageType.Error_CaptureScreen_Error, ref msg);
                    }
                }
                else
                {
                    Exception(eErrorMessageType.Error_USERCLOSE, ref msg);
                }
            }
            catch (Exception e)
            {
                Exception(e, eErrorMessageType.Error_CaptureScreen_Error, ref msg);
            }
            return msg;
        }

        private void ScreenShoter_Snapped(object sender, FunctionEventArgs<System.Windows.Media.ImageSource> e)
        {
            if (e.IsDone)
            {
                CroppedBitmap image = e.Info as CroppedBitmap;
                m_SnapResult.SetData(image.SourceRect);
            }
            else
            {
                m_SnapResult.SetData(Int32Rect.Empty);
            }
        }

        public static bool ScreenShotWithRect(int X, int Y, int Width, int Height, string aPath)
        {
            try
            {
                Rectangle rc = new Rectangle(X, Y, Width, Height);
                int iWidth = rc.Width;
                int iHeight = rc.Height;
                using (Image myImage = new Bitmap(iWidth, iHeight))
                {
                    Graphics.FromImage(myImage).CopyFromScreen(rc.Location, new System.Drawing.Point(0, 0), new System.Drawing.Size(iWidth, iHeight));
                    myImage.Save(aPath, ImageFormat.Png);
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return false;
            }
        }
    }
}

