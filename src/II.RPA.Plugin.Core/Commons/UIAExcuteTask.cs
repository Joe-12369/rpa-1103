using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.Core.Commons
{
    public class UIAExcuteTask
    {
        private static ILogger logger = Logger.OfContext();
        private static List<RPAThreadMethodTask> rpaThreadMethodTaskList = null;
        //private static ConcurrentQueue<RPAThreadMethodTask> rpaThreadMethodTaskList = null;
        private static readonly object _lockObj = new object();
        private static ManualResetEvent _resetEvent;
        private static int _taskThreadID = 0;
        public static Rectangle InspectInfoWndRect { get; set; } = Rectangle.Empty;
        public static Action<Point> MoveInspectInfoWndRect = null;

        static UIAExcuteTask()
        {
            _resetEvent = new ManualResetEvent(false);
            new Task(() =>
            {
                _taskThreadID = Thread.CurrentThread.ManagedThreadId;
                InvokeMarshaledCallbacks();
            }, CancellationToken.None, TaskCreationOptions.LongRunning).Start();
        }


        private static object MarshaledInvoke(Delegate method, object[] args, bool synchronous, int priority)
        {
            //ExecutionContext executionContext = ExecutionContext.Capture();
            RPAThreadMethodTask threadMethod = new RPAThreadMethodTask(method, args, synchronous, priority);
            lock (_lockObj)
            {
                if (rpaThreadMethodTaskList == null)
                {
                    rpaThreadMethodTaskList = new List<RPAThreadMethodTask>();
                }
            }
            if (_taskThreadID == Thread.CurrentThread.ManagedThreadId)
            {
                try
                {
                    InvokeMarshaledCallbackDo(threadMethod);
                }
                catch (Exception ex)
                {
                    threadMethod.exception = ex.GetBaseException();
                }
                finally
                {
                    threadMethod.Complete();
                }
            }
            else
            {
                lock (rpaThreadMethodTaskList)
                {
                    rpaThreadMethodTaskList.Add(threadMethod);
                    rpaThreadMethodTaskList = rpaThreadMethodTaskList.OrderBy(o => o.Priority).ThenBy(o => o.CreateTime).ToList();
                    _resetEvent.Set();
                }
            }
            //InvokeMarshaledCallbacks();

            if (synchronous)
            {
                if (!threadMethod.IsCompleted)
                {
                    WaitForWaitHandle(threadMethod.AsyncWaitHandle);
                }

                if (threadMethod.exception != null)
                {
                    throw threadMethod.exception;
                }

                return threadMethod.retVal;
            }
            return threadMethod;
        }
        private static void InvokeMarshaledCallbacks()
        {
            _resetEvent.WaitOne();
            while (true)
            {
                RPAThreadMethodTask threadMethodEntry = null;
                try
                {
                    lock (rpaThreadMethodTaskList)
                    {
                        if (rpaThreadMethodTaskList.Count > 0)
                        {
                            threadMethodEntry = (RPAThreadMethodTask)rpaThreadMethodTaskList.First();
                            rpaThreadMethodTaskList.RemoveAt(0);
                        }
                    }

                    while (threadMethodEntry != null)
                    {
                        if ((object)threadMethodEntry.method != null)
                        {
                            try
                            {
                                //if (NativeWindow.WndProcShouldBeDebuggable && !threadMethodEntry.synchronous)
                                //{
                                //    InvokeMarshaledCallback(threadMethodEntry);
                                //}
                                //else
                                //{
                                try
                                {
                                    //ExecutionContext.Run(threadMethodEntry.executionContext, InvokeMarshaledCallbackDo, threadMethodEntry);
                                    InvokeMarshaledCallbackDo(threadMethodEntry);
                                }
                                catch (Exception ex)
                                {
                                    threadMethodEntry.exception = ex.GetBaseException();
                                }
                                //}
                            }
                            finally
                            {
                                threadMethodEntry.Complete();
                            }
                        }

                        lock (rpaThreadMethodTaskList)
                        {
                            if (rpaThreadMethodTaskList.Count <= 0)
                            {
                                threadMethodEntry = null;
                            }
                            else
                            {
                                threadMethodEntry = rpaThreadMethodTaskList.First();
                                rpaThreadMethodTaskList.RemoveAt(0);
                            }
                        }
                    }
                    _resetEvent.WaitOne();
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"异步执行方法发生了异常：{threadMethodEntry?.method.GetType().Name}");
                }
            }
        }
        private static void InvokeMarshaledCallbackDo(object obj)
        {
            //if (!IsValidAction())
            //{
            //    logger.LogDebug("鼠标点在拾取状态框范围，本次方法暂停10毫秒再执行");
            //    Thread.Sleep(10);
            //    InvokeMarshaledCallbackDo(obj);
            //    return;
            //}
            RPAThreadMethodTask tmt = (RPAThreadMethodTask)obj;
            if (tmt.method is MethodInvoker)
            {
                ((MethodInvoker)tmt.method)();
            }
            else if (tmt.method is WaitCallback)
            {
                ((WaitCallback)tmt.method)(tmt.args[0]);
            }
            else
            {
                tmt.retVal = tmt.method.DynamicInvoke(tmt.args);
            }
        }
        private static void WaitForWaitHandle(WaitHandle waitHandle)
        {
            waitHandle.WaitOne(60 * 1000, exitContext: false);
        }
        //public static object Invoke(Delegate method, params object[] args)
        //{
        //    return MarshaledInvoke(method, args, true, 0);
        //}
        public static void Send(Action action)
        {
            Send(action, 0);
            return;
        }
        public static void Send(Action action, int priority)
        {
            MarshaledInvoke(action, null, true, priority);
            return;
        }
        public static TResult Send<TResult>(Func<TResult> func)
        {
            return Send(func, 0);
        }

        public static TResult Send<TResult>(Delegate method, params object[] args)
        {
            var ret = MarshaledInvoke(method, args, true, 0);
            if (ret != null)
                return (TResult)ret;
            return default(TResult);
        }
        public static TResult Send<TResult>(Func<TResult> func, int priority)
        {
            var ret = MarshaledInvoke(func, null, true, priority);
            if (ret != null)
                return (TResult)ret;
            return default(TResult);
        }

        //private static bool IsValidAction()
        //{
        //    var screenPoint = Cursor.Position;
        //    var ret = !InspectInfoWndRect.Contains(screenPoint);
        //    if (!ret)
        //    {
        //        logger.LogDebug("鼠标与拾取状态框重合了，需要移动状态框");
        //        //MoveInspectInfoWndRect?.Invoke(screenPoint);
        //    }
        //    return ret;
        //}

    }

    class RPAThreadMethodTask : IAsyncResult
    {
        private ManualResetEvent resetEvent;
        private object invokeSyncObject = new object();
        public bool IsCompleted => isCompleted;
        internal DateTime CreateTime { get; private set; }

        public object AsyncState => null;
        internal bool synchronous;
        private bool isCompleted;
        internal Delegate method;
        internal object[] args;
        internal object retVal;
        internal Exception exception;
        internal ExecutionContext executionContext;
        internal int Priority { get; set; } = 0;
        public RPAThreadMethodTask(Delegate method, object[] args, bool synchronous, int priority = 0)
        {
            //var ec = ExecutionContext.Capture();
            //ExecutionContext.Run(ec, delegate
            //{
            //    // 这里的代码将上述 ec 的状态视为周围环境
            //}, null);
            this.CreateTime = DateTime.Now;
            this.method = method;
            this.args = args;
            exception = null;
            retVal = null;
            this.synchronous = synchronous;
            //this.executionContext = executionContext;
            this.Priority = priority;
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (resetEvent == null)
                {
                    lock (invokeSyncObject)
                    {
                        if (resetEvent == null)
                        {
                            resetEvent = new ManualResetEvent(initialState: false);
                            if (isCompleted)
                            {
                                resetEvent.Set();
                            }
                        }
                    }
                }

                return resetEvent;
            }
        }
        public bool CompletedSynchronously
        {
            get
            {
                if (isCompleted && synchronous)
                {
                    return true;
                }

                return false;
            }
        }


        internal void Complete()
        {
            lock (invokeSyncObject)
            {
                isCompleted = true;
                if (resetEvent != null)
                {
                    resetEvent.Set();
                }
            }
        }
        ~RPAThreadMethodTask()
        {
            if (resetEvent != null)
            {
                resetEvent.Close();
            }
        }
    }
}

