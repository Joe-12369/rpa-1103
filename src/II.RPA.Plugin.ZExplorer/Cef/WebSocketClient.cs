using II.RPA.Plugin.ZExplorer.Cef.EventArgs;
using II.RPA.Plugin.ZExplorer.Cef.EventArgs;
using System;
using System.Threading;
using WebSocket4Net;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public sealed class WebSocketClient : IDisposable
    {
        public bool IsAlive
        {
            get
            {
                return this.webSocket.State == WebSocketState.Open;
            }
        }

        public WebSocketState ReadyState
        {
            get
            {
                switch (this.webSocket.State)
                {
                    case WebSocketState.Connecting:
                        return WebSocketState.Connecting;
                    case WebSocketState.Open:
                        return WebSocketState.Open;
                    case WebSocketState.Closing:
                        return WebSocketState.Closing;
                    case WebSocketState.Closed:
                        return WebSocketState.Closed;
                    default:
                        return WebSocketState.Closed;
                }
            }
        }

        public WebSocketClient(string url)
        {
            this.webSocket = new WebSocket(url);
            this.webSocket.Security.AllowUnstrustedCertificate = true;
            this.webSocket.EnableAutoSendPing = false;
            this.webSocket.Closed += this.RaiseOnClose;
            this.webSocket.Error += this.RaiseOnError;
            this.webSocket.MessageReceived += this.RaiseOnMessage;
            this.webSocket.Opened += this.RaiseOnOpen;
        }

        public event EventHandler<CloseEventArgs> OnClose;

        public event EventHandler<Plugin.ZExplorer.Cef.EventArgs.ErrorEventArgs> OnError;

        public event EventHandler<MessageEventArgs> OnMessage;

        public event EventHandler OnOpen;

        public void Close()
        {
            this.webSocket.Close();
        }

        public void Connect()
        {
            this.webSocket.Open();
            this.connected.WaitOne(this.connectionTimeout);
        }

        public void Dispose()
        {
            this.webSocket.Close();
        }

        public void Send(string data)
        {
            this.webSocket.Send(data);
        }

        private void RaiseOnClose(object sender, System.EventArgs e)
        {
            EventHandler<CloseEventArgs> onClose = this.OnClose;
            if (onClose == null)
            {
                return;
            }
            onClose(sender, new CloseEventArgs(e.ToString(), true));
        }

        private void RaiseOnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            EventHandler<Plugin.ZExplorer.Cef.EventArgs.ErrorEventArgs> onError = this.OnError;
            if (onError == null)
            {
                return;
            }
            onError(sender, new Plugin.ZExplorer.Cef.EventArgs.ErrorEventArgs(e.Exception));
        }

        private void RaiseOnMessage(object sender, MessageReceivedEventArgs e)
        {
            EventHandler<MessageEventArgs> onMessage = this.OnMessage;
            if (onMessage == null)
            {
                return;
            }
            onMessage(sender, new MessageEventArgs(e.Message));
        }

        private void RaiseOnOpen(object sender, System.EventArgs e)
        {
            this.connected.Set();
            EventHandler onOpen = this.OnOpen;
            if (onOpen == null)
            {
                return;
            }
            onOpen(sender, e);
        }

        private readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(5.0);

        private readonly AutoResetEvent connected = new AutoResetEvent(false);

        private readonly WebSocket webSocket;
    }
}
