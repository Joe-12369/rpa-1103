using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WindowsAccessBridgeInterop;

namespace II.RPA.Plugin.Java.Execute
{
    internal class JavaSelectorItem : SelectorItem
    {
        private static ILogger logger = Logger.OfContext();

        protected JavaSelectorItem()
        { }

        public string ElementName { get; set; }

        public new List<JavaSelectorItemProperty> Properties { get; set; }

        protected string Id
        {
            get
            {
                var foundItem = this.Properties.FirstOrDefault(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_ID));
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

        public JavaSelectorItem(JavaNode element, bool disable)
        {
            this.ElementName = element.Role;
            this.Properties = new List<JavaSelectorItemProperty>();

            if (!string.IsNullOrEmpty(element.Name))
            {
                this.Properties.Add(new JavaSelectorItemProperty(ConstFields.SELECTOR_NAME, element.Name));
            }

            if (!string.IsNullOrEmpty(element.Row))
            {
                this.Properties.Add(new JavaSelectorItemProperty(ConstFields.ROLE_VALUE_TABLE_ROW, element.Row) { Accurate = true });
            }
            if (!string.IsNullOrEmpty(element.Column))
            {
                this.Properties.Add(new JavaSelectorItemProperty(ConstFields.ROLE_VALUE_TABLE_COLUMN, element.Column) { Accurate = true });
            }
            if (!string.IsNullOrEmpty(element.CellIndex))
            {
                this.Properties.Add(new JavaSelectorItemProperty(ConstFields.ROLE_VALUE_TABLE_CELLINDEX, element.CellIndex) { Accurate = true });
            }
            if (int.Parse(element.Idx) > -1)
            {
                this.Properties.Add(new JavaSelectorItemProperty(ConstFields.SELECTOR_INDEX_IN_PARENT, element.Idx.ToString()));
            }

            this.Enabled = true;
            this.CanDisable = disable;
        }

        public override JObject json
        {
            get
            {
                this.ProcessValidProperties();

                SelectorProps selectorProp = new SelectorProps()
                {
                    Name = this.ElementName,
                    Props = this.Properties.OfType<JavaSelectorItemProperty>().Where(x => x.Enabled).Select(x => x.Convert2Props()).ToList()
                };

                return JObject.FromObject(selectorProp);
            }
        }

        private void ProcessValidProperties()
        {
            var xItem = this.FindAccordingName(ConstFields.SELECTOR_POSITION_X).FirstOrDefault();
            var idxItem = this.FindAccordingName(ConstFields.SELECTOR_INDEX_IN_PARENT).FirstOrDefault();

            if (xItem != null)
            {
                if (idxItem != null)
                {
                    idxItem.Enabled = false;
                }
            }
        }

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

        public bool Match(JavaNode m)
        {
            return Match(this, m);
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

            foreach (JavaSelectorItemProperty p in item.Properties.Where(x => x.Accurate))
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


        public JavaNode[] Matches(JavaNode parentElement, int windowX = 0, int windowY = 0)
        {
            var provider = AccessBridgeProvider.Singleton;
            if (!provider.CheckAccess())
                return provider.Invoke(() => Matches(parentElement, windowX, windowY));

            try
            {
                var matches = new List<JavaNode>();
                var propsDict = this.Properties.ToDictionary(p => p.Name.ToLowerInvariant(), p => p, StringComparer.OrdinalIgnoreCase);

                var children = parentElement.Children.OfType<JavaNode>().ToList();

                bool isTable = parentElement.Role.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE);

                int CellIndex = -1, RowIndex = -1, ColumnIndex = -1, ColumnCount = 0;

                AccessibleTableInfo tableInfo = null;

                //如果使用坐标进行查找，直接使用parentElement查找下级孩子节点坐标
                if (propsDict.ContainsKey(ConstFields.SELECTOR_POSITION_X.ToLowerInvariant()))
                {
                    if (TryParseInt(propsDict[ConstFields.SELECTOR_POSITION_X.ToLowerInvariant()].Value, out int x) &&
                        TryParseInt(propsDict[ConstFields.SELECTOR_POSITION_Y.ToLowerInvariant()].Value, out int y))
                    {
                        var node = parentElement.GetNodeByPoint(x + windowX, y + windowY);
                        if (node != null)
                        {
                            node.Idx = "0"; // 坐标定位只有一个结果
                            matches.Add(node);
                        }
                    }
                    return matches.ToArray();
                }
                else
                {
                    //表格子元素时，提前验证提供定位的表格信息
                    if (isTable)
                    {

                        if (propsDict.TryGetValue(ConstFields.ROLE_VALUE_TABLE_CELLINDEX.ToLowerInvariant(), out var cellProp) &&
                            TryParseInt(cellProp.Value, out int cellIndex))
                        {
                            CellIndex = cellIndex;
                        }
                        if (propsDict.TryGetValue(ConstFields.ROLE_VALUE_TABLE_ROW.ToLowerInvariant(), out var rowProp) &&
                           TryParseInt(rowProp.Value, out int rowIndex))
                        {
                            RowIndex = rowIndex;
                        }
                        if (propsDict.TryGetValue(ConstFields.ROLE_VALUE_TABLE_COLUMN.ToLowerInvariant(), out var colProp) &&
                          TryParseInt(colProp.Value, out int colIndex))
                        {
                            ColumnIndex = colIndex;
                        }
                        //没有提供任何表格定位信息，直接返回空
                        if (RowIndex == -1 && ColumnIndex == -1 && CellIndex == -1)
                        {
                            return Array.Empty<JavaNode>();
                        }

                        // 表格无数据时，直接返回空
                        if (!JavaNode.accessBridge.Functions.GetAccessibleTableInfo(
                                parentElement.accessibleNode.JvmId,
                                ((AccessibleContextNode)parentElement.accessibleNode).AccessibleContextHandle,
                                out tableInfo) || tableInfo.columnCount <= 0)
                        {
                            return Array.Empty<JavaNode>();
                        }

                        ColumnCount = tableInfo.columnCount;

                        // 验证 定位信息有效性,无效则返回空
                        if (RowIndex != -1 && ColumnIndex != -1 && CellIndex != -1 && (RowIndex * ColumnCount + ColumnIndex) != CellIndex)
                        {
                            return Array.Empty<JavaNode>();
                        }
                        logger.LogDebug($"表格总单元格数量：{children.Count},列数：{ColumnCount}");
                    }

                    for (int index = 0; index < children.Count; index++)
                    {
                        var element = children[index];
                        AccessibleContextNode accessibleNode = parentElement.accessibleNode as AccessibleContextNode;

                        element.Idx = index.ToString();
                        int currentRow = -1, currentCol = -1;
                        if (isTable)
                        {
                            currentRow = index / ColumnCount;
                            currentCol = index % ColumnCount;

                            // 提前终止：已越过目标单元格或目标行
                            if ((CellIndex != -1 && index > CellIndex) ||
                                (RowIndex != -1 && currentRow > RowIndex))
                            {
                                break;
                            }

                            // 跳过非目标项
                            bool isTargetCell = (CellIndex == -1 || CellIndex == index);
                            bool isTargetRow = (RowIndex == -1 || currentRow == RowIndex);
                            bool isTargetCol = (ColumnIndex == -1 || currentCol == ColumnIndex);

                            if (!(isTargetCell && isTargetRow && isTargetCol))
                            {
                                continue;
                            }

                        }

                        if (Match(element))
                        {
                            if (isTable)
                            {
                                logger.LogDebug($"匹配的单元格 {index} (行{currentRow} 列{currentCol})");

                                element.Row = currentRow.ToString();
                                element.Column = currentCol.ToString();
                                element.CellIndex = index.ToString();

                                AccessibleTableInfo tbHeaderColumnInfo;
                                AccessibleTableCellInfo tbHeaderColumnCellInfo;
                                Rectangle cellRect = Rectangle.Empty;
                                Rectangle headerCellRect = Rectangle.Empty;

                                bool validRect = false;

                                // 方案1：使用选择机制获取单元格区域
                                JavaNode.accessBridge.Functions.ClearAccessibleSelectionFromContext(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle);
                                JavaNode.accessBridge.Functions.AddAccessibleSelectionFromContext(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle, index);
                                int selectedCount = JavaNode.accessBridge.Functions.GetAccessibleSelectionCountFromContext(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle);


                                if (selectedCount > 0)
                                {
                                    for (int i = 0; i < selectedCount; i++)
                                    {
                                        var cellASFC = JavaNode.accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle, currentCol);
                                        if (!cellASFC.IsNull)
                                        {
                                            AccessibleContextNode tableAc = new AccessibleContextNode(JavaNode.accessBridge, cellASFC);
                                            var cellInfo = tableAc.GetInfo();

                                            // 检查获取的区域是否有效
                                            if (cellInfo.width > 0 && cellInfo.height > 0)
                                            {
                                                cellRect = new Rectangle(cellInfo.x, cellInfo.y, cellInfo.width, cellInfo.height);
                                                element.BoundingRectangleForCell = cellRect;
                                                validRect = true;
                                                logger.LogDebug($"选择机制成功：单元格[{currentRow},{currentCol}] 区域: {cellRect}");
                                                break;
                                            }
                                        }
                                    }
                                }

                                // 方案2：如果选择机制失败，使用表头信息+几何计算
                                if (!validRect)
                                {
                                    if (JavaNode.accessBridge.Functions.GetAccessibleTableColumnHeader(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle, out tbHeaderColumnInfo))
                                    {
                                        if (JavaNode.accessBridge.Functions.GetAccessibleTableCellInfo(tbHeaderColumnInfo.accessibleTable.JvmId, tbHeaderColumnInfo.accessibleTable, 0, currentCol, out tbHeaderColumnCellInfo))
                                        {
                                            if (!tbHeaderColumnCellInfo.accessibleContext.IsNull)
                                            {
                                                AccessibleContextNode headerColumnCellAc = new AccessibleContextNode(JavaNode.accessBridge, tbHeaderColumnCellInfo.accessibleContext);
                                                var infoHeaderColumnCell = headerColumnCellAc.GetInfo();
                                                headerCellRect = new Rectangle(infoHeaderColumnCell.x, infoHeaderColumnCell.y, infoHeaderColumnCell.width, infoHeaderColumnCell.height);

                                                if (validRect && cellRect != Rectangle.Empty)
                                                {
                                                    // 使用选择机制获取的单元格尺寸 + 表头位置计算
                                                    int y = headerCellRect.Y + headerCellRect.Height + (currentRow) * cellRect.Height;
                                                    element.BoundingRectangleForCell = new Rectangle(cellRect.X, y, cellRect.Width, cellRect.Height);
                                                    logger.LogDebug($"混合计算：使用选择机制尺寸+表头位置");
                                                }
                                                else
                                                {
                                                    // 完全使用表头信息计算
                                                    int y = headerCellRect.Y + headerCellRect.Height + (currentRow) * headerCellRect.Height;
                                                    element.BoundingRectangleForCell = new Rectangle(headerCellRect.X, y, headerCellRect.Width, headerCellRect.Height);
                                                    logger.LogDebug($"表头计算：完全使用表头信息");
                                                }
                                                validRect = true;
                                            }
                                        }
                                    }
                                }

                                if (!validRect)
                                {
                                    element.BoundingRectangleForCell = Rectangle.Empty;
                                    logger.LogWarning($"无法获取单元格[{currentRow},{currentCol}]的有效区域信息");
                                }
                            }
                            matches.Add(element);
                        }
                    }
                }
                return matches.ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return new JavaNode[] { };
        }

        private static bool TryParseInt(string value, out int x)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return int.TryParse(value, out x);
            }
            x = -1;
            return false;
        }


