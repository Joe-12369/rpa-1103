using FlaUI.Core.AutomationElements;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.DataCollection.Utils;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Plugin.GUI.DataCollection.Windows;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Utils;


namespace II.RPA.Plugin.GUI
{
    public class DataCollectingManager
    {
        #region 公用的字段、属性、委托
        private static readonly object _objLock_GetPointAndWndInfo = new object();
        public static DataCollectOverlayWnd dataCollectOverlayWnd = null;
        private static bool IsInitialize = false;
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// 拾取动作结束之后的回调委托，再生成selector之后、异常之后、取消之后等场景执行
        /// </summary>
        internal static Action<DataCollectOverlayWnd> StartElementCapture_OnSelectored = null;
        /// <summary>
        /// 回归数据采集看板窗口主线程的委托
        /// </summary>
        internal static Func<Func<object>, object> UIThreadRunAction = null;
        /// <summary>
        /// 浏览器插件异常提示
        /// </summary>
        internal static Dictionary<string, bool> WebBroswerExtensionPopupStatus = new Dictionary<string, bool>();

        internal static List<eGuiType> SupportBlockDataGuiTypes = new List<eGuiType>() { eGuiType.Chrome, eGuiType.Edge, eGuiType.FireFox, eGuiType.Windows };
        #endregion

        #region 开始、停止数据采集以及功能的交互效果

