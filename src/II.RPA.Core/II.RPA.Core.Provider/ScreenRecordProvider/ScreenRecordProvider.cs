using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Kernel;
using II.RPA.Core.Log;
using II.RPA.Core.Provider.Utility;
using IPA.Ubases.Scheduling;

namespace II.RPA.Core.Provider.ScreenRecordProvider
{
    public class ScreenRecordProvider : IDisposable
    {
        private readonly ILogger logger;

        // 尝试重启ffmpeg间隔
        public int GuardInterval { get; set; } = 5000;
        // 发送结束信号后允许ffmpeg主动退出的最大时间, 超过该时间发送kill
        public double FFmpegTimeout { get; set; } = 60000;

        private readonly ConcurrentDictionary<string, ScreenRecorder> recorders;

        public ScreenRecordProvider(ILogger aLogger)
        {
            this.logger = aLogger;
            this.recorders = new ConcurrentDictionary<string, ScreenRecorder>();
        }

        public ScreenRecorder Get(string id) => recorders.GetValue(id);

        public IEnumerable<ScreenRecorder> GetAll() => recorders.Values;

        public ScreenRecorder Start(VideoOptions options)
        {
            var recorder = recorders.GetOrAdd(options.Id, _ =>
            {
                var sr = new ScreenRecorder(logger, options);
                sr.StateObs
                    .FirstOrDefaultAsync(state => state > ScreenRecordState.Finished)
                    .Subscribe(__ =>
                    {
                        recorders.Remove(options.Id, 1000);
                    });
                return sr;
            });
            recorder.Start();
            return recorder;
        }

        public ScreenRecorder Stop(string id, int cutSeconds = -1)
        {
            var recorder = recorders.GetValue(id);
            if (recorder != null) 
            {
                recorder.CutSeconds = cutSeconds;
                recorder.Stop();
            }
            return recorder;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
