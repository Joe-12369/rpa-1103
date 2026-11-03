using System;
namespace II.RPA.Plugin.ZExplorer.Cef
{
    [Serializable]
    public struct CefError : IEquatable<CefError>
    {
        internal CefError(CefErrorType error, string message)
        {
            this.Error = error;
            this.Message = message;
        }

        public CefErrorType Error { get; private set; }

        public string Message { get; private set; }

        public bool Equals(CefError o)
        {
            return o.Error == this.Error && o.Message == this.Message;
        }

        public override string ToString()
        {
            return string.Format("{0} {{ error = {1}, message = {2} }}", "CefError", this.Error, this.Message);
        }
    }
    [Serializable]
    public enum CefErrorType
    {
        Fail,
        NotStarted,
        NotFound,
        NoValue,
        NotConnected,
        NoWindow
    }
}
