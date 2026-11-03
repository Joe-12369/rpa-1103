using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.ScreenShot
{
    public class ElementShoter
    {
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// 处理浏览器截图并抠取目标元素
        /// </summary>
        /// <param name="browserData">浏览器传递的数据</param>
        /// <param name="outputPath">抠图结果保存路径</param>
        /// <returns>是否处理成功</returns>
        public bool ProcessScreenshot(BrowserScreenshotData browserData, string outputPath)
        {
            try
            {
                //解析Base64图片数据
                byte[] imageBytes = ParseDataUrl(browserData.ScreenshotDataUrl);
                if (imageBytes == null)
                {
                    logger.LogDebug($"获取当前网页标签页截图数据异常");
                    return false;
                }
                //获取缩放比
                float scaleFactor = GetEffectiveScaleFactor(browserData.BrowserScaleFactor);
                logger.LogDebug($"使用的缩放比: {scaleFactor:0.00}");
                //修正元素位置和尺寸（应用缩放比）
                ElementRect scaledRect = ScaleElementRect(browserData.ElementRect, scaleFactor);
                //执行抠图
                return CropImage(imageBytes, scaledRect, outputPath, scaleFactor);
            }
            catch (Exception ex)
            {
                logger.LogError($"网页元素截图失败:", ex);
                return false;
            }
        }
        private byte[] ParseDataUrl(string dataUrl)
        {

            if (string.IsNullOrEmpty(dataUrl))
                return null;
            string base64String = dataUrl;
            if (dataUrl.StartsWith("data:image/"))
            {
                // 提取Base64部分（去掉前缀 data:image/png;base64,）
                int base64StartIndex = dataUrl.IndexOf(',') + 1;
                base64String = dataUrl.Substring(base64StartIndex);
            }
            try
            {
                return Convert.FromBase64String(base64String);
            }
            catch (FormatException)
            {
                return null;
            }
        }
        private float GetEffectiveScaleFactor(float browserScale)
        {
            // 若浏览器传递了有效的缩放比，则优先使用
            if (browserScale > 0)
                return browserScale;
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    float dpiScale = g.DpiX / 96f;
                    return Math.Max(dpiScale, 0.1f); // 防止异常值
                }
            }
            catch
            {
                return 1.0f;
            }
        }
        private ElementRect ScaleElementRect(ElementRect original, float scaleFactor)
        {
            return new ElementRect
            {
                Left = original.Left * scaleFactor,
                Top = original.Top * scaleFactor,
                Width = original.Width * scaleFactor,
                Height = original.Height * scaleFactor
            };
        }
        /// <summary>
        /// 执行图片裁剪
        /// </summary>
        private bool CropImage(byte[] imageBytes, ElementRect scaledRect, string outputPath, float dpr)
        {
            using (MemoryStream ms = new MemoryStream(imageBytes))
            using (Image originalImage = Image.FromStream(ms))
            {
                int srcX = (int)Math.Round(scaledRect.Left);
                int srcY = (int)Math.Round(scaledRect.Top);
                int srcWidth = (int)Math.Round(scaledRect.Width);
                int srcHeight = (int)Math.Round(scaledRect.Height);

                srcX = Math.Max(0, srcX);
                srcY = Math.Max(0, srcY);
                srcWidth = Math.Min(srcWidth, originalImage.Width - srcX);
                srcHeight = Math.Min(srcHeight, originalImage.Height - srcY);

                if (srcWidth <= 0 || srcHeight <= 0)
                {
                    logger.LogDebug($"有效裁剪区域为空");
                    return false;
                }

                // 这里使用原始尺寸（除以DPR）而非缩放后的尺寸
                int destWidth = (int)Math.Round(srcWidth / dpr);
                int destHeight = (int)Math.Round(srcHeight / dpr);

                using (Bitmap croppedBitmap = new Bitmap(destWidth, destHeight))
                using (Graphics g = Graphics.FromImage(croppedBitmap))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    g.DrawImage(
                        originalImage,
                        new Rectangle(0, 0, destWidth, destHeight),  // 目标区域（原始尺寸）
                        new Rectangle(srcX, srcY, srcWidth, srcHeight),  // 源区域（缩放后尺寸）
                        GraphicsUnit.Pixel
                    );
                    string directory = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                    croppedBitmap.Save(outputPath, ImageFormat.Png);
                    return true;
                }
            }
        }
    }

    /// <summary>
    /// 浏览器传递的截图及元素信息模型
    /// </summary>
    public class BrowserScreenshotData
    {
        /// <summary>
        /// 标签页截图的Base64数据（格式： data:image/png;base64,...）
        /// </summary>
        public string ScreenshotDataUrl { get; set; }

        /// <summary>
        /// 目标元素的位置和尺寸信息
        /// </summary>
        public ElementRect ElementRect { get; set; }

        /// <summary>
        /// 浏览器缩放比（由浏览器传递，0表示未传递）
        /// </summary>
        public float BrowserScaleFactor { get; set; } = 0;
    }
    /// <summary>
    /// 元素位置和尺寸模型（对应浏览器getBoundingClientRect()）
    /// </summary>
    public class ElementRect
    {
        public float Left { get; set; }
        public float Top { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }
}
