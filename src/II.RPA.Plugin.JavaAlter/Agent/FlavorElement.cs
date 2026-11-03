using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Xml.Schema;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    [Serializable]
    public abstract class FlavorElement
    {

        public abstract bool Valid { get; }

        public abstract Rectangle ElementRectangle { get; }

        public abstract bool IsClientRectangle { get; }

        public virtual bool ForceGeometricSearchAll
        {
            get
            {
                return false;
            }
        }

        public abstract bool Visible { get; }

        public abstract bool HasFocus { get; }

        public abstract bool Enabled { get; }

        public virtual bool IsUidDomainRoot
        {
            get
            {
                return false;
            }
        }

        public virtual bool SupportsNativePathSearch
        {
            get
            {
                return false;
            }
        }

        protected internal virtual bool DontCache
        {
            get
            {
                return false;
            }
        }

        protected internal virtual int MinPathAttributes
        {
            get
            {
                return 0;
            }
        }

        public virtual IDictionary<string, object> GetInitialValues()
        {
            return null;
        }

        public abstract void Focus();

        public abstract object GetAttributeValue(Element element, string name);

        public abstract object InvokeAction(Element element, string name, object[] args);

        public abstract void SetAttributeValue(Element element, string name, object value);

        public abstract bool EnsureVisible();
        protected internal virtual bool HitTest(Point localPoint)
        {
            return true;
        }
        protected static void AssertArgumentType(Type type, object arg)
        {
            AssertArgumentType(type, arg, false);
        }
        protected static void AssertArgumentType(Type type, object arg, bool allowNull)
        {
            if (arg == null)
            {
                if (!allowNull)
                {
                    throw new ArgumentException("Argument must not be null.");
                }
            }
            else if (!type.IsInstanceOfType(arg))
            {
                throw new ArgumentException("Argument has invalid type, required type is '" + type.Name + "'.");
            }
        }

        protected abstract object[] GetRuntimeTokenValues();
        public abstract string FlavorName { get; }
    }
}
