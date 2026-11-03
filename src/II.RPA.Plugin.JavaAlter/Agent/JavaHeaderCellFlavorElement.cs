using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    class JavaHeaderCellFlavorElement : FlavorElement
    {
        internal JavaHeaderCellFlavorElement(AwtFlavorElement container, JavaObjectWrapper jtable, int columnIndex)
        {
            this.container = container;
            this.columnIndex = columnIndex;
            this.jtable = jtable;
        }

        public override bool Valid
        {
            get
            {
                return this.container.Valid;
            }
        }
        public override string FlavorName => "HeaderCell";

        public override Rectangle ElementRectangle
        {
            get
            {
                object[] array = this.container.JavaObject.InvokeGetSpecialProperty("tableheaderrect", new object[]
                {
                    this.columnIndex
                });
                int x = (int)array[0];
                int y = (int)array[1];
                int width = (int)array[2];
                int height = (int)array[3];
                Rectangle result = new Rectangle(x, y, width, height);
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
                try
                {
                    if (this.container == null)
                    {
                        return false;
                    }
                    Rectangle clippedRectangle = this.container.ClippedRectangle;
                    return this.ElementRectangle.IntersectsWith(clippedRectangle);
                }
                catch (Exception)
                {

                    return false;
                }

            }
        }

        public override bool EnsureVisible()
        {
            bool result;
            try
            {
                this.jtable.InvokeSpecial("ensurevisible", new object[]
                {
                    0,
                    this.columnIndex
                });
                result = true;
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public override object GetAttributeValue(Element element, string name)
        {
            if (name == "text")
            {
                return this.jtable.InvokeMethod("getColumnName", new object[]
                {
                    this.columnIndex
                })[0];
            }
            if (name == "columnindex")
            {
                return this.columnIndex;
            }
            if (name == "rowindex")
            {
                return -1;
            }
            if (name == "selected")
            {
                return false;
            }
            if (name == "isheader")
            {
                return true;
            }
            return null;
        }

        public override object InvokeAction(Element element, string name, object[] args)
        {
            if (name == "select")
            {
                //FlavorElement.AssertArgumentTypes(new Type[0], args);
                this.jtable.Invoke("addColumnSelectionInterval", new object[]
                {
                    this.columnIndex,
                    this.columnIndex
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
                this.container.JavaObject.ObjectId,
                this.columnIndex
            };
        }

        protected readonly AwtFlavorElement container;

        private readonly JavaObjectWrapper jtable;

        private readonly int columnIndex;
    }
}