        internal static void InitDataCollectOverlayWnd()
        {
            dataCollectOverlayWnd = new DataCollectOverlayWnd();
            //dataCollectOverlayWnd.Callback = StartElementCapture_Callback;
            IsInitialize = true;
        }
        internal static void ReleaseDataCollectOverlayWnd()
        {
            try
            {
                IsInitialize = false;
                StopCapture();
                dataCollectOverlayWnd?.Close();
                dataCollectOverlayWnd = null;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"释放DataCollectOverlayWnd资源异常");
            }
            finally
            {
                dataCollectOverlayWnd = null;
            }
        }
        /// <summary>
        /// 开始数据采集功能的元素拾取效果（包含 数据采集元素拾取、普通元素拾取、相似元素拾取）
        /// </summary>
        /// <returns></returns>
        internal static RetElementCaptureModel StartElementCapture(ParamStartCollectElementCaptureModel paramModel)
        {
            RetElementCaptureModel retModel = new RetElementCaptureModel();
            //captureMessage = new JMessage();
            //DataCollectOverlayWnd dataCollectOverlayWnd = null;
            try
            {
                //if (!IsInitialize)
                //{
                //    InitDataCollectOverlayWnd();
                //}
                ShowOverlayWnd();
                dataCollectOverlayWnd.StartCapture(paramModel);
            }
            catch (COMException ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"元素拾取异常 Type:{paramModel.CaptureElementType}");
            }
            catch (Exception ex)
            {
                //if (dataCollectOverlayWnd?.Visibility == System.Windows.Visibility.Visible)
                //{
                //    dataCollectOverlayWnd.Hide();
                //}
                var rpaException = new RPAException(eErrorMessageType.Error_ElementCaptureError, eComponentType.None, ex);
                throw rpaException;
                //retModel.Code = rpaException.ErrorCode;
                //retModel.Message = rpaException.ErrorMessage;
                //Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref captureMessage);
            }
            return retModel;
        }
        /// <summary>
        /// 显示高亮蒙层窗口
        /// </summary>
        private static void ShowOverlayWnd()
        {
            if (!dataCollectOverlayWnd.CheckAccess())
            {
                dataCollectOverlayWnd.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    ShowOverlayWnd();
                });
                return;
            }
            //UI线程不可阻塞，使用回调方式完成返回值的处理操作
            if (dataCollectOverlayWnd.Visibility != System.Windows.Visibility.Visible)
            {
                dataCollectOverlayWnd.Show();
            }
        }
        internal static bool StopCapture()
        {
            dataCollectOverlayWnd?.StopCapture();
            return true;
        }
        internal static void ShowLoadWaiting(bool isShow)
        {
            GlobalData.GetInstance().VM_MainWindowViewModel.ShowLoadWaiting(isShow, "Manager");//.Invoke(isShow);
        }
        #endregion

        #region 数据采集主要功能实现 如：Factory编辑器、Engine文本编译、选择器转义、

        /// <summary>
        /// 执行元素编辑器
        /// </summary>
        /// <param name="paramModel"></param>
        /// <returns></returns>
        internal static List<SelectorProps> DoElementEditor(ParamDoElementEditorModel paramModel)
        {
            try
            {
                logger.LogDebug("DoElementEditor SendMessage:");
                logger.LogDebug(LogBlock.Object(paramModel));

                RPACommon common = new RPACommon();
                common.CommonProps.GUIType = paramModel.guiType;
                common.CommonProps.filePath = paramModel.filePath;
                common.Selector = JArray.Parse(paramModel.selector);
                paramModel.selector = common.ToJson();
                JMessage result = MessageService.SendMessage(TerminalType.Factory, "ShowSelectorEditor", paramModel.ToJson());

                logger.LogDebug("ShowSelectorEditor Result:");
                logger.LogDebug(LogBlock.Object(result));
                if (result.Code == 0)
                {
                    var commonResult = result.Info.ToObject<RPACommon>();
                    return commonResult.Selector.ToObject<List<SelectorProps>>();
                }
                else
                {
                    logger.LogDebug($"数据采集元素调用编辑编辑器,返回失败：{LogBlock.Object(result)}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "数据采集元素使用编辑编辑器异常");
                throw new RPAException(eErrorMessageType.Error_DataCollection_DoElementEditor, ex);
            }

            return null;
        }

        /// <summary>
        /// 执行文本编译
        /// </summary>
        /// <param name="paramModel"></param>
        /// <returns></returns>
        internal static RetDataCollectConfigurationModel DoStringCompile(ParamDoStringCompileModel paramModel)
        {
            try
            {
                logger.LogDebug("DoStringCompile SendMessage:");
                logger.LogDebug(LogBlock.Object(paramModel));
                JMessage result = MessageService.SendMessage(TerminalType.Engine, "CompileSelector", paramModel.ToJson());
                logger.LogDebug("DoStringCompile Result:");
                logger.LogDebug(LogBlock.Object(result));
                if (result.Code == 0)
                {
                    return result.Info.ToObject<RetDataCollectConfigurationModel>();
                }
                else
                {
                    logger.LogDebug($"数据采集元素调用执行文本编译,返回失败：{LogBlock.Object(result)}");
                    var rpaex = new RPAException(eErrorMessageType.Error_DataCollection_DoStringCompile);
                    MessageHelper.ShowWeakError(rpaex.ErrorMessageContent);
                    throw rpaex;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "数据采集元素使用执行文本编译异常");
                //测试时使用，正式情况要注释
                //return paramModel.Text.ToObject<RetDataCollectConfigurationModel>();
#if Trace
                logger.LogWarning("如果你看到了这一行代码，说明你的代码是debug版本，这不能确保在你的环境中正常使用，请联系文件提供者");
                try
                {
                    RetDataCollectConfigurationModel _DataCollectConfigurationModel = paramModel.Text.ToObject<RetDataCollectConfigurationModel>();
                    _DataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelector = SelectorValueRemoveStrEscape(new ParamSelectorValueRemoveStrEscapeModel
                    {
                        selector = _DataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelector
                    });
                    for (int i = 0; i < _DataCollectConfigurationModel.collectInfo.data.Count; i++)
                    {
                        _DataCollectConfigurationModel.collectInfo.data[i].selector = SelectorValueRemoveStrEscape(new ParamSelectorValueRemoveStrEscapeModel
                        {
                            selector = _DataCollectConfigurationModel.collectInfo.data[i].selector
                        });
                    }
                    return _DataCollectConfigurationModel;
                }
                catch (Exception ex2)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex2, $"调用Engien转义失败，尝试本地去转义失败.");
                }
#endif
                throw new RPAException(eErrorMessageType.Error_DataCollection_DoStringCompile, ex);

                //#if Trace

                //#endif
            }

            return null;
        }
        /// <summary>
        /// 对selector 中的value属性【增加】文本转义
        /// </summary>
        /// <param name="paramMopdel"></param>
        /// <returns></returns>
        internal static List<SelectorProps> SelectorValueAddStrEscape(ParamSelectorValueAddStrEscapeModel paramModel)
        {
            for (int i = 0; i < paramModel.selector.Count; i++)
            {
                for (int j = 0; j < paramModel.selector[i].Props.Count; j++)
                {
                    paramModel.selector[i].Props[j].TextValue = paramModel.selector[i].Props[j].Value;
                    paramModel.selector[i].Props[j].Value = paramModel.selector[i].Props[j].Value.ToJson();
                    paramModel.selector[i].Props[j].ExpressionValue = paramModel.selector[i].Props[j].Value;
                }
            }
            return paramModel.selector;
        }
        /// <summary>
        /// 对selector 中的value属性【去除】文本转义
        /// </summary>
        /// <param name="paramMopdel"></param>
        /// <returns></returns>
        internal static List<SelectorProps> SelectorValueRemoveStrEscape(ParamSelectorValueRemoveStrEscapeModel paramModel)
        {
            for (int i = 0; i < paramModel.selector.Count; i++)
            {
                for (int j = 0; j < paramModel.selector[i].Props.Count; j++)
                {
                    paramModel.selector[i].Props[j].Value = paramModel.selector[i].Props[j].Value.ToObject<string>();
                }
            }


            return paramModel.selector;
        }

        #endregion

        #region 根据选择采集数据 ，即 获取采集数据(运行、配置预览)
        /// <summary>
        /// 使用选择器采集数据
        /// </summary>
        /// <param name="paramMopdel"></param>
        /// <returns></returns>
        internal static List<List<string>> CollectDataBySelector(ParamCollectDataBySelectorModel paramMopdel, string selectorType = "")
        {
            List<string> dataFileList = new List<string>();
            return CollectDataBySelector(paramMopdel, ref dataFileList, selectorType);
        }
        /// <summary>
        /// 使用选择器采集数据
        /// </summary>
        /// <param name="paramMopdel"></param>
        /// <param name="dataFileList">本地静态文件名列表，按照采集顺序排序</param>
        /// <returns></returns>
        internal static async Task<List<List<string>>> CollectDataBySelectorAsync(ParamCollectDataBySelectorModel paramMopdel, List<string> dataFileList, IPluginInvoker pluginInvoker, PluginExecutionContext executionContext, string selectorType = "")
        {
            List<List<string>> collectDataTotalList = new List<List<string>>();
            List<List<string>> tempList = new List<List<string>>();
            dataFileList = dataFileList ?? new List<string>();
            string guiType = selectorType;
            var webDataJObj = JObject.Parse(paramMopdel.WedData ?? "{}");
            if (webDataJObj.ContainsKey("type"))
            {
                guiType = webDataJObj["type"].ToString();
            }
            if (paramMopdel.TurnPageConfigType == TurnPageConfigType.CurrentPage)
            {
                paramMopdel.PageCount = 1;
            }
            else if (paramMopdel.TurnPageConfigType == TurnPageConfigType.AllPage)
            {
                paramMopdel.PageCount = 99999999;
            }
            paramMopdel.NextPageBtnSelector.RemoveAll(sp => sp.Accurate != "true");
            paramMopdel.SelectorDataList.ForEach((selectorData) => selectorData.Selector.RemoveAll(sp => sp.Accurate != "true"));
            IGuiOperator_6_3_0 guiOperator = (IGuiOperator_6_3_0)GuiOperatorProvider.Instance.GetGuiOperator(guiType);

            if (guiOperator == null)
            {
                return new List<List<string>>();
            }
            if (!paramMopdel.IsTable && SupportBlockDataGuiTypes.Any(t => t == guiOperator.GuiType))
            {
                List<SelectorProps> sameParentsProps = new List<SelectorProps>();
                var versionCount = paramMopdel.SelectorDataList.Select(s => s.Version).Distinct().Count();
                if (versionCount == 1)
                {
                    //获取相同的父级
                    sameParentsProps = guiOperator.GetSameParentsProps(paramMopdel);
                }
                paramMopdel.SameParentsProps = sameParentsProps;
                if (sameParentsProps.Count > 0)
                {
                    paramMopdel.SelectorDataList.ForEach((item) =>
                    {
                        item.Selector.RemoveRange(0, sameParentsProps.Count);
                    });
                }
            }
            for (int i = 0; i < paramMopdel.PageCount; i++)
            {
                logger.LogDebug($"开始采集第{i + 1}页数据");
                if (i != 0)
                {
                    await ElementMouseClickAsync(paramMopdel.NextPageBtnSelector, guiType, paramMopdel.NextPageBtnSelectorVersion, pluginInvoker, executionContext).ConfigureAwait(false);
                    logger.LogDebug($"翻页点击，点击第{i + 1}次");
                }
                await Task.Delay(paramMopdel.Delay).ConfigureAwait(false);

                //滚动页面
                ScrollIntoView(guiOperator, paramMopdel.ScrollVertical, paramMopdel.ScrollDelay);

                //采集数据
                List<List<string>> collectDataList = null;
                bool hasNewdata = false;

                //新逻辑
                collectDataList = CollectPageData(paramMopdel, guiOperator, tempList, i, out hasNewdata);


                if (!hasNewdata)
                {
                    break;
                }
                tempList.Clear();
                tempList=collectDataList.ToObject<List<List<string>>>();
                collectDataTotalList.AddRange(collectDataList);

                //运行时执行的逻辑
                if (!string.IsNullOrEmpty(paramMopdel.DataFilePath))//collectDataTotalList.Count > 0 &&
                {

                    if (!paramMopdel.IsTable)
                    {
                        collectDataTotalList = collectDataTotalList.ColumnsDataToRowsData();
                    }

                    //拾取第一页 不是表格数据  且是运行采集，采集数据增加标题数据
                    if (i == 0 && !paramMopdel.IsTable)
                    {
                        //List<string> titleRows = paramMopdel.SelectorDataList.Select(item => item.Title).ToList();
                        List<string> titleRows = new List<string>();
                        for (int iColIndex = 0; iColIndex < paramMopdel.SelectorDataList.Count; iColIndex++)
                        {
                            string ColName = string.IsNullOrEmpty(paramMopdel.SelectorDataList[iColIndex].Title) ? ColumnIDHelper.NumberToColumnID(iColIndex) : paramMopdel.SelectorDataList[iColIndex].Title.Trim();

                            titleRows.Add(ColName);
                        }
                        collectDataTotalList.Insert(0, titleRows);
                    }
                    //如果是表格数据，移除非第一页的表头
                    else if (paramMopdel.IsTable && collectDataTotalList.Count > 0)
                    {
                        if (i == 0)
                        {
                            if (paramMopdel.SelectorDataList[0].CommonTalbleTitleList.Count > 0)//如果带自定义表头 则按照自定义表头显示
                            {
                                for (int ColIndex = 0; ColIndex < collectDataTotalList[0].Count; ColIndex++)
                                {
                                    if (ColIndex < paramMopdel.SelectorDataList[0].CommonTalbleTitleList.Count)
                                    {
                                        if (paramMopdel.SelectorDataList[0].CommonTalbleTitleList[ColIndex].HasSet == 1)
                                        {
                                            collectDataTotalList[0][ColIndex] = paramMopdel.SelectorDataList[0].CommonTalbleTitleList[ColIndex].ColName.Trim();
                                        }
                                        else
                                        {
                                            if (string.IsNullOrWhiteSpace(collectDataTotalList[0][ColIndex]))
                                            {
                                                collectDataTotalList[0][ColIndex] = ColumnIDHelper.NumberToColumnID(ColIndex);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            collectDataTotalList.RemoveAt(0);
                        }
                    }
                    if (collectDataTotalList.Count > 0)
                    {
                        for (int colIndex = 0; colIndex < collectDataTotalList[0].Count; colIndex++)
                        {
                            collectDataTotalList[0][colIndex] = collectDataTotalList[0][colIndex].Trim();
                        }
                    }
                    string fileName = $"datafile_{i}.json";
                    SaveDataFile(collectDataTotalList, paramMopdel.DataFilePath, fileName);
                    dataFileList.Add(fileName);
                    collectDataTotalList.Clear();
                }
                logger.LogDebug($"采集第{i + 1}页数据 结束");
            }

            return collectDataTotalList;
        }

        internal static List<List<string>> CollectDataBySelector(ParamCollectDataBySelectorModel paramMopdel, ref List<string> dataFileList, string selectorType = "")
        {
            List<List<string>> collectDataTotalList = new List<List<string>>();
            List<List<string>> tempList = new List<List<string>>();
            dataFileList = dataFileList ?? new List<string>();
            string guiType = selectorType;
            var webDataJObj = JObject.Parse(paramMopdel.WedData ?? "{}");
            if (webDataJObj.ContainsKey("type"))
            {
                guiType = webDataJObj["type"].ToString();
            }
            if (paramMopdel.TurnPageConfigType == TurnPageConfigType.CurrentPage)
            {
                paramMopdel.PageCount = 1;
            }
            else if (paramMopdel.TurnPageConfigType == TurnPageConfigType.AllPage)
            {
                paramMopdel.PageCount = 99999999;
            }
            paramMopdel.PageCount = 1;
            paramMopdel.NextPageBtnSelector.RemoveAll(sp => sp.Accurate != "true");
            paramMopdel.SelectorDataList.ForEach((selectorData) => selectorData.Selector.RemoveAll(sp => sp.Accurate != "true"));
            IGuiOperator_6_3_0 guiOperator = (IGuiOperator_6_3_0)GuiOperatorProvider.Instance.GetGuiOperator(guiType);

            if (guiOperator == null)
            {
                return new List<List<string>>();
            }
            if (!paramMopdel.IsTable && SupportBlockDataGuiTypes.Any(t => t == guiOperator.GuiType))
            {
                List<SelectorProps> sameParentsProps = new List<SelectorProps>();
                var versionCount = paramMopdel.SelectorDataList.Select(s => s.Version).Distinct().Count();
                if (versionCount == 1)
                {
                    //获取相同的父级
                    sameParentsProps = guiOperator.GetSameParentsProps(paramMopdel);
                }
                paramMopdel.SameParentsProps = sameParentsProps;
                if (sameParentsProps.Count > 0)
                {
                    paramMopdel.SelectorDataList.ForEach((item) =>
                    {
                        item.Selector.RemoveRange(0, sameParentsProps.Count);
                    });
                }
            }
            for (int i = 0; i < paramMopdel.PageCount; i++)
            {
                logger.LogDebug($"开始采集第{i + 1}页数据");
                Thread.Sleep(paramMopdel.Delay);

                //滚动页面
                ScrollIntoView(guiOperator, paramMopdel.ScrollVertical, paramMopdel.ScrollDelay);

                //采集数据
                List<List<string>> collectDataList = null;
                bool hasNewdata = false;

                //新逻辑
                collectDataList = CollectPageData(paramMopdel, guiOperator, tempList, i, out hasNewdata);


                if (!hasNewdata)
                {
                    break;
                }
                tempList.Clear();
                tempList=collectDataList.ToObject<List<List<string>>>();
                collectDataTotalList.AddRange(collectDataList);

                //运行时执行的逻辑
                if (!string.IsNullOrEmpty(paramMopdel.DataFilePath))//collectDataTotalList.Count > 0 &&
                {

                    if (!paramMopdel.IsTable)
                    {
                        collectDataTotalList = collectDataTotalList.ColumnsDataToRowsData();
                    }

                    //拾取第一页 不是表格数据  且是运行采集，采集数据增加标题数据
                    if (i == 0 && !paramMopdel.IsTable)
                    {
                        //List<string> titleRows = paramMopdel.SelectorDataList.Select(item => item.Title).ToList();
                        List<string> titleRows = new List<string>();
                        for (int iColIndex = 0; iColIndex < paramMopdel.SelectorDataList.Count; iColIndex++)
                        {
                            string ColName = string.IsNullOrEmpty(paramMopdel.SelectorDataList[iColIndex].Title) ? ColumnIDHelper.NumberToColumnID(iColIndex) : paramMopdel.SelectorDataList[iColIndex].Title.Trim();

                            titleRows.Add(ColName);
                        }
                        collectDataTotalList.Insert(0, titleRows);
                    }
                    //如果是表格数据，移除非第一页的表头
                    else if (paramMopdel.IsTable && collectDataTotalList.Count > 0)
                    {
                        if (i == 0)
                        {
                            if (paramMopdel.SelectorDataList[0].CommonTalbleTitleList.Count > 0)//如果带自定义表头 则按照自定义表头显示
                            {
                                for (int ColIndex = 0; ColIndex < collectDataTotalList[0].Count; ColIndex++)
                                {
                                    if (ColIndex < paramMopdel.SelectorDataList[0].CommonTalbleTitleList.Count)
                                    {
                                        if (paramMopdel.SelectorDataList[0].CommonTalbleTitleList[ColIndex].HasSet == 1)
                                        {
                                            collectDataTotalList[0][ColIndex] = paramMopdel.SelectorDataList[0].CommonTalbleTitleList[ColIndex].ColName.Trim();
                                        }
                                        else
                                        {
                                            if (string.IsNullOrWhiteSpace(collectDataTotalList[0][ColIndex]))
                                            {
                                                collectDataTotalList[0][ColIndex] = ColumnIDHelper.NumberToColumnID(ColIndex);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            collectDataTotalList.RemoveAt(0);
                        }
                    }
                    if (collectDataTotalList.Count > 0)
                    {
                        for (int colIndex = 0; colIndex < collectDataTotalList[0].Count; colIndex++)
                        {
                            collectDataTotalList[0][colIndex] = collectDataTotalList[0][colIndex].Trim();
                        }
                    }
                    string fileName = $"datafile_{i}.json";
                    SaveDataFile(collectDataTotalList, paramMopdel.DataFilePath, fileName);
                    dataFileList.Add(fileName);
                    collectDataTotalList.Clear();
                }
                logger.LogDebug($"采集第{i + 1}页数据 结束");
            }

            return collectDataTotalList;
        }

        private static void SaveDataFile(List<List<string>> collectDataTotalList, string dataFilePath, string fileName)
        {
            if (Directory.Exists(dataFilePath))
            {
                Directory.CreateDirectory(dataFilePath);
            }
            string filePath = Path.Combine(dataFilePath, fileName);
            File.WriteAllText(filePath, collectDataTotalList.ToJson(), Encoding.UTF8);
        }

        private static List<List<string>> CollectPageData(ParamCollectDataBySelectorModel paramMopdel, IGuiOperator_6_1_0 guiOperator, List<List<string>> previousDataList, int pageNum, out bool hasNewData)
        {
            hasNewData = false;
            List<List<string>> collectDataList = new List<List<string>>();
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            do
            {
                try
                {
                    collectDataList = guiOperator.CollectDataBySelector(paramMopdel);
                    ReformDataCollectionContent(ref collectDataList, paramMopdel);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                    throw ex;
                }

                //新逻辑结束
                if (pageNum == 0)
                {
                    hasNewData = true;
                    break;
                }

                //与上一次采集数据是否相同，相同认为已经是最后一页
                if (pageNum != 0)
                {
                    //判断本次数据与上一页数据是否完全相同
                    var isSame = IsSameList(previousDataList, collectDataList);
                    if (!isSame)
                    {
                        hasNewData = true;
                        break;
                    }
                    logger.LogDebug("两次采集数据完全相同，有可能是数据没有加载完，会再次尝试获取新的数据，最大耗时>=10s");
                    Thread.Sleep(300);
                }
            } while (stopWatch.ElapsedMilliseconds < 10000);
            stopWatch.Stop();
            return collectDataList;
        }
        private static bool IsSameList(List<List<string>> list1, List<List<string>> list2)
        {
            if (list1.Count != list2.Count)
            {
                return false;
            }
            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i].Count != list2[i].Count)
                {
                    return false;
                }
                for (int j = 0; j < list1[i].Count; j++)
                {
                    if (!list1[i][j].Equals(list2[i][j]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static void ReformDataCollectionContent(ref List<List<string>> CurrentDataCollection, ParamCollectDataBySelectorModel paramMopdel)
        {
            if (CurrentDataCollection != null && CurrentDataCollection.Count > 0)
            {
                for (int iColumn = 0; iColumn < CurrentDataCollection.Count; iColumn++)
                {
                    if (CurrentDataCollection[iColumn] != null && CurrentDataCollection[iColumn].Count > 0)
                    {
                        for (int iRowItem = 0; iRowItem < CurrentDataCollection[iColumn].Count; iRowItem++)
                        {
                            if (!string.IsNullOrEmpty(CurrentDataCollection[iColumn][iRowItem]))
                            {
                                string tempString = CurrentDataCollection[iColumn][iRowItem].Replace("\r\n", " ").Replace("\n", " ");
                                if (!paramMopdel.IsTable)
                                {
                                    tempString = DataCollectRegexHelper.GenerateStringWithRegex(tempString, paramMopdel.SelectorDataList[iColumn].Regular, " ");
                                }
                                CurrentDataCollection[iColumn][iRowItem] = tempString;
                            }
                        }
                    }
                }
            }
        }

        private static void ScrollIntoView(IGuiOperator_6_1_0 guiOperator, int scrollVertical, int scrollDelay = -1)
        {
            //拾取前滚动
            if (scrollVertical > 0)
            {
                var jObj = new JObject
                {
                    { "scrollLeft", 0 },
                    { "scrollTop", scrollVertical },
                    { "ScrollVerticalDelay",(scrollDelay == -1 ? Configurations.Info.ScrollVerticalDelay : scrollDelay)}
                };
                guiOperator.ScrollIntoView(jObj.ToString());
                //Thread.Sleep(scrollDelay == -1 ? Configurations.Info.ScrollVerticalDelay : scrollDelay);//滚动之后停留2秒等页面渲染，
            }
        }
        /// <summary>
        /// 点击元素
        /// </summary>
        /// <param name="selectorProps"></param>
        /// <param name="guiType"></param>
        /// <returns></returns>
        internal static async Task<bool> ElementMouseClickAsync(
            List<SelectorProps> selectorProps,
            string guiType,
            string selectorVersino,
            IPluginInvoker pluginInvoker,
            PluginExecutionContext executionContext)
        {
            RPACommon common = new RPACommon();
            common.CommonProps.GUIType = guiType;
            common.CommonProps.Version = selectorVersino;
            common.Selector = JArray.FromObject(selectorProps);

            ParamMouseClickModel paramMouseClick = new ParamMouseClickModel()
            {
                Selector = common.ToJson(),
                clickType = MouseParamDef.CLK_BTN_LEFT,
                keyType = MouseParamDef.CLK_TYPE_SINGLE,
                issimulate = MouseParamDef.BACKGROUND_OPT,
                cursorPos = MouseParamDef.CUR_POS_CENTER,
                timeout = 10000
            };
            JMessage msg = new JMessage();
            try
            {
                string jsonParam = paramMouseClick.ToJson();
                int count = 0;
                while (true)
                {
                    count++;
                    string result = await pluginInvoker.InvokeAsync(
                        new PluginRoute("Element", "MouseClick"),
                        jsonParam).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(result))
                    {
                        var rpaEx = new RPAException(eErrorMessageType.Error_DataCollection_NextBtnNotFound);
                        throw rpaEx;
                    }
                    msg = result.ToObject<JMessage>();
                    if (msg.Code != 0)
                    {
                        if (count >= 10)
                        {
                            var rpaEx = new RPAException(eErrorMessageType.Error_DataCollection_NextBtnNotFound);
                            throw rpaEx;
                        }
                        await Task.Delay(200).ConfigureAwait(false);
                    }
                    else
                    {
                        return true;
                    }
                }

            }
            catch (RPAException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "数据采集-下一页按钮点击失败");
                var rpaEx = new RPAException(eErrorMessageType.Error_DataCollection_NextBtnNotFound, ex);
                throw rpaEx;
            }
        }

        #endregion

        #region 元素拾取程序实现相关 如：判断非拾取区域、拾取元素

        /// <summary>
        /// 是否是 非可拾取位置,ture 不可拾取位置
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        internal static bool IsNonCapturePosition(Point screenPoint)
        {
            if (GlobalData.GetInstance().VM_MainWindowViewModel?.GetMainWindowCurrentRect == null)
            {
                return false;
            }

            //var rectList = new List<System.Windows.Rect>();
            var rectList = GlobalData.GetInstance().VM_MainWindowViewModel.GetMainWindowCurrentRect();
            //rectList.Add(rect);
            var point = new System.Windows.Point(screenPoint.X, screenPoint.Y);
            foreach (var rect in rectList)
            {
                if (rect.Contains(point))
                {
                    return true;
                }
            }
            return false;
        }
        private static void GetPointAndWndInfo(Point screenPoint, out AutomationElement rootElement, out AutomationElement pointElement)
        {
            lock (_objLock_GetPointAndWndInfo)
            {
                rootElement = null;
                pointElement = null;
                try
                {
                    //得到自根窗口开始到点窗口（通过点直接获取到）的句柄集合
                    var hWndList = UiCommon.GetRootWindowList(screenPoint);
                    if (hWndList.Count > 0)
                    {
                        foreach (IntPtr hWnd in hWndList)
                        {
                            if (hWnd != IntPtr.Zero)
                            {
                                rootElement = UiCommon.UIAAutomation.FromHandle(hWnd);   //获取根窗体
                            }
                            if (rootElement == null)
                            {
                                break;
                            }
                            if (rootElement.Properties.BoundingRectangle.IsSupported && rootElement.BoundingRectangle.Contains(screenPoint))
                            {
                                break;
                            }
                        }
                        pointElement = UiCommon.UIAAutomation.FromHandle(hWndList.Last());
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"获取屏幕元素时发生一次异常，将忽略本次异常继续拾取");
                    screenPoint = Point.Empty;
                }
            }
        }
        internal static IGuiNode InspectUI(Point screenPoint)
        {
            IGuiNode inspectNode = null;
            string guiType = string.Empty;
            try
            {
                AutomationElement rootElement = null;
                AutomationElement pointElement = null;
                bool inspectResult = false;
                GetPointAndWndInfo(screenPoint, out rootElement, out pointElement);
                if (rootElement == null)
                {
                    logger.LogDebug($"SelectorIssueTrace -- rootElement is null");
                    return null;
                }
                if (pointElement == null)
                {
                    logger.LogDebug($"SelectorIssueTrace -- pointElement is null");
                    return null;
                }

                if (GuiOperatorProvider.Instance.TryGetSupportedOperator(screenPoint, rootElement, pointElement, out var guiOperator))
                {
                    try
                    {
                        //logger.LogDebug($"开始使用插件【{guiOperator.GuiType}】中的拾取方法");
                        if (guiOperator.GuiType == eGuiType.IE || guiOperator.GuiType == eGuiType.SAP)
                        {
                            dataCollectOverlayWnd.Dispatcher.Invoke(() =>
                            {
                                inspectResult = guiOperator.InspectNode(screenPoint, rootElement, pointElement, "", out inspectNode);
                            });
                        }
                        else
                        {
                            inspectResult = guiOperator.InspectNode(screenPoint, rootElement, pointElement, "", out inspectNode);
                        }
                        //GuiElement.overlayForm.CaptureUnauthorizedAccessStr = string.Empty;
                    }
                    catch (RPAException ex)
                    {
                        throw;
                        //if (ex.ErrorMessageType == eErrorMessageType.Error_CaptureUnauthorizedAccessError)
                        //{
                        //    GuiElement.overlayForm.CaptureUnauthorizedAccessStr = ex.ErrorMessageContent;
                        //}
                    }
                    catch (ThreadAbortException ex)
                    {
                        //强制终止线程  不处理
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"循环判断拾取程序发生了异常：{guiOperator.GuiType}");
                    }
                }
                if (!inspectResult)
                {
                    //实例化一个新的Windows UIANode 作为默认的元素，尽可能少的使用
                    inspectNode = GuiManager.CreateWindowsGuiNode(pointElement);
                }
            }
            catch (COMException ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"拾取程序发生底层程序异常");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"拾取程序发生了异常");

                if (ex is RPAException)
                {
                    //只抛出插件相关的错误
                    RPAException rpaex = (RPAException)ex;
                    if (rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnInstalled
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnopen
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnInstalled
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnopen
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnInstalled
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnopen
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnInstalled
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnopen
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnInstalled
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnopen
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_JavaExtensionUnInstalled
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_ServerHubUnopen
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnInstalled
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnopen
                        || rpaex.ErrorMessageType == eErrorMessageType.Error_BrowserPluginVersionTooLow
                        )
                    {
                        //if (!DataCollectingManager.WebBroswerExtensionPopupStatus.ContainsKey($"{guiType}:{rpaex.ErrorMessageType}"))
                        //{
                        throw ex;
                        //isStopCapture = MessageHelper.ShowDialog("Chrome浏览器未安装插件，无法采集数据！", "安装插件");
                        //DataCollectingManager.WebBroswerExtensionPopupStatus[rpaex.ErrorMessageType] = isStopCapture;
                        //}

                    }
                }
            }
            return inspectNode;
        }
        #endregion

        #region 一些辅助方法 如：获取新图片地址
        internal static string GetNewImgPath(params string[] dirs)
        {
            string fileName = Guid.NewGuid().ToString() + ".png";
            string dirPath = Path.Combine(dirs);
            //if (!Directory.Exists(dirPath))
            //{
            //    Directory.CreateDirectory(dirPath);
            //}
            string filePaht = Path.Combine(dirPath, fileName);
            return filePaht;
        }
        #endregion

    }
}



