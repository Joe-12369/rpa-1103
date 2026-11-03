using II.RPA.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    internal class JavaProxy
    {
        private static ILogger logger = Logger.OfContext();       
        internal JavaProxy(TcpClient tcpClient, int targetProcId)
        {
            TargetProcessId = targetProcId;
            InitializeStream(tcpClient, targetProcId);
        }

        private void InitializeStream(TcpClient tcpClient, int targetProcId)
        {
            this.tcpClient = tcpClient;
            stream = tcpClient.GetStream();
            tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, true);
            tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug, true);
            stream.ReadTimeout = 30000;
            TargetProcessId = targetProcId;
            id = ++IdCounter;
        }

        public bool Connected
        {
            get
            {
                return this.tcpClient.Connected;
            }
        }

        TcpClient TcpClient
        {
            get
            {
                return this.tcpClient;
            }
        }

        internal static JavaProxy Create(int targetProcId,int times=1)
        {
            JavaProxy javaProxy = null;
            try
            {             
                Dictionary<int, JavaProxy> obj = cachedConnections;
                lock (obj)
                {
                    CleanupConnectionCache();
                    JavaProxy javaProxy2;
                    if (cachedConnections.TryGetValue(targetProcId, out javaProxy2) && javaProxy2.Connected)
                    {
                        javaProxy = javaProxy2;
                    }
                }
                if (javaProxy != null)
                {
                    try
                    {
                        PingProxy(javaProxy);
                        logger.LogInformation("Successful reuse of proxy for pid: " + targetProcId.ToString());
                        return javaProxy;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Found dead proxy for pid: " + targetProcId.ToString(), ex);
                        obj = cachedConnections;
                        lock (obj)
                        {
                            javaProxy.Close();
                            cachedConnections.Remove(targetProcId);
                        }
                    }
                }

                logger.LogInformation("Creating new proxy for connecting to pid: " + targetProcId.ToString());
                for (int j = 1; j < times; j++)
                {
                    TcpClient tcpClient = new TcpClient
                    {
                        LingerState =
                    {
                        Enabled = false
                    }
                    };
                    for (int i = 0; i < 5; i++)
                    {
                        var num = (int)(10000 + (i * 3333 + targetProcId % 40000));
                        if (IsJavaListening(num))
                        {
                            try
                            {
                                tcpClient.Connect(JavaLocalAddress, num);
                                JavaProxy javaProxy3 = CreateProxyInternal(tcpClient, targetProcId);
                                obj = cachedConnections;
                                lock (obj)
                                {
                                    cachedConnections[targetProcId] = javaProxy3;
                                }
                                return javaProxy3;
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning("Failed to connect to Java process at port: " + num.ToString(), ex);
                            }
                        }
                    }
                    tcpClient.Close();
                    if (times > 1)
                    {
                        System.Threading.Thread.Sleep(500);
                        logger.LogInformation("Retrying to connect times: " + (j + 1));
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Error creating JavaProxy for pid: " + targetProcId.ToString());
            }           
            return null;
        }

        internal JavaObjectWrapper InvokeGetAwtWindow(int hwnd, out string[] clazzes)
        {
            clazzes = null;
            CallStatus callStatus;
            object[] array = this.CallFunction(JavaTech.DontCare, FunctionId.FuncGetWindow, new object[]
            {
                hwnd
            }, out callStatus);
            if (array.Length >= 4 && callStatus == CallStatus.Ok)
            {
                clazzes = new string[]
                {
                    (string)array[1],
                    (string)array[2],
                    (string)array[3]
                };
                return (JavaObjectWrapper)array[0];
            }
            return null;
        }
        internal object[] InvokeGetWindows()
        {
            CallStatus callStatus;
            object[] array = this.CallFunction(JavaTech.DontCare, 0, new object[]
            {
            }, out callStatus);
            if (callStatus == CallStatus.Ok)
            {
                return array;
            }
            return null;
        }

        internal JavaObjectWrapper InvokeGetAwtApplet(int hwnd, out string[] clazzes)
        {
            clazzes = null;
            CallStatus callStatus;
            object[] array = CallFunction(JavaTech.DontCare, FunctionId.FuncGetApplet, new object[]
            {
                hwnd
            }, out callStatus);
            if (array.Length >= 4 && callStatus == CallStatus.Ok)
            {
                clazzes = new string[]
                {
                    (string)array[1],
                    (string)array[2],
                    (string)array[3]
                };
                return (JavaObjectWrapper)array[0];
            }
            return null;
        }

        internal JavaObjectWrapper InvokeGetFxWindow(int hwnd, out string[] clazzes)
        {
            clazzes = null;
            CallStatus callStatus;
            object[] array = CallFunction(JavaTech.Fx, FunctionId.FuncGetWindow, new object[]
            {
                hwnd
            }, out callStatus);
            if (array.Length >= 4 && callStatus == CallStatus.Ok)
            {
                clazzes = new string[]
                {
                    (string)array[1],
                    (string)array[2],
                    (string)array[3]
                };
                return (JavaObjectWrapper)array[0];
            }
            return null;
        }

        internal JavaObjectWrapper InvokeGetSwtWindow(int hwnd, out string[] clazzes)
        {
            clazzes = null;
            CallStatus callStatus;
            object[] array = this.CallFunction(JavaTech.Swt, FunctionId.FuncGetWindow, new object[]
            {
                hwnd
            }, out callStatus);
            if (array.Length >= 4 && callStatus == CallStatus.Ok)
            {
                clazzes = new string[]
                {
                    (string)array[1],
                    (string)array[2],
                    (string)array[3]
                };
                return (JavaObjectWrapper)array[0];
            }
            return null;
        }

        internal object[] CallFunction(JavaTech tech, FunctionId func, object[] args, out CallStatus status)
        {

            func = (FunctionId)(tech + (int)func);
            object obj = this.commLock;
            object[] array;
            lock (obj)
            {
                try
                {
                    this.ClearStream();
                    JavaProxy.logger.LogTrace(string.Format("CallFunction: {0} argcnt: {1} pid:{2} proxyId:{3}", new object[]
                    {
                        func,
                        args.Length,
                        this.TargetProcessId,
                        this.id
                    }));
                    using (MemoryStream memoryStream = new MemoryStream(64))
                    {
                        memoryStream.Write(this.MagicNumber, 0, 4);
                        memoryStream.WriteByte((byte)func);
                        memoryStream.WriteByte((byte)(args.Length >> 8));
                        memoryStream.WriteByte((byte)(args.Length & 255));
                        foreach (object arg in args)
                        {
                            JavaProxy.WriteField(memoryStream, arg);
                        }
                        memoryStream.WriteTo(this.stream);
                        this.stream.Flush();
                        JavaProxy.logger.LogTrace(string.Format("CallFunction flushed, read header (pid:{0} proxyId:{1})", this.TargetProcessId, this.id));
                        byte[] buffer = new byte[7];
                        JavaProxy.ReadAllBytesFromStream(this.stream, buffer);
                        if (buffer[0] != this.MagicNumber[0] || buffer[1] != this.MagicNumber[1] || buffer[2] != this.MagicNumber[2] || buffer[3] != this.MagicNumber[3])
                        {
                            throw new Exception("Invalid header magic.");
                        }
                        status = (CallStatus)buffer[4];
                        int fieldCount = ((int)buffer[5] << 8) + (int)buffer[6];
                        CallStatus statusCopy = status;
                        JavaProxy.logger.LogTrace(string.Format("CallFunction reader ok, status: {0} fields:{1} pid:{2} proxyId:{3}", new object[]
                        {
                        statusCopy,
                        fieldCount,
                        this.TargetProcessId,
                        this.id
                        }));
                        object[] result = new object[fieldCount];
                        for (int j = 0; j < fieldCount; j++)
                        {
                            result[j] = this.ReadField(this.stream, tech);
                        }
                        JavaProxy.logger.LogTrace(string.Format("CallFunction complete: pid: {0} proxyId: {1}", this.TargetProcessId, this.id));
                        array = result;
                    }
                }
                catch (ObjectDisposedException)
                {
                    this.Recreate();
                    array = this.CallFunction(tech, func, args, out status);
                }
            }
            return array;
        }

        internal void Close()
        {
            try
            {
                this.TcpClient.Close();
            }
            catch (Exception)
            {
            }
        }

        private static void WriteField(MemoryStream memStream, object arg)
        {
            if (arg is JavaObjectWrapper)
            {
                memStream.WriteByte(3);
                memStream.Write(JavaProxy.FromLong(((JavaObjectWrapper)arg).ObjectId), 0, 8);
                return;
            }
            if (arg is long)
            {
                memStream.WriteByte(7);
                memStream.Write(JavaProxy.FromLong((long)arg), 0, 8);
                return;
            }
            if (arg is int)
            {
                memStream.WriteByte(2);
                memStream.Write(JavaProxy.FromInt((int)arg), 0, 4);
                return;
            }
            if (arg is float)
            {
                memStream.WriteByte(6);
                memStream.Write(JavaProxy.FromFloat((float)arg), 0, 4);
                return;
            }
            if (arg is double)
            {
                memStream.WriteByte(8);
                memStream.Write(JavaProxy.FromDouble((double)arg), 0, 8);
                return;
            }
            if (arg is bool)
            {
                memStream.WriteByte(1);
                memStream.WriteByte((byte)(((bool)arg) ? byte.MaxValue : 0));
                return;
            }
            if (arg is string)
            {
                string s = (string)arg;
                memStream.WriteByte(4);
                byte[] bytes = Encoding.UTF8.GetBytes(s);
                memStream.Write(JavaProxy.FromInt(bytes.Length), 0, 4);
                memStream.Write(bytes, 0, bytes.Length);
                return;
            }
            if (arg is Rectangle)
            {
                Rectangle rectangle = (Rectangle)arg;
                memStream.WriteByte(9);
                memStream.Write(JavaProxy.FromInt(rectangle.X), 0, 4);
                memStream.Write(JavaProxy.FromInt(rectangle.Y), 0, 4);
                memStream.Write(JavaProxy.FromInt(rectangle.Width), 0, 4);
                memStream.Write(JavaProxy.FromInt(rectangle.Height), 0, 4);
                return;
            }
            if (arg is Point)
            {
                Point point = (Point)arg;
                memStream.WriteByte(10);
                memStream.Write(JavaProxy.FromInt(point.X), 0, 4);
                memStream.Write(JavaProxy.FromInt(point.Y), 0, 4);
                return;
            }
            if (arg is Size)
            {
                Size size = (Size)arg;
                memStream.WriteByte(11);
                memStream.Write(JavaProxy.FromInt(size.Width), 0, 4);
                memStream.Write(JavaProxy.FromInt(size.Height), 0, 4);
                return;
            }
            if (arg == null)
            {
                memStream.WriteByte(5);
                return;
            }
            throw new Exception($"The type {arg.GetType()?.ToString()} is not supported in Java remote calls.");
        }

        private static void ReadAllBytesFromStream(Stream s, byte[] buffer)
        {
            int num;
            for (int i = 0; i < buffer.Length; i += num)
            {
                num = s.Read(buffer, i, buffer.Length - i);
                if (num == 0)
                {
                    throw new Exception("Unexpected end of stream");
                }
            }
        }

        private static long GetLong(byte[] b)
        {
            Array.Reverse(b);
            return BitConverter.ToInt64(b, 0);
        }

        private static int GetInt(byte[] b)
        {
            return (int)b[0] << 24 | (int)b[1] << 16 | (int)b[2] << 8 | (int)b[3];
        }

        private static int GetInt(byte[] b, int i)
        {
            return (int)b[i] << 24 | (int)b[i + 1] << 16 | (int)b[i + 2] << 8 | (int)b[i + 3];
        }

        private static float GetFloat(byte[] b)
        {
            byte[] array = new byte[]
            {
                b[3],
                0,
                0,
                b[0]
            };
            array[2] = b[1];
            array[1] = b[2];
            return BitConverter.ToSingle(array, 0);
        }

        private static double GetDouble(byte[] b)
        {
            return BitConverter.ToDouble(new byte[]
            {
                b[7],
                b[6],
                b[5],
                b[4],
                b[3],
                b[2],
                b[1],
                b[0]
            }, 0);
        }

        private static byte[] FromLong(long val)
        {
            return
                new byte[] {
                    (byte)((val & -72057594037927936L) >> 0x38),
                    (byte)((val & 0xff000000000000L) >> 0x30),
                    (byte)((val & 0xff0000000000L) >> 40),
                    (byte)((val & 0xff00000000L) >> 0x20),
                    (byte)((val & 0xff000000) >> 0x18),
                    (byte)((val & 0xff0000L) >> 0x10),
                    (byte)((val & 0xff00L) >> 8),
                    (byte)(val & 0xffL)
                };

        }

        private static byte[] FromInt(int val)
        {
            return
    new byte[] { (byte)((val & 0xff000000) >> 0x18), (byte)((val & 0xff0000) >> 0x10), (byte)((val & 0xff00) >> 8), (byte)(val & 0xff) };

        }

        private static byte[] FromFloat(float val)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            byte[] array = new byte[]
            {
                bytes[3],
                0,
                0,
                bytes[0]
            };
            array[2] = bytes[1];
            array[1] = bytes[2];
            return array;
        }

        private static byte[] FromDouble(double val)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            return new byte[]
            {
                bytes[7],
                bytes[6],
                bytes[5],
                bytes[4],
                bytes[3],
                bytes[2],
                bytes[1],
                bytes[0]
            };
        }

        private static bool IsJavaListening(long targetPort)
        {
            bool result;
            try
            {
                TcpListener tcpListener = new TcpListener(JavaProxy.JavaLocalAddress, Convert.ToInt32(targetPort));
                tcpListener.ExclusiveAddressUse = true;
                tcpListener.Start(0);
                tcpListener.Stop();
                result = false;
            }
            catch (Exception)
            {
                result = true;
            }
            return result;
        }

        private static void CleanupConnectionCache()
        {
            if (JavaProxy.cachedConnections.Count > 100)
            {
                foreach (KeyValuePair<int, JavaProxy> keyValuePair in cachedConnections)
                {
                    try
                    {
                        keyValuePair.Value.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
                JavaProxy.cachedConnections.Clear();
            }
            foreach (KeyValuePair<int, JavaProxy> keyValuePair2 in cachedConnections)
            {
                try
                {
                    if (!keyValuePair2.Value.Connected)
                    {
                        keyValuePair2.Value.Close();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static JavaProxy CreateProxyInternal(TcpClient tcp, int targetProcessId)
        {
            JavaProxy javaProxy = new JavaProxy(tcp, targetProcessId);
            JavaProxy.PingProxy(javaProxy);
            return javaProxy;
        }

        private static void PingProxy(JavaProxy jp)
        {
            jp.TcpClient.ReceiveTimeout = 1000;
            CallStatus callStatus;
            jp.CallFunction(JavaTech.DontCare, FunctionId.FuncPing, new object[0], out callStatus);
            jp.TcpClient.ReceiveTimeout = 30000;
        }

        private void ClearStream()
        {
            this.stream.Flush();
            int tmp2;
            while (this.stream.DataAvailable && (tmp2 = this.stream.Read(this.unused, 0, this.unused.Length)) > 0)
            {
                int tmp = tmp2;
                JavaProxy.logger.LogTrace("ClearStream: gooble up fuzz: " + tmp.ToString() + " bytes");
            }
        }

        private object ReadField(Stream stream, JavaTech tech)
        {
            int num = stream.ReadByte();
            if (num == -1)
            {
                throw new Exception("Unexpected end of stream.");
            }
            FieldType fieldType = (FieldType)num;
            if (fieldType == FieldType.IdField)
            {
                byte[] array = new byte[8];
                ReadAllBytesFromStream(stream, array);
                long @long = GetLong(array);
                return new JavaObjectWrapper(this, tech, @long);
            }
            if (fieldType == FieldType.IntField)
            {
                byte[] array2 = new byte[4];
                ReadAllBytesFromStream(stream, array2);
                return GetInt(array2);
            }
            if (fieldType == FieldType.FloatField)
            {
                byte[] array3 = new byte[4];
                ReadAllBytesFromStream(stream, array3);
                return GetFloat(array3);
            }
            if (fieldType == FieldType.DoubleField)
            {
                byte[] array4 = new byte[8];
                ReadAllBytesFromStream(stream, array4);
                return GetDouble(array4);
            }
            if (fieldType == FieldType.LongField)
            {
                byte[] array5 = new byte[8];
                ReadAllBytesFromStream(stream, array5);
                return GetLong(array5);
            }
            if (fieldType == FieldType.BoolField)
            {
                byte[] array6 = new byte[1];
                ReadAllBytesFromStream(stream, array6);
                return array6[0] > 0;
            }
            if (fieldType == FieldType.StringField)
            {
                byte[] array7 = new byte[4];
                ReadAllBytesFromStream(stream, array7);
                byte[] array8 = new byte[GetInt(array7)];
                ReadAllBytesFromStream(stream, array8);
                return Encoding.UTF8.GetString(array8);
            }
            if (fieldType == FieldType.RectangleField)
            {
                byte[] array9 = new byte[16];
                ReadAllBytesFromStream(stream, array9);
                return new Rectangle(GetInt(array9, 0), GetInt(array9, 4), GetInt(array9, 8), GetInt(array9, 12));
            }
            if (fieldType == FieldType.PointField)
            {
                byte[] array10 = new byte[8];
                ReadAllBytesFromStream(stream, array10);
                return new Point(GetInt(array10, 0), GetInt(array10, 4));
            }
            if (fieldType == FieldType.DimensionField)
            {
                byte[] array11 = new byte[8];
                ReadAllBytesFromStream(stream, array11);
                return new Size(GetInt(array11, 0), GetInt(array11, 4));
            }
            if (fieldType == FieldType.NullField)
            {
                return null;
            }
            throw new Exception("Unknown field type: " + fieldType.ToString());
        }

        private void Recreate()
        {
            JavaProxy javaProxy = Create(this.TargetProcessId);
            this.InitializeStream(javaProxy.tcpClient, javaProxy.TargetProcessId);
            Dictionary<int, JavaProxy> obj = cachedConnections;
            lock (obj)
            {
                cachedConnections[this.TargetProcessId] = this;
            }
        }

        private static readonly IPAddress JavaLocalAddress = IPAddress.Parse("127.0.0.1");


        private static readonly Dictionary<int, JavaProxy> cachedConnections = new Dictionary<int, JavaProxy>();

        private static int IdCounter = 1;

        private readonly byte[] MagicNumber = new byte[]
{
            66,
            17,
            57,
            165
};

        private readonly object commLock = new object();

        private readonly byte[] unused = new byte[8192];

        private int id;

        public  int TargetProcessId;

        private TcpClient tcpClient;

        private NetworkStream stream;

        internal enum FieldType
        {
            BoolField = 1,
            IntField,
            IdField,
            StringField,
            NullField,
            FloatField,
            LongField,
            DoubleField,
            RectangleField,
            PointField,
            DimensionField
        }
    }
}

