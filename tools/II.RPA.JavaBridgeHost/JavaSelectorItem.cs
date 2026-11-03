using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using WindowsAccessBridgeInterop;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.JavaBridgeHost
{
    internal class JavaSelectorItem : SelectorItem
    {
        private static ILogger logger = Logger.OfContext();

        public JavaSelectorItem(JavaSelectorItem item)
        {
            this.CanDisable = item.CanDisable;
            this.Enabled = item.Enabled;
            this.ElementName = item.ElementName;
            this.Properties = item.Properties.ToList();
        }

        public JavaSelectorItem(JavaNode element, bool isRoot)
        {
            this.ElementName = element.Role;
            this.Properties = new List<JavaSelectorItemProperty>();

            if (!string.IsNullOrEmpty(element.Name))
            {
                this.Properties.Add(new JavaSelectorItemProperty(ConstFields.SELECTOR_NAME, element.Name));
            }
            if (!string.IsNullOrEmpty(element.Role))
            {
                this.Properties.Add(new JavaSelectorItemProperty(ConstFields.SELECTOR_ROLE, element.Role));
            }

            if (int.Parse(element.Idx) > -1)
            {
                this.Properties.Add(new JavaSelectorItemProperty(ConstFields.SELECTOR_INDEX_IN_PARENT, element.Idx.ToString()));
            }

            this.Enabled = (this.Properties.Count > 1);

            foreach (var p in this.Properties)
            {
                p.Enabled = true;
                p.CanDisable = true;
            }
            this.CanDisable = true;
        }

        protected JavaSelectorItem()
        { }

        public string ElementName { get; set; }

        protected string Id
        {
            get
            {
                var foundItem = this.Properties.FirstOrDefault(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_ID));
                return foundItem?.Value;
            }
        }

        protected string IndexInParent
        {
            get
            {
                var foundItem = this.Properties.FirstOrDefault(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_INDEX_IN_PARENT));
                return foundItem?.Value;
            }
        }

        protected string Name
        {
            get
            {
                var foundItem = this.Properties.FirstOrDefault(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_NAME));
                return foundItem?.Value;
            }
        }

        protected string Role
        {
            get
            {
                var foundItem = this.Properties.FirstOrDefault(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_ROLE));
                return foundItem?.Value;
            }
        }

        protected string Title
        {
            get
            {
                var foundItem = this.Properties.FirstOrDefault(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_TITLE));
                return foundItem?.Value;
            }
        }

        public string SelectorVersion { get; private set; }

        public static JavaSelectorItem FromJObject(JObject o, string version)
        {
            JavaSelectorItem selectorItem = new JavaSelectorItem()
            {
                SelectorVersion = version
            };
            selectorItem.ElementName = o[ConstFields.SELECTOR_ELEMENT_NAME_TAG].ToString();
            var props = o[ConstFields.SELECTOR_ELEMENT_PROPS_TAG];
            try
            {
                var accurate = o["accurate"].ToString();
                if (!string.IsNullOrEmpty(accurate) && accurate == "false")
                    return null;
            }
            catch
            {

            }
            var selectItemProperties = props.ToObject<List<JavaSelectorItemProperty>>();
            selectItemProperties.ForEach(x => x.SelectorVersion = version);
            selectorItem.Properties = new List<JavaSelectorItemProperty>(selectItemProperties);
            selectorItem.Enabled = true;
            return selectorItem;
        }

        public void EnumNeededProperties(JavaNode element, JavaNode parent)
        {
            var props = GetPropertyNames().ToArray();
            int i = 1;
            int matchCounter = 0;
            this.Properties.ForEach((p) => p.Enabled = false);

            do
            {
                ///根据条件属性名称启用条件
                var selectedProps = props.Take(i).ToArray();
                foreach (var p in this.Properties)
                {
                    p.Enabled = selectedProps.Contains(p.Name);
                }

                IEnumerable<JavaNode> children = parent != null ? parent.Children : new List<JavaNode>();

                matchCounter = 0;
                ///重新进行匹配，只要匹配个数不是一个就持续循环，直到所有条件都循环到
                foreach (JavaNode childNode in children)
                {
                    if (this.Match(childNode))
                    {
                        matchCounter++;
                    }

                    if (matchCounter > 1)
                    {
                        break;
                    }
                }

                if (matchCounter != 1)
                {
                    ++i;
                }
            }
            while (matchCounter != 1 && i < props.Count());

            this.Properties.ForEach((p) => p.Enabled = false);

            foreach (var p in props.Take(i).ToArray())
            {
                this.Properties.Where(x => x.Name == p).First().Enabled = true;
            }

            var rowProperty = this.Properties.FirstOrDefault(x => x.Name.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE_ROW));
            if (rowProperty != null)
            {
                rowProperty.Enabled = true;
            }

            var columnProperty = this.Properties.FirstOrDefault(x => x.Name.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE_COLUMN));
            if (columnProperty != null)
            {
                columnProperty.Enabled = true;
            }
        }

        public bool Match(JavaNode m)
        {
            return Match(this, m);
        }

        public JavaNode[] Matches(JavaNode parentElement, int windowX = 0, int windowY = 0)
        {
            try
            {
                var matchs = new List<JavaNode>();
                var elements = parentElement.Children.ToArray();
                int index = 0;
                //如果使用坐标进行查找，直接使用parentElement查找下级孩子节点坐标
                if (this.Properties.Exists(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_POSITION_X)))
                {
                    int x = int.Parse(this.Properties.FirstOrDefault(m => m.Name.EqualIgnoreCase(ConstFields.SELECTOR_POSITION_X)).Value) + windowX;
                    int y = int.Parse(this.Properties.FirstOrDefault(n => n.Name.EqualIgnoreCase(ConstFields.SELECTOR_POSITION_Y)).Value) + windowY;
                    JavaNode jd = parentElement.GetNodeByPoint(x, y);
                    if (jd != null)
                    {
                        matchs.Add(jd);
                    }
                }
                else//循环匹配孩子节点
                {
                    foreach (var node in elements)
                    {
                        //解决Idx=-1情况
                        if (parentElement.Role == "table")
                        {
                            node.Idx = index.ToString();
                        }

                        if (this.Match(node))
                        {
                            if (parentElement.Role.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE))
                            {
                                int cellIndex = -1;//后 670版本新增属性，老selector无
                                var cellIndexProp = this.Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE_CELLINDEX)).FirstOrDefault();
                                if (cellIndexProp != null)
                                {
                                    cellIndex = int.Parse(cellIndexProp.Value);
                                }
                                int rowIndex = int.Parse(this.Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE_ROW)).First().Value);
                                int columnIndex = int.Parse(this.Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE_COLUMN)).First().Value);

                                AccessibleTableInfo tbHeaderColumnInfo;
                                AccessibleTableCellInfo tbHeaderColumnCellInfo;
                                Rectangle cellRect = Rectangle.Empty;
                                Rectangle headerCellRect = Rectangle.Empty;
                                AccessibleContextNode accessibleNode = parentElement.accessibleNode as AccessibleContextNode;

                                JavaNode.accessBridge.Functions.ClearAccessibleSelectionFromContext(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle);
                                JavaNode.accessBridge.Functions.AddAccessibleSelectionFromContext(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle, cellIndex != -1 ? cellIndex : 0);
                                int selCount = JavaNode.accessBridge.Functions.GetAccessibleSelectionCountFromContext(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle);
                                if (selCount > 0)
                                {
                                    int selectionIndex = 0;
                                    if (selCount > 1)
                                    {
                                        selectionIndex = columnIndex;
                                    }
                                    for (int i = 0; i < selCount; i++)
                                    {
                                        var cellASFC = JavaNode.accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle, selectionIndex);
                                        if (!cellASFC.IsNull)
                                        {
                                            JavaNode.accessBridge.Functions.ClearAccessibleSelectionFromContext(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle);
                                            AccessibleContextNode tableAc = new AccessibleContextNode(JavaNode.accessBridge, cellASFC);
                                            var cellInfo = tableAc.GetInfo();
                                            cellRect = new Rectangle(cellInfo.x, cellInfo.y, cellInfo.width, cellInfo.height);
                                        }
                                    }
                                }
                                if (selCount <= 0 || cellIndex == -1)
                                {
                                    JavaNode.accessBridge.Functions.GetAccessibleTableColumnHeader(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle, out tbHeaderColumnInfo);
                                    JavaNode.accessBridge.Functions.GetAccessibleTableCellInfo(tbHeaderColumnInfo.accessibleTable.JvmId, tbHeaderColumnInfo.accessibleTable, 0, columnIndex, out tbHeaderColumnCellInfo);
                                    AccessibleContextNode headerColumnCellAc = new AccessibleContextNode(JavaNode.accessBridge, tbHeaderColumnCellInfo.accessibleContext);
                                    var infoHeaderColumnCell = headerColumnCellAc.GetInfo();
                                    headerCellRect = new Rectangle(infoHeaderColumnCell.x, infoHeaderColumnCell.y, infoHeaderColumnCell.width, infoHeaderColumnCell.height);


                                    if (cellRect != Rectangle.Empty)// 老版本selector 已经获取到第一行对应列对应单元格的区域（因为[cellIndex != -1 ? cellIndex : 0]这个条件得到的数据在第一行）
                                    {
                                        int y = headerCellRect.Y + headerCellRect.Height + (rowIndex) * cellRect.Height;
                                        node.BoundingRectangleForCell = new Rectangle(cellRect.X, y, cellRect.Width, cellRect.Height);
                                    }
                                    else//selCount <= 0 ，必不能获取到某个具体数据单元格的区域
                                    {
                                        //JavaNode.accessBridge.Functions.GetAccessibleTableInfo(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle, out var tableInfo);
                                        //AccessibleContextNode tableAc = new AccessibleContextNode(JavaNode.accessBridge, accessibleNode.AccessibleContextHandle);
                                        //var tableAcInfo = tableAc.GetInfo();
                                        //int rowHeight = tableAcInfo.height / tableInfo.rowCount;
                                        //int y = tableAcInfo.y + rowIndex * rowHeight;
                                        //node.BoundingRectangleForCell = new Rectangle(headerCellRect.X, y, headerCellRect.Width, rowHeight);

                                        int y = headerCellRect.Y + headerCellRect.Height + (rowIndex) * headerCellRect.Height;
                                        node.BoundingRectangleForCell = new Rectangle(headerCellRect.X, y, headerCellRect.Width, headerCellRect.Height);
                                    }
                                }
                                else
                                {
                                    node.BoundingRectangleForCell = cellRect;
                                }
                            }
                            matchs.Add(node);
                        }
                        index++;
                    }
                }
                return matchs.ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return new JavaNode[] { };
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Title))
            {
                return Title;
            }
            else
            {
                return $"{ConstFields.SELECTOR_ID}:{Id},{ConstFields.SELECTOR_ROLE}:{Role},{ConstFields.SELECTOR_NAME}:{Name}";
            }
        }

        private static bool Match(JavaSelectorItem item, JavaNode element)
        {
            if (!string.IsNullOrEmpty(item.ElementName))
            {
                if (!item.ElementName.EqualIgnoreCase(element.Role))
                {
                    return false;
                }
            }

            foreach (var p in item.Properties.Where(x => x.Enabled))
            {
                if (p.Name.EqualIgnoreCase(ConstFields.SELECTOR_NAME))
                {
                    bool isMatch = p.ConiditionMatch(element.Name);

                    if (!isMatch)
                    {
                        return false;
                    }
                }

                if (p.Name.EqualIgnoreCase(ConstFields.SELECTOR_INDEX_IN_PARENT))
                {
                    bool isMatch = p.ConiditionMatch(element.Idx);

                    if (!isMatch)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private IEnumerable<string> GetPropertyNames()
        {
            string[] allNames = { ConstFields.SELECTOR_ID, ConstFields.SELECTOR_NAME, ConstFields.SELECTOR_ROLE, ConstFields.SELECTOR_TITLE, ConstFields.SELECTOR_INDEX_IN_PARENT };

            foreach (var item in allNames)
            {
                if (this.Properties.Count(x => x.Name.EqualIgnoreCase(item)) == 1)
                {
                    yield return item;
                }
            }
        }
    }
}