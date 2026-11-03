using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;

namespace II.RPA.Plugin.GUI.ProcessCreator.AttachProperty
{
    public static class ListBoxItemCommand
    {
        private static readonly ILogger logger = Logger.OfContext();
        #region ListBoxItemMouseDownCommand
        private static readonly DependencyProperty ListBoxItemMouseDownCommandProperty = DependencyProperty.RegisterAttached(
            "ListBoxItemMouseDownCommand",
            typeof(ICommand),
            typeof(ListBoxItemCommand),
            new PropertyMetadata(null, OnListBoxItemMouseDownCommandChange));

        public static void SetListBoxItemMouseDownCommand(DependencyObject source, ICommand value)
        {
            source.SetValue(ListBoxItemMouseDownCommandProperty, value);
        }

        public static ICommand GetListBoxItemMouseDownCommand(DependencyObject source)
        {
            return (ICommand)source.GetValue(ListBoxItemMouseDownCommandProperty);
        }

        private static void OnListBoxItemMouseDownCommandChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ListBoxItem uiElement = d as ListBoxItem;
            if (uiElement != null)
            {
                uiElement.RemoveHandler(UIElement.MouseDownEvent, new RoutedEventHandler(OnMouseDownEvent));
                if (e.NewValue is ICommand command)
                {
                    uiElement.AddHandler(UIElement.MouseDownEvent, new RoutedEventHandler(OnMouseDownEvent), true);
                }
            }
        }

        private static void OnMouseDownEvent(object sender, RoutedEventArgs e)
        {
            ListBoxItem uiElement = sender as ListBoxItem;
            if (uiElement == null)
                return;

            if (uiElement.IsEnabled == false)
                return;

            ICommand listBoxItemMouseDownCommand = GetListBoxItemMouseDownCommand(uiElement);

            if (listBoxItemMouseDownCommand == null)
                return;

            var item = uiElement.DataContext;

            if (listBoxItemMouseDownCommand is RoutedCommand)
            {
                (listBoxItemMouseDownCommand as RoutedCommand).Execute(item, uiElement);
            }
            else
            {
                listBoxItemMouseDownCommand.Execute(item);
            }
        }
        #endregion
    }
}
