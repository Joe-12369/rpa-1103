using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Dialog.Converts;
using II.RPA.Plugin.Dialog.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Dialog
{
    public class SheetData
    {
        [JsonProperty("sheetName")]
        public string SheetName { get; set; }
        [JsonProperty("data")]
        public string[,] Data { get; set; }
    }
    public class InputModel
    {
        [PropertyAnnotation("名称","MultiInputModel_230B384E")]
        public string Name { get; set; }
        [PropertyAnnotation("标题","MultiInputModel_33AFF448")]
        public string Label { get; set; }
        [JsonRequired, PropertyAnnotation("类型", "MultiInputModel_1951D753")]
        public string Type { get; set; }
        /// <summary>
        /// 选项值
        /// </summary>
        [PropertyAnnotation("选项","MultiInputModel_223B1ABF")]
        public string[] Options { get; set; }
        [PropertyAnnotation("序号","MultiInputModel_C5C655CB")]
        public string Number { get; set; }
        /// <summary>
        /// 默认值
        /// </summary>
        [PropertyAnnotation("默认值","MultiInputModel_7428CE67")]
        public string Value { get; set; }
        /// <summary>
        /// 默认值(多选、多选下拉)
        /// </summary>
        [PropertyAnnotation("默认值","MultiInputModel_7428CE67")]
        [JsonProperty("valueArray")]
        [JsonConverter(typeof(ValueArrrayJsonConverter))]
        public string[] ValueArray { get; set; }
        /// <summary>
        /// 默认值（开关）
        /// </summary>
        [PropertyAnnotation("默认值","MultiInputModel_7428CE67")]
        public int ValueNumber { get; set; }
        /// <summary>
        /// 默认值（表格）
        /// </summary>
        [PropertyAnnotation("默认值","MultiInputModel_7428CE67")]
        public DataTable ValueTable { get; set; }
        /// <summary>
        /// 提示（文本框提示）
        /// </summary>
        [PropertyAnnotation("空白提示","MultiInputModel_12C2CF84")]
        public string Tips { get; set; }
        /// <summary>
        /// 字体大小
        /// </summary>
        [PropertyAnnotation("字体大小","MultiInputModel_6844F9A5")]
        public int FontSize { get; set; }
        /// <summary>
        /// 字体类型（常规Normal  细体Ligh 粗体Bold 黑体Black）
        /// </summary>
        [PropertyAnnotation("字体属性","MultiInputModel_924DDFFA")]
        public string FontWeight { get; set; }
        /// <summary>
        /// 字体
        /// </summary>
        [PropertyAnnotation("字体选择","MultiInputModel_B26AFA9")]
        public string FontFamily { get; set; }
        /// <summary>
        /// 选择(单选框纵横、复选框纵横 1横向 2 纵向 )
        /// </summary>
        [PropertyAnnotation("方向","MultiInputModel_C27047")]
        public int Direction { get; set; }
        /// <summary>
        /// 文本域高度
        /// </summary>
        [PropertyAnnotation("文本域高度","MultiInputModel_94BC6D55")]
        public double Height { get; set; }
        /// <summary>
        /// 文件还是文件夹（1文件、2文件夹）
        /// </summary>
        [PropertyAnnotation("文件模式","MultiInputModel_8C5FBA52")]
        public string FileOrFolder { get; set; }
        /// <summary>
        /// 行
        /// </summary>
        [PropertyAnnotation("行数","MultiInputModel_ADCE9C79")]
        public int Row { get; set; }
        /// <summary>
        /// 列
        /// </summary>
        [PropertyAnnotation("列","MultiInputModel_E373A77C")]
        public string[] Column { get; set; }
        /// <summary>
        /// 线条颜色
        /// </summary>
        [PropertyAnnotation("线条颜色","MultiInputModel_7B944F43")]
        public string Color { get; set; }
        /// <summary>
        /// 时间格式
        /// </summary>
        [PropertyAnnotation("日期类型","MultiInputModel_5ECC796B")]
        public string DateTimeFormat { get; set; }
        /// <summary>
        /// 是否必填    1是，0否
        /// </summary>
        [PropertyAnnotation("是否必填","MultiInputModel_5AE40202")]
        public string IsRequired { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [PropertyAnnotation("Sheet名称","MultiInputModel_92FDEAAD")]
        public string SheetName { get; set; }

        [JsonProperty("valueArrayUpgrade")]
        public IEnumerable<FileInfoModel> ValueArrayUpgrade { get; set; }
    }

    public class OutputModel
    {
        [JsonRequired]
        public string name { get; set; }
        [JsonRequired]
        public string value { get; set; }
    }

    public class FileInfoModel
    {
        [JsonProperty("id")]
        public string Id
        {
            get;
            set;
        }
        [JsonProperty("folderId")]
        public string FolderId
        {
            get;
            set;
        }
        [JsonProperty("fileId")]
        public string FileId
        {
            get;
            set;
        }
        [JsonProperty("name")]
        public string Name
        {
            get;
            set;
        }
        /// <summary>
        /// 目前最大值100M
        /// </summary>
        [JsonProperty("fileSize")]
        public long FileSize
        {
            get;
            set;
        } = -1;
        [JsonProperty("md5")]
        public string MD5
        {
            get;
            set;
        }
        [JsonProperty("filePath")]
        public string FilePath
        {
            get;
            set;
        }
        /// <summary>
        /// local, cloud (预览的时候显示类型)
        /// </summary>
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public FileType Type
        {
            get;
            set;
        }
        [JsonProperty("timestamp")]
        public string TimeStamp
        {
            get;
            set;
        }
        [JsonProperty("reseiveSize")]
        public long ReseiveSize
        {
            get;
            set;
        }
    }
    [Flags]
    public enum FileType
    {
        Local,
        Cloud,
    }
}
