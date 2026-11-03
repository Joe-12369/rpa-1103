using System;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.RuntimeHost.Services
{
    internal static class SynchronizationContextDispatcher
    {
        public static Task<T> InvokeAsync<T>(
            SynchronizationContext context,
            Func<Task<T>> operation)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            var completion = new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            context.Post(async _ =>
            {
                try
                {
                    completion.TrySetResult(await operation().ConfigureAwait(true));
                }
                catch (OperationCanceledException)
                {
                    completion.TrySetCanceled();
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
            }, null);

            return completion.Task;
        }
    }
}
