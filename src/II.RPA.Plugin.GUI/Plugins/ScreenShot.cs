using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ProcessCreator;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.Plugin.GUI.ProcessCreator.Screenshot;
using II.RPA.Plugin.GUI.ScreenShot;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class ScreenShot : ElementAbilityBase<ParamScreenShotModel>
    {
        private static readonly ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ScreenShot;
        private readonly ScreenShoter m_ScreenShoter;
        private readonly ConcurrencyItemContainer<Int32Rect> m_SnapResult = new ConcurrencyItemContainer<Int32Rect>();

        public ScreenShot()
        {
            m_ScreenShoter = new ScreenShoter();
            m_ScreenShoter.Snapped += ScreenShoter_Snapped;
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
            //using (var bitmap = ToBitmap(image))
            //{
            //    Bitmap bmpTemp = new Bitmap(bitmap);
            //    Bitmap bmp = new Bitmap(bmpTemp);
            //    bmpTemp.Dispose();
            //    bmp.Save("tmp.png");
            //}
        }

        protected override JMessage Execute(ParamScreenShotModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            var packagePath = paramModel.PackagePath;
            if (paramModel.Action == "run")
            {
                if (paramModel.Source == "screen")
                {
                    if (paramModel.RectangleRegion.Width > 0 && paramModel.RectangleRegion.Height > 0)
                    {
                        var pictureRandomName = GetElementShotRandomName();
                        var picturePath = Path.Combine(packagePath, "res", pictureRandomName);
                        if (ScreenShotWithRect(paramModel.RectangleRegion.X,
                            paramModel.RectangleRegion.Y,
                            paramModel.RectangleRegion.Width,
                            paramModel.RectangleRegion.Height, picturePath))
                        {
                            JObject slimSelectorJO = new JObject();
                            slimSelectorJO.Add("screen", 1);
                            slimSelectorJO.Add("path", Path.Combine("res", pictureRandomName));
                            JObject locationJO = new JObject();
                            locationJO.Add("x", paramModel.RectangleRegion.X);
                            locationJO.Add("y", paramModel.RectangleRegion.Y);
                            locationJO.Add("w", paramModel.RectangleRegion.Width);
                            locationJO.Add("h", paramModel.RectangleRegion.Height);
                            slimSelectorJO.Add("location", locationJO);
                            return new JMessage
                            {
                                Code = 0,
                                Message = "截图已完成".I18n("ScreenShot_CBBB9AB5"),
                                Info = $"{{\"uiSelector\":{slimSelectorJO}}}"
                            };
                        }
                        else
                        {
                            throw new RPAException(eErrorMessageType.Error_ScreenshotError);
                        }
                    }
                    else
                    {
                        throw new RPAException(eErrorMessageType.Error_ScreenshotError);
                    }
                }
                else if (paramModel.Source == "file")
                {
                    JObject slimSelectorJO = new JObject();
                    slimSelectorJO.Add("screen", 1);
                    slimSelectorJO.Add("path", paramModel.ImagePath);
                    JObject locationJO = new JObject();
                    locationJO.Add("x", paramModel.RectangleRegion.X);
                    locationJO.Add("y", paramModel.RectangleRegion.Y);
                    locationJO.Add("w", paramModel.RectangleRegion.Width);
                    locationJO.Add("h", paramModel.RectangleRegion.Height);
                    slimSelectorJO.Add("location", locationJO);
                    return new JMessage
                    {
                        Code = 0,
                        Message = "截图已完成".I18n("ScreenShot_CBBB9AB5"),
                        Info = $"{{\"uiSelector\":{slimSelectorJO}}}"
                    };
                }
                else
                {
                    throw new RPAException(eErrorMessageType.Error_ScreenshotError);
                }
            }
            else if (paramModel.Action == "manualShot")
            {
                m_ScreenShoter.Start();
                m_SnapResult.WaitForData(-1);
                var rect = m_SnapResult.TakeData();
                if (rect == Int32Rect.Empty)
                {
                    return new JMessage
                    {
                        Code = 1,
                        Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage(),
                    };
                }
                else
                {
                    var selector = CaptureSlim(rect.X, rect.Y, rect.Width, rect.Height);
                    return new JMessage
                    {
                        Code = 0,
                        Message = "截图已完成".I18n("ScreenShot_CBBB9AB5"),
                        Info = $"{{\"uiSelector\":{selector}}}"
                    };
                }
            }
            else
            {
                throw new RPAException(eErrorMessageType.Error_ScreenshotError);
            }
        }

        protected override void ValidateParameter(ParamScreenShotModel paramModel) {
            if (paramModel == null)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            else
            {
                if (string.IsNullOrEmpty(paramModel.Action))
                {
                    logger.LogError("ParamScreenShotModel:action is empty!");
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
            }
            return;
        }

        public static bool ScreenShotWithRect(int X, int Y, int Width, int Height,string aPath)
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
        private string GetElementShotRandomName()
        {
            return $"{new Random().Next(10000, 99999)}-{ToUnixTimeSeconds(new DateTimeOffset(DateTime.UtcNow))}.png".Trim().Replace(" ", string.Empty);
        }
        public long ToUnixTimeSeconds(DateTimeOffset dateTimeOffset)
        {
            long num = dateTimeOffset.UtcDateTime.Ticks / 10000000;
            return num - 62135596800L;
        }

        public static Bitmap ToBitmap(BitmapSource source)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(ms);
                return new Bitmap(ms);
            }
        }

        private string CaptureSlim(int aX,int aY,int aW,int aH)
        {
            try
            {
                //var pictureRandomName = GetElementShotRandomName();
                //var picturePath = Path.Combine(m_PackagePath, "res", pictureRandomName);
                //var bitmapSource = ImageHelper.GetBitmapSource((int)Current.MaskLeftWidth + 1,
                //(int)Current.MaskTopHeight + 1,
                //(int)CaptureWindow.Current.MainImage.ActualWidth - 2,
                //(int)CaptureWindow.Current.MainImage.ActualHeight - 2);
                //ImageHelper.SaveToPng(bitmapSource, picturePath);
                //logger.LogDebug("Image save done");
                JObject slimSelectorJO = new JObject
                {
                    { "screen", 1 }
                };
                //slimSelectorJO.Add("path", Path.Combine("res", pictureRandomName));
                JObject locationJO = new JObject
                {
                    { "x", aX },
                    { "y", aY },
                    { "w", aW },
                    { "h", aH }
                };
                slimSelectorJO.Add("location", locationJO);
                return slimSelectorJO.ToString();

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return null;
            }
        }
    }
}
