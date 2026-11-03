using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    internal static class ObjectIdHelper
    {
        internal static Guid ComputeGuidForObjectContent(params object[] args)
        {
            return ComputeGuidForObjectContent(args.AsEnumerable<object>());
        }

        internal static Guid ComputeGuidForObjectContent(IEnumerable<object> objects)
        {
            byte[] binaryDataFromObjects = GetBinaryDataFromObjects(objects);
            return new Guid(MD5.Create().ComputeHash(binaryDataFromObjects));
        }

        private static byte[] GetBinaryDataFromObjects(IEnumerable<object> objects)
        {
            if (objects.Any((object o) => o == null))
            {
                throw new ArgumentException("objects contains an object which is null");
            }
            List<byte> list = new List<byte>();
            object[] array = (from o in objects
                              where o.GetType().IsSerializable
                              select o).ToArray<object>();
            if (array.Any<object>())
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    new BinaryFormatter().Serialize(memoryStream, array);
                    list.AddRange(memoryStream.ToArray());
                }
            }
            foreach (object obj in objects.Except(array).ToArray<object>())
            {
                list.AddRange(GetBytes(obj.ToString()));
            }
            return list.ToArray();
        }

        private static byte[] GetBytes(string str)
        {
            byte[] array = new byte[str.Length * 2];
            Buffer.BlockCopy(str.ToCharArray(), 0, array, 0, array.Length);
            return array;
        }

        private const int HashPrime = 31;

        private const int NullHashPrime = 17;
    }
}
