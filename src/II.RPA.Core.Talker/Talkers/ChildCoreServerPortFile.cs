using System;
using System.IO;

namespace II.RPA.Core.Talker.Talkers
{
    /// <summary>
    /// 小窗（章鱼桌面）内子 CoreServer 的端口文件。
    ///
    /// 文件由子核启动后写入，父核读它来定位子核的 gRPC 端口；子核退出时不会清理该文件。
    /// 因此每次启动小窗前都必须删除，否则父核可能连到"上一会话残留、正在退出"的子核端口：
    /// 表现为连接瞬间建立又立刻断开，run 转发失败并回给引擎"章鱼桌面还未启动"。
    /// </summary>
    internal static class ChildCoreServerPortFile
    {
        /// <summary>端口文件路径：%APPDATA%\Z-Core\Z-DeskTop.txt。</summary>
        internal static string FullPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Z-Core",
            "Z-DeskTop.txt");

        /// <summary>
        /// 删除残留的端口文件。
        /// 返回是否处理成功（文件本就不存在也算成功），失败原因通过 <paramref name="error"/> 返回。
        /// </summary>
        internal static bool TryClear(out string error)
        {
            return TryClear(FullPath, out error);
        }

        /// <summary>删除指定路径的端口文件（供测试注入临时路径）。</summary>
        internal static bool TryClear(string path, out string error)
        {
            error = null;
            try
            {
                if (!File.Exists(path))
                {
                    return true;
                }

                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
