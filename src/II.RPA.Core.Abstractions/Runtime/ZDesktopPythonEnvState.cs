namespace II.RPA.Core.Runtime;

/// <summary>
/// 小窗（章鱼桌面）侧 Python 运行环境的跨组件状态。
///
/// 背景：虚拟运行时设计器会先下发 SwitchPythonEnv(withinZdesktop=true)，而此时小窗
/// （及其子 CoreServer）可能还没启动，父核无法把请求转发出去。为了不打断运行流程，
/// 这里记录“期望小窗使用的版本”，由小窗启动参数（ZDesktopProcess 的 --python_env）
/// 或子核就绪后的补发来最终生效。
///
/// 注意：该状态与父核自身的 Global.PythonEnvVersion 相互独立，父核自己的 engine
/// worker 使用哪套 Python 环境不受虚拟运行影响。
/// </summary>
public static class ZDesktopPythonEnvState
{
    private static readonly object SyncRoot = new();

    private static string _desiredVersion = string.Empty;
    private static string _appliedVersion = string.Empty;

    /// <summary>期望小窗侧使用的 Python 环境版本（空串表示尚未收到过虚拟运行的切换请求）。</summary>
    public static string DesiredVersion
    {
        get
        {
            lock (SyncRoot)
            {
                return _desiredVersion;
            }
        }
    }

    /// <summary>已确认下发到子 CoreServer 的版本。</summary>
    public static string AppliedVersion
    {
        get
        {
            lock (SyncRoot)
            {
                return _appliedVersion;
            }
        }
    }

    /// <summary>记录期望版本：虚拟运行时会先记录，再尝试下发给子 CoreServer。</summary>
    public static void SetDesired(string? version)
    {
        lock (SyncRoot)
        {
            _desiredVersion = version ?? string.Empty;
        }
    }

    /// <summary>标记该版本已成功下发到子 CoreServer。</summary>
    public static void MarkApplied(string? version)
    {
        lock (SyncRoot)
        {
            _appliedVersion = version ?? string.Empty;
        }
    }

    /// <summary>小窗退出（子核随之重建）后调用，保证子核下次就绪时会重新应用。</summary>
    public static void ClearApplied()
    {
        lock (SyncRoot)
        {
            _appliedVersion = string.Empty;
        }
    }

    /// <summary>是否存在“已记录但尚未确认生效”的版本。</summary>
    public static bool NeedsApply()
    {
        lock (SyncRoot)
        {
            return !string.IsNullOrWhiteSpace(_desiredVersion)
                && !string.Equals(_desiredVersion, _appliedVersion, StringComparison.Ordinal);
        }
    }
}
