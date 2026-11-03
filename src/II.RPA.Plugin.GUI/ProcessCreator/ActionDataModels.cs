using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shell32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    public static class ActionDataModels
    {
        #region
        public const string SCREENSHOT_ACTION_KEY = "screenshot";
        public const string SCREENSHOT_ACTION_KEY_I18N = "screenshot_i18n";
        #endregion
        #region Static Member
        private static readonly ILogger logger = Logger.OfContext();
        public static readonly Stopwatch s_Stopwatch = new Stopwatch();
        public static readonly LinearGradientBrush s_LinearGradientBrush_Orange =
            new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#FFB790"),
                (Color)ColorConverter.ConvertFromString("#FF5F2D"),
                new System.Windows.Point(0, 0),
                new System.Windows.Point(1, 1));

        //protected static readonly LinearGradientBrush s_LinearGradientBrush_Orange =
        //   new LinearGradientBrush(Colors.Red, Colors.Green, new Point(0, 0), new Point(1, 1));

        public static readonly LinearGradientBrush s_LinearGradientBrush_Blue =
            new LinearGradientBrush(ToColor("#55A6FF"), ToColor("#057CFE"), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));

        public static readonly LinearGradientBrush s_LinearGradientBrush_Purple =
            new LinearGradientBrush(ToColor("#6A8BFF"), ToColor("#505FFF "), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));

        public static readonly LinearGradientBrush s_LinearGradientBrush_Green =
            new LinearGradientBrush(ToColor("#3AF0C6"), ToColor("#0CD0A3"), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));

        public static readonly LinearGradientBrush s_LinearGradientBrush_Pink =
            new LinearGradientBrush(ToColor("#F880FD"), ToColor("#C152F6"), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));

        public static readonly LinearGradientBrush s_LinearGradientBrush_Yellow =
            new LinearGradientBrush(ToColor("#FFD165"), ToColor("#FD9300"), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));

        public static readonly LinearGradientBrush s_LinearGradientBrush_Default =
           new LinearGradientBrush(ToColor("#3AF0C6"), ToColor("#0CD0A3 "), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));

        public static readonly string[] I18NIgnoreLogoList = new string[] 
        {
            "DataService_ChatBot",
            "DataService_ZBrain",
            "OCR_CommonEnglishNumbersCode",
            "OCR_SliderVerificationCode",
            "OCR_SliderVerificationCodeV1",
            "OCR_SliderVerificationCodeV2",
            "OCR_SimpleSliderVerificationCode",
            "OCR_RotateVerificationCode",
            "OCR_ClickVerificationCode",
            "OCR_PuzzleVerificationCode",
            "OCR_MergeGeneralOCR",
            "OCR_MergeCardOCR",
            "OCR_MergePaperOCR",
            "OCR_AmountIdentification",
            "OCR_KeyValueIdentification",
            "OCR_MergeCarOCR",
            "OCR_MergeEducationOCR",
            "OCR_MergeOtherOCR",
            "OcrOffline_MoveToOcrText",
            "OcrOffline_ClickOcrText",
            "OcrOffline_GetOcrTextPosition",
            "OcrOffline_GetOcrTable",
            "NLP_SplitText",
            "NLP_EmotionalAnalysis",
            "NLP_PerspectiveAnalysis",
            "NLP_SemanticSimilarity",
            "NLP_KeyWordsAnalysis",
            "NLP_SplitWords",
            "NLP_AnalyseProductReview",
            "NLP_BatchAnalyseProductReview",
            "NLP_XunFei_MachineTranslation",
            "NLP_XunFei_NLPBaseServer",
            "NLP_XunFei_FeelingAnalysis",
            "NLP_XunFei_KeywordExtraction",
            "DataService_Review",
            "DataService_Diff",
            "DataService_Correct",
            "DataService_KeyWord"
        };
        public static readonly string[] BlackActionLogos = new string[]
        {
            "Dialog_Notice"
        };
        /// <summary>
        /// 静态构造
        /// </summary>
        static ActionDataModels()
        {
            Actions = new List<ActionModel>();
            RecommendActionCategories = new List<ActionModel>();
            DefaultRecommendActions = new List<ActionModel>();
            RecommendLogicActions = new List<ActionModel>();
            SearchSuggestionItems = new List<ComponentModel>();
            ElementComponents = new Dictionary<string, string>();

            s_LinearGradientBrush_Orange.Freeze();
            s_LinearGradientBrush_Blue.Freeze();
            s_LinearGradientBrush_Purple.Freeze();
            s_LinearGradientBrush_Green.Freeze();
            s_LinearGradientBrush_Pink.Freeze();
            s_LinearGradientBrush_Yellow.Freeze();
            s_LinearGradientBrush_Default.Freeze();
            try
            {
                #region handle category json 
                using (var fileStream = System.IO.File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "category-origin.json"), FileMode.Open))
                {
                    using (var sw = new StreamReader(fileStream))
                    {
                        using (var jw = new JsonTextReader(sw))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            Categories = serializer.Deserialize<List<CategoryModel>>(jw);
                        }
                    }
                }
                #endregion

            }
            catch (Exception ex)
            {
                logger.LogError("ActionViewModelBase static init error", ex);
            }
        }
        /// <summary>
        /// 组件分类推荐 暂时hardcode
        /// </summary>
        public static readonly string[] RecommendCategoryNames = new string[]
        {
            "interface_element",
            "process_control",
            "base_command",
            "ai_software_automation"
        };
        /// <summary>
        /// 逻辑组件推荐
        /// </summary>
        public static readonly string[] RecommendLogicActionNames = new string[]
        {
            "Element_WaitElement",
            "logic_control_if_element",
            "Element_Shot_V1",
        };
        /// <summary>
        /// 默认推荐动作名称
        /// </summary>
        public static readonly string[] DefaultRecommendComponentNames = new string[]
        {
            "Element_Click",
            "Element_GetValue",
            "logic_control_if_element"
        };
        /// <summary>
        /// 常驻推荐
        /// </summary>
        public static readonly string[] SearchSuggestionNames = new string[]
        {
            "logic_control_var",
            "Log_Info",
            "logic_control_count_for",
            "logic_control_try_catch"
        };
        public static readonly Dictionary<string, ComponentAttachedModel> ComponentAttachedModels = new Dictionary<string, ComponentAttachedModel>()
        {
            {
                "Element_Click",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_DianJiMuBiao.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Orange
                }
            },
            {
                "Mouse_ClickCombine",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_ShuBiaoDianJi.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "Mouse_DragTo",
                new ComponentAttachedModel
                {
                      ComponentAlias=eCommonMessageType.ActionDataModels_cs_ShuBiaoTuoZhuai.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Purple
                }
            },
            {
                "Keyboard_Press",
                new ComponentAttachedModel
                {
                      ComponentAlias=eCommonMessageType.ActionDataModels_cs_MoNiAnJian.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Green
                }
            },
            {
                "Element_SetValue",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_ShuRuNeiRong.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Purple
                }
            },
            {
                "Element_GetValue",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_HuoQuWenBen.GetMessage() ,
                      ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "Element_SetCheck",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_GouXuanKuangCaoZuo.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Purple
                }
            },
            {
                "Element_Children",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_HuoQuZiYuanSu.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "Element_SetSelect",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_XiaLaKuangCaoZuo.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Purple
                }
            },
            {
                "Window_ActiveWindow",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_JiHuoChuangKou.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Orange
                }
            },
            {
                "Mouse_MoveTo",
                new ComponentAttachedModel
                {
                      ComponentAlias=  eCommonMessageType.ActionDataModels_cs_YiDongShuBiao.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Purple
                }
            },{
                "Element_SetValuePassword",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_ShuRuMiMa.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Purple
                }
            },
            {
                "Element_Region",
                new ComponentAttachedModel
                {
                      ComponentAlias=  eCommonMessageType.ActionDataModels_cs_HuoQuZuoBiao.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "OCR_SourceMergeGeneralOCR",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_WenZiShiBie.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Green
                }
            },
            {
                "logic_control_if_element",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_PanDuanCunZai.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "loop_similar_elements",
                new ComponentAttachedModel
                {
                      ComponentAlias=  eCommonMessageType.ActionDataModels_cs_XunHuanXiangSiYuanSu.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "App_Run",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_QiDongYingYong.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "Clipboard_SetText",
                new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_JianQieWenBen.GetMessage() ,
                      ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "WebBrowser_Create",
                new ComponentAttachedModel
                {
                      ComponentAlias=  eCommonMessageType.ActionDataModels_cs_DaKaiWangYe.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Orange
                }
            },
            {
                "Element_MouseTo",
                 new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_ShuBiaoYiDaoYuanSu.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Orange
                }
            },
            {
                "Element_WaitElement",
                 new ComponentAttachedModel
                {
                      ComponentAlias= eCommonMessageType.ActionDataModels_cs_DengDaiYuanSu.GetMessage(),
                      ComponentBackgroundBrush = s_LinearGradientBrush_Green
                }
            },
            {
                "OcrOffline_GetOcrTextPosition",
                new ComponentAttachedModel
                {
                    ComponentAlias=  eCommonMessageType.ActionDataModels_cs_TuXiangOCR.GetMessage(),
                    ComponentBackgroundBrush = s_LinearGradientBrush_Green
                }
            },
            {
                "Word_New",
                new ComponentAttachedModel
                {
                    ComponentAlias=  eCommonMessageType.ActionDataModels_cs_XinJianWord.GetMessage(),
                    ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "Excel_NewExcel",
                new ComponentAttachedModel
                {
                    ComponentAlias= eCommonMessageType.ActionDataModels_cs_XinJianExcel.GetMessage(),
                    ComponentBackgroundBrush = s_LinearGradientBrush_Green
                }
            },
            {
                "File_OpenFile",
                new ComponentAttachedModel
                {
                    ComponentAlias= eCommonMessageType.ActionDataModels_cs_DaKaiWenJianJia.GetMessage() ,
                    ComponentBackgroundBrush = s_LinearGradientBrush_Yellow
                }
            },
            {
                "logic_control_if_file_path",
                new ComponentAttachedModel
                {
                    ComponentAlias=  eCommonMessageType.ActionDataModels_cs_PanDuanWenJianJia.GetMessage(),
                    ComponentBackgroundBrush = s_LinearGradientBrush_Green
                }
            },
            {
                "Element_SetValuePassword_V1",
                new ComponentAttachedModel
                {
                    ComponentAlias= eCommonMessageType.ActionDataModels_cs_ShuRuMiMa.GetMessage(),
                    ComponentBackgroundBrush =  s_LinearGradientBrush_Purple
                }
            },
            {
                "PDF_GetPdfText",
                new ComponentAttachedModel
                {
                    ComponentAlias=  eCommonMessageType.ActionDataModels_cs_HuoQupdfWenBen.GetMessage(),
                    ComponentBackgroundBrush =  s_LinearGradientBrush_Purple
                }
            },
            {
                "File_Copy",
                new ComponentAttachedModel
                {
                    ComponentAlias= eCommonMessageType.ActionDataModels_cs_FuZhiWenJian.GetMessage(),
                    ComponentBackgroundBrush =  s_LinearGradientBrush_Blue
                }
            },
            {
                "Word_Open",
                new ComponentAttachedModel
                {
                    ComponentAlias= eCommonMessageType.ActionDataModels_cs_DaKaiWenDang.GetMessage(),
                    ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "Excel_Cs_OpenV3",
                new ComponentAttachedModel
                { 
                    ComponentAlias= eCommonMessageType.ActionDataModels_cs_DaKaiExcel.GetMessage(),
                    ComponentBackgroundBrush = s_LinearGradientBrush_Green
                }
            },
            {
                "Word_GetPath",
                new ComponentAttachedModel
                {
                    ComponentAlias= eCommonMessageType.ActionDataModels_cs_HuoQuLuJing.GetMessage(),
                    ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                LocalDataModels.Login_Comp_Type,
                 new ComponentAttachedModel
                {
                        ComponentAlias= eCommonMessageType.ActionDataModels_cs_ZhiNengDengLu.GetMessage(),
                        ComponentBackgroundBrush = s_LinearGradientBrush_Orange
                }
            },
            {
                "WebBrowser_BatchDataCrawl",
                 new ComponentAttachedModel
                {
                        ComponentAlias= eCommonMessageType.DataCaptureForm_cs_ShuJuCaiJi.GetMessage(),
                        ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            },
            {
                "Element_ScrollIntoView",
                 new ComponentAttachedModel
                {
                        ComponentAlias= eCommonMessageType.ActionDataModels_cs_GunDongYeMian.GetMessage(),
                        ComponentBackgroundBrush = s_LinearGradientBrush_Purple
                }
            },
            {
                "excel.record",
                 new ComponentAttachedModel
                {
                        ComponentAlias= eCommonMessageType.ActionDataModels_cs_ShuJuChuLi.GetMessage() ,
                        ComponentBackgroundBrush = s_LinearGradientBrush_Green
                }
            }
            ,
            {
                "File_Rename",
                 new ComponentAttachedModel
                {
                        ///国际化todo
                        ComponentAlias= "重命名文件".I18n("ActionDataModels_64DFF8F7") ,
                        ComponentBackgroundBrush = s_LinearGradientBrush_Purple
                }
            }
            ,
            {
                "File_MoveFolder",
                 new ComponentAttachedModel
                {
                        ///国际化todo
                        ComponentAlias= "移动文件夹".I18n("ActionDataModels_B4D8A340") ,
                        ComponentBackgroundBrush = s_LinearGradientBrush_Yellow
                }
            }
            ,
            {
                "logic_control_count_for",
                 new ComponentAttachedModel
                {       
                     ComponentAlias= "计次循环".I18n("ActionDataModels_BD091EAE") ,
                     ComponentBackgroundBrush = s_LinearGradientBrush_Blue
                }
            }
             ,
            {
                "Element_GetAttribute",
                 new ComponentAttachedModel
                {
                     ComponentAlias= "元素属性".I18n("ActionDataModels_91F8F93D") ,
                     ComponentBackgroundBrush = s_LinearGradientBrush_Yellow
                }
            }
        };
        private static List<ComponentModel> m_Components;
        /// <summary>
        /// 原始组件数据
        /// </summary>
        public static List<ComponentModel> Components
        {
            get
            {
                if (m_Components == null || !m_Components.Any())
                {
                    logger.LogError($"Components get:empty");
                }
                return m_Components;
            }
            set
            {
                if (value == null || value.Count <= 0)
                {
                    logger.LogError($"Components set:empty");
                }
                m_Components = value;
            }
        }
        /// <summary>
        /// 原始组件分类数据
        /// </summary>
        public static List<CategoryModel> Categories
        {
            get;
            set;
        }
        /// <summary>
        /// 推荐的组件分类数据、根据RecommendCategories生成
        /// </summary>
        public static List<ActionModel> RecommendActionCategories
        {
            get;
            set;
        }
        /// <summary>
        /// 默认动作推荐数据
        /// </summary>
        public static List<ActionModel> DefaultRecommendActions
        {
            get;
            set;
        }
        /// <summary>
        /// 默认逻辑动作推荐数据
        /// </summary>
        public static List<ActionModel> RecommendLogicActions
        {
            get;
            set;
        }
        public static List<ComponentModel> SearchSuggestionItems
        {
            get;
            set;
        }
        /// <summary>
        /// 根据Components和Categories转换出的带层级的动作数据,用于动作搜索数据源、动作推荐数据源
        /// </summary>
        public static List<ActionModel> Actions
        {
            get;
            set;
        }
        /// <summary>
        /// 当前流程包路径
        /// </summary>
        public static string PackagePath
        {
            get;
            set;
        }
        /// <summary>
        /// 当前流程包路径
        /// </summary>
        public static Dictionary<string, string> ElementComponents
        {
            get;
            set;
        }
        public static ActionPopupWindow CurrentActionWindow
        {
            get;
            set;
        }
        /// <summary>
        /// dummy method for init static member 
        /// </summary>
        public static void Init()
        {

        }
        public static Color ToColor(string colorName)
        {
            return (Color)ColorConverter.ConvertFromString(colorName);
        }
        /// <summary>
        /// 组件数据处理
        /// </summary>
        /// <param name="components"></param>
        public static void ComponentsDataHandle(List<ComponentModel> components)
        {
            Actions?.Clear();
            DefaultRecommendActions?.Clear();
            RecommendActionCategories?.Clear();
            ElementComponents?.Clear();
            SearchSuggestionItems?.Clear();
            RecommendLogicActions?.Clear();
            //前端传入的组件列表
            if (components != null && components.Any())
            {
                Components = components.Where(c => c.ReleaseTag != 131).ToList();
                logger.LogInformation($"Get components from client");
            }
            //前端传入为空时、使用本地文件
            else
            {
                Components = new List<ComponentModel>();
                string settingFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "component.json");
                using (var fileStream
                    =
                    File.Open(settingFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite))
                {
                    using (var sw = new StreamReader(fileStream))
                    {
                        using (var jw = new JsonTextReader(sw))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Populate(jw, Components);
                        }
                    }
                    Components = Components.Where(c => c.ReleaseTag != 131).ToList();
                }
                logger.LogInformation($"Get components from local");
            }
            logger.LogInformation($"Components counts:{Components.Count}");
            var loginComp = Components.FirstOrDefault(cp=>cp.ComponentId == LocalDataModels.Login_Comp_Type);
            //业务组件-智能登录
            if (loginComp != null)
            {
                logger.LogInformation($"Find loginComp:{LocalDataModels.Login_Comp_Type}");
                if (ComponentAttachedModels.TryGetValue(loginComp.ComponentId, out ComponentAttachedModel attachedModel_Login))
                {
                    loginComp.ComponentAttached = attachedModel_Login;
                    loginComp.ComponentLogo = LocalDataModels.Login_Comp_Type;
                }
            }
            else
            {
                logger.LogError($"Can not Find loginComp:{LocalDataModels.Login_Comp_Type}");
            }

            var excelComp = new ComponentModel
            {
                ComponentId = "excel.record",
                ComponentLogo = "excel.record",
                IconFontClass = "icon-shujuchuli",
                ComponentLabel = eCommonMessageType.ActionDataModels_cs_ShuJuChuLi.GetMessage() 
            };
            var excelCompIcon = excelComp.IconImageSource;
            logger.LogDebug($"excelCompIcon:{excelCompIcon?.UriSource?.AbsoluteUri}");
            Components.Add(excelComp);

            Components.ForEach(c =>
            {
                //生成搜索关键字
                c.GenerateSearchTags();
                //获取图标Unicode
                foreach (var category in Categories)
                {
                    //if (category.Name == c.ComponentCategory)
                    //{
                    //    c.IconFontCode = category.IconUnicode;
                    //    break;
                    //}
                    //else if (category.Children != null && category.Children.Any())
                    //{
                    //    var child = category.Children.FirstOrDefault(ctc => ctc.Name == c.ComponentCategory);
                    //    if (child != null)
                    //    {
                    //        c.IconFontCode = child.IconUnicode;
                    //        break;
                    //    }
                    //}
                }
                //if (RecommendLogicActionNames.Contains(c.ComponentId))
                //{
                //    ActionModel actionModel = new ActionModel
                //    {
                //        ActionIcon = c.IconUnicode,
                //        ActionName = c.ComponentLabel,
                //        ActionId = c.ComponentId,
                //        ActionTip = c.ComponentDescription,
                //        DataItemType = DataItemType.PREFER_ACTION
                //    };
                //    switch (c.ComponentId)
                //    {
                //        case "Element_WaitElement_v1":
                //            actionModel.ActionAlias = actionModel.ActionName;
                //            actionModel.DataTemplateKey = "ElementWait";
                //            break;
                //        case "logic_control_if_element_v1":
                //            actionModel.ActionAlias = "元素存在";
                //            actionModel.DataTemplateKey = "ElementIfExist";
                //            break;
                //        case "Element_Shot_V1":
                //            actionModel.ActionAlias = actionModel.ActionName;
                //            actionModel.DataTemplateKey = "ElementShot";
                //            break;
                //    }
                //    RecommendLogicActions.Add(actionModel);
                //}
                if (SearchSuggestionNames.Contains(c.ComponentId))
                {
                    SearchSuggestionItems.Add(c);
                }
                
                //元素类组件
                if (c.Definition.Args0 != null && c.Definition.Args0.Any())
                {
                    var arg = c.Definition.Args0.FirstOrDefault(a => a["displayType"] != null && a["displayType"].ToString().Equals("comboSelector"));
                    if (arg != null)
                    {
                        if (!ElementComponents.ContainsKey(c.ComponentId))
                        {
                            ElementComponents.Add(c.ComponentId, $"args0.{arg["name"]}");
                        }
                        //NLogger.Default.Debug($"ElementComponents {c.ComponentId}");
                    }
                }

                if (!string.IsNullOrEmpty(c.ComponentLogo) && ComponentAttachedModels.TryGetValue(c.ComponentLogo, out ComponentAttachedModel attachedModel))
                {
                    c.ComponentAttached = attachedModel;
                }
                else if (ComponentAttachedModels.TryGetValue(c.ComponentId, out ComponentAttachedModel attachedModel_front))
                {
                    c.ComponentAttached = attachedModel_front;
                }
                else
                {
                    //logger.LogWarning($"default ComponentAttached! id:{c.ComponentId};logo:{c.ComponentLogo}");
                    c.ComponentAttached = new ComponentAttachedModel
                    {
                        ComponentAlias = c.ComponentLabel,
                        ComponentBackgroundBrush = s_LinearGradientBrush_Default
                    };
                }
                //生成默认推荐数据
                //if (DefaultRecommendComponentNames.Any(d => !string.IsNullOrEmpty(c.ComponentLogo) && d.Equals(c.ComponentLogo)))
                //{
                //    try
                //    {
                //        DefaultRecommendActions.Add(new ActionModel
                //        {
                //            //ActionIcon = c.IconFontCode,
                //            IconImageUri = c.IconImageSource.Clone(),
                //            ActionIcon = c.IconUnicode,
                //            ActionName = c.ComponentLabel,
                //            ActionId = c.ComponentId,
                //            ActionTip = c.ComponentDescription,
                //            DataItemType = DataItemType.PREFER_ACTION,
                //            ActionIconBackground = c.ComponentAttached.ComponentBackgroundBrush.Clone(),
                //        });
                //    }
                //    catch (Exception ex)
                //    {
                //        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex,$"id:{c.ComponentId};logo:{c.ComponentLogo}");
                //    }
                //}

            });
            //默认推荐排序
            DefaultRecommendActions =
                DefaultRecommendActions.OrderBy(a
                => Array.IndexOf(DefaultRecommendComponentNames, a.ActionId)).ToList();
            //组件分类与组件关系挂接
            foreach (var categroy in Categories)
            {
                var cps = Components.Where(cp
                    =>
                {
                    return cp.ComponentCategory == categroy.Name
                      && (cp.ComponentMode.Contains("background") || cp.ComponentMode.Contains("normal"));
                });
                ActionModel categoryModel = new ActionModel
                {
                    ActionIcon = categroy.IconUnicode,
                    ActionName = categroy.Label,
                    ActionId = categroy.Name,
                };
                if (cps != null && cps.Any())
                {
                    categoryModel.ActionModels = cps.Select(cp => new ActionModel
                    {
                        //ActionIcon = categroy.IconUnicode,
                        ActionIcon = cp.IconUnicode,
                        ActionId = cp.ComponentId,
                        ActionName = cp.ComponentLabel,
                        ActionTip = cp.ComponentDescription,
                    }).ToList();
                }
                if (categroy.Children != null && categroy.Children.Any())
                {
                    if (categoryModel.ActionModels == null)
                    {
                        categoryModel.ActionModels = new List<ActionModel>();
                    }
                    foreach (CategoryModel childcategory in categroy.Children)
                    {
                        var childcps = Components.Where(cp
                            =>
                        {
                            return cp.ComponentCategory == childcategory.Name
                            && (cp.ComponentMode.Contains("background") || cp.ComponentMode.Contains("normal"));
                        });
                        categoryModel.ActionModels.Add(new ActionModel
                        {
                            ActionIcon = childcategory.IconUnicode,
                            ActionId = childcategory.Name,
                            ActionName = childcategory.Label,
                            ActionModels = childcps.Select(cp => new ActionModel
                            {
                                //ActionIcon = childcategory.IconUnicode,
                                ActionIcon = cp.IconUnicode,
                                ActionId = cp.ComponentId,
                                ActionName = cp.ComponentLabel,
                            }).ToList(),
                        });
                    }
                }
                ///根据hardcode的category name 匹配、生成RecommendActionCategories
                if (RecommendCategoryNames.Contains(categroy.Name))
                {
                    int index = Array.IndexOf(RecommendCategoryNames, categroy.Name);
                    if (RecommendActionCategories.Count <= index)
                    {
                        RecommendActionCategories.Add(categoryModel);
                    }
                    else
                    {
                        RecommendActionCategories.Insert(index, categoryModel);
                    }
                    logger.LogInformation($"RecommendActionCategories:{RecommendActionCategories.Count}");
                }
                Actions.Add(categoryModel);
            }
        }

        public static void ComponentsDataHandle(string aCacheJsonFile)
        {
            logger.LogInformation($"ComponentsDataHandle.CacheJsonFile:{aCacheJsonFile}");

           var components = new List<ComponentModel>();
            try
            {
                if (File.Exists(aCacheJsonFile))
                {
                    using (var fileStream
                        =
                        File.Open(aCacheJsonFile,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite))
                    {
                        using (var sw = new StreamReader(fileStream))
                        {
                            using (var jw = new JsonTextReader(sw))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                serializer.Populate(jw, components);
                            }
                        }
                    }
                }
            }
            catch(Exception ex) 
            {
                logger.LogError(ex);
            }
            ComponentsDataHandle(components);
        }
        /// <summary>
        /// 此方法在获取programdata文件夹下快捷方式指向文件时、需要管理员权限
        /// </summary>
        /// <param name="shortcutFilename"></param>
        /// <returns></returns>
        private static string GetShortcutTargetFileWithShell(string shortcutFilename)
        {
            try
            {
                string pathOnly = System.IO.Path.GetDirectoryName(shortcutFilename);
                string filenameOnly = System.IO.Path.GetFileName(shortcutFilename);

                Shell shell = new Shell();
                Shell32.Folder folder = shell.NameSpace(pathOnly);
                FolderItem folderItem = folder.ParseName(filenameOnly);
                if (folderItem != null)
                {
                    Shell32.ShellLinkObject link = (Shell32.ShellLinkObject)folderItem.GetLink;
                    return link.Path;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetShortcutTargetFileWithShell");
            }
            return string.Empty;
        }
        //public static IEnumerable<ActionModel> ConvertToActionModel(List<ExecuteAction> anExecuteActions)
        //{
        //    return ConvertToActionModel(anExecuteActions, DataItemType.PREFER_ACTION);
        //}

        public static IEnumerable<ActionModel> ConvertToActionModel(IEnumerable<string> anActionIds)
        {
            if (Components == null || Components.Count <= 0)
            {
                logger.LogError("Components is empty!");
                return Enumerable.Empty<ActionModel>();
            }
            return anActionIds.Select(a =>
            {
                var component = Components.FirstOrDefault(c => c != null && c.ComponentId == a);
                if (component != null)
                {
                    return new ActionModel
                    {
                        ActionId = component.ComponentId,
                        ActionName = component.ComponentLabel,
                        DataItemType = DataItemType.PREFER_ACTION,
                        ActionIcon = component.IconUnicode,
                        ActionTip = component.ComponentDescription,
                        ActionAlias = component.ComponentAttached.ComponentAlias,
                        ActionIconBackground = component.ComponentAttached.ComponentBackgroundBrush.Clone(),
                        IconImageUri = component.IconImageSource.Clone(),
                        ActionLogo = component.ComponentLogo,
                    };
                }
                else
                {
                    logger.LogWarning($"mismatch component by type:{a}");
                    component = Components.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.ComponentLogo) && a.Equals(c.ComponentLogo));
                    if (component != null)
                    {
                        return new ActionModel
                        {
                            ActionId = component.ComponentId,
                            ActionName = component.ComponentLabel,
                            DataItemType = DataItemType.PREFER_ACTION,
                            ActionIcon = component.IconUnicode,
                            ActionTip = component.ComponentDescription,
                            ActionAlias = component.ComponentAttached.ComponentAlias,
                            ActionIconBackground = component.ComponentAttached.ComponentBackgroundBrush.Clone(),
                            IconImageUri = component.IconImageSource.Clone(),
                            ActionLogo = component.ComponentLogo,
                        };
                    }
                    else
                    {
                        logger.LogError($"mismatch component by logo:{a}");
                        if (component != null)
                        {
                            return new ActionModel
                            {
                                ActionId = component.ComponentId,
                                ActionName = component.ComponentLabel,
                                DataItemType = DataItemType.PREFER_ACTION,
                                ActionIcon = component.IconUnicode,
                                ActionTip = component.ComponentDescription,
                                ActionAlias = component.ComponentAttached.ComponentAlias,
                                ActionIconBackground = component.ComponentAttached.ComponentBackgroundBrush.Clone(),
                                IconImageUri = component.IconImageSource.Clone(),
                                ActionLogo = component.ComponentLogo,
                            };
                        }
                        return new ActionModel
                        {
                            ActionId = a,
                            ActionName = a,
                            DataItemType = DataItemType.PREFER_ACTION,
                            ActionTip = a,
                            ActionLogo = a
                        };
                    }
                }
            });
        }

        public static IEnumerable<ActionModel> ConvertToActionModel(
            List<ExecuteAction> anExecuteActions,
            DataItemType aDataItemType = DataItemType.PREFER_ACTION)
        {
            try
            {
                if (Components == null || Components.Count <= 0)
                {
                    logger.LogError("Components is empty!");
                    return Enumerable.Empty<ActionModel>();
                }
                List<ActionModel> actions = new List<ActionModel>();
                anExecuteActions.ForEach(a =>
                {
                    var component = Components.FirstOrDefault(c => c.ComponentId == a.ActionId);
                    if (component == null)
                    {
                        logger.LogWarning($"mismatch component by type:{a.ActionId}");
                        component = Components.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.ComponentLogo) && a.ActionId.Equals(c.ComponentLogo));
                        if (component == null)
                        {
                            logger.LogWarning($"mismatch component by logo:{a.ActionId}");
                            component = Components.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.ComponentLogo) && a.ActionId.StartsWith(c.ComponentLogo));
                            if (component == null)
                            {
                                logger.LogWarning($"mismatch component by logo startwith:{a.ActionId}");
                                if (a.ActionId.StartsWith("Element_GetText"))
                                {
                                    component = Components.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.ComponentLogo) && c.ComponentLogo == "Element_GetValue");
                                    if (component == null)
                                    {
                                        logger.LogError($"mismatch component by logo Element_GetValue:{a.ActionId}");
                                    }
                                }
                            }
                        }
                    }

                    if (component != null)
                    {
                        if (!actions.Any(action => action.ActionId == component.ComponentId))
                        {
                            actions.Add(new ActionModel
                            {
                                ActionId = component.ComponentId,
                                ActionName = component.ComponentLabel,
                                DataItemType = aDataItemType,
                                ActionIcon = component.IconUnicode,
                                ActionTip = component.ComponentDescription,
                                ActionAlias = component.ComponentAttached.ComponentAlias,
                                ActionIconBackground = component.ComponentAttached.ComponentBackgroundBrush.Clone(),
                                IconImageUri = component.IconImageSource.Clone(),
                                DataTemplateKey = a.ActionTemplate,
                                ActionLogo = component.ComponentLogo,
                            });
                        }
                        else
                        {
                            logger.LogWarning($"Duplicate action id:{component.ComponentId}");
                        }
                    }
                });
                return actions;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ConvertToActionModel Error");
                return Enumerable.Empty<ActionModel>();
            }
        }
        #endregion
    }
}

