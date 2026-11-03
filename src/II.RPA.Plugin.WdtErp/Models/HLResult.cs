using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace II.RPA.Plugin.WdtErp
{

    [Serializable]
    public class HLResult
    {
        public int ErrNo { get; set; } = 5;
        public string ErrMsg { get; set; }
        public string Data { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    enum HLDOM_RESULT
    {
        [Description("执行成功")]
        HLDOM_OK = 0,

        [Description("未找到指定的窗口")]
        HLDOM_INVALID_HWND = 1,

        [Description("未找到指定的元素")]
        HLDOM_INVALID_HANDLE = 2,

        [Description("未标记的元素")]
        HLDOM_PASSIVE_HANDLE = 3,

        [Description("参数无效")]
        HLDOM_INVALID_PARAMETER = 4,

        [Description("操作失败")]
        HLDOM_OPERATION_FAILED = 5,

        [Description("初始化失败")]
        HLDOM_INIT_FAILED = 6
    }
}
