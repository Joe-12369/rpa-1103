using Rpa.Core.Mobile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.GrpcService.Interface
{
    public interface IMobileService
    {
        Task<MobileResponse> CaptureMobile(MobileRequest request, System.Threading.CancellationToken cancellationToken = default);
        Task<MobileResponse> GetPoint(MobileRequest request, System.Threading.CancellationToken cancellationToken = default);
        Task<MobileResponse> CheckMobile(MobileRequest request);
        Task<MobileResponse> ActiveMobileWind(MobileRequest request);
        Task<MobileResponse> CloseMobileDeviceApp(MobileRequest request);
        Task<MobileResponse> GetCurrentAppPackageName(MobileRequest request);
        Task<MobileResponse> CaptureMobileElementShot(MobileRequest request);
        Task<MobileResponse> CaptureDomainScope(MobileRequest request);
        Task<MobileResponse> CheckMobileImage(MobileRequest request);
        Task<MobileResponse> TestSlid(MobileRequest request);
    }
}
