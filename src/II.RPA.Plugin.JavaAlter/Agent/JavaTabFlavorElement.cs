using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    class JavaTabFlavorElement : FlavorElement
    {
        internal JavaTabFlavorElement(AwtFlavorElement container, int tabIndex)
        {
            this.container = container;
            this.tabIndex = tabIndex;
        }

        public override bool Valid
        {
            get
            {
                return this.container.Valid;
            }
        }
        public override string FlavorName => "TabPage";
        public override Rectangle ElementRectangle
        {
            get
            {
                object[] array = this.container.JavaObject.InvokeGetSpecialProperty("tabrect", new object[]
                {
                    this.tabIndex
                });
                Rectangle result = new Rectangle((int)array[0], (int)array[1], (int)array[2], (int)array[3]);
                result.Offset(this.container.ElementRectangle.Location);
                return result;
            }
        }

        public override bool Enabled
        {
            get
            {
                return true;
            }
        }

        public override bool HasFocus
        {
            get
            {
                return false;
            }
        }

        public override bool IsClientRectangle
        {
            get
            {
                return false;
            }
        }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }


        public override bool EnsureVisible()
        {
            return false;
        }

        public override object GetAttributeValue(Element element, string name)
        {
            if (name == "title")
            {
                return this.container.JavaObject.InvokeGetSpecialProperty("tabtitle", new object[]
                {
                    this.tabIndex
                })[0];
            }
            if (name == "index")
            {
                return this.tabIndex;
            }
            if (name == "selected")
            {
                return (int)this.container.JavaObject.InvokeGetProperty("getSelectedIndex") == this.tabIndex;
            }
            return null;
        }

        public override object InvokeAction(Element element, string name, object[] args)
        {
            if (name == "select")
            {
                //FlavorElement.AssertArgumentTypes(new Type[0], args);
                this.container.JavaObject.InvokeMethod("setSelectedIndex", new object[]
                {
                    this.tabIndex
                });
                return null;
            }
            throw new NotSupportedException("The operation is not supported.");
        }

        public override void SetAttributeValue(Element element, string name, object value)
        {
            throw new NotSupportedException("The operation is not supported.");
        }

        public override void Focus()
        {
        }

        protected override object[] GetRuntimeTokenValues()
        {
            return new object[]
            {
                this.tabIndex
            };
        }

        private readonly AwtFlavorElement container;

        private readonly int tabIndex;
    }
}
