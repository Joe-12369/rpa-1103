using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.Plugin.Dialog.Depends
{
    public class AutoOutSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private Visibility _isEnable;
        public Visibility IsEnable
        {
            get { return this._isEnable; }
            set { this._isEnable = value; OnPropertyChanged(); }
        }

        private int _OutTime;
        public int OutTime
        {
            get { return this._OutTime; }
            set { this._OutTime = value; OnPropertyChanged(); }
        }

        private string _Tip;
        public string Tip
        {
            get { return this._Tip; }
            set { this._Tip = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// 属性值变化时发生
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "none passed")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
