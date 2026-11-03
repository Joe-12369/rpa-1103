using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.UsbHub
{
   public class UsbHubHelper
    {
        public static readonly UsbHubHelper Instance = new UsbHubHelper();
        private TransmissionCOM COM;
        public UsbHubHelper()
        {
            COM = new TransmissionCOM();
        }
        /// <summary>
        /// 获取Com口名称
        /// </summary>
        /// <returns></returns>
        public string[] GetCom()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            new DetectCom().GetSerialPortInfo(dic);
            List<string> coms = new List<string>();
            foreach(var item in dic)
            {
                if(item.Value.ToLower().Contains("prolific pl2303gc"))
                {
                    coms.Add(item.Key);
                }
            }
            if(coms.Count==0)
            {
                throw new Exception("USB Hub驱动未安装".I18n("controlType_UsbHubHelper_48852859"));
            }
            return coms.ToArray();
        }
        /// <summary>
        /// 打开USB Hub 指定通道
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public bool OpenUsb(string portName, string channel)
        {
            bool result = false;
            try
            {
                COM.Connect(portName);
                channel = channel.PadLeft(2, '0');
                this.DATA_OFF(channel);
                this.POWER_ON(channel);
                this.DATA_ON(channel);
                  result = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                COM.Disconnect();
            }
            return result;
        }
        /// <summary>
        /// 关闭USB Hub 指定通道
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public bool CloseUsb(string portName, string channel)
        {
            bool result = false;
            try
            {
                COM.Connect(portName);
                channel = channel.PadLeft(2, '0');
                this.DATA_OFF(channel);
                this.POWER_OFF(channel);
                result = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                COM.Disconnect();
            }
            return result;
        }
        public void POWER_ON(string num)
        {
            string text = "%" + num + "W{\"POWER\":\"ON\"}\\r";
            try
            {
                this.COM.CmdDelay(5, 0);
                this.COM.DIYcmd(text);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            text = "%99R\r";
            try
            {
                this.COM.CmdDelay(20, 0);
                this.COM.DIYcmd(text);
                GetCmdResult();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void POWER_OFF(string num)
        {
            string text = "%" + num + "W{\"POWER\":\"OFF\"}\\r";
            try
            {
                this.COM.CmdDelay(5, 0);
                this.COM.DIYcmd(text);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            text = "%99R\r";
            try
            {
                this.COM.CmdDelay(20, 0);
                this.COM.DIYcmd(text);
                GetCmdResult();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DATA_ON(string num)
        {
            string text = "%" + num + "W{\"DATA\":\"ON\"}\\r";
            try
            {
                this.COM.CmdDelay(5, 0);
                this.COM.DIYcmd(text);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            text = "%99R\r";
            try
            {
                this.COM.CmdDelay(20, 0);
                this.COM.DIYcmd(text);
                GetCmdResult();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DATA_OFF(string num)
        {
            string text = "%" + num + "W{\"DATA\":\"OFF\"}\\r";
            try
            {
                this.COM.CmdDelay(5, 0);
                this.COM.DIYcmd(text);
            }
            catch(Exception ex)
            {
                throw ex;
            }
            text = "%99R\r";
            try
            {
                this.COM.CmdDelay(20, 0);
                this.COM.DIYcmd(text);
                GetCmdResult();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        void GetCmdResult()
        {
            string result = this.COM.GetDataReceive();
            if (!result.Contains("{\"CMD\":\"OK\"}"))
            {
                string errorInfo = result.Replace("%", "");
                if (result.Contains("E00"))
                {
                    errorInfo = "格式不对".I18n("UsbHubHelper_89F34664");
                }
                else if (result.Contains("E01"))
                {
                    errorInfo = "通道编号不对".I18n("UsbHubHelper_3C1B6517");
                }
                else if (result.Contains("E02"))
                {
                    errorInfo = "命令不对".I18n("UsbHubHelper_755F19DC");
                }
                else if (result.Contains("E03"))
                {
                    errorInfo = "参数不对".I18n("UsbHubHelper_F6EAEDBD");
                }
                throw new Exception(errorInfo);
            }
        }
        internal string ExecuteCmd(string cmd)
        {
            string output = "";
            Process process = new Process();     //创建进程对象
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";      //设定需要执行的命令
            startInfo.Arguments = "/C " + cmd;   //设定参数，其中的“/C”表示执行完命令后马上退出
            startInfo.UseShellExecute = false;     //不使用系统外壳程序启动
            startInfo.RedirectStandardInput = false;   //不重定向输入
            startInfo.RedirectStandardOutput = true;   //重定向输出
            startInfo.CreateNoWindow = true;     //不创建窗口
            process.StartInfo = startInfo;
            try
            {
                if (process.Start())       //开始进程
                {
                    process.WaitForExit(2000);  //这里等待进程结束，等待时间为指定的毫秒
                    output = process.StandardOutput.ReadToEnd();//读取进程的输出
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //if (process != null)
                //    process.Close();
            }
            return output;
        }
    }
}
