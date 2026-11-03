using System.Windows;
using System.Windows.Input;

namespace II.RPA.ZDeskTop.Views;

/// <summary>
/// Interaction logic for OverlayWindow.xaml
/// </summary>
public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += OverlayWindow_MouseLeftButtonDown;
    }


    private void OverlayWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            Owner?.DragMove();
        }
    }

}
