using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Models
{
    class AndroidSelectorItem : MobileSelectorItem
    {
        public const string NAME = "name";
        public const string RESOURCE_ID = "resource-id";           //resource id
        public const string ACCESSIBILITY_ID = "accessibility-id";
        public const string INDEX = "index";
        public const string CLASS = "class";
        public const string DESC = "content-desc";
        public const string TEXT = "text";
        //public const string DESCRIPTIONDescription = "text";


        public string Name
        {
            get
            {
                var e = Properties.Where(x => x.Name == "name").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }

        public string ID
        {
            get
            {
                var e = Properties.Where(x => x.Name == "resource-id").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }

        public string Accessibility_ID
        {
            get
            {
                var e = Properties.Where(x => x.Name == "accessibility-id").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }

        public string ClassName
        {
            get
            {
                var e = Properties.Where(x => x.Name == "class").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }

        public string Description
        {
            get
            {
                var e = Properties.Where(x => x.Name == "content-desc").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }

        public string Index
        {
            get
            {
                var e = Properties.Where(x => x.Name == "index").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string Text
        {
            get
            {
                var e = Properties.Where(x => x.Name == "text").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }


        public AndroidSelectorItem() { }
        public AndroidSelectorItem(MobileSelectorItem item)
        {
            //SetBackingFieldValues(item._backingFieldValues);
            Properties = item.Properties;
        }
        public AndroidSelectorItem(MobileElement element)
        {
            Properties = new List<MobileSelectorItemProperty>();
            if (!string.IsNullOrEmpty(element.ClassName))
            {
                Properties.Add(new MobileSelectorItemProperty(CLASS, element.ClassName));
            }
            if (!string.IsNullOrEmpty(element.Id))
            {
                Properties.Add(new MobileSelectorItemProperty(RESOURCE_ID, element.Id));
            }
            if (!string.IsNullOrEmpty(element.ControlType))
            {
                Properties.Add(new MobileSelectorItemProperty(NAME, element.ControlType));
            }
            //if (!string.IsNullOrEmpty(element.IndexInParent))
            //{
            //    Properties.Add(new MobileSelectorItemProperty(INDEX, element.IndexInParent));
            //}
            //此处新的编辑器使用nth-child index替代原先的 nth-oftype
            if (!string.IsNullOrEmpty(element.NthChildIndex))
            {
                Properties.Add(new MobileSelectorItemProperty(INDEX, element.NthChildIndex));
            }
            if (!string.IsNullOrEmpty(element.Text))
            {
                Properties.Add(new MobileSelectorItemProperty(TEXT, element.Text));
            }
            if (!string.IsNullOrEmpty(element.Desc))
            {
                Properties.Add(new MobileSelectorItemProperty(DESC, element.Desc));
            }
            foreach (var p in Properties)
            {
                p.Enabled = true;
                p.CanDisable = false;
            }
            Enabled = true;
            CanDisable = true;
        }
    }
}
