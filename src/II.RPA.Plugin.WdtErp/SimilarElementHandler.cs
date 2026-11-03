using FlaUI.Core.Logging;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.Commons;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Logger = II.RPA.Logging.Logger;

namespace II.RPA.Plugin.WdtErp
{
    internal class SimilarElementHandler
    {
        public ILogger logger = Logger.OfContext();

        private static readonly Lazy<SimilarElementHandler> _instance = new Lazy<SimilarElementHandler>(() => new SimilarElementHandler());

        public static SimilarElementHandler Instance { get { return _instance.Value; } }

        private SimilarElementHandler()
        {
        }
        public List<IGuiNode> QuerySelectorAll(GetElementFromSelectorParams args)
        {
            logger.LogDebug("开始执行 QuerySelectorAll");
            var elementSelector = JObject.Parse(args.Selector);
            var selectorArray = (JArray)elementSelector["selector"];

            IGuiNode rootNode = WdtErpProxy.Instance.GetElementFromSelector(args.Selector) as WdtErpNode;

            // 提取 table、tr、td 节点
            var tableNode = FindSelectorNode(selectorArray, "table");
            var trNode = FindSelectorNode(selectorArray, "tr");

            if (tableNode == null || trNode == null)
            {
                return new List<IGuiNode> { rootNode };
            }

            // 解析行索引
            var (rowAccurate, rowIndex) = ParseIndex(trNode);
            // 解析列索引
            var tdNode = FindSelectorNode(selectorArray, "td");

            if (tdNode == null)
            {
                // 选择行且固定索引 -> 直接返回根节点
                if (rowAccurate)
                    return new List<IGuiNode> { rootNode };
                else
                    return GetAllRows(elementSelector, selectorArray, trNode);
            }
            else
            {
                var (colAccurate, colIndex) = ParseIndex(tdNode);

                // 仅选择行 ->  返回该行的所有单元格
                if (rowAccurate && !colAccurate)
                {
                    return GetRowCells(elementSelector, selectorArray, trNode, rowIndex);
                }
                // 仅选择列 -> 返回该列的所有单元格
                if (!rowAccurate && colAccurate)
                    return GetColumnCells(elementSelector, selectorArray, tableNode, trNode, colIndex);

                // 行和列都选择 -> 返回单元格
                return new List<IGuiNode> { rootNode };

            }
        }

        private JToken FindSelectorNode(JArray selectorArray, string name)
        {
            // 找到指定 name 且 accurate=true 的节点
            return selectorArray.FirstOrDefault(n => n.Value<string>("name") == name && n.Value<bool>("accurate"));
        }

        private (bool accurate, int index) ParseIndex(JToken node)
        {
            // 从 props 中解析 @index 属性
            var props = node["props"] as JArray;
            var idx = props?.FirstOrDefault(p => p.Value<string>("name") == "@index");
            if (idx == null)
                return (false, 0);
            return (idx.Value<bool>("accurate"), idx.Value<int>("value"));
        }

        private List<IGuiNode> GetAllRows(JObject selectorObj, JArray selectorArray, JToken trNode)
        {
            // 获取所有行节点
            var tableElement = GetTableElement(selectorObj, selectorArray);
            int rowCount = WdtErpProxy.Instance.GetElementChildrenCount(tableElement);
            var nodes = new List<IGuiNode>();
            for (int i = 0; i < rowCount; i++)
            {
                nodes.Add(GetElementByIndex(selectorObj, selectorArray, trNode, i));
            }
            return nodes;
        }

        private List<IGuiNode> GetRowCells(JObject selectorObj, JArray selectorArray, JToken trNode, int rowIndex)
        {
            // 仅对行节点精确匹配
            var cloneArray = (JArray)selectorArray.DeepClone();
            ToggleAccuracyUntil(cloneArray, "tr");
            var trElement = GetElementByIndex(selectorObj, cloneArray, trNode, rowIndex);
            int colCount = WdtErpProxy.Instance.GetElementChildrenCount(trElement);

            // 遍历该行的所有单元格
            var tdNode = selectorArray.First(n => n.Value<string>("name") == "td");
            var nodes = new List<IGuiNode>();
            for (int j = 0; j < colCount; j++)
            {
                nodes.Add(GetElementByIndex(selectorObj, selectorArray, tdNode, j));
            }
            return nodes;
        }

        private List<IGuiNode> GetColumnCells(JObject selectorObj, JArray selectorArray, JToken tableNode, JToken trNode, int colIndex)
        {
            // 遍历所有行并获取指定列的单元格
            var tableElement = GetTableElement(selectorObj, selectorArray);
            int rowCount = WdtErpProxy.Instance.GetElementChildrenCount(tableElement);
            var nodes = new List<IGuiNode>();
            for (int i = 0; i < rowCount; i++)
            {
                var cell = GetElementByIndices(selectorObj, selectorArray, trNode, i, colIndex);
                if (cell != null)
                    nodes.Add(cell);
            }
            return nodes;
        }

        // 根据 @index 从指定节点获取元素
        private WdtErpNode GetElementByIndex(JObject selectorObj, JArray selectorArray, JToken node, int index)
        {
            SetIndexProperty(node, index);
            selectorObj["selector"] = selectorArray;
            return WdtErpProxy.Instance.GetElementFromSelector(selectorObj.ToString()) as WdtErpNode;
        }

        // 获取单元格节点
        private WdtErpNode GetElementByIndices(JObject selectorObj, JArray selectorArray, JToken trNode, int rowIdx, int colIdx)
        {
            SetIndexProperty(trNode, rowIdx);
            var tdNode = selectorArray.First(n => n.Value<string>("name") == "td");
            SetIndexProperty(tdNode, colIdx);
            selectorObj["selector"] = selectorArray;
            return WdtErpProxy.Instance.GetElementFromSelector(selectorObj.ToString()) as WdtErpNode;
        }

        // 获取表格元素，仅保留到 table 级别的精确匹配
        private WdtErpNode GetTableElement(JObject selectorObj, JArray selectorArray)
        {
            var tmp = (JArray)selectorArray.DeepClone();
            ToggleAccuracyUntil(tmp, "table");
            selectorObj["selector"] = tmp;
            return WdtErpProxy.Instance.GetElementFromSelector(selectorObj.ToString()) as WdtErpNode;
        }

        // 设置节点的 @index 属性
        private void SetIndexProperty(JToken node, int index)
        {
            var props = node["props"] as JArray;
            var idx = props.First(p => p.Value<string>("name") == "@index");
            idx["accurate"] = true;
            idx["value"] = index;
        }

        // 将 selector 中从末尾到指定名称节点前的所有节点设置为 accurate=false
        private void ToggleAccuracyUntil(JArray selectorArray, string stopName)
        {
            for (int i = selectorArray.Count - 1; i >= 0; i--)
            {
                var name = selectorArray[i].Value<string>("name");
                if (name == stopName)
                    break;
                selectorArray[i]["accurate"] = false;
            }
        }

    }
}
