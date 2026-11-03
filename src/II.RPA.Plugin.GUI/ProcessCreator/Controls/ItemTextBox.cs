using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Reflection;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;
using System.Windows.Input;
using II.RPA.Plugin.GUI.ProcessCreator.AttachProperty;

namespace II.RPA.Plugin.GUI.ProcessCreator.Controls
{
    [TemplatePart(Name = PART_InputTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_ItemTextBorder, Type = typeof(Border))]
    [TemplatePart(Name = PART_ItemTextBlock, Type = typeof(TextBlock))]
    [TemplatePart(Name = PART_ItemTextClearButton, Type = typeof(Button))]
    public class ItemTextBox : UserControl
    {
        private static readonly ILogger logger = Logger.OfContext();

        private const string PART_InputTextBox = "PART_InputTextBox";
        private const string PART_ItemTextBorder = "PART_ItemTextBorder";
        private const string PART_ItemTextBlock = "PART_ItemTextBlock";
        private const string PART_ItemTextClearButton = "PART_ItemTextClearButton";
        public ItemTextBox()
        {
            Unloaded += ItemTextBox_Unloaded;
            Loaded += ItemTextBox_Loaded;
        }

        internal TextBox Element_PART_InputTextBox { get; set; }
        internal Border Element_PART_ItemTextBorder { get; set; }
        internal TextBlock Element_PART_ItemTextBlock { get; set; }
        internal Button Element_PART_ItemTextClearButton { get; set; }

        #region ItemText
        public string ItemText
        {
            get { return (string)GetValue(ItemTextProperty); }
            set { SetValue(ItemTextProperty, value); }
        }

        public static readonly DependencyProperty ItemTextProperty =
            DependencyProperty.Register("ItemText", typeof(string), typeof(ItemTextBox), new PropertyMetadata(string.Empty, ItemTextPropertyChanged));
        #endregion

        #region InputText
        public string InputText
        {
            get { return (string)GetValue(InputTextProperty); }
            set { SetValue(InputTextProperty, value); }
        }

        public static readonly DependencyProperty InputTextProperty =
            DependencyProperty.Register("InputText", typeof(string), typeof(ItemTextBox), new PropertyMetadata(string.Empty, InputTextPropertyChanged));
        #endregion

