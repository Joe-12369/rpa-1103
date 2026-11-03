using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading;
using static System.Reactive.Linq.Observable;
using II.RPA.Core.Provider.ScreenRecordProvider.FFmpeg;
using IPA.Ubases.Scheduling;
using IPA.Ubases.States;
using II.RPA.Core.Log;

namespace II.RPA.Core.Provider.ScreenRecordProvider
{
    public enum FFmpegState
    {
        Init,
        Processing,
        Stopping,
        Exited
    }

    //public enum FFmpegJob
    //{
    //    Start
    //}

    public class FFmpegWrapper : CtsControllable, IDisposable
    {
        private readonly ILogger logger;
        private readonly ScreenRecorder screenRecorder;
        private VideoOptions videoOptions;

        private ReactiveValue<FFmpegState> rxState;
        // private Subject<FFmpegJob> jobs;
        private FFmpegCLIManager cli;
        private CancellationTokenSource finalCts;

        public DateTime? StartAt { get; private set; }
        public DateTime? StopAt { get; private set; }
        public string Name { get; set; }
        public Exception Error { get; set; }
        public string FileName => $"{Name}.{videoOptions.ExtName}";
        public string FullFileName => Path.Combine(videoOptions.TmpDir, $"{Name}.{videoOptions.ExtName}");
        public FFmpegState StateValue => rxState.Content;
        public IObservable<FFmpegState> State => rxState.ContentObs.Value;
        public IObservable<FFmpegState> Exited => State.FirstOrDefaultAsync(s => s > FFmpegState.Stopping);


        public FFmpegWrapper(ScreenRecorder screenRecorder, ILogger logger, VideoOptions videoOptions)
        {
            this.logger = logger;
            this.screenRecorder = screenRecorder;
            this.videoOptions = videoOptions;

            this.rxState = new ReactiveValue<FFmpegState>();
            // this.jobs = new Subject<FFmpegJob>();

            this.Name = Guid.NewGuid().ToString();
            this.cli = new FFmpegCLIManager(logger, this.videoOptions.FFmpegPath);
            this.finalCts = new CancellationTokenSource();

            this.rxState.Content = FFmpegState.Init;
        }

        public override void Restart()
        {
            throw new NotSupportedException();
        }

        public override void Setup(CancellationTokenSource cts)
        {
            //new Subject<FFmpegJob>()
            //    .ObserveOn(NewThreadScheduler.Default)
            //    .Subscribe(job => 
            //    {
            //        switch (job)
            //        {
            //            case FFmpegJob.Start:
            //                break;
            //            default:
            //                break;
            //        }
            //    }, cts.Token);
            NewThreadScheduler.Default
                .Schedule(() =>
                {
                    try
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        StartAt = DateTime.Now;
                        this.rxState.Content = FFmpegState.Processing;
                        Directory.CreateDirectory(this.videoOptions.TmpDir);
                        var command = this.videoOptions.PieceOptions(Name).GetFFmpegCommands();
                        this.logger.LogInfo($"Run ffmpeg with: {command}");
                        cli.Run(command);
                    }
                    catch (Exception e)
                    {
                        Error = e;
                    }
                    finally
                    {
                        StopAt = DateTime.Now;
                        this.rxState.Content = FFmpegState.Exited;
                    }
                });

            this.rxState.ContentObs.Value
                .FirstOrDefaultAsync(state => state == FFmpegState.Stopping)
                .Subscribe(state =>
                {
                    cli.Stop();
                }, finalCts.Token);
            this.screenRecorder.StateObs
                    .FirstOrDefaultAsync(state => state == ScreenRecordState.Done)
                    .Subscribe(__ =>
                    {
                        if (this.screenRecorder.CutSeconds == 0)
                        {
                            try
                            {
                                File.Delete(this.videoOptions.FileFullName);
                            }
                            catch
                            { }
                        }
                        //else if (this.screenRecorder.CutSeconds != int.MaxValue)
                        //{
                        //    var command = this.videoOptions.PieceOptions(Name).GetCutFFmpegArgs(this.screenRecorder.CutSeconds, this.videoOptions.FileFullName);
                        //    if (!string.IsNullOrEmpty(command))
                        //    {
                        //        try
                        //        {
                        //            var targetFilePath = this.videoOptions.FileFullName;
                        //            var parentPath = Path.GetDirectoryName(targetFilePath);
                        //            var fileName = Path.GetFileNameWithoutExtension(targetFilePath);
                        //            var sourceFilePath = Path.Combine(parentPath, $"{fileName}_cuttemp{Path.GetExtension(targetFilePath)}");
                        //            cli.CuttingVideo(command);
                        //            if (File.Exists(sourceFilePath))
                        //            {
                        //                File.Delete(targetFilePath);
                        //                File.Move(sourceFilePath, targetFilePath);
                        //            }
                        //        }
                        //        catch
                        //        { }
                        //    }
                        //}
                    });

            cts.Token.Register(() =>
            {
                if (this.rxState.Content < FFmpegState.Stopping)
                {
                    this.rxState.Content = FFmpegState.Stopping;
                }
            });
        }

        ~FFmpegWrapper()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            Stop();
            if (finalCts != null && !finalCts.IsCancellationRequested)
            {
                finalCts.Cancel();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
