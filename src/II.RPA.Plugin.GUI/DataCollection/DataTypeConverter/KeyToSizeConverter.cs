using II.RPA.WinAutomation.CommonUtility.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace II.RPA.Plugin.GUI.DataCollection.DataTypeConverter
{
    /// <summary>
    /// 键值到控件尺寸的变化（结合语言环境 ）
    /// </summary>
    [ValueConversion(typeof(string), typeof(int))]
    public class KeyToSizeConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                int iValue = 0;
                switch (value.ToString())
                {
                    case "DataCollectColumnPopupWidth":
                        if (RPAEnvironment.GetInstance().CurrentLanguage == LanguageCultureEnum.zh_CN)
                        {
                            iValue = 110;
                        }
                        else if (RPAEnvironment.GetInstance().CurrentLanguage == LanguageCultureEnum.ja_JP)
                        {
                            iValue = 200;
                        }
                        break;
                    case "DataCollectColumn":
                        iValue = 100;
                        break;
                    default:
                        break;
                }
                return iValue;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion

    }
}
