using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace II.RPA.Plugin.Dialog.TemplateSelector
{
    public class InfoDialogTemplateSelector : DataTemplateSelector
    {
        public DataTemplate InfoTemplate { get; set; }
        public DataTemplate QuestionTemplate { get; set; }
        public DataTemplate WarnTemplate { get; set; }
        public DataTemplate ErrorTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return null;
            var value = (int)item;
            switch (value)
            {
                case 1:
                    return InfoTemplate;
                case 2:
                    return QuestionTemplate;
                case 3:
                    return WarnTemplate;
                case 4:
                    return ErrorTemplate;
                default:
                    return null;
            }
        }
    }
}
