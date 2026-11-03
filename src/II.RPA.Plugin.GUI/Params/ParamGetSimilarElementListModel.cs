using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using Newtonsoft.Json;
using System;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetSimilarElementListModel : ParamBase
    {
        public ParamGetSimilarElementListModel Clone()
        {
            return new ParamGetSimilarElementListModel
            {
                Selector = Selector,
                processId = processId,
                Filter = Filter,
                ComponentLabel = ComponentLabel,
                BrowserType = BrowserType,
                RunUniqueID = RunUniqueID,
                CodeVersion = CodeVersion,
                MatchType = MatchType,
                RunParam = RunParam,
                WindowInfo = WindowInfo,
                SelectorVersion = SelectorVersion,
                SelectorGuiType = SelectorGuiType,

                isAssociationParent = isAssociationParent,
                parentSelector = parentSelector,
                attributeName = attributeName,
                isCompensationLink = isCompensationLink,
                isAntitone = isAntitone,
                startIndex = startIndex,
                endIndex = endIndex,
                apiVersion = apiVersion
            };
        }
        public string cvExecutionServiceURL { get; set; }
        public bool b_isAssociationParent
        {
            get
            {
                return isAssociationParent == 1;
            }
        }
        /// <summary>
        /// 是否关联父元素
        ///  0 否 1 是
        /// </summary>
        public int isAssociationParent { get; set; }
        /// <summary>
        /// 父元素
        /// </summary>
        public string parentSelector { get; set; }
        /// <summary>
        /// 属性名称
        /// 获取元素对象 attributeName: selector 
        /// 获取元素坐标，返回元素范围  attributeName: bounding
        /// 获取文本内容，返回元素text 属性值 attributeName: text
        /// 获取元素值，返回元素value 属性值 attributeName: value
        /// 获取元素链接地址，网页元素，返回 herf或src 的值，win32 元素 无此属性，返回null attributeName: value
        /// 获取元素代码，网页元素，元素标签代码，win32 元素 无此属性，返回null
        /// </summary>
        public string attributeName { get; set; }

        public bool b_isCompensationLink
        {
            get
            {
                return isCompensationLink == 1;
            }
        }
        /// <summary>
        /// 是否补偿herf 或 src
        /// 0 否 1 是
        /// </summary>
        public int isCompensationLink { get; set; }
        public bool b_isAntitone
        {
            get
            {
                return isAntitone == 1;
            }
        }
        /// <summary>
        /// 是否反序
        /// 0 否 1 是
        /// </summary>
        public int isAntitone { get; set; }
        public int startIndex { get; set; } = 1;
        public int endIndex { get; set; } = -1;

        public string apiVersion { get; set; }
    }
}
