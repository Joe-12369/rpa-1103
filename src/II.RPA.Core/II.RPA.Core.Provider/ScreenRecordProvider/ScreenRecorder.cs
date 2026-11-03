using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using II.RPA.Core.Log;
using II.RPA.Core.Provider.ScreenRecordProvider.FFmpeg;
using II.RPA.Core.Provider.Utility;
using IPA.Ubases.Scheduling;
using IPA.Ubases.States;
using Newtonsoft.Json.Linq;

namespace II.RPA.Core.Provider.ScreenRecordProvider
{
    public enum ScreenRecordAction
    {
        Init,
        Start,
        Stop
    }

    public enum ScreenRecordState
    {
        Init,
        Recording,
        Stopping,
        Stopped,
        Finished,
        Done
    }

    public class ScreenRecorder : CtsControllable, IDisposable
    {
        private readonly ILogger logger;
        private readonly VideoOptions videoOptions;

        private volatile InterlockedEnum<ScreenRecordAction> action;
        private readonly ReactiveValue<ScreenRecordState> rxState;
        private readonly BlockingCollection<FFmpegWrapper> wrappers;


        private FFmpegWrapper currentWrapper;

        public DateTime? StartAt { get; private set; }
        public DateTime? StopAt { get; private set; }
        // 忽略小于该时间的时间片, 不生成黑屏
        public double IgnoreLostTime { get; set; } = 2000;

        public string Id => videoOptions.Id;
        public ScreenRecordState Status => rxState.Content;
        public VideoOptions Options => videoOptions;
        public IObservable<ScreenRecordState> StateObs => rxState.ContentObs.Value;
        public bool IsWorking => Status > ScreenRecordState.Init && Status < ScreenRecordState.Done;
        public int CutSeconds { get; set; } = -1;

