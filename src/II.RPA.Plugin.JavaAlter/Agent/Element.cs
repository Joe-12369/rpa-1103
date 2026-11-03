using II.RPA.Plugin.Core.Commons;
using System.Collections.Generic;
using System.Windows.Forms.VisualStyles;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    public class Element
    {
        public Role Role { get; private set; }

        public SelectorProps Selector
        {
            get
            {
                var node = new SelectorProps();
                if (Parent == null)
                {
                    node.Name = MappedClazz.Substring(MappedClazz.LastIndexOf(".") + 1);
                    node.Type = "java";
                    node.Props = new List<Props> {
                        new Props{
                            Name="title",
                            Value=(FlavorElement.GetAttributeValue(null, "title")?.ToString())??"",
                            Accurate="true",
                            Pattern="equal"
                        }

                    };
                }
                else
                {

                    node.Name = ControlType;
                    node.Type = "java";
                    node.Props = new List<Props> {
                        new Props{
                            Name="@index",
                            Value=ElementIndex.ToString(),
                            Accurate="true",
                            Pattern="equal"
                        }

                    };
                    if (FlavorElement is JavaTreeItemFlavorElement || FlavorElement is JavaTabFlavorElement || FlavorElement is JavaListItemFlavorElement)
                    {
                        var textValue = FlavorElement.GetAttributeValue(this, "text");
                        if (textValue != null)
                        {
                            node.Props.Add(new Props
                            {
                                Name = "text",
                                Value = textValue.ToString(),
                                Accurate = "true",
                                Pattern = "equal"
                            });
                        }
                        var titleValue = FlavorElement.GetAttributeValue(this, "title");
                        if (titleValue != null)
                        {
                            node.Props.Add(new Props
                            {
                                Name = "title",
                                Value = titleValue.ToString(),
                                Accurate = "true",
                                Pattern = "equal"
                            });
                        }
                    }

                }
                return node;
            }
        }

        public string MappedClazz { get; private set; }
        public string FlavorName { get { return FlavorElement.FlavorName; } private set { } }
        public FlavorElement FlavorElement { get; private set; }
        public Element(FlavorElement flavorElement, Role role, string Clazz, Element parent, JavaObjectWrapper jow, int index, string controlType)
        {
            Role = role;
            FlavorElement = flavorElement;
            Parent = parent;
            Jow = jow;
            MappedClazz = Clazz;
            ElementIndex = index;
            ControlType = controlType;
        }
        public Element(FlavorElement flavorElement, Element parent, int index, string controlType)
        {
            Role = Role.Unknown;
            FlavorElement = flavorElement;
            Parent = parent;
            Jow = parent.Jow;
            MappedClazz = parent.Jow.Lineage[0];
            ElementIndex = index;
            ControlType = controlType;
        }
        public int ElementIndex { get; set; }
        public string ControlType { get; set; }
        public JavaObjectWrapper Jow { get; private set; }
        public Element Parent
        {
            get;
            private set;
        }     

    }
}
