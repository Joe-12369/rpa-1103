namespace II.RPA.Plugin.ZExplorer.Cef.EventArgs
{
    public class CloseEventArgs : System.EventArgs
    {
        public CloseEventArgs(string reason, bool wasClean)
        {
            this.Reason = reason;
            this.WasClean = wasClean;
        }

        internal string Reason { get; }

        internal bool WasClean { get; }
    }
}
