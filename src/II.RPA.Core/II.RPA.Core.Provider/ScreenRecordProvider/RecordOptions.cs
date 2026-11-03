using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Core.Provider.ScreenRecordProvider.FFmpeg;

namespace II.RPA.Core.Provider.ScreenRecordProvider
{
    public class VideoOptions
    {
        public string Id { get; set; }
        public string Directory { get; set; }
        public string FileName { get; set; }
        public VideoQuality Quality { get; set; }
        public string FFmpegPath { get; set; }
        public string FileFullName => Path.Combine(Directory, FileName);
        public string ExtName => Path.GetExtension(FileName).Replace(".", "");
        public string TmpDir => Path.Combine(Directory, Path.GetFileNameWithoutExtension(FileName));

        public ScreencastOptions PieceOptions(string name)
        {
            var options = new ScreencastOptions
            {
                FPS = 15,
                DrawCursor = true,
                IsLossless = false,
                IsRecording = true,
                Level = Quality,
                OutputPath = Path.Combine(TmpDir, $"{name}.{ExtName}"),
                FFmpeg = new FFmpegOptions
                {
                    CLIPath = FFmpegPath,
                    IsFullWindow = true,
                    IsFullSize = true
                }
            };

            switch (options.Level)
            {
                case VideoQuality.Low:
                    options.FFmpeg.IsFullSize = false;
                    options.FFmpeg.x264_CRF = 40;
                    options.FFmpeg.x264_Preset = FFmpegPreset.superfast;
                    break;
                case VideoQuality.Middle:
                    options.FFmpeg.x264_CRF = 45;
                    options.FFmpeg.x264_Preset = FFmpegPreset.ultrafast;
                    break;
                case VideoQuality.High:
                    options.FFmpeg.x264_CRF = 28;
                    options.FFmpeg.x264_Preset = FFmpegPreset.ultrafast;
                    break;
                case VideoQuality.ExHigh:
                    options.FFmpeg.x264_CRF = 15;
                    options.FFmpeg.x264_Preset = FFmpegPreset.ultrafast;
                    break;
            }

            return options;
        }
    }
}
