using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace II.RPA.Plugin.GUI.ProcessCreator.Theme
{
    public class LogicItemTemplateSelector : DataTemplateSelector
    {
        //public DataTemplate ElementWaitTempalte { get; set; }
        //public DataTemplate ElementIfExistTempalte { get; set; }
        //public DataTemplate ElementShotTempalte { get; set; }
        public DataTemplate DefaultTempalte { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement containerElement = container as FrameworkElement;
            if (containerElement != null && item != null && item is ActionModel actionModel)
            {
                return containerElement.FindResource(actionModel.DataTemplateKey) as DataTemplate;
            }
            return DefaultTempalte;
        }
    }
}
