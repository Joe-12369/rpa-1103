using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.HIDTool
{
    public class HIDSendHelper
    {

        private HIDController HID = new HIDController();
        private KbUtils KUtils = new KbUtils();

        private uint FTimeout = 5000;  //approx five seconds

        private string need_shift_char = "~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:\"ZXCVBNM<>?";
        private string need_shift_char_key = "`1234567890-=qwertyuiop[]\\asdfghjkl;'zxcvbnm,./";

        public HIDSendHelper()
        {
            //tbLog.AppendText("\r\n");
            //create the HIDController 
            HID.OnLog += new EventHandler<LogArgs>(Log);
            HID.VendorID = (ushort)DriversConst.TTC_VENDORID;                //the Tetherscript vendorid
            HID.ProductID = (ushort)DriversConst.TTC_PRODUCTID_KEYBOARD;     //the Tetherscript Virtual Keyboard Driver productid
            HID.Connect();
            tmrPing_Thread();
        }

        public void Dispose()
        {
            HID.Disconnect();
        }
        #region 向外提供的方法
       /// <summary>
       /// 清空
       /// </summary>
       /// <param name="formName"></param>
        public void ClearAllText(string formName)
        {
            try
            {
                SetTarget(formName);
                //we'll need to send a CTRL-A simultaneously, then press the Delete key to clear the selected text.
                //for simplicity, we have pre-computed the values needed
                //Send LCTRL
                Send(1, 0, 0, 0, 0, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                //wait a bit
                System.Threading.Thread.Sleep(50);
                //Send A
                Send(1, 0, 4, 0, 0, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                Send(0, 0, 0, 0, 0, 0, 0, 0);
                //wait a bit
                System.Threading.Thread.Sleep(50);
                //Send Delete
                Send(0, 0, 76, 0, 0, 0, 0, 0);
                System.Threading.Thread.Sleep(50);
                Send(0, 0, 0, 0, 0, 0, 0, 0);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public void SendText(string formName, string text, int Down, int Interkey)
        {
            Down = Down < 100 ? 100 : Down;
            Interkey = Interkey < 100 ? 100 : Interkey;
            //NLogger.Default.Info("Need Send Text:" + text);
            try
            {
                SetTarget(formName);
                ReleaseAllKey();

                //this example sends a simple 'Hello'.  If you want to use other modifers or keys like 'SPACEBAR' or 'TAB' you'll need to tweak this code. 
                //we will put each character in the key0 slot, wait a bit, clear the slot, wait a bit and then do the next letter
                int m;
                byte m1;
                byte k1;
                if (text.ToLower() == "{enter}")
                {
                    m1 = 0;
                    k1 = KUtils.GetKeyKeyCode("ENTER");

                    if (k1 > 0)
                    {
                        //pressing the key down
                        Send(m1, 0, 0, 0, 0, 0, 0, 0);

                        System.Threading.Thread.Sleep(Down);
                        //pressing the key down
                        Send(m1, 0, k1, 0, 0, 0, 0, 0);
                        //keep the key pressed for this many ms.
                        System.Threading.Thread.Sleep(Down);
                        //release the key
                        Send(0, 0, 0, 0, 0, 0, 0, 0);
                        
                        //wait before sending the next key
                        System.Threading.Thread.Sleep(Interkey);
                    }
                    return;
                }
                if (text.ToLower() == "{backspace}")
                {
                    m1 = 0;
                    k1 = KUtils.GetKeyKeyCode("BACKSPACE");

                    if (k1 > 0)
                    {
                        //pressing the key down
                        Send(m1, 0, k1, 0, 0, 0, 0, 0);
                        //keep the key pressed for this many ms.
                        System.Threading.Thread.Sleep(Down);
                        //release the key
                        Send(0, 0, 0, 0, 0, 0, 0, 0);
                        //wait before sending the next key
                        System.Threading.Thread.Sleep(Interkey);
                    }
                    return;
                }
                //iterate through the text.  We'll send one character at a time
                for (int i = 0; i < text.Length; i++)
                {
                    char t = text[i];
                    //check for uppercase letters
                    //if (t == char.ToUpper(t))
                    int index = -1;
                    if ((index = need_shift_char.IndexOf(t)) >= 0)
                    {
                        m = KUtils.GetModifierKeyCode("[LSHIFT]");
                        t = need_shift_char_key[index];
                    }
                    else
                    {
                        //no modifier needed
                        m = -1;
                    }
                    //the modifier is represented by bit positions in a single byte
                    //in this example we are allowing only one modifier to be pressed
                    //but you could have something like a LALT-LSHIFT-A sequence which means
                    //that you would need to set the bit for LALT, then OR that with LSHIFT to give a value of 6
                    //in this example, we are only handling [LSHIFT]. 
                    //calc the modifier
                    switch (m)
                    {
                        case 0: m1 = 1; break;
                        case 1: m1 = 2; break;
                        case 2: m1 = 4; break;
                        case 3: m1 = 8; break;
                        case 4: m1 = 16; break;
                        case 5: m1 = 32; break;
                        case 6: m1 = 64; break;
                        case 7: m1 = 128; break;
                        default: m1 = 0; break;
                    }
                    //the keycode is the same whether it is capitalized or not
                    k1 = KUtils.GetKeyKeyCode(char.ToLower(t).ToString());

                    if (k1 > 0)
                    {
                        if (m1 > 0)
                        {
                            Send(m1, 0, 0, 0, 0, 0, 0, 0);
                            System.Threading.Thread.Sleep(Down);
                            //pressing the key down
                            Send(m1, 0, k1, 0, 0, 0, 0, 0);                         
                        }
                        else
                        {
                            //pressing the key down
                            Send(0, 0, k1, 0, 0, 0, 0, 0);
                        }
                        //keep the key pressed for this many ms.
                        System.Threading.Thread.Sleep(Down);
                        //release the key
                        Send(0, 0, 0, 0, 0, 0, 0, 0);
                        //wait before sending the next key
                        System.Threading.Thread.Sleep(Interkey);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }


        #endregion
        public void Log(object s, LogArgs e)
        {
            
            //tbLog.AppendText(e.Msg + "\r\n");
        }

        //here we send data to the keyboard driver
        void Send(Byte Modifier, Byte Padding, Byte Key0, Byte Key1, Byte Key2, Byte Key3, Byte Key4, Byte Key5)
        {
            SetFeatureKeyboard KeyboardData = new SetFeatureKeyboard();
            KeyboardData.ReportID = 1;
            KeyboardData.CommandCode = 2;
            KeyboardData.Timeout = FTimeout / 5; //5 because we count in blocks of 5 in the driver
            KeyboardData.Modifier = Modifier;
            //padding should always be zero.
            KeyboardData.Padding = Padding;
            KeyboardData.Key0 = Key0;
            KeyboardData.Key1 = Key1;
            KeyboardData.Key2 = Key2;
            KeyboardData.Key3 = Key3;
            KeyboardData.Key4 = Key4;
            KeyboardData.Key5 = Key5;
            //convert struct to buffer
            byte[] buf = getBytesSFJ(KeyboardData, Marshal.SizeOf(KeyboardData));
            //send filled buffer to driver
            HID.SendData(buf, (uint)Marshal.SizeOf(KeyboardData));
        }

        //for converting a struct to byte array
        public byte[] getBytesSFJ(SetFeatureKeyboard sfj, int size)
        {
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(sfj, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        private void tmrPing_Thread()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Ping();
                    Thread.Sleep(200);
                }
            });
        }
        //private void tmrPing_Tick(object sender, EventArgs e)
        //{
        //    tmrPing.Stop();
        //    Ping();
        //    tmrPing.Start();
        //}

        //here we send a ping to the keyboard driver
        void Ping()
        {
            SetFeatureKeyboard KeyboardData = new SetFeatureKeyboard();
            KeyboardData.ReportID = 1;
            KeyboardData.CommandCode = 3;
            //the timeout is how long the driver will wait (milliseconds) without receiving a ping before resetting itself
            //we'll be pinging every 200ms, and loss of ping will cause driver reset in FTimeout.  
            //No more stuck keys requiring reboot to clear.
            //the following fields are not used by the driver for a ping, but we'll zero them anyways
            KeyboardData.Timeout = FTimeout / 5; //50 because we count in blocks of 50 in the driver;
            KeyboardData.Modifier = 0;
            KeyboardData.Padding = 0;
            KeyboardData.Key0 = 0;
            KeyboardData.Key1 = 0;
            KeyboardData.Key2 = 0;
            KeyboardData.Key3 = 0;
            KeyboardData.Key4 = 0;
            KeyboardData.Key5 = 0;
            //convert struct to buffer
            byte[] buf = getBytesSFJ(KeyboardData, Marshal.SizeOf(KeyboardData));
            //send filled buffer to driver
            HID.SendData(buf, (uint)Marshal.SizeOf(KeyboardData));
        }
        void ReleaseAllKey()
        {
            SetFeatureKeyboard KeyboardData = new SetFeatureKeyboard();
            KeyboardData.ReportID = 1;
            KeyboardData.CommandCode = 1;
            //the timeout is how long the driver will wait (milliseconds) without receiving a ping before resetting itself
            //we'll be pinging every 200ms, and loss of ping will cause driver reset in FTimeout.  
            //No more stuck keys requiring reboot to clear.
            //the following fields are not used by the driver for a ping, but we'll zero them anyways
            KeyboardData.Timeout = FTimeout / 5; //50 because we count in blocks of 50 in the driver;
            KeyboardData.Modifier = 0;
            KeyboardData.Padding = 0;
            KeyboardData.Key0 = 0;
            KeyboardData.Key1 = 0;
            KeyboardData.Key2 = 0;
            KeyboardData.Key3 = 0;
            KeyboardData.Key4 = 0;
            KeyboardData.Key5 = 0;
            //convert struct to buffer
            byte[] buf = getBytesSFJ(KeyboardData, Marshal.SizeOf(KeyboardData));
            //send filled buffer to driver
            HID.SendData(buf, (uint)Marshal.SizeOf(KeyboardData));
        }

        /// <summary>
        /// 激活窗口
        /// </summary>
        /// <param name="formName"></param>
        private void SetTarget(string formName)
        {
            try
            {
                //if (string.IsNullOrEmpty(formName))
                //    return;
                //we could just dump the text to the editbox on the main form, but they sleep delays will give odd effects and cause key repeats to not look correct since
                //the main thread is sleeping, the ui isn't updated with the keystrokes until the sleep is finished.
                //tbLog.Focus();  
                KUtils.AppActivate(formName, 300);

            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}
