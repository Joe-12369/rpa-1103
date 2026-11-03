using II.RPA.Logging;
using II.RPA.Plugin.SAP.Utils;
using Microsoft.Extensions.Logging;
using sapfewse;
using System;
using System.Runtime.InteropServices;

namespace II.RPA.Plugin.SAP
{
    /// <summary>
    /// SAP GUI Scripting 杩炴帴绠＄悊鍣ㄣ€?
    /// 璐熻矗 SAP GUI COM 杩炴帴鐨勫缓绔嬨€佷細璇濊矾鐢变笌璧勬簮閲婃斁銆?
    /// 鍙栦唬鍘熸湁鐨?static AttachSap() 鍜?GlobalSession 鍏ㄥ眬闈欐€佸彉閲忋€?
    /// </summary>
    public class SapConnectionManager : IDisposable
    {
        private static readonly ILogger logger = Logger.OfContext();
        private static readonly object _sync = new object();

        private GuiApplication _app;
        private GuiConnection _connection;
        private GuiSession _currentSession;

        private bool _disposed;

        /// <summary>
        /// 鑾峰彇褰撳墠 SAP GUI Application 瀵硅薄銆?
        /// </summary>
        public GuiApplication App => _app;

        /// <summary>
        /// 鑾峰彇褰撳墠 SAP GUI Connection 瀵硅薄銆?
        /// </summary>
        public GuiConnection Connection => _connection;

        /// <summary>
        /// 鑾峰彇褰撳墠 SAP GUI Session 瀵硅薄銆?
        /// </summary>
        public GuiSession CurrentSession
        {
            get
            {
                lock (_sync)
                    return _currentSession;
            }
        }

        /// <summary>
        /// 鏄惁宸茶繛鎺ュ埌 SAP GUI銆?
        /// </summary>
        public bool IsConnected
        {
            get
            {
                try
                {
                    lock (_sync)
                        return _currentSession != null && _currentSession.Id != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 寤虹珛涓?SAP GUI 鐨?Scripting 杩炴帴銆?
        /// 绾跨▼瀹夊叏锛岄噸澶嶈皟鐢ㄤ笉浼氶噸鏂拌繛鎺ャ€?
        /// </summary>
        public void Connect()
        {
            lock (_sync)
            {
                if (IsConnected) return;

                // 娓呯悊鍙兘宸插け鏁堢殑鏃?COM 寮曠敤锛堝 SAP GUI 閲嶅惎鍚庯級锛屽啀閲嶆柊寤虹珛杩炴帴
                ReleaseComObjects();

                try
                {
                    logger.LogInformation("姝ｅ湪杩炴帴 SAP GUI Scripting Engine...");

                    object sapGuiRot = Marshal.BindToMoniker("SAPGUI");
                    logger.LogInformation("BindToMoniker SAPGUI 鎴愬姛");

                    object engine = sapGuiRot.GetType().InvokeMember(
                        "GetScriptingEngine",
                        System.Reflection.BindingFlags.InvokeMethod,
                        null,
                        sapGuiRot,
                        null);
                    logger.LogInformation("GetScriptingEngine 鎴愬姛");

                    _app = (GuiApplication)engine;

                    if (_app.Connections.Count == 0)
                        throw new Exception("没有打开的 SAP 连接。");

                    _connection = (GuiConnection)_app.Connections.ElementAt(0);

                    if (_connection.DisabledByServer)
                    {
                        logger.LogWarning("SAP 脚本运行权限未开启。");
                    }

                    _currentSession = (GuiSession)_connection.Sessions.Item(_connection.Sessions.Count - 1);

                    logger.LogInformation("SAP GUI 杩炴帴鎴愬姛");
                }
                catch (Exception ex)
                {
                    ReleaseComObjects();
                    throw new Exception($"杩炴帴 SAP GUI 澶辫触: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 鍒锋柊 Session 寮曠敤锛堝綋 SAP 鍒涘缓鏂?Session 鎴栧垏鎹㈢獥鍙ｆ椂璋冪敤锛夈€?
        /// </summary>
        public void RefreshSession()
        {
            lock (_sync)
            {
                if (_connection == null) return;

                try
                {
                    _currentSession = (GuiSession)_connection.Sessions.Item(_connection.Sessions.Count - 1);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "鍒锋柊 SAP Session 澶辫触");
                }
            }
        }

        /// <summary>
        /// 閫氳繃 SAP ID 鏌ユ壘鎺т欢銆?
        /// </summary>
        public GuiComponent FindById(string id)
        {
            lock (_sync)
            {
                if (_currentSession == null)
                    throw new InvalidOperationException("SAP 未连接，请先调用 Connect()。");
            }

            return _currentSession.FindById(id);
        }

        /// <summary>
        /// 閫氳繃灞忓箷鍧愭爣鍛戒腑 SAP 鎺т欢锛岃繑鍥?ID 闆嗗悎銆?
        /// </summary>
        public GuiCollection FindByPosition(int x, int y)
        {
            var session = CurrentSession;
            if (session == null)
                throw new InvalidOperationException("SAP 未连接。");

            return session.FindByPosition(x, y, false);
        }

        /// <summary>
        /// 閲婃斁鎵€鏈?COM 璧勬簮銆?
        /// </summary>
        private void ReleaseComObjects()
        {
            // 鑷簳鍚戜笂閲婃斁锛歋ession 鈫?Connection 鈫?App
            SapComReleaser.Release(ref _currentSession);
            SapComReleaser.Release(ref _connection);
            SapComReleaser.Release(ref _app);
        }

        public void Dispose()
        {
            if (_disposed) return;

            lock (_sync)
                ReleaseComObjects();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

