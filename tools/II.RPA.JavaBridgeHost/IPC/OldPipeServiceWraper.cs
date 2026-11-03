using System;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.NamedPipeWrapper;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;

namespace II.RPA.JavaBridgeHost.IPC
{
    internal class OldPipeServiceWraper : IDisposable
    {
        private CancellationTokenSource cts = null;
        private string oldPipeName = null;
        private NamedPipeServer robotPipe;
        private IExecuteMessageable executor;

        public OldPipeServiceWraper(IExecuteMessageable executor, string oldPipeName)
        {
            this.oldPipeName = oldPipeName;
        }

        public void Dispose()
        {
            try
            {
                this.cts?.Cancel();
            }
            catch
            { }
            try
            {
                lock (this)
                {
                    this.robotPipe?.Dispose();
                }
            }
            catch
            { }
        }

        internal void Startup()
        {
            this.cts = new CancellationTokenSource();
            Task.Factory.StartNew(this.StartupOldPipeService, new Tuple<string, CancellationToken>(this.oldPipeName, this.cts.Token), this.cts.Token);
        }

        private void StartupOldPipeService(object obj)
        {
            var param = (Tuple<string, CancellationToken>)obj;
            CancellationToken tokenSource = param.Item2;

            while (!tokenSource.IsCancellationRequested)
            {
                lock (this)
                {
                    this.robotPipe = new NamedPipeServer(param.Item1);
                }

                try
                {
                    this.robotPipe.WaitForConnection();
                    using (MessageReader robotReader = new MessageReader(this.robotPipe.BaseStream))
                    {
                        using (MessageWriter robotWriter = new MessageWriter(this.robotPipe.BaseStream))
                        {
                            while (!tokenSource.IsCancellationRequested)
                            {
                                JavaMessage request = robotReader.ReadMessage<JavaMessage>();
                                JavaMessage response = this.executor.ExecuteReceiveMessage(request);
                                robotWriter.WriteMessage(JsonConvert.SerializeObject(response));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLogger.Default.Error("Pipe exchange error" + ex.Message);
                }
                finally
                {
                    try
                    {
                        this.robotPipe.Dispose();
                    }
                    catch
                    {
                    }
                }

                Thread.Sleep(10);
            }
        }
    }
}