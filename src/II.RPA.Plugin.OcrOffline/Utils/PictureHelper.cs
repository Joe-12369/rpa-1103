using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.OcrOffline.Utils
{
    public class PictureHelper
    {
        private static ILogger logger = Logger.OfContext();

        //public string Path { get; set; }
        //public void SetPath(string _Path)
        //{
        //    Path = _Path;
        //}

        public string Bitmap2Base64(Bitmap image, System.Drawing.Imaging.ImageFormat _imageFormat)
        {
            if (image == null) return null;
            string SigBase64 = string.Empty;
            using (var ms = new System.IO.MemoryStream())
            {
                image.Save(ms, _imageFormat);
                SigBase64 = Convert.ToBase64String(ms.GetBuffer());
            }
            return SigBase64;
        }

        public bool GetImageDataFromFile(string _Path, ref ImageData _ImageData)
        {
            bool bRet = false;
            if (!File.Exists(_Path))
            {
                logger.LogError($"OCR Table File Not Found");
                throw new Exception("File Not Found");
            }

            FileInfo fileInfo = new FileInfo(_Path);
            if (fileInfo.Extension != ".jpg" && fileInfo.Extension != ".png" && fileInfo.Extension != ".jpeg")
            {
                logger.LogError($"Image Format Not Supported:{fileInfo.Extension}");
                throw new Exception("Image Format Not Supported");
            }

            using (Image image = Image.FromFile(_Path))
            {
                using (Bitmap bitmap = new Bitmap(image))
                {
                    if (fileInfo.Extension == ".png")
                    {
                        _ImageData.Base64 = Bitmap2Base64(bitmap, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    else if (fileInfo.Extension == ".jpg" || fileInfo.Extension == ".jpeg")
                    {
                        _ImageData.Base64 = Bitmap2Base64(bitmap, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    else
                    {
                        return false;
                    }

                    _ImageData.StartPoint = new Point(0, 0);
                    _ImageData.EndPoint = new Point(bitmap.Width, bitmap.Height);
                    bRet = true;
                }
            }
            return bRet;
        }

        public string GetCurrentPackageResDirPath()
        {
            string DLLPath =  this.GetType().Assembly.Location;

            string DirPath = Path.GetDirectoryName(DLLPath);
            string DirPath1 = Path.GetDirectoryName(DirPath);
            string DirPath2 = Path.GetDirectoryName(DirPath1);
            string DirPath3 = Path.GetDirectoryName(DirPath2);

            string ResPath = Path.Combine(DirPath3, "res");
            string ResPath2 = Path.Combine(ResPath, "OCRImage");
            return ResPath2;
        }

        /// <summary>
        /// base64 转图片并保存
        /// </summary>
        /// <param name="base64Str"></param>
        /// <param name="path"></param>
        /// <param name="imgName"></param>
        /// <returns></returns>
        public static bool Base64ToImage(string base64Str, string path, string imgName)
        {
            bool bRet = false;
            //图片格式

            //string hz = base64Str.Split(',')[0].Split(';')[0].Split('/')[1];
            //string[] str = base64Str.Split(',');
            //byte[] imageBytes = Convert.FromBase64String(str[1]);

            try
            {

                byte[] imageBytes = Convert.FromBase64String(base64Str);

                //写入内存流
                MemoryStream memoryStream = new MemoryStream(imageBytes, 0, imageBytes.Length);
                memoryStream.Write(imageBytes, 0, imageBytes.Length);
                //物理路径
                string tmpRootDir = path;
                //文件路径不存在就新创建
                if (!Directory.Exists(tmpRootDir))
                {
                    Directory.CreateDirectory(tmpRootDir);
                }
                //string imagesurl2 = tmpRootDir + imgName + "." + hz; //转换成绝对路径 
                //  转成图片
                string ImageFullPath = Path.Combine(tmpRootDir,$"{imgName}.jpg");
                Image image = Image.FromStream(memoryStream);
                image.Save(ImageFullPath);
                memoryStream.Flush();
                memoryStream.Close();
                memoryStream.Dispose();

                bRet = true;
            }
            catch (Exception ex)
            {
                bRet = false;
                logger.LogError(ex.Message);
            }

            return bRet;
        }


    }

    public class ImageData
    {
        public string Base64 { get; set; }
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
    }

    public class Rectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int WindowsX { get; set; }
        public int WindowsY { get; set; }
        public int WindowsWidth { get; set; }
        public int WindowsHeight { get; set; }

        public string Selector { get; set; }
    }

    public class ImageDataEx
    {
        public string Base64 { get; set; }
        public string TableAlgInfo { get; set; }
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        public ImageDataEx() { }
        public ImageDataEx(ImageData cImageData)
        {
            this.StartPoint = cImageData.StartPoint;
            this.EndPoint = cImageData.EndPoint;
            this.Base64 = cImageData.Base64;
            this.TableAlgInfo = string.Empty;
        }
    }

    public class ScreenShootStruct
    {
        [JsonIgnore]
        public bool IsValid = false;

        [JsonIgnore]
        public string Base64 = string.Empty;
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double ScaleX { get; set; }//缩放倍率 1, 1.25, 1.5, 1.75
        public double ScaleY { get; set; }//缩放倍率 1, 1.25, 1.5, 1.75
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public string TableAlgInfo { get; set; }
        public string ResDir { get; set; }

        public Dictionary<string, object> GetKeyValuePairs()
        {
            Dictionary<string, object> _KeyValuePairs = new Dictionary<string, object>();
            _KeyValuePairs.Add("x", StartX);
            _KeyValuePairs.Add("y", StartX);
            _KeyValuePairs.Add("width", StartX);
            _KeyValuePairs.Add("height", StartX);
            _KeyValuePairs.Add("screenWidth", ScreenWidth);
            _KeyValuePairs.Add("screenHeight", ScreenHeight);
            _KeyValuePairs.Add("scaleX", StartX);
            _KeyValuePairs.Add("scaleY", StartX);
            return _KeyValuePairs;
        }
    }
}
