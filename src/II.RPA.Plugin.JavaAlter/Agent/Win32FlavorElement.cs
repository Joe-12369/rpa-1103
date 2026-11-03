using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    internal class Win32FlavorElement : FlavorElement
    {
        public IntPtr Handle { get; private set; }

        public override bool Valid => throw new NotImplementedException();

        public override Rectangle ElementRectangle => throw new NotImplementedException();

        public override bool IsClientRectangle => throw new NotImplementedException();

        public override bool Visible => throw new NotImplementedException();

        public override bool HasFocus => throw new NotImplementedException();

        public override bool Enabled => throw new NotImplementedException();

        public override string FlavorName => "Windows";

        public Win32FlavorElement(IntPtr intPtr)
        {
            this.Handle = intPtr;
        }

        public override void Focus()
        {
            throw new NotImplementedException();
        }

        public override object GetAttributeValue(Element element, string name)
        {
            throw new NotImplementedException();
        }

        public override object InvokeAction(Element element, string name, object[] args)
        {
            throw new NotImplementedException();
        }

        public override void SetAttributeValue(Element element, string name, object value)
        {
            throw new NotImplementedException();
        }

        public override bool EnsureVisible()
        {
            throw new NotImplementedException();
        }

        protected override object[] GetRuntimeTokenValues()
        {
            throw new NotImplementedException();
        }
    }
}
