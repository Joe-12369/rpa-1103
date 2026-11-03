using II.Framework.Utility;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace II.RPA.ZDeskTop.Views;

/// <summary>
/// LoginWindow.xaml 的交互逻辑
/// </summary>
public partial class LoginWindow : Window, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private readonly UserInfo? _lastUserInfo;
    private readonly bool _dialog = false;
    public LoginWindow(bool dialog = false)
    {
        InitializeComponent();
        this.DataContext = this;
        TipsVisibility = Visibility.Hidden;
        _lastUserInfo = WindowsLogon.GetCurrentCacheUserInfo();
        if (_lastUserInfo.HasValue)
        {
            UserName = _lastUserInfo.Value.UserName;
            Password = _lastUserInfo.Value.Password;
        }
        _dialog = dialog;
        Loaded += MessageWindow_Loaded;
    }

    private void MessageWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(UserName))
        {
            WindowsLogon.GetCurrentUserAndMachineName(out string uName, out string mName);
            UserName = uName;
        }
    }

    private string userName;
    public string UserName
    {
        get 
        { 
            return userName; 
        }
        set 
        {
            userName = value; 
            RaisePropertyChanged(); 
        }
    }
    private string password;
    public string Password
    {
        get
        {
            return password;
        }
        set
        {
            password = value;
            RaisePropertyChanged();
        }
    }
    private Visibility tipsVisibility;
    public Visibility TipsVisibility
    {
        get
        {
            return tipsVisibility;
        }
        set
        {
            tipsVisibility = value;
            RaisePropertyChanged();
        }
    }


    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (_dialog)
        {
            this.DialogResult = false;
        }
        this.Close();
    }
    private void ConfirmClick(object sender, RoutedEventArgs e)
    {
        WindowsLogon.GetCurrentUserAndMachineName(out string uName, out string mName);
        if (string.IsNullOrWhiteSpace(Password))
        {
            Password = string.Empty;
        }
        if (WindowsLogon.WindowsIdentityVerify(UserName, Password, mName))
        {
            UserInfo userInfo = new UserInfo
            {
                UserName = UserName,
                Password = Password
            };
            if (!userInfo.Equals(_lastUserInfo))
            {
                WindowsLogon.SetCurrentCacheUserInfo(userInfo);
            }
            if (_dialog)
            {
                this.DialogResult = true;
            }
            this.Close();
        }
        else
        {
            TipsVisibility = Visibility.Visible;
        }
    }
    private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Normal)
                {
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    WindowState = WindowState.Normal;
                }
            }
            else
            {
                DragMove();
            }
        }
    }
    private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    private void Button_LostFocus(object sender, RoutedEventArgs e)
    {
        TipsVisibility = Visibility.Hidden;
    }

}
