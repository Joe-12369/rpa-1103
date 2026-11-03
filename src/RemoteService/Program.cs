using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace RemoteService
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServiceHost host = new ServiceHost(typeof(RemoteRunService));
                host.Open();
                Console.WriteLine("服务已开启！");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("服务启动异常！",ex);
            }
        }
    }
}
