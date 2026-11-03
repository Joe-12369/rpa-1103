using Grpc.Core;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using Newtonsoft.Json;
using Rpa.Core.Base;
using Rpa.Core.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.Service
{
    /// <summary>
    /// Win32服务
    /// </summary>
    public class Win32GRPCService : Win32Service.Win32ServiceBase, IGRPCService
    {
        private readonly ILogger m_Logger;

        public Win32GRPCService() {}

        public Win32GRPCService(ILogger aLogger):this()
        {
            m_Logger = aLogger;
        }

        #region IGRPCService
        public ServerServiceDefinition BindingToService()
        {
            return Win32Service.BindService(this);
        }
        #endregion

        public override Task<ProcessInfos> WhoIsLockingUnary(StringValue request, ServerCallContext context)
        {
            var processeInfos = new ProcessInfos();
            try
            {
                var fileAttrs = File.GetAttributes(request.Value);
                var paths = fileAttrs.HasFlag(FileAttributes.Directory) ? Directory.GetFiles(request.Value, "*", SearchOption.AllDirectories) : new string[] { request.Value };
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
                return Task.FromResult(processeInfos);
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult(processeInfos);
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message, ex));
            }
        }
    }
}
