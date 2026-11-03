namespace II.RPA.Plugin.GUI.Commons.WindowMatch.WindowCache
{
    public sealed class DesktopWindowCacheOptions
    {
        public static DesktopWindowCacheOptions Default { get; } = new DesktopWindowCacheOptions();

        public bool IncludeInvisibleWindows { get; set; }

        public bool IncludeToolWindows { get; set; }

        public bool IncludeNonRootWindows { get; set; }

        public int SnapshotDelayMilliseconds { get; set; } = 120;
    }
}


