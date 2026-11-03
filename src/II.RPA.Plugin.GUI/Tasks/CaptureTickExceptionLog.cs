using System;
using II.RPA.Plugin.GUI.Tasks;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI
{
    /// <summary>
    /// 拾取 tick 内异常的日志策略。
    /// “用户停止拾取”会以 <see cref="CaptureTaskStopedException"/> 的形式从 tick 里抛出，
    /// 这属于正常控制流（外层 <see cref="CaptureTask"/> 会继续处理），不应记录为错误。
    /// </summary>
    internal static class CaptureTickExceptionLog
    {
        internal static void Log(ILogger logger, Exception exception, string errorMessage)
        {
            if (exception is CaptureTaskStopedException)
            {
                //外层 CaptureTask 会再记录一条“拾取任务已停止”的信息日志，这里只留调试痕迹
                logger.LogDebug(exception.Message);
                return;
            }

            LoggingExtensions.LogError(logger, exception, errorMessage);
        }
    }
}
