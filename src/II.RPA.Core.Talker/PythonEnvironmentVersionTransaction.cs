namespace II.RPA.Core.Talker;

internal static class PythonEnvironmentVersionTransaction
{
    internal static async Task ApplyAsync(
        string requestedVersion,
        Func<string> getCurrentVersion,
        Action<string> setVersion,
        Func<Task> restartAsync)
    {
        ArgumentNullException.ThrowIfNull(getCurrentVersion);
        ArgumentNullException.ThrowIfNull(setVersion);
        ArgumentNullException.ThrowIfNull(restartAsync);

        var previousVersion = getCurrentVersion();
        setVersion(requestedVersion);
        try
        {
            await restartAsync().ConfigureAwait(false);
        }
        catch
        {
            setVersion(previousVersion);
            throw;
        }
    }
}
