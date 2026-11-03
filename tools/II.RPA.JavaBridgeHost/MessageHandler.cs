using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.JavaBridgeHost
{
    public class MessageHandler
    {
        public event Action<NativeMessagingMessage> onMessage;
        private AutoResetEvent autoReset = null;
        public static List<int> windows = new List<int>();
        public MessageHandler()
        {

            Task.Run(async () =>
            {
                while (true)
                {
                    using (var stdin = Console.OpenStandardInput())
                    {
                        string input = "";
                        try
                        {
                            int length = 0;
                            byte[] bytes = new byte[4];
                            await stdin.ReadAsync(bytes, 0, 4);
                            length = BitConverter.ToInt32(bytes, 0);
                            byte[] buff = new byte[length];
                            stdin.Read(buff, 0, buff.Length);
                            input += Encoding.UTF8.GetString(buff);
                            if (string.IsNullOrEmpty(input))
                            {
                                if (autoReset != null)
                                    autoReset.Set();
                                return;
                            }
                            var msg = (NativeMessagingMessage)JsonHelper.JsonDesByNewtonJson(input);
                            if (msg.functionName == "openrpautilscript")
                            {
                                var r2 = new NativeMessagingMessage(msg.functionName, msg.debug);

                            }
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                }
            });
        }
        internal static string GetStringFromResource(string resourceName)
        {
            string[] names = typeof(MessageHandler).Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                if (name.EndsWith(resourceName))
                {
                    using (Stream stream = typeof(MessageHandler).Assembly.GetManifestResourceStream(name))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string result = reader.ReadToEnd();
                            return result;
                        }
                    }
                }
            }
            return null;
        }

        public static void LoadScript(ref NativeMessagingMessage msg, string name)
        {
            msg.script = GetStringFromResource(name + ".js");
        }
        public void WaitForExit()
        {
            autoReset = new AutoResetEvent(false);
            autoReset.WaitOne();
        }
        public void SendMessage(NativeMessagingMessage msg)
        {
            OpenStandardStreamOut(JsonHelper.JsonByNewtonJson(msg));
        }
        private static void OpenStandardStreamOut(string msgdata)
        {
            int dataLength = msgdata.Length;
            Stream stdout = Console.OpenStandardOutput();
            stdout.WriteByte((byte)((dataLength >> 0) & 0xFF));
            stdout.WriteByte((byte)((dataLength >> 8) & 0xFF));
            stdout.WriteByte((byte)((dataLength >> 16) & 0xFF));
            stdout.WriteByte((byte)((dataLength >> 24) & 0xFF));
            //Console.Write(msgdata);
        }
    }
}
