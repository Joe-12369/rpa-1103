using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    internal class JavaTreeItemFlavorElement : FlavorElement
    {
        internal JavaTreeItemFlavorElement(AwtFlavorElement container, List<int> treePath, int index)
        {
            this.container = container;
            this.treePath = treePath;
            this.index = index;
        }
        public override string FlavorName => "TreeItem";
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
                try
                {
                    object[] array = this.container.JavaObject.InvokeGetItemProperty("rect", this.getTreePathObjs());
                    Rectangle result = new Rectangle((int)array[0], (int)array[1], (int)array[2], (int)array[3]);
                    result.Offset(this.container.ElementRectangle.Location);
                    return result;
                }
                catch (Exception)
                {

                    return Rectangle.Empty;
                }

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
                return (bool)this.container.JavaObject.InvokeGetItemProperty("visible", this.getTreePathObjs())[0];
            }
        }

        internal AwtFlavorElement Container
        {
            get
            {
                return this.container;
            }
        }
        internal List<int> ItemPath
        {
            get
            {
                return this.treePath;
            }
        }

        private JavaObjectWrapper Jow
        {
            get
            {
                if (this._jow == null)
                {
                    this._jow = (this.container.JavaObject.InvokeGetItemProperty("object", this.getTreePathObjs())[0] as JavaObjectWrapper);
                }
                return this._jow;
            }
        }

        public override bool EnsureVisible()
        {
            try
            {
                this.container.JavaObject.InvokeSetItemSpecialProperty("makevisible", true, this.treePath);
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
                    return this.container.JavaObject.InvokeGetItemProperty("text", this.getTreePathObjs())[0];
                }
                if (name == "index")
                {
                    return this.index;
                }
                if (name == "selected")
                {
                    return this.container.JavaObject.InvokeGetItemProperty("selected", this.getTreePathObjs())[0];
                }
                if (name == "expanded")
                {
                    return this.container.JavaObject.InvokeGetItemProperty("expanded", this.getTreePathObjs())[0];
                }
                if (name == "checked")
                {
                    return false;
                }
                if (name == "image")
                {
                    return null;
                }
                if (name == "javaobject")
                {
                    return this.Jow;
                }
                if (this.Jow != null)
                {
                    return this.Jow.GetPropertyCascaded(name);
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;

        }

        public override object InvokeAction(Element element, string name, object[] args)
        {
            if (name == "select")
            {
                this.container.JavaObject.InvokeSetItemSpecialProperty("selected", true, this.treePath);
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
                this.container.JavaObject.InvokeSetItemSpecialProperty("selected", flag, this.treePath);
                return;
            }
            if (name == "expanded")
            {
                FlavorElement.AssertArgumentType(typeof(bool), value);
                bool flag2 = (bool)value;
                this.container.JavaObject.InvokeSetItemSpecialProperty("expanded", flag2, this.treePath);
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
            List<object> list = new List<object>();
            list.Add("treeitem");
            list.AddRange(this.getTreePathObjs());
            return list.ToArray();
        }

        private object[] getTreePathObjs()
        {
            object[] array = new object[this.treePath.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = this.treePath[i];
            }
            return array;
        }

        private readonly AwtFlavorElement container;

        private readonly List<int> treePath;

        private readonly int index;

        private JavaObjectWrapper _jow;
    }
}
