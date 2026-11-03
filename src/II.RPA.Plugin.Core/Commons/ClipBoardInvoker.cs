using System;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Commons
{
    public static class ClipBoardInvoker
    {
        private static int _retryCount = 30;
        private static TimeSpan _retryDelay = TimeSpan.FromMilliseconds(100);
        private static readonly SemaphoreSlim _clipboardSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// OpenClipboard
        /// </summary>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        private static async Task<IDisposable> OpenClipboardAsync()
        {
            await Task.Delay(5);
            await _clipboardSemaphore.WaitAsync();
            try
            {
                var i = _retryCount;

                while (!PInvoke.OpenClipboard(HANDLE.Null))
                {
                    if (--i == 0)
                        throw new TimeoutException("Timeout opening clipboard.");
                    await Task.Delay(_retryDelay);
                }

                return Disposable.Create(() => PInvoke.CloseClipboard());
            }
            finally
            {
                _clipboardSemaphore.Release();
            }
        }
        private static IDisposable OpenClipboard()
        {
            Thread.Sleep(5);
            _clipboardSemaphore.Wait();
            try
            {
                var i = _retryCount;

                while (!PInvoke.OpenClipboard(HANDLE.Null))
                {
                    if (--i == 0)
                        throw new TimeoutException("Timeout opening clipboard.");
                    Thread.Sleep(_retryDelay);
                }

                return Disposable.Create(() => PInvoke.CloseClipboard());
            }
            finally
            {
                _clipboardSemaphore.Release();
            }
        }

        public static void SetupEnvironment(int retryCount, TimeSpan retryDelay)
        {
            _retryCount = retryCount;
            _retryDelay = retryDelay;
        }
        public static async Task<string> GetTextAsync()
        {
            using (await OpenClipboardAsync())
            {
                unsafe
                {
                    IntPtr hText = PInvoke.GetClipboardData((uint)ClipboardFormat.CF_UNICODETEXT);
                    if (hText == IntPtr.Zero)
                    {
                        return null;
                    }

                    var pText = (IntPtr)GlobalLock(new GlobalFreeSafeHandle(hText));
                    if (pText == IntPtr.Zero)
                    {
                        return null;
                    }

                    var rv = Marshal.PtrToStringUni(pText);
                    GlobalUnlock(new GlobalFreeSafeHandle(hText));
                    return rv;
                }
            }
        }
        public static string GetText()
        {
            using (OpenClipboard())
            {
                unsafe
                {
                    IntPtr hText = PInvoke.GetClipboardData((uint)ClipboardFormat.CF_UNICODETEXT);
                    if (hText == IntPtr.Zero)
                    {
                        return null;
                    }

                    var pText = (IntPtr)GlobalLock(new GlobalFreeSafeHandle(hText));
                    if (pText == IntPtr.Zero)
                    {
                        return null;
                    }

                    var rv = Marshal.PtrToStringUni(pText);
                    GlobalUnlock(new GlobalFreeSafeHandle(hText));
                    return rv;
                }
            }
        }
        public static async Task SetTextAsync(string text)
        {
            using (await OpenClipboardAsync())
            {
                PInvoke.EmptyClipboard();

                if (!(text is null))
                {
                    var hGlobal = Marshal.StringToHGlobalUni(text);
                    PInvoke.SetClipboardData((uint)ClipboardFormat.CF_UNICODETEXT, new HANDLE(hGlobal));
                }
            }
        }
        public static void SetText(string text)
        {
            using (OpenClipboard())
            {
                PInvoke.EmptyClipboard();

                if (!(text is null))
                {
                    var hGlobal = Marshal.StringToHGlobalUni(text);
                    PInvoke.SetClipboardData((uint)ClipboardFormat.CF_UNICODETEXT, new HANDLE(hGlobal));
                }
            }
        }
        public static async Task ClearAsync()
        {
            using (await OpenClipboardAsync())
            {
                PInvoke.EmptyClipboard();
            }
        }
        public static void Clear()
        {
            using (OpenClipboard())
            {
                PInvoke.EmptyClipboard();
            }
        }

        public static async Task SleepAsync(TimeSpan timeSpan)
        {
            await Task.Delay(timeSpan);
        }

        public static async Task SleepAsync(int milliSecond)
        {
            await Task.Delay(milliSecond);
        }

        public enum ClipboardFormat
        {
            CF_TEXT = 1,
            CF_BITMAP = 2,
            CF_DIB = 3,
            CF_UNICODETEXT = 13,
            CF_HDROP = 15,
        }

        private static unsafe void* GlobalLock(SafeHandle hMem)
        {
            bool hMemAddRef = false;
            try
            {
                HANDLE hMemLocal;
                if (hMem is object)
                {
                    hMem.DangerousAddRef(ref hMemAddRef);
                    hMemLocal = (HANDLE)hMem.DangerousGetHandle();
                }
                else
                    throw new ArgumentNullException(nameof(hMem));
                void* __result = PInvoke.GlobalLock(hMemLocal);
                return __result;
            }
            finally
            {
                if (hMemAddRef)
                    hMem.Dispose();
            }
        }
        private static unsafe BOOL GlobalUnlock(SafeHandle hMem)
        {
            bool hMemAddRef = false;
            try
            {
                HANDLE hMemLocal;
                if (hMem is object)
                {
                    hMem.DangerousAddRef(ref hMemAddRef);
                    hMemLocal = (HANDLE)hMem.DangerousGetHandle();
                }
                else
                    throw new ArgumentNullException(nameof(hMem));
                BOOL __result = PInvoke.GlobalUnlock(hMemLocal);
                return __result;
            }
            finally
            {
                if (hMemAddRef)
                    hMem.Dispose();
            }
        }
    }
	internal static partial class PInvoke
    {
        [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern HANDLE GlobalFree(HANDLE hMem);
        [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern unsafe void* GlobalLock(HANDLE hMem);
        [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern BOOL GlobalUnlock(HANDLE hMem);
        [DllImport("USER32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern BOOL OpenClipboard(HANDLE hWndNewOwner);
        [DllImport("USER32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern BOOL EmptyClipboard();
        [DllImport("USER32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern HANDLE SetClipboardData(uint uFormat, HANDLE hMem);
        [DllImport("USER32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern BOOL CloseClipboard();
        [DllImport("USER32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern HANDLE GetOpenClipboardWindow();
        [DllImport("USER32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern HANDLE GetClipboardData(uint uFormat);
        [DllImport("USER32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern HANDLE GetClipboardOwner();
    }
    internal class GlobalFreeSafeHandle : SafeHandle
    {
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1L);

        public GlobalFreeSafeHandle() : base(INVALID_HANDLE_VALUE, true)
        {
        }

        public GlobalFreeSafeHandle(IntPtr preexistingHandle, bool ownsHandle = true) : base(INVALID_HANDLE_VALUE, ownsHandle)
        {
            this.SetHandle(preexistingHandle);
        }

        public override bool IsInvalid => this.handle.ToInt64() == -1L || this.handle.ToInt64() == 0L;

        protected override bool ReleaseHandle() => PInvoke.GlobalFree((HANDLE)this.handle) == default(HANDLE);
    }
    internal readonly partial struct BOOL
            : IEquatable<BOOL>
    {
        public readonly int Value;

        public BOOL(int value) => this.Value = value;

        public static implicit operator int(BOOL value) => value.Value;

        public static explicit operator BOOL(int value) => new BOOL(value);

        public static bool operator ==(BOOL left, BOOL right) => left.Value == right.Value;

        public static bool operator !=(BOOL left, BOOL right) => !(left == right);

        public bool Equals(BOOL other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is BOOL other && this.Equals(other);

        public override int GetHashCode() => this.Value.GetHashCode();

        public override string ToString() => $"0x{this.Value:x}";

        public BOOL(bool value) => this.Value = value ? 1 : 0;

        public static implicit operator bool(BOOL value) => value.Value != 0;

        public static implicit operator BOOL(bool value) => new BOOL(value);
    }
    internal unsafe readonly struct HANDLE
            : IEquatable<HANDLE>
    {
        public readonly void* Value;

        public HANDLE(void* value) => this.Value = value;

        public HANDLE(IntPtr value) : this((void*)value)
        {
        }

        public static HANDLE Null => default;

        public bool IsNull => Value == default;

        public static implicit operator void*(HANDLE value) => value.Value;

        public static explicit operator HANDLE(void* value) => new HANDLE(value);

        public static bool operator ==(HANDLE left, HANDLE right) => left.Value == right.Value;

        public static bool operator !=(HANDLE left, HANDLE right) => !(left == right);

        public bool Equals(HANDLE other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is HANDLE other && this.Equals(other);

        public override int GetHashCode() => unchecked((int)this.Value);

        public override string ToString() => $"0x{(UIntPtr)this.Value:x}";

        public static implicit operator IntPtr(HANDLE value) => new IntPtr(value.Value);

        public static explicit operator HANDLE(IntPtr value) => new HANDLE((void*)value.ToPointer());

        public static explicit operator HANDLE(UIntPtr value) => new HANDLE((void*)value.ToPointer());
    }
}
