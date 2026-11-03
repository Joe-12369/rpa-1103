using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using System.Windows.Interop;
using System.Windows.Media;

namespace II.RPA.Plugin.GUI.ProcessCreator.AttachProperty
{
    public class PopupFocusBehavior : Behavior<Popup>
    {
        [DllImport("User32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);
        private Popup _popup;
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Opened += AssociatedObject_Opened;
            AssociatedObject.Closed += AssociatedObject_Closed;
        }
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Opened -= AssociatedObject_Opened;
            AssociatedObject.Closed -= AssociatedObject_Closed;
        }
        private void AssociatedObject_Closed(object sender, EventArgs e)
        {
        }
        private void AssociatedObject_Opened(object sender, EventArgs e)
        {
            var source = (HwndSource)PresentationSource.FromVisual(AssociatedObject.Child);
            if (source != null)
            {
                SetFocus(source.Handle);
            }
            var txtes = FindVisualChild<TextBox>(AssociatedObject);
            if (txtes != null && txtes.Any())
            {
                txtes.First().Focus();
            }

        }
        private void AssociatedObject_GotFocus(object sender, RoutedEventArgs e)
        {

            if (AssociatedObject is Popup popup)
            {
                _popup = popup;
                if (_popup != null)
                {
                    var source = (HwndSource)PresentationSource.FromVisual(_popup.Child);
                    if (source != null)
                    {
                        SetFocus(source.Handle);
                    }
                    if (!(_popup.Child is TextBox))
                    {
                        var txtes = FindVisualChild<TextBox>(_popup.Child);
                        if (txtes != null && txtes.Any())
                        {
                            var sourceText = (HwndSource)PresentationSource.FromVisual(txtes.First());
                            if (sourceText != null)
                            {
                                SetFocus(sourceText.Handle);
                            }
                        }
                    }
                }
            }
        }

        private void AssociatedObject_LostFocus(object sender, RoutedEventArgs e)
        {
            //
        }

        /// <summary>
        /// FindVisualChild
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static List<T> FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            try
            {
                List<T> TList = new List<T>();
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    if (child != null && child is T t)
                    {
                        TList.Add(t);
                        List<T> childOfChildren = FindVisualChild<T>(child);
                        if (childOfChildren != null)
                        {
                            TList.AddRange(childOfChildren);
                        }
                    }
                    else
                    {
                        List<T> childOfChildren = FindVisualChild<T>(child);
                        if (childOfChildren != null)
                        {
                            TList.AddRange(childOfChildren);
                        }
                    }
                }
                return TList;
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
                return null;
            }
        }
    }
}
