using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;
using System.Windows.Forms;
using System.IO;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class ScreenShotExecutor : ElementAbilityBase<ParamScreenShotExecutorModel>
    {
        private static readonly ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ScreenShot;

        protected override JMessage Execute(ParamScreenShotExecutorModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            try
            {
                switch (paramModel.PictureSource)
                {
                    //屏幕
                    case 1:
                        //全屏
                        if (paramModel.ShotRange == 1)
                        {
                            using (var image = ScreenShotAll())
                            {
                                image.Save(paramModel.OutImagePath, ImageFormat.Png);
                            }
                        }
                        //自定义区域
                        else if (paramModel.ShotRange == 2)
                        {
                            if (RectVerify(paramModel.Position.X,
                                paramModel.Position.Y,
                                paramModel.Position.Width,
                                paramModel.Position.Height))
                            {
                                using (var image = ScreenShotRect(paramModel.Position.X,
                                    paramModel.Position.Y,
                                    paramModel.Position.Width,
                                    paramModel.Position.Height))
                                {
                                    image.Save(paramModel.OutImagePath, ImageFormat.Png);
                                }
                            }
                            else
                            {
                                throw new RPAException(eErrorMessageType.Error_ScreenshotError, Localize($"坐标参数值非法：{paramModel.Position.X};{paramModel.Position.Y};{paramModel.Position.Width};{paramModel.Position.Height};", "controlType_ScreenShotExecutor_773662B5"));
                            }
                        }
                        else
                        {
                            throw new RPAException(eErrorMessageType.Error_ScreenshotError, Localize($"截图范围参数值非法：{paramModel.ShotRange}", "controlType_ScreenShotExecutor_BC3511EE"));
                        }
                        break;
                    //图片
                    case 2:
                        if (RectVerify(paramModel.Position.X,
                               paramModel.Position.Y,
                               paramModel.Position.Width,
                               paramModel.Position.Height))
                        {
                            if (File.Exists(paramModel.ImagePath))
                            {
                                CaptureImage(paramModel.ImagePath,
                                    paramModel.OutImagePath,
                                    paramModel.Position.X,
                                    paramModel.Position.Y,
                                    paramModel.Position.Width,
                                    paramModel.Position.Height);
                            }
                            else
                            {
                                throw new RPAException(eErrorMessageType.Error_ScreenshotError, Localize($"截图范围参数值非法：{paramModel.ImagePath}", "controlType_ScreenShotExecutor_914DE341"));
                            }
                        }
                        else
                        {
                            throw new RPAException(eErrorMessageType.Error_ScreenshotError, Localize($" 截图失败,目标图片尺寸超出当前桌面分辨率,请调整图片或截图区域。", "controlType_ScreenShotExecutor_841FD475"));
                        }
                        break;
                    case 3:
                        break;

                }
                return new JMessage
                {
                    Code = 0,
                    Message = "截图完成".I18n("ScreenShotExecutor_55C0D27D"),
                    Info = ""
                };
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex,"截图失败");
                if (ex is RPAException rex)
                {
                    throw rex;
                }
                else
                {
                    throw new RPAException(eErrorMessageType.Error_ScreenshotError, Localize($"截图失败", "controlType_ScreenShotExecutor_834D07FD"));
                }
            }
        }

        protected override void ValidateParameter(ParamScreenShotExecutorModel paramModel) {
            if (paramModel == null)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            else
            {
                if (string.IsNullOrEmpty(paramModel.OutImagePath))
                {
                    logger.LogError("ParamScreenShotExecutorModel:OutImagePath is empty!");
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
            }
            return;
        }

        /// <summary>
        /// 从图片中截取一部分图片
        /// </summary>
        /// <param name="fromImagePath">来源图片地址</param>        
        /// <param name="nX">从偏移X坐标位置开始截取</param>
        /// <param name="nY">从偏移Y坐标位置开始截取</param>
        /// <param name="toImagePath">保存图片地址</param>
        /// <param name="width">保存图片的宽度</param>
        /// <param name="height">保存图片的高度</param>
        /// <returns></returns>
        public void CaptureImage(string fromImagePath, string toImagePath, int nX, int nY, int width, int height)
        {
            //原图片文件
            using (Image fromImage = Image.FromFile(fromImagePath))
            {
                //创建新图位图
                Bitmap bitmap = new Bitmap(width, height);
                //创建作图区域
                Graphics graphic = Graphics.FromImage(bitmap);
                //截取原图相应区域写入作图区
                graphic.DrawImage(fromImage, 0, 0, new Rectangle(nX, nY, width, height), GraphicsUnit.Pixel);
                //从作图区生成新图
                Image saveImage = Image.FromHbitmap(bitmap.GetHbitmap());
                //保存图片
                saveImage.Save(toImagePath, ImageFormat.Png);
                //释放资源   
                saveImage.Dispose();
                graphic.Dispose();
                bitmap.Dispose();
                fromImage.Dispose();
            }
        }
        /// <summary>
        /// 截取整个桌面
        /// </summary>
        /// <returns></returns>
        public static Image ScreenShotAll()
        {
            Rectangle rc = Screen.PrimaryScreen.Bounds;
            int iWidth = rc.Width;
            int iHeight = rc.Height;
            Image myImage = new Bitmap(iWidth, iHeight);
            Graphics.FromImage(myImage).CopyFromScreen(new System.Drawing.Point(0, 0), new System.Drawing.Point(0, 0), new System.Drawing.Size(iWidth, iHeight));
            return myImage;
        }

        /// <summary>
        /// 在桌面上截取矩形区域
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <returns></returns>
        public static Image ScreenShotRect(int X, int Y, int Width, int Height)
        {
            Rectangle rc = new Rectangle(X, Y, Width, Height);
            int iWidth = rc.Width;
            int iHeight = rc.Height;
            Image myImage = new Bitmap(iWidth, iHeight);
            Graphics.FromImage(myImage).CopyFromScreen(rc.Location, new System.Drawing.Point(0, 0), new System.Drawing.Size(iWidth, iHeight));
            return myImage;
        }

        public bool RectVerify(int X, int Y, int Width, int Height)
        {
            if (X < 0 || Y < 0 || Width <= 0 || Height <= 0)
            {
                return false;
            }
            Rectangle rc = Screen.PrimaryScreen.Bounds;
            if (X + Width > rc.Width || Y + Height > rc.Height)
            {
                return false;
            }
            return true;
        }
    }
}

