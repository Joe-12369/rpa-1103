using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Common
{
    public class RandomHelper
    {
        private static readonly Random random = new Random();
        private const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly int charactersLength = characters.Length;
        public static string RandomString(int len)
        {
            char[] result = new char[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = characters[random.Next(charactersLength)];
            }
            return new string(result);
        }
    }
}
