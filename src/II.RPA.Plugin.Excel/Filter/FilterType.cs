using System;


namespace II.RPA.Plugin.Excel.Filter
{
    /// <summary>
    /// 过滤器类型
    /// </summary>
    [Flags]
    internal enum FilterType
    {
        None = 0,
        //执行前
        Executing = 0x1,
        //执行后
        Executed = 0x2,
        //执行结果
        Result = 0x4,
        //异常
        Exception = 0x8
    }
}
