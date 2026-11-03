using System;
using II.RPA.Plugin.GUI.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace II.RPA.Plugin.GUI.Tests
{
    public class CaptureTickExceptionLogTests
    {
        [Fact]
        public void Log_WhenCaptureTaskStoped_DoesNotWriteError()
        {
            var logger = new RecordingLogger();
            var exception = new CaptureTaskStopedException("拾取任务：数据采集普通元素拾取任务v2， 已停止");

            CaptureTickExceptionLog.Log(logger, exception, "拾取程序发生了异常");

            Assert.DoesNotContain(logger.Records, record => record.Level == LogLevel.Error);
        }

        [Fact]
        public void Log_WhenUnexpectedException_WritesErrorWithOriginalException()
        {
            var logger = new RecordingLogger();
            var exception = new InvalidOperationException("boom");

            CaptureTickExceptionLog.Log(logger, exception, "拾取程序发生了异常");

            var record = Assert.Single(logger.Records);
            Assert.Equal(LogLevel.Error, record.Level);
            Assert.Same(exception, record.Exception);
            Assert.Equal("拾取程序发生了异常", record.Message);
        }
    }
}
