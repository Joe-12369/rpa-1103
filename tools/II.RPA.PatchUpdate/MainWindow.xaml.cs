using II.RPA.PatchUpdate.Helpers;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;

namespace II.RPA.PatchUpdate
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string dll7z = AppDomain.CurrentDomain.BaseDirectory + (Environment.Is64BitProcess ? "x64" : "x86") + "/7z.dll";
        private string dllSevenZipSharp = AppDomain.CurrentDomain.BaseDirectory + "SevenZipSharp.dll";

        private string _appInstPath = "";
        private PatchInfo _patchInfo = null;
        private string _engineDirName = "";
        private LogHelper _log = null;
        SevenZipExtractor extractor = null;
        private int MaxValue = 0;
        private volatile int CurrentValue = 0;

        private List<string> pluginsInstPaths = null;
        private List<string> pluginsSDKPaths = null;
        private string oEnginePath = "";
        private string nEnginePath = "";
        private bool isFinished = false;
        private string clintType = "";

        public MainWindow(string title, string appInstPath, PatchInfo patchInfo, string engineDirName, LogHelper log)
        {
            //this.SourceInitialized += aaa;
            InitializeComponent();
            this.Title = title;
            if (patchInfo.type.ToLower().Contains("z-factory"))
            {
                deviceType.Text = "设计器";
            }
            else
            {
                deviceType.Text = "机器人";
            }
            Check7zAndSevenZipSharpDll();
            _appInstPath = appInstPath;
            _patchInfo = patchInfo;
            _engineDirName = engineDirName;
            _log = log;
            pluginsInstPaths = new List<string>();
            pluginsSDKPaths = new List<string>();
            SevenZipExtractor.SetLibraryPath(dll7z);
            extractor = new SevenZipExtractor(AppDomain.CurrentDomain.BaseDirectory + _patchInfo.name + ".zip");

            if (!extractor.Check())
            {
                _log.Error("Patch包校验失败");
                throw new Exception("Patch包校验失败");
            }

            progressBar1.Maximum = extractor.ArchiveFileData.Count + 1;
            MaxValue = extractor.ArchiveFileData.Count;
            this.Loaded += (o, e) =>
            {
                Form_Load();
            };
        }

        public async void Form_Load()
        {
            await Task.Run(() =>
            {
                try
                {
                    extractor.FileExtractionStarted += new EventHandler<FileInfoEventArgs>(extr_FileExtractionStarted);
                    extractor.FileExtractionFinished += new EventHandler<FileInfoEventArgs>(extr_FileExtractionFinished);
                    extractor.BeginExtractArchive(_appInstPath);
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                }
            });
        }

        private void Extractor_ExtractionFinished(object sender, EventArgs e)
        {

            MessageBox.Show("finished");

            #region 覆盖engine目录下文件
            if (nEnginePath != oEnginePath)
            {
                CopyFile(nEnginePath, oEnginePath);
                Directory.Delete(nEnginePath, true);
            }
            #endregion

            #region 将插件更新复制去Z-SDK目录下
            if (pluginsInstPaths.Count > 0 && pluginsSDKPaths.Count > 0)
            {
                for (int i = 0; i < pluginsInstPaths.Count; i++)
                {
                    CopyFile(pluginsInstPaths[i], pluginsSDKPaths[i]);
                }
            }
            #endregion

            Thread.Sleep(1000);
            Close();
        }

        private volatile object m_extract_lock = new object();
        private void extr_FileExtractionStarted(object sender, FileInfoEventArgs e)
        {
            try
            {
                // label1.Text = String.Format("{0}", e.FileInfo.FileName);

                string[] arrPath = e.FileInfo.FileName.Split('\\');
                string enginePath = e.FileInfo.FileName;

                if (string.IsNullOrEmpty(clintType))
                {
                    clintType = arrPath[0];
                }
                if (arrPath.Length > 5 && arrPath[5].IndexOf("rpa-engine-") > -1)
                {
                    enginePath = enginePath.Replace(arrPath[5], _engineDirName);
                    if (oEnginePath == "")
                    {
                        oEnginePath = _appInstPath + "\\" + enginePath; ;
                        _log.Info($"判断文件【{oEnginePath}】是否被占用");
                        ProcessInfos processInfos = Win32Service.WhoIsLockingUnary(oEnginePath);
                        processInfos.Processes.ForEach(a =>
                        {
                            _log.Info($"被占用文件【{a.Name}】,进程Id【{a.PId}】,文件路径【{a.Path}】");
                            Process.GetProcessById(a.PId)?.Kill();
                        });

                    }
                    if (nEnginePath == "")
                    {
                        nEnginePath = _appInstPath + "\\" + e.FileInfo.FileName;
                    }

                }

                #region 备份需要替换的文件
                string backup_oPath = _appInstPath + "\\" + enginePath;
                string backup_nPath = _appInstPath + "\\" + $"backup-{_patchInfo.patchVersion}" + "\\" + enginePath;

                if (Directory.Exists(backup_oPath) && !Directory.Exists(backup_nPath))
                {
                    Directory.CreateDirectory(backup_nPath);
                }

                if (File.Exists(backup_oPath))
                {
                    File.Copy(backup_oPath, backup_nPath, true);
                }
                #endregion

                #region 如果是插件，安装目录下执行替换，Z-SDK下执行增加

                if (arrPath.Length > 7 && arrPath[6] == "plugins")
                {
                    if (Directory.Exists(_appInstPath + "\\" + enginePath))
                    {
                        CopyFile(backup_oPath, backup_nPath);
                        Directory.Delete(_appInstPath + "\\" + enginePath, true);
                    }

                    //记录需要复制的插件的路径
                    if (arrPath.Length == 8)
                    {
                        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string sdkPath = appDataPath + "\\" + "Z-SDKs";
                        pluginsSDKPaths.Add(sdkPath + "\\" + arrPath[arrPath.Length - 1]);
                        pluginsInstPaths.Add(_appInstPath + "\\" + enginePath);
                    }
                }
                #endregion

                lock (m_extract_lock)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        progressBar1.Value += 1;
                        CurrentValue += 1;
                        //ImageProgressBar(CurrentValue, MaxValue, 193, image1);
                        _log.Info($"总文件数:{progressBar1.Maximum}");
                        _log.Info($"当前解压到文件数顺序:{progressBar1.Value + 1}");
                        //if (progressBar1.Maximum == progressBar1.Value + 1)
                        if (CurrentValue >= MaxValue)
                        {
                            // label1.Text = "更新处理中...";
                            isFinished = true;
                        }
                    });
                }

            }
            catch (Exception ex)
            {
                _log.Info($"{e.FileInfo.FileName}解压失败:{ex}");
                throw ex;
            }
        }
        private void extr_FileExtractionFinished(object sender, FileInfoEventArgs e)
        {
            _log.Info($"解压文件{e.FileInfo.FileName}成功");
            //try
            //{
            if (isFinished)
            {
                //Task.Run(async () =>
                //{
                try
                {
                    _log.Info("Patch更新完成");
                    //MessageBox.Show("finished");

                    #region 覆盖engine目录下文件
                    if (nEnginePath != oEnginePath)
                    {
                        CopyFile(nEnginePath, oEnginePath);
                        Directory.Delete(nEnginePath, true);
                    }
                    #endregion

                    #region 将插件更新复制去Z-SDK目录下
                    if (pluginsInstPaths.Count > 0 && pluginsSDKPaths.Count > 0)
                    {
                        for (int i = 0; i < pluginsInstPaths.Count; i++)
                        {
                            CopyFile(pluginsInstPaths[i], pluginsSDKPaths[i]);
                        }
                    }
                    #endregion

                    //await Task.Delay(1000);
                    Thread.Sleep(1000);
                    this.Dispatcher.Invoke(() =>
                    {
                        _log.Info("关闭更新窗口");
                        this.DialogResult = true;
                        Close();
                    });
                }
                catch (Exception ex)
                {
                    _log.Error($"替换文件异常:【{ex.ToString()}】");
                    this.Dispatcher.Invoke(() =>
                    {
                        _log.Info("关闭更新窗口");
                        this.DialogResult = false;
                        Close();
                    });
                    //throw ex;
                }

                //});

            }
            //}
            //catch (Exception ex)
            //{
            //    throw ex;
            //}
        }

        /// <summary>
        /// 图片进度条
        /// </summary>
        /// <param name="current"></param>
        /// <param name="max"></param>
        /// <param name="imageWidth">图片的实际宽度</param>
        /// <param name="image1"></param>
        private static void ImageProgressBar(double current, double max, double imageWidth, System.Windows.Controls.Image image1)
        {
            if (max != 0)
                image1.Width = Math.Round((current / max) * imageWidth);
        }

        private void Check7zAndSevenZipSharpDll()
        {
            bool b1 = File.Exists(dll7z);
            bool b2 = File.Exists(dllSevenZipSharp);

            if (!b1)
            {
                MessageBox.Show(Properties.Resources._7zDllNotExistMessage, Properties.Resources.WrongInfoCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = true;
                Close();
            }

            if (!b2)
            {
                MessageBox.Show(Properties.Resources.SevenZipSharpDllNotExistMessage, Properties.Resources.WrongInfoCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// copy文件夹下的所有文件
        /// </summary>
        /// <param name="oPath">要拷贝的文件夹路径</param>
        /// <param name="nPath">目标路径</param>
        private static void CopyFile(string oPath, string nPath)
        {
            try
            {
                if (!Directory.Exists(nPath))
                {
                    Directory.CreateDirectory(nPath);
                }

                DirectoryInfo sDir = new DirectoryInfo(oPath);
                FileInfo[] fileArray = sDir.GetFiles();
                foreach (FileInfo file in fileArray)
                {
                    file.CopyTo(nPath + "\\" + file.Name, true);
                }

                //DirectoryInfo dDir = new DirectoryInfo(nPath);
                DirectoryInfo[] subDirArray = sDir.GetDirectories();
                foreach (DirectoryInfo subDir in subDirArray)
                {
                    CopyFile(subDir.FullName, nPath + "\\" + subDir.Name);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _log.Info($"更新程序即将关闭");
            //关闭前的检查
            Task[] tasks = new Task[1];
            tasks[0] = Task.Run(() =>
             {
                 #region 2-3-4
                 //关闭窗口先判断是否还存在多余的engine目录
                 try
                 {
                     _log.Info($"更新异常关闭中...");
                     string backUpPath_Current = _appInstPath + "\\" + $"backup-{_patchInfo.patchVersion}" + "\\" + clintType;
                     _log.Info($"找到当前备份文件:{backUpPath_Current}");
                     string appInstallPath_Current = _appInstPath + "\\" + clintType;
                     _log.Info($"找到当前安装文件:{appInstallPath_Current}");

                     _log.Info($"多余engine目录:{nEnginePath}");
                     _log.Info($"原有engine目录:{oEnginePath}");
                     //判定多余的engine-xx目录是否有被删除成功
                     if (Directory.Exists(nEnginePath) && nEnginePath != oEnginePath)
                     {
                         _log.Info($"进行多余的engine目录删除:{nEnginePath}");
                         Directory.Delete(nEnginePath, true);

                         //先判定当前是否已经做好文件备份
                         if (Directory.Exists(backUpPath_Current))
                         {
                             _log.Info($"如果未删除成功多余的engine目录，则进行文件还原");
                             //File.Copy(backUpPath_Current, appInstallPath_Current, true);
                             CopyFile(backUpPath_Current, appInstallPath_Current);
                         }
                     }
                 }
                 catch (Exception ex)
                 {
                     _log.Error($"内部还原更新异常:{ex}");
                 }
                 #endregion
             });
            Task.WaitAll(tasks);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

    }
}
