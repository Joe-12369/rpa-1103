using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    public class JavaListItemFlavorElement : FlavorElement
    {
        internal JavaListItemFlavorElement(AwtFlavorElement container, int itemIndex)
        {
            this.container = container;
            this.itemIndex = itemIndex;
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
                if (this.container.MappedClazz == "java.awt.List")
                {
                    Rectangle rectFromWin = this.GetRectFromWin32();
                    rectFromWin.Offset(this.container.ElementRectangle.Location);
                    return rectFromWin;
                }
                object[] array = this.container.JavaObject.InvokeGetSpecialProperty("listitemrect", new object[]
                {
                    this.itemIndex
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
                if (this.container.MappedClazz == "java.awt.List")
                {
                    Rectangle elementRectangle = this.container.ElementRectangle;
                    return this.GetRectFromWin32().IntersectsWith(new Rectangle(0, 0, elementRectangle.Width, elementRectangle.Height));
                }
                int num = (int)this.container.JavaObject.InvokeMethod("getFirstVisibleIndex", Array.Empty<object>())[0];
                int num2 = (int)this.container.JavaObject.InvokeMethod("getLastVisibleIndex", Array.Empty<object>())[0];
                return num <= this.itemIndex && this.itemIndex <= num2;
            }
        }

        private JavaObjectWrapper Jow
        {
            get
            {
                if (this._jow == null)
                {
                    this._jow = (this.container.JavaObject.InvokeGetSpecialProperty("listitemobject", new object[]
                    {
                        this.itemIndex
                    })[0] as JavaObjectWrapper);
                }
                return this._jow;
            }
        }

        public override string FlavorName => "ListItem";

        public override bool EnsureVisible()
        {
            try
            {
                if (this.container.MappedClazz == "java.awt.List")
                {
                    this.container.JavaObject.InvokeMethod("makeVisible", new object[]
                    {
                        this.itemIndex
                    });
                }
                else
                {
                    this.container.JavaObject.InvokeMethod("ensureIndexIsVisible", new object[]
                    {
                        this.itemIndex
                    });
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public override object GetAttributeValue(Element element, string name)
        {
            if (name == "text")
            {
                if (this.container.MappedClazz == "java.awt.List")
                {
                    return this.container.JavaObject.InvokeMethod("getItem", new object[]
                    {
                        this.itemIndex
                    })[0];
                }
                return this.container.JavaObject.InvokeGetSpecialProperty("listitemtext", new object[]
                {
                    this.itemIndex
                })[0];
            }
            else
            {
                if (name == "index")
                {
                    return this.itemIndex;
                }
                if (name == "selected")
                {
                    if (this.container.MappedClazz == "java.awt.List")
                    {
                        return this.container.JavaObject.InvokeMethod("isIndexSelected", new object[]
                        {
                            this.itemIndex
                        })[0];
                    }
                    return (bool)this.container.JavaObject.InvokeMethod("isSelectedIndex", new object[]
                    {
                        this.itemIndex
                    })[0];
                }
                else
                {
                    if (name == "javaobject")
                    {
                        return this.Jow;
                    }
                    if (this.Jow != null)
                    {
                        return this.Jow.GetPropertyCascaded(name);
                    }
                    return null;
                }
            }
        }

        public override object InvokeAction(Element element, string name, object[] args)
        {
            if (name == "select")
            {
                //FlavorElement.AssertArgumentTypes(new Type[0], args);
                if (this.container.MappedClazz == "java.awt.List")
                {
                    this.container.JavaObject.InvokeMethod("select", new object[]
                    {
                        this.itemIndex
                    });
                }
                else
                {
                    this.container.JavaObject.InvokeMethod("addSelectionInterval", new object[]
                    {
                        this.itemIndex,
                        this.itemIndex
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
                bool flag = (bool)value;
                if (this.container.MappedClazz == "java.awt.List")
                {
                    if (flag)
                    {
                        this.container.JavaObject.InvokeMethod("select", new object[]
                        {
                            this.itemIndex
                        });
                        return;
                    }
                    this.container.JavaObject.InvokeMethod("deselect", new object[]
                    {
                        this.itemIndex
                    });
                    return;
                }
                else
                {
                    if (flag)
                    {
                        this.container.JavaObject.InvokeMethod("addSelectionInterval", new object[]
                        {
                            this.itemIndex,
                            this.itemIndex
                        });
                        return;
                    }
                    this.container.JavaObject.InvokeMethod("removeSelectionInterval", new object[]
                    {
                        this.itemIndex,
                        this.itemIndex
                    });
                    return;
                }
            }
            else
            {
                if (this.Jow != null)
                {
                    this.Jow.SetPropertyCascaded(name, value);
                    return;
                }
                throw new NotSupportedException();
            }
        }

        public override void Focus()
        {
        }

        protected override object[] GetRuntimeTokenValues()
        {
            return new object[]
            {
                this.itemIndex
            };
        }

        private Rectangle GetRectFromWin32()
        {
            Rectangle result = new Rectangle();

            //CommonControls.GetListBoxItemRect(new IntPtr((int)this.container.JavaObject.InvokeGetSpecialProperty("hwnd", Array.Empty<object>())[0]), this.itemIndex, out result);
            return result;
        }

        private readonly AwtFlavorElement container;

        private readonly int itemIndex;

        private JavaObjectWrapper _jow;
    }
}
