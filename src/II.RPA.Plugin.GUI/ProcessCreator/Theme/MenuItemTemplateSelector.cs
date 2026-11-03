using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;

namespace II.RPA.Plugin.GUI.ProcessCreator.Theme
{
    /// <summary>
    /// absolute
    /// </summary>
    public class MenuItemTemplateSelector : DataTemplateSelector
    {
        private static readonly ILogger logger = Logger.OfContext();
        public HierarchicalDataTemplate SubActionDataTemplate
        {
            get;
            set;
        }
        public HierarchicalDataTemplate DefaultActionDataTemplate
        {
            get;
            set;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate dataTemplate = DefaultActionDataTemplate;
            if (item is ActionModel actionItem)
            {
                if (actionItem.DataItemType == DataItemType.SUB_ACTION)
                {
                    dataTemplate = SubActionDataTemplate;
                    logger.LogDebug("Select:SubActionDataTemplate");
                }
                else
                {
                    if (container is FrameworkElement containerElement
                        && actionItem != null
                        && !string.IsNullOrEmpty(actionItem.DataTemplateKey))
                    {
                        logger.LogDebug($"Select:{actionItem.DataTemplateKey}");
                        dataTemplate = containerElement.FindResource(actionItem.DataTemplateKey) as DataTemplate;
                    }
                }
            }
            else if(item is string itemstring)
            {
                if (container is FrameworkElement containerElement)
                {
                    dataTemplate = containerElement.FindResource(itemstring) as DataTemplate;
                }
            }
            return dataTemplate;
        }
    }
    public class MenuItemStyleSelector : StyleSelector
    {
        public Style PreferActionStyle
        {
            get;
            set;
        }

        public Style SeparatorStyle
        {
            get;
            set;
        }
        public Style MoreActionStyle
        {
            get;
            set;
        }
        public Style SubActionStyle
        {
            get;
            set;
        }


        public override Style SelectStyle(object item, DependencyObject container)
        {

            var actionItem = (ActionModel)item;
            Style style;
            switch (actionItem.DataItemType)
            {
                case DataItemType.PREFER_ACTION:
                    style = PreferActionStyle;
                    break;
                case DataItemType.SEPARATOR:
                    style = SeparatorStyle;
                    break;
                case DataItemType.MORE_ACTION:
                    style = MoreActionStyle;
                    break;
                case DataItemType.SUB_ACTION:
                    style = SubActionStyle;
                    break;
                default:
                    style = PreferActionStyle;
                    break;
            }
            NLogger.Default.Debug($"SelectStyle:{actionItem.DataItemType}");
            return style;
        }
    }
}
