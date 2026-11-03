using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.GUI.Commons;
using sapfewse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.SAP
{
    internal static class SAPExtend
    {
        public static string GetNodeTextByColTreeKey(this GuiTree tree, string ext)
        {
            string[] colNodelist = ext?.Split(new char[] { '?' });
            string curItemText = "";
            if (colNodelist.Count() == 2)
            {
                curItemText = tree.GetItemText(colNodelist[0], colNodelist[1]);
                if (string.IsNullOrEmpty(curItemText))
                {
                    curItemText = "extId_" + colNodelist[1];
                }
            }
            return curItemText;
        }
        public static void SelectItemBySubId(this GuiTree tree, string ext, RPAMousePresses type)
        {
            string[] colNodeList = ext.Split(new char[] { '?' });
            if (colNodeList.Count() != 2)
            {
                throw new Exception("Sap Tree SelectItemBySubId发生错误".I18n("controlType_SAPExtend_FB07DBE2"));
            }

            switch (type)
            {
                case RPAMousePresses.Click:
                case RPAMousePresses.DoubleClick:
                default:
                    // ClickLink 触发节点的展开/跳转行为，与 GUI 中双击效果一致。
                    // DoubleClickItem API 在某些 SAP 版本上不生效，统一使用 ClickLink。
                    tree.ClickLink(colNodeList[0], colNodeList[1]);
                    break;
            }
        }

        public static string GetNodeTextPathByKey(this GuiTree tree,string ext)
        {
            string[] colNodeList = ext.Split(new char[] { '?' });
            List<string> nodeTextPath=new List<string>();
            if (colNodeList.Count() == 2)
            {
                nodeTextPath.Insert(0, "extId:" + colNodeList[1]);
            }
            nodeTextPath.Insert(0, tree.GetTextByItemOrNode(colNodeList[0]));

            string parentNode = tree.GetParent(colNodeList[0]);
            while(parentNode!="")
            {
                nodeTextPath.Insert(0, tree.GetTextByItemOrNode(parentNode));
                parentNode = tree.GetParent(parentNode);
            }

            return string.Join("/", nodeTextPath);
        }


        public static void BringNodeIntoViewByNodeKey(this GuiTree guiTree,string nodeExt,bool needselect)
        {
            List<string> nodePath = new List<string>();
            string curNodeKey = nodeExt.Contains("?") ? nodeExt.Split('?')[0] : nodeExt;
            string parentNode=guiTree.GetParent(curNodeKey);
            while(parentNode!="")
            {
                nodePath.Insert(0, parentNode);
                parentNode=guiTree.GetParent(parentNode);
            }
            foreach(string curNode in nodePath)
            {
                guiTree.ExpandNode(curNode);
            }

            // 将目标节点的直接父节点设为 TopNode，确保目标节点滚动到可视区域内。
            // 如果 nodePath 非空，取最后一个（离目标最近）的祖先节点；否则用目标节点自身。
            string topNodeKey = nodePath.Count > 0 ? nodePath.Last() : curNodeKey;
            try
            {
                guiTree.TopNode = topNodeKey;
            }
            catch
            {
                // TopNode 并非所有 SAP 版本都支持，失败时忽略
            }

            if(needselect)
            {
                guiTree.SelectNode(curNodeKey);
            }
        }

        public static string GetNodeKeyByNodeTextPath(this GuiTree guiTree,string textPath,string extId)
        {
            string[] nodesTextPath = textPath.Split('/');


            return guiTree.GetTreeNodeKey(nodesTextPath, extId);
        }
        /// <summary>
        /// 通过完整的路径来获取对应的节点信息
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static string GetTreeNodeKey(this GuiTree guitree,IEnumerable<string> nodesTextPath,string extId)
        {
            string currentNodeKey = string.Empty;
            var subNodesColOfRoot = guitree.GetSubNodesColOfRoot();
            foreach (var nodeKey in subNodesColOfRoot)
            {
                if (guitree.GetTextByItemOrNode(nodeKey) == nodesTextPath.ElementAt(0)) //先确认根节点
                {
                    currentNodeKey = nodeKey;
                    guitree.ExpandNode(nodeKey);
                    break;
                }
            }

            if (nodesTextPath.Count() == 1) //如果只有根节点
            {
                return currentNodeKey;
            }
            int count=nodesTextPath.Count();
            for (int i = 1; i < count; i++)
            {

                bool isFindLast = false;
                GuiCollection obj = guitree.GetSubNodesCol(currentNodeKey) as GuiCollection;
                string nodeText = nodesTextPath.ElementAt(i);
                foreach (object curChild in obj)
                {
                    if (nodesTextPath.Count() == i + 1 && !string.IsNullOrEmpty(extId))  //如果是最后一个节点
                    {
                        if (guitree.GetTextByItemOrNode(curChild.ToString()) == nodeText)
                        {
                            currentNodeKey = curChild.ToString();
                            isFindLast = true;
                            break;
                        }
                    }
                    else if(guitree.GetTextByItemOrNode(curChild.ToString())==nodeText)
                    {
                        currentNodeKey=curChild.ToString();
                        isFindLast = true;
                        if(i+1<nodesTextPath.Count())
                        {
                            guitree.ExpandNode(currentNodeKey);
                        }
                        break;
                    }
                }
                if(!isFindLast)
                {
                    throw new Exception("没有找到对应节点信息".I18n("controlType_SAPExtend_AC6F94E5"));
                }
            }

            return currentNodeKey;
        }
        public static string GetTextByItemOrNode(this GuiTree tree,string nodekey)
        {
            string nodeText = string.Empty;
            if(tree.GetTreeType()==2)  //复杂列表
            {
                nodeText = tree.GetItemText(nodekey, "TEXT") ?? string.Empty;
            }

           if(string.IsNullOrEmpty(nodeText))
            {
                nodeText = tree.GetNodeTextByKey(nodekey);
            }
            return nodeText.Replace("/", "");

        }

        //获取根节点
        public static List<string> GetSubNodesColOfRoot(this GuiTree guiTree)
        {
            List<string> nodesKeys = new List<string>();
            int nodesCount = guiTree.GetNodeChildrenCountByPath(string.Empty);
            string currentNodeKey = guiTree.GetNodeKeyByPath("1");
            nodesKeys.Add(currentNodeKey);
            for(int i = 1;i<nodesCount;i++)
            {
                currentNodeKey = guiTree.GetNextNodeKey(currentNodeKey);
                nodesKeys.Add(currentNodeKey);
            }
            return nodesKeys;
        }

    }
}
