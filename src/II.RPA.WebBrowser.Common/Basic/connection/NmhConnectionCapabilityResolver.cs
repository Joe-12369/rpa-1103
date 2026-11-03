using II.RPA.Plugin.Core;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace II.RPA.WebBrowser.Common.Basic.connection
{
    internal enum NmhConnectionProtocol
    {
        Failure,
        WcfIpc,
        NamedPipe
    }

    internal sealed class NmhConnectionCapability
    {
        internal NmhConnectionCapability(
            int processId,
            string exeFilePath,
            Version fileVersion,
            NmhConnectionProtocol protocol,
            Exception error = null)
        {
            ProcessId = processId;
            ExeFilePath = exeFilePath;
            FileVersion = fileVersion;
            Protocol = protocol;
            Error = error;
        }

        public int ProcessId { get; }

        public string ExeFilePath { get; }

        public Version FileVersion { get; }

        public NmhConnectionProtocol Protocol { get; }

        public Exception Error { get; }
    }

    internal interface INmhProcessMetadataReader
    {
        string GetExeFilePath(int processId);

        string GetFileVersion(string exeFilePath);
    }

    internal interface INmhConnectionCapabilityResolver
    {
        NmhConnectionCapability Resolve(int processId);
    }

    internal sealed class NmhProcessMetadataReader : INmhProcessMetadataReader
    {
        public string GetExeFilePath(int processId)
        {
            using (Process process = Process.GetProcessById(processId))
            {
                return process.GetExeFilePath();
            }
        }

        public string GetFileVersion(string exeFilePath)
        {
            if (!File.Exists(exeFilePath))
            {
                throw new FileNotFoundException("NMH executable does not exist.", exeFilePath);
            }

            return FileVersionInfo.GetVersionInfo(exeFilePath).FileVersion;
        }
    }

    internal sealed class NmhConnectionCapabilityResolver : INmhConnectionCapabilityResolver
    {
        internal static readonly Version NamedPipeMinimumVersion = new Version(7, 3, 7, 0);

        private readonly INmhProcessMetadataReader _metadataReader;

        internal NmhConnectionCapabilityResolver()
            : this(new NmhProcessMetadataReader())
        {
        }

        internal NmhConnectionCapabilityResolver(INmhProcessMetadataReader metadataReader)
        {
            _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
        }

        public NmhConnectionCapability Resolve(int processId)
        {
            string exeFilePath = null;
            try
            {
                exeFilePath = _metadataReader.GetExeFilePath(processId);
                if (string.IsNullOrWhiteSpace(exeFilePath))
                {
                    throw new InvalidOperationException("NMH executable path is empty.");
                }

                string fileVersionText = _metadataReader.GetFileVersion(exeFilePath);
                Logger.OfContext().LogInformation(
                    $"[NMH.FileVersion] NmhPid={processId} FileVersion={fileVersionText}");
                if (!Version.TryParse(fileVersionText, out Version fileVersion))
                {
                    throw new FormatException($"Invalid NMH file version: '{fileVersionText}'.");
                }

                NmhConnectionProtocol protocol = fileVersion >= NamedPipeMinimumVersion
                    ? NmhConnectionProtocol.NamedPipe
                    : NmhConnectionProtocol.WcfIpc;
                return new NmhConnectionCapability(
                    processId,
                    exeFilePath,
                    fileVersion,
                    protocol);
            }
            catch (Exception ex)
            {
                return new NmhConnectionCapability(
                    processId,
                    exeFilePath,
                    null,
                    NmhConnectionProtocol.Failure,
                    ex);
            }
        }
    }
}
