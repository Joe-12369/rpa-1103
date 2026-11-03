using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Helper
{
    public class SocketHelper
    {
        public static bool TestPort(string ipString, int port)
        {
            IPAddress ip = IPAddress.Parse(ipString);
            bool ret = false;
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ip, port);
                if (socket.Connected == true)
                    ret = true;
                socket.Close();
            }
            catch (SocketException ex)
            {
                ret = false;
            }
            return ret;
        }
    }
}
