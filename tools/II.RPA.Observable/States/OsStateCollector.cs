using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using Cassia;
using II.RPA.Observable.Contracts.States;
using II.RPA.Observable.Framework.Interops;
using II.RPA.Observable.States.OsStates;
using Microsoft.Extensions.Logging;
using ConnectionState = II.RPA.Observable.States.OsStates.ConnectionState;

namespace II.RPA.Observable.States
{
    public class OsStateCollector : StateCollector<OsState>,
        IOsStateCollector
    {
        public OsStateCollector() {}

        public override void Setup(CancellationTokenSource cts)
        {
            var commonInterval = TimeSpan.FromSeconds(1);

            System.Reactive.Linq.Observable.Interval(commonInterval)
                .Subscribe(
                    x =>
                    {
                        var session = GetOsSession();

                        if (session != null)
                        {
                            this.State.OsSession = session;
                        }
                    },
                    cts.Token);

            System.Reactive.Linq.Observable.Interval(commonInterval)
                .Subscribe(
                    x =>
                    {
                        var screenInfo = GetScreenInfo();

                        if (screenInfo != null)
                        {
                            this.State.ScreenInfo = screenInfo;
                        }
                    },
                    cts.Token);

            System.Reactive.Linq.Observable.Interval(commonInterval)
               .Subscribe(
                   x =>
                   {
                       var screenLocked = GetIsScreenLocked();

                       if (screenLocked != null)
                       {
                           this.State.IsScreenLocked = screenLocked.Value;
                       }
                   },
                   cts.Token);
        }

        private OsSession GetOsSession()
        {
            try
            {
                List<ITerminalServicesSession> sessions;
                var manager = new TerminalServicesManager();

                using (var server = manager.GetLocalServer())
                {
                    server.Open();
                    sessions = server.GetSessions().ToList();
                    server.Close();
                }

                var session = sessions
                    .FirstOrDefault(x => x.UserName == State.UserName);

                if (session != null)
                {
                    return new OsSession
                    {
                        ClientName = session.ClientName,
                        ConnectionState = session.ConnectionState
                            .ToString()
                            .Enum<ConnectionState>().Value,
                        ConnectTime = session.ConnectTime,
                        CurrentTime = session.CurrentTime,
                        DisconnectTime = session.DisconnectTime,
                        DomainName = session.DomainName,
                        IdleTime = session.IdleTime,
                        LastInputTime = session.LastInputTime,
                        LoginTime = session.LoginTime,
                        SessionId = session.SessionId,
                        WindowStationName = session.WindowStationName,
                    };
                }

                return null;
            }
            catch
            {
                return default;
            }
        }

        private bool? GetIsScreenLocked()
        {
            try
            {
                return Win32Api.GetIsScreenLocked();
            }
            catch {}

            return default;
        }

        private ScreenInfo GetScreenInfo()
        {
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    var intPtr = g.GetHdc();
                    var width = Win32Api.GetDeviceCaps(intPtr, 118);
                    var height = Win32Api.GetDeviceCaps(intPtr, 117);

                    var scalingFactor = (1d * width) /
                        SystemParameters.PrimaryScreenWidth;

                    return new ScreenInfo
                    {
                        Width = width,
                        Height = height,
                        Scale = scalingFactor,
                    };
                }
            }
            catch {}

            return default;
        }
    }
}
