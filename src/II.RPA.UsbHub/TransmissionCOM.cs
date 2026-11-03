using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.UsbHub
{
        public class TransmissionCOM
        {
            private string DataSend = "";

            private string DataReceive = "";

            private byte[] data_receive;

            private SerialPort spSerial = new SerialPort();

            private int SendDelay;

            private int ReceiveDelay;

            public void Connect(string COM)
            {
                this.spSerial.PortName = COM;
                this.spSerial.BaudRate = 115200;
                this.spSerial.DataBits = 8;
                this.spSerial.Parity = Parity.None;
                this.spSerial.StopBits = StopBits.One;
                if(this.spSerial.IsOpen)
                {
                    this.spSerial.Close();
                }
                this.spSerial.Open();
            }

            public void Disconnect()
            {
                if (this.spSerial.IsOpen)
                {
                    this.spSerial.Close();
                }
            }

            public void CmdDelay(int sendDelay, int receiveDelay)
            {
                this.SendDelay = sendDelay;
                this.ReceiveDelay = receiveDelay;
            }

            public void DIYcmd(string Function)
            {
                this.COM_CMD(Function);
            }

            public void RefreshDataReceive()
            {
            }

            public string GetDataSend()
            {
                return this.DataSend;
            }

            public string GetDataReceive()
            {
                return this.DataReceive;
            }

            public void ClearData()
            {
                if (this.spSerial.IsOpen)
                {
                    this.spSerial.DiscardInBuffer();
                    this.spSerial.DiscardOutBuffer();
                    byte[] array = new byte[this.spSerial.BytesToRead];
                    this.spSerial.Read(array, 0, array.Length);
                    this.spSerial.DiscardInBuffer();
                    this.spSerial.DiscardOutBuffer();
                }
            }

            private byte[] COM_CMD(string Function)
            {
                try
                {
                    this.spSerial.DiscardInBuffer();
                    this.spSerial.DiscardOutBuffer();
                }
                catch
                {
                }
                byte[] result;
                try
                {
                    if (this.spSerial.IsOpen)
                    {
                        byte[] array = new byte[this.spSerial.BytesToRead];
                        this.spSerial.Read(array, 0, array.Length);
                    }
                    this.DataSend = "";
                    this.DataReceive = "";
                    string[] array2 = Function.Split(new char[]
                    {
                    ' '
                    });
                    byte[] bytes = Encoding.ASCII.GetBytes(Function);
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        this.DataSend = this.DataSend + bytes[i].ToString() + " ";
                    }
                    //Console.WriteLine(this.DataSend);
                    byte[] array3 = new byte[1024];
                    if (this.spSerial.IsOpen)
                    {
                        this.spSerial.Write(bytes, 0, bytes.Length);
                        Thread.Sleep(this.SendDelay);
                        if (this.spSerial.IsOpen)
                        {
                            array3 = new byte[this.spSerial.BytesToRead];
                            this.spSerial.Read(array3, 0, array3.Length);
                        }
                    }
                    Thread.Sleep(this.ReceiveDelay);
                    for (int j = 0; j < array3.Length; j++)
                    {
                        this.DataReceive = Encoding.ASCII.GetString(array3);
                    }
                    //Console.WriteLine(this.DataReceive);
                    result = array3;
                }
                catch
                {
                    byte[] array4 = new byte[1024];
                    result = array4;
                }
                return result;
            }

            private byte[] Refresh_CMD(string Function)
            {
                try
                {
                    this.spSerial.DiscardInBuffer();
                    this.spSerial.DiscardOutBuffer();
                }
                catch
                {
                }
                byte[] result;
                try
                {
                    if (this.spSerial.IsOpen)
                    {
                        byte[] array = new byte[this.spSerial.BytesToRead];
                        this.spSerial.Read(array, 0, array.Length);
                    }
                    this.DataReceive = "";
                    byte[] array2 = new byte[1024];
                    array2 = new byte[this.spSerial.BytesToRead];
                    this.spSerial.Read(array2, 0, array2.Length);
                    Thread.Sleep(this.ReceiveDelay);
                    for (int i = 0; i < array2.Length; i++)
                    {
                        this.DataReceive = this.DataReceive + array2[i].ToString("X2") + " ";
                    }
                    result = array2;
                }
                catch
                {
                    byte[] array3 = new byte[1024];
                    result = array3;
                }
                return result;
            }
        }
}
