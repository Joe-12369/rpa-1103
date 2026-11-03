using II.Framework.Resource;
using static II.Framework.Resource.I18nExtensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace II.RPA.ZDeskTop.DataValidation
{
    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return string.IsNullOrWhiteSpace((value ?? "").ToString())
                ? new ValidationResult(false, "请输入内容!".I18n("NotEmptyValidationRule_88017F3B"))
                : ValidationResult.ValidResult;
        }
    }
}
