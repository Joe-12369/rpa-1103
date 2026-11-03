using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace II.RPA.Plugin.GUI.ElementHighlight
{
    /// <summary>
    /// Interaction logic for FontIcon.xaml
    /// </summary>
    public class FontIcon : UserControl
    {
        public static readonly DependencyProperty CodeProperty;

        public static readonly DependencyProperty CodeStringProperty;

        public static readonly DependencyProperty HighContrastForegroundProperty;

        public static readonly DependencyProperty SizeProperty;

        public string Code
        {
            get
            {
                return (string)GetValue(CodeProperty);
            }
            set
            {
                SetValue(CodeProperty, value);
            }
        }

        public string CodeString
        {
            get
            {
                return (string)GetValue(CodeStringProperty);
            }
            set
            {
                SetValue(CodeStringProperty, value);
            }
        }

        public Brush HighContrastForeground
        {
            get
            {
                return (Brush)GetValue(HighContrastForegroundProperty);
            }
            set
            {
                SetValue(HighContrastForegroundProperty, value);
            }
        }

        public double Size
        {
            get
            {
                return (double)GetValue(SizeProperty);
            }
            set
            {
                SetValue(SizeProperty, value);
            }
        }
        public FontIcon()
        {
        }
        static FontIcon()
        {
            CodeProperty = DependencyProperty.Register("Code", typeof(string), typeof(FontIcon), new PropertyMetadata(null, OnCodeChanged));
            CodeStringProperty = DependencyProperty.Register("CodeString", typeof(string), typeof(FontIcon), new PropertyMetadata(null));
            HighContrastForegroundProperty = DependencyProperty.Register("HighContrastForeground", typeof(Brush), typeof(FontIcon), new PropertyMetadata((object)null));
            SizeProperty = DependencyProperty.Register("Size", typeof(double), typeof(FontIcon), new PropertyMetadata(16.0));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FontIcon), new FrameworkPropertyMetadata(typeof(FontIcon)));
        }

        private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            string text = e.NewValue as string;
            if (text == null || string.IsNullOrEmpty(text))
            {
                d.ClearValue(CodeStringProperty);
                return;
            }
            char c;
            try
            {
                c = (char)short.Parse(text, NumberStyles.AllowHexSpecifier);
            }
            catch (Exception)
            {
                d.ClearValue(CodeStringProperty);
                return;
            }
            d.SetCurrentValue(CodeStringProperty, char.ToString(c));
        }
    }
}
