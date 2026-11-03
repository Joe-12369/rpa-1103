using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Excel.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace II.RPA.Plugin.Excel.Actions.Software
{
    public partial class SoftwareExcel : IExcel
    {
        private static readonly object exportRangeAsPictureSyncRoot = new object();

        private object ResolveExportWorksheet(ComScope com, object workbook, string sheet)
        {
            if (string.IsNullOrWhiteSpace(sheet))
            {
                return com.Track(ComLateBinding.GetProp(workbook, "ActiveSheet"));
            }

            object worksheets = com.Track(ComLateBinding.GetProp(workbook, "Worksheets"));
            return com.Track(ComLateBinding.GetItem(worksheets, sheet));
        }

        private void DeleteExportFileIfExists(string file)
        {
            ExcelUtils.TryFuncIgnoreException(() =>
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            });
        }

        private void ResetCopyState(object app)
        {
            ExcelUtils.TryFuncIgnoreException(() =>
            {
                if (app != null)
                {
                    ComLateBinding.SetProp(app, "CutCopyMode", false);
                }
            });
        }

        private bool WaitForClipboardImageAvailable(int timeoutMilliseconds)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds)
            {
                if (ExcelUtils.TryFuncIgnoreException(() => System.Windows.Forms.Clipboard.ContainsImage()))
                {
                    return true;
                }

                Thread.Sleep(60);
            }

            return false;
        }

        private System.Drawing.Image WaitForClipboardImage(int timeoutMilliseconds)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds)
            {
                var image = ExcelUtils.TryFuncIgnoreException(() => System.Windows.Forms.Clipboard.GetImage());
                if (image != null)
                {
                    return image;
                }

                Thread.Sleep(60);
            }

            return null;
        }

        private void RunSta(Action action, int timeoutMilliseconds)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                action();
                return;
            }

            Exception threadException = null;
            using (var completed = new ManualResetEventSlim(false))
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        threadException = ex;
                    }
                    finally
                    {
                        completed.Set();
                    }
                });

                thread.IsBackground = true;
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                if (!completed.Wait(timeoutMilliseconds))
                {
                    throw new TimeoutException("瀵煎嚭鍥剧墖瓒呮椂");
                }
            }

            if (threadException != null)
            {
                ExceptionDispatchInfo.Capture(threadException).Throw();
            }
        }

        private bool TryExportRangeAsPictureByClipboard(object exportRange, string file, int maxAttempts, int clipboardWaitMilliseconds, out Exception lastException)
        {
            lastException = null;
            object app = null;
            try
            {
                app = ComLateBinding.GetProp(exportRange, "Application");
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        ResetCopyState(app);
                        ExcelUtils.TryFuncIgnoreException(() => System.Windows.Forms.Clipboard.Clear());
                        ComLateBinding.Invoke(exportRange, "CopyPicture", 1, 2);

                        using (var image = WaitForClipboardImage(clipboardWaitMilliseconds + (attempt - 1) * 300))
                        {
                            if (image == null)
                            {
                                throw new IOException("剪贴板图片为空。");
                            }

                            image.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                        }

                        if (File.Exists(file))
                        {
                            return true;
                        }

                        throw new IOException("导出图片文件不存在。");
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        DeleteExportFileIfExists(file);
                        ExcelUtils.TryFuncIgnoreException(() => System.Windows.Forms.Clipboard.Clear());
                    }
                    finally
                    {
                        ResetCopyState(app);
                    }
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            return false;
        }

        private bool TryExportRangeAsPictureByChart(object exportRange, string file, int maxAttempts, out Exception lastException)
        {
            lastException = null;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using (var com = new ComScope())
                {
                    object app = null;
                    object tempWorkbook = null;
                    bool screenUpdating = true;
                    try
                    {
                        app = com.Track(ComLateBinding.GetProp(exportRange, "Application"));
                        screenUpdating = ExcelUtils.SafeGet(() => ComLateBinding.ToBool(ComLateBinding.GetProp(app, "ScreenUpdating")), true);
                        ComLateBinding.SetProp(app, "ScreenUpdating", false);
                        ResetCopyState(app);
                        ExcelUtils.TryFuncIgnoreException(() => System.Windows.Forms.Clipboard.Clear());

                        object workbooks = com.Track(ComLateBinding.GetProp(app, "Workbooks"));
                        tempWorkbook = com.Track(ComLateBinding.Invoke(workbooks, "Add"));
                        object worksheets = com.Track(ComLateBinding.GetProp(tempWorkbook, "Worksheets"));
                        object tempWorksheet = com.Track(ComLateBinding.GetItem(worksheets, 1));
                        object chartObjects = com.Track(ComLateBinding.Invoke(tempWorksheet, "ChartObjects"));
                        double width = Math.Max(1d, Convert.ToDouble(ComLateBinding.GetProp(exportRange, "Width")));
                        double height = Math.Max(1d, Convert.ToDouble(ComLateBinding.GetProp(exportRange, "Height")));
                        object chartObject = com.Track(ComLateBinding.Invoke(chartObjects, "Add", 0, 0, width, height));
                        object chart = com.Track(ComLateBinding.GetProp(chartObject, "Chart"));

                        ComLateBinding.Invoke(exportRange, "CopyPicture", 1, 2);
                        if (!WaitForClipboardImageAvailable(600 + attempt * 200))
                        {
                            throw new IOException("图表导出等待剪贴板图片超时。");
                        }

                        ComLateBinding.Invoke(chart, "Paste");
                        ComLateBinding.Invoke(chart, "Export", file);
                        if (File.Exists(file))
                        {
                            return true;
                        }

                        throw new IOException("导出图片文件不存在。");
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        DeleteExportFileIfExists(file);
                    }
                    finally
                    {
                        ExcelUtils.TryFuncIgnoreException(() =>
                        {
                            if (tempWorkbook != null)
                            {
                                ComLateBinding.Invoke(tempWorkbook, "Close", false);
                            }
                        });
                        ExcelUtils.TryFuncIgnoreException(() =>
                        {
                            if (app != null)
                            {
                                ComLateBinding.SetProp(app, "ScreenUpdating", screenUpdating);
                            }
                        });
                        ResetCopyState(app);
                    }
                }
            }

            return false;
        }

        private void ExportRangeAsPictureWithFallback(object exportRange, string file)
        {
            if (TryExportRangeAsPictureByClipboard(exportRange, file, 3, 700, out Exception clipboardException))
            {
                return;
            }

            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, clipboardException, "ExportRangeAsPicture fast clipboard route failed, fallback to chart route.");
            if (TryExportRangeAsPictureByChart(exportRange, file, 1, out Exception chartException))
            {
                return;
            }

            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, chartException, "ExportRangeAsPicture chart route failed, fallback to compatible clipboard recovery route.");
            if (TryExportRangeAsPictureByClipboard(exportRange, file, 2, 1600, out Exception recoveryClipboardException))
            {
                return;
            }

            throw recoveryClipboardException ?? chartException ?? clipboardException ?? new ExcelException("导出图片失败。".I18n("SheetInfo_DD341061"));
        }

        private void ExportRangeAsPictureCore(string workbook, string sheet, string range, string file)
        {
            RunSta(() =>
            {
                using (var com = new ComScope())
                {
                    object workerWorkbook = GetWorkbook(workbook);
                    object workerWorksheet = ResolveExportWorksheet(com, workerWorkbook, sheet);
                    object workerRange = com.Track(ComLateBinding.GetProp(workerWorksheet, "Range", ExcelUtils.ConvertExcelRangeAddress(range, true)));
                    ExportRangeAsPictureWithFallback(workerRange, file);
                }
            }, 15000);
        }
    }
}
