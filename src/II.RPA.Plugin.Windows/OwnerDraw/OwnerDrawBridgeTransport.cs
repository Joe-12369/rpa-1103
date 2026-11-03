using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    internal interface IOwnerDrawBridgeTransport
    {
        bool TryRoundTrip(string pipePrefix, int processId, string request,
            int connectTimeoutMs, int readTimeoutMs, int maximumResponseBytes,
            out string response, out string status);
    }

    /// <summary>
    /// One request per connection, matching the customer-validated bridge servers.
    /// UTF-8 is strict and response size is bounded so malformed native output cannot
    /// turn a selector lookup into unbounded memory growth.
    /// </summary>
    internal sealed class NamedPipeOwnerDrawBridgeTransport : IOwnerDrawBridgeTransport
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public bool TryRoundTrip(string pipePrefix, int processId, string request,
            int connectTimeoutMs, int readTimeoutMs, int maximumResponseBytes,
            out string response, out string status)
        {
            response = string.Empty;
            if (string.IsNullOrWhiteSpace(pipePrefix) || processId <= 0
                || string.IsNullOrWhiteSpace(request))
            {
                status = "Invalid owner-draw bridge transport request.";
                return false;
            }

            string pipeName = pipePrefix + processId;
            string command = request.Split('\t')[0];
            var watch = Stopwatch.StartNew();
            try
            {
                using (var pipe = new NamedPipeClientStream(".", pipeName,
                    PipeDirection.InOut, PipeOptions.Asynchronous,
                    TokenImpersonationLevel.Identification))
                {
                    pipe.Connect(connectTimeoutMs);
                    if (!TryValidateServerProcess(pipe, processId,
                        out string serverStatus))
                    {
                        status = command + " pipe=\"" + pipeName
                            + "\" rejected: " + serverStatus;
                        return false;
                    }

                    byte[] requestBytes = StrictUtf8.GetBytes(
                        request.TrimEnd('\r', '\n') + "\n");
                    WriteWithTimeout(pipe, requestBytes, readTimeoutMs);
                    response = ReadLine(pipe, maximumResponseBytes, readTimeoutMs);
                    if (string.IsNullOrWhiteSpace(response))
                    {
                        status = command + " pipe=\"" + pipeName
                            + "\" returned an empty response elapsedMs="
                            + watch.ElapsedMilliseconds + ".";
                        return false;
                    }

                    status = command + " pipe=\"" + pipeName + "\" requestBytes="
                        + requestBytes.Length + " responseLength=" + response.Length
                        + " elapsedMs=" + watch.ElapsedMilliseconds + ".";
                    return true;
                }
            }
            catch (Exception ex) when (ex is TimeoutException
                || ex is IOException || ex is UnauthorizedAccessException
                || ex is InvalidDataException || ex is DecoderFallbackException)
            {
                status = command + " pipe=\"" + pipeName + "\" failed elapsedMs="
                    + watch.ElapsedMilliseconds + ": " + ex.GetType().Name + ": "
                    + Short(ex.Message);
                return false;
            }
        }

        private static void WriteWithTimeout(Stream stream, byte[] bytes,
            int timeoutMs)
        {
            IAsyncResult pending = null;
            WaitHandle waitHandle = null;
            try
            {
                pending = stream.BeginWrite(bytes, 0, bytes.Length, null, null);
                waitHandle = pending.AsyncWaitHandle;
                if (!waitHandle.WaitOne(Math.Max(1, timeoutMs)))
                    throw new TimeoutException("Bridge pipe write timed out after "
                        + timeoutMs + " ms.");
                stream.EndWrite(pending);
            }
            finally
            {
                if (waitHandle != null) waitHandle.Close();
            }
        }

        private static string ReadLine(Stream stream, int maximumResponseBytes,
            int timeoutMs)
        {
            int limit = Math.Max(1024, maximumResponseBytes);
            var bytes = new List<byte>(Math.Min(4096, limit));
            var buffer = new byte[1024];
            var deadline = Stopwatch.StartNew();
            while (bytes.Count < limit)
            {
                if (deadline.ElapsedMilliseconds >= timeoutMs)
                    throw new TimeoutException("Bridge pipe read timed out after "
                        + timeoutMs + " ms.");
                int remaining = Math.Max(1, timeoutMs
                    - unchecked((int)deadline.ElapsedMilliseconds));

                IAsyncResult pending = null;
                WaitHandle waitHandle = null;
                int read;
                try
                {
                    pending = stream.BeginRead(buffer, 0, buffer.Length, null, null);
                    waitHandle = pending.AsyncWaitHandle;
                    if (!waitHandle.WaitOne(remaining))
                        throw new TimeoutException("Bridge pipe read timed out after "
                            + timeoutMs + " ms.");
                    read = stream.EndRead(pending);
                }
                finally
                {
                    if (waitHandle != null) waitHandle.Close();
                }
                if (read <= 0) break;
                for (int index = 0; index < read; index++)
                {
                    byte value = buffer[index];
                    if (value == (byte)'\n')
                        return StrictUtf8.GetString(bytes.ToArray()).TrimEnd('\r');
                    if (value != 0) bytes.Add(value);
                    if (bytes.Count >= limit)
                    {
                        throw new InvalidDataException("Bridge response exceeded "
                            + limit + " bytes.");
                    }
                }
            }
            return bytes.Count == 0
                ? string.Empty
                : StrictUtf8.GetString(bytes.ToArray()).TrimEnd('\r');
        }

        private static bool TryValidateServerProcess(NamedPipeClientStream pipe,
            int expectedProcessId, out string status)
        {
            if (pipe == null || !pipe.IsConnected || expectedProcessId <= 0)
            {
                status = "Named-pipe server validation received invalid state.";
                return false;
            }

            if (!GetNamedPipeServerProcessId(
                pipe.SafePipeHandle.DangerousGetHandle(), out uint serverProcessId))
            {
                status = "GetNamedPipeServerProcessId failed win32="
                    + Marshal.GetLastWin32Error() + ".";
                return false;
            }
            if (serverProcessId != unchecked((uint)expectedProcessId))
            {
                status = "Named-pipe endpoint PID mismatch expected="
                    + expectedProcessId + " actual=" + serverProcessId + ".";
                return false;
            }
            status = "Named-pipe endpoint PID verified.";
            return true;
        }

        private static string Short(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string normalized = value.Replace("\r", " ").Replace("\n", " ");
            return normalized.Length <= 200
                ? normalized : normalized.Substring(0, 197) + "...";
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNamedPipeServerProcessId(IntPtr pipe,
            out uint serverProcessId);
    }
}
