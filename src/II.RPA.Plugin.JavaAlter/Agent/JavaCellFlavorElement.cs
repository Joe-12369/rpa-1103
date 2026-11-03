using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    class JavaCellFlavorElement : FlavorElement
    {
        internal JavaCellFlavorElement(AwtFlavorElement container, int rowIndex, int colIndex)
        {
            this.container = container;
            this.rowIndex = rowIndex;
            this.colIndex = colIndex;
        }
        public override string FlavorName => "Cell";
        public override bool Valid
        {
            get
            {
                return this.container.Valid;
            }
        }
        public JavaObjectWrapper JavaObject
        {
            get
            {
                return this.Jow;
            }
        }
        public override Rectangle ElementRectangle
        {
            get
            {
                return this.GetCellRect();
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
                return this.GetCellRect().IntersectsWith(clippedRectangle);
            }
        }
        internal AwtFlavorElement Container
        {
            get
            {
                return this.container;
            }
        }
        private JavaObjectWrapper Jow
        {
            get
            {
                if (this._jow == null)
                {
                    this._jow = (this.container.JavaObject.InvokeGetItemProperty("cellobject", new object[]
                    {
                        this.rowIndex,
                        this.colIndex
                    })[0] as JavaObjectWrapper);
                }
                return this._jow;
            }
        }
        public override bool EnsureVisible()
        {
            try
            {
                this.container.JavaObject.InvokeSpecial("ensurevisible", new object[]
                {
                    this.rowIndex,
                    this.colIndex
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
            try
            {
                if (name == "text")
                {
                    return this.container.JavaObject.InvokeGetItemProperty("celltext", new object[]
                    {
                    this.rowIndex,
                    this.colIndex
                    })[0];
                }
                if (name == "rowindex")
                {
                    return this.rowIndex;
                }
                if (name == "columnindex")
                {
                    return this.colIndex;
                }
                if (name == "selected")
                {
                    return this.IsCellSelected();
                }
                if (name == "javaobject")
                {
                    return this.Jow;
                }
                if (name == "cellrenderercomp")
                {
                    return this.container.JavaObject.InvokeGetItemProperty("cellrenderercomp", new object[]
                    {
                    this.rowIndex,
                    this.colIndex
                    })[0];
                }
                if (this.Jow != null)
                {
                    return this.Jow.GetPropertyCascaded(name);
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }

        }
        public override object InvokeAction(Element element, string name, object[] args)
        {
            if (name == "select")
            {
                // FlavorElement.AssertArgumentTypes(new Type[0], args);
                if (!this.IsCellSelected())
                {
                    this.container.JavaObject.InvokeMethod("changeSelection", new object[]
                    {
                        this.rowIndex,
                        this.colIndex,
                        true,
                        false
                    });
                }
                return null;
            }
            if (this.Jow != null)
            {
                return this.Jow.Invoke(name, args);
            }
            throw new NotSupportedException();
        }
        public override void SetAttributeValue(Element element, string name, object value)
        {
            if (name == "selected")
            {
                FlavorElement.AssertArgumentType(typeof(bool), value);
                if ((bool)value != this.IsCellSelected())
                {
                    this.container.JavaObject.InvokeMethod("changeSelection", new object[]
                    {
                        this.rowIndex,
                        this.colIndex,
                        true,
                        false
                    });
                }
                return;
            }
            if (this.Jow != null)
            {
                this.Jow.SetPropertyCascaded(name, value);
                return;
            }
            throw new NotSupportedException();
        }
        public override void Focus()
        {
        }
        protected override object[] GetRuntimeTokenValues()
        {
            return new object[]
            {
                "javacell",
                this.rowIndex,
                this.colIndex
            };
        }
        private Rectangle GetCellRect()
        {
            object[] array = this.container.JavaObject.InvokeGetItemProperty("cellrect", new object[]
            {
                this.rowIndex,
                this.colIndex
            });
            Rectangle result = new Rectangle((int)array[0], (int)array[1], (int)array[2], (int)array[3]);
            result.Offset(this.container.ElementRectangle.Location);
            return result;
        }
        private bool IsCellSelected()
        {
            return (bool)this.container.JavaObject.InvokeMethod("isCellSelected", new object[]
            {
                this.rowIndex,
                this.colIndex
            })[0];
        }
        private readonly AwtFlavorElement container;
        private readonly int rowIndex;
        private readonly int colIndex;
        private JavaObjectWrapper _jow;
    }
}
