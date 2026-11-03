//using II.RPA.Logging;
//using II.RPA.Plugin.Core;
//using II.RPA.WebBrowser.Common.Basic;
//using II.RPA.WinAutomation.CommonUtility;
//using Microsoft.AspNet.SignalR.Client;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using System;
//using System.Threading;
//using System.Threading.Tasks;

//namespace II.RPA.Plugin.Browsers.Sogou
//{
//    internal class SogouConnection : IBrowserConnectionNew
//    {
//        private static ILogger logger = Logger.OfContext();
//        private object _object = new Object();
//        private Connection connection;
//        /// <summary>
//        ///  格式 "http://127.0.0.1:52050/rpa";
//        /// </summary>
//        private string signalRUrl = string.Empty;
//        //private int connection_timeout = 10000;
//        private AutoResetEvent waitEvent = new AutoResetEvent(false);

//        public bool IsConnected
//        {
//            get
//            {
//                //return connection != null && connection.State == ConnectionState.Connected;
//                return System.Diagnostics.Process.GetProcessesByName("II.RPA.BrowserAutomationServer").Length > 0;
//            }
//            set { }
//        }
//        public PipeResponse PipeResponse { get; set; }
//        public SogouConnection(string signalRUrl)
//        {
//            this.signalRUrl = signalRUrl;
//            InitConnection();
//        }
//        private void InitConnection()
//        {
//            try
//            {
//                if (connection == null)
//                {
//                    connection = new Connection(this.signalRUrl, "clientId=RPA");
//                    connection.TraceLevel = TraceLevels.All;
//                    connection.TraceWriter = Console.Out;
//                    //connection.Error += Connection_Error;
//                    connection.StateChanged += Connection_StateChanged;
//                    connection.Received += Connection_Received;
//                    connection.Start().Wait();
//                }
//                else if (connection.State == ConnectionState.Disconnected)
//                {
//                    connection.Start().Wait();
//                }
//            }
//            catch (Exception ex)
//            {
//                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Service Connected Eror:");
//                throw new SogouException(eErrorMessageType.Error_SogouServiceNotFound);
//            }
//        }

//        private void Connection_Received(string obj)
//        {
//            logger.LogInformation($"Service Response {obj}");
//            PipeResponse = JsonConvert.DeserializeObject<PipeResponse>(obj);
//            waitEvent.Set();
//        }
//        private void Connection_Error(Exception ex)
//        {
//            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"Service Error:");
//            try
//            {
//                connection.Dispose();
//                connection = null;
//            }
//            catch (Exception)
//            {

//                throw;
//            }
//        }
//        private void Connection_StateChanged(StateChange obj)
//        {
//            logger.LogInformation($"Service Sate: {obj.OldState}  {obj.NewState}");
//        }

//        public PipeResponse Send(string command, object args, int timeout)
//        {

//            logger.LogInformation($"Sogou Plugin Send:  {command} {args}");
//            waitEvent.Reset();
//            PipeResponse = null;
//            lock (_object)
//            {
                

//                if (IsConnected)
//                {
//                    InitConnection();
//                    //connection.Send(JsonConvert.SerializeObject(new { command = command, args = args })).Wait(timeout);
//                    //return PipeResponse;


//                    //while (timeout > 0)
//                    //{
//                    //new Thread(() =>
//                    //{
//                    //    connection.Send(JsonConvert.SerializeObject(new { command = command, args = args }));
//                    //}).Start();
//                    Task.Run(() =>
//                   {
//                       connection.Send(JsonConvert.SerializeObject(new { command = command, args = args }));
//                   });

//                    waitEvent.WaitOne(timeout);
//                    //if (PipeResponse == null)
//                    //{
//                    //    //retry
//                    //    //timeout -= 5000;
//                    //    continue;
//                    //}
//                    //break;
//                    //}
//                    return PipeResponse;
//                }
//                else
//                {
//                    logger.LogInformation($"Service Disconnect!");
//                    new SogouException(eErrorMessageType.Error_SogouServiceNotFound);
//                    return null;
//                }
//            }
//        }
//        public void Start()
//        {

//        }
//        public void Stop()
//        {
//            try
//            {
//                connection?.Dispose();
//            }
//            catch (Exception ex)
//            {
//                logger.LogInformation($"stop connect error:{ex}!");
//            }
//        }

//        public bool TestConnection()
//        {
//            return true;
//            ////try
//            ////{
//            ////    var address = pipeName;
//            ////    using (var remoteClient = ClientGenerator.Instance.Generate<IMessageExchange>(PipeConfig.DefaultPipe, address))
//            ////    {
//            ////        remoteClient.Service.Pending();
//            ////    }
//            ////    return true;
//            ////}
//            ////catch
//            ////{
//            ////    return false;
//            ////}
//        }
//    }
//}