        /// <summary>
        /// update : wenyi, ZOCO-22691,ZOCO-21758
        /// 启用条件属性，直到匹配到唯一的节点
        /// 旧逻辑：逐次启用条件属性（Enable=false），直到匹配到唯一的节点,默认所有属性都选择（Accurate = true），如果匹配到多个节点，则继续启用下一个属性，直到匹配到唯一节点
        /// 新需求逻辑：尽可能多的显示节点属性（Enable=true），尽可能少的选择属性（Accurate = false）
        ///                    
        /// 可以优化：在获取元素的原始节点时，处理属性启用，现在处理相当于把所有节点全都转成 JavaNode 去处理，性能上有点浪费。
        /// </summary>     
        /// <param name="parent">父节点</param>
        public void EnumNeededProperties(JavaNode parent)
        {

            bool matched = false;
            IEnumerable<JavaNode> children = parent?.Children?.OfType<JavaNode>() ?? Enumerable.Empty<JavaNode>();
            if (children.Count() <= 1)
            {
                //只有一个子节点，不选择任何属性；
                return;
            }

            foreach (var prop in this.Properties)
            {
                //先不启用 index 属性
                if (prop.Name.Equals(ConstFields.SELECTOR_INDEX_IN_PARENT))
                {
                    continue;
                }
                //找出兄弟节点中最后一个层级节点属性中是否有值等于当前属性值的节点，只有一个节点满足条件，则选择该属性就行了
                if (children?.Count(c =>
                     c?.LastOrDefault()?.Properties.OfType<JavaSelectorItemProperty>()?.Count(p =>
                        p.Name.EqualIgnoreCase(ConstFields.SELECTOR_NAME) && p.ConiditionMatch(c.Name)) == 1) == 1)
                {
                    matched = true;
                    break;
                }
            }
            if (!matched)
            {
                //其他属性不能唯一确定节点，选择 index 属性
                this.Properties.FirstOrDefault(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_INDEX_IN_PARENT)).Accurate = true;
            }
        }
    }

    internal static class SelectorItemExtendsion
    {
        internal static IEnumerable<JavaSelectorItemProperty> FindAccordingName(this JavaSelectorItem selectorItem, string propName)
        {
            var ret = selectorItem?.Properties.OfType<JavaSelectorItemProperty>().Where(x => x.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
            if (ret != null)
            {
                foreach (var item in ret)
                {
                    yield return item;
                }
            }
        }

        internal static IEnumerable<JavaSelectorItemProperty> FindEnableAccordingName(this JavaSelectorItem selectorItem, string propName)
        {
            var ret = selectorItem?.Properties.OfType<JavaSelectorItemProperty>().Where(x => x.Enabled && x.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
            if (ret != null)
            {
                foreach (var item in ret)
                {
                    yield return item;
                }
            }
        }
    }



}
