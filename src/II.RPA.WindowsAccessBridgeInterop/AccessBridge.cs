// Copyright 2015 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CoreConfigurations = II.RPA.Plugin.Core.GlobleConfigurations;
using WindowsAccessBridgeInterop.Win32;

namespace WindowsAccessBridgeInterop
{
    /// <summary>
    /// Class used to dynamically load and access the Java Access Bridge DLL.
    /// </summary>
    public class AccessBridge : IDisposable
    {
        private AccessBridgeLibrary _library;
        private AccessBridgeFunctions _functions;
        private AccessBridgeEvents _events;
        private bool _disposed;
        private static ILogger logger = Logger.OfContext();

        public AccessBridge()
        {
            CollectionSizeLimit = 1000;
            TextLineCountLimit = 200;
            TextLineLengthLimit = 200;
            TextBufferLengthLimit = 1024;
        }

        public AccessBridgeFunctions Functions
        {
            get
            {
                ThrowIfDisposed();
                Initialize();
                return _functions;
            }
        }

        public AccessBridgeEvents Events
        {
            get
            {
                ThrowIfDisposed();
                Initialize();
                return _events;
            }
        }

        public bool IsLoaded
        {
            get
            {
                return _library != null;
            }
        }

        public FileVersionInfo LibraryVersion
        {
            get
            {
                ThrowIfDisposed();
                Initialize();
                return _library.Version;
            }
        }

        public bool IsLegacy
        {
            get
            {
                ThrowIfDisposed();
                Initialize();
                return _library.IsLegacy;
            }
        }

        public int CollectionSizeLimit { get; set; }
        public int TextLineCountLimit { get; set; }
        public int TextLineLengthLimit { get; set; }
        public int TextBufferLengthLimit { get; set; }

        public event EventHandler Initilized;
        public event EventHandler Disposed;

        public void Initialize()
        {
            ThrowIfDisposed();
            if (_library != null)
                return;
            var library = LoadLibrary();
            if (library.IsLegacy)   //if the system is old system
            {
                var libraryFunctions = LoadEntryPointsLegacy(library);
                var functions = new AccessBridgeNativeFunctionsLegacy(libraryFunctions);
                var events = new AccessBridgeNativeEventsLegacy(libraryFunctions);
                // Everything is initialized correctly, save to member variables.
                _library = library;
                _functions = functions;
                _events = events;
            }
            else
            {
                var libraryFunctions = LoadEntryPoints(library);
                var functions = new AccessBridgeNativeFunctions(libraryFunctions);
                var events = new AccessBridgeNativeEvents(libraryFunctions);
                // Everything is initialized correctly, save to member variables.
                _library = library;
                _functions = functions;
                _events = events;
            }
            _functions.Windows_run();
            OnInitilized();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_events != null)
            {
                _events.Dispose();
            }

            if (_library != null)
            {
                _library.Dispose();
                _functions = null;
                _library = null;
            }

            _disposed = true;
            OnDisposed();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("Access Bridge library has been disposed");
        }

        public List<AccessibleJvm> EnumJvms()
        {
            return EnumJvms(hwnd =>
            {
                return CreateAccessibleWindow(hwnd);
            });
        }

        public List<AccessibleJvm> EnumJvms(Func<IntPtr, AccessibleWindow> windowFunc)
        {
            if (_library == null)
                return new List<AccessibleJvm>();

            try
            {
                var windows = new List<AccessibleWindow>();
                var success = NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    //NLogger.Default.Debug("Child" + hWnd.ToInt32());
                    var window = windowFunc(hWnd);
                    if (window != null)
                    {
                        // NLogger.Default.Debug("Match Child" + hWnd.ToInt32());
                        windows.Add(window);
                    }
                    NativeMethods.EnumChildWindows(hWnd, (hchildWnd, lchildParam) =>
                    {
                        //https://stackoverflow.com/questions/7014190/why-is-enumchildwindows-skipping-children
                        Thread.Sleep(0);
                        //NLogger.Default.Debug("hchildWnd" + hchildWnd.ToInt32());
                        var windowsTemp = windowFunc(hchildWnd);
                        if (windowsTemp != null)
                        {
                            //NLogger.Default.Debug("Match SubChild" + hchildWnd.ToInt32());
                            windows.Add(windowsTemp);
                        }
                        return true;
                    }, IntPtr.Zero);

                    return true;
                }, IntPtr.Zero);