        public ScreenRecorder(
            ILogger logger,
            VideoOptions videoOptions)
        {
            this.logger = logger;
            this.videoOptions = videoOptions;

            this.action = new InterlockedEnum<ScreenRecordAction>(ScreenRecordAction.Init);
            this.rxState = new ReactiveValue<ScreenRecordState>();
            this.wrappers = new BlockingCollection<FFmpegWrapper>();

            this.rxState.Content = ScreenRecordState.Init;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void HandleStatus(ScreenRecordState state, CancellationToken token)
        {
            switch (state)
            {
                case ScreenRecordState.Recording:
                    StartWrapper(token);
                    break;
                case ScreenRecordState.Stopping:
                    this.wrappers.CompleteAdding();
                    currentWrapper.Stop();
                    CheckWrappers(token);
                    break;
                case ScreenRecordState.Finished:
                    GenerateVideo();
                    break;
            }
        }

        private void CheckWrappers(CancellationToken token)
        {
            if (wrappers.ToList().All(w => w.StateValue == FFmpegState.Exited))
            {
                rxState.Content = ScreenRecordState.Finished;
            }
        }

        private void StartWrapper(CancellationToken token)
        {
            currentWrapper = new FFmpegWrapper(this, this.logger, videoOptions);
            if (!wrappers.TryAdd(currentWrapper))
            {
                return;
            }

            currentWrapper.Exited.Subscribe(_ =>
            {
                Observable.Return(0).Delay(TimeSpan.FromSeconds(5)).FirstOrDefaultAsync().Subscribe(__ =>
                {
                    switch (action.Get())
                    {
                        case ScreenRecordAction.Start:
                            StartWrapper(token);
                            break;
                        case ScreenRecordAction.Stop:
                            CheckWrappers(token);
                            break;
                    }
                });
            });
            currentWrapper.Start();
        }

        public override void Start()
        {
            var currentAction = action.Get();
            if (currentAction == ScreenRecordAction.Init)
            {
                if (action.ChangeTo(currentAction, ScreenRecordAction.Start))
                {
                    base.Start();
                    StartAt = DateTime.Now;
                    rxState.Content = ScreenRecordState.Recording;
                }
            }
        }

        public override void Stop()
        {
            var currentAction = action.Get();
            if (currentAction < ScreenRecordAction.Stop)
            {
                if (action.ChangeTo(currentAction, ScreenRecordAction.Stop))
                {
                    StopAt = DateTime.Now;
                    rxState.Content = ScreenRecordState.Stopping;
                }
            }
        }

        public override void Setup(CancellationTokenSource cts)
        {
            this.rxState.ContentObs.Value
                .Subscribe(state =>
                {
                    HandleStatus(state, cts.Token);
                }, cts.Token);

            //Task.Factory.StartNew(async () => { 
            //    while(cts.Token.IsCancellationRequested)
            //    {
            //        start ffmpeg

            //        recording...
            //        wait ffmpeg...

            //        iasyncenumerable
            //        rx

            //        /*await foreach(var i in items)
            //        {
            //            Observable.Range(1,10).
            //        }*/


            //        react to ffmpeg
            //    }
            //});
        }


        private void GenerateVideo()
        {
            try
            {
                logger.LogInfo($"Merge video. Start time [{StartAt.Value}], End time [{StopAt.Value}].");
                var previousTime = StartAt.Value;
                var validHelper = wrappers.Where(w => File.Exists(w.FullFileName)).ToList();
                var parts = validHelper.SelectMany(h =>
                {
                    logger.LogInfo($"Handle span after file: ${h.FileName}. Start time [{h.StartAt.Value}], End time [{h.StopAt.Value}].");
                    var spans = new List<string>() { h.FullFileName };
                    var spanDuration = (h.StartAt.Value - previousTime).TotalMilliseconds;
                    if (spanDuration > IgnoreLostTime)
                    {
                        var emptyPath = CreateEmptyVideo((h.StartAt.Value - previousTime).TotalSeconds);
                        if (emptyPath != null)
                        {
                            spans.Insert(0, emptyPath);
                        }
                        else
                        {
                            logger.LogInfo($"Create empty span fail.");
                        }
                    }
                    else
                    {
                        logger.LogInfo($"Span duration {spanDuration} less then {IgnoreLostTime}, skip");
                    }
                    previousTime = h.StopAt.Value;
                    return spans;
                }).ToList();
                var lastPart = validHelper.LastOrDefault();
                if (lastPart != null)
                {
                    if ((StopAt.Value - lastPart.StopAt.Value).TotalMilliseconds > IgnoreLostTime)
                    {
                        var emptyPath = CreateEmptyVideo((StopAt.Value - lastPart.StopAt.Value).TotalSeconds);
                        parts.Add(emptyPath);
                    }
                }

                if (parts.Count() > 0)
                {
                    var concatFile = Path.Combine(videoOptions.TmpDir, "concat.txt");
                    File.WriteAllLines(concatFile, parts.Select(p => $"file '{p}'"));

                    var targetFilePath = this.videoOptions.FileFullName;
                    var parentPath = Path.GetDirectoryName(targetFilePath);
                    var fileName = Path.GetFileNameWithoutExtension(targetFilePath);
                    var sourceFilePath = Path.Combine(parentPath, $"{fileName}_cuttemp{Path.GetExtension(targetFilePath)}");
                    var ffmpegCLIManager = new FFmpegCLIManager(this.logger, this.videoOptions.FFmpegPath);
                    ffmpegCLIManager.ConcatVideos(concatFile, sourceFilePath);

                    try
                    {
                        if (CutSeconds != 0 && CutSeconds != int.MaxValue)
                        {
                            ffmpegCLIManager.CuttingVideo(CutSeconds, sourceFilePath, targetFilePath);
                        }
                        else
                        {
                            File.Move(sourceFilePath, targetFilePath);
                        }

                        File.Delete(sourceFilePath);
                    }
                    catch(Exception ex)
                    {
                        logger.LogWarning(ex.Message);
                    }
                }
                else
                {
                    logger.LogInfo("No valid video parts. Create empty");
                    CreateEmptyVideo((StopAt.Value - StartAt.Value).TotalSeconds, this.videoOptions.FileFullName);
                }
            }
            catch (Exception e)
            {
                logger.LogInfo(e.Message);
            }
            finally
            {
                rxState.Content = ScreenRecordState.Done;
                // Clean all files
                try
                {
                    Directory.Delete(videoOptions.TmpDir, true);
                }
                catch { }
            }
        }

        private string CreateEmptyVideo(double duration, string emptyPath = null)
        {
            emptyPath = emptyPath ?? Path.Combine(videoOptions.TmpDir, $"{Guid.NewGuid()}.mp4");

            logger.LogInfo($"Create empty video: {emptyPath} duration {duration}.");

            if (new FFmpegCLIManager(this.logger, this.videoOptions.FFmpegPath).CreateEmptyVideo(emptyPath, Convert.ToInt32(duration), 15))
            {
                return emptyPath;
            }

            return null;
        }

        public override void Restart()
        {
            throw new NotSupportedException();
        }
    }
}
