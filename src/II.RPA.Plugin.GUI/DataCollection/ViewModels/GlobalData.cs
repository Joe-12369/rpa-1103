using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.ViewModels
{
    /// <summary>
    /// 全局变量集合
    /// </summary>
    public class GlobalData:SingleInstance<GlobalData>
    {
        private MainWindowViewModel _MainWindowViewModel = new MainWindowViewModel();
        public MainWindowViewModel VM_MainWindowViewModel
        {
            get { return _MainWindowViewModel; }
            set { _MainWindowViewModel = value; }
        }
        //public MainWindowViewModel VM_MainWindowViewModel = new MainWindowViewModel();
    }
}
