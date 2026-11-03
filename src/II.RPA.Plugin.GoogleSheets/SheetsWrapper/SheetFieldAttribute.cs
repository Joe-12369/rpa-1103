using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.SheetsWrapper
{
    public class SheetFieldAttribute : Attribute
    {
        protected static Dictionary<SheetFieldType, string> DefaultFormatPatterns = new Dictionary<SheetFieldType, string>()
        {
            { SheetFieldType.String, "" },
            { SheetFieldType.Number, "#,##0.00" },
            { SheetFieldType.PhoneNumber, "(###)\" \"###\"-\"####" },
            { SheetFieldType.DateTime, "M/d/yyyy H:mm:ss" },
            { SheetFieldType.Currency, "\"$\"#,##0.00" },
        };

        /// <summary>
        /// 列名
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 列ID，从1开始
        /// </summary>
        public int ColumnID { get; set; }

        /// <summary>
        /// 字段类型 (ie String, DateTime, Currency, PhoneNumber, Number, etc)
        /// </summary>
        public SheetFieldType FieldType { get; set; }

        private string _numberFormatPattern;

        /// <summary>
        /// 可选参数
        /// </summary>
        public string NumberFormatPattern
        {
            get
            {
                if (_numberFormatPattern == null)
                {
                    return DefaultFormatPatterns[this.FieldType];
                }

                return _numberFormatPattern;
            }

            set
            {
                _numberFormatPattern = value;
            }
        }

    }

    public enum SheetFieldType
    {
        String,
        DateTime,
        Currency,
        PhoneNumber,
        Number
    }
}
