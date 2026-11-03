using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.Plugin.GUI.ProcessCreator.AttachProperty
{
    public class BindingProxy : Freezable
    {
        /// <summary>
        /// Backing storage of the Data dependency property.
        ///
        /// Gets/sets the data object this class is forwarding to everyone
        /// who has a reference to this object.
        /// </summary>
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

        /// <summary>
        /// Gets/sets the data object this class is forwarding to everyone
        /// who has a reference to this object.
        /// </summary>
        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        /// <summary>
        /// Overrides of Freezable
        /// </summary>
        /// <returns></returns>
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }
    }
}
