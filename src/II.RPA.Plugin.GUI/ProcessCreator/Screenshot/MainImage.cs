using II.RPA.WinAutomation.CommonUtility;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace II.RPA.Plugin.GUI.ProcessCreator.Screenshot
{
    public class MainImage : Control
    {
        public Point point;

        static MainImage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MainImage), new FrameworkPropertyMetadata(typeof(MainImage)));
        }

        public MainImage()
        {
            _Current = this;
            AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnDragStart));
            AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnDragCompleted));
            AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnDragDelta));
            AddHandler(MouseMoveEvent, new MouseEventHandler(OnMove));
            AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnMouseUp));
            Limit = new Limit();
        }

        #region 属性 Current
        private static MainImage _Current = null;
        public static MainImage Current
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

        #region MoveCursor DependencyProperty
        public Cursor MoveCursor
        {
            get { return (Cursor)GetValue(MoveCursorProperty); }
            set { SetValue(MoveCursorProperty, value); }
        }
        public static readonly DependencyProperty MoveCursorProperty =
                DependencyProperty.Register("MoveCursor", typeof(Cursor), typeof(MainImage),
                new PropertyMetadata(Cursors.SizeAll, new PropertyChangedCallback(MainImage.OnMoveCursorPropertyChanged)));

        private static void OnMoveCursorPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is MainImage)
            {
                (obj as MainImage).OnMoveCursorValueChanged();
            }
        }

        protected void OnMoveCursorValueChanged()
        {

        }
        #endregion

        #region Direction DependencyProperty
        public Direction Direction
        {
            get { return (Direction)GetValue(DirectionProperty); }
            set { SetValue(DirectionProperty, value); }
        }
        public static readonly DependencyProperty DirectionProperty =
                DependencyProperty.Register("Direction", typeof(Direction), typeof(MainImage),
                new PropertyMetadata(Direction.Null, new PropertyChangedCallback(MainImage.OnDirectionPropertyChanged)));

        private static void OnDirectionPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is MainImage)
            {
                (obj as MainImage).OnDirectionValueChanged();
            }
        }

        protected void OnDirectionValueChanged()
        {

        }
        #endregion

        #region Limit DependencyProperty
        public Limit Limit
        {
            get { return (Limit)GetValue(LimitProperty); }
            set { SetValue(LimitProperty, value); }
        }
        public static readonly DependencyProperty LimitProperty =
                DependencyProperty.Register("Limit", typeof(Limit), typeof(MainImage),
                new PropertyMetadata(null, new PropertyChangedCallback(MainImage.OnLimitPropertyChanged)));

        private static void OnLimitPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is MainImage)
            {
                (obj as MainImage).OnLimitValueChanged();
            }
        }

        protected void OnLimitValueChanged()
        {

        }
        #endregion

        #region ZoomThumbVisibility DependencyProperty
        public Visibility ZoomThumbVisibility
        {
            get { return (Visibility)GetValue(ZoomThumbVisibilityProperty); }
            set { SetValue(ZoomThumbVisibilityProperty, value); }
        }
        public static readonly DependencyProperty ZoomThumbVisibilityProperty =
                DependencyProperty.Register("ZoomThumbVisibility", typeof(Visibility), typeof(MainImage),
                new PropertyMetadata(Visibility.Visible, new PropertyChangedCallback(MainImage.OnZoomThumbVisibilityPropertyChanged)));

        private static void OnZoomThumbVisibilityPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is MainImage)
            {
                (obj as MainImage).OnZoomThumbVisibilityValueChanged();
            }
        }

        protected void OnZoomThumbVisibilityValueChanged()
        {

        }
        #endregion

        #region 开始滑动事件
        private void OnDragStart(object sender, DragStartedEventArgs e)
        {
            Direction = (e.OriginalSource as ZoomThumb).Direction;
            //if (SizeColorBar.Current.Selected != Tool.Null)
            //{
            //    point = Mouse.GetPosition(this);
            //    if (SizeColorBar.Current.Selected == Tool.Text)
            //    {
            //        DrawText();
            //    }
            //}
        }
        #endregion

        #region 滑动中事件
        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            CaptureWindow.Current.LocationBorder.Visibility = Visibility.Visible;
            var X = e.HorizontalChange;
            var Y = e.VerticalChange;
            switch (Direction)
            {
                case Direction.Move:
                    //if (SizeColorBar.Current.Selected == Tool.Null)
                    //{
                    OnMove(X, Y);
                    //}
                    //else
                    //{
                    //    switch (SizeColorBar.Current.Selected)
                    //    {
                    //        case Tool.Rectangle:
                    //            DrawRectangle(X, Y);
                    //            break;
                    //        case Tool.Ellipse:
                    //            DrawEllipse(X, Y);
                    //            break;
                    //        case Tool.Arrow:
                    //            DrawArrow(X, Y);
                    //            break;
                    //        case Tool.Line:
                    //            DrawLine(X, Y);
                    //            break;
                    //        case Tool.Text:
                    //            break;
                    //        default:
                    //            break;
                    //    }
                    //}
                    break;
                case Direction.Null:
                    break;
                default:
                    var str = Direction.ToString();
                    if (X != 0)
                    {
                        if (str.Contains("Left"))
                        {
                            Left(X);
                        }
                        if (str.Contains("Right"))
                        {
                            Right(X);
                        }
                    }
                    if (Y != 0)
                    {
                        if (str.Contains("Top"))
                        {
                            Top(Y);
                        }
                        if (str.Contains("Bottom"))
                        {
                            Bottom(Y);
                        }
                    }
                    DataContextModel.Current.ChangeShowSize();
                    break;
            }
            ImageActionBar.Current.ResetCanvas();
        }
        #endregion

        #region 滑动结束事件
        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            CaptureWindow.Current.LocationBorder.Visibility = Visibility.Collapsed;
            if (Direction == Direction.Move)
            {
                //switch (SizeColorBar.Current.Selected)
                //{
                //    case Tool.Rectangle:
                //        if (_Rectangle != null)
                //        {
                //            ResetLimit(Canvas.GetLeft(_Rectangle), Canvas.GetTop(_Rectangle), Canvas.GetLeft(_Rectangle) + _Rectangle.Width, Canvas.GetTop(_Rectangle) + _Rectangle.Height);
                //            CaptureWindow.Register(_Rectangle);
                //            _Rectangle = null;
                //        }
                //        break;
                //    case Tool.Ellipse:
                //        if (_Ellipse != null)
                //        {
                //            ResetLimit(Canvas.GetLeft(_Ellipse), Canvas.GetTop(_Ellipse), Canvas.GetLeft(_Ellipse) + _Ellipse.Width, Canvas.GetTop(_Ellipse) + _Ellipse.Height);
                //            CaptureWindow.Register(_Ellipse);
                //            _Ellipse = null;
                //        }
                //        break;
                //    case Tool.Arrow:
                //        if (_Arrow != null)
                //        {
                //            geometry.Clear();
                //            ResetLimit(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
                //            points = null;
                //            CaptureWindow.Register(_Arrow);
                //            _Arrow = null;
                //        }
                //        break;
                //    case Tool.Line:
                //        if (_Line != null)
                //        {
                //            geometry.Clear();
                //            ResetLimit(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
                //            points = null;
                //            CaptureWindow.Register(_Line);
                //            _Line = null;
                //        }
                //        break;
                //    case Tool.Text:
                //        break;
                //    default:
                //        break;
                //}
            }
            Direction = Direction.Null;
        }
        #endregion

        #region 画矩形
        private void DrawRectangle(double X, double Y)
        {

        }
        #endregion

        #region 画椭圆
        private void DrawEllipse(double X, double Y)
        {

        }
        #endregion

        #region 画箭头
        private void DrawArrow(double X, double Y)
        {

        }
        #endregion

        #region 画刷
        private void DrawLine(double X, double Y)
        {

        }
        #endregion

        #region 添加输入框
        private void DrawText()
        {

        }
        #endregion

        #region 拖动截图区域
        private void OnMove(double X, double Y)
        {
            #region X轴移动
            if (X > 0)
            {
                var max = DataContextModel.Current.MaskRightWidth > Limit.Left - DataContextModel.Current.MaskLeftWidth ? Limit.Left - DataContextModel.Current.MaskLeftWidth : DataContextModel.Current.MaskRightWidth;
                if (X > max)
                {
                    X = max;
                }
            }
            else
            {
                var max = DataContextModel.Current.MaskLeftWidth > DataContextModel.Current.MaskLeftWidth + Width - Limit.Right ? DataContextModel.Current.MaskLeftWidth + Width - Limit.Right : DataContextModel.Current.MaskLeftWidth;
                if (-X > max)
                {
                    X = -max;
                }
            }
            if (X != 0)
            {
                DataContextModel.Current.MaskLeftWidth += X;
                DataContextModel.Current.MaskRightWidth -= X;
                Canvas.SetLeft(this, Canvas.GetLeft(this) + X);
            }
            #endregion

            #region Y轴移动
            if (Y > 0)
            {
                var max = DataContextModel.Current.MaskBottomHeight > Limit.Top - DataContextModel.Current.MaskTopHeight ? Limit.Top - DataContextModel.Current.MaskTopHeight : DataContextModel.Current.MaskBottomHeight;
                if (Y > max)
                {
                    Y = max;
                }
            }
            else
            {
                var max = DataContextModel.Current.MaskTopHeight > DataContextModel.Current.MaskTopHeight + Height - Limit.Bottom ? DataContextModel.Current.MaskTopHeight + Height - Limit.Bottom : DataContextModel.Current.MaskTopHeight;
                if (-Y > max)
                {
                    Y = -max;
                }
            }
            if (Y != 0)
            {
                DataContextModel.Current.MaskTopHeight += Y;
                DataContextModel.Current.MaskBottomHeight -= Y;
                Canvas.SetTop(this, Canvas.GetTop(this) + Y);
            }
            #endregion
        }
        #endregion

        #region 左缩放
        private void Left(double X)
        {
            if (X > 0)
            {
                var max = CaptureWindow.Current.list.Count == 0 ? Width - CaptureWindow.MinSize
                    : Limit.Left - DataContextModel.Current.MaskLeftWidth < Width - CaptureWindow.MinSize ? Limit.Left - DataContextModel.Current.MaskLeftWidth
                    : Width - CaptureWindow.MinSize;
                if (X > max)
                {
                    X = max;
                }
            }
            else
            {
                var max = DataContextModel.Current.MaskLeftWidth;
                if (-X > max)
                {
                    X = -max;
                }
            }
            if (X != 0)
            {
                Width -= X;
                Canvas.SetLeft(this, Canvas.GetLeft(this) + X);
                DataContextModel.Current.MaskLeftWidth += X;
                DataContextModel.Current.MaskTopWidth -= X;
            }
        }
        #endregion

        #region 右缩放
        private void Right(double X)
        {
            if (X > 0)
            {
                var max = DataContextModel.Current.MaskRightWidth;
                if (X > max)
                {
                    X = max;
                }
            }
            else
            {
                var max = CaptureWindow.Current.list.Count == 0 ? Width - CaptureWindow.MinSize
                    : DataContextModel.Current.MaskLeftWidth + Width - Limit.Right < Width - CaptureWindow.MinSize ? DataContextModel.Current.MaskLeftWidth + Width - Limit.Right
                    : Width - CaptureWindow.MinSize;
                if (-X > max)
                {
                    X = -max;
                }
            }
            if (X != 0)
            {
                Width += X;
                DataContextModel.Current.MaskRightWidth -= X;
                DataContextModel.Current.MaskTopWidth += X;
            }
        }
        #endregion

        #region 上缩放
        private void Top(double Y)
        {
            if (Y > 0)
            {
                var max = CaptureWindow.Current.list.Count == 0 ? Height - CaptureWindow.MinSize
                    : Limit.Top - DataContextModel.Current.MaskTopHeight < Height - CaptureWindow.MinSize ? Limit.Top - DataContextModel.Current.MaskTopHeight
                    : Height - CaptureWindow.MinSize;
                if (Y > max)
                {
                    Y = max;
                }
            }
            else
            {
                var max = DataContextModel.Current.MaskLeftWidth;
                if (-Y > max)
                {
                    Y = -max;
                }
            }
            if (Y != 0)
            {
                Height -= Y;
                Canvas.SetTop(this, Canvas.GetTop(this) + Y);
                DataContextModel.Current.MaskTopHeight += Y;
            }
        }
        #endregion

        #region 下缩放
        private void Bottom(double Y)
        {
            if (Y > 0)
            {
                var max = DataContextModel.Current.MaskBottomHeight;
                if (Y > max)
                {
                    Y = max;
                }
            }
            else
            {
                var max = CaptureWindow.Current.list.Count == 0 ? Height - CaptureWindow.MinSize
                    : DataContextModel.Current.MaskTopHeight + Height - Limit.Bottom < Height - CaptureWindow.MinSize ? DataContextModel.Current.MaskTopHeight + Height - Limit.Bottom
                    : Height - CaptureWindow.MinSize;
                if (-Y > max)
                {
                    Y = -max;
                }
            }
            if (Y != 0)
            {
                Height += Y;
                DataContextModel.Current.MaskBottomHeight -= Y;
            }
        }
        #endregion

        #region 刷新RGB
        private void OnMove(object sender, MouseEventArgs e)
        {
            var point = PointToScreen(e.GetPosition(this));
            DataContextModel.Current.ShowRGB = ImageHelper.GetRGB((int)point.X, (int)point.Y);
        }
        #endregion

        #region 计算图片移动的极限值
        public void ResetLimit(double left, double top, double right, double bottom)
        {
            ResetLeft(left);
            ResetTop(top);
            ResetRight(right);
            ResetButtom(bottom);
        }

        private void ResetLeft(double value)
        {
            if (value < Limit.Left)
            {
                Limit.Left = value;
            }
        }

        private void ResetTop(double value)
        {
            if (value < Limit.Top)
            {
                Limit.Top = value;
            }
        }

        private void ResetRight(double value)
        {
            if (value > Limit.Right)
            {
                Limit.Right = value;
            }
        }

        private void ResetButtom(double value)
        {
            if (value > Limit.Bottom)
            {
                Limit.Bottom = value;
            }
        }
        #endregion
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            CaptureWindow.Current.Window_MouseLeftButtonUp(sender, e);
        }
    }
}
