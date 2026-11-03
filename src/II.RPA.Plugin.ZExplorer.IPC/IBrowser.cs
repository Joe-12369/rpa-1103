namespace II.RPA.Plugin.ZExplorer.IPC
{
    public interface IBrowser
    {
        string Ping();

        int GetActiveTab();

        int GetActiveTabByTitleOrUrl(string title, string url);

        string GetActiveTabsId();

        int NewTab(string title, string url);

        string GetTitle(int tabId);

        string GetUrl(int tabId);

        string GetAllTab();

        string GetTabDetail(int tabId);

        void CloseAll();

        void Maximized();

        void Minimized();

        string Query(string param);

        string GetTab(string param);

        string Stop(string param);

        string QueryCssSlector(string param);

        string GetScroll(string param);

        string Scroll(string param);

        string Search(string param);

        string DownloadUrl(string param);

        void ResetTrace(string param);

        string GetTraceItems(string param);        

        void RemoveCookie(string param);

        string GetCookies(string param);

        void Navigate(string param);

        void Close(string param);

        void GoBack(string param);

        void GoForward(string param);

        void Reload(string param);

        string Active(string param);
        string GetDocumentPoint();
    }
}