using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;

namespace II.RPA.Plugin.Window
{
    public abstract class WindowComponentBase<TParam> : II.RPA.Plugin.Core.PluginBase.PluginAbilityBase<TParam> where TParam : ParamBase
    {
        protected override void ValidateParameter(TParam paramModel)
        {
        }

        protected override void NormalizeParameter(TParam paramModel)
        {
            if (paramModel.Keywords == null && !string.IsNullOrEmpty(paramModel.Title))
            {
                paramModel.Keywords = new[] { paramModel.Title };
                paramModel.MatchType = GetWindowDataList_MatchType.Equals;
            }
        }
    }
}
