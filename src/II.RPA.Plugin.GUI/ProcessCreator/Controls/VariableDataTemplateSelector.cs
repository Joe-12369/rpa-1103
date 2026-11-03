using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using II.RPA.Plugin.GUI.ProcessCreator.Models;

namespace II.RPA.Plugin.GUI.ProcessCreator.Controls
{
    public class VariableDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate VariableTemplate { get; set; }
        public DataTemplate VariableCollectionTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is VariableModel model)
            {
                if (model.HasMember)
                {
                    return VariableCollectionTemplate;
                }
            }
            return VariableTemplate;
        }
    }
}
