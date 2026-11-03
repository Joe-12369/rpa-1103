using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    internal sealed class PowerBuilderResidentLifecycleDecision
    {
        internal bool LoadLibrary { get; set; }
        internal bool CallStartWork { get; set; }
        internal bool CallEndWork { get; set; }
        internal bool CallFreeLibrary { get; set; }
        internal bool RestartRequired { get; set; }
        internal int ControllerProcessId { get; set; }
        internal string Status { get; set; }
    }

    /// <summary>
    /// Same-bitness LoadLibrary/remote-export implementation adapted from the
    /// production NativePicking branch. It reads exports from the image loaded in
    /// the target, so an older resident bridge is detected by ABI instead of calling
    /// offsets from a different local DLL build.
    /// </summary>
    internal sealed class NativeOwnerDrawBridgeActivator : IOwnerDrawBridgeActivator
    {
        // This value is also exported by MfcGdiAccBridge.cpp. Increment both
        // constants whenever the PB Bridge binary, hooks, protocol behaviour, or
        // lifecycle semantics change. ABI alone is intentionally insufficient:
        // an installer can replace the on-disk DLL while an older image remains
        // mapped in a running PB process.
        internal const uint RequiredPowerBuilderBridgeBuildId = 0x50420005u;

        internal static PowerBuilderResidentLifecycleDecision
            DecidePowerBuilderResidentLifecycle(bool resident,
                bool abiAvailable, uint residentAbi, bool buildIdAvailable,
                uint residentBuildId, bool start, int controllerProcessId)
        {
            var decision = new PowerBuilderResidentLifecycleDecision
            {
                ControllerProcessId = controllerProcessId,
                // A remote PB Bridge is resident-only by design in every branch.
                CallFreeLibrary = false,
            };
            if (!resident)
            {
                decision.LoadLibrary = start;
                decision.CallStartWork = start;
                decision.Status = start
                    ? "No resident PB Bridge; load the verified packaged image then "
                        + "call StartWork with controller pid=" + controllerProcessId + "."
                    : "No resident PB Bridge; no lifecycle call is required.";
                return decision;
            }
            if (!abiAvailable || residentAbi != 6 || !buildIdAvailable
                || residentBuildId != RequiredPowerBuilderBridgeBuildId)
            {
                decision.RestartRequired = true;
                decision.Status = "Resident PB Bridge identity mismatch/missing: abi="
                    + (abiAvailable ? residentAbi.ToString() : "missing")
                    + " buildId=" + (buildIdAvailable
                        ? "0x" + residentBuildId.ToString("X8") : "missing")
                    + ". Restart the target; StartWork, EndWork, and remote "
                    + "FreeLibrary are all forbidden.";
                return decision;
            }
            decision.CallStartWork = start;
            decision.CallEndWork = !start;
            decision.Status = start
                ? "Resident PB Bridge identity matches; reuse it and transfer "
                    + "StartWork to controller pid=" + controllerProcessId + "."
                : "Resident PB Bridge identity matches; EndWork may stop it while "
                    + "the DLL remains resident.";
            return decision;
        }

        private const uint ProcessCreateThread = 0x0002;
        private const uint ProcessQueryInformation = 0x0400;
        private const uint ProcessVmOperation = 0x0008;
        private const uint ProcessVmWrite = 0x0020;
        private const uint ProcessVmRead = 0x0010;
        private const uint MemCommit = 0x1000;
        private const uint MemReserve = 0x2000;
        private const uint MemRelease = 0x8000;
        private const uint PageReadWrite = 0x04;
        private const uint WaitObject0 = 0;

        public bool TryCheckLegacyConflict(int processId, string legacyFileName,
            out bool found, out string status)
        {
            found = false;
            if (processId <= 0 || string.IsNullOrWhiteSpace(legacyFileName)
                || !string.Equals(Path.GetFileName(legacyFileName), legacyFileName,
                    StringComparison.Ordinal))
            {
                status = "Invalid legacy Bridge conflict check pid=" + processId
                    + " module=\"" + legacyFileName + "\".";
                return false;
            }

            try
            {
                RemoteModule legacy = FindRemoteModule(processId, legacyFileName);
                if (legacy == null)
                {
                    status = "No legacy Bridge module named \"" + legacyFileName
                        + "\" is resident in pid=" + processId + ".";
                    return true;
                }

                found = true;
                status = "LegacyConflict: legacy Bridge module \""
                    + legacyFileName + "\" is resident at \"" + legacy.FileName
                    + "\" in pid=" + processId + ". No Bridge export was called; "
                    + "restart the target application before using the product Bridge.";
                return true;
            }
            catch (Exception ex)
            {
                status = "Cannot inspect pid=" + processId
                    + " for legacy Bridge module \"" + legacyFileName + "\": "
                    + ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        public bool TryStart(int processId, string bridgePath,
            string legacyFileName, int requiredAbi, int controllerProcessId,
            out string status)
        {
            var log = new StringBuilder();
            try
            {
                if (processId <= 0 || controllerProcessId <= 0
                    || string.IsNullOrWhiteSpace(bridgePath)
                    || !File.Exists(bridgePath))
                {
                    status = "Invalid bridge start request pid=" + processId
                        + " controllerPid=" + controllerProcessId
                        + " path=\"" + bridgePath + "\".";
                    return false;
                }

                if (!IsProcessAlive(controllerProcessId))
                {
                    status = "Bridge controller process is not running: pid="
                        + controllerProcessId + ".";
                    return false;
                }

                bridgePath = Path.GetFullPath(bridgePath);
                string moduleName = Path.GetFileName(bridgePath);
                if (!TryValidatePowerBuilderRequestIdentity(moduleName,
                    requiredAbi, out string identityStatus))
                {
                    status = identityStatus;
                    return false;
                }
                if (IsPowerBuilderBridge(moduleName, requiredAbi)
                    && !TryValidatePackagedPowerBuilderBridge(bridgePath,
                        out string packagedIdentityStatus))
                {
                    status = packagedIdentityStatus;
                    return false;
                }
                string packagedHash = ComputeSha256(bridgePath);
                log.AppendLine("pid=" + processId + " dll=\"" + bridgePath
                    + "\" sha256=" + packagedHash + " controllerPid="
                    + controllerProcessId);

                if (!TryCheckLegacyConflict(processId, legacyFileName,
                    out bool legacyFound, out string legacyStatus))
                {
                    status = log.AppendLine("Legacy Bridge inspection failed; "
                        + "refusing LoadLibraryW and StartWork. " + legacyStatus)
                        .ToString();
                    return false;
                }
                if (legacyFound)
                {
                    status = log.AppendLine(legacyStatus).ToString();
                    return false;
                }

                IntPtr process = OpenProcess(ProcessCreateThread | ProcessQueryInformation
                    | ProcessVmOperation | ProcessVmWrite | ProcessVmRead,
                    false, unchecked((uint)processId));
                if (process == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "OpenProcess(" + processId + ") failed");
                try
                {
                    RemoteModule module = FindRemoteModule(processId, moduleName);
                    if (module == null)
                    {
                        LoadLibraryRemote(process, processId, bridgePath, log);
                        module = WaitForRemoteModule(processId, moduleName, 3000);
                        if (module == null)
                            throw new InvalidOperationException(
                                "Bridge module did not appear after LoadLibraryW.");
                    }
                    else
                    {
                        log.AppendLine("module already loaded base=0x"
                            + module.BaseAddress.ToInt64().ToString("X") + " path=\""
                            + module.FileName + "\"");
                    }

                    string residentHash = ComputeSha256(module.FileName);
                    log.AppendLine("resident bridge sha256=" + residentHash);
                    if (!string.Equals(residentHash, packagedHash,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        status = log.AppendLine("Resident bridge image does not match "
                            + "the packaged DLL; target restart is required before use.")
                            .ToString();
                        return false;
                    }

                    if (!TryCallRemoteExport(process, module.BaseAddress,
                        "BridgeAbiVersion", 5000, log, out uint abi))
                    {
                        status = log.AppendLine("Bridge ABI query failed; target restart may be required.")
                            .ToString();
                        return false;
                    }
                    if (abi != requiredAbi)
                    {
                        status = log.AppendLine("Bridge ABI=" + abi + " required="
                            + requiredAbi + "; target restart with the packaged bridge is required.")
                            .ToString();
                        return false;
                    }

                    if (IsPowerBuilderBridge(moduleName, requiredAbi)
                        && !TryValidateRemotePowerBuilderBuildId(process,
                            module.BaseAddress, log, true, controllerProcessId,
                            out string buildStatus))
                    {
                        status = log.AppendLine(buildStatus).ToString();
                        return false;
                    }

                    if (!TryCallRemoteExport(process, module.BaseAddress, "StartWork",
                        new IntPtr(controllerProcessId), 8000, log,
                        out uint startCode))
                    {
                        status = log.ToString();
                        return false;
                    }
                    bool success = startCode == 0 || startCode == 4;
                    log.AppendLine(success ? "StartWork accepted exit=" + startCode
                        : "StartWork rejected exit=" + startCode);
                    status = log.ToString();
                    return success;
                }
                finally
                {
                    CloseHandle(process);
                }
            }
            catch (Exception ex)
            {
                log.AppendLine("Bridge start failed: " + ex.GetType().Name + ": "
                    + ex.Message);
                status = log.ToString();
                return false;
            }
        }

        public bool TryStopResident(int processId, string bridgePath,
            string legacyFileName, int requiredAbi,
            out OwnerDrawBridgeStopState stopState,
            out string status)
        {
            stopState = OwnerDrawBridgeStopState.NotLoaded;
            var log = new StringBuilder();
            try
            {
                if (processId <= 0 || string.IsNullOrWhiteSpace(bridgePath)
                    || requiredAbi <= 0)
                {
                    status = "Invalid resident Bridge stop request pid=" + processId
                        + " path=\"" + bridgePath + "\" abi=" + requiredAbi + ".";
                    return false;
                }
                bridgePath = Path.GetFullPath(bridgePath);
                string moduleName = Path.GetFileName(bridgePath);
                if (!TryValidatePowerBuilderRequestIdentity(moduleName,
                    requiredAbi, out string identityStatus))
                {
                    status = identityStatus;
                    return false;
                }
                if (IsPowerBuilderBridge(moduleName, requiredAbi)
                    && !TryValidatePackagedPowerBuilderBridge(bridgePath,
                        out string packagedIdentityStatus))
                {
                    status = packagedIdentityStatus;
                    return false;
                }
                if (!TryCheckLegacyConflict(processId, legacyFileName,
                    out bool legacyFound, out string legacyStatus))
                {
                    status = "Legacy Bridge inspection failed; refusing EndWork. "
                        + legacyStatus;
                    return false;
                }
                if (legacyFound)
                {
                    stopState = OwnerDrawBridgeStopState.LegacyConflict;
                    status = legacyStatus;
                    return false;
                }
                RemoteModule module = FindRemoteModule(processId, moduleName);
                if (module == null)
                {
                    status = "Bridge module is not loaded; no action was required.";
                    return true;
                }
                if (!File.Exists(bridgePath))
                {
                    status = "Packaged Bridge is missing; refusing to call an "
                        + "unverified resident module named \"" + moduleName + "\".";
                    return false;
                }
                string packagedHash = ComputeSha256(bridgePath);
                string residentHash = ComputeSha256(module.FileName);
                log.AppendLine("stop identity packaged=\"" + bridgePath
                    + "\" sha256=" + packagedHash + " resident=\""
                    + module.FileName + "\" sha256=" + residentHash);
                if (!string.Equals(packagedHash, residentHash,
                    StringComparison.OrdinalIgnoreCase))
                {
                    status = log.AppendLine("Resident Bridge image does not match "
                        + "the packaged DLL; refusing EndWork.").ToString();
                    return false;
                }

                IntPtr process = OpenProcess(ProcessCreateThread | ProcessQueryInformation
                    | ProcessVmOperation | ProcessVmWrite | ProcessVmRead, false,
                    unchecked((uint)processId));
                if (process == IntPtr.Zero)
                {
                    status = "OpenProcess(" + processId + ") failed: "
                        + Marshal.GetLastWin32Error() + ".";
                    return false;
                }
                try
                {
                    if (!TryCallRemoteExport(process, module.BaseAddress,
                        "BridgeAbiVersion", 5000, log, out uint abi)
                        || abi != requiredAbi)
                    {
                        status = log.AppendLine("Resident Bridge ABI=" + abi
                            + " required=" + requiredAbi
                            + "; refusing EndWork.").ToString();
                        return false;
                    }
                    if (IsPowerBuilderBridge(moduleName, requiredAbi)
                        && !TryValidateRemotePowerBuilderBuildId(process,
                            module.BaseAddress, log, false, 0,
                            out string buildStatus))
                    {
                        status = log.AppendLine(buildStatus).ToString();
                        return false;
                    }
                    if (!TryCallRemoteExport(process, module.BaseAddress, "EndWork",
                        8000, log, out uint endCode))
                    {
                        status = log.ToString();
                        return false;
                    }
                    // VCL returns 4 when EndWork is called after it is already
                    // stopped. Treat that as an idempotent resident stop.
                    bool stopped = endCode == 0 || endCode == 4;
                    log.AppendLine(stopped
                        ? "EndWork completed; module intentionally remains resident."
                        : "EndWork returned " + endCode
                            + "; stop was not confirmed and module remains resident.");
                    status = log.ToString();
                    if (stopped)
                        stopState = OwnerDrawBridgeStopState.StoppedResident;
                    return stopped;
                }
                finally
                {
                    CloseHandle(process);
                }
            }
            catch (Exception ex)
            {
                log.AppendLine("Bridge stop failed: " + ex.GetType().Name + ": "
                    + ex.Message);
                status = log.ToString();
                return false;
            }
        }

        private sealed class RemoteModule
        {
            internal IntPtr BaseAddress { get; set; }
            internal string FileName { get; set; }
        }

        internal static bool IsExpectedPowerBuilderBuildId(uint residentBuildId,
            out string status)
        {
            bool matches = residentBuildId == RequiredPowerBuilderBridgeBuildId;
            status = "PowerBuilder Bridge BuildId resident=0x"
                + residentBuildId.ToString("X8") + " required=0x"
                + RequiredPowerBuilderBridgeBuildId.ToString("X8")
                + (matches
                    ? "."
                    : "; the resident image is not this packaged build. Restart "
                        + "the target application before retrying; the resident "
                        + "Bridge was not called or unloaded.");
            return matches;
        }

        internal static bool TryValidatePackagedPowerBuilderBridge(string bridgePath,
            out string status)
        {
            const ushort ImageFileMachineI386 = 0x014c;
            const ushort ImageFileMachineAmd64 = 0x8664;
            IntPtr localModule = IntPtr.Zero;
            try
            {
                bridgePath = Path.GetFullPath(bridgePath);
                if (!File.Exists(bridgePath))
                {
                    status = "Packaged PowerBuilder Bridge is missing: \""
                        + bridgePath + "\".";
                    return false;
                }
                ushort expectedMachine = Environment.Is64BitProcess
                    ? ImageFileMachineAmd64 : ImageFileMachineI386;
                if (!TryReadPeMachine(bridgePath, out ushort actualMachine,
                    out string peStatus) || actualMachine != expectedMachine)
                {
                    status = peStatus + " Packaged PowerBuilder Bridge machine=0x"
                        + actualMachine.ToString("X4") + " required=0x"
                        + expectedMachine.ToString("X4")
                        + "; LoadLibrary was not attempted.";
                    return false;
                }

                // This is a local, short-lived validation mapping only. DllMain has
                // no hook side effects; StartWork is never called in this process.
                // FreeLibrary below unloads only this local validation mapping and
                // is unrelated to the resident-only remote PB Bridge policy.
                const uint LoadLibrarySearchDllLoadDir = 0x00000100;
                const uint LoadLibrarySearchSystem32 = 0x00000800;
                localModule = LoadLibraryExW(bridgePath, IntPtr.Zero,
                    LoadLibrarySearchDllLoadDir | LoadLibrarySearchSystem32);
                if (localModule == IntPtr.Zero)
                {
                    status = "Local packaged PowerBuilder Bridge validation load "
                        + "failed win32=" + Marshal.GetLastWin32Error()
                        + "; no remote LoadLibrary was attempted.";
                    return false;
                }
                IntPtr abiAddress = GetProcAddress(localModule, "BridgeAbiVersion");
                IntPtr buildAddress = GetProcAddress(localModule, "BridgeBuildId");
                if (abiAddress == IntPtr.Zero || buildAddress == IntPtr.Zero)
                {
                    status = "Packaged PowerBuilder Bridge is missing required ABI "
                        + "or BuildId exports; no remote LoadLibrary was attempted.";
                    return false;
                }
                var abi = (BridgeIdentityExport)Marshal.GetDelegateForFunctionPointer(
                    abiAddress, typeof(BridgeIdentityExport));
                var build = (BridgeIdentityExport)Marshal.GetDelegateForFunctionPointer(
                    buildAddress, typeof(BridgeIdentityExport));
                uint abiValue = abi(IntPtr.Zero);
                uint buildValue = build(IntPtr.Zero);
                string buildStatus = "BuildId was not evaluated because ABI mismatched.";
                bool buildMatches = abiValue == 6
                    && IsExpectedPowerBuilderBuildId(buildValue, out buildStatus);
                if (abiValue != 6 || !buildMatches)
                {
                    status = "Packaged PowerBuilder Bridge identity rejected: ABI="
                        + abiValue + " required=6; " + buildStatus
                        + " No remote LoadLibrary was attempted.";
                    return false;
                }
                status = "Packaged PowerBuilder Bridge validated locally: machine=0x"
                    + actualMachine.ToString("X4") + " ABI=6 BuildId=0x"
                    + buildValue.ToString("X8") + ".";
                return true;
            }
            catch (Exception ex)
            {
                status = "Packaged PowerBuilder Bridge validation failed: "
                    + ex.GetType().Name + ": " + ex.Message
                    + "; no remote LoadLibrary was attempted.";
                return false;
            }
            finally
            {
                if (localModule != IntPtr.Zero)
                    FreeLibrary(localModule);
            }
        }

        private static bool TryReadPeMachine(string path, out ushort machine,
            out string status)
        {
            machine = 0;
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open,
                    FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    if (stream.Length < 64 || reader.ReadUInt16() != 0x5A4D)
                    {
                        status = "Packaged Bridge is not an MZ image.";
                        return false;
                    }
                    stream.Position = 0x3c;
                    uint peOffset = reader.ReadUInt32();
                    if (peOffset < 64 || peOffset > stream.Length - 6)
                    {
                        status = "Packaged Bridge has an invalid PE offset.";
                        return false;
                    }
                    stream.Position = peOffset;
                    if (reader.ReadUInt32() != 0x00004550)
                    {
                        status = "Packaged Bridge has no PE signature.";
                        return false;
                    }
                    machine = reader.ReadUInt16();
                    status = "Packaged Bridge PE machine read.";
                    return true;
                }
            }
            catch (Exception ex) when (ex is IOException
                || ex is UnauthorizedAccessException
                || ex is EndOfStreamException)
            {
                status = "Cannot read packaged Bridge PE identity: " + ex.Message;
                return false;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint BridgeIdentityExport(IntPtr ignored);

        private static bool TryValidateRemotePowerBuilderBuildId(IntPtr process,
            IntPtr moduleBase, StringBuilder log, bool start,
            int controllerProcessId, out string status)
        {
            log.AppendLine("PowerBuilder Bridge required BuildId=0x"
                + RequiredPowerBuilderBridgeBuildId.ToString("X8") + ".");
            if (!TryCallRemoteExport(process, moduleBase, "BridgeBuildId", 5000,
                log, out uint residentBuildId))
            {
                status = DecidePowerBuilderResidentLifecycle(true, true, 6,
                    false, 0, start, controllerProcessId).Status;
                return false;
            }
            PowerBuilderResidentLifecycleDecision decision
                = DecidePowerBuilderResidentLifecycle(true, true, 6, true,
                    residentBuildId, start, controllerProcessId);
            status = decision.Status;
            return start ? decision.CallStartWork : decision.CallEndWork;
        }

        private static bool TryValidatePowerBuilderRequestIdentity(
            string moduleName, int requiredAbi, out string status)
        {
            bool powerBuilderName = string.Equals(moduleName,
                "MfcGdiAccBridge.x86.dll", StringComparison.Ordinal)
                || string.Equals(moduleName, "MfcGdiAccBridge.x64.dll",
                    StringComparison.Ordinal);
            if ((requiredAbi == 6) != powerBuilderName)
            {
                status = "Bridge identity/ABI pairing is not recognized: module=\""
                    + moduleName + "\" abi=" + requiredAbi + ".";
                return false;
            }
            status = "Bridge identity/ABI pairing accepted.";
            return true;
        }

        private static bool IsPowerBuilderBridge(string moduleName,
            int requiredAbi)
        {
            return requiredAbi == 6
                && (string.Equals(moduleName, "MfcGdiAccBridge.x86.dll",
                        StringComparison.Ordinal)
                    || string.Equals(moduleName, "MfcGdiAccBridge.x64.dll",
                        StringComparison.Ordinal));
        }

        private static bool IsProcessAlive(int processId)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                    return !process.HasExited;
            }
            catch (Exception ex) when (ex is ArgumentException
                || ex is InvalidOperationException || ex is Win32Exception)
            {
                return false;
            }
        }

        private static RemoteModule FindRemoteModule(int processId, string moduleName)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    foreach (ProcessModule module in process.Modules)
                    {
                        if (string.Equals(module.ModuleName, moduleName,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return new RemoteModule
                            {
                                BaseAddress = module.BaseAddress,
                                FileName = module.FileName ?? string.Empty,
                            };
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is Win32Exception
                || ex is InvalidOperationException || ex is ArgumentException)
            {
                throw new InvalidOperationException("Cannot enumerate target modules for pid="
                    + processId + ": " + ex.Message, ex);
            }
            return null;
        }

        private static RemoteModule WaitForRemoteModule(int processId,
            string moduleName, int timeoutMs)
        {
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < timeoutMs)
            {
                RemoteModule module = FindRemoteModule(processId, moduleName);
                if (module != null) return module;
                Thread.Sleep(50);
            }
            return null;
        }

        private static void LoadLibraryRemote(IntPtr process, int processId,
            string bridgePath, StringBuilder log)
        {
            IntPtr loadLibrary = ResolveRemoteProcedureAddress(processId,
                "kernel32.dll", "LoadLibraryW", log);
            if (loadLibrary == IntPtr.Zero)
                throw new InvalidOperationException(
                    "Remote kernel32!LoadLibraryW was not resolved.");

            byte[] path = Encoding.Unicode.GetBytes(bridgePath + "\0");
            IntPtr remote = VirtualAllocEx(process, IntPtr.Zero,
                (IntPtr)path.Length, MemCommit | MemReserve, PageReadWrite);
            if (remote == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "VirtualAllocEx failed");
            bool remoteThreadStarted = false;
            bool remoteThreadCompleted = false;
            try
            {
                if (!WriteProcessMemory(process, remote, path, (IntPtr)path.Length,
                    out IntPtr written) || written.ToInt64() != path.Length)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "WriteProcessMemory failed");
                }
                if (!TryCallRemoteAddress(process, loadLibrary, remote, 10000,
                    log, "LoadLibraryW", out uint exitCode,
                    out remoteThreadStarted, out remoteThreadCompleted)
                    || exitCode == 0)
                {
                    throw new InvalidOperationException(
                        "Remote LoadLibraryW returned 0x" + exitCode.ToString("X8") + ".");
                }
            }
            finally
            {
                // If the remote thread timed out, it may still be reading this
                // buffer. A tiny leak is safer than freeing memory underneath a
                // live LoadLibraryW call in the target process.
                if (!remoteThreadStarted || remoteThreadCompleted)
                    VirtualFreeEx(process, remote, IntPtr.Zero, MemRelease);
                else
                    log.AppendLine("LoadLibraryW did not complete in the bounded wait; "
                        + "the remote path buffer was retained to avoid use-after-free.");
            }
        }

        private static IntPtr ResolveRemoteProcedureAddress(int processId,
            string moduleName, string procedureName, StringBuilder log)
        {
            IntPtr localModule = GetModuleHandleW(moduleName);
            IntPtr localProcedure = GetProcAddress(localModule, procedureName);
            if (localModule == IntPtr.Zero || localProcedure == IntPtr.Zero)
                return IntPtr.Zero;

            string ownerModuleName = null;
            IntPtr ownerBase = IntPtr.Zero;
            int ownerSize = 0;
            using (Process current = Process.GetCurrentProcess())
            {
                foreach (ProcessModule module in current.Modules)
                {
                    long start = module.BaseAddress.ToInt64();
                    long end = start + module.ModuleMemorySize;
                    long address = localProcedure.ToInt64();
                    if (address >= start && address < end)
                    {
                        ownerModuleName = module.ModuleName;
                        ownerBase = module.BaseAddress;
                        ownerSize = module.ModuleMemorySize;
                        break;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(ownerModuleName) || ownerBase == IntPtr.Zero)
                return IntPtr.Zero;

            long offset = localProcedure.ToInt64() - ownerBase.ToInt64();
            if (offset < 0 || offset >= ownerSize) return IntPtr.Zero;
            RemoteModule remoteOwner = FindRemoteModule(processId, ownerModuleName);
            if (remoteOwner == null) return IntPtr.Zero;

            IntPtr remoteProcedure = Add(remoteOwner.BaseAddress, offset);
            log.AppendLine(procedureName + " resolved through " + ownerModuleName
                + " localBase=0x" + ownerBase.ToInt64().ToString("X")
                + " remoteBase=0x" + remoteOwner.BaseAddress.ToInt64().ToString("X")
                + " rva=0x" + offset.ToString("X") + ".");
            return remoteProcedure;
        }

        private static bool TryCallRemoteExport(IntPtr process, IntPtr moduleBase,
            string exportName, int timeoutMs, StringBuilder log, out uint exitCode)
        {
            return TryCallRemoteExport(process, moduleBase, exportName,
                IntPtr.Zero, timeoutMs, log, out exitCode);
        }

        private static bool TryCallRemoteExport(IntPtr process, IntPtr moduleBase,
            string exportName, IntPtr parameter, int timeoutMs,
            StringBuilder log, out uint exitCode)
        {
            IntPtr address = TryGetRemoteExport(process, moduleBase, exportName);
            if (address == IntPtr.Zero)
            {
                exitCode = 0;
                log.AppendLine(exportName + ": remote export not found");
                return false;
            }
            return TryCallRemoteAddress(process, address, parameter, timeoutMs,
                log, exportName, out exitCode);
        }

        private static bool TryCallRemoteAddress(IntPtr process, IntPtr address,
            IntPtr parameter, int timeoutMs, StringBuilder log, string name,
            out uint exitCode)
        {
            return TryCallRemoteAddress(process, address, parameter, timeoutMs,
                log, name, out exitCode, out _, out _);
        }

        private static bool TryCallRemoteAddress(IntPtr process, IntPtr address,
            IntPtr parameter, int timeoutMs, StringBuilder log, string name,
            out uint exitCode, out bool threadStarted, out bool threadCompleted)
        {
            exitCode = 0;
            threadStarted = false;
            threadCompleted = false;
            IntPtr thread = CreateRemoteThread(process, IntPtr.Zero, IntPtr.Zero,
                address, parameter, 0, out uint threadId);
            if (thread == IntPtr.Zero)
            {
                log.AppendLine(name + ": CreateRemoteThread failed win32="
                    + Marshal.GetLastWin32Error());
                return false;
            }
            threadStarted = true;
            try
            {
                uint wait = WaitForSingleObject(thread, unchecked((uint)timeoutMs));
                if (wait != WaitObject0)
                {
                    log.AppendLine(name + ": wait=" + wait + " timeoutMs=" + timeoutMs
                        + " tid=" + threadId);
                    return false;
                }
                threadCompleted = true;
                if (!GetExitCodeThread(thread, out exitCode))
                {
                    log.AppendLine(name + ": GetExitCodeThread failed win32="
                        + Marshal.GetLastWin32Error());
                    return false;
                }
                log.AppendLine(name + ": tid=" + threadId + " exit=" + exitCode);
                return true;
            }
            finally
            {
                CloseHandle(thread);
            }
        }

        private static IntPtr TryGetRemoteExport(IntPtr process, IntPtr moduleBase,
            string exportName)
        {
            try
            {
                byte[] dos = ReadRemoteBytes(process, moduleBase, 0x100);
                if (dos.Length < 0x40 || ReadUInt16(dos, 0) != 0x5A4D)
                    return IntPtr.Zero;
                uint ntOffset = ReadUInt32(dos, 0x3C);
                if (ntOffset == 0 || ntOffset > 0x100000) return IntPtr.Zero;

                byte[] nt = ReadRemoteBytes(process, Add(moduleBase, ntOffset), 0x200);
                if (nt.Length < 0xF8 || ReadUInt32(nt, 0) != 0x4550)
                    return IntPtr.Zero;
                ushort optionalMagic = ReadUInt16(nt, 24);
                int dataDirectory = optionalMagic == 0x20B ? 136 : 120;
                uint exportRva = ReadUInt32(nt, dataDirectory);
                uint exportSize = ReadUInt32(nt, dataDirectory + 4);
                if (exportRva == 0) return IntPtr.Zero;

                byte[] export = ReadRemoteBytes(process, Add(moduleBase, exportRva), 40);
                uint functionCount = ReadUInt32(export, 20);
                uint nameCount = ReadUInt32(export, 24);
                uint functionsRva = ReadUInt32(export, 28);
                uint namesRva = ReadUInt32(export, 32);
                uint ordinalsRva = ReadUInt32(export, 36);
                if (functionCount == 0 || nameCount == 0
                    || functionCount > 65536 || nameCount > 65536)
                    return IntPtr.Zero;

                byte[] functions = ReadRemoteBytes(process, Add(moduleBase, functionsRva),
                    checked((int)functionCount * 4));
                byte[] names = ReadRemoteBytes(process, Add(moduleBase, namesRva),
                    checked((int)nameCount * 4));
                byte[] ordinals = ReadRemoteBytes(process, Add(moduleBase, ordinalsRva),
                    checked((int)nameCount * 2));
                if (functions.Length == 0 || names.Length == 0 || ordinals.Length == 0)
                    return IntPtr.Zero;

                for (int index = 0; index < nameCount; index++)
                {
                    uint nameRva = ReadUInt32(names, index * 4);
                    string name = ReadRemoteAnsiString(process, Add(moduleBase, nameRva), 256);
                    if (!string.Equals(name, exportName, StringComparison.Ordinal)) continue;
                    ushort ordinal = ReadUInt16(ordinals, index * 2);
                    if (ordinal >= functionCount) return IntPtr.Zero;
                    uint functionRva = ReadUInt32(functions, ordinal * 4);
                    if (exportSize > 0 && functionRva >= exportRva
                        && functionRva < exportRva + exportSize)
                        return IntPtr.Zero;
                    return Add(moduleBase, functionRva);
                }
            }
            catch
            {
            }
            return IntPtr.Zero;
        }

        private static byte[] ReadRemoteBytes(IntPtr process, IntPtr address, int count)
        {
            var bytes = new byte[count];
            return ReadProcessMemory(process, address, bytes, (IntPtr)count,
                out IntPtr read) && read.ToInt64() >= count ? bytes : new byte[0];
        }

        private static string ReadRemoteAnsiString(IntPtr process, IntPtr address,
            int maximumBytes)
        {
            byte[] bytes = ReadRemoteBytes(process, address, maximumBytes);
            int length = Array.IndexOf(bytes, (byte)0);
            if (length < 0) length = bytes.Length;
            return length == 0 ? string.Empty : Encoding.ASCII.GetString(bytes, 0, length);
        }

        private static ushort ReadUInt16(byte[] bytes, int offset)
        {
            return offset >= 0 && offset + 2 <= bytes.Length
                ? BitConverter.ToUInt16(bytes, offset) : (ushort)0;
        }

        private static uint ReadUInt32(byte[] bytes, int offset)
        {
            return offset >= 0 && offset + 4 <= bytes.Length
                ? BitConverter.ToUInt32(bytes, offset) : 0;
        }

        private static IntPtr Add(IntPtr address, long offset)
        {
            return new IntPtr(address.ToInt64() + offset);
        }

        private static string ComputeSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint desiredAccess,
            bool inheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address,
            IntPtr size, uint allocationType, uint protection);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VirtualFreeEx(IntPtr process, IntPtr address,
            IntPtr size, uint freeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteProcessMemory(IntPtr process, IntPtr address,
            byte[] buffer, IntPtr size, out IntPtr written);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadProcessMemory(IntPtr process, IntPtr address,
            byte[] buffer, IntPtr size, out IntPtr read);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr process,
            IntPtr threadAttributes, IntPtr stackSize, IntPtr startAddress,
            IntPtr parameter, uint creationFlags, out uint threadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetExitCodeThread(IntPtr thread, out uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandleW(string moduleName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryExW(string fileName,
            IntPtr file, uint flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr module);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr module, string procName);
    }
}
