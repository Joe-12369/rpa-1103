using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Common
{
    public static class IPAddressUtils
    {
        /// <summary>
        /// 获取Ipv4 地址
        /// </summary>
        /// <returns>ip</returns>
        public static IPAddress GetLocalV4Ip()
        {
            string hostname = Dns.GetHostName();
            IPHostEntry localhost = Dns.GetHostEntry(hostname);
            for (int i = 0; i < localhost.AddressList.Length; i++)
            {
                if (localhost.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    return localhost.AddressList[i];
                }
            }

            return IPAddress.None;
        }

        /// <summary>
        /// 获取localhost指向的地址集合
        /// </summary>
        /// <returns>ip集合</returns>
        public static IEnumerable<IPAddress> GetLocalHostIps()
        {
            string hostname = "localhost";
            IPHostEntry hostEntry = Dns.GetHostEntry(hostname);
            return hostEntry.AddressList;
        }
    }
}
