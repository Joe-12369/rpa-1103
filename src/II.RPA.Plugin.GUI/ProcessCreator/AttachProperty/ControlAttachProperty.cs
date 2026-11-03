using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.Logging;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.Controls;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RadioButton = System.Windows.Controls.RadioButton;
using RichTextBox = System.Windows.Controls.RichTextBox;
using TextBox = System.Windows.Controls.TextBox;

namespace II.RPA.Plugin.GUI.ProcessCreator.AttachProperty
{
    public static class ControlAttachProperty
    {
        static ControlAttachProperty()
        {
            ClearTextCommand = new RoutedUICommand();
            ClearTextCommandBinding = new CommandBinding(ClearTextCommand);
            ClearTextCommandBinding.Executed += ClearButtonClick;
        }
        #region 
        public static readonly DependencyProperty BorderCornerRadiusProperty = DependencyProperty.RegisterAttached(
            "BorderCornerRadius", typeof(CornerRadius), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(new CornerRadius(0)));

        public static void SetBorderCornerRadius(DependencyObject element, CornerRadius value)
        {
            element.SetValue(BorderCornerRadiusProperty, value);
        }

        public static CornerRadius GetBorderCornerRadius(DependencyObject element)
        {
            return (CornerRadius)element.GetValue(BorderCornerRadiusProperty);
        }
        #endregion

        #region FocusBackground 获得焦点背景色，

        public static readonly DependencyProperty FocusBackgroundProperty = DependencyProperty.RegisterAttached(
            "FocusBackground", typeof(Brush), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(Brushes.Transparent));

        public static void SetFocusBackground(DependencyObject element, Brush value)
        {
            element.SetValue(FocusBackgroundProperty, value);
        }

        public static Brush GetFocusBackground(DependencyObject element)
        {
            return (Brush)element.GetValue(FocusBackgroundProperty);
        }

        #endregion

        #region FocusForeground 获得焦点前景色，

        public static readonly DependencyProperty FocusForegroundProperty = DependencyProperty.RegisterAttached(
            "FocusForeground", typeof(Brush), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(Brushes.Transparent));

        public static void SetFocusForeground(DependencyObject element, Brush value)
        {
            element.SetValue(FocusForegroundProperty, value);
        }

        public static Brush GetFocusForeground(DependencyObject element)
        {
            return (Brush)element.GetValue(FocusForegroundProperty);
        }

        #endregion

        #region MouseOverBackgroundProperty 鼠标悬浮背景色

        public static readonly DependencyProperty MouseOverBackgroundProperty = DependencyProperty.RegisterAttached(
            "MouseOverBackground", typeof(Brush), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(Brushes.Transparent));
        [AttachedPropertyBrowsableForType(typeof(System.Windows.Controls.Control))]
        public static void SetMouseOverBackground(DependencyObject element, Brush value)
        {
            element.SetValue(MouseOverBackgroundProperty, value);
        }
        [AttachedPropertyBrowsableForType(typeof(System.Windows.Controls.Control))]
        public static Brush MouseOverBackground(DependencyObject element)
        {
            return (Brush)element.GetValue(MouseOverBackgroundProperty);
        }

        #endregion

        #region PressBackgroundProperty 鼠标按下背景色

        public static readonly DependencyProperty PressBackgroundProperty = DependencyProperty.RegisterAttached(
            "PressBackground", typeof(Brush), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(Brushes.Transparent));
        [AttachedPropertyBrowsableForType(typeof(System.Windows.Controls.Control))]
        public static void SetPressBackground(DependencyObject element, Brush value)
        {
            element.SetValue(PressBackgroundProperty, value);
        }
        [AttachedPropertyBrowsableForType(typeof(System.Windows.Controls.Control))]
        public static Brush GetPressBackground(DependencyObject element)
        {
            return (Brush)element.GetValue(PressBackgroundProperty);
        }

        #endregion

        #region MouseOverForegroundProperty 鼠标悬浮前景色

        public static readonly DependencyProperty MouseOverForegroundProperty = DependencyProperty.RegisterAttached(
            "MouseOverForeground", typeof(Brush), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(Brushes.White));

        public static void SetMouseOverForeground(DependencyObject element, Brush value)
        {
            element.SetValue(MouseOverForegroundProperty, value);
        }

