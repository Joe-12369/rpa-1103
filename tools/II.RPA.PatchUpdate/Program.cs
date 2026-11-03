using II.RPA.PatchUpdate.Properties;
using System;
using System.Configuration;
using System.Windows;

namespace II.RPA.PatchUpdate
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        public static void Main()
        {
            try
            {
                string runningProcessNames = ConfigurationManager.AppSettings["CheckRunningProcessNames"];
                string[] arrRunningProcessNames = runningProcessNames.Split(';');
                string callBackExeName = ConfigurationManager.AppSettings["CallbackExeName"];
                string title = ConfigurationManager.AppSettings["Title"];

                string needKillProcessNames = "";
                foreach (string name in arrRunningProcessNames)
                {
                    if (Helpers.ApplicationHelper.FindExistByProcessName(name))
                    {
                        needKillProcessNames += $"【{name}】，";
                    }
                }

                bool executeUpdate = false;
                if (needKillProcessNames != "")
                {
                    if (MessageBox.Show($"检测到{needKillProcessNames.Substring(0, needKillProcessNames.Length - 1)}正在运行，点击【确定】将强制关闭已运行的程序，是否继续？", "提示信息", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                    {
                        foreach (string processName in arrRunningProcessNames)
                        {
                            if (Helpers.ApplicationHelper.KillExistByProcessName(processName))
                            {
                                executeUpdate = true;
                            }
                            else
                            {
                                MessageBox.Show(Resources.FailedToCloseTargetProcess.Replace("$0", "【" + processName + "】"));
                                return;
                            }
                        }

                    }
                }
                else
                {
                    executeUpdate = true;
                }

                if (executeUpdate)
                {
                    //App app = new App();
                    //app.InitializeComponent();
                    var MainWindow = new MainWindow(callBackExeName, title);
                    MainWindow.ShowDialog();
                    //app.Run();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
