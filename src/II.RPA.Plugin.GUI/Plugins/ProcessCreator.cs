using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ProcessCreator;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.Plugin.GUI.ProcessCreator.Screenshot;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using II.RPA.Plugin.Abstractions;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class ProcessCreator : ElementAbilityBase<ParamProcessCreatorModel>
    {
        private static readonly ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ProcessCreator;
        private readonly ElementTracker m_ElementTracker;
        private readonly ElementHighlighter m_ElementHighlighter;
        private readonly ImageTracker m_ImageTracker;
        private readonly ZbrainService m_ZbrainService;
        private readonly ElementCacheProvider m_ElementCacheProvider;
        private bool m_HasInit = false;
        private bool m_HasDummyInit = false;
        private volatile bool m_HasBeenOver;
        private readonly IPluginInvoker pluginInvoker;
        public ProcessCreator(IPluginInvoker pluginInvoker)
        {
            this.pluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
            logger.LogDebug("Creater ProcessCreator");
            m_ElementCacheProvider = new ElementCacheProvider();
            m_ZbrainService = new ZbrainService(GlobleConfigurations.ZBrainServicePath);
            m_ElementHighlighter = new ElementHighlighter();
            m_ImageTracker = new ImageTracker();
            m_ElementTracker = new ElementTracker(m_ElementHighlighter, 
                m_ImageTracker, 
                m_ZbrainService, 
                m_ElementCacheProvider,
                pluginInvoker);
        }
        private bool IsStartFlagForExcel
        {
            get
            {
                if (m_ElementTracker == null)
                {
                    return false;
                }
                else
                {
                    return m_ElementTracker.IsStart;
                }
            }
        }
        protected override JMessage Execute(ParamProcessCreatorModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            try
            {
                m_ElementTracker.BindPluginInvocation(executionContext);
                logger.LogDebug($"ZBrainServicePath:{GlobleConfigurations.ZBrainServicePath}");

                GlobleConfigurations.CVServer = paramModel.CV_Server_Url;
                logger.LogInformation($"ProcessCreator.Action:{paramModel.CreatorAction}");
                logger.LogInformation($"ProcessCreator.ClientType:{paramModel.ClientType}");
                logger.LogInformation($"ProcessCreator.HasInit:{m_HasInit}");
                logger.LogInformation($"ProcessCreator.HasDummyInit:{m_HasDummyInit}");
                logger.LogInformation($"ProcessCreator.PackagePath:{paramModel.PackagePath}");
                logger.LogInformation($"ProcessCreator.IsCvSupport:{paramModel.IsCvSupport}");
                logger.LogInformation($"ProcessCreator.Addition:{paramModel.Addition}");

                switch (paramModel.CreatorAction)
                {
                    case CreatorActionType.INIT:
                        if (!m_HasDummyInit)
                        {
                            DummyInit(paramModel.PackagePath);
                        }
                        logger.LogInformation("Init Completely!");
                        AutoInitDetect(paramModel.PackagePath);
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = eCommonMessageType.ProcessCreator_cs_LiuChengBianJiChuShiHuaWanCheng.GetMessage(),
                        };
                    //启动
                    case CreatorActionType.START:
                        m_HasBeenOver = false;
                        AutoInitDetect(paramModel.PackagePath);
                        MessageService.PostMessage(TerminalType.CoreServer, "visualdata.excel", $"{{\"processcreatorstatus\":0}}", _ => { });
                        MessageService.PostMessage(TerminalType.CoreServer, "flowedit.notify", $"{{\"processcreatorstatus\":0}}", _ => { });
                        m_ElementTracker.IsNeedDeskTopToolDetect = true;
                        m_ElementTracker.IsEnableCV = paramModel.IsCvSupport == 1;
                        m_ElementTracker.IsStart = true;
                        if (!m_ElementTracker.IsTracking)
                        {
                            m_ElementTracker.Start().ConfigureAwait(false);
                            return new JMessage
                            {
                                Code = 0,
                                Info = "{}",
                                Message = eCommonMessageType.ProcessCreator_cs_LiuChengBianJiMoShiYiQiDong.GetMessage(),
                            };
                        }
                        else
                        {
                            return new JMessage
                            {
                                Code = 0,
                                Info = "{}",
                                Message = eCommonMessageType.ProcessCreator_cs_ZhongFuCaoZuoLiuChengBianJiMo.GetMessage(),
                            };
                        }
                    //恢复
                    case CreatorActionType.RESUME:
                        m_HasBeenOver = false;
                        AutoInitDetect(paramModel.PackagePath);
                        MessageService.PostMessage(TerminalType.CoreServer, "visualdata.excel", $"{{\"processcreatorstatus\":0}}", _ => { });
                        MessageService.PostMessage(TerminalType.CoreServer, "flowedit.notify", $"{{\"processcreatorstatus\":0}}", _ => { });
                        m_ElementTracker.IsStart = true;
                        if (!m_ElementTracker.IsTracking)
                        {
                            m_ElementTracker.Start("run.end" == paramModel.Addition);
                        }
                        if (m_HasBeenOver)
                        {
                            m_ElementTracker.IsStart = false;
                            m_ElementTracker.Stop();
                        }
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = eCommonMessageType.ProcessCreator_cs_LiuChengBianJiMoShiYiQiDong.GetMessage(),
                        };
                    //暂停
                    case CreatorActionType.PAUSE:
                        m_HasBeenOver = true;
                        m_ElementTracker.IsStart = false;
                        MessageService.PostMessage(TerminalType.CoreServer, "visualdata.excel", $"{{\"processcreatorstatus\":1}}", _ => { });
                        MessageService.PostMessage(TerminalType.CoreServer, "flowedit.notify", $"{{\"processcreatorstatus\":1}}", _ => { });
                        m_ElementTracker.Stop();
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = eCommonMessageType.ProcessCreator_cs_LiuChengBianJiMoShiYiZanTing.GetMessage(),
                        };
                    //结束
                    case CreatorActionType.END:
                        m_HasBeenOver = true;
                        m_ElementTracker.IsStart = false;
                        m_ElementTracker.Stop();
                        MessageService.PostMessage(TerminalType.CoreServer, "visualdata.excel", $"{{\"processcreatorstatus\":1}}", _ => { });
                        MessageService.PostMessage(TerminalType.CoreServer, "flowedit.notify", $"{{\"processcreatorstatus\":1}}", _ => { });
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = eCommonMessageType.ProcessCreator_cs_LiuChengBianJiMoShiYiJieShu.GetMessage(),
                        };
                    case CreatorActionType.RESTART:
                        m_HasBeenOver = false;
                        AutoInitDetect(paramModel.PackagePath);
                        m_ElementTracker.Stop();
                        m_ElementTracker.Start().ConfigureAwait(false);
                        MessageService.PostMessage(TerminalType.CoreServer, "visualdata.excel", $"{{\"processcreatorstatus\":0}}", _ => { });
                        MessageService.PostMessage(TerminalType.CoreServer, "flowedit.notify", $"{{\"processcreatorstatus\":0}}", _ => { });
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = eCommonMessageType.ProcessCreator_cs_LiuChengBianJiMoShiYiZhongQi.GetMessage(),
                        };
                    case CreatorActionType.INSTEAD_ELEMENT:
                        AutoInitDetect(paramModel.PackagePath);
                        m_ElementTracker.Stop();
                        var selector = m_ElementTracker.StartOnceTime();
                        Thread.Sleep(300);
                        if (selector != "2")
                        {
                            if (m_ElementTracker.IsStart)
                            {
                                m_ElementTracker.Start().ConfigureAwait(false);
                            }
                        }
                        if (string.IsNullOrEmpty(selector))
                        {
                            return new JMessage
                            {
                                Code = 1,
                                Info = "{}",
                                Message = eCommonMessageType.ProcessCreator_cs_ZhongXinShiQuShiBai.GetMessage(),
                            };
                        }
                        else if (selector == "1")
                        {
                            return new JMessage
                            {
                                Code = 1,
                                Info = "{}",
                                Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage(),
                            };
                        }
                        else
                        {
                            return new JMessage
                            {
                                Code = 0,
                                Message = eCommonMessageType.ProcessCreator_cs_ZhongXinShiQuYiWanCheng.GetMessage(),
                                Info = $"{{\"uiSelector\":{selector}}}",
                            };
                        }
                    case CreatorActionType.CANCEL_INSTEAD_ELEMENT:
                        m_ElementTracker.StopOnceTime();
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage(),
                        };
                    case CreatorActionType.CAPTURE_IMAGE_ONCETIME:
                        AutoInitDetect(paramModel.PackagePath);
                        m_ElementTracker.Stop();
                        var selectorImage = m_ImageTracker.StartOnceTime(m_ElementTracker);
                        if (string.IsNullOrEmpty(selectorImage) || selectorImage == "-1")
                        {
                            return new JMessage
                            {
                                Code = 1,
                                Message = eCommonMessageType.ProcessCreator_cs_ZhongXinJieTuYiQuXiao.GetMessage(),
                                Info = "{}",
                            };
                        }
                        else
                        {
                            return new JMessage
                            {
                                Code = 0,
                                Message = eCommonMessageType.ProcessCreator_cs_ZhongXinJieTuYiWanCheng.GetMessage(),
                                Info = $"{{\"uiSelector\":{selectorImage}}}",
                            };
                        }
                    case CreatorActionType.CAPTURE_IMAGE:
                        if (!m_ImageTracker.IsStarted)
                        {
                            AutoInitDetect(paramModel.PackagePath);
                            m_ElementTracker.Stop();
                            m_ImageTracker.Start(m_ElementTracker);
                            return new JMessage
                            {
                                Code = 0,
                                Info = "{}",
                                Message = eCommonMessageType.ProcessCreator_cs_YiQieHuanWeiJieTuMoShi.GetMessage(),
                            };
                        }
                        else
                        {
                            return new JMessage
                            {
                                Code = 0,
                                Info = "{}",
                                Message = eCommonMessageType.ProcessCreator_cs_ZhongFuDiaoYongJieTuMoShiYi.GetMessage(),
                            };
                        }
                    case CreatorActionType.GET_STATUS:
                        return new JMessage
                        {
                            Code = 0,
                            Message = eCommonMessageType.ProcessCreator_cs_LiuChengBianJiZhuangTaiIs.GetMessage() + $"{IsStartFlagForExcel}",
                            Info = $"{{\"processcreatorstatus\":{(IsStartFlagForExcel ? 0 : 1)}}}",

                        };
                    case CreatorActionType.DUMMY_INIT:
                        DummyInit(paramModel.PackagePath);
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = eCommonMessageType.ProcessCreator_cs_LiuChengBianJiChuShiHuaWanCheng.GetMessage() + "(Dummy)",
                        };
                    case CreatorActionType.START_EXCEL_RECORD:
                        m_ElementTracker.IgnoreExcelElement();
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = "已停止拾取excel".I18n("ProcessCreator_9D347BEC"),
                        };
                    case CreatorActionType.STOP_EXCEL_RECORD:
                        m_ElementTracker.ConsiderExcelElement();
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = "已恢复拾取excel".I18n("ProcessCreator_1AE5133F"),
                        };
                    case CreatorActionType.ENABLE_CV:
                        m_ElementTracker.IsEnableCV = true;
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = Localize($"是否启用CV拾取：{m_ElementTracker.IsEnableCV} ", "ProcessCreator_C82D61BE"),
                        };
                    default:
                        return new JMessage
                        {
                            Code = 0,
                            Info = "{}",
                            Message = eCommonMessageType.ProcessCreator_cs_CanShuCuoWu.GetMessage(),
                        };
                }
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_UNKOWN, eComponentType.ProcessCreator, ex);
            }
            finally
            {
                if (!m_ElementTracker.IsStart && m_ElementTracker.IsTracking)
                {
                    logger.LogWarning("ipa status dismatch:stop!");
                    m_ElementTracker.Stop();
                }
            }
        }

        protected override void ValidateParameter(ParamProcessCreatorModel paramModel) {
            if (paramModel == null)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            return;
        }

        private void AutoInitDetect(string aPackagePath)
        {
            if (!m_HasInit)
            {
                logger.LogInformation("AutoInitDetect Start");
                m_HasInit = true;
                LocalDataModels.Init();
                ActionDataModels.PackagePath = aPackagePath;
                logger.LogInformation($"AutoInitDetect PackagePath:{ActionDataModels.PackagePath}");
                LocalDataModels.SearchHistoryList = LocalDataModels.LoadSearchHistory();
                ActionDataModels.Init();
                m_ZbrainService.SaveUserCustomModel().ContinueWith(_ => m_ZbrainService.LoadRecommendModel()).ConfigureAwait(false);
                string aSettingFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Core", "component.json");
                ActionDataModels.ComponentsDataHandle(aSettingFile);
                logger.LogInformation("AutoInitDetect Over");
            }
        }
        private void DummyInit(string aPackagePath)
        {
            if (!m_HasDummyInit && !m_HasInit)
            {
                m_HasDummyInit = true;
                LocalDataModels.Init();
                ActionDataModels.PackagePath = aPackagePath;
                logger.LogInformation($"DummyInit PackagePath:{ActionDataModels.PackagePath}");
                LocalDataModels.SearchHistoryList = LocalDataModels.LoadSearchHistory();
                ActionDataModels.Init();
                m_ZbrainService.SaveUserCustomModel().ContinueWith(_ => m_ZbrainService.LoadRecommendModel()).ConfigureAwait(false);
                string aSettingFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Core", "component.json");
                ActionDataModels.ComponentsDataHandle(aSettingFile);
                logger.LogInformation("Dummy Init Completely!");
            }
        }
    }
}
