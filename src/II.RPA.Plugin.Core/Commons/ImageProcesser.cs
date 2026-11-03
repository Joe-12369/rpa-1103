using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Commons
{
    public static class ImageProcesser
    {
        private static int GetRealLength(int length, decimal ratio) => (int)Math.Ceiling(length * ratio);

        public static Bitmap MergeImages(IEnumerable<Image> images, int width, int height, int perHeight, decimal ratio)
        {
            Bitmap bitMap = null;
            width = GetRealLength(width, ratio);
            height = GetRealLength(height, ratio);
            perHeight = GetRealLength(perHeight, ratio);
            try
            {
                bitMap = new Bitmap(width, height);
                using (Graphics graphic = Graphics.FromImage(bitMap))
                {
                    images.Select((img, index) =>
                    {
                        graphic.DrawImage(img, 0, index * perHeight, width, Math.Min(perHeight, height - index * perHeight));
                        return false;
                    }).ToList();
                }
                return bitMap;
            }
            catch
            {
                if (bitMap != null)
                {
                    bitMap.Dispose();
                }
                throw;
            }
        }
    }
}
