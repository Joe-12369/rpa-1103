using System;
using System.Drawing;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    class AwtWindowFlavorElement : AwtFlavorElement
    {       
        internal AwtWindowFlavorElement(JavaObjectWrapper jow, string[] clazzes, Win32FlavorElement nativeWindow) : base(jow.WithLineage(clazzes), nativeWindow, nativeWindow, -1)
        {
            this.Win32FlavorElement = nativeWindow;
        }      
        public Win32FlavorElement Win32FlavorElement { get; }

        public override bool Valid
        {
            get
            {
                return this.Win32FlavorElement.Valid;
            }
        }

        public override Rectangle ElementRectangle
        {
            get
            {
                try
                {
                    object[] array = jow.InvokeGetSpecialProperty("rect", Array.Empty<object>());
                    return new Rectangle((int)array[0], (int)array[1], (int)array[2], (int)array[3]);
                }
                catch (Exception)
                {
                    return Rectangle.Empty;
                }
            }
        }

        public override bool EnsureVisible()
        {
            return this.Win32FlavorElement.EnsureVisible();
        }

        public override object GetAttributeValue(Element element, string name)
        {
            if (name == "title")
            {
                return base.JavaObject.InvokeGetStringProperty("title");
            }
            if (name == "state")
            {
                return this.Win32FlavorElement.GetAttributeValue(element, name);
            }
            if (name == "active")
            {
                return base.JavaObject.InvokeGetProperty("isActive");
            }
            if (name == "appicon")
            {
                return this.Win32FlavorElement.GetAttributeValue(element, name);
            }
            if (name == "name")
            {
                return base.GetAttributeValue(element, name);
            }
            object result;
            //if (this.Win32FlavorElement.NativeWindowGetAttributeValue(element, name, out result))
            //{
            //    return result;
            //}
            return base.GetAttributeValue(element, name);
        }


        public override object InvokeAction(Element element, string name, object[] args)
        {
            if (name == "activate" || name == "close" || name == "move" || name == "resize")
            {
                return this.Win32FlavorElement.InvokeAction(element, name, args);
            }
            object result;
            //if (this.Win32FlavorElement.NativeWindowInvokeAction(element, name, args, out result))
            //{
            //    return result;
            //}
            return base.InvokeAction(element, name, args);
        }

        public override void SetAttributeValue(Element element, string name, object value)
        {
            if (name == "state")
            {
                this.Win32FlavorElement.SetAttributeValue(element, name, value);
                return;
            }
            //if (this.Win32FlavorElement.NativeWindowSetAttributeValue(element, name, value))
            //{
            //    return;
            //}
            base.SetAttributeValue(element, name, value);
        }

        protected override object[] GetRuntimeTokenValues()
        {
            return new object[]
            {
                "javawindow",
                this.Win32FlavorElement.Handle
            };
        }
    }
}
