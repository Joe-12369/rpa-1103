using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace II.RPA.Plugin.GUI.DataCollection.DataTypeConverter
{
    public class MultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 4 && values[0] != null && values[1] != null && values[2] != null && values[3] != null)
            {
                string strName = values[0] as string;
                Popup _popup = values[1] as Popup;
                DataGrid dataGrid = values[2] as DataGrid;
                Grid _Grid = values[3] as Grid;
                MultiValueData _multiValueData = new MultiValueData();
                if (_multiValueData != null)
                {
                    _multiValueData.ColName = strName;
                    _multiValueData.PopupEx = _popup;
                    _multiValueData.DataGridEx = dataGrid;
                    _multiValueData.GridEx = _Grid;
                    return _multiValueData;
                }
            }
            else if (values.Length == 2 && values[0] != null && values[1] != null)
            {
                string strName = values[0] as string;
                Popup _popup = values[1] as Popup;
                //DataGrid dataGrid = values[2] as DataGrid;
                //Grid _Grid = values[3] as Grid;
                MultiValueData _multiValueData = new MultiValueData();
                if (_multiValueData != null)
                {
                    _multiValueData.ColName = strName;
                    _multiValueData.PopupEx = _popup;
                    //_multiValueData.DataGridEx = dataGrid;
                    //_multiValueData.GridEx = _Grid;
                    return _multiValueData;
                }
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MultiValueData
    {
        public string ColName = string.Empty;
        public Popup PopupEx = null;
        public DataGrid DataGridEx = null;
        public Grid GridEx = null;
    }

    public class DictionaryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
