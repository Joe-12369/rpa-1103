using System;

namespace II.RPA.WebBrowser.Common.Basic.connection.NamedPipe
{
    internal enum NamedPipeDiagnosticLevel
    {
        Debug,
        Warning,
        Error
    }

    internal interface INamedPipeRequestDiagnosticSink
    {
        void Write(
            NamedPipeDiagnosticLevel level,
            string message,
            Exception exception);
    }
}
