using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.Core.Commons
{
    public class RPAUtils
    {
        private static ILogger logger = Logger.OfContext();
        #region [重试]
        public static bool Retry(Func<bool> func, CancellationToken token)
        {
            return Retry(func, -1, token);
        }
        public static bool Retry(Func<bool> func, CancellationToken token, int interval)
        {
            return Retry(func, -1, token, interval);
        }
        public static bool Retry(Func<bool> func, int timeout, CancellationToken token)
        {
            return Retry(func, timeout, token, 200);
        }
        public static bool Retry(Func<bool> func, int timeout, CancellationToken token, int interval)
        {
            return Retry(func, token, timeout, -1, interval, 0);
        }
        public static bool Retry(Func<bool> func, int timeout)
        {
            return Retry(func, timeout, -1, 200, 0);
        }
        public static bool Retry(Func<bool> func, int timeout, int interval)
        {
            return Retry(func, timeout, -1, interval, 0);
        }
        /// <summary>
        /// 重试执行，至少执行一次
        /// </summary>
        /// <param name="func"></param>
        /// <param name="timeout">大于-1有效,0执行一次</param>
        /// <param name="interval"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public static bool Retry(Func<bool> func, int timeout, int interval, int delay)
        {
            return Retry(func, timeout, -1, interval, delay);
        }

        public static bool Retry(Func<bool> func, int timeout, int times, int interval, int delay)
        {
            return Retry(func, default, timeout, times, interval, delay);
        }

        /// <summary>
        /// 重试执行
        /// </summary>
        /// <param name="func">返回true终止重试</param>
        /// <param name="token">默认 default(CancellationToken)</param>
        /// <param name="timeout">大于-1有效,0执行一次</param>
        /// <param name="times">重试次数</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="delay">执行前延时</param>
        /// <returns></returns>
        public static bool Retry(Func<bool> func, CancellationToken token, int timeout, int times, int interval, int delay)
        {
            Stopwatch sw = Stopwatch.StartNew();
            if (delay > 0)
            {
                Task.Delay((int)delay).Wait();
            }
            try
            {
                do
                {
                    if (func())
                    {
                        return true;
                    }
                    times--;
                    if (times == 0)
                    {
                        break;
                    }
                    if (timeout < 0 && token == CancellationToken.None)
                    {
                        break;
                    }
                    if (timeout > -1 && sw.ElapsedMilliseconds + interval >= timeout)
                    {
                        break;
                    }
                    Task.Delay((int)interval).Wait();
                }
                while (!token.IsCancellationRequested);
                return false;
            }
            finally
            {
                sw.Stop();
                logger.LogDebug($"Retry {sw.ElapsedMilliseconds}");
            }
        }
        /// <summary>
        /// 重试等待有效值（非default）
        /// </summary>
        /// <param name="func">返回值不等于默认值时，终止重试</param>
        /// <param name="timeout">大于-1有效,0执行一次</param>
        /// <param name="interval">间隔时间</param>
        /// <returns></returns>
        public static T RetryWait<T>(Func<T> func, uint timeout, uint interval)
        {
            T ret = default(T);
            Retry(() =>
            {
                ret = func();
                if (ret == null)
                    return false;
                return !ret.Equals(default(T));
            }, (int)timeout, (int)interval);
            return ret;
        }
        #endregion

        #region [获取可能存在异常的数据]
        public static bool TryGet<T>(Func<T> func, out T retVal)
        {
            return TryGet<T>(func, default, out retVal);
        }
        public static bool TryGet<T>(Func<T> func, T defaultVal, out T retVal)
        {
            try
            {
                retVal = func();
                return true;
            }
            catch
            {
                retVal = defaultVal;
                return false;
            }
        }
        #endregion

        #region [通过SessionId确认进程]
        public static Process[] GetProcessesBySessionId(string processName)
        {
            try
            {
                int sid = Process.GetCurrentProcess().SessionId;
                return (from m in Process.GetProcessesByName(processName)
                        where m.SessionId == sid
                        select m).ToArray();
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
            return null;
        }
        #endregion

        #region [Task.Delay().Wait()]
        public static void Delay(int timeout)
        {
            Task.Delay(timeout).Wait();
        }
        #endregion

        #region 对比两个版本号
        /// <summary>
        /// 对比两个版本号的高低
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns> -1:version1 小于 version2, 0:version1 等于 version2, 1 version1 大于 version2 </returns>
        public static int CompareVersionNumber(string version1, string version2)
        {
            if (version1.Trim() == version2.Trim())
            {
                return 0;
            }
            var version1Numbers = Array.ConvertAll(version1.Split('.'), (s) => int.Parse(s));
            var version2Numbers = Array.ConvertAll(version2.Split('.'), (s) => int.Parse(s));
            if (version1Numbers.Length == version2Numbers.Length)
            {
                for (int i = 0; i < version1Numbers.Length; i++)
                {
                    if (version1Numbers[i] > version2Numbers[i])
                    {
                        break;
                    }
                    else if (version1Numbers[i] < version2Numbers[i])
                    {
                        return -1;
                    }
                    if (i == version1Numbers.Length - 1)
                    {
                        return -1;
                    }
                }
            }
            else if (version1Numbers.Length < version2Numbers.Length)
            {
                return -1;
            }
            return 1;
        }
        /// <summary>
        /// 是否在版本范围内
        /// </summary>
        /// <param name="version"></param>
        /// <param name="minVersion">大于等于minVersion</param>
        /// <param name="maxVersion">小于等于maxVersion</param>
        /// <returns></returns>
        public static bool IsVersionInRange(string version, string minVersion, string maxVersion)
        {
            // 首先检查版本是否大于等于最小版本
            bool isMinVersion = string.IsNullOrEmpty(minVersion) || CompareVersionNumber(minVersion, version) < 1;
            // 然后检查版本是否小于等于最大版本
            bool isMaxVersion = string.IsNullOrEmpty(maxVersion) || CompareVersionNumber(version, maxVersion) < 1;

            // 如果两个条件都满足，则版本在指定范围内
            return isMinVersion && isMaxVersion;
        }
        #endregion

        #region 异步执行并等待方法
        public static async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> asyncMethod, int timeout, Func<T> onTimeout = null)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task<T> methodTask = Task.Factory.StartNew(
                    async () => await asyncMethod(),
                    cts.Token,
                    TaskCreationOptions.LongRunning, // 使用 LongRunning 确保立即启动独立线程
                    TaskScheduler.Default
                ).Unwrap();
                Task delayTask = Task.Delay(timeout, cts.Token);

                Task completedTask = await Task.WhenAny(methodTask, delayTask);

                if (completedTask == methodTask)
                {
                    cts.Cancel(); // 取消延迟任务
                    return await methodTask; // 返回方法的结果
                }
                else
                {
                    if (onTimeout != null)
                    {
                        return onTimeout.Invoke();
                    }
                    return default(T);
                }
            }
        }

        // 泛型方法，支持无返回值的异步方法
        public static async Task ExecuteWithTimeoutAsync(Func<Task> asyncMethod, int timeout, Action onTimeout = null)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task methodTask = Task.Factory.StartNew(
                    async () => await asyncMethod(),
                    cts.Token,
                    TaskCreationOptions.LongRunning, // 使用 LongRunning 确保立即启动独立线程
                    TaskScheduler.Default
                ).Unwrap();
                Task delayTask = Task.Delay(timeout, cts.Token);
                Task completedTask = await Task.WhenAny(methodTask, delayTask);
                if (completedTask == methodTask)
                {
                    cts.Cancel(); // 取消延迟任务
                    await methodTask; // 确保方法执行完成
                }
                else
                {
                    onTimeout?.Invoke();
                }
            }
        }
        public static T ExecuteWithTimeout<T>(Func<T> syncMethod, int timeout, Func<T> onTimeout = null)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task<T> methodTask = Task.Factory.StartNew(
                     () => syncMethod(),
                    cts.Token,
                    TaskCreationOptions.LongRunning, // 使用 LongRunning 确保立即启动独立线程
                    TaskScheduler.Default
                );
                Task delayTask = Task.Delay(timeout, cts.Token);
                Task completedTask = Task.WhenAny(methodTask, delayTask).Result;

                if (completedTask == methodTask)
                {
                    cts.Cancel(); // 取消延迟任务
                    return methodTask.Result; // 返回方法的结果
                }
                else
                {
                    if (onTimeout != null)
                    {
                        return onTimeout.Invoke();
                    }
                    return default(T);
                }
            }
        }

        // 同步方法，支持执行无返回值的异步方法并设置超时
        public static void ExecuteWithTimeout(Action syncMethod, int timeout, Action onTimeout = null)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task methodTask = Task.Factory.StartNew(
                    syncMethod,
                    cts.Token,
                    TaskCreationOptions.LongRunning, // 使用 LongRunning 确保立即启动独立线程
                    TaskScheduler.Default
                );
                Task delayTask = Task.Delay(timeout, cts.Token);

                Task completedTask = Task.WhenAny(methodTask, delayTask).Result;

                if (completedTask == methodTask)
                {
                    cts.Cancel(); // 取消延迟任务
                    methodTask.Wait(); // 确保方法执行完成
                }
                else
                {
                    onTimeout?.Invoke();
                }
            }
        }

        #endregion
    }
}
