using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using Newtonsoft.Json;
using System;

namespace II.RPA.Plugin.Excel.Models
{
    public class ExcelRule
    {
        public dynamic rawField;
    }
    public class ExcelRange
    {
        /// <summary>
        /// 区域模式（单元格、行、列、区域、已编辑区域）,displayType为 chooseRange 时没有该选项
        /// </summary>
        public int? option { get; set; } = 0;
        /// <summary>
        /// 区域模式（0普通、1高级）
        /// </summary>
        public int type { get; set; } = 0;
        /// <summary>
        /// 开始列
        /// </summary>
        public string startColumn { get; set; } = "A";
        /// <summary>
        /// 结束列
        /// </summary>
        public string lastColumn { get; set; } = "A";
        /// <summary>
        /// 开始行
        /// </summary>
        public int startRow { get; set; }
        /// <summary>
        /// 结束行
        /// </summary>
        public int lastRow { get; set; }
        /// <summary>
        /// 高级模式的值
        /// </summary>
        public string range { get; set; }
        /// <summary>
        /// 列名
        /// </summary>
        public string column { get; set; } = "A";
        /// <summary>
        /// 行号
        /// </summary>
        public int row { get; set; }

        [JsonIgnore]
        public string OptionText
        {
            get
            {
                switch (option)
                {
                    case 0: return "单元格";
                    case 1: return "行";
                    case 2: return "列";
                    case 3: return "区域";
                    case 4: return "已编辑区域";
                }
                throw new ExcelException("无效的区域选项".I18n("ExcelRange_A1D3A2D0"));
            }
            private set { }
        }
    }
}
