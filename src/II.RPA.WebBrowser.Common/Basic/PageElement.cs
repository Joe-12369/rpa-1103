using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace II.RPA.WebBrowser.Common.Basic
{
    public abstract class PageElement
    {
        protected static ILogger logger = Logger.OfContext();
        public BrowserTabPage TabPage { get; set; }
        public string UserDefinedID { get; set; }
        public eGuiType GuiType => this.TabPage.GuiType;

        public PageElement(string userDefinedID, BrowserTabPage tabPage)
        {
            this.UserDefinedID = userDefinedID;
            this.TabPage = tabPage;
        }

        private Rectangle rectangle = Rectangle.Empty;
        public Rectangle BoundingRectangle
        {
            get
            {
                /* Rectangle 为空时主动获取一次，若无有效值，将width设置为1，避免多次获取浪费性能 ;
                 可重复使用BuildElementBounding()更新 Rectangle  */
                if (rectangle == Rectangle.Empty)
                {
                    rectangle = this.BuildElementBounding();
                    if (rectangle == Rectangle.Empty)
                    {
                        rectangle.Width = 1;
                    }
                }
                return rectangle;
            }
            set { rectangle = value; }
        }

        private Rectangle relativeRectangle = Rectangle.Empty;
        public Rectangle BuildRelativeElementRectangle
        {
            get
            {
                if (relativeRectangle == Rectangle.Empty)
                {
                    relativeRectangle = this.BuildRelativeElementBounding();
                }
                return relativeRectangle;
            }
            set { relativeRectangle = value; }
        }

        protected WebDataProvider WebDataProvider
        {
            get
            {
                Debug.Assert(this.TabPage != null);
                return this.TabPage?.Browser.WebDataProvider;
            }
        }
        public virtual void SetCaupureBounding(Rectangle boundingRectangle)
        {
            boundingRectangle = new Rectangle
            {
                X = (int)(boundingRectangle.X / NativeMethods.ScaleX),
                Y = (int)(boundingRectangle.Y / NativeMethods.ScaleY),
                Width = (int)(boundingRectangle.Width / NativeMethods.ScaleX),
                Height = (int)(boundingRectangle.Height / NativeMethods.ScaleY),
            };
            //同步Document的偏移量
            this.TabPage.SyncDocumentRectangle();
            logger.LogDebug($"SyncDocumentRectangle end");
            this.BoundingRectangle = new Rectangle
            {
                X = boundingRectangle.X + this.TabPage.DocumentRectangle.X,
                Y = boundingRectangle.Y + this.TabPage.DocumentRectangle.Y,
                Width = boundingRectangle.Width,
                Height = boundingRectangle.Height,
            };
        }
        public virtual Rectangle BuildElementBounding()
        {
            Rectangle boundingRectangle = this.BuildRelativeElementBounding();
            logger.LogDebug($"BuildRelativeElementBounding end");

            //同步Document的偏移量
            this.TabPage.SyncDocumentRectangle();
            logger.LogDebug($"SyncDocumentRectangle end");
            this.BoundingRectangle = new Rectangle
            {
                X = boundingRectangle.X + this.TabPage.DocumentRectangle.X,
                Y = boundingRectangle.Y + this.TabPage.DocumentRectangle.Y,
                Width = boundingRectangle.Width,
                Height = boundingRectangle.Height,
            };
            return this.BoundingRectangle;
        }
        public virtual List<Rectangle> BuildElementBoundingAll(List<string> elementIds)
        {
            List<Rectangle> boundingRectangles = this.BuildRelativeElementBoundingAll(elementIds);
            logger.LogDebug($"BuildRelativeElementBoundingAll end");

            //同步Document的偏移量
            this.TabPage.SyncDocumentRectangle();
            logger.LogDebug($"SyncDocumentRectangle end");
            for (int i = 0; i < boundingRectangles.Count; i++)
            {
                boundingRectangles[i] = new Rectangle(boundingRectangles[i].X + this.TabPage.DocumentRectangle.X,
                    boundingRectangles[i].Y + this.TabPage.DocumentRectangle.Y,
                    boundingRectangles[i].Width,
                    boundingRectangles[i].Height);
            }
            return boundingRectangles;
        }
        public virtual List<Rectangle> BuildRelativeElementBoundingAll(List<string> elementIds)
        {
            List<Rectangle> rectangles = new List<Rectangle>();
            var retJToken = this.WebDataProvider.BuildElementBoundingAll(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                elementIds = elementIds,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });

            foreach (JToken item in retJToken)
            {
                Rectangle boundingRectangle = new Rectangle
                {
                    X = item.Value<int>("left"),
                    Y = item.Value<int>("top"),
                    Width = item.Value<int>("width"),
                    Height = item.Value<int>("height")
                };
                boundingRectangle = new Rectangle
                {
                    X = (int)(boundingRectangle.X / NativeMethods.ScaleX),
                    Y = (int)(boundingRectangle.Y / NativeMethods.ScaleY),
                    Width = (int)(boundingRectangle.Width / NativeMethods.ScaleX),
                    Height = (int)(boundingRectangle.Height / NativeMethods.ScaleY),
                };
                rectangles.Add(boundingRectangle);
            }
            return rectangles;
        }

        public virtual Rectangle BuildRelativeElementBounding()
        {
            var retJToken = this.WebDataProvider.BuildElementBounding(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            Rectangle boundingRectangle = new Rectangle
            {
                X = retJToken.Value<int>("left"),
                Y = retJToken.Value<int>("top"),
                Width = retJToken.Value<int>("width"),
                Height = retJToken.Value<int>("height")
            };
            boundingRectangle = new Rectangle
            {
                X = (int)(boundingRectangle.X / NativeMethods.ScaleX),
                Y = (int)(boundingRectangle.Y / NativeMethods.ScaleY),
                Width = (int)(boundingRectangle.Width / NativeMethods.ScaleX),
                Height = (int)(boundingRectangle.Height / NativeMethods.ScaleY),
            };
            return boundingRectangle;
        }

        public string BuildSelector(bool innerTextFirst, bool rawData = false, bool excludeInnerText = false)
        {
            var model = new Params.ParamBuildSelectorModel()
            {
                innerTextFirst = innerTextFirst,
                rawData = rawData,
                excludeInnerText = excludeInnerText
            };
            return BuildSelector(model);
        }
        public string BuildSelector(ParamBuildSelectorModel paramModel)
        {
            paramModel.TabId = this.TabPage.TabId;
            paramModel.ElementId = this.UserDefinedID;

            logger.LogDebug($"action timestamp:{TabPage.ParamExtensionModel.RunParam.StartTimeStamp}");
            /// 对所有selector生成的操作 都重置超时时间位30s
            //if (string.IsNullOrEmpty(this.TabPage.ParamExtensionModel.RunParam.StartTimeStamp))
            {
                var runParam = this.TabPage.ParamExtensionModel.RunParam;
                runParam.StartTimeStamp = Stopwatch.GetTimestamp().ToString();
                runParam.TotalTime = 30000;
                this.TabPage.ParamExtensionModel.RunParam = runParam;
            }

            paramModel.RunParam = this.TabPage.ParamExtensionModel.RunParam;
            var retJToken = this.WebDataProvider.BuildSelector(paramModel);
            return retJToken.ToString();
        }
        public string BuildCssSelector()
        {
            try
            {
                var retJToken = this.WebDataProvider.BuildCssSelector(new Params.ParamModel()
                {
                    TabId = this.TabPage.TabId,
                    ElementId = this.UserDefinedID,
                    RunParam = this.TabPage.ParamExtensionModel.RunParam
                });
                var value = retJToken["path"].ToString();
                return value;
            }
            catch
            {
                return "";//异常捕获为原逻辑，可能和脚本内的返回值有关，先拿过来
            }
        }
        public void ScrollIntoViewIfNeeded_V2()
        {
            this.WebDataProvider.ScrollIntoViewIfNeeded_V2(new Params.ParamScrollModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
        }
        public void ScrollIntoViewIfNeeded(string location = "center")
        {
            this.WebDataProvider.ScrollIntoViewIfNeeded(new Params.ParamScrollModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                location = location,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
        }
        public bool IsDisplayed()
        {
            var retJToken = this.WebDataProvider.IsDisplayed(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.As(false);
        }
        public List<string> GetTextAll(List<string> elementIds)
        {
            var retJToken = this.WebDataProvider.GetTextAll(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                elementIds = elementIds,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToObject<List<string>>();
        }
        public string GetText()
        {
            var retJToken = this.WebDataProvider.GetText(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public void Input(string value, bool isReplace = true)
        {
            logger.LogInformation($"浏览器通信透传超时时间:{this.TabPage.ParamExtensionModel.RunParam.Timeout}ms");
            this.WebDataProvider.Input(new Params.ParamInputModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                value = value,
                isReplace = isReplace,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
        }
        public string GetIsTableName()
        {
            var retJToken = this.WebDataProvider.GetIsTableName(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public string GetTableData()
        {
            var retJToken = this.WebDataProvider.GetTableData(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public string GetAttribute(string name, string apiVersion)
        {
            var retJToken = this.WebDataProvider.GetAttribute(new Params.ParamAttributeModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                name = name,
                RunParam = this.TabPage.ParamExtensionModel.RunParam,
                apiVersion = apiVersion
            });
            if (string.IsNullOrEmpty(apiVersion))
            {
                return retJToken?.ToString();
            }
            return retJToken.Value<string>("value");
        }
        public List<string> GetAttributeAll(List<string> elementIds, string attributeName, bool isIntact, string apiVersion)
        {
            var retJToken = this.WebDataProvider.GetAttributeAll(new Params.ParamAttributeModel()
            {
                TabId = this.TabPage.TabId,
                elementIds = elementIds,
                name = attributeName,
                isIntact = isIntact,
                apiVersion = apiVersion,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToObject<List<string>>();
        }
        public string GetAttribute_V1(string name, bool isIntact, string apiVersion)
        {
            var retJToken = this.WebDataProvider.GetAttribute_V1(new Params.ParamAttributeModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                name = name,
                isIntact = isIntact,
                apiVersion = apiVersion,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            if (string.IsNullOrEmpty(apiVersion))
            {
                return retJToken == null || retJToken?.Type == JTokenType.Null ? null : retJToken?.ToString();
            }
            return retJToken.Value<string>("value");
        }
        public string GetAttributes()
        {
            var retJToken = this.WebDataProvider.GetAttributes(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public List<string> GetHtmlAll(List<string> elementIds)
        {
            var retJToken = this.WebDataProvider.GetHtmlAll(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                elementIds = elementIds,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToObject<List<string>>();
        }
        public string GetHtml()
        {
            var retJToken = this.WebDataProvider.GetHtml(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public string GetCleanHtml()
        {
            var retJToken = this.WebDataProvider.GetCleanHtml(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public List<string> GetParentIdList(string filter, int level)
        {
            var idsJToken = this.WebDataProvider.GetParentIdList(new Params.ParamAssociatedElementModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                filter = filter,
                level = level,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return idsJToken.ToObject<List<string>>();
        }
        public List<string> GetSiblingsIdList(string filter, int position)
        {
            var idsJToken = this.WebDataProvider.GetSiblingsIdList(new Params.ParamAssociatedElementModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                filter = filter,
                position = position,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return idsJToken.ToObject<List<string>>();

        }
        public List<string> GetChildrenIdList(string filter, string index)
        {
            var idsJToken = this.WebDataProvider.GetChildrenIdList(new Params.ParamAssociatedElementModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                filter = filter,
                index = index,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            //返回的数据 是以 | 分隔的字符串
            var retList = idsJToken.ToString().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            return retList;
        }
        public bool IsEditable()
        {
            var retJToken = this.WebDataProvider.IsEditable(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return Convert.ToBoolean(retJToken);
        }
        public bool GetProperty(string name)
        {
            var retJToken = this.WebDataProvider.GetProperty(new Params.ParamGetPropertyModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                name = name,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return Convert.ToBoolean(retJToken);
        }

        public string GetCheckStatus()
        {
            var retJToken = this.WebDataProvider.GetCheckStatus(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public string GetSelectOption(string type = "text")
        {
            var retJToken = this.WebDataProvider.GetSelectOption(new Params.ParamSelectOptionModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                type = type,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public List<string> GetSelectOptions(string type = "text", int selected = 0)
        {
            var retJToken = this.WebDataProvider.GetSelectOptions(new Params.ParamSelectOptionModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                type = type,
                selected = selected,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToObject<List<string>>();
        }
        public void Click()
        {
            this.WebDataProvider.Click(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
        }
        public void RightClick()
        {
            this.WebDataProvider.RightClick(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
        }
        public void DoubleClick()
        {
            this.WebDataProvider.DoubleClick(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
        }
        public void RightDoubleClick()
        {
            this.WebDataProvider.RightDoubleClick(this.TabPage.TabId, this.UserDefinedID);
        }
        public string SetAttribute(string name, string value)
        {
            var retJToken = this.WebDataProvider.SetAttribute(new Params.ParamAttributeModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                name = name,
                value = value,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public string SetCheckStatus(bool isChecked)
        {
            var retJToken = this.WebDataProvider.SetCheckStatus(new Params.ParamCheckStatusModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                isChecked = isChecked,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public void Focus()
        {
            this.WebDataProvider.Focus(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
        }
        //public List<string> GetElementShot()
        //{
        //    var retJToken = this.WebDataProvider.GetElementShot(new Params.ParamModel()
        //    {
        //        TabId = this.TabPage.TabId,
        //        ElementId = this.UserDefinedID,
        //        RunParam = this.TabPage.ParamExtensionModel.RunParam
        //    });
        //    return retJToken.ToObject<List<string>>();
        //}
        public List<string> GetElementShot()
        {
            var retJToken = this.WebDataProvider.GetElementShot(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToObject<List<string>>();
        }
        public string SetSelectOptions(List<int> idxList)
        {
            var retJToken = this.WebDataProvider.SetSelectOptions(new Params.ParamSelectOptionModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                idxList = idxList,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        [Obsolete("新功能不要再使用此方法，必须指定world")]
        public string Executejs(string scriptCode)
        {
            //var retJToken = this.WebDataProvider.Executejs(new ParamExecuteJavaScriptModel()
            //{
            //    TabId = this.TabPage.TabId,
            //    ElementId = this.UserDefinedID,
            //    scriptCode = scriptCode,
            //    RunParam = this.TabPage.ParamExtensionModel.RunParam
            //});
            return Executejs(scriptCode, "MAIN");
        }
        public string Executejs(string scriptCode, string world)
        {
            var retJToken = this.WebDataProvider.Executejs(new ParamExecuteJavaScriptModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                scriptCode = scriptCode,
                RunParam = this.TabPage.ParamExtensionModel.RunParam,
                ExecutionWorld = world
            });
            return retJToken.ToString();
        }
        public virtual string GetTurningPageElement(List<string> cssSelectors)
        {
            var retJToken = this.WebDataProvider.GetTurningPageElement(new Params.ParamcssSelectorModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                cssSelectors = cssSelectors,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        public virtual string GetSimilarElement()
        {
            var retJToken = this.WebDataProvider.GetSimilarElement(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken?.ToString();
        }

        public virtual string[] GetUsableAttributes()
        {
            var retJToken = this.WebDataProvider.GetUsableAttributes(new Params.ParamModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam
            });
            return retJToken.ToString().ToObject<string[]>();
        }

        public string GetLineAndBarChartData(ParamGetChartDataModel param)
        {
            var retJToken = this.WebDataProvider.GetLineAndBarChartData(new Params.ParamGetChartDataModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam,
                moveDirection = param.moveDirection,
                moveSpeed = param.moveSpeed,
                moveModel = param.moveModel,
                offset = param.offset,
                parentElementId = param.parentElementId
            });
            return retJToken.ToString();
        }
        public string GetPieChartData(ParamGetChartDataModel param)
        {
            var retJToken = this.WebDataProvider.GetPieChartData(new Params.ParamGetChartDataModel()
            {
                TabId = this.TabPage.TabId,
                ElementId = this.UserDefinedID,
                RunParam = this.TabPage.ParamExtensionModel.RunParam,
                radiusRatio = param.radiusRatio,
                horOffset = param.horOffset,
                verOffset = param.verOffset,
                moveSpeed = param.moveSpeed
            });
            return retJToken.ToString();
        }
    }
}
