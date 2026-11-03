using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons;
using sapfewse;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace II.RPA.Plugin.SAP.Controls
{
    internal class GuiTreeAction : ISapControl
    {
        public void Execute(ApplicationContext context)
        {
            throw new NotImplementedException();
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            Rectangle pr = new Rectangle();
            string extId = "TEXT";

         
            if(!string.IsNullOrEmpty(context.subId))  //匹配的时候；
            {
                extId= context.subId;
            }
            try
            {
                GuiShell gs = context.component as GuiShell;

                if (gs is GuiTree guiTree)
                {
                    if (context.inspectId!=null)
                    {
                        string[] colNodelist = context.inspectId?.Split('?');// 拾取的时候，树节点信息中回带nodeKey？extId的形式；

                        if (colNodelist.Count() == 2)
                        {
                            extId = colNodelist[1];
                        }
                        int x = guiTree.ScreenLeft + guiTree.GetItemLeft(colNodelist[0], extId);
                        int y = guiTree.ScreenTop + guiTree.GetItemTop(colNodelist[0], extId);
                        int width = guiTree.GetItemWidth(colNodelist[0], extId);
                        int height = guiTree.GetItemHeight(colNodelist[0], extId);

                        pr = new Rectangle(x, y, width, height);
                    }
                    else
                    {
                        //这里是匹配的逻辑，需要根据extension去找到对应的key
                        string nodekey = guiTree.GetNodeKeyByNodeTextPath(context.ext, context.subId);

                        int x = guiTree.ScreenLeft + guiTree.GetItemLeft(nodekey, extId);
                        int y = guiTree.ScreenTop + guiTree.GetItemTop(nodekey, extId);
                        int width = guiTree.GetItemWidth(nodekey, extId);
                        int height = guiTree.GetItemHeight(nodekey, extId);
                        pr = new Rectangle(x, y, width, height);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GuiTree throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiTree guiTree = context.component as GuiTree;
                if(guiTree!=null)
                {
                    string nodeKey = guiTree.GetNodeKeyByNodeTextPath(context.ext, context.subId);
                    if(!string.IsNullOrEmpty(context.subId))
                    {
                        return guiTree.GetItemText(nodeKey, context.subId);
                    }
                    else
                    {
                        return guiTree.GetItemText(nodeKey, "TEXT");
                    }
             
                }
                
            }
            catch (Exception ex)
            {
                throw new Exception("GuiTree throw ex", ex);
            }
            return string.Empty;
        }

        public void SetText(ApplicationContext context, string txt)
        {
            throw new Exception("no setText method of this Type");
        }

        public void Click(ApplicationContext context, RPAMousePresses clickType)
        {
            try
            {
                GuiTree guiTree = context.component as GuiTree;
                if (guiTree != null)
                {
                    string nodeKey = guiTree.GetNodeKeyByNodeTextPath(context.ext, context.subId);
                    string ext = $"{nodeKey}?{context.subId}";
                    guiTree.SelectItemBySubId(ext, clickType);
                }

            }
            catch (Exception ex)
            {
                throw new Exception("GuiTree throw ex", ex);
            }        
        }
    }
}
