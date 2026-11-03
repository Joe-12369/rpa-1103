using DevExpress.CodeParser;
using DevExpress.Portable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Models
{
    internal class CellFormat
    {
        public dynamic NumberFormat { get; set; }
        public dynamic HorizontalAlignment { get; set; }
        public dynamic VerticalAlignment { get; set; }
        public dynamic WrapText { get; set; }
        public dynamic Orientation { get; set; }
        public dynamic AddIndent { get; set; }
        public dynamic ShrinkToFit { get; set; }
        public dynamic MergeCells { get; set; }
    }
    internal class Font
    {
        public dynamic Name { get; set; }
        public dynamic FontStyle { get; set; }
        public dynamic Size { get; set; }
        public dynamic Strikethrough { get; set; }
        public dynamic Superscript { get; set; }
        public dynamic Subscript { get; set; }
        public dynamic Underline { get; set; }
        public dynamic ThemeColor { get; set; }
        public dynamic TintAndShade { get; set; }
        public dynamic ThemeFont { get; set; }
    }
}
