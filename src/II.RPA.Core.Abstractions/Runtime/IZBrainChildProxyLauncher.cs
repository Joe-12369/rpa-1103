namespace II.RPA.Core.Runtime;

/// <summary>
/// 子会话 ZBrain HTTP 代理（ChildStartup，固定 6442 端口）的生命周期入口。
/// 由主 CoreServer 在子会话 z_brain 注册时启动，小窗(ZDesktop)退出时停止。
/// </summary>
public interface IZBrainChildProxyLauncher
{
    void StartChildProxy();

    void StopChildProxy();
}
