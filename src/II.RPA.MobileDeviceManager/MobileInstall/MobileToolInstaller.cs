using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.MobileInstall
{
    internal class MobileToolInstaller
    {
        private string _downloadFilePath;

        private string _mobileToolsTargetPath;

        private CancellationTokenSource _tokenSource;

        public event EventHandler<double> DownloadProgressChanged;

        public event EventHandler<double> UnpackProgressChanged;

        public event EventHandler MobileToolInstallEnded;

        public MobileToolInstaller()
        {
            _downloadFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "mobile.zip");
            string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
            _mobileToolsTargetPath = Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA");
        }

        public async Task Install()
        {
            _tokenSource = new CancellationTokenSource();
            string resourcePath = "http://rpa-mobile-tool.oss-cn-hangzhou.aliyuncs.com/mobile-tools.zip";
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"));
            }
            await new Downloader
            {
                ProgressCallback = delegate (double p)
                {
                    this.DownloadProgressChanged?.Invoke(this, p);
                }
            }.DownloadAsync(new Uri(resourcePath), _downloadFilePath, _tokenSource.Token);
            string mobilePath = Path.Combine(_mobileToolsTargetPath, "mobile");
            if (!Directory.Exists(mobilePath))
            {
                Directory.CreateDirectory(mobilePath);
            }
            await Task.Run(delegate
            {
                DeleteFolder(mobilePath);
            });
            await ZipHandler.GetInstance().UnpackAllAsync(_downloadFilePath, mobilePath, delegate (double p)
            {
                this.UnpackProgressChanged?.Invoke(this, p);
            });
            this.MobileToolInstallEnded?.Invoke(this, null);
        }

        public void Stop()
        {
            _tokenSource?.Cancel();
            _tokenSource = null;
        }

        private static void DeleteFolder(string path)
        {
            int retryCount = 0;
            do
            {
                try
                {
                    DirectoryInfo targetDir = new DirectoryInfo(path);
                    foreach (FileInfo item3 in targetDir.EnumerateFiles())
                    {
                        item3.Delete();
                    }
                    foreach (DirectoryInfo item4 in targetDir.EnumerateDirectories())
                    {
                        item4.Delete(recursive: true);
                    }
                    break;
                }
                catch (UnauthorizedAccessException ex)
                {
                    if (retryCount < 10)
                    {
                        Process[] adbScreenProcesses = Process.GetProcessesByName("adb");
                        if (adbScreenProcesses != null)
                        {
                            Process[] array = adbScreenProcesses;
                            foreach (Process item2 in array)
                            {
                                if (!item2.HasExited)
                                {
                                    item2.Kill();
                                }
                            }
                        }
                        Process[] nodeScreenProcesses = Process.GetProcessesByName("node");
                        if (nodeScreenProcesses != null)
                        {
                            Process[] array = nodeScreenProcesses;
                            foreach (Process item in array)
                            {
                                if (!item.HasExited)
                                {
                                    item.Kill();
                                }
                            }
                        }
                        retryCount++;
                        continue;
                    }
                    throw;
                }
                catch (DirectoryNotFoundException ex2)
                {
                    throw ex2;
                }
            }
            while (retryCount < 10);
        }
    }
}
