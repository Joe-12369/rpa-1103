using DriverInput.Utils;
using System;
using System.Threading;
using System.Windows.Forms;

namespace DriverInput
{
    public class Program
    {
        static void Main(string[] args)
        {
            string ErrMsg = string.Empty;
            try
            {
                ControlKey controlKey = new ControlKey();
                //LogHelper.GetInstance().LogInfo($"入参: {args.Length}个 {args[0]} {args[1]}");
                //LogHelper.GetInstance().LogError($"异常:组件初始化失败");
                //LogHelper.GetInstance().PrintLogPath();
                if (args != null)
                {
                   
                    var value = args[0].Replace("--value=", "");
                    var timeout = args[1].Replace("--timeout=", "");
                    //var timeSpan = args[2].Replace("--timeSpan=", "");
                   
                    Thread.Sleep(Convert.ToInt32(timeout) * 1000);
                    controlKey.KeyDown(value);
                }
                else
                {
                    ErrMsg += "启动参数错误!";
                }

                Console.WriteLine(string.Format("{{\"code\":{0},\"msg\":{1}}}",0,""));
            }
            catch (Exception e)
            {
                ErrMsg += e.ToString();
                Console.WriteLine(string.Format("{{\"code\":{0},\"msg\":{1}}}", 1, ErrMsg));
            }
        }
    }

    public class InputResult
    {
        /// <summary>
        /// code 0成功；1失败
        /// </summary>
        public int code = 0;
        /// <summary>
        /// msg 成功为空字符串；失败返回错误原因（可能不全）
        /// </summary>
        public string msg = string.Empty;

        public InputResult() { }
        public InputResult(int iCode,string strMsg) 
        {
            code = iCode;
            msg = strMsg;
        }
    }
}

