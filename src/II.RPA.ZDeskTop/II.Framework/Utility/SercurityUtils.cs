using System;
using System.Security.Cryptography;
using System.Text;

namespace II.Framework.Utility
{
    public class SercurityUtils
    {
        //private readonly static SecureString Key =  new SecureString();
        //private readonly static SecureString Iv = new SecureString();

        private const string Key = "szznszznszznszzn";
        private const string Iv = "grpcgrpcgrpcgrpc";
        static SercurityUtils()
        {
            //Key.AppendChar('s');
            //Key.AppendChar('z');
            //Key.AppendChar('z');
            //Key.AppendChar('n');
            //Key.AppendChar('n');
            //Key.AppendChar('n');
            //Key.AppendChar('n');
            //Key.AppendChar('n');
            //Key.AppendChar('s');
            //Key.AppendChar('z');
            //Key.AppendChar('z');
            //Key.AppendChar('n');
            //Key.AppendChar('n');
            //Key.AppendChar('n');
            //Key.AppendChar('n');
            //Key.AppendChar('n');

            //Key.MakeReadOnly();

            //Iv.AppendChar('1');
            //Iv.AppendChar('q');
            //Iv.AppendChar('a');
            //Iv.AppendChar('z');
            //Iv.AppendChar('1');
            //Iv.AppendChar('q');
            //Iv.AppendChar('a');
            //Iv.AppendChar('z');
            //Iv.AppendChar('1');
            //Iv.AppendChar('q');
            //Iv.AppendChar('a');
            //Iv.AppendChar('z');
            //Iv.AppendChar('1');
            //Iv.AppendChar('q');
            //Iv.AppendChar('a');
            //Iv.AppendChar('z');

            //Iv.MakeReadOnly();
        }
        public static string Encrypt(string aToEncrypt)
        {
            if (string.IsNullOrEmpty(aToEncrypt))
            {
                return string.Empty;
            }
            var keyArray = Encoding.UTF8.GetBytes(Key.ToString());
            var ivArray = Encoding.UTF8.GetBytes(Iv.ToString());
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
                var keyArray = Encoding.UTF8.GetBytes(Key.ToString());
                var ivArray = Encoding.UTF8.GetBytes(Iv.ToString());
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
