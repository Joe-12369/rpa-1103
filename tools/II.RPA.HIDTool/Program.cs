using System;
using System.Threading;

namespace II.RPA.HIDTool
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(params string[] args)
        {

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new HIDTools());

            try
            {
                //ControlKey controlKey = new ControlKey();
                if (args != null)
                {
                    var text = args[0].Replace("--text=", "");
                    var isappend = args[1].Replace("--isappend=", "");
                    var timeout = args[2].Replace("--timeout=", "");
                    HIDSendHelper hid = new HIDSendHelper();
                    if (isappend == "0")
                    {
                        hid.ClearAllText("");
                        Thread.Sleep(50);
                    }
                    hid.SendText("", text, 100, 100);
                    Thread.Sleep(Convert.ToInt32(timeout) * 1000);
                }
                else
                {
                    Console.WriteLine("启动参数错误!");
                    Console.ReadKey();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("启动参数错误:" + e.Message.ToString());
                Console.ReadKey();
            }
        }
    }
}
