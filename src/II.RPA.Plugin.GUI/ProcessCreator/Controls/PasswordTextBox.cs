using II.RPA.Plugin.GUI.ProcessCreator.Theme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace II.RPA.Plugin.GUI.ProcessCreator.Controls
{
    public class PasswordTextBox : TextBox
    {
        /// <summary>
        /// 密码
        /// </summary>
        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        /// <summary>
        /// 密码修饰符
        /// </summary>
        public string PasswordChar
        {
            get { return (string)GetValue(PasswordCharProperty); }
            set { SetValue(PasswordCharProperty, value); }
        }

        /// <summary>
        /// 是否加密
        /// </summary>
        public bool Encrypt
        {
            get { return (bool)GetValue(EncryptProperty); }
            set
            {
                SetValue(EncryptProperty, value);
            }
        }

        /// <summary>
        /// 水印
        /// </summary>
        public string PlaceHolder
        {
            get { return (string)GetValue(PlaceHolderProperty); }
            set
            {
                SetValue(PlaceHolderProperty, value);
            }
        }

        /// <summary>
        /// 记住切换明文和密文之前，上一次插入符所在的位置
        /// </summary>
        int lastSelectionStart;

        public static DependencyProperty PasswordProperty = DependencyProperty.Register("Password", typeof(string), typeof(PasswordTextBox), new PropertyMetadata(""));
        public static DependencyProperty PasswordCharProperty = DependencyProperty.Register("PasswordChar", typeof(string), typeof(PasswordTextBox), new PropertyMetadata("*"));
        public static DependencyProperty EncryptProperty = DependencyProperty.Register("Encrypt", typeof(bool), typeof(PasswordTextBox), new PropertyMetadata(true, Changed));
        public static DependencyProperty PlaceHolderProperty = DependencyProperty.Register("PlaceHolder", typeof(string), typeof(PasswordTextBox), new PropertyMetadata(""));

        /// <summary>
        /// 当Encrypt改变时记住插入符在的位置
        /// 在Encrypt改变后，肯定会触发下一次textchanged，因为text的内容改变了
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordTextBox box = d as PasswordTextBox;
            box.lastSelectionStart = box.SelectionStart;
            box.TextChanged += Box_TextChanged;
        }

        /// <summary>
        /// 在文字改变之后，将插入符位置复位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            PasswordTextBox box = sender as PasswordTextBox;
            box.SelectionStart = box.lastSelectionStart;
            box.TextChanged -= Box_TextChanged;
        }

        public PasswordTextBox()
        {
            MultiBinding mutiBuilding = new MultiBinding();
            mutiBuilding.Converter = new PasswordConverter();
            mutiBuilding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            mutiBuilding.Bindings.Add(new Binding() { Path = new PropertyPath("Password"), Source = this });
            mutiBuilding.Bindings.Add(new Binding() { Path = new PropertyPath("PasswordChar"), Source = this });
            mutiBuilding.Bindings.Add(new Binding() { Path = new PropertyPath("Encrypt"), Source = this });
            BindingOperations.SetBinding(this, TextProperty, mutiBuilding);
        }

    }
    public class PasswordConverter : IMultiValueConverter
    {
        string realPassword = "";
        string realPasswordChar = "*";
        bool encryptPawssword = true;

        /// <summary>
        /// 由返回值可以看出这是将绑定的值转化为最终显示的Text
        /// </summary>
        /// <param name="value">value的值顺序就和绑定的顺序一样</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        object IMultiValueConverter.Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            StringBuilder sb = new StringBuilder();
            if (value != null && value.Length >= 1)
            {
                string sValue = value[0].ToString();
                bool encrypt = (bool)value.Where(v => v.GetType() == typeof(bool)).FirstOrDefault();
                encryptPawssword = encrypt;

                realPassword = sValue;

                if (value.Length > 1 && encrypt)
                {
                    char passChar = value[1].ToString()[0];
                    realPasswordChar = passChar.ToString();
                    if (!string.IsNullOrEmpty(sValue))
                    {
                        for (int i = 0; i < sValue.Length; i++)
                        {
                            sb.Append(passChar);
                        }
                    }

                    return sb.ToString();
                }
                else
                {
                    return sValue;
                }
            }

            return null;
        }

        /// <summary>
        /// 将Text的值传进来，转化为绑定数值
        /// 这里需要注意，如果PasswordChar为*，那value的值就是*****x
        /// 假设已经输入了五位数，第六个数量入了g，那么value就是*****g
        /// 所以在此类需要保存真正的密码
        /// 举个例子 前五个你输入了12345，第六个输入了g
        /// 那么value的值就是*****g，全局realPassword值为12345
        /// 此时需要将g交给realPassword
        /// 如果你是在12后面插入的g，那valu就是**g***
        /// 所以需要将**g***和realPassword的位置做对比，将g插在第三位
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string backValue = "";
            if (value != null)
            {
                string strValue = value.ToString();

                if (encryptPawssword)
                {
                    char replaceChar = realPasswordChar[0];
                    //这里需要注意一下，原作者这里用的是realPassword索引backValue+=realPassword[index]
                    //如果这么做的话，正常输入密码没问题，但是在密码中间插入密码，就会出现数组越界
                    IEnumerator<char> realChars = realPassword.GetEnumerator();
                    for (int index = 0; index < strValue.Length; ++index)
                    {
                        if (strValue[index] == replaceChar)
                        {
                            if (realChars.MoveNext()) backValue += realChars.Current;
                        }
                        else
                        {
                            backValue += strValue[index];
                        }
                    }

                    strValue = backValue;
                }

                return new object[] { strValue, realPasswordChar, encryptPawssword };
            }

            return null;
        }
    }

}
