using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RobotLiteControls
{
    public class InputModel
    {
        public string Name { get; set; }
        public string Label { get; set; }
        [JsonRequired]
        public string Type { get; set; }
        /// <summary>
        /// 选项值
        /// </summary>
        public string[] Options { get; set; }
        [JsonRequired]
        public string Number { get; set; }
        /// <summary>
        /// 默认值
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 默认值(多选、多选下拉)
        /// </summary>
        public string[] ValueArray { get; set; }
        /// <summary>
        /// 默认值（开关）
        /// </summary>
        public int ValueNumber { get; set; }
        /// <summary>
        /// 默认值（表格）
        /// </summary>
        public DataTable ValueTable { get; set; }
        /// <summary>
        /// 提示（文本框提示）
        /// </summary>
        public string Tips { get; set; }
        /// <summary>
        /// 字体大小
        /// </summary>
        public int FontSize { get; set; }
        /// <summary>
        /// 字体类型（常规Normal  细体Ligh 粗体Bold 黑体Black）
        /// </summary>
        public string FontWeight { get; set; }
        /// <summary>
        /// 字体
        /// </summary>
        public string FontFamily { get; set; }
        /// <summary>
        /// 选择(单选框纵横、复选框纵横 1横向 2 纵向 )
        /// </summary>
        public int Direction { get; set; }
        /// <summary>
        /// 文本域高度
        /// </summary>
        public double Height { get; set; }
        /// <summary>
        /// 文件还是文件夹（1文件、2文件夹）
        /// </summary>
        public string FileOrFolder { get; set; }
        /// <summary>
        /// 行
        /// </summary>
        public int Row { get; set; }
        /// <summary>
        /// 列
        /// </summary>
        public string[] Column { get; set; }
        /// <summary>
        /// 线条颜色
        /// </summary>
        public string Color { get; set; }
        /// <summary>
        /// 时间格式
        /// </summary>
        public string DateTimeFormat { get; set; }
    }

    public class OutputModel
    {
        [JsonRequired]
        public string name { get; set; }
        [JsonRequired]
        public string value { get; set; }
    }
    public enum emInputType
    {
        [EnumMember(Value = "input")]
        input,
        [EnumMember(Value = "radio")]
        radio,
        [EnumMember(Value = "checkbox")]
        checkbox,
        [EnumMember(Value = "comboBox")]
        comboBox
    }
}
