using Grpc.Core;
using II.RPA.Core.Abstract;
using Rpa.Core.Worker;
using System;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.Service
{
    public class WorkerGRPCService : WorkerService.WorkerServiceBase, IGRPCService
    {
        private readonly IWorkerTalkerRegistrar _workerTalkerRegistrar;

        public WorkerGRPCService(IWorkerTalkerRegistrar workerTalkerRegistrar)
        {
            _workerTalkerRegistrar = workerTalkerRegistrar ??
                throw new ArgumentNullException(nameof(workerTalkerRegistrar));
        }

        public ServerServiceDefinition BindingToService()
        {
            return WorkerService.BindService(this);
        }

        public override Task Talk(
            IAsyncStreamReader<FrameStream> requestStream,
            IServerStreamWriter<FrameStream> responseStream,
            ServerCallContext context)
        {
            return _workerTalkerRegistrar.RegisterTalker(
                requestStream,
                responseStream,
                context);
        }

        public override Task ChunkTalk(
            IAsyncStreamReader<ChunkFrameStream> requestStream,
            IServerStreamWriter<ChunkFrameStream> responseStream,
            ServerCallContext context)
        {
            return _workerTalkerRegistrar.RegisterChunkTalker(
                requestStream,
                responseStream,
                context);
        }
    }
}
