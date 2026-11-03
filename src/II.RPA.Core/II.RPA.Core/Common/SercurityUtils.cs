using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Common
{
    public class SercurityUtils
    {
        private const string Key = "szznszznszznszzn";
        private const string Iv = "grpcgrpcgrpcgrpc";

        public static string Encrypt(string aToEncrypt)
        {
            if (string.IsNullOrEmpty(aToEncrypt))
            {
                return string.Empty;
            }
            var keyArray = Encoding.UTF8.GetBytes(Key);
            var ivArray = Encoding.UTF8.GetBytes(Iv);
            var toEncryptArray = Encoding.UTF8.GetBytes(aToEncrypt);
            var rDel = new RijndaelManaged
            {
                BlockSize = 128,
                KeySize = 256,
                FeedbackSize = 128,
                Padding = PaddingMode.PKCS7,
                Key = keyArray,
                IV = ivArray,
                Mode = CipherMode.CBC
            };
            var cTransform = rDel.CreateEncryptor();
            var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static string Decrypt(string aToDecrypt)
        {
            if (string.IsNullOrEmpty(aToDecrypt))
            {
                return string.Empty;
            }
            try
            {
                var keyArray = Encoding.UTF8.GetBytes(Key);
                var ivArray = Encoding.UTF8.GetBytes(Iv);
                var toEncryptArray = Convert.FromBase64String(aToDecrypt);
                var rDel = new RijndaelManaged
                {
                    Key = keyArray,
                    IV = ivArray,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                };
                var cTransform = rDel.CreateDecryptor();
                var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                return Encoding.UTF8.GetString(resultArray);
            }
            catch
            {
                return aToDecrypt;
            }
        }
    }
}
