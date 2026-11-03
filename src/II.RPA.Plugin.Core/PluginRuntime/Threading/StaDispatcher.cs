using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace II.RPA.Plugin.Core.Threading
{
    public sealed class StaDispatcher : IStaDispatcher
    {
        private readonly object initializationLock = new object();
        private Task<Dispatcher> dispatcherTask;

        public Task InvokeAsync(Action operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            Func<object> operationAdapter = () =>
            {
                operation();
                return null;
            };
            return InvokeAsync(operationAdapter);
        }

        public async Task<TResult> InvokeAsync<TResult>(Func<TResult> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            var dispatcher = await GetDispatcherAsync().ConfigureAwait(false);
            if (dispatcher.CheckAccess()) return operation();

            var completion = CreateCompletionSource<TResult>();
            try
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        completion.TrySetResult(operation());
                    }
                    catch (Exception exception)
                    {
                        completion.TrySetException(exception);
                    }
                }));
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }

            return await completion.Task.ConfigureAwait(false);
        }

        public async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            var dispatcher = await GetDispatcherAsync().ConfigureAwait(false);
            if (dispatcher.CheckAccess()) return await operation();

            var completion = CreateCompletionSource<TResult>();
            try
            {
                dispatcher.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        completion.TrySetResult(await operation());
                    }
                    catch (Exception exception)
                    {
                        completion.TrySetException(exception);
                    }
                }));
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }

            return await completion.Task.ConfigureAwait(false);
        }

        private Task<Dispatcher> GetDispatcherAsync()
        {
            lock (initializationLock)
            {
                if (dispatcherTask == null) dispatcherTask = StartDispatcherThread();
                return dispatcherTask;
            }
        }

        private static Task<Dispatcher> StartDispatcherThread()
        {
            var completion = CreateCompletionSource<Dispatcher>();
            var thread = new Thread(() => RunDispatcher(completion))
            {
                IsBackground = true,
                Name = "RPA Plugin STA Dispatcher"
            };

            try
            {
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }

            return completion.Task;
        }

        private static void RunDispatcher(TaskCompletionSource<Dispatcher> completion)
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(dispatcher));
                completion.TrySetResult(dispatcher);
                Dispatcher.Run();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }

        private static TaskCompletionSource<TResult> CreateCompletionSource<TResult>()
        {
            return new TaskCompletionSource<TResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
