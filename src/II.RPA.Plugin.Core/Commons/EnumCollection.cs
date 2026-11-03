using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
//using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core
{

    public class EnumCollection
    {

    }
    [Flags]
    public enum AbilityState
    {
        None = 0,
        Runing = 1 << 0,
        NotRuning = 1 << 1
    }
    //public enum UIAModeType
    //{
    //    None = 0,
    //    UIA2 = 2,
    //    UIA3 = 3
    //}
    public enum JavaCaptureModeType
    {
        /// <summary>
        /// 直接根据点获取（应用库提供的接口）
        /// </summary>
        Direct = 0,
        /// <summary>
        /// 一层一层
        /// </summary>
        LayerByLayer = 1
    }
    /// <summary>
    /// 拾取节点的类型
    /// </summary>
    public enum InspectNodeType
    {
        /// <summary>
        /// 根据点 从窗口中获取
        /// </summary>
        FromWindowAndPoint = 0,
        /// <summary>
        /// 根据点 从指定的元素中获取
        /// </summary>
        FromElementAndPoint = 1
    }
    /// <summary>
    /// 数据采集内容类型
    /// </summary>
    [Flags]
    public enum CollectType
    {
        [Collect("innerText")]
        Content = 1 << 0,
        [Collect("href")]
        Href = 1 << 1,
        [Collect("src")]
        Src = 1 << 2
    }
    /// <summary>
    ///拾取元素类型
    /// </summary>
    public enum CaptureElementType
    {
        None = 0,
        PagingButtonElement = 1,
        SimilarElement = 2,
        DataCollectingElement = 3
    }
    /// <summary>
    ///终端类型
    /// </summary>
    [Flags]
    public enum TerminalType
    {
        Nonw = 0,
        Engine = 1 << 0,
        Factory = 1 << 1,
        CoreServer = 1 << 2,
        Bot = 1 << 3
    }

    /// <summary>
    ///数据采集拾取操作状态
    /// </summary>
    public enum DataCollectionCaptureStatusType
    {
        Success = 0,
        UserCancel = 100,//程序中1、2等已有统一含义
        NonSamePage = 101,//程序中1、2等已有统一含义
        OtherError = 200
    }

    /// <summary>
    /// 数据采集 翻页类型
    /// </summary>
    public enum TurnPageConfigType : int
    {
        AllPage = 0,
        CurrentPage = 1,
        CustomPage = 2
    }
    /// <summary>
    /// 元素相对类型
    /// </summary>
    public enum ElementRelativeType
    {
        Self = 0,
        Similar = 1,
        Parent = 2,
        Children = 3,
        Siblings = 4,
    }
    public enum ElementMatchingMode
    {
        Normal = 0,
        Advanced = 1
    }
    /// <summary>
    /// 文档的加载状态
    /// </summary>
    [Flags]
    public enum DocumentReadyState
    {
        Loading = 1 << 0, //加载中

        Interactive = 1 << 1, //结束渲染，用户已经可以与网页进行交互。但内嵌资源还在加载中

        Complete = 1 << 2, //完全加载完成
    }
    [Flags]
    public enum PageLoadStatus
    {
        Loading = 1 << 0,

        Success = 1 << 1,

        Error = 1 << 2,
    }
    public enum ClientType
    {
        [Description("Z-Factory")]
        Factory = 0,
        [Description("Z-Bot")]
        Bot = 1,
        [Description("Z-DeskTop")]
        DeskTop = 2,
        [Description("Z-Admin")]
        Admin = 3,
        INVALID = 99,
    }
    [Flags]
    public enum BrowserConnType
    {
        None = 0,
        ServiceHub = 1 << 1,
        NativeMessageHost = 1 << 2,
    }
    [Flags]
    public enum ElementMatchType
    {
        None = 0,
        Normal = 1 << 1,
        AiRepair = 1 << 2
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProcessingType
    {
        None = 0,
        Prefix = 1,
        Suffix = 2,
        Trim = 3,
        Replace = 4,
        ExtractNum = 5,
        RemoveLineBreaks = 6,
        FormatTime = 7,
        Regular = 8
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DataFilterLogicalType
    {
        [EnumMember(Value = "and")]
        And = 0,
        [EnumMember(Value = "or")]
        Or = 1
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DataFilterType
    {
        [EnumMember(Value = "conditions")]
        Conditions = 0,
        [EnumMember(Value = "regulars")]
        Regulars = 1
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PatternType
    {
        [EnumMember(Value = "none")]
        None = 0,
        [EnumMember(Value = "equel")]
        Equel = 1,
        [EnumMember(Value = "unequal")]
        Unequal = 2,
        [EnumMember(Value = "greaterThan")]
        GreaterThan = 3,
        [EnumMember(Value = "lessThan")]
        LessThan = 4,
        [EnumMember(Value = "greaterEquel")]
        GreaterEquel = 5,
        [EnumMember(Value = "lessEquel")]
        LessEquel = 6,
        [EnumMember(Value = "contain")]
        Contain = 7,
        [EnumMember(Value = "notContain")]
        NotContain = 8,
        [EnumMember(Value = "isEmpty")]
        IsEmpty = 9,
        [EnumMember(Value = "notEmpty")]
        NotEmpty = 10,
        [EnumMember(Value = "startWith")]
        StartWith = 11,
        [EnumMember(Value = "notStartWith")]
        NotStartWith = 12,
        [EnumMember(Value = "endWith")]
        EndWith = 13,
        [EnumMember(Value = "notEndWith")]
        NotEndWith = 14,
        [EnumMember(Value = "dateBefore")]
        DateBefore = 15,
        [EnumMember(Value = "dateAfter")]
        DateAfter = 16,
        [EnumMember(Value = "dateBetween")]
        DateBetween = 17,
        [EnumMember(Value = "test")]
        RegularTest = 18
    }
    public enum AidKeyType
    {
        None = 0,
        Alt = 1,
        Ctrl = 2,
        Shift = 3,
        Win = 4
    }

    [Flags]
    public enum RunningState
    {
        None = 0,
        Running = 1 << 0,
        Paused = 1 << 1
    }
}
