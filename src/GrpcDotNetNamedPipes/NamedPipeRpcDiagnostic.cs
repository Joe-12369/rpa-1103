namespace GrpcDotNetNamedPipes;

/// <summary>Optional observer; only calls carrying the diagnostic header are traced.</summary>
public static class NamedPipeRpcDiagnostic
{
    public static Action<string, string> Observer { get; set; }

    internal static void Mark(Metadata headers, string stage)
    {
        try
        {
            var observer = Observer;
            if (observer == null || headers == null) return;
            foreach (var header in headers)
            {
                if (header.Key == "x-window-rpc-diagnostic-id" && !header.IsBinary)
                {
                    observer(header.Value, stage);
                    return;
                }
            }
        }
        catch { /* An observer must never interrupt transport processing. */ }
    }
}
