using II.Framework.Resource;
using static II.Framework.Resource.I18nExtensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace II.RPA.ZDeskTop.DataValidation
{
    public class CommonValidationRule : ValidationRule
    {
        private static readonly Dictionary<RegularTypeEnum, RegularValidation> _regularValidations = new Dictionary<RegularTypeEnum, RegularValidation>
        {
            {
                RegularTypeEnum.PositiveInteger,
                new RegularValidation
                {
                    RegularString = "^([0-9]{1,})$",
                    ErrorMessage = " Invalid input "
                }
            },
            {
                RegularTypeEnum.NotNull,
                new RegularValidation
                {
                    RegularString = "[^\\s]",
                    ErrorMessage = " 不允许为空 ".I18n("CommonValidationRule_3AF0D375")
                }
            },
            {
                RegularTypeEnum.EndPoint,
                new RegularValidation
                {
                    RegularString = "^(\\d|[1-9]\\d|1\\d{2}|2[0-4]\\d|25[0-5])\\.(\\d|[1-9]\\d|1\\d{2}|2[0-4]\\d|25[0-5])\\.(\\d|[1-9]\\d|1\\d{2}|2[0-4]\\d|25[0-5])\\.(\\d|[1-9]\\d|1\\d{2}|2[0-4]\\d|25[0-5]):([0-9]|[1-9]\\d|[1-9]\\d{2}|[1-9]\\d{3}|[1-5]\\d{4}|6[0-4]\\d{3}|65[0-4]\\d{2}|655[0-2]\\d|6553[0-5])$",
                    ErrorMessage = " Invalid endpoint "
                }
            }
        };
        public CommonValidationRule()
        {
            ValidatesOnTargetUpdated = true;
        }
        public RegularTypeEnum RegularType { get; set; }
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string str = value as string;
            if (!string.IsNullOrWhiteSpace(str))
            {
                if (_regularValidations.TryGetValue(RegularType, out RegularValidation regularValidation))
                {
                    if (!Regex.IsMatch(str, regularValidation.RegularString))
                    {
                        return new ValidationResult(false, regularValidation.ErrorMessage);
                    }
                }
            }
            return ValidationResult.ValidResult;
        }
    }
    public enum RegularTypeEnum
    {
        PositiveInteger,
        EndPoint,
        NotNull,
    }
    public struct RegularValidation
    {
        public string RegularString
        {
            get;
            set;
        }
        public string ErrorMessage
        {
            get;
            set;
        }
    }
}
