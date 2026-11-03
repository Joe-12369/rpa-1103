using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenCapture
{
    public class ScreenRecorder : IDisposable
    {
        public bool IsRecording { get; private set; }

        public int FPS
        {
            get
            {
                return fps;
            }
            set
            {
                if (!IsRecording)
                {
                    fps = value;
                    UpdateInfo();
                }
            }
        }

        public float DurationSeconds
        {
            get
            {
                return durationSeconds;
            }
            set
            {
                if (!IsRecording)
                {
                    durationSeconds = value;
                    UpdateInfo();
                }
            }
        }

        public Rectangle CaptureRectangle
        {
            get
            {
                return captureRectangle;
            }
            private set
            {
                if (!IsRecording)
                {
                    captureRectangle = value;
                }
            }
        }


        public ScreenRecordOutput OutputType { get; private set; }

        public ScreencastOptions Options { get; set; }

        public event Action RecordingStarted;

        public delegate void ProgressEventHandler(int progress);
        public event ProgressEventHandler EncodingProgressChanged;

        private int fps, delay, frameCount, previousProgress;
        private float durationSeconds;
        private Rectangle captureRectangle;
        private FFmpegCLIManager ffmpeg;

        public ScreenRecorder(ScreencastOptions options)
        {
            if (string.IsNullOrEmpty(options.OutputPath))
            {
                throw new Exception("Screen recorder cache path is empty.");
            }

            FPS = options.FPS;
            DurationSeconds = options.Duration;

            Options = options;

            Helpers.CreateDirectoryFromFilePath(Options.OutputPath);
            ffmpeg = new FFmpegCLIManager(Options.FFmpeg.FFmpegPath);
            ffmpeg.EncodeStarted += OnRecordingStarted;
            ffmpeg.EncodeProgressChanged += OnEncodingProgressChanged;

        }

        private void UpdateInfo()
        {
            delay = 1000 / fps;
            frameCount = (int)(fps * durationSeconds);
        }

        public bool StartRecording()
        {
            var result = false;
            if (!IsRecording)
            {
                IsRecording = true;
                result = ffmpeg.Run(Options.GetFFmpegCommands());
            }

            IsRecording = false;
            return result;
        }

        public void StopRecording()
        {
            if (ffmpeg != null)
            {
                ffmpeg.Close();
            }
        }

        public void KillRecording()
        {
            ffmpeg?.Kill();
        }

        public bool FFmpegEncodeVideo(string input, string output)
        {
            Helpers.CreateDirectoryFromFilePath(output);

            Options.IsRecording = false;
            Options.IsLossless = false;
            Options.InputPath = input;
            Options.OutputPath = output;

            try
            {
                ffmpeg.TrackEncodeProgress = true;

                return ffmpeg.Run(Options.GetFFmpegCommands());
            }
            finally
            {
                ffmpeg.TrackEncodeProgress = false;
            }
        }

        public bool FFmpegEncodeAsGIF(string input, string output)
        {
            Helpers.CreateDirectoryFromFilePath(output);

            try
            {
                ffmpeg.TrackEncodeProgress = true;

                // https://ffmpeg.org/ffmpeg-filters.html#palettegen-1
                // https://ffmpeg.org/ffmpeg-filters.html#paletteuse
                return ffmpeg.Run($"-i \"{input}\" -lavfi \"palettegen=stats_mode={Options.FFmpeg.GIFStatsMode}[palette]," +
                    $"[0:v][palette]paletteuse=dither={Options.FFmpeg.GIFDither}" +
                    $"{(Options.FFmpeg.GIFDither == FFmpegPaletteUseDither.bayer ? ":bayer_scale=" + Options.FFmpeg.GIFBayerScale : "")}\"" +
                    $" -y \"{output}\"");
            }
            finally
            {
                ffmpeg.TrackEncodeProgress = false;
            }
        }

        protected void OnRecordingStarted()
        {
            RecordingStarted?.Invoke();
        }

        protected void OnEncodingProgressChanged(float progress)
        {
            int currentProgress = (int)progress;

            if (EncodingProgressChanged != null && currentProgress != previousProgress)
            {
                EncodingProgressChanged(currentProgress);
                previousProgress = currentProgress;
            }
        }

        public void Dispose()
        {
            if (ffmpeg != null)
            {
                ffmpeg.Dispose();
            }
        }
    }
}
