using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class AutomationHelper
    {
        public static SynchronizationContext syncContext { get; set; }

        private static ILogger logger = Logger.OfContext();

        public static TResult Send<TResult>(Func<TResult> func)
        {
            TResult retval = default(TResult);
            syncContext.Send(new SendOrPostCallback((x) =>
            {
                retval = func();
            })
            , null);
            return retval;
        }
        public static TResult Post<TResult>(Func<TResult> func)
        {
            TResult retval = default(TResult);
            syncContext.Post(new SendOrPostCallback((x) =>
            {
                retval = func();
            })
            , null);
            return retval;
        }

        //public static TResult Send<TResult>(Func<TResult> func)
        //{
        //    TResult retval = default(TResult);
        //    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
        //    {
        //        retval = func();
        //    });
        //    return retval;
        //}

        public static Task<T> RunSTAThread<T>(Func<T> action, TimeSpan Timeout)
        {
            var tcs = new TaskCompletionSource<T>();
            var runThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    var result = action();
                    tcs.TrySetResult(result);
                }
                catch (ThreadAbortException ex)
                {
                    // Do NOT set it here, or applicatin will crash with an UnhandledException
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, ex.Message);
                    tcs.TrySetException(ex);
                }
            }));

            runThread.Name = "STAThread";
            runThread.SetApartmentState(ApartmentState.STA);
            runThread.Start();

            if (!runThread.Join(Timeout))
            {
                runThread.Abort();
                tcs.TrySetResult(default(T));
            }

            return tcs.Task;
        }
    }
}

