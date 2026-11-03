using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.PatchUpdate.Helpers
{
    public class Win32Service
    {
        public static ProcessInfos WhoIsLockingUnary(string FilePath)
        {
            var processeInfos = new ProcessInfos();
            try
            {
                var fileAttrs = File.GetAttributes(FilePath);
                var paths = fileAttrs.HasFlag(FileAttributes.Directory) ? Directory.GetFiles(FilePath, "*", SearchOption.AllDirectories) : new string[] { FilePath };
                var processes = paths.AsParallel()
                    .WithDegreeOfParallelism(paths.Count() / 100 + 1 > 10 ? 10 : paths.Count() / 100 + 1)
                    .Where(p => p.IsLocked()).ToList()
                    .SelectMany(p => RstrtMgrUtil.WhoIsLocking(p)).Select(p => new ProcessInfo
                    {
                        PId = p.Id,
                        Path = p.MainModule.FileName,
                        Name = p.ProcessName
                    }).ToList();
                processeInfos.Processes.AddRange(processes.Select(p => p.PId).Distinct().Select(id => processes.FirstOrDefault(p => p.PId == id)));
                return processeInfos;
            }
            catch (FileNotFoundException)
            {
                return processeInfos;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
