using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace II.RPA.ZDeskTop.Views;

/// <summary>
/// Interaction logic for MessageDialog.xaml
/// </summary>
public partial class MessageDialog : Window
{
    public MessageDialog(UserControl view)
    {
        InitializeComponent();
        Content = view;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
