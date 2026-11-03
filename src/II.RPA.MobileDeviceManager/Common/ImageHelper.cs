using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace II.RPA.MobileDeviceManager.Common
{
    public static class ImageSourceToFileHelper
    {
        /// <summary>
        /// 将 ImageSource 保存为本地图片文件
        /// </summary>
        /// <param name="imageSource">要转换的 ImageSource 对象</param>
        /// <param name="savePath">保存路径（含文件名和后缀，如 "D:/test.png"）</param>
        /// <param name="imageFormat">图片格式（支持 png/jpg/bmp）</param>
        /// <exception cref="ArgumentNullException">参数为空</exception>
        /// <exception cref="NotSupportedException">不支持的图片格式</exception>
        public static void SaveToLocalFile(this ImageSource imageSource, string savePath, string imageFormat = "png")
        {
            if (imageSource == null)
                throw new ArgumentNullException(nameof(imageSource));
            if (string.IsNullOrEmpty(savePath))
                throw new ArgumentNullException(nameof(savePath));

            // 1. 将 ImageSource 转换为 BitmapFrame（处理不同子类）
            BitmapFrame bitmapFrame = GetBitmapFrameFromImageSource(imageSource);

            // 2. 创建对应的 BitmapEncoder
            BitmapEncoder encoder = GetBitmapEncoder(imageFormat.ToLower(), bitmapFrame);

            // 3. 写入文件流并保存
            using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(fileStream);
            }
        }

        /// <summary>
        /// 处理不同类型的 ImageSource，转换为 BitmapFrame
        /// </summary>
        private static BitmapFrame GetBitmapFrameFromImageSource(ImageSource imageSource)
        {
            // 核心：CroppedBitmap 继承自 BitmapSource，可直接创建 BitmapFrame
            if (imageSource is BitmapSource bitmapSource)
            {
                return BitmapFrame.Create(bitmapSource);
            }
            // 兼容 DrawingImage（矢量图需渲染）
            else if (imageSource is DrawingImage drawingImage)
            {
                double width = drawingImage.Drawing.Bounds.Width > 0 ? drawingImage.Drawing.Bounds.Width : 800;
                double height = drawingImage.Drawing.Bounds.Height > 0 ? drawingImage.Drawing.Bounds.Height : 600;

                var renderBitmap = new RenderTargetBitmap(
                    (int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                var drawingVisual = new DrawingVisual();
                using (var dc = drawingVisual.RenderOpen())
                {
                    dc.DrawDrawing(drawingImage.Drawing);
                }
                renderBitmap.Render(drawingVisual);
                return BitmapFrame.Create(renderBitmap);
            }
            else
            {
                throw new NotSupportedException(Localize($"不支持的 ImageSource 类型：{imageSource.GetType().Name}", "controlType_ImageHelper_27FE446E"));
            }
        }

        /// <summary>
        /// 根据格式获取对应的 BitmapEncoder
        /// </summary>
        private static BitmapEncoder GetBitmapEncoder(string format, BitmapFrame bitmapFrame)
        {
            BitmapEncoder encoder;
            switch (format)
            {
                case "png":
                    encoder = new PngBitmapEncoder();
                    break;
                case "jpg":
                case "jpeg":
                    encoder = new JpegBitmapEncoder { QualityLevel = 90 }; // 质量 0-100
                    break;
                case "bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                default:
                    throw new NotSupportedException(Localize($"不支持的图片格式：{format}", "controlType_ImageHelper_6291897F"));
            }
            encoder.Frames.Add(bitmapFrame);
            return encoder;
        }
    }
}
