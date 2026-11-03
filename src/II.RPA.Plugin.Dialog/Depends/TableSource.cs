using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Dialog.Depends
{
    class TableSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _SelectColumn;
        public string SelectColumn
        {
            get { return this._SelectColumn; }
            set { this._SelectColumn = value; OnPropertyChanged(); }
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
