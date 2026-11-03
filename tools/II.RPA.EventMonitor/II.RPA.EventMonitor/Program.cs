using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.EventMonitor
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (Utils.IsExistsSameProcess())
            {
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

        }
    }
}
