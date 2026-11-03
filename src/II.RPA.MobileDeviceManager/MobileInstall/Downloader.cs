using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.MobileInstall
{

    public class Downloader
    {
        public int Timeout { get; set; } = 1200000;


        public int NoResponseTimeout { get; set; } = 10000;


        public Action<double> ProgressCallback { get; set; }

        public async Task DownloadAsync(Uri uri, string fileName, CancellationToken token)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            using (FileStream fileStream = File.OpenWrite(fileName))
            {
                Task monitorTask = DownloadMonitor(fileStream, token);
                Task downloadTask = Download(uri, fileStream, token);
                await (await Task.WhenAny(monitorTask, downloadTask));
            }
        }

        private async Task Download(Uri uri, FileStream fileStream, CancellationToken token)
        {
            Action<double> progressCallback = ProgressCallback;
            if (progressCallback == null)
            {
                progressCallback = delegate
                {
                };
            }
            try
            {
                using (HttpClient _httpClient = new HttpClient())
                {
                    using (HttpResponseMessage response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                    {
                        long length = response.Content.Headers.ContentLength ?? (-1);
                        using (Stream stream = await response.Content.ReadAsStreamAsync())
                        {
                            byte[] buffer = new byte[4096];
                            int totalRead = 0;
                            while (true)
                            {
                                int num;
                                int read = (num = await stream.ReadAsync(buffer, 0, buffer.Length));
                                if (num <= 0)
                                {
                                    break;
                                }
                                await fileStream.WriteAsync(buffer, 0, read);
                                totalRead += read;
                                progressCallback((double)totalRead / (double)length * 100.0);
                                token.ThrowIfCancellationRequested();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task DownloadMonitor(FileStream fileStream, CancellationToken token)
        {
            long length = 0L;
            Stopwatch noResponseStopwatch = new Stopwatch();
            Stopwatch timeoutStopwatch = new Stopwatch();
            timeoutStopwatch.Start();
            try
            {
                while (true)
                {
                    await Task.Delay(100);
                    token.ThrowIfCancellationRequested();
                    if (Timeout > 0 && Timeout - timeoutStopwatch.ElapsedMilliseconds <= 0)
                    {
                        throw new TimeoutException("Downloader下载超时".I18n("controlType_Downloader_51D45D41"));
                    }
                    long fileLength;
                    try
                    {
                        fileLength = fileStream.Length;
                    }
                    catch (IOException)
                    {
                        return;
                    }
                    if (fileLength > length)
                    {
                        length = fileLength;
                        noResponseStopwatch.Reset();
                    }
                    else if (noResponseStopwatch.IsRunning)
                    {
                        if (NoResponseTimeout - noResponseStopwatch.ElapsedMilliseconds <= 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        noResponseStopwatch.Start();
                    }
                }
                throw new TimeoutException("Downloader下载无响应".I18n("controlType_Downloader_C52A577"));
            }
            finally
            {
                timeoutStopwatch.Stop();
                noResponseStopwatch.Stop();
            }
        }
    }
}
