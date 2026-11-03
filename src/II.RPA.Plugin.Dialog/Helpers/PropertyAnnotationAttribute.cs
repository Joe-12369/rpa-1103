using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Resource;

namespace II.RPA.Plugin.Dialog.Helpers
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PropertyAnnotationAttribute : Attribute
    {
        private string _annotation;
        public string Annotation
        {
            get
            {
                if (string.IsNullOrEmpty(ResourceKey))
                {
                    return _annotation;
                }
                var localAnnotation = II_RPA_Resource.ResourceManager.GetString(ResourceKey);
                if (!string.IsNullOrEmpty(localAnnotation))
                {
                    return localAnnotation;
                }
                return _annotation;
            }
            private set { }
        }
        public string ResourceKey { get; set; }
        public PropertyAnnotationAttribute(string annotation, string resourceKey = "")
        {
            _annotation = annotation;
            ResourceKey = resourceKey;
        }
    }
}
