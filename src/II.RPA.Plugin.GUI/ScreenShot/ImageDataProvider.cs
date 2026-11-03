using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace II.RPA.Plugin.GUI.ScreenShot
{
    public class ImageDataProvider
    {
        public ImageData Provide(string aPackagePath)
        {
            var data = new ImageData
            {
                ImgName = $"{GenerateImageFileName()}.png",
                ImgId = GenerateFolderName()
            };
            var folder = Path.Combine(aPackagePath, "res", data.ImgId);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            data.ImgSrc = $@"res\{data.ImgId}\{data.ImgName}";
            data.ImagePath = Path.Combine(folder, data.ImgName);

            return data;
        }
        public static string GenerateFolderName(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return GenerateSecureRandomString(chars, length);
        }

        public static string GenerateImageFileName(int length = 8)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return GenerateSecureRandomString(chars, length);
        }
        private static string GenerateSecureRandomString(string chars, int length)
        {
            byte[] randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            StringBuilder sb = new StringBuilder(length);
            foreach (byte b in randomBytes)
            {
                sb.Append(chars[b % chars.Length]);
            }
            return sb.ToString();
        }
    }
    public struct Resolution
    {
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
    }
    public class ImageData
    {
        [JsonIgnore]
        public string ImagePath { get; set; }
        [JsonIgnore]
        public bool Saved { get; set; }

        [JsonProperty("imgName")]
        public string ImgName { get; set; }

        [JsonProperty("imgSrc")]
        public string ImgSrc { get; set; }

        [JsonProperty("imgId")]
        public string ImgId { get; set; }

        [JsonProperty("imgResolution")]
        public Resolution ImgResolution { get; set; }

        [JsonProperty("deviceResolution")]
        public Resolution DeviceResolution { get; set; }

        #region const
        [JsonProperty("device")]
        public string Device { get; private set; } = "pc";

        [JsonProperty("categoryName")]
        public string CategoryName { get; private set; } = "";

        [JsonProperty("categoryIcon")]
        public string CategoryIcon { get; private set; } = "";
        #endregion
    }
}
