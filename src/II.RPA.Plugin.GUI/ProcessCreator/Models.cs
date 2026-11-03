using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ProcessCreator.ActionProvider;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    public class ActionModel : ModelBase
    {
        public DataItemType DataItemType
        {
            get;
            set;
        }
        public string ActionIcon
        {
            get;
            set;
        } = "\ue631";
        public string ActionName
        {
            get;
            set;
        }
        public string ActionId
        {
            get;
            set;
        }
        public string ActionTip
        {
            get;
            set;
        }
        public string ActionAlias
        {
            get;
            set;
        }
        public bool IsDiss
        {
            get;
            set;
        }
        public string DataTemplateKey
        {
            get;
            set;
        }
        public List<ActionModel> ActionModels
        {
            get;
            set;
        }
        public ImageSource IconImageUri
        {
            get;
            set;
        }
        public Brush ActionIconBackground
        {
            get;
            set;
        }
        public string ActionLogo
        {
            get;
            set;
        }
        private InputVarViewModel actionDataContext;
        public InputVarViewModel ActionDataContext
        {
            get
            {
                return actionDataContext;
            }
            set
            {
                actionDataContext = value;
                RaisePropertyChanged("ActionDataContext");
            }
        }
    }
    public enum DataItemType
    {
        PREFER_ACTION = 0,
        MORE_ACTION = 1,
        SUB_ACTION = 2,
        ///...
        SEPARATOR = 99,
    }
    public class ComponentModel : ModelBase
    {
        [JsonProperty("type")]
        public string ComponentId
        {
            get;
            set;
        }
        [JsonProperty("label")]
        public string ComponentLabel
        {
            get;
            set;
        }
        [JsonProperty("category")]
        public List<string> ComponentCategories
        {
            get;
            set;
        }
        public string ComponentCategory
        {
            get => ComponentCategories?.First();
        }
        [JsonProperty("tag")]
        public JArray ComponentTags
        {
            get;
            set;
        }
        [JsonProperty("description")]
        public string ComponentDescription
        {
            get;
            set;
        }
        [JsonProperty("componentMode")]
        public List<string> ComponentMode
        {
            get;
            set;
        }
        [JsonProperty("definition")]
        public Definition Definition
        {
            get;
            set;
        }
        [JsonProperty("icon")]
        private string iconFontClass;
        public string IconFontClass
        {
            get => iconFontClass;
            set
            {
                iconFontClass = value;
            }
        }
        //public string IconFontCode
        //{
        //    get;
        //    set;
        //}
        public string IconUnicode
        {
            get => IconFontConverter.ConvertClassToUnicode(IconFontClass);
        }
        private BitmapImage iconImageSource;
        public BitmapImage IconImageSource
        {
            get => IconConvert(IconFontClass);
        }
        private TagList<string> m_SearchTags;
        public TagList<string> SearchTags
        {
            get
            {
                return m_SearchTags;
            }
            set
            {
                m_SearchTags = value;
                RaisePropertyChanged("SearchTags");
            }
        }
        [JsonProperty("releaseTag")]
        public int ReleaseTag
        {
            get;
            set;
        }
        [JsonProperty("logo")]
        public string ComponentLogo
        {
            get;
            set;
        }
        [JsonProperty("version")]
        public string ComponentVersion
        {
            get;
            set;
        }
        public ComponentAttachedModel ComponentAttached
        {
            get;
            set;
        }
        public BitmapImage IconConvert(string iconFontClass)
        {
            try
            {
                var bitmap = new BitmapImage(new Uri($"pack://application:,,,/II.RPA.Plugin.GUI;Component/ProcessCreator/Theme/Images/{iconFontClass}.png"));
                iconImageSource = bitmap;
            }
            catch
            {
                iconImageSource = new BitmapImage(new Uri($"pack://application:,,,/II.RPA.Plugin.GUI;Component/ProcessCreator/Theme/Images/icon-default.png"));
            }
            iconImageSource.Freeze();
            return iconImageSource;
        }

        public void GenerateSearchTags()
        {
            SearchTags = new TagList<string>();
            if (RPAEnvironment.GetInstance().CurrentLanguage == LanguageCultureEnum.zh_CN)
            {
                var baseTags = ComponentLabel.GetTotalPingYin().Concat(ComponentLabel.GetFirstPingYin());

                if (ComponentTags != null && ComponentTags.Any())
                {
                    var tags = ComponentTags.Select(t => t.Value<string>("label"));
                    baseTags = baseTags.Concat(tags);
                }
                else
                {
                    //baseTags = baseTags;
                }
                SearchTags.AddRange(baseTags);
                SearchTags.Add(ComponentLabel);
            }
            else
            {
                SearchTags.Clear();
                if (ComponentTags != null && ComponentTags.Any())
                {
                    SearchTags.AddRange(ComponentTags.Select(t => t.Value<string>("label")));
                }
                SearchTags.Add(ComponentLabel);
            }
        }
    }

    public struct ComponentAttachedModel
    {
        public System.Windows.Media.Brush ComponentBackgroundBrush
        {
            get;
            set;
        }
        public string ComponentAlias
        {
            get;
            set;
        }
    }
    public struct Definition
    {
        [JsonProperty("message0")]
        public string Message0
        {
            get;
            set;
        }
        [JsonProperty("args0")]
        public JArray Args0
        {
            get;
            set;
        }
        [JsonProperty("options0")]
        public JArray Options0
        {
            get;
            set;
        }
    }
    public class CategoryModel
    {
        [JsonProperty("name")]
        public string Name
        {
            get;
            set;
        }
        [JsonProperty("label")]
        public string Label
        {
            get;
            set;
        }
        [JsonProperty("icon")]
        public string Icon
        {
            get;
            set;
        }
        public string IconUnicode
        {
            get => IconFontConverter.ConvertClassToUnicode(Icon);
        }
        [JsonProperty("isTop")]
        public bool IsTop
        {
            get;
            set;
        }
        [JsonProperty("searchContent")]
        public CategorySearchContent SearchContent
        {
            get;
            set;
        }
        [JsonProperty("components")]
        public List<string> Components
        {
            get;
            set;
        }
        [JsonProperty("bgComponents")]
        public List<string> BgComponents
        {
            get;
            set;
        }
        [JsonProperty("mobileComponents")]
        public List<string> MobileComponents
        {
            get;
            set;
        }
        [JsonProperty("children")]
        public List<CategoryModel> Children
        {
            get;
            set;
        }
    }
    public struct CategorySearchContent
    {
        [JsonProperty("label")]
        public CategorySearchContentLabel Label
        {
            get;
            set;
        }
    }
    public struct CategorySearchContentLabel
    {
        [JsonProperty("character")]
        public string Character
        {
            get;
            set;
        }
        [JsonProperty("firstLetter")]
        public string FirstLetter
        {
            get;
            set;
        }
        [JsonProperty("allLetter")]
        public string[] AllLetter
        {
            get;
            set;
        }
    }
    public class TagList<T> : List<T>
    {
        public override string ToString()
        {
            return this.Last().ToString();
        }
    }
    public struct ActionParamModel
    {
        public System.Drawing.Point MouseLocation
        {
            get;
            set;
        }
        public eGuiType GuiType
        {
            get;
            set;
        }
        /// <summary>
        /// 元素缩略图的绝对路径
        /// </summary>
        public string PicturePath
        {
            get;
            set;
        }
        public string PictureRelativePath
        {
            get => Path.Combine("res", Path.GetFileName(PicturePath));
        }
        /// <summary>
        /// 元素缩略图的绝对路径 无高亮框
        /// </summary>
        public string PicturePath_WithOutHighLight
        {
            get;
            set;
        }
        public string ProcessName
        {
            get;
            set;
        }
        /// <summary>
        /// 转换前的元素tag内容
        /// </summary>
        public string ControlType
        {
            get;
            set;
        }
        public string Attributes
        {
            get;
            set;
        }
        /// <summary>
        /// 不断增加参数、干脆整个Node都拿过来
        /// </summary>
        public IGuiNode CurrentGuiNode
        {
            get;
            set;
        }
        public ElementType ElementType
        {
            get;
            set;
        }
        public string ElementTypeText
        {
            get;
            set;
        }
        public string ElementShotBase64
        {
            get;
            set;
        }
        public bool IsExplorer
        {
            get;
            set;
        }
        public AutomationElement UIAElement
        {
            get;
            set;
        }
        public AutomationElement UIARootElement
        {
            get;
            set;
        }

        public SelectElementModel ElementSelector
        {
            get;
            set;
        }
        public Rectangle RectangleRegion
        {
            get;
            set;
        }
    }
    public struct ElementAction
    {
        [JsonProperty("elementtype")]
        public string ElementType
        {
            get;
            set;
        }
        [JsonProperty("priority")]
        public int Priority
        {
            get;
            set;
        }
        /// <summary>
        /// 过于复杂、使用rules替代
        /// </summary>
        [JsonProperty("selectorrules")]
        public List<SelectorRule> SelectorRules
        {
            get;
            set;
        }
        [JsonProperty("rules")]
        public List<string> Rules
        {
            get;
            set;
        }
        /// <summary>
        /// class匹配、web元素类别判断用
        /// </summary>
        [JsonProperty("class")]
        public string[] MatchClasses
        {
            get;
            set;
        }
        /// <summary>
        /// type匹配、web元素类别判断用
        /// </summary>
        [JsonProperty("type")]
        public string MatchType
        {
            get;
            set;
        }
        [JsonProperty("actions")]
        public List<ExecuteAction> Actions
        {
            get;
            set;
        }
        [JsonProperty("category")]
        public List<Category> Categories
        {
            get;
            set;
        }
    }
    public struct Category
    {
        [JsonProperty("name")]
        public string CategoryName
        {
            get;
            set;
        }
        [JsonProperty("icon")]
        public string CategoryIcon
        {
            get;
            set;
        }
        [JsonProperty("actions")]
        public List<ExecuteAction> CategoryActions
        {
            get;
            set;
        }
        [JsonProperty("actiontemplate")]
        public string ActionTemplate
        {
            get;
            set;
        }
    }
    public struct SelectorRule
    {
        //"selectornode":"last",
        //"property": "name",
        //"rule":"EndWith",
        //"value":[".gif",".bmp",".jpeg",".jpg",".png",".tif",".svg"]
        [JsonProperty("selectornode")]
        public SelectorNodePattern SelectorNode
        {
            get;
            set;
        }
        [JsonProperty("selectornodeindex")]
        public int SelectorNodeIndex
        {
            get;
            set;
        }
        [JsonProperty("property")]
        public string NodeProperty
        {
            get;
            set;
        }
        [JsonProperty("rule")]
        public MatchRuleType MatchRule
        {
            get;
            set;
        }
        [JsonProperty("value")]
        public List<string> RuleValue
        {
            get;
            set;
        }
    }
    public struct ExecuteAction
    {
        [JsonProperty("action")]
        public string ActionId
        {
            get;
            set;
        }
        /// <summary>
        /// 从配置文件读取的参数信息
        /// </summary>
        [JsonProperty("input")]
        public List<string> ActionInput
        {
            get;
            set;
        }
        [JsonProperty("actionfeature")]
        public string ActionFeature
        {
            get;
            set;
        }
        [JsonProperty("actionflow")]
        public List<ExecuteAction> Actionflow
        {
            get;
            set;
        }
        [JsonProperty("actionalias")]
        public string ActionAlias
        {
            get;
            set;
        }
        [JsonProperty("actiontemplate")]
        public string ActionTemplate
        {
            get;
            set;
        }
        public bool ActionIsAI
        {
            get;
            set;
        }
    }
    public enum SelectorNodePattern
    {
        FIRST = 0,
        ALL = 1,
        ANY = 2,
        LAST = 3,
        SEPCIFIC = 9,
    }
    public enum MatchRuleType
    {
        EndWith,
    }
    public struct SelectElementModel
    {
        public bool IsValid
        {
            get;
            set;
        }
        public bool IsCvElement
        {
            get;
            set;
        }
        public string Selector
        {
            get;
            set;
        }
        public JObject CvElementAdditionJO
        {
            get;
            set;
        }
    }
    public struct LoginDataModel
    {
        [JsonProperty("selectors")]
        public LoginDataSelectorModel Selectors
        {
            get;
            set;
        }
    }
    public struct LoginDataSelectorModel
    {
        [JsonProperty("username_selector")]
        public JObject UserName
        {
            get;
            set;
        }
        [JsonProperty("password_selector")]
        public JObject Password
        {
            get;
            set;
        }
        [JsonProperty("loginbutton_selector")]
        public JObject LoginButton
        {
            get;
            set;
        }
        [JsonProperty("verifycode")]
        public JObject VerifyCode
        {
            get;
            set;
        }
        [JsonProperty("verifycodebackground")]
        public JObject VerifyCodeBackground
        {
            get;
            set;
        }
    }

    public struct Dpi
    {
        public double X
        {
            get; set;
        }
        public double Y
        {
            get; set;
        }
        public Dpi(double aX, double aY)
        {
            X = aX;
            Y = aY;
        }
        public override string ToString()
        {
            return $"[{X},{Y}]";
        }
    }
}

