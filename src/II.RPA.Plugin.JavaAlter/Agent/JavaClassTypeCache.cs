using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    // Token: 0x0200001A RID: 26
    internal class JavaClassTypeCache
    {
        // Token: 0x06000142 RID: 322 RVA: 0x0000B9EF File Offset: 0x00009BEF
        internal JavaClassTypeCache()
        {
        }

        // Token: 0x06000143 RID: 323 RVA: 0x0000BA10 File Offset: 0x00009C10
        internal JavaClassTypeCache.JavaTypeInfo EnsureTypeInfoAvailable(string className, JavaObjectWrapper jow)
        {
            object obj = this.cacheLock;
            JavaClassTypeCache.JavaTypeInfo result;
            lock (obj)
            {
                JavaClassTypeCache.JavaTypeInfo javaTypeInfo;
                if (!this.typeCache.TryGetValue(className, out javaTypeInfo))
                {
                    javaTypeInfo = new JavaClassTypeCache.JavaTypeInfo();
                    string[] array = (jow.InvokeGetPropertiesAndMethods()[0] as string).Split(new char[]
                    {
                        ';'
                    }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < array.Length; i++)
                    {
                        string[] array2 = array[i].Split(new char[]
                        {
                            ':'
                        });
                        string a = array2[0];
                        string text = array2[1];
                        string text2 = text.ToLowerInvariant();
                        if (a == "G")
                        {
                            Type key = JavaClassTypeCache.MapJavaType(array2[2]);
                            if (text2.StartsWith("get") || text2.StartsWith("has"))
                            {
                                javaTypeInfo.Getters[text2.Substring(3)] = new KeyValuePair<Type, string>(key, text);
                            }
                            else if (text2.StartsWith("is"))
                            {
                                javaTypeInfo.Getters[text2.Substring(2)] = new KeyValuePair<Type, string>(key, text);
                            }
                        }
                        else if (a == "S")
                        {
                            Type key2 = JavaClassTypeCache.MapJavaType(array2[2]);
                            if (text2.StartsWith("set"))
                            {
                                javaTypeInfo.Setters[text2.Substring(3)] = new KeyValuePair<Type, string>(key2, text);
                            }
                        }
                        else if (a == "M")
                        {
                            string[] array3 = array2[2].Split(new char[]
                            {
                                ','
                            }, StringSplitOptions.RemoveEmptyEntries);
                            List<Type> list = new List<Type>();
                            foreach (string axorTypeStr in array3)
                            {
                                list.Add(JavaClassTypeCache.MapJavaType(axorTypeStr));
                            }
                            javaTypeInfo.Methods[text2] = new KeyValuePair<List<Type>, string>(list, text);
                        }
                        else if (a == "F")
                        {
                            Type key3 = JavaClassTypeCache.MapJavaType(array2[2]);
                            javaTypeInfo.Fields[text2] = new KeyValuePair<Type, string>(key3, text);
                        }
                    }
                    this.typeCache[className] = javaTypeInfo;
                }
                result = javaTypeInfo;
            }
            return result;
        }

        // Token: 0x06000144 RID: 324 RVA: 0x0000BC54 File Offset: 0x00009E54
        private static Type MapJavaType(string axorTypeStr)
        {
            Type typeFromHandle;
            if (axorTypeStr == "boolean")
            {
                typeFromHandle = typeof(bool);
            }
            else if (axorTypeStr == "int")
            {
                typeFromHandle = typeof(int);
            }
            else if (axorTypeStr == "java.lang.String")
            {
                typeFromHandle = typeof(string);
            }
            else if (axorTypeStr == "long")
            {
                typeFromHandle = typeof(long);
            }
            else if (axorTypeStr == "float")
            {
                typeFromHandle = typeof(float);
            }
            else if (axorTypeStr == "double")
            {
                typeFromHandle = typeof(double);
            }
            else if (axorTypeStr == "void")
            {
                typeFromHandle = typeof(void);
            }
            else if (axorTypeStr == "java.awt.Rectangle")
            {
                typeFromHandle = typeof(Rectangle);
            }
            else if (axorTypeStr == "java.awt.Point")
            {
                typeFromHandle = typeof(Point);
            }
            else if (axorTypeStr == "java.awt.Dimension")
            {
                typeFromHandle = typeof(Size);
            }
            else
            {
                typeFromHandle = typeof(JavaObjectWrapper);
            }
            return typeFromHandle;
        }

        // Token: 0x0400003A RID: 58
        private readonly Dictionary<string, JavaClassTypeCache.JavaTypeInfo> typeCache = new Dictionary<string, JavaClassTypeCache.JavaTypeInfo>();

        // Token: 0x0400003B RID: 59
        private readonly object cacheLock = new object();

        // Token: 0x02000055 RID: 85
        internal class JavaTypeInfo
        {
            // Token: 0x06000404 RID: 1028 RVA: 0x00017832 File Offset: 0x00015A32
            internal JavaTypeInfo()
            {
                this.Getters = new Dictionary<string, KeyValuePair<Type, string>>();
                this.Setters = new Dictionary<string, KeyValuePair<Type, string>>();
                this.Fields = new Dictionary<string, KeyValuePair<Type, string>>();
                this.Methods = new Dictionary<string, KeyValuePair<List<Type>, string>>();
            }

            // Token: 0x17000164 RID: 356
            // (get) Token: 0x06000405 RID: 1029 RVA: 0x00017866 File Offset: 0x00015A66
            // (set) Token: 0x06000406 RID: 1030 RVA: 0x0001786E File Offset: 0x00015A6E
            public Dictionary<string, KeyValuePair<Type, string>> Setters { get; private set; }

            // Token: 0x17000165 RID: 357
            // (get) Token: 0x06000407 RID: 1031 RVA: 0x00017877 File Offset: 0x00015A77
            // (set) Token: 0x06000408 RID: 1032 RVA: 0x0001787F File Offset: 0x00015A7F
            public Dictionary<string, KeyValuePair<Type, string>> Getters { get; private set; }

            // Token: 0x17000166 RID: 358
            // (get) Token: 0x06000409 RID: 1033 RVA: 0x00017888 File Offset: 0x00015A88
            // (set) Token: 0x0600040A RID: 1034 RVA: 0x00017890 File Offset: 0x00015A90
            public Dictionary<string, KeyValuePair<Type, string>> Fields { get; private set; }

            // Token: 0x17000167 RID: 359
            // (get) Token: 0x0600040B RID: 1035 RVA: 0x00017899 File Offset: 0x00015A99
            // (set) Token: 0x0600040C RID: 1036 RVA: 0x000178A1 File Offset: 0x00015AA1
            public Dictionary<string, KeyValuePair<List<Type>, string>> Methods { get; private set; }

            // Token: 0x0600040D RID: 1037 RVA: 0x000178AC File Offset: 0x00015AAC
            //internal void GetDynamicAttributesAndActions(IList<AttributeDescriptor> attributes, IList<ActionDescriptor> actions, bool includeFields)
            //{
            //    if (this.descWithFields == null || this.descWithFields.Value != includeFields)
            //    {
            //        this.attributesToAdd = new List<AttributeDescriptor>(this.Getters.Count + (includeFields ? this.Fields.Count : 0));
            //        this.actionsToAdd = new List<ActionDescriptor>(this.Methods.Count);
            //        foreach (string text in this.Getters.Keys)
            //        {
            //            string text2 = string.Intern(this.Getters[text].Value);
            //            int startIndex = text2.Length - text.Length;
            //            string name = string.Intern(text2.Substring(startIndex));
            //            this.attributesToAdd.Add(new AttributeDescriptor(name, "A Java getter-/setter-style property value.", this.Getters[text].Key, 0, !this.Setters.ContainsKey(text)));
            //        }
            //        if (includeFields)
            //        {
            //            foreach (string key in this.Fields.Keys)
            //            {
            //                string name2 = string.Intern(this.Fields[key].Value);
            //                this.attributesToAdd.Add(new AttributeDescriptor(name2, "A Java object field value.", this.Fields[key].Key, 0, true));
            //            }
            //        }
            //        foreach (string key2 in this.Methods.Keys)
            //        {
            //            KeyValuePair<List<Type>, string> keyValuePair = this.Methods[key2];
            //            string name3 = string.Intern(keyValuePair.Value);
            //            List<ActionDescriptor.Argument> list = new List<ActionDescriptor.Argument>();
            //            for (int i = 1; i < keyValuePair.Key.Count; i++)
            //            {
            //                list.Add(new ActionDescriptor.Argument(string.Intern("arg" + i.ToString()), keyValuePair.Key[i], "Argument value."));
            //            }
            //            ActionDescriptor item = new ActionDescriptor(name3, "A Java object method.", new ActionDescriptor.Argument("returns", keyValuePair.Key[0], "Return value."), list);
            //            this.actionsToAdd.Add(item);
            //        }
            //        this.descWithFields = new bool?(includeFields);
            //    }
            //    foreach (AttributeDescriptor item2 in this.attributesToAdd)
            //    {
            //        attributes.Add(item2);
            //    }
            //    foreach (ActionDescriptor item3 in this.actionsToAdd)
            //    {
            //        actions.Add(item3);
            //    }
            //}

            // Token: 0x040002A8 RID: 680
            private bool? descWithFields;

            // Token: 0x040002A9 RID: 681
            //private List<AttributeDescriptor> attributesToAdd;

            // Token: 0x040002AA RID: 682
            //private List<ActionDescriptor> actionsToAdd;
        }
    }
}