        public static Brush MouseOverForeground(DependencyObject element)
        {
            return (Brush)element.GetValue(FocusForegroundProperty);
        }

        #endregion

        #region FocusBorderBrush 焦点边框色，输入控件

        public static readonly DependencyProperty FocusBorderBrushProperty = DependencyProperty.RegisterAttached(
            "FocusBorderBrush", typeof(Brush), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(Brushes.White));
        public static void SetFocusBorderBrush(DependencyObject element, Brush value)
        {
            element.SetValue(FocusBorderBrushProperty, value);
        }
        public static Brush GetFocusBorderBrush(DependencyObject element)
        {
            return (Brush)element.GetValue(FocusBorderBrushProperty);
        }

        #endregion

        #region MouseOverBorderBrush 鼠标进入边框色，输入控件

        public static readonly DependencyProperty MouseOverBorderBrushProperty =
            DependencyProperty.RegisterAttached("MouseOverBorderBrush", typeof(Brush), typeof(ControlAttachProperty),
                new FrameworkPropertyMetadata(Brushes.Transparent,
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Sets the brush used to draw the mouse over brush.
        /// </summary>
        public static void SetMouseOverBorderBrush(DependencyObject obj, Brush value)
        {
            obj.SetValue(MouseOverBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets the brush used to draw the mouse over brush.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        [AttachedPropertyBrowsableForType(typeof(CheckBox))]
        [AttachedPropertyBrowsableForType(typeof(RadioButton))]
        [AttachedPropertyBrowsableForType(typeof(DatePicker))]
        [AttachedPropertyBrowsableForType(typeof(ComboBox))]
        [AttachedPropertyBrowsableForType(typeof(RichTextBox))]
        public static Brush GetMouseOverBorderBrush(DependencyObject obj)
        {
            return (Brush)obj.GetValue(MouseOverBorderBrushProperty);
        }

        #endregion

        #region AttachContentProperty 附加组件模板
        /// <summary>
        /// 附加组件模板
        /// </summary>
        public static readonly DependencyProperty AttachContentProperty = DependencyProperty.RegisterAttached(
            "AttachContent", typeof(ControlTemplate), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(null));

        public static ControlTemplate GetAttachContent(DependencyObject d)
        {
            return (ControlTemplate)d.GetValue(AttachContentProperty);
        }

        public static void SetAttachContent(DependencyObject obj, ControlTemplate value)
        {
            obj.SetValue(AttachContentProperty, value);
        }
        #endregion

        #region WatermarkProperty 水印
        /// <summary>
        /// 水印
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.RegisterAttached(
            "Watermark", typeof(string), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(""));

        public static string GetWatermark(DependencyObject d)
        {
            return (string)d.GetValue(WatermarkProperty);
        }

        public static void SetWatermark(DependencyObject obj, string value)
        {
            obj.SetValue(WatermarkProperty, value);
        }
        #endregion
        #region InputTextProperty CheckBox Popup Input
        /// <summary>
        /// CheckBox Popup Input
        /// </summary>
        public static readonly DependencyProperty InputTextProperty = DependencyProperty.RegisterAttached(
            "InputText", typeof(string), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(""));

        public static string GetInputText(DependencyObject d)
        {
            return (string)d.GetValue(InputTextProperty);
        }

        public static void SetInputText(DependencyObject obj, string value)
        {
            obj.SetValue(InputTextProperty, value);
        }
        #endregion
        #region FIconProperty 字体图标
        /// <summary>
        /// 字体图标
        /// </summary>
        public static readonly DependencyProperty FIconProperty = DependencyProperty.RegisterAttached(
            "FIcon", typeof(string), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(""));

        public static string GetFIcon(DependencyObject d)
        {
            return (string)d.GetValue(FIconProperty);
        }

        public static void SetFIcon(DependencyObject obj, string value)
        {
            obj.SetValue(FIconProperty, value);
        }
        #endregion

        #region FIconSizeProperty 字体图标大小
        /// <summary>
        /// 字体图标
        /// </summary>
        public static readonly DependencyProperty FIconSizeProperty = DependencyProperty.RegisterAttached(
            "FIconSize", typeof(double), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(12D));

        public static double GetFIconSize(DependencyObject d)
        {
            return (double)d.GetValue(FIconSizeProperty);
        }

        public static void SetFIconSize(DependencyObject obj, double value)
        {
            obj.SetValue(FIconSizeProperty, value);
        }
        #endregion

        #region FIconMarginProperty 字体图标边距
        /// <summary>
        /// 字体图标
        /// </summary>
        public static readonly DependencyProperty FIconMarginProperty = DependencyProperty.RegisterAttached(
            "FIconMargin", typeof(Thickness), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(new Thickness(0)));

        public static Thickness GetFIconMargin(DependencyObject d)
        {
            return (Thickness)d.GetValue(FIconMarginProperty);
        }

        public static void SetFIconMargin(DependencyObject obj, Thickness value)
        {
            obj.SetValue(FIconMarginProperty, value);
        }
        #endregion

        #region AllowsAnimationProperty 启用旋转动画
        /// <summary>
        /// 启用旋转动画
        /// </summary>
        public static readonly DependencyProperty AllowsAnimationProperty = DependencyProperty.RegisterAttached("AllowsAnimation"
            , typeof(bool), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(false, AllowsAnimationChanged));

        public static bool GetAllowsAnimation(DependencyObject d)
        {
            return (bool)d.GetValue(AllowsAnimationProperty);
        }

        public static void SetAllowsAnimation(DependencyObject obj, bool value)
        {
            obj.SetValue(AllowsAnimationProperty, value);
        }

        /// <summary>
        /// 旋转动画刻度
        /// </summary>
        private static DoubleAnimation RotateAnimation = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(200)));

        /// <summary>
        /// 绑定动画事件
        /// </summary>
        private static void AllowsAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var uc = d as FrameworkElement;
            if (uc == null) return;
            if (uc.RenderTransformOrigin == new Point(0, 0))
            {
                uc.RenderTransformOrigin = new Point(0.5, 0.5);
                RotateTransform trans = new RotateTransform(0);
                uc.RenderTransform = trans;
            }
            var value = (bool)e.NewValue;
            if (value)
            {
                RotateAnimation.To = 180;
                uc.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, RotateAnimation);
            }
            else
            {
                RotateAnimation.To = 0;
                uc.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, RotateAnimation);
            }
        }
        #endregion

