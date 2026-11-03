using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using II.RPA.Observable.States;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScreenCapture
{
    public enum RecorderState
    {
        Ready,
        Recording,
        Stoped,
        Finished,
        Done
    }

    public class RecorderWrapper : IDisposable
    {
        private const int KEEP_RECORDING = 5000;
        private const int GUARD_INTERVAL = 5000;
        private const double FFMPEG_TIMEOUT = 60000;
        private const double IGNORE_TIME = 2000;

        private const string TASK_KEY = "CurrentTask";

        private volatile RecorderState m_state = RecorderState.Ready;
        private volatile string m_command;
        private volatile ScreencastOptions m_options;

        private readonly ConcurrentDictionary<string, ScreenHelperWrapper> m_currentTask = new ConcurrentDictionary<string, ScreenHelperWrapper>();

        private readonly BlockingCollection<ScreenHelperWrapper> m_helpers = new BlockingCollection<ScreenHelperWrapper>();
        private readonly System.Timers.Timer m_guardimer = new System.Timers.Timer(GUARD_INTERVAL)
        {
            AutoReset = true
        };

        public event EventHandler<RecorderState> StateChanged;

        private IEnumerable<ScreenHelperWrapper> helpers { get => m_helpers.ToList().Where(v => v.State != ScreenHelperState.Abort); } 
        public RecorderState State { 
            get
            {
                return m_state;
            }
            private set 
            {
                if (value > m_state)
                {
                    m_state = value;
                    StateChanged(this, value);
                }
            }
        } 
        public DateTime? StartAt { get; private set; }
        public DateTime? StopAt { get; private set; }
        public string Command { get => m_command; }


        private static OsStateCollector m_collector = new OsStateCollector();
        static RecorderWrapper()
        {
            m_collector.Start();
        }

        private void GuardHandle(object sender, ElapsedEventArgs e)
        {
            switch (State)
            {
                case RecorderState.Recording:
                    if (!m_collector.State.IsScreenLocked && m_collector.State.OsSession.ConnectionState == II.RPA.Observable.States.OsStates.ConnectionState.Active)
                    {
                        RecordVideo();
                    }
                    break;
                case RecorderState.Stoped:
                    if (StopAt != null)
                    {
                        if ((DateTime.Now - StopAt.Value).TotalMilliseconds > FFMPEG_TIMEOUT)
                        {
                            KillAllHelper();
                        }
                        else
                        {
                            WaitAllHelper();
                        }
                    }
                    break;
            }
        }

        public RecorderWrapper(string command)
        {
            m_command = command;
            m_options = JsonConvert.DeserializeObject<ScreencastOptions>(m_command);

            StateChanged += HandleState;
            m_guardimer.Elapsed += GuardHandle;
        }

        private void HandleState(object sender, RecorderState e)
        {
            switch (e)
            {
                case RecorderState.Recording:
                    StartAt = DateTime.Now;
                    m_guardimer.Enabled = true;
                    RecordVideo();
                    break;
                case RecorderState.Stoped:
                    Task.Delay(KEEP_RECORDING).ContinueWith(_ =>
                    {
                        m_helpers.CompleteAdding();
                        StopAt = DateTime.Now;
                        WaitAllHelper();
                    });
                    break;
                case RecorderState.Finished:
                    m_guardimer.Enabled = false;
                    GenerateVideo();
                    break;
            }
        }

        private void WaitAllHelper()
        {
            helpers.Where(v => v.State < ScreenHelperState.Stop).ToList().ForEach(v => v.DoStopScreenCapture());
            // 优化结束速度
            Thread.Sleep(1000);

            if (helpers.All(v => v.State >= ScreenHelperState.Finished))
            {
                State = RecorderState.Finished;
            }
        }

        private void KillAllHelper()
        {
            helpers.Where(v => v.State < ScreenHelperState.Stop).ToList().ForEach(v => v.KillRecording());
            Thread.Sleep(1000);

            if (helpers.All(v => v.State >= ScreenHelperState.Finished))
            {
                State = RecorderState.Finished;
            }
        }

        private void RecordVideo()
        {
            var taskWrapper = m_currentTask.GetOrAdd(TASK_KEY, _ =>
            {
                var wrapper = new ScreenHelperWrapper(m_options);
                try
                {
                    if (m_helpers.TryAdd(wrapper))
                    {
                        DebugHelper.WriteLine($"New ScreenHelperWrapper created.");
                        wrapper.Job.ContinueWith(__ =>
                        {
                            DebugHelper.WriteLine($"Clear ScreenHelperWrapper after done.");
                            m_currentTask.TryRemove(TASK_KEY, out var ___);
                        });
                    }
                    else
                    {
                        DebugHelper.WriteLine($"New ScreenHelperWrapper abort.");
                        wrapper.Abort();
                    }
                }
                catch
                {
                    DebugHelper.WriteLine($"New ScreenHelperWrapper exception.");
                    wrapper.Abort();
                }
                return wrapper;
            });
            taskWrapper.DoStartScreenCapture();
            if (taskWrapper.State >= ScreenHelperState.Finished)
            {
                DebugHelper.WriteLine($"Clear ScreenHelperWrapper when state changes.");
                m_currentTask.TryRemove(TASK_KEY, out var _);
            }
        }

        private void GenerateVideo()
        {
            try
            {
                DebugHelper.WriteLine($"Merge video. Start time [{StartAt.Value}], End time [{StopAt.Value}].");
                var previousTime = StartAt.Value;
                var validHelper = m_helpers.Where(h => File.Exists(h.FileName)).ToList();
                var parts = validHelper.SelectMany(h =>
                {
                    DebugHelper.WriteLine($"Handle span after file: ${h.FileName}. Start time [{h.StartAt.Value}], End time [{h.EndAt.Value}].");
                    var spans = new List<string>() { h.FileName };
                    var spanDuration = (h.StartAt.Value - previousTime).TotalMilliseconds;
                    if (spanDuration > IGNORE_TIME)
                    {
                        var emptyPath = CreateEmptyVideo((h.StartAt.Value - previousTime).TotalSeconds);
                        if (emptyPath != null)
                        {
                            spans.Insert(0, emptyPath);
                        }
                        else
                        {
                            DebugHelper.WriteLine($"Create empty span fail.");
                        }
                    }
                    else
                    {
                        DebugHelper.WriteLine($"Span duration {spanDuration} less then {IGNORE_TIME}, skip");
                    }
                    previousTime = h.EndAt.Value;
                    return spans;
                }).ToList();
                var lastPart = validHelper.LastOrDefault();
                if (lastPart != null)
                {
                    if ((StopAt.Value - lastPart.EndAt.Value).TotalMilliseconds > IGNORE_TIME)
                    {
                        var emptyPath = CreateEmptyVideo((StopAt.Value - lastPart.EndAt.Value).TotalSeconds);
                        parts.Add(emptyPath);
                    }
                }

                if (parts.Count() > 0)
                {
                    var concatFile = Path.Combine(m_options.OutputPath, "concat.txt");
                    File.WriteAllLines(concatFile, parts.Select(p => $"file '{p}'"));
                    new ScreenHelper().ConcatVideos(concatFile, $"{m_options.OutputPath}.{m_options.FFmpeg.Extension}");
                }
                else
                {
                    DebugHelper.WriteLine("No valid video parts. Create empty");
                    CreateEmptyVideo((StopAt.Value - StartAt.Value).TotalSeconds, $"{m_options.OutputPath}.{m_options.FFmpeg.Extension}");
                }
            }
            catch (Exception e)
            {
                DebugHelper.WriteError(e.Message);
            }
            finally
            {
                State = RecorderState.Done;
                // Clean all files
                Directory.Delete(m_options.OutputPath, true);
            }
        }

        private string CreateEmptyVideo(double duration, string emptyPath = null)
        {
            emptyPath = emptyPath ?? Path.Combine(m_options.OutputPath, $"{Guid.NewGuid()}.mp4");

            DebugHelper.WriteLine($"Create empty video: {emptyPath} duration {duration}.");

            if (new ScreenHelper().CreateEmptyVideo(emptyPath, Convert.ToInt32(duration), 15))
            {
                return emptyPath;
            }

            return null;
        }

        public void Start()
        {
            State = RecorderState.Recording;
        }

        public void Stop()
        {
            State = RecorderState.Stoped;
        }

        public void Dispose()
        {
        }
    }
}
