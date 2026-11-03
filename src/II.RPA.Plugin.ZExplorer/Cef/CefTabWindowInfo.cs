using System;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    [Serializable]
    public struct CefTabWindowInfo : IEquatable<CefTabWindowInfo>
    {
        public string Id { get; private set; }

        public bool Selected { get; private set; }

        public string Url { get; private set; }

        public string Title { get; private set; }

        public string Status { get; private set; }

        public string BrowserVersion { get; private set; }

        public string Framework { get; private set; }

        // public IntPtr HWND { get; private set; }

        public CefTabWindowInfo(string id, bool selected, string url, string title, string status, string browserVersion, string framework)
        {
            this.Id = id;
            this.Selected = selected;
            this.Url = url;
            this.Title = title;
            this.Status = status;
            this.BrowserVersion = browserVersion;
            this.Framework = framework;
        }

        public bool Equals(CefTabWindowInfo o)
        {
            return o.Id == this.Id;// && o.WindowInfo.Id.Equals(this.WindowInfo.Id);
        }

        public override string ToString()
        {
            return $"ID:{Id}";
        }
    }
}
