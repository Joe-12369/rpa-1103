using System;
using System.Linq;
using System.Text.RegularExpressions;
namespace II.RPA.Plugin.JavaAlter.Agent
{
    abstract class JavaBaseFlavorElement : FlavorElement
    {
        internal JavaBaseFlavorElement(JavaObjectWrapper jow, FlavorElement containerFlavorElement, FlavorElement rootContainerFlavorElement)
        {
            this.jp = jow.Proxy;
            this.jow = jow;
            this.containerFlavorElement = containerFlavorElement;
            this.rootContainerFlavorElement = rootContainerFlavorElement;
            this.clazzes = jow.Lineage;
        }
        public override string FlavorName => "Java";

        public JavaObjectWrapper JavaObject
        {
            get
            {
                return this.jow;
            }
        }

        public override bool IsClientRectangle
        {
            get
            {
                return false;
            }
        }

        internal string MappedClazz
        {
            get
            {
                return this.mappedClazz;
            }
        }

        internal string ObjectClazz
        {
            get
            {
                return this.clazzes[0];
            }
        }

        internal JavaProxy JP
        {
            get
            {
                return this.jp;
            }
        }

        internal FlavorElement ContainerFlavorElement
        {
            get
            {
                return this.containerFlavorElement;
            }
            set
            {
                this.containerFlavorElement = value;
            }
        }

        internal FlavorElement RootContainerFlavorElement
        {
            get
            {
                return this.rootContainerFlavorElement;
            }
        }

        protected internal override bool DontCache
        {
            get
            {
                return true;
            }
        }

        public bool HasClazz(string name)
        {
            return this.clazzes.Contains(name, StringComparer.OrdinalIgnoreCase);
        }


        public override void SetAttributeValue(Element element, string name, object value)
        {
            this.jow.SetPropertyCascaded(name, value);
        }

        protected string GetShortTypeName()
        {
            if (this.shortTypeName == null)
            {
                string[] array = this.clazzes[0].Split(new char[]
                {
                    '.'
                });
                if (array.Length != 0)
                {
                    this.shortTypeName = array[array.Length - 1];
                }
            }
            return this.shortTypeName;
        }

        protected override object[] GetRuntimeTokenValues()
        {
            return new object[]
            {
                "java",
                this.jow.ObjectId
            };
        }

        internal static readonly Regex allDigits = new Regex("^[0-9]+$", RegexOptions.Compiled);

        internal readonly JavaProxy jp;

        internal readonly JavaObjectWrapper jow;

        internal readonly string[] clazzes;

        internal string mappedClazz;

        protected readonly FlavorElement rootContainerFlavorElement;

        protected FlavorElement containerFlavorElement;

        private string shortTypeName;
    }
}
