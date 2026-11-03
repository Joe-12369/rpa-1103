using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace II.RPA.Plugin.GUI.ProcessCreator.Controls
{
    public class OriginCheckBox : CheckBox
    {
        static OriginCheckBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OriginCheckBox), new FrameworkPropertyMetadata(typeof(OriginCheckBox)));
        }

        public static readonly DependencyProperty MouseOverBackgroundProperty =
          DependencyProperty.Register("MouseOverBackground", typeof(Brush), typeof(OriginCheckBox), new PropertyMetadata(Brushes.RoyalBlue));

        /// <summary>
        /// 鼠标进入背景样式
        /// </summary>
        public Brush MouseOverBackground
        {
            get { return (Brush)GetValue(MouseOverBackgroundProperty); }
            set { SetValue(MouseOverBackgroundProperty, value); }
        }

        public static readonly DependencyProperty DefaultBackgroundProperty =
          DependencyProperty.Register("DefaultBackground", typeof(Brush), typeof(OriginCheckBox), new PropertyMetadata(Brushes.Transparent));

        /// <summary>
        /// 鼠标进入背景样式
        /// </summary>
        public Brush DefaultBackground
        {
            get { return (Brush)GetValue(DefaultBackgroundProperty); }
            set { SetValue(DefaultBackgroundProperty, value); }
        }

        public static readonly DependencyProperty CheckedBackgroundProperty =
          DependencyProperty.Register("CheckedBackground", typeof(Brush), typeof(OriginCheckBox), new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF9810"))));

        /// <summary>
        /// 鼠标进入背景样式
        /// </summary>
        public Brush CheckedBackground
        {
            get { return (Brush)GetValue(CheckedBackgroundProperty); }
            set { SetValue(CheckedBackgroundProperty, value); }
        }


        public static readonly DependencyProperty MouseOverForegroundProperty =
            DependencyProperty.Register("MouseOverForeground", typeof(Brush), typeof(OriginCheckBox), new PropertyMetadata(Brushes.White));

        /// <summary>
        /// 鼠标进入前景样式
        /// </summary>
        public Brush MouseOverForeground
        {
            get { return (Brush)GetValue(MouseOverForegroundProperty); }
            set { SetValue(MouseOverForegroundProperty, value); }
        }


        public static readonly DependencyProperty HoverBorderBrushProperty =
       DependencyProperty.Register("HoverBorderBrush", typeof(Brush), typeof(OriginCheckBox), new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF9810"))));

        /// <summary>
        /// 鼠标进入背景样式
        /// </summary>
        public Brush HoverBorderBrush
        {
            get { return (Brush)GetValue(HoverBorderBrushProperty); }
            set { SetValue(HoverBorderBrushProperty, value); }
        }



        public static readonly DependencyProperty BoxBorderThicknessProperty =
           DependencyProperty.Register("BoxBorderThickness", typeof(Thickness), typeof(OriginCheckBox), new PropertyMetadata(new Thickness(1, 1, 1, 1)));

        /// <summary>
        /// 鼠标进入前景样式
        /// </summary>
        public Thickness BoxBorderThickness
        {
            get { return (Thickness)GetValue(BoxBorderThicknessProperty); }
            set { SetValue(BoxBorderThicknessProperty, value); }
        }

        //public static readonly DependencyProperty BoxBorderCheckedThicknessProperty =
        //  DependencyProperty.Register("BoxBorderCheckedThickness", typeof(double), typeof(OriginCheckBox), new PropertyMetadata(0));

        ///// <summary>
        ///// 鼠标进入前景样式
        ///// </summary>
        //public double BoxBorderCheckedThickness
        //{

        //    get { return (double)GetValue(BoxBorderCheckedThicknessProperty); }
        //    set { SetValue(BoxBorderCheckedThicknessProperty, value); }
        //}


        public static readonly DependencyProperty BoxBorderDefaultBrushProperty =
           DependencyProperty.Register("BoxBorderDefaultBrush", typeof(Brush), typeof(OriginCheckBox), new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD9DDE6"))));

        /// <summary>
        /// 鼠标进入背景样式
        /// </summary>
        public Brush BoxBorderDefaultBrush
        {
            get { return (Brush)GetValue(BoxBorderDefaultBrushProperty); }
            set { SetValue(BoxBorderDefaultBrushProperty, value); }
        }

        public static readonly DependencyProperty BoxBorderHoverBrushProperty =
          DependencyProperty.Register("BoxBorderHoverBrush", typeof(Brush), typeof(OriginCheckBox), new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF9810"))));

        /// <summary>
        /// 鼠标进入背景样式
        /// </summary>
        public Brush BoxBorderHoverBrush
        {
            get { return (Brush)GetValue(BoxBorderHoverBrushProperty); }
            set { SetValue(BoxBorderHoverBrushProperty, value); }
        }
    }
}
