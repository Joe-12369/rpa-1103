using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    [TypeConverter(typeof(JavaObjectWrapper.JavaObjectTypeConverter))]
    public class JavaObjectWrapper
    {
        internal JavaObjectWrapper(JavaProxy jp, JavaTech tech, long objectId)
        {
            this.jp = jp;
            this.objectId = objectId;
            this.tech = tech;
            //this.typeCache = jp.TypeCache;
        }

        public IList<string> PropertyNames
        {
            get
            {
                List<string> list = new List<string>();
                this.EnsureTypeInfoAvailable();
                foreach (string text in this.typeInfo.Getters.Keys)
                {
                    string value = this.typeInfo.Getters[text].Value;
                    int startIndex = value.Length - text.Length;
                    string item = value.Substring(startIndex);
                    list.Add(item);
                }
                foreach (string key in this.typeInfo.Fields.Keys)
                {
                    string value2 = this.typeInfo.Fields[key].Value;
                    if (!this.typeInfo.Getters.ContainsKey(value2.ToLowerInvariant()))
                    {
                        list.Add(value2);
                    }
                }
                return list;
            }
        }

        public IList<string> MethodNames
        {
            get
            {
                List<string> list = new List<string>();
                this.EnsureTypeInfoAvailable();
                foreach (string key in this.typeInfo.Methods.Keys)
                {
                    string value = this.typeInfo.Methods[key].Value;
                    list.Add(value);
                }
                return list;
            }
        }

        public string[] Lineage
        {
            get
            {
                this.EnsureTypeLineageAvailable();
                return this.lineage;
            }
            set
            {
                this.lineage = value;
            }
        }

        public bool IsArray
        {
            get
            {
                return this.GetClassName().StartsWith("[");
            }
        }

        internal JavaTech Tech
        {
            get
            {
                return this.tech;
            }
            set
            {
                this.tech = value;
            }
        }

        internal long ObjectId
        {
            get
            {
                return this.objectId;
            }
        }

        internal JavaProxy Proxy
        {
            get
            {
                return this.jp;
            }
        }

        public object this[string name]
        {
            get
            {
                return this.GetProperty(name);
            }
            set
            {
                this.SetProperty(name, value);
            }
        }

        public JavaObjectWrapper GetObjectProperty(string name)
        {
            return (JavaObjectWrapper)this.GetProperty(name);
        }

        public object GetProperty(string name)
        {
            try
            {
                this.EnsureTypeInfoAvailable();
                name = name.ToLowerInvariant();
                KeyValuePair<Type, string> keyValuePair;
                if (typeInfo == null) return null;
                if (typeInfo.Getters.TryGetValue(name, out keyValuePair))
                {
                    object[] array = this.InvokeMethod(keyValuePair.Value, Array.Empty<object>());
                    if (array != null && array.Length != 0)
                    {
                        if (array[0] is JavaObjectWrapper || keyValuePair.Key == typeof(JavaObjectWrapper))
                        {
                            return array[0];
                        }
                        if (array[0] == null)
                        {
                            return null;
                        }
                        //return ValueConverter.FromString(ValueConverter.ToString(array[0]), keyValuePair.Key);
                        return array[0];
                    }
                }
                KeyValuePair<Type, string> keyValuePair2;
                if (this.typeInfo.Fields.TryGetValue(name, out keyValuePair2))
                {
                    object obj = this.InvokeGetField(keyValuePair2.Value);
                    if (obj != null)
                    {
                        if (obj is JavaObjectWrapper || keyValuePair2.Key == typeof(JavaObjectWrapper))
                        {
                            return obj;
                        }
                        if (obj == null)
                        {
                            return null;
                        }
                        //return ValueConverter.FromString(ValueConverter.ToString(obj), keyValuePair2.Key);
                        return obj;
                    }
                }
                object[] array2 = this.InvokeMethod(name, Array.Empty<object>());
                if (array2 != null && array2.Length != 0)
                {
                    return array2[0];
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        public bool HasProperty(string name)
        {
            this.EnsureTypeInfoAvailable();
            name = name.ToLowerInvariant();
            return this.typeInfo.Getters.ContainsKey(name) || this.typeInfo.Fields.ContainsKey(name);
        }

        public Type GetPropertyType(string name)
        {
            this.EnsureTypeInfoAvailable();
            name = name.ToLowerInvariant();
            KeyValuePair<Type, string> keyValuePair;
            if (this.typeInfo.Getters.TryGetValue(name, out keyValuePair))
            {
                return keyValuePair.Key;
            }
            KeyValuePair<Type, string> keyValuePair2;
            if (this.typeInfo.Fields.TryGetValue(name, out keyValuePair2))
            {
                return keyValuePair2.Key;
            }
            return null;
        }

        public void SetProperty(string name, object value)
        {
            this.EnsureTypeInfoAvailable();
            name = name.ToLowerInvariant();
            KeyValuePair<Type, string> keyValuePair;
            if (this.typeInfo.Setters.TryGetValue(name, out keyValuePair))
            {
                if (value is JavaObjectWrapper || keyValuePair.Key == typeof(JavaObjectWrapper))
                {
                    this.InvokeMethod(keyValuePair.Value, new object[]
                    {
                        value
                    });
                    return;
                }
                this.InvokeMethod(keyValuePair.Value, new object[]
                {
                    //ValueConverter.FromString(ValueConverter.ToString(value), keyValuePair.Key)
                    value
                });
            }
        }

        public object Invoke(string methodName, params object[] args)
        {
            this.EnsureTypeInfoAvailable();
            methodName = methodName.ToLowerInvariant();
            KeyValuePair<List<Type>, string> keyValuePair;
            if (this.typeInfo.Methods.TryGetValue(methodName, out keyValuePair))
            {
                return this.InvokeMethod(keyValuePair.Value, args)[0];
            }
            throw new NotSupportedException();
        }

        public string GetClassName()
        {
            this.EnsureTypeLineageAvailable();
            return this.lineage[0];
        }

        public bool IsClass(string name)
        {
            this.EnsureTypeLineageAvailable();
            return ((ICollection<string>)this.lineage).Contains(name);
        }

        public string GetClassMemberName(JavaObjectWrapper container)
        {
            if (this._classMemberName == null)
            {
                this._classMemberName = (this.InvokeMethod("#CLAZZMEMBERNAME", new object[]
                {
                    container
                })[0] as string);
            }
            return this._classMemberName;
        }

        public T[] GetArrayItems<T>()
        {
            object[] array = this.InvokeMethod("#ARRAYITEMS", Array.Empty<object>());
            T[] array2 = new T[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array2[i] = (T)((object)array[i]);
            }
            return array2;
        }

        public T GetArrayItem<T>(int i)
        {
            return (T)((object)this.InvokeMethod("#ARRAYITEM", new object[]
            {
                i
            })[0]);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            JavaObjectWrapper javaObjectWrapper = obj as JavaObjectWrapper;
            if (javaObjectWrapper != null)
            {
                return javaObjectWrapper.objectId == this.objectId;
            }
            FakeJavaObjectWrapper fakeJavaObjectWrapper = obj as FakeJavaObjectWrapper;
            return fakeJavaObjectWrapper != null && fakeJavaObjectWrapper.Equals(this);
        }

        public override int GetHashCode()
        {
            return this.objectId.GetHashCode();
        }

        public List<JavaObjectWrapper> InvokeGetChildren()
        {
            List<JavaObjectWrapper> list = new List<JavaObjectWrapper>();
            CallStatus callStatus;
            object[] array = this.jp.CallFunction(this.tech, FunctionId.FuncGetChildren, new object[]
            {
                this
            }, out callStatus);
            if (array.Length == 0)
            {
                return new List<JavaObjectWrapper>();
            }
            List<string> list2 = new List<string>();
            JavaObjectWrapper javaObjectWrapper = (JavaObjectWrapper)array[0];
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] is string)
                {
                    list2.Add((string)array[i]);
                }
                if (i < array.Length - 1 && array[i + 1] is JavaObjectWrapper)
                {
                    javaObjectWrapper.Lineage = list2.ToArray();
                    list.Add(javaObjectWrapper);
                    javaObjectWrapper = (JavaObjectWrapper)array[i + 1];
                    list2.Clear();
                }
            }
            if (list2.Count > 0)
            {
                javaObjectWrapper.Lineage = list2.ToArray();
                list.Add(javaObjectWrapper);
            }
            return list;
        }

        public override string ToString()
        {
            return (this.InvokeMethod("toString", Array.Empty<object>())[0] ?? string.Empty).ToString();
        }

        internal JavaObjectWrapper WithLineage(string[] clazzes)
        {
            this.lineage = clazzes;
            return this;
        }

        internal object GetPropertyCascaded(string name)
        {
            if (name == null)
            {
                return null;
            }
            string[] array = name.Split(new char[]
            {
                '.'
            }, StringSplitOptions.RemoveEmptyEntries);
            JavaObjectWrapper javaObjectWrapper = this;
            for (int i = 0; i < array.Length - 1; i++)
            {
                javaObjectWrapper = (javaObjectWrapper.GetProperty(array[i]) as JavaObjectWrapper);
                if (javaObjectWrapper == null)
                {
                    throw new ArgumentException("Property '" + array[i] + "' did not return an object.");
                }
            }
            return javaObjectWrapper.GetProperty(array[array.Length - 1]);
        }

        internal void SetPropertyCascaded(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            string[] array = name.Split(new char[]
            {
                '.'
            }, StringSplitOptions.RemoveEmptyEntries);
            JavaObjectWrapper javaObjectWrapper = this;
            for (int i = 0; i < array.Length - 1; i++)
            {
                javaObjectWrapper = (javaObjectWrapper.GetProperty(array[i]) as JavaObjectWrapper);
                if (javaObjectWrapper == null)
                {
                    throw new ArgumentException("Property '" + array[i] + "' did not return an object.");
                }
            }
            javaObjectWrapper.SetProperty(array[array.Length - 1], value);
        }

        internal void EnsureTypeInfoAvailable()
        {
            object obj = this.cacheLock;
            lock (obj)
            {
                if (this.typeInfo == null)
                {
                    string className = this.GetClassName();
                    //this.typeInfo = this.typeCache.EnsureTypeInfoAvailable(className, this);
                }
            }
        }

        //internal void GetDynamicAttributesAndActions(IList<AttributeDescriptor> attributes, IList<ActionDescriptor> actions, bool includeFields)
        //{
        //    this.EnsureTypeInfoAvailable();
        //    this.typeInfo.GetDynamicAttributesAndActions(attributes, actions, includeFields);
        //}

        internal object[] InvokeGetChildItems(IList<int> itemIds)
        {
            List<object> list = new List<object>
            {
                this
            };
            foreach (int num in itemIds)
            {
                list.Add(num);
            }
            CallStatus callStatus;
            return this.jp.CallFunction(this.tech, FunctionId.FuncGetChildItems, list.ToArray(), out callStatus);
        }

        internal object[] InvokeGetItemProperty(string propName, params object[] itemIds)
        {
            List<object> list = new List<object>
            {
                this,
                propName
            };
            foreach (object item in itemIds)
            {
                list.Add(item);
            }
            CallStatus callStatus;
            return this.jp.CallFunction(this.tech, FunctionId.FuncGetItemProperty, list.ToArray(), out callStatus);
        }

        internal object[] InvokeSetItemSpecialProperty(string propName, object propValue, IList<int> itemIds)
        {
            List<object> list = new List<object>
            {
                this,
                propName,
                propValue
            };
            foreach (int num in itemIds)
            {
                long num2 = (long)num;
                list.Add(num2);
            }
            CallStatus callStatus;
            return this.jp.CallFunction(this.tech, FunctionId.FuncSetItemSpecialProperty, list.ToArray(), out callStatus);
        }

        internal string InvokeGetStringProperty(string propName)
        {
            CallStatus callStatus;
            object[] array = this.jp.CallFunction(this.tech, FunctionId.FuncGetProperty, new object[]
            {
                this,
                propName
            }, out callStatus);
            if (callStatus == CallStatus.Ok)
            {
                if (array[0] == null)
                {
                    return null;
                }
                return array[0].ToString();
            }
            else
            {
                if (callStatus == CallStatus.Exception)
                {
                    throw new Exception((string)array[0]);
                }
                return null;
            }
        }

        internal object InvokeGetProperty(string propName)
        {
            CallStatus callStatus;
            object[] array = this.jp.CallFunction(this.tech, FunctionId.FuncGetProperty, new object[]
            {
                this,
                propName
            }, out callStatus);
            if (callStatus == CallStatus.Ok)
            {
                return array[0];
            }
            if (callStatus == CallStatus.Exception)
            {
                throw new Exception((string)array[0]);
            }
            return null;
        }

        internal object InvokeGetField(string fieldName)
        {
            CallStatus callStatus;
            object[] array = this.jp.CallFunction(this.tech, FunctionId.FuncGetField, new object[]
            {
                this,
                fieldName
            }, out callStatus);
            if (callStatus == CallStatus.Ok)
            {
                return array[0];
            }
            if (callStatus == CallStatus.Exception)
            {
                throw new Exception((string)array[0]);
            }
            return null;
        }

        internal void InvokeSetProperty(string propName, object propValue)
        {
            CallStatus callStatus;
            object[] array = this.jp.CallFunction(this.tech, FunctionId.FuncSetProperty, new object[]
            {
                this,
                propName,
                propValue
            }, out callStatus);
            if (callStatus == CallStatus.Ok)
            {
                return;
            }
            if (callStatus == CallStatus.Exception)
            {
                throw new Exception((string)array[0]);
            }
        }

        internal object[] InvokeGetSpecialProperty(string propName, params object[] args)
        {
            List<object> list = new List<object>
            {
                this,
                propName
            };
            list.AddRange(args);
            CallStatus callStatus;
            object[] array = this.jp.CallFunction(this.tech, FunctionId.FuncGetSpecialProperty, list.ToArray(), out callStatus);
            if (callStatus == CallStatus.Ok)
            {
                return array;
            }
            if (callStatus == CallStatus.Exception)
            {
                throw new Exception((string)array[0]);
            }
            return null;
        }

        internal object[] InvokeGetPropertiesAndMethods()
        {
            List<object> list = new List<object>
            {
                this
            };
            CallStatus callStatus;
            object[] array = this.jp.CallFunction(this.tech, FunctionId.FuncGetPropertyAndMethodNames, list.ToArray(), out callStatus);
            if (callStatus == CallStatus.Ok)
            {
                return array;
            }
            if (callStatus == CallStatus.Exception)
            {
                throw new Exception((string)array[0]);
            }
            return null;
        }

        internal object[] InvokeSpecial(string funcName, params object[] args)
        {
            List<object> list = new List<object>
            {
                this,
                funcName
            };
            list.AddRange(args);
            CallStatus callStatus;
            object[] array = this.jp.CallFunction(this.tech, FunctionId.FuncInvokeSpecial, list.ToArray(), out callStatus);
            if (callStatus == CallStatus.Ok)
            {
                return array;
            }
            if (callStatus == CallStatus.Exception)
            {
                throw new Exception((string)array[0]);
            }
            return null;
        }

        internal object[] InvokeMethod(string funcName, params object[] args)
        {
            List<object> list = new List<object>
            {
                this,
                funcName
            };
            list.AddRange(args);
            CallStatus callStatus;
            object[] array = this.jp.CallFunction(this.tech, FunctionId.FuncInvoke, list.ToArray(), out callStatus);
            if (callStatus == CallStatus.Ok)
            {
                return array;
            }
            if (callStatus == CallStatus.Exception)
            {
                throw new Exception((string)array[0]);
            }
            return null;
        }

        internal TResult Invoke<TResult>(string funcName, params object[] args)
        {
            return (TResult)((object)this.InvokeMethod(funcName, args)[0]);
        }

        internal void InvokeVoid(string funcName, params object[] args)
        {
            this.InvokeMethod(funcName, args);
        }

        private void EnsureTypeLineageAvailable()
        {
            if (this.lineage == null)
            {
                object[] array = this.InvokeMethod("#LINEAGE", Array.Empty<object>());
                this.lineage = new string[array.Length - 1];
                for (int i = 1; i < array.Length; i++)
                {
                    this.lineage[i - 1] = (string)array[i];
                }
            }
        }

        private readonly JavaProxy jp;

        private readonly long objectId;

        private readonly JavaClassTypeCache typeCache;

        private readonly object cacheLock = new object();

        private JavaTech tech;

        private JavaClassTypeCache.JavaTypeInfo typeInfo;

        private string[] lineage;

        private string _classMemberName;

        internal class JavaObjectTypeConverter : ExpandableObjectConverter
        {
            public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
            {
                JavaObjectWrapper javaObjectWrapper = value as JavaObjectWrapper;
                if (javaObjectWrapper == null)
                {
                    return base.GetProperties(context, value, attributes);
                }
                PropertyDescriptorCollection propertyDescriptorCollection = new PropertyDescriptorCollection(new PropertyDescriptor[0]);
                foreach (string name in javaObjectWrapper.PropertyNames)
                {
                    propertyDescriptorCollection.Add(new JavaObjectWrapper.JavaObjectPropertyDescriptor(javaObjectWrapper, name));
                }
                return propertyDescriptorCollection;
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return false;
            }
        }

        internal class JavaObjectPropertyDescriptor : PropertyDescriptor
        {
            public JavaObjectPropertyDescriptor(JavaObjectWrapper jow, string name) : base(name, new Attribute[]
{
                new TypeConverterAttribute(typeof(JavaObjectWrapper.JavaObjectTypeConverter))
})
            {
                this.name = name;
                this.jow = jow;
            }

            public override Type ComponentType
            {
                get
                {
                    return typeof(JavaObjectWrapper);
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public override Type PropertyType
            {
                get
                {
                    return this.jow.GetPropertyType(this.name);
                }
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override object GetValue(object component)
            {
                JavaObjectWrapper javaObjectWrapper = component as JavaObjectWrapper;
                if (javaObjectWrapper == null)
                {
                    return null;
                }
                return javaObjectWrapper.GetProperty(this.name);
            }

            public override void ResetValue(object component)
            {
            }

            public override void SetValue(object component, object value)
            {
                JavaObjectWrapper javaObjectWrapper = component as JavaObjectWrapper;
                if (javaObjectWrapper != null)
                {
                    javaObjectWrapper.SetProperty(this.name, value);
                }
            }

            public override bool ShouldSerializeValue(object component)
            {
                return true;
            }

            private readonly JavaObjectWrapper jow;

            private readonly string name;
        }
    }
}
