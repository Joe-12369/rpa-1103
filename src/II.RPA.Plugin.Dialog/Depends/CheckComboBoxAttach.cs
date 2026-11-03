using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.Plugin.Dialog.Depends
{
    public class CheckComboBoxAttach
    {
        public static IList<object> GetSelections(DependencyObject obj)
        {
            return (IList<object>)obj.GetValue(SelectionsProperty);
        }

        public static void SetSelections(DependencyObject obj, IList<object> value)
        {
            obj.SetValue(SelectionsProperty, value);
        }

        public static readonly DependencyProperty SelectionsProperty =
            DependencyProperty.RegisterAttached(
                "Selections",
                typeof(IList<object>),
                typeof(CheckComboBoxAttach),
                new PropertyMetadata(
                    null,
                    (s, e) =>
                    {
                        var newValue = ((IList<object>)e.NewValue)?.ToArray();

                        var ccb = (CheckComboBox)s;
                        ccb.SelectedItems.Clear();

                        if (newValue != null)
                        {
                            foreach (var item in newValue)
                            {
                                ccb.SelectedItems.Add(item);
                            }
                        }
                    }));
    }
}
