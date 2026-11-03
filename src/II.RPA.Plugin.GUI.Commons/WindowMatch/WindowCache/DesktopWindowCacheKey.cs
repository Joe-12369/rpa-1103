using System;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.WindowCache
{
    public struct DesktopWindowCacheKey : IEquatable<DesktopWindowCacheKey>
    {
        public DesktopWindowCacheKey(IntPtr handle, int processId)
        {
            Handle = handle;
            ProcessId = processId;
        }

        public IntPtr Handle { get; }

        public int ProcessId { get; }

        public bool Equals(DesktopWindowCacheKey other)
        {
            return Handle == other.Handle && ProcessId == other.ProcessId;
        }

        public override bool Equals(object obj)
        {
            return obj is DesktopWindowCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Handle.GetHashCode() * 397) ^ ProcessId;
            }
        }

        public override string ToString()
        {
            return ProcessId + "_" + Handle;
        }
    }
}


