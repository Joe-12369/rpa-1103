using II.RPA.EventMonitor.Common;
using II.RPA.EventMonitor.Entitys;
using II.RPA.EventMonitor.JSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace II.RPA.EventMonitor
{
    class FileTrigger : Trigger, IDisposable
    {
        #region 常量
        const string MonitorEventAll = "全部";
        const string MonitorEventCreate = "创建";
        const string MonitorEventDelete = "删除";
        const string MonitorEventUpdate = "更新";
        const string MonitorEventRename = "重命名";
        #endregion

        FileSystemWatcher FileWatcher = null;
        NetworkFilePoller NetworkFilePoller = null;
        private readonly object queueObject = null;
        private readonly object dicObject = null;
        private ConcurrentQueue<FileSystemEventArgs> watcherChangeQueue = null;
        private WatcherChangeTypes MonitorTypes;
        private Dictionary<string, DateTime> dicFileAndEventTime = new Dictionary<string, DateTime>();
        private AutoResetEvent triggerWait = new AutoResetEvent(false);


        public FileTrigger()
        {
            //FileWatcher = new FileSystemWatcher();
            watcherChangeQueue = new ConcurrentQueue<FileSystemEventArgs>();
            queueObject = new object();
            dicObject = new object();
        }
        public FileTrigger(FileTriggerJson json) : this()
        {
            try
            {
                this.TriggerID = json.id;
                this.FilePath = json.filePath;
                this.FileType = string.IsNullOrEmpty(json.fileType.Trim()) ? "*.*" : json.fileType.Trim().TrimEnd(';');
                this.ContainSub = json.containSubfolder == 0 ? false : true;
                this.MonitorEvent = json.monitorEvent.Trim().TrimEnd(';');
                this.TriggerName = json.name;

                this.EqualsFunc = (a, b) =>
                {
                    return true;
                };
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// 是否包含子路径
        /// </summary>
        public bool ContainSub { get; set; }
        /// <summary>
        /// 文件类型
        /// </summary>
        public string FileType { get; set; }
        /// <summary>
        /// 监控事件类型 all;create;delete;update;rename
        /// </summary>
        public string MonitorEvent { get; set; }
        private string[] MonitorEventArr { get; set; }

        public void Dispose()
        {
            FileWatcher?.Dispose();
            FileWatcher = null;
        }
        private void InitMonitorTypes()
        {
            MonitorTypes = WatcherChangeTypes.All;

            if (this.MonitorEvent.ToLower() == "all")
            {
                this.MonitorEvent = "all;create;delete;update;rename";//兼容bot factory传递的格式差异
            }
            this.MonitorEventArr = MonitorEvent.ToLower().Split(';');
            //all; create; delete; update; rename
            if (MonitorEventArr.Contains("all"))
            {
                return;
            }
            if (!MonitorEventArr.Contains("create"))
            {
                MonitorTypes = MonitorTypes & ~WatcherChangeTypes.Created;
            }
            if (!MonitorEventArr.Contains("delete"))
            {
                MonitorTypes = MonitorTypes & ~WatcherChangeTypes.Deleted;
            }
            if (!MonitorEventArr.Contains("update"))
            {
                MonitorTypes = MonitorTypes & ~WatcherChangeTypes.Changed;
            }
            if (!MonitorEventArr.Contains("rename") && !MonitorEventArr.Contains("rename_to"))
            {
                MonitorTypes = MonitorTypes & ~WatcherChangeTypes.Renamed;
            }
        }
        /// <summary>
        /// 替代 System.Web.MimeMapping（.NET Core 中不存在），仅覆盖 Office 文档所需的最小 MIME 映射
        /// </summary>
        private static string GetMimeMapping(string path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".pptx":
                    return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case ".xls":
                    return "application/vnd.ms-excel";
                case ".doc":
                    return "application/msword";
                case ".ppt":
                    return "application/vnd.ms-powerpoint";
                default:
                    return "application/octet-stream";
            }
        }
        private static bool IsRemoteShare(string path)
        {
            try
            {
                // UNC \\server\share
                if (path.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
                    return true;

                // 映射盘符
                if (path.Length >= 3 && path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
                {
                    var drive = path.Substring(0, 2);   // 例如 Z:
                    var driveInfo = new DriveInfo(drive);
                    return driveInfo.DriveType == DriveType.Network;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        internal override void StartTrigger()
        {
            JMessage message = new JMessage();
            try
            {
                this.Status = eTriggerStatus.Starting;
                if (!Directory.Exists(FilePath) && !File.Exists(FilePath))
                {
                    string msg = $"地址不存在：{FilePath}";
                    ErrorAndStop(eMessageStatus.Error_File_PathNotExists, msg, null);
                    return;
                }
                InitMonitorTypes();
                if (IsRemoteShare(FilePath))
                {
                    Loggers.Info($"【{this.TriggerName}】目标为远程目录");
                    NetworkFilePoller = new NetworkFilePoller(FilePath, TimeSpan.FromSeconds(3), this.ContainSub,this.MonitorTypes, FileType.TrimEnd(';'));
                    NetworkFilePoller.RemoteFileChanged += (sender, e) => TryTrigger_Befor(this, e);
                    NetworkFilePoller.Start();
                }
                else
                {
                    SetFileWarcher();
                }

                StartConsumeWatcherChanges();
                this.Status = eTriggerStatus.Runing;
            }
            catch (Exception ex)
            {
                string msg = "文件触发器启动异常";
                ErrorAndStop(eMessageStatus.Error_File_Exception, msg, ex);
            }
        }

        private void SetFileWarcher()
        {
            try
            {
                FileWatcher = new FileSystemWatcher();
                FileWatcher.Path = FilePath;
                if (FileType.TrimEnd(';').Contains(";") || FileType.Trim().TrimEnd(';') == "*.*")
                {
                    //FileWatcher.Filter = "";
                }
                else
                {
                    FileWatcher.Filter = FileType.TrimEnd(';');
                }
                FileWatcher.IncludeSubdirectories = this.ContainSub;
                FileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite;   //设置文件的文件名、目录名及文件的大小改动会触发Changed事件
                


                if ((MonitorTypes & WatcherChangeTypes.Created) != 0)
                {
                    FileWatcher.Created -= FileWatcher_Created;
                    FileWatcher.Created += FileWatcher_Created;
                }
                if ((MonitorTypes & WatcherChangeTypes.Deleted) != 0)
                {
                    FileWatcher.Deleted -= FileWatcher_Deleted;
                    FileWatcher.Deleted += FileWatcher_Deleted;
                }
                if ((MonitorTypes & WatcherChangeTypes.Changed) != 0)
                {
                    FileWatcher.Changed -= FileWatcher_Changed;
                    FileWatcher.Changed += FileWatcher_Changed;
                }
                if ((MonitorTypes & WatcherChangeTypes.Renamed) != 0)
                {
                    FileWatcher.Renamed -= FileWatcher_Renamed;
                    FileWatcher.Renamed += FileWatcher_Renamed;
                }
                //如果不包含修改，但是监控.xlsx
                else if (FileType.Split(';').Any(s => s.EndsWith(".xlsx")))
                {
                    FileWatcher.Renamed -= FileWatcher_Renamed;
                    FileWatcher.Renamed += FileWatcher_Renamed;
                }

                FileWatcher.EnableRaisingEvents = true;

            }
            catch (Exception ex)
            {
                FileWatcher.EnableRaisingEvents = false;
                string msg = "文件触发器启动异常";
                ErrorAndStop(eMessageStatus.Error_File_Exception, msg, ex);
            }
        }

        internal override void StopTrigger()
        {
            this.Status = eTriggerStatus.Stoping;
            try
            {
                if (FileWatcher != null)
                {
                    FileWatcher.EnableRaisingEvents = false;
                }
                NetworkFilePoller?.Stop();
                this.Dispose();
            }
            catch (Exception ex)
            {
                string msg = "文件触发器停止异常";
                SendMessage(eMessageStatus.Error_CloseTrigger_File, msg, ex);
            }
            this.Status = eTriggerStatus.Stoped;
        }
        internal override void Continue()
        {
            try
            {
                if (FileWatcher != null)
                    FileWatcher.EnableRaisingEvents = true;
                HasTriggered = false;
                this.Status = eTriggerStatus.Runing;
                triggerWait.Set();
            }
            catch (Exception ex)
            {
                string msg = "文件触发器继续执行异常";
                ErrorAndStop(eMessageStatus.Error_File_Exception, msg, ex);
            }
        }
        //private Dictionary<string, DateTime> dicRameToChange = new Dictionary<string, DateTime>();
        private void FileWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            try
            {
                #region Excel更新事件
                if (this.MonitorEventArr.Contains("update"))
                {
                    Thread.Sleep(50);
                    string ext_new = Path.GetExtension(e.Name).ToLower();
                    string ext_old = Path.GetExtension(e.OldName).ToLower();
                    string contentType = GetMimeMapping(e.OldFullPath);
                    string[] extArr = { ".xlsx", ".docx", ".xls", ".doc" };
                    /**************Excel 更新 修改机制说明********************/
                    //创建：2003及以前Excel版本，可以触发创建，但同时会触发多次更新事件 
                    //      2003(不含)以后的Excel，不触发创建，但触发两次重命名：1、xlsx到tmp,tmp格式文件名不包含包含~ 。 2、无格式到xlsx
                    //更新：2003及以前Excel版本，不触发更新，但触发两次重命名：1、xls到tmp,tmp格式文件名中间包含~ 。 2、无格式到xls 
                    //      2003(不含)以后的Excel，不触发更新，但触发两次重命名：1、xlsx到tmp,tmp格式文件名中间不包含~  。 2、无格式到xlsx

                    //excel2003往后的版本 修改和添加都会触发两次重命名，其中都有一次从xlsx到tmp格式重命名的情况，但创建的临时文件中包含~号
                    /**********************************/
                    if ((ext_new == ".tmp" && contentType.Contains("officedocument"))
                        || ((ext_new == ".tmp") && extArr.Contains(ext_old)))
                    {
                        FileSystemEventArgs eFile = new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(e.OldFullPath), e.OldName);

                        if (eFile != null)
                        {
                            TryTrigger_Befor(sender, eFile);
                            return;
                        }
                    }
                }

                //如果没有监控rename 则return
                if (!this.MonitorEventArr.Contains("rename"))
                {
                    return;
                }
                #endregion
                ////在重名命事件中，重命名后的名字 包含在监控名中
                //if (FileType.Split(';').Contains(e.Name))
                //{
                //    if (!MonitorEventArr.Contains("rename_to"))
                //    {
                //        return;
                //    }
                //}
                //修改之前的名字 是符合具体的监控名，且 包含 "rename"
                if (FileType.Split(';').Contains(e.OldName) && this.MonitorEventArr.Contains("rename"))
                {
                    FileSystemEventArgs eFile = new FileSystemEventArgs(WatcherChangeTypes.Renamed, Path.GetDirectoryName(e.OldFullPath), e.OldName);

                    TryTrigger_Befor(this, eFile);
                }
                else
                {
                    TryTrigger_Befor(this, e);
                }

            }
            catch (Exception ex)
            {
                string logMsg = $"文件更新事件异常。{e}";
                ErrorAndStop(eMessageStatus.Error_File_EventExecute, logMsg, ex);
            }
        }

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            TryTrigger_Befor(this, e);
        }

        private void FileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            TryTrigger_Befor(this, e);
        }

        private void FileWatcher_Created(object sender, FileSystemEventArgs e)
        {
            TryTrigger_Befor(this, e);
        }

        protected void TryTrigger_Befor(object sender, FileSystemEventArgs e)
        {
            try
            {

                string ext = Path.GetExtension(e.FullPath).ToLower();
                string fileName = Path.GetFileNameWithoutExtension(e.Name) + ".*";
                /// FileType.Contains(";") &&
                //过虑条件格式 不符合*.后缀 也不符合 具体的名字
                if (FileType != "*.*" && !(FileType.Split(';').Contains("*" + ext) || FileType.Split(';').Contains(e.Name)))
                {
                    if (!string.IsNullOrEmpty(ext))//可能存在无后缀 
                        return;
                    else if (e.ChangeType == WatcherChangeTypes.Created)
                    {
                        return;
                    }
                }


                ////不包含；，且格式不符合
                //if (FileType != "*" + ext)
                //{
                //    return;
                //}
                if (e.Name.StartsWith("~"))
                {
                    return;
                }

                FileInfo file = new FileInfo(e.FullPath);
                var filterAttr = FileAttributes.Temporary | FileAttributes.Hidden;

                if ((e.ChangeType == WatcherChangeTypes.Deleted) || (file.Attributes & filterAttr) == 0)
                {
                    EnqueueChangeQueue(e);
                }
                else if (e.ChangeType == WatcherChangeTypes.Renamed)
                {
                    EnqueueChangeQueue(e);
                }

            }
            catch (Exception ex)
            {
                string logMsg = $"文件触发器 事件处理方法异常。{e}";
                ErrorAndStop(eMessageStatus.Error_File_EventExecute, logMsg, ex);
            }
        }
        protected override void TryTrigger(object sender, EventArgs e)
        {
            base.TryTrigger(sender, e);
        }
        #region 消费事件
        private void StartConsumeWatcherChanges()
        {
            Thread t = new Thread(ConsumeWatcherChanges);
            t.IsBackground = true;
            t.Start();
        }

        private void ConsumeWatcherChanges()
        {
            try
            {

                while (this.Status != eTriggerStatus.Stoped)
                {
                    if (watcherChangeQueue.Any())
                    {
                        if (!watcherChangeQueue.TryDequeue(out var EventArg))
                        {
                            continue;
                        }
                        TryTrigger(this, EventArg);
                        if (this.Status == eTriggerStatus.Runing)
                        {
                            continue;
                        }
                        lock (queueObject)
                        {
                            if (FileWatcher != null)//非重复执行，可能会被释放
                            {
                                FileWatcher.EnableRaisingEvents = false;
                            }
                            //清除按键队列
                            while (watcherChangeQueue.TryDequeue(out var e))
                            { }
                        }
                        //triggerWait.WaitOne();
                    }
                    else
                    {
                        triggerWait.WaitOne();
                    }
                    //Thread.Sleep(100);//没有数据时100毫秒后 继续
                }
                //清除按键队列
                while (watcherChangeQueue.TryDequeue(out var e))
                { }

            }
            catch (Exception ex)
            {
                string logMsg = $"文件触发器 消费事件队列时方法异常。";
                ErrorAndStop(eMessageStatus.Error_File_EventExecute, logMsg, ex);
            }
        }
        #endregion
        private void EnqueueChangeQueue(FileSystemEventArgs e)
        {
            try
            {
                //此处逻辑是用来判断200毫秒内，同一个文件只触发一次事件（不区分类型）
                lock (dicObject)
                {
                    DateTime now = DateTime.Now;
                    if (dicFileAndEventTime.Keys.Contains(e.FullPath))
                    {
                        return;
                    }
                    dicFileAndEventTime[e.FullPath] = now;
                    Task.Delay(200).ContinueWith(delegate
                    {
                        lock (dicObject)
                        {
                            dicFileAndEventTime.Remove(e.FullPath);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                string logMsg = $"文件触发器 事件进入队列前，200毫秒内非重复触发限制 异常。";
                ErrorAndStop(eMessageStatus.Error_File_EventExecute, logMsg, ex);
            }

            try
            {
                //事件进入消费队列
                lock (queueObject)
                {
                    if (FileWatcher?.EnableRaisingEvents == true || NetworkFilePoller != null)
                    {
                        MainForm.PrintMessage($"【入列】{e.ChangeType}【File】{e.FullPath}");
                        watcherChangeQueue.Enqueue(e);
                        triggerWait.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                string logMsg = $"文件触发器 事件进入队列时方法异常。";
                ErrorAndStop(eMessageStatus.Error_File_EventExecute, logMsg, ex);
            }
        }
    }
}
