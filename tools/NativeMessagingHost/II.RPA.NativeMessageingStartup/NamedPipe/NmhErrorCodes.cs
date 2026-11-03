namespace II.RPA.NativeMessageingStartup.NamedPipe
{
    internal static class NmhErrorCodes
    {
        internal const int ServiceBusy = -110001;
        internal const int MessageIdConflict = -110002;
        internal const int UnprocessableMessage = -110003;
        internal const int RequestExpired = -110004;
        internal const int CrxTimeout = -110005;
        internal const int CrxUnavailable = -110006;
        internal const int ConnectionClosed = -110007;
        internal const int HostStopping = -110008;
        internal const int InternalError = -110009;
    }
}
