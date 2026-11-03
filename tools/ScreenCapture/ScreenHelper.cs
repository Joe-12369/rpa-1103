using II.RPA.Observable.States;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenCapture
{
    public class ScreenHelper
    {
        private static ScreenHelper instance;
        private readonly string _ffmpegExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        static object lockThis = new object();
        public ScreenHelper() { }
        public static ScreenHelper GetSingleton()
        {
            if (instance == null)
            {
                lock (lockThis)
                {
                    if (instance == null)
                    {
                        instance = new ScreenHelper();
                        GCHandle hander = GCHandle.Alloc(instance);
                        var pin = GCHandle.ToIntPtr(hander);
                        DebugHelper.WriteLine("实例地址：" + pin.ToString());
                    }
                }
            }

            return instance;
        }

        private static void RemoveWrapper(object sender, RecorderState e)
        {
            if (e == RecorderState.Done)
            {
                var wrapper = sender as RecorderWrapper;
                if (wrapper != null)
                {
                    wrapper.StateChanged -= RemoveWrapper;
                    m_wrappers.TryRemove(wrapper.Command.GetHashCode(), out var _);
                }
            }
        }

        private static readonly ConcurrentDictionary<int, RecorderWrapper> m_wrappers = new ConcurrentDictionary<int, RecorderWrapper>();
        public static RecorderWrapper GetRecorderWrapper(string command)
        {
            var code = command.GetHashCode();
            var recorderWrapper = m_wrappers.GetOrAdd(code, _ =>
            {
                var wrapper = new RecorderWrapper(command);
                wrapper.StateChanged += RemoveWrapper;
                return wrapper;
            });
            m_wrappers.Keys.ToList().Where(k => k != code).ToList().ForEach(k =>
            {
                if(m_wrappers.TryGetValue(k, out var wrapper))
                {
                    wrapper.Stop();
                }
            });
            return recorderWrapper;
        }

        public static void StopWrapper()
        {
            m_wrappers.Keys.ToList().ForEach(k =>
            {
                if (m_wrappers.TryGetValue(k, out var wrapper))
                {
                    wrapper.Stop();
                }
            });
        }


        private ScreenRecorder screenRecorder;
        public bool DoStartScreenCapture(string command)
        {
            DoStopScreenCapture();
            // KillFFmpeg();
            ScreencastOptions options = JsonConvert.DeserializeObject<ScreencastOptions>(command);

            options.FPS = 15;
            options.DrawCursor = true;
            options.IsLossless = false;
            options.IsRecording = true;

            var rect = new Rectangle(new Point(0,0),User32Ex.DESKTOP); 
            if (rect.Width % 2 != 0)
            {
                rect.Width -= 1;
            }
            if (rect.Height % 2 != 0)
            {
                rect.Height -= 1;
            }
            options.CaptureArea = rect;

            FFmpegOptions ffMpegOptions = new FFmpegOptions(_ffmpegExePath);

            ffMpegOptions.IsFullWindow = true;
            ffMpegOptions.IsFullSize = true;
            switch (options.Level)
            {
                case VideoLevel.None:
                case VideoLevel.Low:
                    ffMpegOptions.IsFullSize = false;
                    ffMpegOptions.x264_CRF = 40;
                    ffMpegOptions.x264_Preset = FFmpegPreset.superfast;
                    break;
                case VideoLevel.Middle:
                    ffMpegOptions.x264_CRF = 45;
                    ffMpegOptions.x264_Preset = FFmpegPreset.ultrafast;
                    break;
                case VideoLevel.High:
                    ffMpegOptions.x264_CRF = 28;
                    ffMpegOptions.x264_Preset = FFmpegPreset.ultrafast;
                    break;
            }
            options.FFmpeg = ffMpegOptions;

            screenRecorder = new ScreenRecorder(options);
            return screenRecorder.StartRecording();
        }

        private void KillFFmpeg()
        {
            Process[] app = Process.GetProcessesByName("ffmpeg");
            if (app != null)
            {
                DebugHelper.WriteLine($"启动录屏时尝试强制结束前次录屏！");
                Thread.Sleep(2000);
                app = Process.GetProcessesByName("ffmpeg");
                if (app != null)
                { 
                    DebugHelper.WriteLine($"启动录屏时强制结束前次录屏！");
                    foreach (Process process in app)
                    {
                        process.Kill();
                    }
                }
            }
        }

        public void DoStopScreenCapture()
        {
            screenRecorder?.StopRecording();
        }

        public void KillRecording()
        {
            screenRecorder?.KillRecording();
        }

        public bool CreateEmptyVideo(string path, int duration, int bitrate) => new FFmpegCLIManager(_ffmpegExePath).CreateEmptyVideo(path, duration, bitrate);

        public bool ConcatVideos(string listFile, string output) => new FFmpegCLIManager(_ffmpegExePath).ConcatVideos(listFile, output);

        public void CutVideo(string command)
        {
            CutOption cutOption;
            try
            {
                cutOption = JsonConvert.DeserializeObject<CutOption>(command);
            }
            catch (Exception ex)
            {
                throw new Exception("截取参数有误" + ex.Message);
            }
            FFmpegCLIManager FFmpegOptions = new FFmpegCLIManager(_ffmpegExePath);
            FFmpegOptions.Run(cutOption.GetCutCommands());
        }

        public void LastCutVideo(string command)
        {
            LastCutOption lastCutOption;
            try
            {
                lastCutOption = JsonConvert.DeserializeObject<LastCutOption>(command);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"截取参数有误！" + ex.Message);
                return;
            }
            if (!File.Exists(lastCutOption.InputFile))
            {
                DebugHelper.WriteLine($"目标视频文件不存在" + lastCutOption.InputFile);
                return;
            }
            FFmpegCLIManager FFmpegOptions = new FFmpegCLIManager(_ffmpegExePath);
            VideoInfo videoInfo = FFmpegOptions.GetVideoInfo(lastCutOption.InputFile);
            lastCutOption.Startime = videoInfo.Duration.Add(new TimeSpan(0, 0, -lastCutOption.Duration));
            FFmpegOptions.Run(lastCutOption.GetCutCommands());
        }

        public void MergeVideo(string command)
        {
            MergeOption mergeOption;
            try
            {
                mergeOption = JsonConvert.DeserializeObject<MergeOption>(command);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"合并参数有误：" + ex.Message);
                return;
            }
            FFmpegCLIManager FFmpegOptions = new FFmpegCLIManager(_ffmpegExePath);
            string[] videoS = mergeOption.InputFile.Split(',');
            foreach (var video in videoS)
            {
                FFmpegOptions.Run(mergeOption.GetTsCommands(video));
            }
            FFmpegOptions.Run(mergeOption.GetCutCommands());
            foreach (var video in videoS)
            {
                mergeOption.ClearTs(video);
            }
        }
    }
}
