using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.Plugins
{
    [Obsolete]
    public class IsSimilarElementExist : GuiComponentBase<ParamIsSimilarElementExistModel>
    {
        private static ILogger logger = Logger.OfContext();
        private readonly GuiHighlightService guiHighlightService;

        public IsSimilarElementExist(GuiHighlightService guiHighlightService)
        {
            this.guiHighlightService = guiHighlightService;
        }


        private const string support_chilid_parent_siblingd_GuiType = ",chrome,edge,firefox,web360se,web360x,superbrowser,zexplorer,chromium,";

        protected override eComponentType ComponentType => eComponentType.IsSimilarElementExist;

        protected override void ValidateParameter(ParamIsSimilarElementExistModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (paramModel.RelativeType == ElementRelativeType.Children)
                {
                    if (paramModel.UseAdvanced == (int)ElementMatchingMode.Normal && !string.IsNullOrEmpty(paramModel.Num.Trim()))
                    {
                        if (!int.TryParse(paramModel.Num.Trim(), out var num) || num <= 0 || paramModel.Num.IndexOf(".") >= 0)
                        {
                            throw new RPAException(eErrorMessageType.Error_GetSubElements_IndexValFail_Error);
                        }
                    }
                }
                if (paramModel.RelativeType == ElementRelativeType.Parent)
                {
                    if (paramModel.UseAdvanced == (int)ElementMatchingMode.Normal)
                    {
                        if (!string.IsNullOrEmpty(paramModel.Num.Trim()))
                        {
                            if (!int.TryParse(paramModel.Num.Trim(), out var num) || num <= 0 || paramModel.Num.IndexOf(".") >= 0)
                            {
                                throw new RPAException(eErrorMessageType.Error_GetParentElements_LevelValFail_Error);
                            }
                        }
                        else
                        {
                            throw new RPAException(eErrorMessageType.Error_GetParentElements_LevelValFail_Error);
                        }
                    }
                }
                return;
            }
            catch (Exception ex)
            {
                NLogger.Default.Info($"IsSimilarElementExist {paramModel.RelativeType.ToString()} 参数格式不符合要求：{paramModel.ToJson()}");
                throw ex;
            }
        }

        protected override void NormalizeParameter(ParamIsSimilarElementExistModel paramModel)
        {
            if (paramModel.UseAdvanced == (int)ElementMatchingMode.Advanced && !string.IsNullOrEmpty(paramModel.Filter))
            {
                var filterHelper = new FilterHelper(paramModel.Filter);
                paramModel.Filter = filterHelper.ToFilterNodesString();
            }
        }

        protected override async Task<JMessage> ExecuteAsync(ParamIsSimilarElementExistModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            if ((paramModel.RelativeType == ElementRelativeType.Children ||
                paramModel.RelativeType == ElementRelativeType.Parent ||
                paramModel.RelativeType == ElementRelativeType.Siblings) &&
                support_chilid_parent_siblingd_GuiType.IndexOf($",{paramModel.SelectorType.ToLower()},") < 0)
            {
                Exception(eErrorMessageType.Error_NotImplemented, ref msg);
                return msg;
            }
            msg.Info = "0";
            Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId },
                        {"useAdvanced",paramModel.UseAdvanced },
                        {"resultAttrName","" },
                        {"filter",paramModel.UseAdvanced ==0?string.Empty: paramModel.Filter },//普通模式 忽略 filter的使用
                        {"relativeType",paramModel.RelativeType },
                        {"positionNum",paramModel.UseAdvanced ==0?paramModel.Num:null }
                    };

            {
                string rectangles = string.Empty;
                ElementMatchType matchType = ElementMatchType.Normal;
                List<Rectangle> rects = new List<Rectangle>();
                int hiddenElementCount = 0;
                try
                {
                    paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
                    var param = new GetElementFromSelectorParams
                    {
                        Selector = paramModel.Selector,
                        SelectorType = paramModel.SelectorType,
                        FindOnce = true,
                        CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
                        isActive = paramModel.isHighlighting,
                        ExtendParam = hashtable,
                        WindowInfo = paramModel.WindowInfo,
                        RunParam = paramModel.RunParam,
                        VerifyMode = paramModel.VerifyMode
                    };
                    if (GuiElement.selectorType_Web.Contains(paramModel.SelectorType.ToLower())
                       || GuiElement.selectorType_Win.Contains(paramModel.SelectorType.ToLower()))
                    {
                        var elementAll = GuiElement.QuerySelectorAll(param);
                        matchType = param.MatchType;
                        if (elementAll.Count == 0)
                            throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementSelectorAll);
                        rects = elementAll.FirstOrDefault().GetRegionAll(elementAll, 3);
                        hiddenElementCount = elementAll.Count(a => (a.BoundingRectangle.Width <= 0 && a.BoundingRectangle.Height <= 0) || !a.IsShow());
                    }
                    else
                    {
                        hashtable["resultAttrName"] = "bounding";
                        rectangles = GuiElement.FromSelectorAll(param, out matchType);
                        if (string.IsNullOrEmpty(rectangles) || rectangles.Equals("[]"))
                            Exception(null, eErrorMessageType.Error_NotFoundElement, ref msg);
                        rects = rectangles.ToObject<List<Rectangle>>(); //获取范围出现异常 会造成 {"code":0,"message":"[400012074][元素判断]元素判断失败","info":"1"} 
                        hiddenElementCount = rects.Count(a => a.Width <= 0 && a.Height <= 0);
                    }
                }
                catch (Exception e)
                {
                    Exception(e, eErrorMessageType.Error_ElementDeterminationFailed, ref msg);
                }
                if (paramModel.isHighlighting && rects != null && rects.Count > 0)
                {
                    GlobleConfigurations.IsAiRepaireOfVerify = matchType == ElementMatchType.AiRepair;
                    if (await guiHighlightService.HighlightAsync(
                        rects,
                        paramModel.SelectorType.Equals(eGuiType.SAP.ToString(), StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false))
                    {
                        var message = matchType == ElementMatchType.AiRepair ? eCommonMessageType.IsSimilarElementExist_cs_Airepair.GetMessage() : eCommonMessageType.IsSimilarElementExist_cs_Normal.GetMessage();
                        msg.Info = "1";
                        msg.Extend = new JObject() {
                                { "ElementCount", rects.Count },
                                { "HiddenElementCount", hiddenElementCount },
                                { "Message", message },
                                { "IsAiRepair", (matchType == ElementMatchType.AiRepair)?1:0 }
                            }.ToString();
                    }
                    else
                        Exception(null, eErrorMessageType.Error_ElementValidationFailed, ref msg);
                }
            }

            msg.Code = 0;
            msg.UnifyCode = "0";

            return msg;

        }

        protected override string HandlingErrorMessage(JMessage resultMsg)
        {
            //GuiElement.ChromeRelease();
            return base.HandlingErrorMessage(resultMsg);
        }
    }
}
