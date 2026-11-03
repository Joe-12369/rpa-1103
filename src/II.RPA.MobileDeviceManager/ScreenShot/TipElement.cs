using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.MobileDeviceManager.ScreenShot
{
    public class TipElement
    {
        public static readonly DependencyProperty VisibilityProperty = DependencyProperty.RegisterAttached(
            "Visibility", typeof(Visibility), typeof(TipElement), new PropertyMetadata(Visibility.Collapsed));

        public static void SetVisibility(DependencyObject element, Visibility value) => element.SetValue(VisibilityProperty, value);

        public static Visibility GetVisibility(DependencyObject element) => (Visibility)element.GetValue(VisibilityProperty);

        public static readonly DependencyProperty PlacementProperty = DependencyProperty.RegisterAttached(
            "Placement", typeof(PlacementType), typeof(TipElement), new PropertyMetadata(default(PlacementType)));

        public static void SetPlacement(DependencyObject element, PlacementType value) => element.SetValue(PlacementProperty, value);

        public static PlacementType GetPlacement(DependencyObject element) => (PlacementType)element.GetValue(PlacementProperty);

        public static readonly DependencyProperty StringFormatProperty = DependencyProperty.RegisterAttached(
            "StringFormat", typeof(string), typeof(TipElement), new PropertyMetadata("#0.0"));

        public static void SetStringFormat(DependencyObject element, string value)
            => element.SetValue(StringFormatProperty, value);

        public static string GetStringFormat(DependencyObject element)
            => (string)element.GetValue(StringFormatProperty);
    }
    public enum PlacementType
    {
        /// <summary>
        /// 左上
        /// </summary>
        LeftTop,

        /// <summary>
        /// 左
        /// </summary>
        Left,

        /// <summary>
        /// 左下
        /// </summary>
        LeftBottom,

        /// <summary>
        /// 上左
        /// </summary>
        TopLeft,

        /// <summary>
        /// 上
        /// </summary>
        Top,

        /// <summary>
        /// 上右
        /// </summary>
        TopRight,

        /// <summary>
        /// 右上
        /// </summary>
        RightTop,

        /// <summary>
        /// 右
        /// </summary>
        Right,

        /// <summary>
        /// 右下
        /// </summary>
        RightBottom,

        /// <summary>
        /// 下左
        /// </summary>
        BottomLeft,

        /// <summary>
        /// 下
        /// </summary>
        Bottom,

        /// <summary>
        /// 下右
        /// </summary>
        BottomRight,
    }
}
