using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor
{
    internal class NetworkFilePoller
    {
        private readonly string _pathToWatch;
        private readonly ConcurrentDictionary<string, (long Size, DateTime LastWrite)> _fileSnapshot;
        private readonly TimeSpan _pollingInterval;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly bool _recursive;   // 待过滤扩展名，空数组=全部文件
        private readonly string[] _extensions;   // 待过滤扩展名，空数组=全部文件
        //public event FileSystemEventHandler Created;
        //public event FileSystemEventHandler Deleted;
        //public event FileSystemEventHandler Changed;
        //public event FileSystemEventHandler Renamed;//其实没有

        public event FileSystemEventHandler RemoteFileChanged;   // 
        public Action<object, FileSystemEventArgs> TryTrigger_Befor { get; set; }
        private WatcherChangeTypes WatcherChangeTypes { get; set; }
        public NetworkFilePoller(string networkPath, TimeSpan pollingInterval, bool recursive, WatcherChangeTypes watcherChangeTypes, string extFilter = null)
        {
            if (!Directory.Exists(networkPath))
                throw new DirectoryNotFoundException($"目录不存在或无法访问: {networkPath}");
            WatcherChangeTypes = watcherChangeTypes;
            // 解析扩展名
            _extensions = string.IsNullOrWhiteSpace(extFilter)
                ? Array.Empty<string>()
                : extFilter.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                           //.Select(e => e.StartsWith(".") ? e : "." + e)
                           .ToArray();
            _pathToWatch = networkPath;
            _fileSnapshot = new ConcurrentDictionary<string, (long, DateTime)>();
            _pollingInterval = pollingInterval;
            _recursive = recursive;
        }

        private List<string> GetFilteredFiles(string root, bool recursive = true)
        {
            if (_extensions.Length == 0)
            {
                // 不限扩展名，直接返回延迟序列
                return Directory.EnumerateFiles(root, "*.*",
                           recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .ToList();
            }

            // 每个扩展名单独用通配符搜，Win32 层提前过滤
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return _extensions
                   .Select(ext => Directory.EnumerateFiles(root, "*" + ext, option).ToList())
                   .SelectMany(a => a)
                   .Distinct()
                   .ToList();
        }




        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            // 首次扫描，建立初始文件状态快照
            BuildInitialSnapshot();

            // 启动后台轮询任务
            Task.Run(async () => await PollingLoop(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            Console.WriteLine("文件轮询监控已停止。");
        }

        private void BuildInitialSnapshot()
        {
            _fileSnapshot.Clear();
            try
            {
                var allFiles = GetFilteredFiles(_pathToWatch, _recursive);
                foreach (var filePath in allFiles)
                {
                    var fileInfo = new FileInfo(filePath);
                    // 记录文件大小和最后修改时间作为标识
                    _fileSnapshot[filePath] = (fileInfo.Length, fileInfo.LastWriteTimeUtc);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"建立初始文件快照时出错: {ex.Message}");
            }
        }

        private async Task PollingLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_pollingInterval, cancellationToken);

                    //var currentFiles = Directory.GetFiles(_pathToWatch, "*.*", SearchOption.AllDirectories);
                    var currentFiles = GetFilteredFiles(_pathToWatch, recursive: false).ToList();
                    var currentSnapshot = new ConcurrentDictionary<string, (long, DateTime LastWrite)>();

                    // 获取当前文件状态
                    foreach (var filePath in currentFiles)
                    {
                        var fileInfo = new FileInfo(filePath);
                        currentSnapshot[filePath] = (fileInfo.Length, fileInfo.LastWriteTimeUtc);
                    }

                    // 检查新文件和被修改的文件
                    foreach (var file in currentSnapshot.Keys)
                    {
                        if (!_fileSnapshot.ContainsKey(file))
                        {
                            OnFileCreated(file);
                            break;
                        }
                        else if (Math.Abs((currentSnapshot[file].LastWrite - _fileSnapshot[file].LastWrite).TotalSeconds) > 1) // 修改时间差大于1秒视为变更
                        {
                            OnFileChanged(file);
                            break;
                        }
                    }

                    // 检查被删除的文件
                    foreach (var file in _fileSnapshot.Keys)
                    {
                        if (!currentSnapshot.ContainsKey(file))
                        {
                            OnFileDeleted(file);
                            break;
                        }
                    }

                    // 更新快照
                    _fileSnapshot.Clear();
                    foreach (var item in currentSnapshot)
                    {
                        _fileSnapshot[item.Key] = item.Value;
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // 处理暂时的I/O或权限错误，下次轮询重试
                    Console.WriteLine($"轮询过程中发生可恢复错误: {ex.Message}");
                }
                catch (OperationCanceledException)
                {
                    // 任务被取消，正常退出
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"轮询过程中发生未预期错误: {ex.Message}");
                }
            }
        }
        private void RaiseFileEvent(WatcherChangeTypes changeType, string fullPath)
        {
            var dir = Path.GetDirectoryName(fullPath) ?? string.Empty;
            var name = Path.GetFileName(fullPath);
            var eFile = new FileSystemEventArgs(changeType, dir, name);
            // 再走公共事件
            RemoteFileChanged?.Invoke(this, eFile);
        }
        // 您的事件处理方法
        private void OnFileCreated(string filePath)
        {
            if (WatcherChangeTypes != WatcherChangeTypes.All && !WatcherChangeTypes.HasFlag(WatcherChangeTypes.Created)) return;
            MainForm.PrintMessage($"[检测到创建] {filePath}");
            RaiseFileEvent(WatcherChangeTypes.Created, filePath);
        }

        private void OnFileChanged(string filePath)
        {
            if (WatcherChangeTypes != WatcherChangeTypes.All && !WatcherChangeTypes.HasFlag(WatcherChangeTypes.Changed)) return;
            MainForm.PrintMessage($"[检测到修改] {filePath}");
            RaiseFileEvent(WatcherChangeTypes.Changed, filePath);
        }

        private void OnFileDeleted(string filePath)
        {
            if (WatcherChangeTypes != WatcherChangeTypes.All && !WatcherChangeTypes.HasFlag(WatcherChangeTypes.Deleted)) return;
            MainForm.PrintMessage($"[检测到删除] {filePath}");
            RaiseFileEvent(WatcherChangeTypes.Deleted, filePath);
        }
    }
}
