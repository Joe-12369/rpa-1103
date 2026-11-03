using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading;
using WindowsAccessBridgeInterop;

namespace II.RPA.Plugin.Java.Execute
{
    internal interface IAccessBridgeOperations
    {
        AccessBridge Create();
        void Initialize(AccessBridge accessBridge);
        List<AccessibleJvm> EnumJvms(AccessBridge accessBridge);
        bool IsJavaWindow(AccessBridge accessBridge, IntPtr windowHandle);
        void Dispose(AccessBridge accessBridge);
    }

    internal sealed class AccessBridgeOperations : IAccessBridgeOperations
    {
        public AccessBridge Create() => new AccessBridge();

        public void Initialize(AccessBridge accessBridge) => accessBridge.Initialize();

        public List<AccessibleJvm> EnumJvms(AccessBridge accessBridge)
        {
            return accessBridge.EnumJvms(
                hwnd => accessBridge.CreateAccessibleWindow(hwnd));
        }

        public bool IsJavaWindow(AccessBridge accessBridge, IntPtr windowHandle)
        {
            return accessBridge.Functions.IsJavaWindow(windowHandle);
        }

        public void Dispose(AccessBridge accessBridge) => accessBridge.Dispose();
    }

    public class AccessBridgeProvider : IDisposable
    {
        private static readonly ILogger logger = Logger.OfContext();
        private readonly object runtimeLock = new object();
        private readonly IAccessBridgeOperations operations;
        private List<AccessibleJvm> cachedJvms;
        private bool isBridgeInitlized;
        private IJavaUiThread javaUiThread;
        private AccessBridge accessBridgeInstance;

        public static readonly AccessBridgeProvider Singleton =
            new AccessBridgeProvider(new AccessBridgeOperations());

        internal AccessBridgeProvider(IAccessBridgeOperations operations)
        {
            this.operations = operations
                ?? throw new ArgumentNullException(nameof(operations));
        }

        public AccessBridge AccessBridgeInstance =>
            GetJavaUiThread().Invoke(GetOrCreateAccessBridge);

        public bool IsHasJavaEnv { get; set; } = true;

        public List<AccessibleJvm> CachedJvms => cachedJvms;

        public bool IsInitilized => isBridgeInitlized;

        internal void AttachUiThread(IJavaUiThread uiThread)
        {
            if (uiThread == null) throw new ArgumentNullException(nameof(uiThread));

            lock (runtimeLock)
            {
                if (javaUiThread == null)
                {
                    javaUiThread = uiThread;
                    return;
                }

                if (!ReferenceEquals(javaUiThread, uiThread))
                {
                    throw new InvalidOperationException(
                        "AccessBridgeProvider is already attached to another Java UI thread.");
                }
            }
        }

        internal bool CheckAccess()
        {
            return GetJavaUiThread().CheckAccess();
        }

        internal void Invoke(Action operation)
        {
            GetJavaUiThread().Invoke(operation);
        }

        internal TResult Invoke<TResult>(Func<TResult> operation)
        {
            return GetJavaUiThread().Invoke(operation);
        }

        public void InitBridge()
        {
            GetJavaUiThread().Invoke(InitBridgeCore);
        }

        public List<AccessibleJvm> EnumJvms(bool bRefresh = false)
        {
            return GetJavaUiThread().Invoke(() => EnumJvmsCore(bRefresh));
        }

        public void RefreshJvms()
        {
            GetJavaUiThread().Invoke(() =>
            {
                InitBridgeCore();
                cachedJvms?.Clear();
                cachedJvms = EnumJvmsCore(true);
            });
        }

        public bool IsJavaWnd(IntPtr ptr)
        {
            return GetJavaUiThread().Invoke(() => IsJavaWndCore(ptr));
        }

        public void IsExecution()
        {
            RegistryKey keyNode = Registry.CurrentUser;
            var nodes = keyNode.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Accessibility");

            foreach (var item in nodes.GetValueNames())
            {
                if (nodes.GetValue(item).Equals("oracle_javaaccessbridge"))
                {
                    keyNode.Close();
                    return;
                }
            }
            throw new JavaException(eErrorMessageType.Error_JavaExtensionUnInstalled);
        }

        public void Dispose()
        {
            GetJavaUiThread().Invoke(() =>
            {
                try
                {
                    if (isBridgeInitlized && accessBridgeInstance != null)
                    {
                        operations.Dispose(accessBridgeInstance);
                        logger.LogInformation("AccessBridge.Dispose");
                    }
                }
                catch (Exception e)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(
                        logger, e, "AccessBridge Initialize Error");
                }
                isBridgeInitlized = false;
            });
        }

        private IJavaUiThread GetJavaUiThread()
        {
            lock (runtimeLock)
            {
                return javaUiThread ?? throw new InvalidOperationException(
                    "AccessBridgeProvider must be attached to the Java UI thread before use.");
            }
        }

        private AccessBridge GetOrCreateAccessBridge()
        {
            if (accessBridgeInstance == null)
            {
                accessBridgeInstance = operations.Create();
            }
            return accessBridgeInstance;
        }

        private void InitBridgeCore()
        {
            logger.LogInformation("JavaInitBridge");
            try
            {
                if (!isBridgeInitlized)
                {
                    var accessBridge = GetOrCreateAccessBridge();
                    accessBridge.Initilized -= AccessBridge_Initilized;
                    accessBridge.Initilized += AccessBridge_Initilized;
                    operations.Initialize(accessBridge);
                    Thread.Sleep(50);
                }
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(
                    logger, e, "Java Exception: AccessBridge Initialize Error");
            }
        }

        private List<AccessibleJvm> EnumJvmsCore(bool refresh)
        {
            if (refresh)
            {
                cachedJvms = null;
            }
            if (cachedJvms == null || cachedJvms.Count == 0)
            {
                cachedJvms = operations.EnumJvms(GetOrCreateAccessBridge());
            }
            return cachedJvms;
        }

        private bool IsJavaWndCore(IntPtr ptr)
        {
            try
            {
                return operations.IsJavaWindow(GetOrCreateAccessBridge(), ptr);
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(
                    logger, e, "判断是否为Java窗口失败");
                IsHasJavaEnv = false;
                return false;
            }
        }

        private void AccessBridge_Initilized(object sender, EventArgs e)
        {
            isBridgeInitlized = true;
            logger.LogInformation("AccessBridge_Initilized");
        }
    }
}
