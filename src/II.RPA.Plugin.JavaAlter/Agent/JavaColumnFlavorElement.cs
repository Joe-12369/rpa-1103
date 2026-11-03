using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    class JavaColumnFlavorElement : FlavorElement
    {
        internal JavaColumnFlavorElement(AwtFlavorElement container, int colIndex)
        {
            this.container = container;
            this.colIndex = colIndex;
        }

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
                return this.GetColRect();
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
                return this.GetColRect().IntersectsWith(clippedRectangle);
            }
        }

        internal int ColumnIndex
        {
            get
            {
                return this.colIndex;
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
                    0,
                    this.colIndex
                });
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public override string FlavorName => "Column";
        public override object GetAttributeValue(Element element, string name)
        {
            if (name == "text")
            {
                return this.container.JavaObject.InvokeMethod("getColumnName", new object[]
                {
                    this.colIndex
                })[0];
            }
            if (name == "index")
            {
                return this.colIndex;
            }
            if (name == "selected")
            {
                return (bool)this.container.JavaObject.InvokeMethod("isColumnSelected", new object[]
                {
                    this.colIndex
                })[0];
            }
            return null;
        }
        public override object InvokeAction(Element element, string name, object[] args)
        {
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
                "javacol",
                this.colIndex
            };
        }
        private Rectangle GetColRect()
        {
            object[] array = this.container.JavaObject.InvokeGetItemProperty("colrect", new object[]
            {
                this.colIndex
            });
            Rectangle result = new Rectangle((int)array[0], (int)array[1], (int)array[2], (int)array[3]);
            result.Offset(this.container.ElementRectangle.Location);
            return result;
        }

        private readonly AwtFlavorElement container;

        private readonly int colIndex;
    }
}
