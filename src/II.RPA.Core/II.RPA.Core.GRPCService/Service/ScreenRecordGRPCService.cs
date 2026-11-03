using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Rpa.Core.Record;
using Rpa.Core.Base.Resp;
using Google.Protobuf.Collections;
using II.RPA.Core.Provider.ScreenRecordProvider;
using Google.Protobuf.WellKnownTypes;
using II.RPA.Core.Provider.ScreenRecordProvider.FFmpeg;
using Google.Protobuf;
using II.RPA.Core.Abstract;
using Rpa.Core.Admin;
using Grpc.Core.Interceptors;
using II.RPA.Core.Log;

namespace II.RPA.Core.GRPCService.Service
{
    public class ScreenRecordGRPCService : ScreenRecordService.ScreenRecordServiceBase, IGRPCService
    {
        private readonly ILogger logger;
        private readonly ScreenRecordProvider screenRecordProvider;

        public ScreenRecordGRPCService(
            ILogger aLogger,
            ScreenRecordProvider aScreenRecordProvider)
        {
            this.logger = aLogger;
            this.screenRecordProvider = aScreenRecordProvider;
        }

        public ServerServiceDefinition BindingToService()
        {
            return ScreenRecordService.BindService(this);
        }

        public Response Ok<T>(string id, string clientId, T data) where T: IMessage => new Response
        {
            RequestId = id,
            Code = 200,
            IsSuccess = true,
            Multiple = false,
            Data = Any.Pack(data),
            ClientId = clientId
        };

        public Response OkList<T>(string id, string clientId, List<T> list) where T : IMessage
        {
            var resp = new Response
            {
                RequestId = id,
                Code = 200,
                IsSuccess = true,
                Multiple = true,
                ClientId = clientId
            };
            resp.List.AddRange(list.Select(i => Any.Pack(i)).ToList());
            return resp;
        }

        public Response NotFound(string id, string clientId) => new Response
        {
            RequestId = id,
            Code = 400,
            IsSuccess = false,
            Multiple = false,
            Message = "Object is missing",
            ClientId = clientId
        };

        public ScreenRecordInstance InstanceMapping(ScreenRecorder recorder) => new ScreenRecordInstance
        {
            Id = recorder.Options.Id,
            FileFullName = recorder.Options.FileFullName,
            Directory = recorder.Options.Directory,
            FileName = recorder.Options.FileName,
            FfmpegPath = recorder.Options.FFmpegPath,
            IsPending = recorder.IsWorking,
            Quality = (Quality)recorder.Options.Quality
        };

        public override Task<Response> GetUnary(GetRequest request, ServerCallContext context)
        {
            var recorder = screenRecordProvider.Get(request.Id);
            if (recorder == null)
            {
                return Task.FromResult(NotFound(request.RequestId, request.ClientId));
            }
            return Task.FromResult(Ok(request.RequestId, request.ClientId, InstanceMapping(recorder)));
        }

        public override Task<Response> GetPendingsUnary(GetPendingsRequest request, ServerCallContext context)
        {
            var recorders = screenRecordProvider.GetAll();
            return Task.FromResult(OkList(request.RequestId, request.ClientId, recorders.Select(r => InstanceMapping(r)).ToList()));
        }

        public override Task<Response> StartUnary(StartRequest request, ServerCallContext context)
        {
            var options = new VideoOptions
            {
                Id = request.Id,
                Directory = request.Directory,
                FileName = request.FileName,
                Quality = (VideoQuality)request.Quality,
                FFmpegPath = request.FfmpegPath,
            };
            var recorder = screenRecordProvider.Start(options);

            return Task.FromResult(Ok(request.RequestId, request.ClientId, InstanceMapping(recorder)));
        }

        public override Task<Response> StopUnary(StopRequest request, ServerCallContext context)
        {
            var recorder = screenRecordProvider.Stop(request.Id, request.CutSeconds);
            if (recorder == null)
            {
                return Task.FromResult(NotFound(request.RequestId, request.ClientId));
            }
            return Task.FromResult(Ok(request.RequestId, request.ClientId, InstanceMapping(recorder)));
        }
    }
}
