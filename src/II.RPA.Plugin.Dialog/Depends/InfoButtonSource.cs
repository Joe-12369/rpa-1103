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
    public class InfoButtonSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Visibility _isEnableConfirm;
        public Visibility IsEnableConfirm
        {
            get { return this._isEnableConfirm; }
            set { this._isEnableConfirm = value; OnPropertyChanged(); }
        }

        private Visibility _isEnableCancel;
        public Visibility IsEnableCancel
        {
            get { return this._isEnableCancel; }
            set { this._isEnableCancel = value; OnPropertyChanged(); }
        }

        private Visibility _isEnableRetry;
        public Visibility IsEnableRetry
        {
            get { return this._isEnableRetry; }
            set { this._isEnableRetry = value; OnPropertyChanged(); }
        }

        private Visibility _isEnableAbort;
        public Visibility IsEnableAbort
        {
            get { return this._isEnableAbort; }
            set { this._isEnableAbort = value; OnPropertyChanged(); }
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
