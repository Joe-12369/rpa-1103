using II.RPA.Plugin.GUI.ProcessCreator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace II.RPA.Plugin.GUI.ProcessCreator.Controls
{
    public class VariableItemStyleSelector:StyleSelector
    {
        public Style VariableStyle { get; set; }
        public Style VariableCollectionStyle { get; set; }
        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is VariableModel model)
            {
                if (model.HasMember)
                {
                    return VariableCollectionStyle;
                }
            }
            return VariableStyle;
        }
    }
}
