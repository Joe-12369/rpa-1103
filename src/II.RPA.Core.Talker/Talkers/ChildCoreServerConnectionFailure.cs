using Grpc.Core;
using System.IO;

namespace II.RPA.Core.Talker.Talkers
{
    /// <summary>
    /// 区分"子核还没就绪"这类可预期的失败与真正的故障。
    /// 子核由子会话登录后拉起，启动期间父核必然经历"端口文件还没写入 / gRPC 连不上"，
    /// 这些由 2s 重试循环负责，不应该按错误记录（否则每次启动小窗都会刷一堆错误堆栈，
    /// 真正的故障反而被淹没）。
    /// </summary>
    internal static class ChildCoreServerConnectionFailure
    {
        /// <summary>端口文件未写入/内容非法，或子核 gRPC 暂时不可达。</summary>
        internal static bool IsNotReady(Exception exception)
        {
            switch (exception)
            {
                case FileNotFoundException:
                case DirectoryNotFoundException:
                case InvalidDataException:
                    return true;
                case RpcException rpc:
                    return rpc.StatusCode == StatusCode.Unavailable;
                default:
                    return false;
            }
        }
    }
}
