using System;
namespace II.RPA.Plugin.GUI.Commons.WindowMatch.WindowCache
{
    public enum DesktopWindowState
    {
        Active = 0,
        Hidden = 1,
        Closed = 2
    }

    public enum DesktopWindowUpdateSource
    {
        InitialScan = 0,
        WinEvent = 1,
        Manual = 2
    }

    public sealed class DesktopWindowRecord
    {
        public DesktopWindowRecord(DesktopWindowCacheKey key)
            : this(key, DateTimeOffset.Now)
        {
        }

        private DesktopWindowRecord(DesktopWindowCacheKey key, DateTimeOffset firstSeenAt)
        {
            Key = key;
            Handle = key.Handle;
            ProcessId = key.ProcessId;
            FirstSeenAt = firstSeenAt;
            LastSeenAt = FirstSeenAt;
            State = DesktopWindowState.Active;
        }

        public DesktopWindowCacheKey Key { get; }

        public IntPtr Handle { get; }

        public int ProcessId { get; }

        public string ProcessName { get; set; }

        public string ProgramPath { get; set; }

        public DateTimeOffset FirstSeenAt { get; }

        public DateTimeOffset LastSeenAt { get; set; }

        public DateTimeOffset? ClosedAt { get; set; }

        public DesktopWindowState State { get; set; }

        public DesktopWindowUpdateSource LastUpdateSource { get; set; }

        public DesktopWindowRecord Clone()
        {
            return new DesktopWindowRecord(Key, FirstSeenAt)
            {
                ProcessName = ProcessName,
                ProgramPath = ProgramPath,
                LastSeenAt = LastSeenAt,
                ClosedAt = ClosedAt,
                State = State,
                LastUpdateSource = LastUpdateSource
            };
        }

        internal void ApplySnapshot(DesktopWindowRecord snapshot)
        {
            ProcessName = snapshot.ProcessName;
            ProgramPath = string.IsNullOrWhiteSpace(snapshot.ProgramPath) ? ProgramPath : snapshot.ProgramPath;
            LastSeenAt = DateTimeOffset.Now;
            ClosedAt = null;
            State = snapshot.State;
            LastUpdateSource = snapshot.LastUpdateSource;
        }
    }
}


