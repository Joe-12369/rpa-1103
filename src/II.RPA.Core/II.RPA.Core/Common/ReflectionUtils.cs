using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Common
{
    public static class ReflectionUtils
    {
        public static TAttrib GetEnumAttribute<TAttrib>(Enum anEnum) where TAttrib : Attribute
        {
            string enumName = Enum.GetName(anEnum.GetType(), anEnum);
            FieldInfo field = anEnum.GetType().GetField(enumName);
            return GetAttribute<TAttrib>(field, false);
        }
        public static TChild AutoCopy<TParent, TChild>(TParent parent) where TChild : TParent, new()
        {
            TChild child = new TChild();
            var ParentType = typeof(TParent);
            var Properties = ParentType.GetProperties();
            foreach (var Propertie in Properties)
            {
                if (Propertie.CanRead && Propertie.CanWrite)
                {
                    Propertie.SetValue(child, Propertie.GetValue(parent, null), null);
                }
            }
            return child;
        }
        public static Tattrib GetAttribute<Tattrib>(FieldInfo aFieldInfo,bool anInheritFlag) where Tattrib : Attribute
        {
            Tattrib attr = default;
            if (aFieldInfo != null)
            {
                object[] obj = aFieldInfo.GetCustomAttributes(typeof(Tattrib),anInheritFlag);
                if (obj.Length > 0)
                    attr = (Tattrib)obj[0];
            }
            return attr;
        }
    }
}
