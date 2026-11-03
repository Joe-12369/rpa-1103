using II.RPA.WinAutomation.CommonUtility.Utils;
using System.Windows;
using System.Windows.Controls;

namespace II.RPA.Plugin.GUI.ProcessCreator.Screenshot
{
    /// <summary>
    /// 截图模式 推荐菜单
    /// </summary>
    public class ImageActionBar : Control
    {
        static ImageActionBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageActionBar), new FrameworkPropertyMetadata(typeof(ImageActionBar)));
        }

        public ImageActionBar()
        {
            _Current = this;
            if (RPAEnvironment.GetInstance().CurrentLanguage == LanguageCultureEnum.zh_CN)
            {
                this.Width = 610;
            }
            else 
            {
                this.Width = 396;
            }
        }

        #region 属性 Current
        private static ImageActionBar _Current = null;
        public static ImageActionBar Current
        {
            get
            {
                return _Current;
            }
            set
            {
                _Current = value;
            }
        }
        #endregion

        #region 定位
        public void ResetCanvas()
        {
            ResetCanvasLeft();
            ResetCanvasTop();
        }
        #endregion

        #region CanvasLeft DependencyProperty
        public double CanvasLeft
        {
            get { return (double)GetValue(CanvasLeftProperty); }
            set { SetValue(CanvasLeftProperty, value); }
        }
        public static readonly DependencyProperty CanvasLeftProperty =
                DependencyProperty.Register("CanvasLeft", typeof(double), typeof(ImageActionBar),
                new PropertyMetadata(0.0, new PropertyChangedCallback(ImageActionBar.OnCanvasLeftPropertyChanged)));

        private static void OnCanvasLeftPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is ImageActionBar)
            {
                (obj as ImageActionBar).OnCanvasLeftValueChanged();
            }
        }

        protected void OnCanvasLeftValueChanged()
        {

        }

        private void ResetCanvasLeft()
        {
            CanvasLeft = DataContextModel.Current.MaskRightWidth > CaptureWindow.ScreenWidth - Width ? 0 : CaptureWindow.ScreenWidth - DataContextModel.Current.MaskRightWidth - Width;
        }
        #endregion

        #region CanvasTop DependencyProperty
        public double CanvasTop
        {
            get { return (double)GetValue(CanvasTopProperty); }
            set { SetValue(CanvasTopProperty, value); }
        }
        public static readonly DependencyProperty CanvasTopProperty =
                DependencyProperty.Register("CanvasTop", typeof(double), typeof(ImageActionBar),
                new PropertyMetadata(0.0, new PropertyChangedCallback(ImageActionBar.OnCanvasTopPropertyChanged)));

        private static void OnCanvasTopPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is ImageActionBar)
            {
                (obj as ImageActionBar).OnCanvasTopValueChanged();
            }
        }

        protected void OnCanvasTopValueChanged()
        {

        }

        private void ResetCanvasTop()
        {
            var need = ActualHeight + 0;
            CanvasTop = DataContextModel.Current.MaskBottomHeight >= need ? CaptureWindow.ScreenHeight - DataContextModel.Current.MaskBottomHeight + 5
                : DataContextModel.Current.MaskTopHeight >= need ? DataContextModel.Current.MaskTopHeight - ActualHeight
                : DataContextModel.Current.MaskTopHeight;
        }
        #endregion

        #region ConfirmButtonVisibility DependencyProperty
        public Visibility ConfirmButtonVisibility
        {
            get { return (Visibility)GetValue(ConfirmButtonVisibilityProperty); }
            set { SetValue(ConfirmButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty ConfirmButtonVisibilityProperty =
                DependencyProperty.Register("ConfirmButtonVisibility", typeof(Visibility), typeof(ImageActionBar),
                new PropertyMetadata(Visibility.Collapsed));
        #endregion
        #region SaveButtonVisibility DependencyProperty
        public Visibility SaveButtonVisibility
        {
            get { return (Visibility)GetValue(SaveButtonVisibilityProperty); }
            set { SetValue(SaveButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty SaveButtonVisibilityProperty =
                DependencyProperty.Register("SaveButtonVisibility", typeof(Visibility), typeof(ImageActionBar),
                new PropertyMetadata(Visibility.Visible));
        #endregion

        #region IsCaptureOnceTime DependencyProperty
        public bool IsCaptureOnceTime
        {
            get { return (bool)GetValue(IsCaptureOnceTimeProperty); }
            set { SetValue(IsCaptureOnceTimeProperty, value); }
        }
        public static readonly DependencyProperty IsCaptureOnceTimeProperty =
                DependencyProperty.Register("IsCaptureOnceTime", typeof(bool), typeof(ImageActionBar),
                new PropertyMetadata(false, new PropertyChangedCallback(ImageActionBar.OnIsCaptureOnceTimePropertyChanged)));

        private static void OnIsCaptureOnceTimePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is ImageActionBar)
            {
                (obj as ImageActionBar).OnIsCaptureOnceTimeChanged(e.NewValue);
            }
        }

        protected void OnIsCaptureOnceTimeChanged(object value)
        {
            if (value is bool isOnceTime)
            {
                ConfirmButtonVisibility = isOnceTime ? Visibility.Visible : Visibility.Collapsed;
                SaveButtonVisibility = isOnceTime ? Visibility.Collapsed : Visibility.Visible;
                Width = isOnceTime ? 88 : Width;
            }
            
        }

        #endregion
    }
}
