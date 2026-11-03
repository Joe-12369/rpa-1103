using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace II.RPA.Plugin.Dialog.Depends
{
    public class DependRequired : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _IsRequired;
        public string IsRequired
        {
            get => _IsRequired;
            set
            {
                _IsRequired = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRequired"));
            }
        }

        private Visibility _EmptyVisibity = Visibility.Collapsed;
        public Visibility EmptyVisibity
        {
            get => _EmptyVisibity;
            set
            {
                _EmptyVisibity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EmptyVisibity"));
            }
        }

        private SolidColorBrush _WarningColor;
        public SolidColorBrush WarningColor
        {
            get => _WarningColor;
            set
            {
                _WarningColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WarningColor"));
            }
        }
    }
}
