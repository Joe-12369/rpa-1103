using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze
{
    public class AnalyzerContainer
    {
        private static readonly ILogger logger = Logger.OfContext();
        public static readonly AnalyzerContainer AnalyzerContainerInstance = new AnalyzerContainer();
        private static ConcurrentDictionary<Type, IAnalyzer> Analyzers;
        private static readonly IAnalyzer AIAnalyzer = new AnalyzerAI();
        public static ZbrainService ZbrainServiceInstance
        {
            get;
            private set;
        }

        private readonly ConcurrentDictionary<Type, IAnalyzer> m_Analyzers;
        private readonly ConcurrentDictionary<Type, IAnalyzer> m_SlimAnalyzers;
        private readonly ConcurrentDictionary<Type, IAnalyzer> m_AdditionalAnalyzers;
        private readonly ConcurrentDictionary<Type, IAnalyzer> m_ElementClassAnalyzers;


        private ActionParamModel m_ActionParam;
        private Task<SelectElementModel> m_SelectTask;
        private IEnumerable<string> m_Components;
        private ElementCacheProvider m_ElementCacheProvider;

        static AnalyzerContainer()
        {
            RegisterInternal();
        }
        private AnalyzerContainer()
        {
            m_Analyzers = new ConcurrentDictionary<Type, IAnalyzer>();
            m_SlimAnalyzers = new ConcurrentDictionary<Type, IAnalyzer>();
            m_AdditionalAnalyzers = new ConcurrentDictionary<Type, IAnalyzer>();
            m_ElementClassAnalyzers = new ConcurrentDictionary<Type, IAnalyzer>();

            //CurrentResolvedActions = new List<ExecuteAction>();
            AdditionalResolvedActions = new List<ExecuteAction>();
            AdditionalResolvedActionParams = new Dictionary<string, string>();
        }
        /// <summary>
        /// 当前解析出的附加action
        /// </summary>
        public List<ExecuteAction> AdditionalResolvedActions
        {
            get;
            private set;
        }
        /// <summary>
        /// 当前解析到的附加actionparam
        /// </summary>
        public Dictionary<string, string> AdditionalResolvedActionParams
        {
            get;
            private set;
        }
        private List<ExecuteAction> CacheResolvedActions
        {
            get;
            set;
        }
        /// <summary>
        /// 当前解析出的action
        /// </summary>
        public List<ExecuteAction> CurrentResolvedActions
        {
            get;
            private set;
        }
        /// <summary>
        /// 当前解析到的actionparam
        /// </summary>
        public Dictionary<string,string> CurrentResolvedActionParams
        {
            get;
            private set;
        }
        public string ElementClassResult
        {
            get;
            private set;
        }

        public void InitAnalyzerContainer(ActionParamModel anActionParam,
              Task<SelectElementModel> aSelectTask,
              IEnumerable<string> aCurrentComponents,
              ZbrainService aZbrainService,
              ElementCacheProvider anElementCacheProvider)
        {
            m_ActionParam = anActionParam;
            m_SelectTask = aSelectTask;
            m_Components = aCurrentComponents;
            ZbrainServiceInstance = aZbrainService;
            m_ElementCacheProvider = anElementCacheProvider;
        }
        private static void RegisterInternal()
        {
            Analyzers = new ConcurrentDictionary<Type, IAnalyzer>();
            var curTypes = Assembly.GetExecutingAssembly().GetTypes();
            var analyzerTypes = curTypes.Where(t => typeof(IAnalyzer).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
            var analyzerInstances = analyzerTypes.Select(t => Activator.CreateInstance(t)).Cast<IAnalyzer>();
            analyzerInstances.ForEach(a => Analyzers.TryAdd(a.GetType(),a));
        }
        /// <summary>
        /// 注册分析器
        /// </summary>
        public void RegisterAnalyzer()
        {
            UnRegisterAll();
            if (m_ActionParam.ElementType != Params.ElementType.Ordinary)
            {
                m_ElementClassAnalyzers.TryAdd(typeof(AnalyzerForCv), Analyzers[typeof(AnalyzerForCv)]);
            }
            else
            {
                if (m_ActionParam.GuiType == eGuiType.Windows)
                {
                    if (!string.IsNullOrEmpty(m_ActionParam.ProcessName))
                    {
                        if (m_ActionParam.ProcessName.ToLower().StartsWith("explorer"))
                        {
                            RegisterAnalyzer(Analyzers[typeof(AnalyzerForExplorer)], Priority.High);
                            RegisterAnalyzer(Analyzers[typeof(AnalyzerForDeskTopApp)], Priority.Highest);
                        }
                        else if (m_ActionParam.ProcessName.ToLower().StartsWith("dingtalk"))
                        {
                            RegisterAdditionalAnalyzer(Analyzers[typeof(AnalyzerForCv)]);
                            return;
                        }
                        else if (m_ActionParam.ProcessName.Equals("msedge", StringComparison.OrdinalIgnoreCase)
                            || m_ActionParam.ProcessName.Equals("chrome", StringComparison.OrdinalIgnoreCase)
                            || m_ActionParam.ProcessName.Equals("iexplore", StringComparison.OrdinalIgnoreCase))
                        {
                            if (m_ActionParam.ControlType.Equals("edit", StringComparison.OrdinalIgnoreCase))
                            {
                                RegisterSlimAnalyzer(Analyzers[typeof(AnalyzerForBrowserAddressBar)], Priority.Highest);
                            }
                        }
                    }
                    RegisterAnalyzer(Analyzers[typeof(AnalyzerForWindows)], Priority.Default);
                }
                else if (m_ActionParam.GuiType == eGuiType.Chrome
                    || m_ActionParam.GuiType == eGuiType.Edge
                    || m_ActionParam.GuiType == eGuiType.IE
                    || m_ActionParam.GuiType == eGuiType.Sogou
                    || m_ActionParam.GuiType == eGuiType.FireFox
                    || m_ActionParam.GuiType == eGuiType.ZExplorer
                    || m_ActionParam.GuiType == eGuiType.Web360se
                    || m_ActionParam.GuiType == eGuiType.Web360X
                    || m_ActionParam.GuiType == eGuiType.SuperBrowser
                    || m_ActionParam.GuiType == eGuiType.Chromium)
                {
                    RegisterAnalyzer(Analyzers[typeof(AnalyzerForInput)], Priority.Highest);
                    RegisterAnalyzer(Analyzers[typeof(AnalyzerForWeb)], Priority.High);
                }
                RegisterAdditionalAnalyzer(Analyzers[typeof(AnalyzerForCv)]);
            }
            //RegisterAnalyzer(new AnalyzerForNativeDefault(m_ActionParam), Priority.Lowest);
        }
        /// <summary>
        /// 执行分析器
        /// </summary>
        /// <returns></returns>
        public void RegisterAnalyzer(IAnalyzer anElementAnalyzer)
        {
            RegisterAnalyzer(anElementAnalyzer, anElementAnalyzer.ExecutePriority);
        }
        public void RegisterAnalyzer(IAnalyzer anElementAnalyzer,Priority aPriority)
        {
            anElementAnalyzer.ExecutePriority = aPriority;
            if (m_Analyzers.TryAdd(anElementAnalyzer.GetType(), anElementAnalyzer))
            {
                logger.LogInformation($"Register AnalyzerName:{anElementAnalyzer.AnalyzerName}");
            }
            else
            {
                logger.LogError($"Duplicate AnalyzerName:{anElementAnalyzer.AnalyzerName}");
            }
        }
        public void RegisterSlimAnalyzer(IAnalyzer anElementAnalyzer, Priority aPriority)
        {
            anElementAnalyzer.ExecutePriority = aPriority;
            if (m_SlimAnalyzers.TryAdd(anElementAnalyzer.GetType(), anElementAnalyzer))
            {
                logger.LogInformation($"Register Slim AnalyzerName:{anElementAnalyzer.AnalyzerName}");
            }
            else
            {
                logger.LogError($"Duplicate Slim AnalyzerName:{anElementAnalyzer.AnalyzerName}");
            }
        }
        public void RegisterAdditionalAnalyzer(IAnalyzer anElementAnalyzer)
        {
            if (m_AdditionalAnalyzers.TryAdd(anElementAnalyzer.GetType(), anElementAnalyzer))
            {
                logger.LogInformation($"Register Additional AnalyzerName:{anElementAnalyzer.AnalyzerName}");
            }
            else
            {
                logger.LogError($"Duplicate Additional AnalyzerName:{anElementAnalyzer.AnalyzerName}");
            }
        }

        public IEnumerable<IAnalyzer> ResolveAnalyzer()
        {
            if (m_Analyzers.Any())
            {
                return m_Analyzers.Select(a=>a.Value);
            }
            return Enumerable.Empty<IAnalyzer>();
        }
        public IEnumerable<IAnalyzer> ResolveSlimAnalyzer()
        {
            if (m_SlimAnalyzers.Any())
            {
                return m_SlimAnalyzers.Select(a => a.Value);
            }
            return Enumerable.Empty<IAnalyzer>();
        }
        public IEnumerable<IAnalyzer> ResolveAdditionalAnalyzer()
        {
            if (m_AdditionalAnalyzers.Any())
            {
                return m_AdditionalAnalyzers.Select(a => a.Value);
            }
            return Enumerable.Empty<IAnalyzer>();
        }
        public void UnRegisterAll()
        {
            m_AdditionalAnalyzers?.Clear();
            m_ElementClassAnalyzers?.Clear();
            m_Analyzers?.Clear();
            m_SlimAnalyzers?.Clear();
        }
        public async Task<List<ExecuteAction>> ResolveAction(bool aNeedAI, int aNeedCount = 6)
        {
            if (!aNeedAI)
            {
                var nativeActions = await ResolveNativeAction();
                if (nativeActions.Count > aNeedCount)
                {
                    return nativeActions.Take(aNeedCount).ToList();
                }
                else
                {
                    return nativeActions;
                }
            }
            else
            {
                var actions = await Task.WhenAll(ResolveNativeAction(), ResolveAIActions());
                var nativeActions = actions[0];
                var aiActions = actions[1];


                bool nativeOK, aiOK;
                if (nativeActions != null && nativeActions.Count > 0)
                {
                    nativeOK = true;
                    logger.LogInformation($"nativeActions:{string.Join(";", nativeActions.Select(na => na.ActionId))}");
                }
                else
                {
                    nativeOK = false;
                    logger.LogError("nativeActions:empty");
                }

                if (aiActions != null && aiActions.Count > 0)
                {
                    aiOK = true;
                    logger.LogInformation($"aiActions:{string.Join(";", aiActions.Select(na => na.ActionId))}");
                }
                else
                {
                    aiOK = false;
                    logger.LogError("aiActions:empty");
                }

                if (nativeOK && aiOK)
                {
                    List<ExecuteAction> resolvedActions = new List<ExecuteAction>();
                    var nativeIds = nativeActions.Select(na => na.ActionId);
                    var aiIds = aiActions.Select(na => na.ActionId);

                    resolvedActions.AddRange(nativeActions);
                    if (resolvedActions.Count >= aNeedCount)
                    {
                        logger.LogInformation($"ResolveAction Over:only nativeIds is enough({nativeIds.Count()})");
                        return resolvedActions.Take(aNeedCount).ToList();
                    }
                    else
                    {
                        var onlyAiIds = aiIds.Except(nativeIds);
                        if (onlyAiIds != null && onlyAiIds.Count() > 0)
                        {
                            logger.LogInformation($"Add onlyAiIds:{string.Join(";", onlyAiIds)}");
                            resolvedActions.AddRange(aiActions.Where(na => onlyAiIds.Contains(na.ActionId)));
                            if (resolvedActions.Count >= aNeedCount)
                            {
                                logger.LogInformation($"ResolveAction Over:nativeIds({nativeIds.Count()}) + onlyAiIds({onlyAiIds.Count()})");
                                return resolvedActions.Take(aNeedCount).ToList();
                            }
                            else
                            {
                                logger.LogInformation($"ResolveAction Over:nativeIds({nativeIds.Count()}) + onlyAiIds({onlyAiIds.Count()})");
                                return resolvedActions.ToList();
                            }
                        }
                        else
                        {
                            logger.LogInformation($"onlyAiIds:empty");
                            logger.LogInformation($"ResolveAction Over:nativeIds({nativeIds.Count()})");
                            return resolvedActions.ToList();
                        }
                    }
                }
                else if (nativeOK && !aiOK)
                {
                    if (nativeActions.Count > aNeedCount)
                    {
                        return nativeActions.Take(aNeedCount).ToList();
                    }
                    else
                    {
                        return nativeActions;
                    }
                }
                else if (!nativeOK && aiOK)
                {
                    logger.LogCritical("Native ResolveAction Error");
                    if (aiActions.Count > aNeedCount)
                    {
                        return aiActions.Take(aNeedCount).ToList();
                    }
                    else
                    {
                        return aiActions;
                    }
                }
                else
                {
                    logger.LogCritical("ResolveAction Error");
                    return Enumerable.Empty<ExecuteAction>().ToList();
                }
            }

        }
        public async Task<IEnumerable<ActionModel>> ResolveAction(bool aNeedAI, Action<string> anElementClassResultAction, int aNeedCount = 10)
        {
            var res = await ResolveAction(aNeedAI, aNeedCount);
            anElementClassResultAction?.Invoke(ElementClassResult);
            return ConvertToActionModel(res);
        }
        public async Task<IEnumerable<ActionModel>> ResolveActionNew(bool aNeedAI, Action<string> anElementClassResultAction, int aNeedCount = 10)
        {
            var nativeActions = await ResolveNativeAction();
            anElementClassResultAction?.Invoke(ElementClassResult);
            if (!aNeedAI)
            {
                List<ExecuteAction> resultList;
                if (nativeActions.Count > aNeedCount)
                {
                    resultList = nativeActions.Take(aNeedCount).ToList();
                }
                else
                {
                    resultList = nativeActions;
                }
                return ConvertToActionModel(resultList);
            }
            else
            {
                var nativeActionModels = ConvertToActionModel(nativeActions);
                try
                {
                    var aiActions = await ResolveAIActions(nativeActionModels);
                    if (aiActions != null && aiActions.Any())
                    {
                        return ConvertToActionModel(aiActions);
                    }
                    else
                    {
                        logger.LogError("ResolveAIActions return Nothing!");
                        return nativeActionModels;
                    }
                }
                catch(Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ResolveAIActions Error");
                    return nativeActionModels;
                }
            }
        }
        public async Task<List<ExecuteAction>> ResolveNativeAction()
        {
            try
            {
                if (m_ActionParam.ElementType != Params.ElementType.Ordinary)
                {
                    var result = (await m_SelectTask);
                    JObject cvDataJO = result.CvElementAdditionJO;
                    var cvAnalyzer = m_ElementClassAnalyzers.Select(a=>a.Value).FirstOrDefault();
                    if (await ExecuteCVAnalyzers(cvAnalyzer, cvDataJO))
                    {
                        return CacheResolvedActions;
                    }
                }
                else
                {
                    bool elementClassIsMatchCondition = false;
                    try
                    {
                        var additionalAnalyzer = ResolveAdditionalAnalyzer().FirstOrDefault();
                        if (additionalAnalyzer is AnalyzerForCv analyzer)
                        {
                            var result = await analyzer.TryExecuteAnalyzerAsAddition(m_ActionParam);
                            {
                                logger.LogInformation($"Element analyze success:{analyzer.AnalyzerName}");
                                if (result != null
                                && result.GetValueOrDefault().RecommendActionExecutionParams != null
                                && result.GetValueOrDefault().RecommendActionExecutionParams.Any()
                                && AdditionalResolvedActionParams != null
                                && AdditionalResolvedActionParams.Any())
                                {
                                    AdditionalResolvedActionParams = result.GetValueOrDefault().RecommendActionExecutionParams;
                                }
                            }
                            AdditionalResolvedActions = result.Value.RecommendActionExecutions;
                            ElementClassResult = result.Value.ElementClass;
                            elementClassIsMatchCondition = result.Value.IsMatchCondition;
                        }
                        else
                        {
                            logger.LogError("ElementClass was executed failed!");
                            elementClassIsMatchCondition = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ElementClass was executed failed!");
                        elementClassIsMatchCondition = false;
                    }
                    if (elementClassIsMatchCondition)
                    {
                        m_SelectTask.ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
                        CacheResolvedActions = AdditionalResolvedActions;
                        CurrentResolvedActionParams = AdditionalResolvedActionParams;
                        logger.LogInformation($"Pick AdditionalResolvedActions:{AdditionalResolvedActions.Count}.Because of condition is true");
                        return CacheResolvedActions;
                    }
                    else
                    {
                        var slimAnalyzers = ResolveSlimAnalyzer();
                        logger.LogInformation($"Number of slimAnalyzers:{slimAnalyzers.Count()}");
                        if (ExecuteAnalyzers(slimAnalyzers))
                        {
                            m_SelectTask.ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
                            return CacheResolvedActions;
                        }
                        else
                        {
                            var analyzers = ResolveAnalyzer();
                            if (analyzers.Any())
                            {
                                var watchTask = Task.Delay(TimeSpan.FromMilliseconds(3000));
                                var resutTask = await Task.WhenAny(watchTask, m_SelectTask);
                                if (m_SelectTask.IsCompleted)
                                {
                                    var result = (await m_SelectTask);
                                    if (!result.IsValid)
                                    {
                                        throw new InvalidOperationException($"Select Task Result:{result.IsValid}");
                                    }
                                    JObject selectorJO = JObject.Parse(result.Selector);
                                    logger.LogInformation($"Number of analyzers:{analyzers.Count()}");
                                    if (ExecuteAnalyzers(analyzers, selectorJO))
                                    {
                                        return CacheResolvedActions;
                                    }
                                    else
                                    {
                                        logger.LogWarning("All analyzers were executed failed!Use additional instead");
                                        if (AdditionalResolvedActions != null && AdditionalResolvedActions.Any())
                                        {
                                            CacheResolvedActions = AdditionalResolvedActions;
                                            CurrentResolvedActionParams = AdditionalResolvedActionParams;
                                            logger.LogInformation($"Pick AdditionalResolvedActions:{AdditionalResolvedActions.Count}");
                                            return CacheResolvedActions;
                                        }
                                    }
                                }
                                else
                                {
                                    logger.LogWarning("SelectTask timeout!Use additional instead");
                                    if (AdditionalResolvedActions != null && AdditionalResolvedActions.Any())
                                    {
                                        CacheResolvedActions = AdditionalResolvedActions;
                                        CurrentResolvedActionParams = AdditionalResolvedActionParams;
                                        logger.LogInformation($"Pick AdditionalResolvedActions:{AdditionalResolvedActions.Count}");
                                        return CacheResolvedActions;
                                    }
                                }
                            }
                            else
                            {
                                logger.LogWarning("None of analyzer was registered!Use additional instead");
                                if (AdditionalResolvedActions != null && AdditionalResolvedActions.Any())
                                {
                                    CacheResolvedActions = AdditionalResolvedActions;
                                    CurrentResolvedActionParams = AdditionalResolvedActionParams;
                                    logger.LogInformation($"Pick AdditionalResolvedActions:{AdditionalResolvedActions.Count}");
                                    return CacheResolvedActions;
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return CacheResolvedActions;

        }
        private bool ExecuteAnalyzers(IEnumerable<IAnalyzer> analyzers)
        {
            if (analyzers != null && analyzers.Any())
            {
                analyzers = analyzers.OrderBy(a => a.ExecutePriority);
                foreach (var analyzer in analyzers)
                {
                    try
                    {
                        if (analyzer.TryExecuteAnalyzer(m_ActionParam,
                            out List <ExecuteAction> aResolvedActions,
                            out Dictionary<string, string> aResolvedActionParams))
                        {
                            logger.LogInformation($"Element analyze success:{analyzer.AnalyzerName}");
                            CacheResolvedActions = aResolvedActions;
                            CurrentResolvedActionParams = aResolvedActionParams;
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex);
                    }
                }
            }
            return false;
        }
        private bool ExecuteAnalyzers(IEnumerable<IAnalyzer> analyzers,JObject selectorJO)
        {
            if (analyzers != null && analyzers.Any())
            {
                analyzers = analyzers.OrderBy(a => a.ExecutePriority);
                foreach (var analyzer in analyzers)
                {
                    try
                    {
                        if (analyzer.TryExecuteAnalyzer(selectorJO,
                            m_ActionParam,
                            m_ElementCacheProvider,
                            out List<ExecuteAction> aResolvedActions,
                            out Dictionary<string, string> aResolvedActionParams))
                        {
                            logger.LogInformation($"Element analyze success:{analyzer.AnalyzerName}");
                            CacheResolvedActions = aResolvedActions;
                            CurrentResolvedActionParams = aResolvedActionParams;
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex);
                    }
                }
            }
            return false;
        }
        private async Task<bool> ExecuteCVAnalyzers(IAnalyzer analyzer, JObject selectorJO)
        {
            if (analyzer is AnalyzerForCv analyzerCv)
            {
                var result = await analyzerCv.TryExecuteAnalyzer(selectorJO, m_ActionParam);
                logger.LogInformation($"Element analyze success:{analyzer.AnalyzerName}");
                CacheResolvedActions = result.GetValueOrDefault().RecommendActionExecutions;
                ElementClassResult = result.GetValueOrDefault().ElementClass;
                logger.LogInformation($"Element_Class:{ElementClassResult}");
                return true;
            }
            return false;
        }
        private async Task<List<ExecuteAction>> ResolveAIActions()
        {
            JObject selectorJO = new JObject();
            if (!m_ActionParam.ProcessName.ToLower().StartsWith("dingtalk"))
            {
                var watchTask = Task.Delay(TimeSpan.FromMilliseconds(2000));
                var resutTask = await Task.WhenAny(watchTask, m_SelectTask);
                if (resutTask is Task<SelectElementModel> resutSelectTask)
                {
                    var slectorResult = (await resutSelectTask);
                    selectorJO = JObject.Parse(slectorResult.Selector);
                }
            }
            if (AIAnalyzer is AnalyzerAI analyzerAI)
            {
                var result = await analyzerAI.ExecuteAnalyzer(selectorJO,
                    m_ActionParam,
                    m_Components);
                {
                    if (result.GetValueOrDefault().RecommendActionExecutions != null)
                    {
                        logger.LogInformation($"ResolveAIActions:{result.GetValueOrDefault().RecommendActionExecutions.Count}");
                    }
                    return result.GetValueOrDefault().RecommendActionExecutions;
                }
            }
            else
            {
                return new List<ExecuteAction>();
            }
        }
        private async Task<List<ExecuteAction>> ResolveAIActions(IEnumerable<ActionModel> anNativeActions)
        {
            var slectorResult = (await m_SelectTask);
            JObject selectorJO = JObject.Parse(slectorResult.Selector);
            if (AIAnalyzer is AnalyzerAI analyzerAI)
            {
                var result = await analyzerAI.ExecuteAnalyzerWithNativeActions(selectorJO,
                    m_ActionParam,
                    m_Components,
                    anNativeActions);
                //var result = await analyzerAI.ExecuteAnalyzer(selectorJO,
                //    m_ActionParam,
                //    m_Components);
                {
                    if (result.GetValueOrDefault().RecommendActionExecutions != null)
                    {
                        logger.LogInformation($"ResolveAIActions:{result.GetValueOrDefault().RecommendActionExecutions.Count}");
                    }
                    return result.GetValueOrDefault().RecommendActionExecutions;
                }
            }
            else
            {
                return new List<ExecuteAction>();
            }
        }

        private IEnumerable<ActionModel> ConvertToActionModel(List<ExecuteAction> anExecuteActions)
        {
            IEnumerable<ActionModel> resolvedActions = null;
            Dispatcher.CurrentDispatcher.Invoke(() => { });
            if (ElementClassResult == "登录区域" && RPAEnvironment.GetInstance().CurrentLanguage == LanguageCultureEnum.zh_CN)
            {
                resolvedActions = ActionDataModels.ConvertToActionModel(new string[] { LocalDataModels.Login_Comp_Type });
            }
            //else if (CurrentResolvedActions.First().ActionId == "excel.record")
            //{
            //    m_ResolvedActions = ActionDataModels.ConvertToActionModel(new string[] { "excel.record" });
            //    SetActionCollectionView(m_ResolvedActions);
            //}
            else
            {
                if (anExecuteActions != null && anExecuteActions.Count > 0)
                {
                    resolvedActions = ActionDataModels.ConvertToActionModel(anExecuteActions);
                }
                else
                {
                    logger.LogError("CurrentResolvedActions is null");
                }
                if (resolvedActions == null || !resolvedActions.Any())
                {
                    logger.LogError($"all analyzer execute failed:{m_ActionParam.GuiType}.use default instead");
                    resolvedActions = new List<ActionModel>();
                    logger.LogDebug($"DefaultRecommendActions:{ActionDataModels.DefaultRecommendActions.Count}");
                    var defaultResult = ActionDataModels.ConvertToActionModel(ActionDataModels.DefaultRecommendComponentNames);
                    if (defaultResult != null && defaultResult.Any())
                    {
                        resolvedActions = defaultResult;
                    }
                }
            }
            return resolvedActions;
        }
    }
    public sealed class AnalyzerEqualityComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
}