        #region Password
        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string), typeof(ItemTextBox), new PropertyMetadata(string.Empty));
        #endregion


        #region Mode
        public ItemTextBoxMode Mode
        {
            get { return (ItemTextBoxMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", 
                typeof(ItemTextBoxMode), 
                typeof(ItemTextBox), 
                new PropertyMetadata(defaultValue: ItemTextBoxMode.TextMode));
        #endregion

        #region IsPassword
        public bool IsPassword
        {
            get { return (bool)GetValue(IsPasswordProperty); }
            set { SetValue(IsPasswordProperty, value); }
        }

        public static readonly DependencyProperty IsPasswordProperty =
            DependencyProperty.Register("IsPassword",
                typeof(bool),
                typeof(ItemTextBox),
                new PropertyMetadata(defaultValue: false));
        #endregion

        #region InputTextWaterMark
        public string InputTextWaterMark
        {
            get { return (string)GetValue(InputTextWaterMarkProperty); }
            set { SetValue(InputTextWaterMarkProperty, value); }
        }

        public static readonly DependencyProperty InputTextWaterMarkProperty =
            DependencyProperty.Register("InputTextWaterMark", typeof(string), typeof(ItemTextBox), new PropertyMetadata(string.Empty));
        #endregion

        #region InputTextContentVerticalAlignment
        public VerticalAlignment InputTextContentVerticalAlignment
        {
            get { return (VerticalAlignment)GetValue(InputTextContentVerticalAlignmentProperty); }
            set { SetValue(InputTextContentVerticalAlignmentProperty, value); }
        }

        public static readonly DependencyProperty InputTextContentVerticalAlignmentProperty =
            DependencyProperty.Register("InputTextContentVerticalAlignment", typeof(VerticalAlignment), typeof(ItemTextBox), new PropertyMetadata(VerticalAlignment.Top));
        #endregion


        #region IsNeedFocus
        public bool IsNeedFocus
        {
            get { return (bool)GetValue(IsNeedFocusProperty); }
            set { SetValue(IsNeedFocusProperty, value); }
        }

        public static readonly DependencyProperty IsNeedFocusProperty =
            DependencyProperty.Register("IsNeedFocus",
                typeof(bool),
                typeof(ItemTextBox),
                new PropertyMetadata(defaultValue: true));
        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Element_PART_InputTextBox = GetTemplateChild(PART_InputTextBox) as TextBox;
            Element_PART_ItemTextBorder = GetTemplateChild(PART_ItemTextBorder) as Border;
            Element_PART_ItemTextBlock = GetTemplateChild(PART_ItemTextBlock) as TextBlock;
            Element_PART_ItemTextClearButton = GetTemplateChild(PART_ItemTextClearButton) as Button;
            if (Element_PART_ItemTextClearButton != null)
            {
                Element_PART_ItemTextClearButton.Click += Element_PART_ItemTextClearButton_Click;
            }
            if (Element_PART_InputTextBox != null)
            {
                Element_PART_InputTextBox.TextChanged += Element_PART_InputTextBox_TextChanged;
                if (IsNeedFocus)
                {
                    Element_PART_InputTextBox.Focus();
                }
            }
            var onlyNumber = this.GetValue(TextBoxAttachedProperties.IsOnlyNumberProperty);
            logger.LogDebug($"onlyNumber:{onlyNumber}");
            if (onlyNumber is bool flag)
            {
                if (Element_PART_InputTextBox != null)
                {
                    Element_PART_InputTextBox.SetValue(InputMethod.IsInputMethodEnabledProperty, !flag);
                    Element_PART_InputTextBox.PreviewTextInput -= TextBoxAttachedProperties.TxtInput;
                    if (flag)
                    {
                        Element_PART_InputTextBox.PreviewTextInput += TextBoxAttachedProperties.TxtInput;
                    }
                    Element_PART_InputTextBox.Unloaded += (sender, arg) => Element_PART_InputTextBox.PreviewTextInput -= TextBoxAttachedProperties.TxtInput;
                }
            }
        }

        private void Element_PART_InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            InputText = Element_PART_InputTextBox.Text;
            if (IsPassword && Element_PART_InputTextBox is PasswordTextBox box)
            {
                Password = box.Password;
                logger.LogDebug($"Element_PART_InputTextBox:{Password}");
            }
            logger.LogDebug($"Element_PART_InputTextBox:{InputText}");
        }

        private static void ItemTextPropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            if (dobj is ItemTextBox itemTextBox)
            {
                var textborder = itemTextBox.Element_PART_ItemTextBorder;
                if (textborder != null)
                {
                    if (e != null && !string.IsNullOrWhiteSpace((string)(e.NewValue)))
                    {
                        textborder.Visibility = Visibility.Visible;
                        itemTextBox.Mode = ItemTextBoxMode.ItemMode;
                        var inputbox = itemTextBox.Element_PART_InputTextBox;
                        if (inputbox != null)
                        {
                            inputbox.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        textborder.Visibility = Visibility.Collapsed;
                        itemTextBox.Mode = ItemTextBoxMode.TextMode;
                        var inputbox = itemTextBox.Element_PART_InputTextBox;
                        if (inputbox != null)
                        {
                            inputbox.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }
        private static void InputTextPropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {

        }
        private void ItemTextBox_Loaded(object sender, RoutedEventArgs e)
        {
 
        }

        private void Element_PART_ItemTextClearButton_Click(object sender, RoutedEventArgs e)
        {
            Element_PART_ItemTextBorder.Visibility = Visibility.Collapsed;
            Element_PART_InputTextBox.Visibility = Visibility.Visible;
            ItemText = string.Empty;
            //Mode = ItemTextBoxMode.TextMode;
            //ItemText = string.Empty;
        }

        private void ItemTextBox_Unloaded(object sender, RoutedEventArgs e)
        {
            //if (Element_PART_ItemTextClearButton != null)
            //{
            //    Element_PART_ItemTextClearButton.Click -= Element_PART_ItemTextClearButton_Click;
            //    Element_PART_InputTextBox.TextChanged -= Element_PART_InputTextBox_TextChanged;
            //}
        }
    }

    public enum ItemTextBoxMode
    {
        ItemMode = 0,
        TextMode = 1,
    }
}
