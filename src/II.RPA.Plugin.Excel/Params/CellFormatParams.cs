using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Params
{
    /// <summary>
    /// 单元格格式参数
    /// </summary>
    public class CellFormatParams
    {
        /// <summary>
        /// 字体 需加个默认选项 以方便部署电脑
        /// </summary>
        public string FontFormat { get; set; } = "默认";

        /// <summary>
        /// 字号
        /// </summary>
        public double? FontSize { get; set; } = 12.0;

        /// <summary>
        /// 粗体斜体
        /// </summary>
        public EnumBoldAndItalic BoldAndItalic { get; set; } = EnumBoldAndItalic.General;

        /// <summary>
        /// 字体颜色
        /// </summary>
        public string FontColor { get; set; } = "#000000";

        /// <summary>
        /// 背景颜色
        /// </summary>
        public string BackgroundColor { get; set; } = "#FFFFFF,100%";

        /// <summary>
        /// 文本控制
        /// </summary>
        public int TextControl { get; set; }

        /// <summary>
        /// 水平对齐
        /// </summary>
        public EnumHorizontalAlignment HorizontalAlignment { get; set; } = EnumHorizontalAlignment.General;

        /// <summary>
        /// 垂直对齐
        /// </summary>
        public EnumVerticalAlignment VerticalAlignment { get; set; } = EnumVerticalAlignment.Center;

        /// <summary>
        /// 数值格式
        /// </summary>
        public DigitalFormatClass DigitalFormat { get; set; } = new DigitalFormatClass();

        public Border[] Borders = new Border[8];

        #region 可视化 水平对齐 垂直对齐 参数转换

        /// <summary>
        /// GetHorizontalParamOfInteropExcel 水平对齐
        /// </summary>
        /// <param name="enumHorizontalAlignment"></param>
        /// <returns></returns>
        #region 水平对齐 EnumHorizontalAlignment 转 Interop.Excel.XlHAlign
        public dynamic GetHorizontalParamOfInteropExcel(EnumHorizontalAlignment enumHorizontalAlignment)
        {
            //https://learn.microsoft.com/en-us/office/vba/api/excel.xlhalign
            int xlHAlign = 1;

            switch (enumHorizontalAlignment)
            {
                case EnumHorizontalAlignment.General:
                    xlHAlign = 1;
                    break;
                case EnumHorizontalAlignment.Left:
                    xlHAlign = -4131;
                    break;
                case EnumHorizontalAlignment.Center:
                    xlHAlign = -4108;
                    break;
                case EnumHorizontalAlignment.Right:
                    xlHAlign = -4152;
                    break;
                case EnumHorizontalAlignment.Fill:
                    xlHAlign = 5;
                    break;
                case EnumHorizontalAlignment.Justify:
                    xlHAlign = -4130;
                    break;
                case EnumHorizontalAlignment.CenterAcrossSelection:
                    xlHAlign = 7;
                    break;
                case EnumHorizontalAlignment.Distributed:
                    xlHAlign = -4117;
                    break;
                default:
                    xlHAlign = 1;
                    break;
            }
            return xlHAlign;
        }
        #endregion

        /// <summary>
        /// GetEnumHorizontalAlignment  水平对齐 EnumHorizontalAlignment 转 Interop.Excel.XlHAlign 
        /// </summary>
        /// <param name="xlHAlign"></param>
        /// <returns></returns>
        #region 水平对齐 Interop.Excel.XlHAlign 转 EnumHorizontalAlignment
        public EnumHorizontalAlignment GetEnumHorizontalAlignment(dynamic xlHAlign)
        {
            //https://learn.microsoft.com/en-us/office/vba/api/excel.xlhalign
            EnumHorizontalAlignment enumHorizontalAlignment = EnumHorizontalAlignment.General;
            int enumValue = (int)xlHAlign;
            switch (enumValue)
            {
                case 1:
                    enumHorizontalAlignment = EnumHorizontalAlignment.General;
                    break;
                case -4131:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Left;
                    break;
                case -4108:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Center;
                    break;
                case -4152:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Right;
                    break;
                case 5:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Fill;
                    break;
                case -4130:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Justify;
                    break;
                case 7:
                    enumHorizontalAlignment = EnumHorizontalAlignment.CenterAcrossSelection;
                    break;
                case -4117:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Distributed;
                    break;
                default:
                    enumHorizontalAlignment = EnumHorizontalAlignment.General;
                    break;
            }
            return enumHorizontalAlignment;
        }
        #endregion

        /// <summary>
        ///  GetVerticalmentParamOfInteropExcel 垂直对齐 EnumVerticalAlignment 转 Interop.Excel.XlVAlign
        /// </summary>
        /// <param name="enumVerticalAlignment"></param>
        /// <returns></returns>
        #region 垂直对齐 EnumVerticalAlignment 转 Interop.Excel.XlVAlign
        public dynamic GetVerticalParamOfInteropExcel(EnumVerticalAlignment enumVerticalAlignment)
        {
            //https://learn.microsoft.com/en-us/office/vba/api/excel.xlvalign
            dynamic xlVAlign = -4108;
            switch (enumVerticalAlignment)
            {
                case EnumVerticalAlignment.Center:
                    xlVAlign = -4108;
                    break;
                case EnumVerticalAlignment.Top:
                    xlVAlign = -4160;
                    break;
                case EnumVerticalAlignment.Bottom:
                    xlVAlign = -4107;
                    break;
                case EnumVerticalAlignment.Justify:
                    xlVAlign = -4130;
                    break;
                case EnumVerticalAlignment.Distributed:
                    xlVAlign = -4117;
                    break;
                case EnumVerticalAlignment.Others:
                    xlVAlign = -4108;
                    break;
                default:
                    xlVAlign = -4108;
                    break;
            }
            return xlVAlign;
        }
        #endregion

        /// <summary>
        /// GetEnumVerticalAlignmentFromXlVAlign 垂直对齐  Interop.Excel.XlVAlign  转  EnumVerticalAlignment
        /// </summary>
        /// <param name="xlHAlign"></param>
        /// <returns></returns>
        #region 垂直对齐 Interop.Excel.XlVAlign 转  EnumVerticalAlignment
        public EnumVerticalAlignment GetEnumVerticalAlignmentFromXlVAlign(dynamic xlVAlign)
        {
            //https://learn.microsoft.com/en-us/office/vba/api/excel.xlvalign
            EnumVerticalAlignment enumVerticalAlignment = EnumVerticalAlignment.Center;
            int enumValue = (int)xlVAlign;
            switch (xlVAlign)
            {
                case -4108:
                    enumVerticalAlignment = EnumVerticalAlignment.Center;
                    break;
                case -4160:
                    enumVerticalAlignment = EnumVerticalAlignment.Top;
                    break;
                case -4107:
                    enumVerticalAlignment = EnumVerticalAlignment.Bottom;
                    break;
                case -4130:
                    enumVerticalAlignment = EnumVerticalAlignment.Justify;
                    break;
                case -4117:
                    enumVerticalAlignment = EnumVerticalAlignment.Distributed;
                    break;
                default:
                    enumVerticalAlignment = EnumVerticalAlignment.Center;
                    break;
            }
            return enumVerticalAlignment;
        }
        #endregion

        #endregion

        #region 非可视化 水平对齐 垂直对齐 参数转换

        /// <summary>
        /// 获取 Spreadsheet水平对齐
        /// </summary>
        /// <param name="enumHorizontalAlignment"></param>
        /// <returns></returns>
        #region GetSpreadsheetHorAlignment
        public DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment GetSpreadsheetHorAlignment(EnumHorizontalAlignment enumHorizontalAlignment)
        {
            DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment spreadsheetHorizontalAlignment =
                default(DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment);
            switch (enumHorizontalAlignment)
            {
                case EnumHorizontalAlignment.Center:
                    spreadsheetHorizontalAlignment = DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Center;
                    break;
                case EnumHorizontalAlignment.Left:
                    spreadsheetHorizontalAlignment = DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Left;
                    break;
                case EnumHorizontalAlignment.Right:
                    spreadsheetHorizontalAlignment = DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Left;
                    break;
                case EnumHorizontalAlignment.General:
                    spreadsheetHorizontalAlignment = DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.General;
                    break;
                case EnumHorizontalAlignment.Fill:
                    spreadsheetHorizontalAlignment = DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Fill;
                    break;
                case EnumHorizontalAlignment.Justify:
                    spreadsheetHorizontalAlignment = DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Justify;
                    break;
                case EnumHorizontalAlignment.CenterAcrossSelection:
                    spreadsheetHorizontalAlignment = DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.CenterContinuous;
                    break;
                case EnumHorizontalAlignment.Distributed:
                    spreadsheetHorizontalAlignment = DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Distributed;
                    break;
                case EnumHorizontalAlignment.Others:
                    spreadsheetHorizontalAlignment = DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.General;
                    break;
                default:
                    spreadsheetHorizontalAlignment = DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.General;
                    break;
            }
            return spreadsheetHorizontalAlignment;
        }
        #endregion

        /// <summary>
        /// 获取 水平对齐
        /// </summary>
        /// <param name="enumHorizontalAlignment"></param>
        /// <returns></returns>
        #region GetEnumHorizontalAlignmentNonVisual
        public EnumHorizontalAlignment GetEnumHorizontalAlignmentNonVisual(DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment spreadsheetHorAlignment)
        {
            EnumHorizontalAlignment enumHorizontalAlignment = default(EnumHorizontalAlignment);
            switch (spreadsheetHorAlignment)
            {
                case DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Center:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Center;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Left:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Left;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Right:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Right;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.General:
                    enumHorizontalAlignment = EnumHorizontalAlignment.General;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Fill:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Fill;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Justify:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Justify;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.CenterContinuous:
                    enumHorizontalAlignment = EnumHorizontalAlignment.CenterAcrossSelection;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetHorizontalAlignment.Distributed:
                    enumHorizontalAlignment = EnumHorizontalAlignment.Distributed;
                    break;
                default:
                    enumHorizontalAlignment = EnumHorizontalAlignment.General;
                    break;
            }
            return enumHorizontalAlignment;
        }
        #endregion

        #region GetSpreadsheetVerticalAlignment 获取Spreadsheet垂直对齐
        public DevExpress.Spreadsheet.SpreadsheetVerticalAlignment GetSpreadsheetVerticalAlignment(EnumVerticalAlignment enumVerticalAlignment)
        {
            DevExpress.Spreadsheet.SpreadsheetVerticalAlignment spreadsheetVerticalAlignment =
                default(DevExpress.Spreadsheet.SpreadsheetVerticalAlignment);
            switch (enumVerticalAlignment)
            {
                case EnumVerticalAlignment.Top:
                    spreadsheetVerticalAlignment = DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Top;
                    break;
                case EnumVerticalAlignment.Center:
                    spreadsheetVerticalAlignment = DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Center;
                    break;
                case EnumVerticalAlignment.Bottom:
                    spreadsheetVerticalAlignment = DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Bottom;
                    break;
                case EnumVerticalAlignment.Justify:
                    spreadsheetVerticalAlignment = DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Justify;
                    break;
                case EnumVerticalAlignment.Distributed:
                    spreadsheetVerticalAlignment = DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Distributed;
                    break;
                case EnumVerticalAlignment.Others:
                    spreadsheetVerticalAlignment = DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Center;
                    break;
                default:
                    spreadsheetVerticalAlignment = DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Center;
                    break;
            }

            return spreadsheetVerticalAlignment;
        }
        #endregion

        #region GetEnumVerticalAlignmentNonVisual 获取垂直对齐
        public EnumVerticalAlignment GetEnumVerticalAlignmentNonVisual(DevExpress.Spreadsheet.SpreadsheetVerticalAlignment spreadsheetVerticalAlignment)
        {
            EnumVerticalAlignment enumVerticalAlignment = default(EnumVerticalAlignment);
            switch (spreadsheetVerticalAlignment)
            {
                case DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Top:
                    enumVerticalAlignment = EnumVerticalAlignment.Top;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Center:
                    enumVerticalAlignment = EnumVerticalAlignment.Center;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Bottom:
                    enumVerticalAlignment = EnumVerticalAlignment.Bottom;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Justify:
                    enumVerticalAlignment = EnumVerticalAlignment.Justify;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetVerticalAlignment.Distributed:
                    enumVerticalAlignment = EnumVerticalAlignment.Distributed;
                    break;
                default:
                    enumVerticalAlignment = EnumVerticalAlignment.Center;
                    break;
            }

            return enumVerticalAlignment;
        }
        #endregion

        #endregion
    }
    public class Border
    {
        public int Index { get; set; }
        public dynamic Color { get; set; }
        public dynamic RGB_Color { get; set; }
        public int ColorIndex { get; set; }
        public int LineStyle { get; set; }
        public dynamic TintAndShade { get; set; }
        public int Weight { get; set; }
    }

    public class BotAndItalicParam
    {
        public bool Bold { get; set; }
        public bool Italic { get; set; }

        #region 可视化 字体样式 粗体斜体转化
        /// <summary>
        /// 获取粗体斜体枚举值
        /// </summary>
        /// <returns></returns>
        #region EnumBoldAndItalic 获取粗体斜体枚举值
        public EnumBoldAndItalic GetBotAndItalicDescription()
        {
            EnumBoldAndItalic enumBoldAndItalic = EnumBoldAndItalic.General;
            if (Bold)
            {
                if (Italic)
                {
                    enumBoldAndItalic = EnumBoldAndItalic.BoldAndItalic;
                }
                else
                {
                    enumBoldAndItalic = EnumBoldAndItalic.Bold;
                }
            }
            else
            {
                if (Italic)
                {
                    enumBoldAndItalic = EnumBoldAndItalic.Italic;
                }
                else
                {
                    enumBoldAndItalic = EnumBoldAndItalic.General;
                }
            }

            return enumBoldAndItalic;
        }
        #endregion

        /// <summary>
        /// AnalyseParam 解析粗体斜体枚举值 赋值当前参数
        /// </summary>
        /// <param name="enumBoldAndItalic"></param>
        #region AnalyseParam 解析粗体斜体枚举值 赋值当前参数
        public void AnalyseParam(EnumBoldAndItalic enumBoldAndItalic)
        {
            switch (enumBoldAndItalic)
            {
                case EnumBoldAndItalic.General:
                    Bold = false;
                    Italic = false;
                    break;
                case EnumBoldAndItalic.Italic:
                    Bold = false;
                    Italic = true;
                    break;
                case EnumBoldAndItalic.Bold:
                    Bold = true;
                    Italic = false;
                    break;
                case EnumBoldAndItalic.BoldAndItalic:
                    Bold = true;
                    Italic = true;
                    break;
                default:
                    Bold = false;
                    Italic = false;
                    break;
            }
        }
        #endregion

        #endregion

        #region 非可视化 字体样式 粗体斜体与现有字体枚举相互转化
        public static EnumBoldAndItalic TurnFontStyleToEnumBoldAndItalic(DevExpress.Spreadsheet.SpreadsheetFontStyle spreadsheetFontStyle)
        {
            EnumBoldAndItalic enumBoldAndItalic = default(EnumBoldAndItalic);
            switch (spreadsheetFontStyle)
            {
                case DevExpress.Spreadsheet.SpreadsheetFontStyle.Regular:
                    enumBoldAndItalic = EnumBoldAndItalic.General;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetFontStyle.Bold:
                    enumBoldAndItalic = EnumBoldAndItalic.Bold;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetFontStyle.Italic:
                    enumBoldAndItalic = EnumBoldAndItalic.Italic;
                    break;
                case DevExpress.Spreadsheet.SpreadsheetFontStyle.BoldItalic:
                    enumBoldAndItalic = EnumBoldAndItalic.BoldAndItalic;
                    break;
                default:
                    break;
            }

            return enumBoldAndItalic;
        }

        public static DevExpress.Spreadsheet.SpreadsheetFontStyle TurnEnumBoldAndItalicToFontStyle(EnumBoldAndItalic enumBoldAndItalic)
        {
            DevExpress.Spreadsheet.SpreadsheetFontStyle fontStyle = default(DevExpress.Spreadsheet.SpreadsheetFontStyle);
            switch (enumBoldAndItalic)
            {
                case EnumBoldAndItalic.General:
                    fontStyle = DevExpress.Spreadsheet.SpreadsheetFontStyle.Regular;
                    break;
                case EnumBoldAndItalic.Bold:
                    fontStyle = DevExpress.Spreadsheet.SpreadsheetFontStyle.Bold;
                    break;
                case EnumBoldAndItalic.Italic:
                    fontStyle = DevExpress.Spreadsheet.SpreadsheetFontStyle.Italic;
                    break;
                case EnumBoldAndItalic.BoldAndItalic:
                    fontStyle = DevExpress.Spreadsheet.SpreadsheetFontStyle.BoldItalic;
                    break;
                default:
                    break;
            }

            return fontStyle;
        }
        #endregion
    }

    /// <summary>
    /// 常规 粗体 斜体  粗体斜体
    /// </summary>
    public enum EnumBoldAndItalic : int
    {
        General = 0,
        Bold = 1,
        Italic = 2,
        BoldAndItalic = 3
    }

    /// <summary>
    /// 水平布局: 常规 左对齐 水平居中 右对齐 填充 两端对齐 跨列居中 分散对齐 其他
    /// </summary>
    public enum EnumHorizontalAlignment : int
    {
        General = 0,
        Left = 1,
        Center = 2,
        Right = 3,
        Fill = 4,
        Justify = 5,
        CenterAcrossSelection = 6,
        Distributed = 7,
        Others = 8
    }

    /// <summary>
    /// 垂直布局:居中 靠上 靠下 两端对齐 等距分布 其他
    /// </summary>
    public enum EnumVerticalAlignment : int
    {
        Center = 0,
        Top = 1,
        Bottom = 2,
        Justify = 3,
        Distributed = 4,
        Others = 5
    }

    #region DigitalFormatClass 数字格式 相关

    public class FormatDesAttribute : Attribute
    {
        private string m_strFormatDescription;
        public FormatDesAttribute(string formatDes)
        {
            m_strFormatDescription = formatDes;
        }

        public string FormatDescription
        {
            get { return m_strFormatDescription; }
        }
    }
    #region
    /// <summary>
    /// 数字格式: 常规 数字 货币 会计专用 短日期 长日期 时间 百分比 分数 科学计数 文本 自定义
    /// General Number Currency AccountingSpecific ShortDate LongDate Time Percentage Fraction ScientificCounting Text Other
    /// </summary>
    public enum EnumDigitalFormat : int
    {
        [FormatDes("General")]
        General = 0,

        [FormatDes("0.00_);[Red](0.00)")]
        Number = 1,

        [FormatDes("$#,##0.00_);[Red]($#,##0.00)")]
        Currency = 2,

        [FormatDes("_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)")]
        AccountingSpecific = 3,

        [FormatDes("m/d/yyyy")]
        ShortDate = 4,

        [FormatDes("[$-F800]dddd, mmmm dd, yyyy")]
        LongDate = 5,

        [FormatDes("[$-F400]h:mm:ss AM/PM")]
        Time = 6,

        [FormatDes("0.00%")]
        Percentage = 7,

        [FormatDes("# ?/?")]
        Fraction = 8,

        [FormatDes("0.00E+00")]
        ScientificCounting = 9,

        [FormatDes("@")]
        Text = 10,

        [FormatDes("General")]
        Custom = 11
    }
    #endregion



    public class DigitalFormatClass
    {
        public EnumDigitalFormat enumDigitalFormat = EnumDigitalFormat.General;
        public string formatstring = "";
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? Decimal { get; set; }
        public int? FractionType { get; set; }
        public string GetFormatDes()
        {
            if (enumDigitalFormat == EnumDigitalFormat.Custom)
            {
                if (!string.IsNullOrEmpty(formatstring))
                {
                    return formatstring;
                }
            }
            var str_decimal = string.Empty;
            if (Decimal.HasValue)
            {
                int zeroCount = (int)Decimal.Value;
                if (zeroCount > 0)
                    str_decimal = "." + new string('0', Math.Abs(zeroCount));
            }
            else
            {
                str_decimal = ".00";
            }
            var str_fractionType = string.Empty;
            if (FractionType.HasValue)
            {
                switch (FractionType.Value)
                {
                    case 0:
                        str_fractionType = "# ?/?";// 分母为一位数
                        break;
                    case 1:
                        str_fractionType = "# ??/??";//分母为两位数
                        break;
                    case 2:
                        str_fractionType = "# ???/???";//分母为三位数
                        break;
                    case 3:
                        str_fractionType = "# ?/2";//以 2 为分母
                        break;
                    case 4:
                        str_fractionType = "# ?/4";//以 4 为分母
                        break;
                    case 5: 
                        str_fractionType = "# ?/8";//以 8 为分母
                        break;
                    case 6:
                        str_fractionType = "# ??/16";//以 16 为分母
                        break;
                    case 7:
                        str_fractionType = "# ?/10";//以 10 为分母
                        break;
                    case 8:
                        str_fractionType = "# ??/100";//百分之几
                        break;
                    default:
                        str_fractionType= "# ?/?";
                        break;
                }
            }
            
            switch (enumDigitalFormat)
            {
                case EnumDigitalFormat.General:
                    return "General";
                case EnumDigitalFormat.Number:
                    return $"0{str_decimal}_);[红色](0{str_decimal})";
                case EnumDigitalFormat.Currency:
                    return $"￥#,##0{str_decimal}_);[红色](￥#,##0{str_decimal})";
                case EnumDigitalFormat.AccountingSpecific:
                    return $"_(￥* #,##0{str_decimal}_);_(￥* (#,##0{str_decimal});_(￥* \"-\"??_);_(@_)";
                case EnumDigitalFormat.ShortDate:
                    return "yyyy/m/d;@";
                case EnumDigitalFormat.LongDate:
                    return "yyyy\"年\"m\"月\"d\"日\";@";
                case EnumDigitalFormat.Time:
                    return "h:mm:ss;@";
                case EnumDigitalFormat.Percentage:
                    return $"0{str_decimal}%";
                case EnumDigitalFormat.Fraction:
                    return str_fractionType;
                case EnumDigitalFormat.ScientificCounting:
                    return $"0{str_decimal}E+00";
                case EnumDigitalFormat.Text:
                    return "@";
                case EnumDigitalFormat.Custom:
                default:
                    return "General";
            }
        }

        public bool GetEnumFormat(string strFormatDes, out EnumDigitalFormat enumElement)
        {
            enumElement = default(EnumDigitalFormat);
            bool basFound = false;

            foreach (var item in EnumDigitalFormat.GetValues(enumElement.GetType()))
            {
                FormatDesAttribute attribute = item.GetType().GetField(item.ToString()).GetCustomAttributes<FormatDesAttribute>().FirstOrDefault();
                if (attribute != null && attribute.FormatDescription == strFormatDes)
                {
                    enumElement = (EnumDigitalFormat)item;
                    basFound = true;
                    break;
                }
            }

            return basFound;
        }
    }

    #endregion
}
