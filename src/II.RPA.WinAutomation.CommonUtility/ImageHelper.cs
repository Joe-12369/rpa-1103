using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class ImageHelper
    {
        private static ILogger logger = Logger.OfContext();
        public const int ActivityPreviewImageWidth = 300;
        public const int ActivityPreviewImageHeight = 100;
        public static void SaveImageStamped(Bitmap img, string message)
        {
            SaveImageStamped(img, System.IO.Directory.GetCurrentDirectory(), message);
        }
        public static void SaveImageStamped(Bitmap img, string path, string message)
        {
            SaveImage(img, path, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-") + "-" + message + ".png");
        }
        public static void SaveImage(Bitmap img, string path, string filename)
        {
            try
            {
                if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                img.Save(System.IO.Path.Combine(path, filename));
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, ex.Message);
                throw;
            }
        }
        public static Bitmap Screenshot(Rectangle rect)
        {
            return Screenshot(rect.X, rect.Y, rect.Width, rect.Height);
        }
        public static Bitmap Screenshot()
        {
            return Screenshot(0, 0, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
        }
        public static Bitmap Screenshot(int x, int y, int width, int height)
        {
            if (width < 10) width = ActivityPreviewImageWidth;
            if (height < 10) height = ActivityPreviewImageHeight;
            var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap as System.Drawing.Image))
            {
                graphics.CopyFromScreen(x, y, 0, 0, bitmap.Size);
            }
            return bitmap;
        }
        public static Bitmap Screenshot(int x, int y, int width, int height, int maxWidth, int maxHeight)
        {
            if (width < 10) width = ActivityPreviewImageWidth;
            if (height < 10) height = ActivityPreviewImageHeight;
            var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap as System.Drawing.Image))
            {
                graphics.CopyFromScreen(x, y, 0, 0, bitmap.Size);
            }
            if (width > maxWidth || height > maxHeight)
            {
                using (bitmap) // dispose bitmap
                {
                    return Resize(bitmap, maxWidth, maxHeight);
                }
            }
            return bitmap;
        }
        public static void Screenshot(Rectangle rectangle, string picPath)
        {
            try
            {
                using (Bitmap b = new Bitmap(rectangle.Width, rectangle.Height))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.CopyFromScreen(rectangle.X, rectangle.Y, 0, 0, rectangle.Size);
                    }
                    b.Save(picPath);//保存图片
                }
            }
            catch (Exception e)
            {
                throw new RPAException(eErrorMessageType.Error_UnauthorizedAccessError, e);
            }
        }
        public static void RemoveNoise(ref Bitmap bitmap)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.R < 162 && pixel.G < 162 && pixel.B < 162)
                        bitmap.SetPixel(x, y, Color.Black);
                    else if (pixel.R > 162 && pixel.G > 162 && pixel.B > 162)
                        bitmap.SetPixel(x, y, Color.White);
                }
            }
        }
        public static void SetGrayscale(ref Bitmap img)
        {
            //var temp = img;
            //var bmap = temp.Clone();
            Color c;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    c = img.GetPixel(i, j);
                    byte gray = (byte)(.299 * c.R + .587 * c.G + .114 * c.B);
                    img.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
                }
            }
        }
        public static Bitmap Resize(Bitmap image, int tWidth, int tHeight)
        {
            var brush = new SolidBrush(Color.White);

            float Width = tWidth; float Height = tHeight;

            float scale = Math.Min(Width / image.Width, Height / image.Height);

            var bmp = new Bitmap((int)Width, (int)Height);
            using (var graph = Graphics.FromImage(bmp))
            {
                // uncomment for higher quality output
                graph.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graph.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                var scaleWidth = (int)(image.Width * scale);
                var scaleHeight = (int)(image.Height * scale);

                graph.FillRectangle(brush, new RectangleF(0, 0, Width, Height));
                graph.DrawImage(image, ((int)Width - scaleWidth) / 2, ((int)Height - scaleHeight) / 2, scaleWidth, scaleHeight);
                brush.Dispose();
            }

            return bmp;

        }
        public static System.Windows.Media.Imaging.BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (var memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapimage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
        public static System.Windows.Media.Imaging.BitmapImage BitmapToImageSource(Bitmap bitmap, int Width, int Height)
        {
            if (bitmap == null) return null;
            if (bitmap.Width > Width || bitmap.Height > Height)
            {
                bitmap = Resize(bitmap, Width, Height);
            }
            using (var memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapimage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }
        public static string Bitmap2Base64(Bitmap image)
        {
            if (image == null) {
                NLogger.Default.Trace($"Bitmap2Base64() param image is null");
                return null;
            }
            string SigBase64 = string.Empty;
            using (var ms = new System.IO.MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                SigBase64 = Convert.ToBase64String(ms.GetBuffer());
            }
            return SigBase64;
        }
        public static Bitmap Base642Bitmap(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;
            using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(base64)))
            using (var image = System.Drawing.Image.FromStream(ms, false, true))
                return new System.Drawing.Bitmap(image);
        }

        public static Bitmap AreaClear(Bitmap sourceImage, Rectangle rect)
        {
            //Bitmap sourceImage = new Bitmap(imageFileName);
            Bitmap image = new Bitmap(sourceImage.Width, sourceImage.Height);
            using (Graphics g = Graphics.FromImage(image))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height);
                sourceImage.Dispose();
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddRectangle(rect);
                    g.SetClip(path);
                    g.Clear(Color.Transparent);
                    return image;
                }
            }
        }
        #region 从大图中截取一部分图片
        /// <summary>
        /// 从大图中截取一部分图片
        /// </summary>
        /// <param name="sourceImage">来源图片</param>        
        /// <param name="rect">截取区域</param>
        /// <returns></returns>
        public static Bitmap CaptureImage(Bitmap sourceImage, Rectangle rect)
        {
            //创建新图位图
            Bitmap bitmap = new Bitmap(rect.Width, rect.Height);
            //创建作图区域
            using (Graphics graphic = Graphics.FromImage(bitmap))
            {
                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphic.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                //截取原图相应区域写入作图区
                graphic.DrawImage(sourceImage, 0, 0, rect, GraphicsUnit.Pixel);
            }
            return bitmap;
        }
        public static Bitmap CaptureImage(Bitmap sourceImage)
        {
            Bitmap bitmap = null;
            try
            {
                Rectangle rect = new Rectangle(0, 0, sourceImage.Width, sourceImage.Height);
                bitmap =  CaptureImage(sourceImage, rect);
            }
            catch (Exception ex)
            {
                logger.LogError($"CaptureImage:{ex.ToString()}");
            }
            return bitmap;
        }
        #endregion

        #region 压缩图片
        /// <summary>
        /// 将图片等比例压缩不留多余区域
        /// </summary>
        /// <param name="image"></param>
        /// <param name="destWidth"></param>
        /// <param name="destHeight"></param>
        /// <returns></returns>
        public static Bitmap CompressImage(Bitmap image, int destWidth, int destHeight)
        {
            try
            {
                //按比例缩放           
                int sourWidth = image.Width;
                int sourHeight = image.Height;
                if (sourHeight < destHeight && sourWidth < destWidth)
                {
                    return image;
                }
                var brush = new SolidBrush(Color.White);
                float Width = destWidth; float Height = destHeight;
                float scale = Math.Min(Width / image.Width, Height / image.Height);
                Width = image.Width * scale;
                Height = image.Height * scale;
                var bmp = new Bitmap((int)Width, (int)Height);
                using (var graph = Graphics.FromImage(bmp))
                {
                    graph.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graph.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                    graph.FillRectangle(brush, new RectangleF(0, 0, Width, Height));
                    graph.DrawImage(image, 0, 0, Width, Height);
                    brush.Dispose();
                }
                return bmp;
            }
            catch
            {
                return image;
            }
        }
        #endregion

    }
}

