namespace II.RPA.Plugin.Excel.Params
{
    public class ColumnSplitParam
    {
        //https://learn.microsoft.com/en-us/office/vba/api/excel.xltextparsingtype
        public dynamic xlTextFormat = 1;
        //https://learn.microsoft.com/en-us/office/vba/api/excel.xltextqualifier
        public dynamic xlTextQualifier = -4142;
        public bool xlConsecutiveDelimiter = false;
        public int? xlWidth = 0;
        public bool xlTab = false;
        public bool xlSemicolon = false;
        public bool xlComma = false;
        public bool xlSpace = false;
        public bool xlOther = false;
        public string xlOtherChar = string.Empty;
        public string decimalSeparator { get; set; }
        public string thousandsSeparator { get; set; }
        public int trailingMinusNumbers { get; set; }
    }

    public class ColumnSplitJsonParam
    {
        public string textFormat { get; set; }
        public int dataType { get; set; }
        public int? width { get; set; }
        public bool tab { get; set; }
        public bool semicolon { get; set; }
        public bool comma { get; set; }
        public bool space { get; set; }
        public bool other { get; set; }
        public string otherChar { get; set; }
        public bool consecutiveDelimiter { get; set; }
        public int textQualifier { get; set; }
        public string decimalSeparator { get; set; }
        public string thousandsSeparator { get; set; }
        public int trailingMinusNumbers { get; set; }
        public ColumnSplitParam Reform()
        {
            ColumnSplitParam columnSplitParam = new ColumnSplitParam();
            columnSplitParam.xlWidth = width;
            columnSplitParam.xlTab = tab;
            columnSplitParam.xlSemicolon = semicolon;
            columnSplitParam.xlComma = comma;
            columnSplitParam.xlSpace = space;
            columnSplitParam.xlOther = other;
            columnSplitParam.xlOtherChar = otherChar;
            columnSplitParam.xlConsecutiveDelimiter = consecutiveDelimiter;
            //https://learn.microsoft.com/en-us/office/vba/api/excel.xltextparsingtype
            switch (textFormat)
            {
                case "分隔符号":
                    columnSplitParam.xlTextFormat = 1;
                    break;
                case "固定宽度":
                    columnSplitParam.xlTextFormat = 2;
                    break;
                default:
                    columnSplitParam.xlTextFormat = dataType;
                    break;
            }
            //https://learn.microsoft.com/en-us/office/vba/api/excel.xltextqualifier
            columnSplitParam.xlTextQualifier = 1;

            columnSplitParam.xlTextQualifier = textQualifier;
            columnSplitParam.decimalSeparator = decimalSeparator;
            columnSplitParam.thousandsSeparator = thousandsSeparator;
            columnSplitParam.trailingMinusNumbers = trailingMinusNumbers;

            return columnSplitParam;
        }
    }

}
