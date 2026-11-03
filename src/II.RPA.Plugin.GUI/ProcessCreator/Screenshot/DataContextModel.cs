using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Plugin.GUI.ProcessCreator.ActionProvider;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace II.RPA.Plugin.GUI.ProcessCreator.Screenshot
{
    public class DataContextModel : BindingModelBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        private readonly List<ExecuteAction> m_CurrentResolvedExecuteActions;
        private readonly IEnumerable<ActionModel> m_CurrentResolvedActions;
        private readonly string m_PackagePath = ActionDataModels.PackagePath;
        public DataContextModel()
        {
            _Current = this;
            m_CurrentResolvedExecuteActions = new List<ExecuteAction>();
            ActionCommand = new DelegateCommand(ActionCommandExe);
            if (!CaptureWindow.Current.IsOnceTime)
            {
                ActionCollection = new ObservableCollection<ActionModel>();
                string actionKey = RPAEnvironment.GetInstance().CurrentLanguage == LanguageCultureEnum.zh_CN ? 
                    ActionDataModels.SCREENSHOT_ACTION_KEY : 
                    ActionDataModels.SCREENSHOT_ACTION_KEY_I18N;
                if (LocalDataModels.SpecialElementActions.TryGetValue(actionKey, out List<ElementAction> elementActions))
                {
                    var actions = ActionDataModels.ConvertToActionModel(elementActions.First().Actions);
                    actions.ToList().ForEach(a =>
                    {
                        ActionCollection.Add(a);
                    });
                    m_CurrentResolvedExecuteActions.AddRange(elementActions.First().Actions);
                    m_CurrentResolvedActions = ActionCollection.ToList();
                    foreach (var categoryItem in elementActions.First().Categories)
                    {
                        ActionModel actionModel = new ActionModel
                        {
                            ActionIcon = categoryItem.CategoryIcon,
                            DataItemType = DataItemType.MORE_ACTION,
                            ActionName = categoryItem.CategoryName,
                            ActionId = categoryItem.CategoryName,
                            DataTemplateKey = categoryItem.ActionTemplate,
                            ActionAlias = categoryItem.CategoryName,
                            ActionModels = ActionDataModels.ConvertToActionModel(categoryItem.CategoryActions, DataItemType.SUB_ACTION).ToList()
                        };
                        ActionCollection.Add(actionModel);
                        m_CurrentResolvedExecuteActions.AddRange(categoryItem.CategoryActions);
                        m_CurrentResolvedActions = m_CurrentResolvedActions.Concat(actionModel.ActionModels);
                    }

                    logger.LogInformation($"ActionLogos:{string.Join(";", m_CurrentResolvedActions.Select(a=>a.ActionLogo))}");
                    logger.LogInformation($"ActionIds:{string.Join(";", m_CurrentResolvedActions.Select(a => a.ActionId))}");
                }
            }
            //ActionCollection.Add(new ActionModel 
            //{
            //    ActionId = "cancel",
            //    ActionIcon = "\ue7d4",
            //    ActionTip = "取消"
            //});
            //ActionCollection.Add(new ActionModel
            //{
            //    ActionId = "save",
            //    ActionIcon = "\ue7d3",
            //    ActionTip = "保存到元素库",
            //    ActionAlias = "保存"
            //});
        }
        public ICommand ActionCommand
        {
            get;
            set;
        }
        #region 属性 Current
        private static DataContextModel _Current = null;
        public static DataContextModel Current
        {
            get
            {
                return _Current;
            }
        }
        #endregion

        #region 属性 MaskLeftWidth
        private double _MaskLeftWidth = CaptureWindow.ScreenWidth;
        public double MaskLeftWidth
        {
            get
            {
                return _MaskLeftWidth;
            }
            set
            {
                _MaskLeftWidth = value;
                ShowSizeLeft = value;
                RaisePropertyChanged(() => MaskLeftWidth);
            }
        }
        #endregion

        #region 属性 MaskRightWidth
        private double _MaskRightWidth = 0;
        public double MaskRightWidth
        {
            get
            {
                return _MaskRightWidth;
            }
            set
            {
                _MaskRightWidth = value;
                RaisePropertyChanged(() => MaskRightWidth);
            }
        }
        #endregion

        #region 属性 MaskTopWidth
        private double _MaskTopWidth = 0;
        public double MaskTopWidth
        {
            get
            {
                return _MaskTopWidth;
            }
            set
            {
                _MaskTopWidth = value;
                RaisePropertyChanged(() => MaskTopWidth);
            }
        }
        #endregion

        #region 属性 MaskTopHeight
        private double _MaskTopHeight = 0;
        public double MaskTopHeight
        {
            get
            {
                return _MaskTopHeight;
            }
            set
            {
                _MaskTopHeight = value;
                ShowSizeTop = MaskTopHeight < 40 ? MaskTopHeight + 10 : MaskTopHeight - 40;
                RaisePropertyChanged(() => MaskTopHeight);
            }
        }
        #endregion

        #region 属性 MaskBottomHeight
        private double _MaskBottomHeight = 0;
        public double MaskBottomHeight
        {
            get
            {
                return _MaskBottomHeight;
            }
            set
            {
                _MaskBottomHeight = value;
                RaisePropertyChanged(() => MaskBottomHeight);
            }
        }
        #endregion

        #region 属性 ShowSize
        private string _ShowSize = "0 * 0";
        public string ShowSize
        {
            get
            {
                return _ShowSize;
            }
            set
            {
                _ShowSize = value;
                RaisePropertyChanged(() => ShowSize);
            }
        }

        private static StringBuilder sb = new StringBuilder();
        public void ChangeShowSize()
        {
            sb.Clear();
            sb.Append((int)(CaptureWindow.Current.MainImage.Width * CaptureWindow.ScreenScale));
            sb.Append(" * ");
            sb.Append((int)(CaptureWindow.Current.MainImage.Height * CaptureWindow.ScreenScale));
            ShowSize = sb.ToString();
        }
        #endregion

        #region 属性 ShowSizeLeft
        private double _ShowSizeLeft = 0;
        public double ShowSizeLeft
        {
            get
            {
                return _ShowSizeLeft;
            }
            set
            {
                _ShowSizeLeft = value;
                RaisePropertyChanged(() => ShowSizeLeft);
            }
        }
        #endregion

        #region 属性 ShowSizeTop
        private double _ShowSizeTop = 0;
        public double ShowSizeTop
        {
            get
            {
                return _ShowSizeTop;
            }
            set
            {
                _ShowSizeTop = value;
                RaisePropertyChanged(() => ShowSizeTop);
            }
        }
        #endregion

        #region 属性 ShowRGB
        private string _ShowRGB = string.Empty;
        public string ShowRGB
        {
            get
            {
                return _ShowRGB;
            }
            set
            {
                _ShowRGB = value;
                RaisePropertyChanged(() => ShowRGB);
            }
        }
        #endregion
        #region 属性 LocationBorderLeft
        private double _LocationBorderLeft = 0;
        public double LocationBorderLeft
        {
            get
            {
                return _LocationBorderLeft;
            }
            set
            {
                _LocationBorderLeft = value;
                RaisePropertyChanged(() => LocationBorderLeft);
            }
        }
        #endregion
        #region 属性 LocationBorderTop
        private double _LocationBorderTop = 0;
        public double LocationBorderTop
        {
            get
            {
                return _LocationBorderTop;
            }
            set
            {
                _LocationBorderTop = value;
                RaisePropertyChanged(() => LocationBorderTop);
            }
        }
        #endregion
        #region 属性 MousePointX
        private double _MousePointX = 0;
        public double MousePointX
        {
            get
            {
                return _MousePointX;
            }
            set
            {
                _MousePointX = value;
                RaisePropertyChanged(() => MousePointX);
            }
        }
        #endregion
        #region 属性 MousePointY
        private double _MousePointY = 0;
        public double MousePointY
        {
            get
            {
                return _MousePointY;
            }
            set
            {
                _MousePointY = value;
                RaisePropertyChanged(() => MousePointY);
            }
        }
        #endregion

        private ObservableCollection<ActionModel> m_ActionCollection;
        public ObservableCollection<ActionModel> ActionCollection
        {
            get
            {
                return m_ActionCollection;
            }
            set
            {
                m_ActionCollection = value;
                RaisePropertyChanged("ActionCollection");
            }
        }

        private void ActionCommandExe(object obj)
        {
            try
            {
                if (obj is ActionModel actionModel)
                {
                    logger.LogInformation($"ActionCommandExe:{actionModel.ActionId}");
                    GenerateComponent(actionModel.ActionId);
                    CaptureWindow.Current.Close();
                }
                else if (obj is string actionparam)
                {
                    if ("save" == actionparam)
                    {
                        logger.LogInformation("保存截图");
                        SaveToElementLibrary();
                        CaptureWindow.Current.Close();
                    }
                    else if ("confirm" == actionparam)
                    {

                        string selector;
                        if (CaptureWindow.Current.ShotPolicy == ShotPolicy.SLIM)
                        {
                            CaptureWindow.Current.Close();
                            selector = CaptureSlim();
                            CaptureWindow.Current.SetSelector(selector);
                            logger.LogInformation("截图已完成");
                        }
                        else
                        {
                            CaptureWindow.Current.Close();
                            selector = ReCapture();
                            CaptureWindow.Current.SetSelector(selector);
                            logger.LogInformation("重新截图已完成");
                        }
                    }
                    else
                    {
                        logger.LogError(actionparam);
                    }
                }
                else
                {
                    logger.LogError($"{obj}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            finally
            {
                
            }
        }
        private void SaveToElementLibrary()
        {
            Task.Factory.StartNew(() => 
            {
                try
                {
                    var pictureRandomName = GetElementShotRandomName();
                    var picturePath = Path.Combine(m_PackagePath, "res", pictureRandomName);
                    var uiSelector = DoCapture(picturePath);

                    var elemementJO = new JObject
                    {
                        { "uiSelector", JToken.Parse(uiSelector) }
                    };
                    if (MessageService.PostMessage(TerminalType.CoreServer, "insertElement", elemementJO, (ret) =>
                    {
                        logger.LogDebug($"post return : {ret}");
                    }))
                    {
                        logger.LogDebug($"post done");
                    }
                    else
                    {
                        logger.LogError($"post fail");
                    }
                    CaptureWindow.Current.Dispatcher.Invoke(() => 
                    {
                        Image img = Image.FromFile(picturePath);
                        System.Windows.Forms.Clipboard.SetImage(img);
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
        }
        private string ReCapture()
        {
            try
            {
                var pictureRandomName = GetElementShotRandomName();
                var picturePath = Path.Combine(m_PackagePath, "res", pictureRandomName);
                var uiSelector = DoCapture(picturePath);
                return uiSelector;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return null;
            }
        }
        private string CaptureSlim()
        {
            try
            {
                var pictureRandomName = GetElementShotRandomName();
                var picturePath = Path.Combine(m_PackagePath, "res", pictureRandomName);
                var bitmapSource = ImageHelper.GetBitmapSource((int)Current.MaskLeftWidth + 1,
                (int)Current.MaskTopHeight + 1,
                (int)CaptureWindow.Current.MainImage.ActualWidth - 2,
                (int)CaptureWindow.Current.MainImage.ActualHeight - 2);
                if (bitmapSource != null)
                {
                    ImageHelper.SaveToPng(bitmapSource, picturePath);
                    logger.LogDebug("Image save done");
                    JObject slimSelectorJO = new JObject();
                    slimSelectorJO.Add("screen", 1);
                    slimSelectorJO.Add("path", Path.Combine("res", pictureRandomName));
                    JObject locationJO = new JObject();
                    locationJO.Add("x", (int)Current.MaskLeftWidth);
                    locationJO.Add("y", (int)Current.MaskTopHeight);
                    locationJO.Add("w", (int)CaptureWindow.Current.MainImage.ActualWidth);
                    locationJO.Add("h", (int)CaptureWindow.Current.MainImage.ActualHeight);
                    slimSelectorJO.Add("location", locationJO);
                    return slimSelectorJO.ToString();
                }
                else
                {
                    logger.LogError("ReCaptureSlim:截图失败");
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return null;
            }
        }
        protected void GenerateComponent(string aComponentId)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var pictureRandomName = GetElementShotRandomName();
                    var picturePath = Path.Combine(m_PackagePath, "res", pictureRandomName);
                    var uiSelector = DoCapture(picturePath);
                    JArray actions = new JArray();
                    logger.LogInformation($"GenerateComponent Id:{aComponentId}");

                    var actionJO = new JObject
                    {
                        { "type", aComponentId }
                    };
                    var propertynJO = new JObject();
                    var action = m_CurrentResolvedActions.FirstOrDefault(a=>a.ActionId == aComponentId);
                    if (action != null)
                    {
                        logger.LogInformation($"GenerateComponent Logo:{action.ActionLogo}");
                        ExecuteAction? currentAction = m_CurrentResolvedExecuteActions.FirstOrDefault(ca => ca.ActionId == action.ActionLogo 
                        || ca.ActionId == action.ActionId);
                        
                        if (currentAction.HasValue && currentAction.Value.ActionInput != null)
                        {
                            logger.LogDebug($"Find {aComponentId} in CurrentResolvedActions!");
                            foreach (string input in currentAction.Value.ActionInput)
                            {
                                //有默认值
                                if (input.Contains("="))
                                {
                                    string[] inputParams = input.Split('=');
                                    if (inputParams.Length == 2)
                                    {
                                        JObject valueJo = new JObject
                                        {
                                            { "value", inputParams[1] }
                                        };
                                        propertynJO.Add(inputParams[0], valueJo);
                                    }
                                }
                                else
                                {
                                    JObject valueJo = new JObject();
                                    switch (input)
                                    {
                                        case "args0.uiSelector":
                                            valueJo.Add("value", uiSelector);
                                            break;
                                        case "args0.condition":
                                        case "args0.image_path":
                                        case "args0.imagePath":
                                        case "args0.image":
                                            var _picturePath = Path.Combine("res", pictureRandomName);
                                            valueJo.Add("value", JsonConvert.ToString(_picturePath));
                                            break;
                                        default:
                                            break;
                                    }
                                    propertynJO.Add(input, valueJo);
                                }
                            }
                        }
                    }
                    actionJO.Add("properties", propertynJO);
                    var optionJO = new JObject
                        {
                            { "addFoldComponent", false },
                            { "sourceTracker", "IPAScreenShot" }
                        };
                    actionJO.Add("options", optionJO);
                    actions = new JArray { actionJO };
                    logger.LogDebug($"{actionJO.ToJson()}");
                    if (MessageService.PostMessage(TerminalType.CoreServer, "flowedit.plugingenerate", actions, (ret) =>
                    {
                        logger.LogDebug($"post return : {ret}");
                    }))
                    {
                        logger.LogDebug($"post done");
                    }
                    else
                    {
                        logger.LogError($"post fail");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
        }
        private string DoCapture(string aFilePath)
        {
            var bitmapSource = ImageHelper.GetBitmapSource((int)Current.MaskLeftWidth + 1,
                (int)Current.MaskTopHeight + 1,
                (int)CaptureWindow.Current.MainImage.ActualWidth - 2,
                (int)CaptureWindow.Current.MainImage.ActualHeight - 2);
            if (bitmapSource != null)
            {
                ImageHelper.SaveToPng(bitmapSource, aFilePath);
                logger.LogDebug("Image save done");
                return DoSelect((int)(Current.MaskLeftWidth * CaptureWindow.ScreenScale),
                    (int)(Current.MaskTopHeight * CaptureWindow.ScreenScale),
                    (int)(CaptureWindow.Current.MainImage.ActualWidth * CaptureWindow.ScreenScale),
                    (int)(CaptureWindow.Current.MainImage.ActualHeight * CaptureWindow.ScreenScale), aFilePath);
            }
            else
            {
                return null;
            }
        }


        private string DoSelect(int aX,int aY,int aWidth,int aHeight, string aFilePath)
        {
            JArray jarray = new JArray();
            JObject jobj = new JObject
            {
                { "name", "cv" }
            };
            Props x = new Props()
            {
                Name = "x",
                Value = $"{aX}",
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"

            };
            Props y = new Props()
            {
                Name = "y",
                Value = $"{aY}",
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"

            };
            Props Width = new Props()
            {
                Name = "width",
                Value = $"{aWidth}",
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"

            };
            Props Height = new Props()
            {
                Name = "height",
                Value = $"{aHeight}",
                Pattern = Pattern.EQUAL.GetPatternAttribute(),
                Accurate = "true"
            };
            JArray array = new JArray
            {
                JsonConvert.DeserializeObject(JsonConvert.SerializeObject(x)),
                JsonConvert.DeserializeObject(JsonConvert.SerializeObject(y)),
                JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Width)),
                JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Height))
            };
            jobj.Add("props", array);
            jarray.Add(jobj);
            var selectorString =  ExtractViewData.AddCommonProps(jarray.ToString(), eGuiType.CV, "", "OCR元素", filePath: aFilePath);
            if (SelectorFormat(selectorString, out string aSelectorFormated))
            {
                logger.LogDebug("SelectorFormat Done");
                return aSelectorFormated;
            }
            else
            {
                logger.LogError("SelectorFormat failed");
                return null;
            }
        }
        private bool SelectorFormat(string aSelectorOriginal, out string aSelectorFormated)
        {
            string _picturePath;
            aSelectorFormated = aSelectorOriginal;
            if (string.IsNullOrEmpty(aSelectorOriginal))
            {
                logger.LogError("aSelectorOriginal is null");
                return false;
            }
            try
            {
                JObject uiSelector = JToken.Parse(aSelectorOriginal) as JObject;
                if (uiSelector["commonProps"]["filePath"] != null)
                {
                    var picturePath = uiSelector["commonProps"]["filePath"].ToString();
                    _picturePath = Path.Combine("res", Path.GetFileName(picturePath));
                    uiSelector["commonProps"]["filePath"] = _picturePath;
                }
                //uiSelector["shot"] = RPicturePath;
                if (uiSelector["elementDetail"] != null)
                {
                    var icon_name = uiSelector["elementDetail"]["categoryIcon"];
                    try
                    {
                        if (icon_name != null && !string.IsNullOrEmpty(icon_name.ToString()))
                        {
                            uiSelector["elementDetail"]["categoryIcon"] = Path.Combine("res", icon_name.ToString());
                        }
                        else
                        {
                            uiSelector["elementDetail"]["categoryIcon"] = icon_name?.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex);
                        aSelectorFormated = string.Empty;
                        return false;
                    }
                }
                aSelectorFormated = uiSelector.ToString();
            }
            catch(Exception ex)
            {
                logger.LogError(ex);
            }
            return true;
        }
        private string GetElementShotRandomName()
        {
            return $"{new Random().Next(10000, 99999)}-{ToUnixTimeSeconds(new DateTimeOffset(DateTime.UtcNow))}.png".Trim().Replace(" ", string.Empty);
        }
        public long ToUnixTimeSeconds(DateTimeOffset dateTimeOffset)
        {
            long num = dateTimeOffset.UtcDateTime.Ticks / 10000000;
            return num - 62135596800L;
        }

    }

    public class BindingModelBase : System.ComponentModel.INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        public virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(name));
            }
        }

        protected virtual void RaisePropertyChanged<TProperty>(Expression<Func<TProperty>> property)
        {
            var lambda = (LambdaExpression)property;
            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else memberExpression = (MemberExpression)lambda.Body;
            RaisePropertyChanged(memberExpression.Member.Name);
        }
        #endregion
    }

    public enum Direction
    {
        Null = 0,
        Move = 1,
        LeftTop = 2,
        LeftMiddle = 3,
        LeftBottom = 4,
        MiddleTop = 5,
        MiddleBottom = 6,
        RightTop = 7,
        RightMiddle = 8,
        RightBottom = 9
    }
    public class Limit
    {
        public double Left { get; set; } = CaptureWindow.ScreenWidth;
        public double Right { get; set; } = 0;
        public double Top { get; set; } = CaptureWindow.ScreenHeight;
        public double Bottom { get; set; } = 0;

        public Limit Clone()
        {
            return MemberwiseClone() as Limit;
        }

        public Limit()
        {

        }
    }
    public class NameAndLimit
    {
        public string Name { get; set; }
        public Limit Limit { get; set; }

        public NameAndLimit(string name)
        {
            Name = name;
            Limit = CaptureWindow.Current.MainImage.Limit.Clone();
        }
    }
}
