using System;
using System.Runtime.InteropServices;

namespace II.RPA.Plugin.SAP.Utils
{
    /// <summary>
    /// SAP COM 对象安全释放工具。
    /// 处理 SAP 进程已退出等异常场景，确保释放流程不中断。
    /// </summary>
    public static class SapComReleaser
    {
        /// <summary>
        /// 安全释放单个 COM 对象，并在释放后强制将其置为 null。
        /// 使用 Marshal.ReleaseComObject 而非 FinalReleaseComObject，
        /// 避免在复杂树形结构中误杀仍在被其他变量使用的 COM 对象。
        /// </summary>
        /// <typeparam name="T">COM 对象类型（必须是 class）</typeparam>
        /// <param name="comObject">需要释放的对象引用</param>
        public static void Release<T>(ref T comObject) where T : class
        {
            if (comObject == null) return;

            try
            {
                if (Marshal.IsComObject(comObject))
                {
                    Marshal.ReleaseComObject(comObject);
                }
            }
            catch (Exception)
            {
                // 静默处理：SAP 客户端可能已被用户关闭（RPC_E_DISCONNECTED 等）
            }
            finally
            {
                comObject = null;
            }
        }

        /// <summary>
        /// 仅执行安全释放，不强制置空引用。
        /// 适用于通过属性返回的 COM 对象。
        /// </summary>
        /// <param name="comObject">需要释放的对象引用</param>
        public static void SafeRelease(object comObject)
        {
            if (comObject == null) return;

            try
            {
                if (Marshal.IsComObject(comObject))
                {
                    Marshal.ReleaseComObject(comObject);
                }
            }
            catch
            {
                // 静默处理
            }
        }
    }
}
