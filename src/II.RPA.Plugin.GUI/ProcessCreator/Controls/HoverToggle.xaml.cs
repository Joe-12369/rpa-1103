using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace II.RPA.Plugin.GUI.ProcessCreator.Controls
{
    /// <summary>
    /// Interaction logic for HoverToggle.xaml
    /// </summary>
    public partial class HoverToggle : UserControl
    {
         public static readonly RoutedEvent DissEvent = EventManager.RegisterRoutedEvent("DissButtonClick",
             RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HoverToggle));

         public event RoutedEventHandler DissButtonClick
         {
             add
             {
                 AddHandler(DissEvent, value);
             }
             remove
             {
                 RemoveHandler(DissEvent, value);
             }
         }

        private bool isOpen = false;
        public HoverToggle()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            SwitchPop(true);
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            SwitchPop(false);
        }

        private void SwitchPop(bool targetState)
        {
            Task.Factory.StartNew(
            () =>
            {
                isOpen = targetState;
                if (!isOpen)
                {
                    new ValueTask(Task.Delay(500)).GetAwaiter().GetResult();
                }
                else
                {
                    new ValueTask(Task.Delay(100)).GetAwaiter().GetResult();
                }
                Dispatcher.Invoke(() => { this.popup.IsOpen = isOpen; });
            },
            TaskCreationOptions.LongRunning).ConfigureAwait(false)
            .GetAwaiter()
            .OnCompleted(() => { });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs arg = new RoutedEventArgs
            {
                RoutedEvent = DissEvent
            };
            SwitchPop(false);
            RaiseEvent(arg);
            this.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }

        private void HoverButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchPop(true);
            e.Handled = true;
        }
    }
}
