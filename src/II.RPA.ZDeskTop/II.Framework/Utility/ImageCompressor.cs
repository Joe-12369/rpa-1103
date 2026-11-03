using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace II.Framework.Utility
{
    public static class ImageCompressor
    {
        public static void CompressImage(Bitmap originalImage, string outputPath, long quality)
        {
            if (originalImage == null)
                throw new ArgumentNullException(nameof(originalImage), "Original image cannot be null.");

            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 100.");

            try
            {
                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                ImageCodecInfo jpegEncoder = GetEncoder(ImageFormat.Jpeg);

                // Save the compressed image
                originalImage.Save(outputPath, jpegEncoder, encoderParams);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error occurred while compressing the image.", ex);
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            throw new ArgumentException($"No encoder found for format: {format}");
        }
    }
}
