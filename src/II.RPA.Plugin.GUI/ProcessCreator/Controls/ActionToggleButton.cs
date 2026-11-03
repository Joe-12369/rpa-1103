using II.RPA.Plugin.GUI.ProcessCreator.AttachProperty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace II.RPA.Plugin.GUI.ProcessCreator.Controls
{
    public class ActionToggleButton : ToggleButton
    {

        private const string LOOP_KEY = "logic_control_count_for";
        #region ActionBoxWatermarkProperty
        public string ActionBoxWatermark
        {
            get { return (string)GetValue(ActionBoxWatermarkProperty); }
            set { SetValue(ActionBoxWatermarkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActionBoxWatermark.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActionBoxWatermarkProperty =
            DependencyProperty.Register("ActionBoxWatermark", typeof(string), typeof(ActionToggleButton), new PropertyMetadata(""));
        #endregion



        public BitmapImage ActionIconImage
        {
            get { return (BitmapImage)GetValue(ActionIconImageProperty); }
            set { SetValue(ActionIconImageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActionIconImage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActionIconImageProperty =
            DependencyProperty.Register("ActionIconImage", typeof(BitmapImage), typeof(ActionToggleButton), new PropertyMetadata(new BitmapImage(new Uri($"pack://application:,,,/II.RPA.Plugin.GUI;Component/ProcessCreator/Theme/Images/icon-default.png"))));

        #region AttachContentProperty 附加组件模板
        /// <summary>
        /// 附加组件模板
        /// </summary>
        public static readonly DependencyProperty AttachContentProperty = DependencyProperty.Register(
            "AttachContent", typeof(ControlTemplate), typeof(ActionToggleButton), new FrameworkPropertyMetadata(null));

        public ControlTemplate AttachContent
        {
            get { return (ControlTemplate)GetValue(AttachContentProperty); }
            set { SetValue(AttachContentProperty, value); }
        }
        #endregion

        #region AttachContentValueProperty 
        /// <summary>
        /// 附加组件模板
        /// </summary>
        public static readonly DependencyProperty AttachContentValueProperty = DependencyProperty.Register(
            "AttachContentValue", typeof(string), typeof(ActionToggleButton), new FrameworkPropertyMetadata("0", (s, e) =>
            {
                if (s is ActionToggleButton control)
                {
                    //
                }
            }));

        public string AttachContentValue
        {
            get { return (string)GetValue(AttachContentValueProperty); }
            set { SetValue(AttachContentValueProperty, value); }
        }
        #endregion

        #region ActionButtonIconCode 按钮Icon Code
        public string ActionButtonIconCode
        {
            get { return (string)GetValue(ActionButtonIconCodeProperty); }
            set { SetValue(ActionButtonIconCodeProperty, value); }
        }
        public static readonly DependencyProperty ActionButtonIconCodeProperty =
            DependencyProperty.Register("ActionButtonIconCode", typeof(string), typeof(ActionToggleButton), new PropertyMetadata(""));
        #endregion


        #region AttachContentDefaultValueProperty 
        /// <summary>
        /// 附加组件模板
        /// </summary>
        public static readonly DependencyProperty AttachContentDefaultValueProperty = DependencyProperty.Register(
            "AttachContentDefaultValue", typeof(string), typeof(ActionToggleButton), new FrameworkPropertyMetadata("0", (s, e) =>
            {
                if (s is ActionToggleButton control)
                {
                    try
                    {
                        control.AttachContentValue = (string)e.NewValue;
                    }
                    catch
                    { }
                }
            }));

        public string AttachContentDefaultValue
        {
            get { return (string)GetValue(AttachContentDefaultValueProperty); }
            set { SetValue(AttachContentDefaultValueProperty, value); }
        }
        #endregion
    }
}