        #region CornerRadiusProperty Border圆角
        /// <summary>
        /// Border圆角
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
            "CornerRadius", typeof(CornerRadius), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(new CornerRadius(0)));

        public static CornerRadius GetCornerRadius(DependencyObject d)
        {
            return (CornerRadius)d.GetValue(CornerRadiusProperty);
        }

        public static void SetCornerRadius(DependencyObject obj, CornerRadius value)
        {
            obj.SetValue(CornerRadiusProperty, value);
        }
        #endregion
        #region BlurRadiusProperty Border圆角
        /// <summary>
        /// Blur圆角
        /// </summary>
        public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.RegisterAttached(
            "BlurRadius", typeof(CornerRadius), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(new CornerRadius(0)));

        public static CornerRadius GetBlurRadius(DependencyObject d)
        {
            return (CornerRadius)d.GetValue(BlurRadiusProperty);
        }

        public static void SetBlurRadius(DependencyObject obj, CornerRadius value)
        {
            obj.SetValue(BlurRadiusProperty, value);
        }
        #endregion
        #region LabelProperty TextBox的头部Label
        /// <summary>
        /// TextBox的头部Label
        /// </summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.RegisterAttached(
            "Label", typeof(string), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(""));

        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static string GetLabel(DependencyObject d)
        {
            return (string)d.GetValue(LabelProperty);
        }

        public static void SetLabel(DependencyObject obj, string value)
        {
            obj.SetValue(LabelProperty, value);
        }
        #endregion

        #region LabelTemplateProperty TextBox的头部Label模板
        /// <summary>
        /// TextBox的头部Label模板
        /// </summary>
        public static readonly DependencyProperty LabelTemplateProperty = DependencyProperty.RegisterAttached(
            "LabelTemplate", typeof(ControlTemplate), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(null));

        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static ControlTemplate GetLabelTemplate(DependencyObject d)
        {
            return (ControlTemplate)d.GetValue(LabelTemplateProperty);
        }

        public static void SetLabelTemplate(DependencyObject obj, ControlTemplate value)
        {
            obj.SetValue(LabelTemplateProperty, value);
        }
        #endregion

        #region ComboxPopupVerticalOffset ComboBox的Popup 相对位置
        /// <summary>
        /// ComboBox的Popup 纵向相对位置
        /// </summary>
        public static readonly DependencyProperty ComboxPopupVerticalOffsetProperty = DependencyProperty.RegisterAttached(
            "ComboxPopupVerticalOffset", typeof(double), typeof(ControlAttachProperty), new FrameworkPropertyMetadata((double)0));

        [AttachedPropertyBrowsableForType(typeof(ComboBox))]
        public static double GetComboxPopupVerticalOffset(DependencyObject d)
        {
            return (double)d.GetValue(ComboxPopupVerticalOffsetProperty);
        }

        public static void SetComboxPopupVerticalOffset(DependencyObject obj, double value)
        {
            obj.SetValue(ComboxPopupVerticalOffsetProperty, value);
        }

        /// <summary>
        /// ComboBox的Popup 横向相对位置
        /// </summary>
        public static readonly DependencyProperty ComboxPopupHorizontalOffsetProperty = DependencyProperty.RegisterAttached(
            "ComboxPopupHorizontalOffset", typeof(double), typeof(ControlAttachProperty), new FrameworkPropertyMetadata((double)0));

        [AttachedPropertyBrowsableForType(typeof(ComboBox))]
        public static double GetComboxPopupHorizontalOffset(DependencyObject d)
        {
            return (double)d.GetValue(ComboxPopupHorizontalOffsetProperty);
        }

        public static void SetComboxPopupHorizontalOffset(DependencyObject obj, double value)
        {
            obj.SetValue(ComboxPopupHorizontalOffsetProperty, value);
        }
        #endregion

        #region IsClearTextButtonBehaviorEnabledProperty 清除输入框Text值按钮行为开关（设为ture时才会绑定事件）
        /// <summary>
        /// 清除输入框Text值按钮行为开关
        /// </summary>
        public static readonly DependencyProperty IsClearTextButtonBehaviorEnabledProperty = DependencyProperty.RegisterAttached("IsClearTextButtonBehaviorEnabled"
            , typeof(bool), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(false, IsClearTextButtonBehaviorEnabledChanged));

        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static bool GetIsClearTextButtonBehaviorEnabled(DependencyObject d)
        {
            return (bool)d.GetValue(IsClearTextButtonBehaviorEnabledProperty);
        }

        public static void SetIsClearTextButtonBehaviorEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsClearTextButtonBehaviorEnabledProperty, value);
        }

        /// <summary>
        /// 绑定清除Text操作的按钮事件
        /// </summary>
        private static void IsClearTextButtonBehaviorEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as TFButton;
            if (e.OldValue != e.NewValue && button != null)
            {
                button.CommandBindings.Add(ClearTextCommandBinding);
            }
        }

        #endregion

        #region IsOpenFileButtonBehaviorEnabledProperty 选择文件命令行为开关
        /// <summary>
        /// 选择文件命令行为开关
        /// </summary>
        public static readonly DependencyProperty IsOpenFileButtonBehaviorEnabledProperty = DependencyProperty.RegisterAttached("IsOpenFileButtonBehaviorEnabled"
            , typeof(bool), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(false, IsOpenFileButtonBehaviorEnabledChanged));

        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static bool GetIsOpenFileButtonBehaviorEnabled(DependencyObject d)
        {
            return (bool)d.GetValue(IsOpenFileButtonBehaviorEnabledProperty);
        }

        public static void SetIsOpenFileButtonBehaviorEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsOpenFileButtonBehaviorEnabledProperty, value);
        }

        private static void IsOpenFileButtonBehaviorEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as TFButton;
            if (e.OldValue != e.NewValue && button != null)
            {
                button.CommandBindings.Add(OpenFileCommandBinding);
            }
        }

        #endregion

        #region IsOpenFolderButtonBehaviorEnabledProperty 选择文件夹命令行为开关
        /// <summary>
        /// 选择文件夹命令行为开关
        /// </summary>
        public static readonly DependencyProperty IsOpenFolderButtonBehaviorEnabledProperty = DependencyProperty.RegisterAttached("IsOpenFolderButtonBehaviorEnabled"
            , typeof(bool), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(false, IsOpenFolderButtonBehaviorEnabledChanged));

        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static bool GetIsOpenFolderButtonBehaviorEnabled(DependencyObject d)
        {
            return (bool)d.GetValue(IsOpenFolderButtonBehaviorEnabledProperty);
        }

        public static void SetIsOpenFolderButtonBehaviorEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsOpenFolderButtonBehaviorEnabledProperty, value);
        }

        private static void IsOpenFolderButtonBehaviorEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as TFButton;
            if (e.OldValue != e.NewValue && button != null)
            {
                button.CommandBindings.Add(OpenFolderCommandBinding);
            }
        }

        #endregion

        #region IsSaveFileButtonBehaviorEnabledProperty 选择文件保存路径及名称
        /// <summary>
        /// 选择文件保存路径及名称
        /// </summary>
        public static readonly DependencyProperty IsSaveFileButtonBehaviorEnabledProperty = DependencyProperty.RegisterAttached("IsSaveFileButtonBehaviorEnabled"
            , typeof(bool), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(false, IsSaveFileButtonBehaviorEnabledChanged));

        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static bool GetIsSaveFileButtonBehaviorEnabled(DependencyObject d)
        {
            return (bool)d.GetValue(IsSaveFileButtonBehaviorEnabledProperty);
        }

        public static void SetIsSaveFileButtonBehaviorEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSaveFileButtonBehaviorEnabledProperty, value);
        }

        private static void IsSaveFileButtonBehaviorEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as TFButton;
            if (e.OldValue != e.NewValue && button != null)
            {
                button.CommandBindings.Add(SaveFileCommandBinding);
            }
        }

        #endregion

        #region ClearTextCommand 清除输入框Text事件命令

        /// <summary>
        /// 清除输入框Text事件命令，需要使用IsClearTextButtonBehaviorEnabledChanged绑定命令
        /// </summary>
        public static RoutedUICommand ClearTextCommand { get; private set; }

        /// <summary>
        /// ClearTextCommand绑定事件
        /// </summary>
        private static readonly CommandBinding ClearTextCommandBinding;
        /// <summary>
        /// 清除输入框文本值
        /// </summary>
        private static void ClearButtonClick(object sender, ExecutedRoutedEventArgs e)
        {
            var tbox = e.Parameter as FrameworkElement;
            if (tbox == null) return;
            if (tbox is TextBox)
            {
                ((TextBox)tbox).Clear();
            }
            if (tbox is PasswordBox)
            {
                ((PasswordBox)tbox).Clear();
            }
            if (tbox is ComboBox)
            {
                var cb = tbox as ComboBox;
                cb.SelectedItem = null;
                cb.Text = string.Empty;
            }
            if (tbox is DatePicker)
            {
                var dp = tbox as DatePicker;
                dp.SelectedDate = null;
                dp.Text = string.Empty;
            }
            tbox.Focus();
        }

        #endregion
        #region OpenFileCommand 选择文件命令

        /// <summary>
        /// 选择文件命令，需要使用IsClearTextButtonBehaviorEnabledChanged绑定命令
        /// </summary>
        public static RoutedUICommand OpenFileCommand { get; private set; }

        /// <summary>
        /// OpenFileCommand绑定事件
        /// </summary>
        private static readonly CommandBinding OpenFileCommandBinding;

        /// <summary>
        /// 执行OpenFileCommand
        /// </summary>
        private static void OpenFileButtonClick(object sender, ExecutedRoutedEventArgs e)
        {
            var tbox = e.Parameter as FrameworkElement;
            var txt = tbox as TextBox;
            string filter = txt.Tag == null ? "所有文件(*.*)|*.*" : txt.Tag.ToString();
            if (filter.Contains(".bin"))
            {
                filter += "|所有文件(*.*)|*.*";
            }
            if (txt == null) return;
            OpenFileDialog fd = new OpenFileDialog();
            fd.Title = "请选择文件".I18n("ControlAttachProperty_9D1176A5");
            //“图像文件(*.bmp, *.jpg)|*.bmp;*.jpg|所有文件(*.*)|*.*”
            fd.Filter = filter;
            fd.FileName = txt.Text.Trim();
            if (fd.ShowDialog() == true)
            {
                txt.Text = fd.FileName;
            }
            tbox.Focus();
        }

        #endregion

        #region OpenFolderCommand 选择文件夹命令

        /// <summary>
        /// 选择文件夹命令
        /// </summary>
        public static RoutedUICommand OpenFolderCommand { get; private set; }

        /// <summary>
        /// OpenFolderCommand绑定事件
        /// </summary>
        private static readonly CommandBinding OpenFolderCommandBinding;

        /// <summary>
        /// 执行OpenFolderCommand
        /// </summary>
        private static void OpenFolderButtonClick(object sender, ExecutedRoutedEventArgs e)
        {
            var tbox = e.Parameter as FrameworkElement;
            var txt = tbox as TextBox;
            if (txt == null) return;
            FolderBrowserDialog fd = new FolderBrowserDialog();
            fd.Description = "请选择文件路径".I18n("ControlAttachProperty_8E8E06A4");
            fd.SelectedPath = txt.Text.Trim();
            if (fd.ShowDialog() == DialogResult.OK)
            {
                txt.Text = fd.SelectedPath;
            }
            tbox.Focus();
        }

        #endregion

        #region SaveFileCommand 选择文件保存路径及名称

        /// <summary>
        /// 选择文件保存路径及名称
        /// </summary>
        public static RoutedUICommand SaveFileCommand { get; private set; }

        /// <summary>
        /// SaveFileCommand绑定事件
        /// </summary>
        private static readonly CommandBinding SaveFileCommandBinding;

        /// <summary>
        /// 执行OpenFileCommand
        /// </summary>
        private static void SaveFileButtonClick(object sender, ExecutedRoutedEventArgs e)
        {
            var tbox = e.Parameter as FrameworkElement;
            var txt = tbox as TextBox;
            if (txt == null) return;
            SaveFileDialog fd = new SaveFileDialog();
            fd.Title = "文件保存路径".I18n("ControlAttachProperty_951831DC");
            fd.Filter = "所有文件(*.*)|*.*";
            fd.FileName = txt.Text.Trim();
            if (fd.ShowDialog() == DialogResult.OK)
            {
                txt.Text = fd.FileName;
            }
            tbox.Focus();
        }

        #endregion

        #region 子成员的margin
        /// <summary>
        /// 子成员的margin
        /// </summary>
        public static readonly DependencyProperty PartMarginProperty = DependencyProperty.RegisterAttached(
            "PartMargin", typeof(Thickness), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(new Thickness(0, 0, 0, 0)));

        public static Thickness GetPartMargin(DependencyObject d)
        {
            return (Thickness)d.GetValue(PartMarginProperty);
        }

        public static void SetPartMargin(DependencyObject obj, Thickness value)
        {
            obj.SetValue(PartMarginProperty, value);
        }
        #endregion

        public static readonly DependencyProperty ViewBoxChildCanvasProperty = DependencyProperty.RegisterAttached(
            "ViewBoxChildCanvas", typeof(Canvas), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(null));

        [AttachedPropertyBrowsableForType(typeof(Viewbox))]
        public static Canvas GetViewBoxChildCanvas(DependencyObject d)
        {
            return (Canvas)d.GetValue(ViewBoxChildCanvasProperty);
        }

        public static void SetViewBoxChildCanvas(DependencyObject obj, Canvas value)
        {
            obj.SetValue(ViewBoxChildCanvasProperty, value);
            ((Viewbox)obj).Child = value;
        }

        public static readonly DependencyProperty VisibilityProperty = DependencyProperty.RegisterAttached(
           "Visibility", typeof(Visibility), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(Visibility.Collapsed));

        public static void SetVisibility(DependencyObject element, Visibility value) 
            => element.SetValue(VisibilityProperty, value);

        public static Visibility GetVisibility(DependencyObject element) 
            => (Visibility)element.GetValue(VisibilityProperty);



        public static readonly DependencyProperty PlacementProperty = DependencyProperty.RegisterAttached(
            "Placement", typeof(PlacementType), typeof(ControlAttachProperty), new FrameworkPropertyMetadata(default(PlacementType)));

        public static void SetPlacement(DependencyObject element, PlacementType value) 
            => element.SetValue(PlacementProperty, value);

        public static PlacementType GetPlacement(DependencyObject element)
            => (PlacementType)element.GetValue(PlacementProperty);



        public static readonly DependencyProperty StringFormatProperty = DependencyProperty.RegisterAttached(
            "StringFormat", typeof(string), typeof(ControlAttachProperty), new FrameworkPropertyMetadata("#0.0"));

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
