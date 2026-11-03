//using II.RPA.Plugin.Core;
//using II.RPA.Plugin.Core.GuiBase;
//using II.RPA.Plugin.CV.Params;
//using II.RPA.WinAutomation.CommonUtility;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace II.RPA.Plugin.CV.Plugins
//{
//    public class GetCVText : ComponentBase<ParamGetCVTextModel>
//    {
//        protected override eComponentType ComponentType => eComponentType.GetCVText;

//        protected override bool ParamVerification(string jsonParam, ref ParamGetCVTextModel paramModel)
//        {
//            try
//            {
//                paramModel = jsonParam.ToObject<ParamGetCVTextModel>();
//                if (string.IsNullOrEmpty(paramModel.selector))
//                {
//                    return false;
//                }
//                if (string.IsNullOrEmpty(paramModel.cvserver))
//                {
//                    return false;
//                }
//                return true;
//            }
//            catch (Exception ex)
//            {
//                NLogger.Default.Info($"GetCVText 参数格式不符合要求：{jsonParam}");
//                return false;
//            }
//        }

//        protected override JMessage Excute(ParamGetCVTextModel paramModel)
//        {
//            JMessage msg = new JMessage();
//            string selectorType;
//            string selector = paramModel.selector;
//            IGuiCVOperator cvOperator = null;
//            try
//            {
//                cvOperator = InstanceManager.GetAbility<IGuiCVOperator>("GUICV.GuiOperator", false);
//                if (cvOperator == null)
//                {
//                    msg.Code = 1;
//                    msg.Message = "未匹配到支撑程序";
//                    return msg;
//                }

//                IGuiNode guiNode = cvOperator.GetElementFromSelector(new GetElementFromSelectorParams
//                {
//                    Selector = selector,
//                    SelectorType = "cv",
//                    CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
//                    FindOnce = true
//                });
//                if (guiNode == null)
//                {
//                    msg.Code = 1;
//                    msg.Message = "未匹配到元素";
//                    return msg;
//                }
//                string result = ((IGuiCVNode)guiNode).GetText(paramModel.cvserver);
//                msg.Info = result;
//            }
//            catch (Exception e)
//            {
//                msg.Code = 1;
//                msg.Message = "获取文本异常";
//            }
//            return msg;

//        }

//        protected override string Config(string jsonParam)
//        {
//            throw new NotImplementedException();
//        }

//    }
//}