                if (!success)
                {
                    var hr = Marshal.GetHRForLastWin32Error();
                    Marshal.ThrowExceptionForHR(hr);
                }

                // Group windows by JVM id
                return windows.GroupBy(x => x.JvmId).Select(g =>
                {
                    var result = new AccessibleJvm(this, g.Key);
                    result.Windows.AddRange(g.OrderBy(x => x.GetDisplaySortString()));
                    return result;
                }).OrderBy(x => x.JvmId).ToList();
            }
            catch (Exception e)
            {
                throw new Exception("Error detecting running applications", e);
            }
        }

        public AccessibleWindow CreateAccessibleWindow(IntPtr hwnd)
        {
            if (!Functions.IsJavaWindow(hwnd))
            {
                return null;
            }

            int vmId;
            JavaObjectHandle ac;
            if (!Functions.GetAccessibleContextFromHWND(hwnd, out vmId, out ac))
                return null;

            return new AccessibleWindow(this, hwnd, ac);
        }

        public class AccessBridgeLibrary : UnmanagedLibrary
        {
            public AccessBridgeLibrary(string fileName) : base(fileName)
            {
            }

            public bool IsLegacy { get; set; }
        }

        /// <summary>
        /// Loads the Java Access Bridge using an absolute path when the runtime
        /// package contains the native bridge DLL.  The old implementation only
        /// passed a bare file name to LoadLibrary, which relies on the process
        /// current directory/PATH and fails for installed RuntimeHost layouts
        /// where the bridge is under resources\support\JavaPlugin.
        /// </summary>
        private static AccessBridgeLibrary LoadNativeLibrary(string fileName)
        {
            var candidates = new List<string>();
            AddNativeLibraryCandidate(candidates, fileName);

            foreach (string supportDirectory in GetSupportDirectories())
            {
                string javaPluginDirectory = System.IO.Path.Combine(
                    supportDirectory,
                    "JavaPlugin");
                AddNativeLibraryCandidate(
                    candidates,
                    System.IO.Path.Combine(javaPluginDirectory, fileName));

                // 2.0.2 is the bundled Java Access Bridge package used by the
                // current runtime.  Keep this explicit candidate ahead of the
                // versioned fallback directories below.
                AddNativeLibraryCandidate(
                    candidates,
                    System.IO.Path.Combine(javaPluginDirectory, "2.0.2", fileName));

                AddNativeLibraryCandidate(
                    candidates,
                    System.IO.Path.Combine(
                        javaPluginDirectory,
                        "WindowsAccessBridge",
                        fileName));

                string versionDirectory = System.IO.Path.Combine(
                    javaPluginDirectory,
                    "WindowsAccessBridge");
                if (Directory.Exists(versionDirectory))
                {
                    try
                    {
                        foreach (string directory in Directory.GetDirectories(
                            versionDirectory,
                            "*",
                            SearchOption.TopDirectoryOnly)
                            .OrderByDescending(
                                System.IO.Path.GetFileName,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            AddNativeLibraryCandidate(
                                candidates,
                                System.IO.Path.Combine(directory, fileName));
                        }
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(
                            logger,
                            ex,
                            $"扫描 Java Access Bridge 目录失败:{versionDirectory}");
                    }
                }
            }

            Exception lastException = null;
            foreach (string candidate in candidates)
            {
                try
                {
                    var library = new AccessBridgeLibrary(candidate);
                    logger.LogDebug($"Load {fileName}: {candidate}");
                    return library;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            if (lastException != null)
            {
                throw lastException;
            }

            // This should only be reached when no candidate could be constructed;
            // retain the original exception shape for callers and diagnostics.
            throw new Exception($"Error loading library \"{fileName}\"");
        }

        private static void AddNativeLibraryCandidate(
            ICollection<string> candidates,
            string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            if (System.IO.Path.IsPathRooted(candidate))
            {
                candidate = System.IO.Path.GetFullPath(candidate);
                if (!File.Exists(candidate))
                {
                    return;
                }
            }

            if (!candidates.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Add(candidate);
            }
        }

        private static IEnumerable<string> GetSupportDirectories()
        {
            var directories = new List<string>();
            AddSupportDirectory(directories, CoreConfigurations.SupportDirectory);

            // JavaBridgeHost and other tools can use this library without
            // constructing PluginLoader first, so derive the installed support
            // directory from the assembly location as a fallback.
            string assemblyLocation = typeof(AccessBridge).Assembly.Location;
            if (!string.IsNullOrWhiteSpace(assemblyLocation))
            {
                DirectoryInfo current = new DirectoryInfo(
                    System.IO.Path.GetDirectoryName(assemblyLocation));
                while (current != null)
                {
                    if (string.Equals(
                        current.Name,
                        "rpa-engine",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        AddSupportDirectory(
                            directories,
                            System.IO.Path.Combine(
                                current.Parent?.FullName ?? current.FullName,
                                "support"));
                        break;
                    }

                    current = current.Parent;
                }
            }

            return directories;
        }

        private static void AddSupportDirectory(
            ICollection<string> directories,
            string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) ||
                !Directory.Exists(directory))
            {
                return;
            }

            string fullPath = System.IO.Path.GetFullPath(directory);
            if (!directories.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
            {
                directories.Add(fullPath);
            }
        }

        private static AccessBridgeLibrary LoadLibrary()
        {
            try
            {
                AccessBridgeLibrary library = null;
                if (IntPtr.Size == 4)
                {
                    try
                    {
                        library = LoadNativeLibrary("WindowsAccessBridge-32.dll");
                        logger.LogDebug("Load WindowsAccessBridge-32.dll");
                    }
                    catch (Exception e)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, e, "Load WindowsAccessBridge-32.dll error");
                        try
                        {
                            library = LoadNativeLibrary("WindowsAccessBridge.dll");
                            library.IsLegacy = true;
                            logger.LogDebug("Load WindowsAccessBridge.dll");
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "WindowsAccessBridge error ");
                            // Ignore, we'll trow the initial exception
                            library = null;
                        }
                        if (library == null)
                        {
                            throw;
                        }
                    }
                }
                else if (IntPtr.Size == 8)
                {
                    try
                    {
                        library = LoadNativeLibrary("WindowsAccessBridge-64.dll");
                        logger.LogTrace("Load WindowsAccessBridge-64.dll");
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "WindowsAccessBridge-64 error");
                        library = null;

                        if (library == null)
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unknown platform.");
                }

                logger.LogDebug("loading successful");
                return library;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "loading failed");
                var sb = new StringBuilder();
                sb.AppendLine("Error loading the Java Access Bridge DLL.");
                sb.AppendLine("This usually happens if the Java Access Bridge is not installed.");
                if (IntPtr.Size == 8)
                {
                    sb.Append("Please make sure to install the 64-bit version of the " +
                      "Java SE Runtime Environment version 7 or later. ");
                    sb.AppendFormat("Alternatively, try running the 32-bit version of {0} " +
                      "if a 32-bit version of the JRE is installed.", Assembly.GetEntryAssembly().GetName().Name);
                }
                else
                {
                    sb.Append(
                      "Please make sure to install the 32-bit version of the " +
                      "Java SE Runtime Environment version 7 or later.");
                }
                throw new Exception(sb.ToString(), e);
            }
        }

        private static AccessBridgeEntryPoints LoadEntryPoints(UnmanagedLibrary library)
        {
            var functions = new AccessBridgeEntryPoints();
            LoadEntryPointsImpl(library, functions);
            return functions;
        }

        private static AccessBridgeEntryPointsLegacy LoadEntryPointsLegacy(UnmanagedLibrary library)
        {
            var functions = new AccessBridgeEntryPointsLegacy();
            LoadEntryPointsImpl(library, functions);
            return functions;
        }

        private static void LoadEntryPointsImpl(UnmanagedLibrary library, object functions)
        {
            var publicMembers = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance;
            foreach (var property in functions.GetType().GetProperties(publicMembers))
            {
                var name = property.Name;

                // All entry point names have lower case, except for "Windows_run"
                switch (name)
                {
                    case "Windows_run":
                        break;

                    default:
                        name = char.ToLower(name[0], CultureInfo.InvariantCulture) + name.Substring(1);
                        break;
                }

                // Event setters have a "FP" suffix
                if (name.StartsWith("set") && name != "setTextContents")
                {
                    name += "FP";
                }

                try
                {
                    var function = library.GetUnmanagedFunction(name, property.PropertyType);
                    if (function == null)
                    {
                        throw new Exception(string.Format("Function {0} not found in AccessBridge", name));
                    }
                    property.SetValue(functions, function, null);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(string.Format("Error loading function {0} from access bridge library", name), e);
                }
            }
        }

        protected virtual void OnInitilized()
        {
            var handler = Initilized;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnDisposed()
        {
            var handler = Disposed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }

}

