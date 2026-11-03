using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace II.RPA.PatchUpdate
{
    public class BaseWindow : Window
    {
        public ICommand CloseWindowCommand { get; protected set; }
        public ICommand MaximizeWindowCommand { get; protected set; }
        public ICommand MinimizeWindowCommand { get; protected set; }
        public ICommand DoubleClickMaximizeWindowCommand { get; protected set; }

        public BaseWindow()
        {


            //this.Style = this.FindResource("AmssWindowStyle") as Style;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.MaxHeight = SystemParameters.WorkArea.Height;
            this.MaxWidth = SystemParameters.WorkArea.Width;


            this.CloseWindowCommand = new RoutedUICommand();
            this.MaximizeWindowCommand = new RoutedUICommand();
            this.MinimizeWindowCommand = new RoutedUICommand();
            this.DoubleClickMaximizeWindowCommand = new RoutedUICommand();


            var bind = new CommandBinding(CloseWindowCommand);
            bind.Executed += new ExecutedRoutedEventHandler(CloseCommand_Execute);
            this.CommandBindings.Add(bind);

            var bind1 = new CommandBinding(MaximizeWindowCommand);
            bind1.Executed += new ExecutedRoutedEventHandler(MaxCommand_Execute);
            this.CommandBindings.Add(bind1);

            var bind3 = new CommandBinding(MinimizeWindowCommand);
            bind3.Executed += new ExecutedRoutedEventHandler(MinCommand_Execute);
            this.CommandBindings.Add(bind3);

            var bind4 = new CommandBinding(DoubleClickMaximizeWindowCommand);
            //bind4.Executed += new ExecutedRoutedEventHandler(DoubleClickMaxCommand_Execute);
        }



        private void CloseCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
        private void MaxCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
        }

        private void MinCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            e.Handled = true;
        }

        private void DoubleClickMaxCommand_Execute(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                e.Handled = true;
            }
        }

        public static readonly DependencyProperty AIconProperty =
            DependencyProperty.Register("AIcon", typeof(Brush), typeof(BaseWindow), new PropertyMetadata(Brushes.DarkCyan));

        public Brush AIcon { get { return (Brush)GetValue(AIconProperty); } set { SetValue(AIconProperty, value); } }

        public static readonly DependencyProperty AContentProperty =
            DependencyProperty.Register("AContent", typeof(object), typeof(BaseWindow), new PropertyMetadata(""));

        public object AContent { get { return GetValue(AContentProperty); } set { SetValue(AContentProperty, value); } }

        public static readonly DependencyProperty IsShowCancleProperty =
            DependencyProperty.Register("IsShowCancle", typeof(bool), typeof(BaseWindow), new PropertyMetadata(false));

        public bool IsShowCancle { get { return (bool)GetValue(IsShowCancleProperty); } set { SetValue(IsShowCancleProperty, value); } }
    }
}
