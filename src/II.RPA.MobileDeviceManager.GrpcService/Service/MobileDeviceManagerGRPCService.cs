using Grpc.Core;
using II.RPA.MobileDeviceManager.GrpcService.Interface;
using Rpa.Core.Mobile;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.GrpcService.Service
{
    public class MobileDeviceManagerGRPCService : MobileDeviceManagerService.MobileDeviceManagerServiceBase
    {
        private IMobileService _mobileService;
        public MobileDeviceManagerGRPCService(IMobileService mobileService)
        {
            _mobileService = mobileService;
        }
        public override Task<MobileResponse> CaptureMobileUnary(MobileRequest request, ServerCallContext context)
        {
            return _mobileService.CaptureMobile(request, context.CancellationToken);
        }
        public override Task<MobileResponse> CheckMobileUnary(MobileRequest request, ServerCallContext context)
        {
            return _mobileService.CheckMobile(request);
        }

        public override Task<MobileResponse> GetConnectStatusUnary(MobileRequest request, ServerCallContext context)
        {
            return base.GetConnectStatusUnary(request, context);
        }
        public override Task<MobileRequest> GetDeviceInfoUnary(MobileRequest request, ServerCallContext context)
        {
            return base.GetDeviceInfoUnary(request, context);
        }
        public override Task<MobileResponse> GetPluginStatusUnary(MobileRequest request, ServerCallContext context)
        {
            return base.GetPluginStatusUnary(request, context);
        }
        public override Task<MobileResponse> GetPointUnary(MobileRequest request, ServerCallContext context)
        {
            return _mobileService.GetPoint(request, context.CancellationToken);
        }
        public override Task<Message> PluginInstallResultToClintUnary(Message request, ServerCallContext context)
        {
            return base.PluginInstallResultToClintUnary(request, context);
        }
        public override Task<MobileResponse> StartDeviceUnary(MobileRequest request, ServerCallContext context)
        {
            return _mobileService.ActiveMobileWind(request);
        }
        public override Task SubMessageInfoServerStreaming(Message request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            return base.SubMessageInfoServerStreaming(request, responseStream, context);
        }
        public override Task<MobileResponse> CloseDeviceUnary(MobileRequest request, ServerCallContext context)
        {
            return _mobileService.CloseMobileDeviceApp(request);
        }
        public override Task<MobileResponse> GetCurrentAppPackageNameUnary(MobileRequest request, ServerCallContext context)
        {
            return _mobileService.GetCurrentAppPackageName(request);
        }
        public override Task<MobileResponse> CaptureMobileElementShotUnary(MobileRequest request, ServerCallContext context)
        {
            return _mobileService.CaptureMobileElementShot(request);
        }
        public override Task<MobileResponse> CaptureDomainScopeUnary(MobileRequest request, ServerCallContext context)
        {
            return _mobileService.CaptureDomainScope(request);
        }
        public override Task<MobileResponse> CheckMobileImageUnary(MobileRequest request, ServerCallContext context)
        {
            return _mobileService.CheckMobileImage(request);
        }
        public override Task<MobileResponse> TestSlidUnary(MobileRequest request, ServerCallContext context)
        {
            return _mobileService.TestSlid(request);
        }
    }
}
