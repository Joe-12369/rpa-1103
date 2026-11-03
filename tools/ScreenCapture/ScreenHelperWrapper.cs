using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static System.Windows.Forms.AxHost;

namespace ScreenCapture
{
    public enum ScreenHelperState
    {
        Ready,
        Recording,
        Stop,
        Finished,
        Abort
    }

    public class ScreenHelperWrapper
    {
        private volatile ScreenHelperState m_state = ScreenHelperState.Ready;
        private ScreenHelper m_screenHelper;
        private string m_command;

        public ScreenHelperState State { get => m_state; }
        public DateTime? StartAt { get; private set; }
        public DateTime? EndAt { get; private set; }
        public Task Job { get; private set; }
        public bool Result { get; private set; } = false;
        public ScreencastOptions Options { get; private set; }
        public string Output { get; private set; }
        public string FileName { get => $"{Output}.{Options.FFmpeg.Extension}"; }

        public ScreenHelperWrapper(ScreencastOptions options)
        {
            m_screenHelper = new ScreenHelper();

            Options = JsonConvert.DeserializeObject<ScreencastOptions>(JsonConvert.SerializeObject(options));
            Output = Path.Combine(options.OutputPath, Guid.NewGuid().ToString());
            Options.OutputPath = Output;
            m_command = JsonConvert.SerializeObject(Options);
            Job = new Task(Capture, CancellationToken.None, TaskCreationOptions.DenyChildAttach);
        }

        private void Capture()
        {
            try
            {
                StartAt = DateTime.Now;
                Result = m_screenHelper.DoStartScreenCapture(m_command);
            }
            finally
            {
                EndAt = DateTime.Now;
                m_state = ScreenHelperState.Finished;
            }
        }

        public Task DoStartScreenCapture()
        {
            if (m_state > ScreenHelperState.Ready)
            {
                return Job;
            }
            m_state = ScreenHelperState.Recording;
            Job.Start(TaskScheduler.Default);
            return Job;
        }

        public void DoStopScreenCapture()
        {
            if (m_state > ScreenHelperState.Recording)
            {
                return;
            }
            m_state = ScreenHelperState.Stop;
            m_screenHelper.DoStopScreenCapture();
        }

        public void KillRecording()
        {
            m_screenHelper.KillRecording();
        }

        public void Abort()
        {
            m_state = ScreenHelperState.Abort;
        }
    }
}
