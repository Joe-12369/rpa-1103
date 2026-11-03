using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    class JavaRowFlavorElement : FlavorElement
    {
        internal JavaRowFlavorElement(AwtFlavorElement container, int rowIndex)
        {
            this.container = container;
            this.rowIndex = rowIndex;

        }

        public override string FlavorName => "Row";

        public override bool Valid
        {
            get
            {
                return this.container.Valid;
            }
        }

        public override Rectangle ElementRectangle
        {
            get
            {
                return this.GetRowRect();
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
                Rectangle clippedRectangle = this.Container.ClippedRectangle;
                return this.GetRowRect().IntersectsWith(clippedRectangle);
            }
        }

        internal int RowIndex
        {
            get
            {
                return this.rowIndex;
            }
        }

        internal AwtFlavorElement Container
        {
            get
            {
                return this.container;
            }
        }

        public override bool EnsureVisible()
        {
            try
            {
                this.container.JavaObject.InvokeSpecial("ensurevisible", new object[]
                {
                    this.rowIndex,
                    0
                });
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public override object GetAttributeValue(Element element, string name)
        {
            if (name == "index")
            {
                return this.rowIndex;
            }
            if (name == "selected")
            {
                return (bool)this.container.JavaObject.InvokeMethod("isRowSelected", new object[]
                {
                    this.rowIndex
                })[0];
            }
            return null;
        }

        public override object InvokeAction(Element element, string name, object[] args)
        {
            if (name == "select")
            {
                //FlavorElement.AssertArgumentTypes(new Type[0], args);
                this.container.JavaObject.InvokeMethod("addRowSelectionInterval", new object[]
                {
                    this.rowIndex,
                    this.rowIndex
                });
                return null;
            }
            throw new NotSupportedException("The operation is not supported.");
        }

        public override void SetAttributeValue(Element element, string name, object value)
        {
            if (!(name == "selected"))
            {
                throw new NotSupportedException("The operation is not supported.");
            }
            FlavorElement.AssertArgumentType(typeof(bool), value);
            if ((bool)value)
            {
                this.container.JavaObject.InvokeMethod("addRowSelectionInterval", new object[]
                {
                    this.rowIndex,
                    this.rowIndex
                });
                return;
            }
            this.container.JavaObject.InvokeMethod("removeRowSelectionInterval", new object[]
            {
                this.rowIndex,
                this.rowIndex
            });
        }

        public override void Focus()
        {
        }

        protected override object[] GetRuntimeTokenValues()
        {
            return new object[]
            {
                "javarow",
                this.rowIndex
            };
        }

        private Rectangle GetRowRect()
        {
            object[] array = this.container.JavaObject.InvokeGetItemProperty("rowrect", new object[]
            {
                this.rowIndex
            });
            Rectangle result = new Rectangle((int)array[0], (int)array[1], (int)array[2], (int)array[3]);
            result.Offset(this.container.ElementRectangle.Location);
            return result;
        }

        private readonly AwtFlavorElement container;

        private readonly int rowIndex;
    }
}
