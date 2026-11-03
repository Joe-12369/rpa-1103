namespace II.RPA.WinAutomation.CommonUtility
{
    /// <summary>
    /// 普通文本码 简称文本码
    /// 用于将代码中写死的中文替换成枚举值相关 以进一步处理多语言环境下文字的内容显示
    /// 适用情况： 本地显示，会被用户看到 同时不需要传code给engine或者前端 如果遇到需要传递给engine或者前端的话 走错误码！！！ eErrorMessageType
    /// 仅仅是本地打日志的话 中文不需要替换！ 直接写中文就行！
    /// 历史原因文本码是从1000开始的 你可以从0001开始添加
    /// </summary>
    #region 文本码
    public enum eCommonMessageType : int
    {
        /// <summary>
        /// 
        /// </summary>
        Error_None = 0000,

        ////1000开始为国际化项目统计的文本码(不一定是报错语) 批量抓取生成 有可能没有被引用到

        /// <summary>
        /// 开启拾取
        /// </summary>
        InspectorStatusForm_cs_KaiQiShiQu = 1000,

        /// <summary>
        /// 关闭拾取
        /// </summary>
        InspectorStatusForm_cs_GuanBiShiQu = 1001,

        /// <summary>
        /// 坐标位置
        /// </summary>
        InspectorStatusForm_Designer_cs_ZuoBiaoWeiZhi = 1002,

        /// <summary>
        /// 窗口类型
        /// </summary>
        InspectorStatusForm_Designer_cs_ChuangKouLeiXing = 1003,

        /// <summary>
        /// 退出拾取
        /// </summary>
        InspectorStatusForm_Designer_cs_TuiChuShiQu = 1004,

        /// <summary>
        /// 显示
        /// </summary>
        OverlayForm_cs_XianShi = 1005,

        /// <summary>
        /// 打开
        /// </summary>
        OverlayForm_cs_DaKai = 1006,

        /// <summary>
        /// 另存为
        /// </summary>
        OverlayForm_cs_LingCunWei = 1007,

        /// <summary>
        /// 上传
        /// </summary>
        OverlayForm_cs_ShangChuan = 1008,

        /// <summary>
        /// 打印
        /// </summary>
        OverlayForm_cs_DaYin = 1009,

        /// <summary>
        /// 想获得以下权限：显示通知
        /// </summary>
        OverlayForm_cs_XiangHuoDeYiXiaQuanXianXianShi = 1010,

        /// <summary>
        /// CV元素解析失败，请检查服务器是否正常连接
        /// </summary>
        OverlayForm_cs_CVYuanSuJieXiShiBaiQing = 1011,

        /// <summary>
        /// 用户取消
        /// </summary>
        SDK_cs_YongHuQuXiao = 1012,

        /// <summary>
        /// 设置成功
        /// </summary>
        SDK_cs_SheZhiChengGong = 1013,

        /// <summary>
        /// 加载完成
        /// </summary>
        SDK_cs_JiaZaiWanCheng = 1014,

        /// <summary>
        /// 在“解锁”时未检测到admintransfer，请排查原因
        /// </summary>
        SupportSocketClient_cs_ZaiJieSuoShiWeiJianCeDao = 1015,

        /// <summary>
        /// 在“解锁”时已检测到admintransfer，但无法使用，可以关闭ii.rpa.admintransfer进程后重试
        /// </summary>
        SupportSocketClient_cs_ZaiJieSuoShiYiJianCeDao = 1016,

        /// <summary>
        /// Socket通信超时失败，请确认是否开启解锁程序
        /// </summary>
        SupportSocketClient_cs_SocketTongXinChaoShi = 1017,

        /// <summary>
        /// 屏幕解锁可能异常，注意
        /// </summary>
        SupportSocketClient_cs_PingMuJieSuoKeNengYiChangZhu = 1018,

        /// <summary>
        /// 智能章鱼正在识别中…
        /// </summary>
        WaitingWnd_xaml_cs_ZhiNengZhangYuZhengZaiShiBieZhong = 1019,

        /// <summary>
        /// 文件不存在
        /// </summary>
        ChromeExtensionProvider_cs_WenJianBuCunZai = 1020,

        /// <summary>
        /// 保留
        /// </summary>
        ChromeNode_cs_BaoLiu = 1021,

        /// <summary>
        /// 确认另存为
        /// </summary>
        ChromeNode_cs_QueRenLingCunWei = 1022,

        /// <summary>
        /// 是(Y)
        /// </summary>
        ChromeNode_cs_ShiY = 1023,

        /// <summary>
        /// 文件名:
        /// </summary>
        ChromeNode_cs_WenJianMing = 1024,

        /// <summary>
        /// 保存(S)
        /// </summary>
        ChromeNode_cs_BaoCunS = 1025,

        /// <summary>
        /// 数据返回错误
        /// </summary>
        ChromeTab_cs_ShuJuFanHuiCuoWu = 1026,

        /// <summary>
        /// 企业微信
        /// </summary>
        CVCapture_cs_QiYeWeiXin = 1027,

        /// <summary>
        /// 确定
        /// </summary>
        CVScreenWnd_Designer_cs_QueDing = 1028,

        /// <summary>
        /// 通道客户端连接状态异常
        /// </summary>
        HIDSender_cs_TongDaoKeHuDuanLianJieZhuangTaiYi = 1029,

        /// <summary>
        /// HIDTool 通信失败，请确认是否开启
        /// </summary>
        HIDSender_cs_HIDToolTongXin = 1030,

        /// <summary>
        /// ClearAllText 异常
        /// </summary>
        HIDSender_cs_ClearAllTe = 1031,

        /// <summary>
        /// HIDTool 通信 发送文本失败，请确认是否开启
        /// </summary>
        HIDSender_cs_HIDToolTongXin1 = 1032,

        /// <summary>
        /// SendText 异常
        /// </summary>
        HIDSender_cs_SendTextYi = 1033,

        /// <summary>
        /// 查询切换网页有问题，可能找不到该Title的Tab页；或者默认当前窗体，而当前窗体非网页
        /// </summary>
        JavaScriptExecutor_cs_ChaXunQieHuanWangYeYouWenTi = 1034,

        /// <summary>
        /// JS 运行失败，语法问题或者IE内核对某些JS函数不兼容
        /// </summary>
        JavaScriptExecutor_cs_JSYunXingShiBaiYuFa = 1035,

        /// <summary>
        /// 注入JS Code失败，可能JS Code入口格式有问题
        /// </summary>
        JavaScriptExecutor_cs_ZhuRuJSCodeShi = 1036,

        /// <summary>
        /// 查询切换网页有问题，可能找不到该Title的Tab页；或者浏览器已崩溃
        /// </summary>
        JavaScriptExecutor_cs_ChaXunQieHuanWangYeYouWenTi1 = 1037,

        /// <summary>
        /// 空白页
        /// </summary>
        JavaScriptExecutor_cs_KongBaiYe = 1038,

        /// <summary>
        /// 是否启用Java Access Bridge？
        /// </summary>
        JavaNode_cs_ShiFouQiYongJavaA = 1039,

        /// <summary>
        /// 询问
        /// </summary>
        JavaNode_cs_XunWen = 1040,

        /// <summary>
        /// 操作完成，请重新运行Java程序以便操作生效
        /// </summary>
        JavaNode_cs_CaoZuoWanChengQingZhongXinYunXing = 1041,

        /// <summary>
        /// 提示
        /// </summary>
        JavaNode_cs_TiShi = 1042,

        /// <summary>
        /// 微信
        /// </summary>
        UIANode_cs_WeiXin = 1043,

        /// <summary>
        /// 微信文件
        /// </summary>
        UIANode_cs_WeiXinWenJian = 1044,

        /// <summary>
        /// 看一看
        /// </summary>
        UIANode_cs_KanYiKan = 1045,

        /// <summary>
        /// 钉钉
        /// </summary>
        UIANode_cs_DingDing = 1046,

        /// <summary>
        /// 需要提供有效的参数
        /// </summary>
        RDPLock_cs_XuYaoTiGongYouXiaoDeCanShu = 1047,

        /// <summary>
        /// 屏幕解锁失败,未匹配到合适的会话
        /// </summary>
        RDPUnlock_cs_PingMuJieSuoShiBaiWeiPiPei = 1048,

        /// <summary>
        /// 屏幕解锁成功
        /// </summary>
        RDPUnlock_cs_PingMuJieSuoChengGong = 1049,

        /// <summary>
        /// 屏幕解锁 过程中发生了异常,详情请参考日志
        /// </summary>
        RDPUnlock_cs_PingMuJieSuoGuoChengZhongFaSheng = 1050,

        /// <summary>
        /// 杭州实在智能科技有限公司
        /// </summary>
        CoreAssemblyInfo_cs_HangZhouShiZaiZhiNengKeJiYouXian = 1051,

        /// <summary>
        /// ©2020-2022 杭州实在智能科技有限公司.All rights reserved.
        /// </summary>
        CoreAssemblyInfo_cs_20202022 = 1052,

        /// <summary>
        /// 获取命令行参数失败！
        /// </summary>
        CommandArgHelper_cs_HuoQuMingLingXingCanShuShiBai = 1053,

        /// <summary>
        /// {0}:{2}/{3}MB/{4}MB/{1}%可用;
        /// </summary>
        SystemInfoUtility_cs_023 = 1054,

        /// <summary>
        /// 机器名:{Environment.MachineName};
        /// </summary>
        SystemInfoUtility_cs_JiQiMing = 1055,

        /// <summary>
        /// {Environment.NewLine}操作系统:{Environment.OSVersion};
        /// </summary>
        SystemInfoUtility_cs_Cuozuoxitong = 1056,

        /// <summary>
        /// {Environment.NewLine}系统文件夹:{Environment.SystemDirectory};
        /// </summary>
        SystemInfoUtility_cs_Xitongwenjianjia = 1057,

        /// <summary>
        /// {Environment.NewLine}语言:{CultureInfo.InstalledUICulture.EnglishName};
        /// </summary>
        SystemInfoUtility_cs_Yuyan = 1058,

        /// <summary>
        /// {Environment.NewLine}当前目录:{Environment.CurrentDirectory};
        /// </summary>
        SystemInfoUtility_cs_Dangqianmulu = 1059,

        /// <summary>
        /// {Environment.NewLine}当前用户:{Environment.UserName};
        /// </summary>
        SystemInfoUtility_cs_Dangqianyonghu = 1060,

        /// <summary>
        /// 内存
        /// </summary>
        SystemInfoUtility_cs_NeiCun = 1061,

        /// <summary>
        /// 频率:
        /// </summary>
        SystemInfoUtility_cs_PinLu = 1062,

        /// <summary>
        /// 总物理内存:
        /// </summary>
        SystemInfoUtility_cs_ZongWuLiNeiCun = 1063,

        /// <summary>
        /// 总可用内存:
        /// </summary>
        SystemInfoUtility_cs_ZongKeYongNeiCun = 1064,

        /// <summary>
        /// {0:F2}%可用;
        /// </summary>
        SystemInfoUtility_cs_0F2KeYong = 1065,

        /// <summary>
        /// 厂商:
        /// </summary>
        SystemInfoUtility_cs_ChangShang = 1066,

        /// <summary>
        /// 产品名称:
        /// </summary>
        SystemInfoUtility_cs_ChanPinMingChen = 1067,

        /// <summary>
        /// 最大频率:
        /// </summary>
        SystemInfoUtility_cs_ZuiDaPinLu = 1068,

        /// <summary>
        /// 当前频率:
        /// </summary>
        SystemInfoUtility_cs_DangQianPinLu = 1069,

        /// <summary>
        /// 系统异常，请重试。若仍未解决，请联系运营人员。
        /// </summary>
        PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo = 1070,

        /// <summary>
        /// 请求失败，请重试。若仍未解决，请联系运营人员。
        /// </summary>
        PluginGRPCService_cs_QingQiuShiBaiQingZhongShiRuo = 1071,

        /// <summary>
        /// 未初始化！
        /// </summary>
        VisualDataGRPCService_cs_WeiChuShiHua = 1072,

        /// <summary>
        /// 管道名称不能为Null或者为空！
        /// </summary>
        IPCClient_cs_GuanDaoMingChenBuNengWeiNul = 1073,

        /// <summary>
        /// 发送数据不能为空！
        /// </summary>
        IPCClient_cs_FaSongShuJuBuNengWeiKong = 1074,

        /// <summary>
        /// 回调服务不能为空！
        /// </summary>
        IPCServer_cs_HuiDiaoFuWuBuNengWeiKong = 1075,

        /// <summary>
        /// [Request]参数为空，无法执行空操作！
        /// </summary>
        PluginIPCService_cs_RequestCan = 1076,

        /// <summary>
        /// [Request]业务执行操作为空，无法执行！
        /// </summary>
        PluginIPCService_cs_RequestYe = 1077,

        /// <summary>
        /// [Request]IRequestParams.Parameters 无法转换为 PluginIPCInitializeRequest，Json字符串异常!
        /// </summary>
        PluginInitializeService_cs_RequestI = 1078,

        /// <summary>
        /// [Request]PluginIPCInitializeRequest为Null,或者 PackagesPath为Null，或者 Action为Null
        /// </summary>
        PluginInitializeService_cs_RequestP = 1079,

        /// <summary>
        /// 路径[{receivedMessage.PackagesPath}]不存在,或者格式不正确
        /// </summary>
        PluginInitializeService_cs_LuJingreceiv = 1080,

        /// <summary>
        /// [Request]编号[{request.GetHashCode()}]执行器为空...
        /// </summary>
        PluginInitializeService_cs_RequestBian = 1081,

        /// <summary>
        /// [Request]IRequestParams.Parameters 无法转换为 PluginIPCRequest，Json字符串异常!
        /// </summary>
        PluginRunService_cs_RequestI = 1082,

        /// <summary>
        /// [Request]PluginIPCRequest为Null,或者 PluginName为Null，或者 Action为Null
        /// </summary>
        PluginRunService_cs_RequestP = 1083,

        /// <summary>
        /// [Request]IRequestParams.Parameters 无法转换为 PluginIPCShareValueRequest，Json字符串异常!
        /// </summary>
        PluginShareValueService_cs_RequestI = 1084,

        /// <summary>
        /// [Request]PluginIPCShareValueRequest为Null,或者 JsonParam为Null
        /// </summary>
        PluginShareValueService_cs_RequestP = 1085,

        /// <summary>
        /// 订阅者不能为空
        /// </summary>
        MessageBroker_cs_DingYueZheBuNengWeiKong = 1086,

        /// <summary>
        /// 消息键不能为空
        /// </summary>
        MessageBroker_cs_XiaoXiJianBuNengWeiKong = 1087,

        /// <summary>
        /// 消息不能为空
        /// </summary>
        MessageBroker_cs_XiaoXiBuNengWeiKong = 1088,

        /// <summary>
        /// 通讯消息订阅超时，超时时间为:{timeout}.
        /// </summary>
        MessageBroker_cs_TongXunXiaoXiDingYueChaoShiChao = 1089,

        /// <summary>
        /// 拾取失败
        /// </summary>
        BaseActionProvider_cs_ShiQuShiBai = 1090,

        /// <summary>
        /// 返回值解析失败，请重试。若仍未解决，请联系运营人员。
        /// </summary>
        BaseActionProvider_cs_FanHuiZhiJieXiShiBaiQingZhong = 1091,

        /// <summary>
        /// HID驱动未安装
        /// </summary>
        CheckDriveProvider_cs_HIDQuDongWeiAnZhuang = 1092,

        /// <summary>
        /// 已安装
        /// </summary>
        CheckDriveProvider_cs_YiAnZhuang = 1093,

        /// <summary>
        /// 数据爬取已取消
        /// </summary>
        CollectDataProvider_cs_ShuJuPaQuYiQuXiao = 1094,

        /// <summary>
        /// 文件格式错误，不是标准的Sqlite数据库文件
        /// </summary>
        DBTestProvider_cs_WenJianGeShiCuoWuBuShiBiao = 1095,

        /// <summary>
        /// 文件格式错误，不是标准的Access数据库文件
        /// </summary>
        DBTestProvider_cs_WenJianGeShiCuoWuBuShiBiao1 = 1096,

        /// <summary>
        /// 兼容性问题
        /// </summary>
        UI_Selector_Combo_Provider_cs_JianRongXingWenTi = 1097,

        /// <summary>
        /// selector格式有误
        /// </summary>
        UI_Selector_CS_Provider_cs_selectorGeShi = 1098,

        /// <summary>
        /// selector解析失败
        /// </summary>
        UI_Selector_CS_Provider_cs_selectorJieXi = 1099,

        /// <summary>
        /// 操作超时
        /// </summary>
        ConcreteCommand_cs_CaoZuoChaoShi = 1100,

        /// <summary>
        /// 执行失败,请重试!
        /// </summary>
        PluginLoaderRunCommand_cs_ZhiXingShiBaiQingZhongShi = 1101,

        /// <summary>
        /// [ReceiveHandle]参数为空，无法执行空操作！
        /// </summary>
        IPCService_cs_ReceiveHa = 1102,

        /// <summary>
        /// [ReceiveHandle]请求参数非法！
        /// </summary>
        IPCService_cs_ReceiveHa1 = 1103,

        /// <summary>
        /// [ReceiveHandle]业务执行操作为空，无法执行！
        /// </summary>
        IPCService_cs_ReceiveHa2 = 1104,

        /// <summary>
        /// [ReceiveHandle]无法处理的业务动作:({actionRequest.Action})！
        /// </summary>
        IPCService_cs_ReceiveHa3 = 1105,

        /// <summary>
        /// [Request]请求参数非法！
        /// </summary>
        IPCService_cs_RequestQing = 1106,

        /// <summary>
        /// 参数[anInitParam]不能空!
        /// </summary>
        PluginProcessFacade_cs_CanShuanInitP = 1107,

        /// <summary>
        /// 插件进程没有启动!
        /// </summary>
        PluginProcessFacade_cs_ChaJianJinChengMeiYouQiDong = 1108,

        /// <summary>
        /// 插件进程正在初始化，无法执行重复请求!
        /// </summary>
        PluginProcessFacade_cs_ChaJianJinChengZhengZaiChuShiHua = 1109,

        /// <summary>
        /// 插件进程已经初始化，无效请求!
        /// </summary>
        PluginProcessFacade_cs_ChaJianJinChengYiJingChuShiHua = 1110,

        /// <summary>
        /// 初始化执行返回结果不能空!
        /// </summary>
        PluginProcessFacade_cs_ChuShiHuaZhiXingFanHuiJieGuoBu = 1111,

        /// <summary>
        /// 参数[anRunParam]不能空!
        /// </summary>
        PluginProcessFacade_cs_CanShuanRunPa = 1112,

        /// <summary>
        /// 插件进程[状态:{this.Status.ToString()}]没有启动或者初始化!
        /// </summary>
        PluginProcessFacade_cs_ChaJianJinChengZhuangTait = 1113,

        /// <summary>
        /// 运行执行返回结果不能空!
        /// </summary>
        PluginProcessFacade_cs_YunXingZhiXingFanHuiJieGuoBuNeng = 1114,

        /// <summary>
        /// KillProcessBy方法中，进程名称不能为空
        /// </summary>
        PluginProcessFacade_cs_KillProces = 1115,

        /// <summary>
        /// 正在初始化、请稍后重试
        /// </summary>
        PluginProcessProvider_cs_ZhengZaiChuShiHuaQingShaoHouZhong = 1116,

        /// <summary>
        /// 设置的超时时间过短，请重新调整超时时间
        /// </summary>
        PluginProcessProvider_cs_SheZhiDeChaoShiShiJianGuoDuan = 1117,

        /// <summary>
        /// 操作失败，已超时，请增加属性中的时间值。
        /// </summary>
        PluginProcessProvider_cs_CaoZuoShiBaiYiChaoShiQing = 1118,

        /// <summary>
        /// 准备启动IPC服务器
        /// </summary>
        PluginServiceConnection_cs_ZhunBeiQiDongIPCFuWuQi = 1119,

        /// <summary>
        /// 启动上下文:
        /// </summary>
        PluginServiceConnection_cs_QiDongShangXiaWen = 1120,

        /// <summary>
        /// 启动IPC服务器成功
        /// </summary>
        PluginServiceConnection_cs_QiDongIPCFuWuQiChengGong = 1121,

        /// <summary>
        /// 准备关闭IPC服务器
        /// </summary>
        PluginServiceConnection_cs_ZhunBeiGuanBiIPCFuWuQi = 1122,

        /// <summary>
        /// 关闭上下文:
        /// </summary>
        PluginServiceConnection_cs_GuanBiShangXiaWen = 1123,

        /// <summary>
        /// 已经关闭IPC服务器
        /// </summary>
        PluginServiceConnection_cs_YiJingGuanBiIPCFuWuQi = 1124,

        /// <summary>
        /// CancellationTokenSource 尚未初始化
        /// </summary>
        CtsTimeout_cs_Cancellati = 1125,

        /// <summary>
        /// 截断
        /// </summary>
        TestFunction_cs_JieDuan = 1126,

        /// <summary>
        /// [{this.facade.CurrentPluginProcessId}]进程不存在
        /// </summary>
        PluginProcessFacadeTests_cs_thisfac = 1127,

        /// <summary>
        /// 进程尚未复活
        /// </summary>
        PluginProcessFacadeTests_cs_JinChengShangWeiFuHuo = 1128,

        /// <summary>
        /// 进程尚未复活old:{oldPid},new:{this.facade.CurrentPluginProcessId}
        /// </summary>
        PluginProcessFacadeTests_cs_JinChengShangWeiFuHuoold = 1129,

        /// <summary>
        /// CPU信息
        /// </summary>
        Program_cs_CPUXinXi = 1130,

        /// <summary>
        /// 操作系统信息
        /// </summary>
        Program_cs_CaoZuoXiTongXinXi = 1131,

        /// <summary>
        /// 内存序列号
        /// </summary>
        Program_cs_NeiCunXuLieHao = 1132,

        /// <summary>
        /// 磁盘序列号
        /// </summary>
        Program_cs_CiPanXuLieHao = 1133,

        /// <summary>
        /// 内存信息
        /// </summary>
        Program_cs_NeiCunXinXi = 1134,

        /// <summary>
        /// SQL语句参数错误
        /// </summary>
        Db2_cs_SQLYuJuCanShuCuoWu = 1135,

        /// <summary>
        /// 不支持的入参类型
        /// </summary>
        DbHelper_cs_BuZhiChiDeRuCanLeiXing = 1136,

        /// <summary>
        /// 无效的数据库连接
        /// </summary>
        DbHelper_cs_WuXiaoDeShuJuKuLianJie = 1137,

        /// <summary>
        /// JS文件不存在:{0}
        /// </summary>
        CaptureInterface_cs_JSWenJianBuCunZai0 = 1138,

        /// <summary>
        /// 警告
        /// </summary>
        CaptureInterface_cs_JingGao = 1139,

        /// <summary>
        /// 采集页加载显示有误，请确保该目标采集页为当前屏幕窗体!
        /// </summary>
        CaptureInterface_cs_CaiJiYeJiaZaiXianShiYouWu = 1140,

        /// <summary>
        /// 人为中止采集配置过程或者采集配置过程报错中止！
        /// </summary>
        CaptureInterface_cs_RenWeiZhongZhiCaiJiPeiZhiGuoCheng = 1141,

        /// <summary>
        /// 请将采集页置于屏幕上,并且点击配置页的保存配置按钮!
        /// </summary>
        CheckIEWeb_cs_QingJiangCaiJiYeZhiYuPingMuShang = 1142,

        /// <summary>
        /// 采集页加载显示有误，请确保目标采集页为当前屏幕窗体!
        /// </summary>
        CheckIEWeb_cs_CaiJiYeJiaZaiXianShiYouWu = 1143,

        /// <summary>
        /// 数据采集
        /// </summary>
        DataCaptureForm_cs_ShuJuCaiJi = 1144,

        /// <summary>
        /// 配置页加载显示有误，请确保IE8及以上内核!
        /// </summary>
        DataCaptureForm_cs_PeiZhiYeJiaZaiXianShiYouWu = 1145,

        /// <summary>
        /// 配置页加载显示有误!
        /// </summary>
        DataCaptureForm_cs_PeiZhiYeJiaZaiXianShiYouWu1 = 1146,

        /// <summary>
        /// 配置页通信或者Rule格式有误，请确保IE8及以上内核且Rule格式正确!
        /// </summary>
        DataCaptureForm_cs_PeiZhiYeTongXinHuoZheRul = 1147,

        /// <summary>
        /// ./dll/ElementSDKV1/II.RPA/数据采集关闭White.png
        /// </summary>
        DataCaptureForm_cs_dllElem = 1148,

        /// <summary>
        /// ./dll/ElementSDKV1/II.RPA/数据采集关闭.png
        /// </summary>
        DataCaptureForm_cs_dllElem1 = 1149,

        /// <summary>
        /// 关闭
        /// </summary>
        DataCaptureForm_cs_GuanBi = 1150,

        /// <summary>
        /// 最小化
        /// </summary>
        DataCaptureForm_cs_ZuiXiaoHua = 1151,

        /// <summary>
        /// 宋体
        /// </summary>
        DataCaptureForm_designer_cs_SongTi = 1152,

        /// <summary>
        /// JS 运行失败，原因为{0}
        /// </summary>
        JavaScriptExecutor_cs_JSYunXingShiBaiYuanYin = 1153,

        /// <summary>
        /// 采集页加载显示有误，请确保该目标采集页存在，未关闭!
        /// </summary>
        ReadExecutorJS_cs_CaiJiYeJiaZaiXianShiYouWu = 1154,

        /// <summary>
        /// 执行JS失败，考虑IE内核不兼容或JS函数语法有误!
        /// </summary>
        ReadExecutorJS_cs_ZhiXingJSShiBaiKaoLuI = 1155,

        /// <summary>
        /// 请确保该目标采集页为当前屏幕窗体，并且为IE8及以上内核，刷新网页重试!
        /// </summary>
        ResizeIEFrame_cs_QingQueBaoGaiMuBiaoCaiJiYeWei = 1156,

        /// <summary>
        /// 规则不能为空数组\
        /// </summary>
        ResolveUpMessage_cs_GuiZeBuNengWeiKongShuZu = 1157,

        /// <summary>
        /// 规则不能为空数组!
        /// </summary>
        ResolveUpMessage_cs_GuiZeBuNengWeiKongShuZu1 = 1158,

        /// <summary>
        /// 确定关闭?关闭后无法进行java元素拾取和操作
        /// </summary>
        IIRPAJavaHost_cs_QueDingGuanBiGuanBiHouWuFa = 1159,

        /// <summary>
        /// 退出
        /// </summary>
        IIRPAJavaHost_Designer_cs_TuiChu = 1160,

        /// <summary>
        /// 设置输入法状态失败
        /// </summary>
        JavaExecutor_cs_SheZhiShuRuFaZhuangTaiShiBai = 1161,

        /// <summary>
        /// 中文
        /// </summary>
        JavaExecutor_cs_ZhongWen = 1162,

        /// <summary>
        /// 英文
        /// </summary>
        JavaExecutor_cs_YingWen = 1163,

        /// <summary>
        /// 获取输入法状态失败
        /// </summary>
        JavaExecutor_cs_HuoQuShuRuFaZhuangTaiShiBai = 1164,

        /// <summary>
        /// 不存在子元素
        /// </summary>
        JavaNode_cs_BuCunZaiZiYuanSu = 1165,

        /// <summary>
        /// 单击
        /// </summary>
        JavaNode_cs_DanJi = 1166,

        /// <summary>
        /// 次数
        /// </summary>
        JavaNode_cs_CiShu = 1167,

        /// <summary>
        /// 内容已截断
        /// </summary>
        Extension_cs_NeiRongYiJieDuan = 1168,

        /// <summary>
        /// 加载日志配置出错，使用了默认的日志配置
        /// </summary>
        Logger_cs_JiaZaiRiZhiPeiZhiChuCuoShi = 1169,

        /// <summary>
        /// 应用程序检测到配置文件变更，处理注册的回调程序发生了异常
        /// </summary>
        AppConfigWatcher_cs_YingYongChengXuJianCeDaoPeiZhiWen = 1170,

        /// <summary>
        /// 应用程序准备重新装载配置。等待配置可用，已经超过了设定的延迟{msDelay}
        /// </summary>
        AppConfigWatcher_cs_YingYongChengXuZhunBeiZhongXinZhuangZai = 1171,

        /// <summary>
        /// {callerMemberName}@{callerLineNumber}输出：
        /// </summary>
        LogBlock_cs_callerMem = 1172,

        /// <summary>
        /// {callerMemberName}@{callerLineNumber}检测对象是否为空{name}
        /// </summary>
        LogBlock_cs_callerMem1 = 1173,

        /// <summary>
        /// {callerMemberName}@{callerLineNumber}打印对象信息：null
        /// </summary>
        LogBlock_cs_callerMem2 = 1174,

        /// <summary>
        /// {callerMemberName}@{callerLineNumber}打印对象信息：{content}
        /// </summary>
        LogBlock_cs_callerMem3 = 1175,

        /// <summary>
        /// {callerMemberName}@{callerLineNumber}打印对象信息：{json.Limit()}
        /// </summary>
        LogBlock_cs_callerMem4 = 1176,

        /// <summary>
        /// 启动更新程序......
        /// </summary>
        App_xaml_cs_QiDongGengXinChengXu = 1177,

        /// <summary>
        /// 实在设计器
        /// </summary>
        App_xaml_cs_ShiZaiSheJiQi = 1178,

        /// <summary>
        /// 实在机器人
        /// </summary>
        App_xaml_cs_ShiZaiJiQiRen = 1179,

        /// <summary>
        /// 需要关闭应用程序{needKillProcessNames.Substring(0, needKillProcessNames.Length - 1)}
        /// </summary>
        App_xaml_cs_XuYaoGuanBiYingYongChengXun = 1180,

        /// <summary>
        /// 关联程序，
        /// </summary>
        App_xaml_cs_GuanLianChengXu = 1181,

        /// <summary>
        /// 关闭应用程序【{processName}】成功
        /// </summary>
        App_xaml_cs_GuanBiYingYongChengXupr = 1182,

        /// <summary>
        /// 关闭应用程序【{processName}】失败
        /// </summary>
        App_xaml_cs_GuanBiYingYongChengXupr1 = 1183,

        /// <summary>
        /// 取消关闭应用程序{needKillProcessNames.Substring(0, needKillProcessNames.Length - 1)}
        /// </summary>
        App_xaml_cs_QuXiaoGuanBiYingYongChengXun = 1184,

        /// <summary>
        /// 无需关闭应用程序
        /// </summary>
        App_xaml_cs_WuXuGuanBiYingYongChengXu = 1185,

        /// <summary>
        /// 【{patchInfo.type}】安装目录路径{appInstPath}
        /// </summary>
        App_xaml_cs_patchInf = 1186,

        /// <summary>
        /// 替换Patch包开始
        /// </summary>
        App_xaml_cs_TiHuanPatchBaoKaiShi = 1187,

        /// <summary>
        /// 替换Patch包正常结束
        /// </summary>
        App_xaml_cs_TiHuanPatchBaoZhengChang = 1188,

        /// <summary>
        /// 替换Patch包异常结束
        /// </summary>
        App_xaml_cs_TiHuanPatchBaoYiChang = 1189,

        /// <summary>
        /// 更新成功......
        /// </summary>
        App_xaml_cs_GengXinChengGong = 1190,

        /// <summary>
        /// 更新状态文件patch.json
        /// </summary>
        App_xaml_cs_GengXinZhuangTaiWenJianpatc = 1191,

        /// <summary>
        /// 更新失败......
        /// </summary>
        App_xaml_cs_GengXinShiBai = 1192,

        /// <summary>
        /// 离线路径【{appInstPath}】
        /// </summary>
        App_xaml_cs_LiXianLuJingappI = 1193,

        /// <summary>
        /// 启动路径:
        /// </summary>
        App_xaml_cs_QiDongLuJing = 1194,

        /// <summary>
        /// 唤起【{patchInfo.type}】成功
        /// </summary>
        App_xaml_cs_HuanQipatchI = 1195,

        /// <summary>
        /// 唤起【{patchInfo.type}】失败：{ex.Message}
        /// </summary>
        App_xaml_cs_HuanQipatchI1 = 1196,

        /// <summary>
        /// patch.json路径:
        /// </summary>
        App_xaml_cs_patchjson = 1197,

        /// <summary>
        /// 执行cmd命令字符:{arguments}
        /// </summary>
        App_xaml_cs_ZhiXingcmdMingLingZiFu = 1198,

        /// <summary>
        /// 处理路径:{workingDir}
        /// </summary>
        App_xaml_cs_ChuLiLuJingwork = 1199,

        /// <summary>
        /// 删除备份文件失败:
        /// </summary>
        App_xaml_cs_ShanChuBeiFenWenJianShiBai = 1200,

        /// <summary>
        /// 删除patch文件失败:
        /// </summary>
        App_xaml_cs_ShanChupatchWenJianShi = 1201,

        /// <summary>
        /// 设计器
        /// </summary>
        MainWindow_xaml_SheJiQi = 1202,

        /// <summary>
        /// 机器人
        /// </summary>
        MainWindow_xaml_cs_JiQiRen = 1203,

        /// <summary>
        /// Patch包校验失败
        /// </summary>
        MainWindow_xaml_cs_PatchBaoXiaoYanShiBai = 1204,

        /// <summary>
        /// 判断文件【{oEnginePath}】是否被占用
        /// </summary>
        MainWindow_xaml_cs_PanDuanWenJianoEng = 1205,

        /// <summary>
        /// 被占用文件【{a.Name}】,进程Id【{a.PId}】,文件路径【{a.Path}】
        /// </summary>
        MainWindow_xaml_cs_BeiZhanYongWenJianaN = 1206,

        /// <summary>
        /// {e.FileInfo.FileName}解压失败:{ex}
        /// </summary>
        MainWindow_xaml_cs_eFileInf = 1207,

        /// <summary>
        /// {e.FileInfo.FileName}解压成功
        /// </summary>
        MainWindow_xaml_cs_eFileInf1 = 1208,

        /// <summary>
        /// Patch更新完成
        /// </summary>
        MainWindow_xaml_cs_PatchGengXinWanCheng = 1209,

        /// <summary>
        /// 关闭更新窗口
        /// </summary>
        MainWindow_xaml_cs_GuanBiGengXinChuangKou = 1210,

        /// <summary>
        /// 替换文件异常:【{ex.ToString()}】
        /// </summary>
        MainWindow_xaml_cs_TiHuanWenJianYiChange = 1211,

        /// <summary>
        /// 更新程序即将关闭
        /// </summary>
        MainWindow_xaml_cs_GengXinChengXuJiJiangGuanBi = 1212,

        /// <summary>
        /// 更新异常关闭中...
        /// </summary>
        MainWindow_xaml_cs_GengXinYiChangGuanBiZhong = 1213,

        /// <summary>
        /// 找到当前备份文件:{backUpPath_Current}
        /// </summary>
        MainWindow_xaml_cs_ZhaoDaoDangQianBeiFenWenJian = 1214,

        /// <summary>
        /// 找到当前安装文件:{appInstallPath_Current}
        /// </summary>
        MainWindow_xaml_cs_ZhaoDaoDangQianAnZhuangWenJian = 1215,

        /// <summary>
        /// 多余engine目录:{nEnginePath}
        /// </summary>
        MainWindow_xaml_cs_DuoYuengineMuLu = 1216,

        /// <summary>
        /// 原有engine目录:{oEnginePath}
        /// </summary>
        MainWindow_xaml_cs_YuanYouengineMuLu = 1217,

        /// <summary>
        /// 进行多余的engine目录删除:{nEnginePath}
        /// </summary>
        MainWindow_xaml_cs_JinXingDuoYuDeengin = 1218,

        /// <summary>
        /// 如果未删除成功多余的engine目录，则进行文件还原
        /// </summary>
        MainWindow_xaml_cs_RuGuoWeiShanChuChengGongDuoYuDe = 1219,

        /// <summary>
        /// 内部还原更新异常:{ex}
        /// </summary>
        MainWindow_xaml_cs_NeiBuHaiYuanGengXinYiChang = 1220,

        /// <summary>
        /// 检测到{needKillProcessNames.Substring(0, needKillProcessNames.Length - 1)}正在运行，点击【确定】将强制关闭已运行的程序，是否继续？
        /// </summary>
        Program_cs_JianCeDaoneedKi = 1221,

        /// <summary>
        /// 提示信息
        /// </summary>
        Program_cs_TiShiXinXi = 1222,

        /// <summary>
        /// Patch状态文件不存在
        /// </summary>
        ApplicationHelper_cs_PatchZhuangTaiWenJianBu = 1223,

        /// <summary>
        /// 取消
        /// </summary>
        XMessageBox_xaml_QuXiao = 1224,

        /// <summary>
        /// 错误
        /// </summary>
        XMessageBox_xaml_cs_CuoWu = 1225,

        /// <summary>
        /// 通知
        /// </summary>
        XMessageBox_xaml_cs_TongZhi = 1226,

        /// <summary>
        /// 确认
        /// </summary>
        XMessageBox_xaml_cs_QueRen = 1227,

        /// <summary>
        /// 360安全浏览器
        /// </summary>
        Chrome360_cs_360AnQuanLiuLanQi = 1228,

        /// <summary>
        /// - 360安全浏览器
        /// </summary>
        Chrome360Node_cs_360AnQuanLiuLanQi = 1229,

        /// <summary>
        /// 下载
        /// </summary>
        Chrome360Node_cs_XiaZai = 1230,

        /// <summary>
        /// 窗格
        /// </summary>
        GuiOperator_cs_ChuangGe = 1231,

        /// <summary>
        /// 飞鸽客服工作台.exe
        /// </summary>
        GuiOperator_cs_FeiGeKeFuGongZuoTaiex = 1232,

        /// <summary>
        /// 数据发送服务未注册
        /// </summary>
        MessageService_cs_ShuJuFaSongFuWuWeiZhuCe = 1233,

        /// <summary>
        /// 操作失败
        /// </summary>
        SocketClient_cs_CaoZuoShiBai = 1234,

        /// <summary>
        /// 远程服务器连接失败！
        /// </summary>
        SocketClient_cs_YuanChengFuWuQiLianJieShiBai = 1235,

        /// <summary>
        /// 远程服务器未响应，请检查IP、端口参数以及网络状态！
        /// </summary>
        SocketClient_cs_YuanChengFuWuQiWeiXiangYingQing = 1236,

        /// <summary>
        /// 调用失败
        /// </summary>
        WindowCreateProcess_cs_DiaoYongShiBai = 1237,

        /// <summary>
        /// 操作成功
        /// </summary>
        JMessage_cs_CaoZuoChengGong = 1238,

        /// <summary>
        /// 行号或列号起止范围格式输入出错，请检查
        /// </summary>
        TableModel_cs_XingHaoHuoLieHaoQiZhiFanWeiGe = 1239,

        /// <summary>
        /// 起始行号或列号必须为整数
        /// </summary>
        TableModel_cs_QiShiXingHaoHuoLieHaoBiXuWei = 1240,

        /// <summary>
        /// 截止行号或列号必须为整数
        /// </summary>
        TableModel_cs_JieZhiXingHaoHuoLieHaoBiXuWei = 1241,

        /// <summary>
        /// 未知的数据库类型
        /// </summary>
        DbHelper_cs_WeiZhiDeShuJuKuLeiXing = 1242,

        /// <summary>
        /// 数据库链接参数不可为空
        /// </summary>
        BatchExecuteSql_cs_ShuJuKuLianJieCanShuBuKeWei = 1243,

        /// <summary>
        /// SQL语句参数不可为空
        /// </summary>
        BatchExecuteSql_cs_SQLYuJuCanShuBuKeWei = 1244,

        /// <summary>
        /// 数据库链接配置不可为空
        /// </summary>
        CreateDbConnection_cs_ShuJuKuLianJiePeiZhiBuKeWei = 1245,

        /// <summary>
        /// 秒
        /// </summary>
        ConfigSetWnd_xaml_Miao = 1246,

        /// <summary>
        /// 自定义对话框
        /// </summary>
        ConfigSetWnd_xaml_ZiDingYiDuiHuaKuang = 1247,

        /// <summary>
        /// 保存当前值
        /// </summary>
        ConfigSetWnd_xaml_BaoCunDangQianZhi = 1248,

        /// <summary>
        /// 确认
        /// </summary>
        ConfigSetWnd_xaml_QueRen = 1249,

        /// <summary>
        /// 输出对话框
        /// </summary>
        ConfigSetWnd_xaml_cs_ShuChuDuiHuaKuang = 1250,

        /// <summary>
        /// 计时未操作将自动执行【确认按钮】，倒计时
        /// </summary>
        ConfigSetWnd_xaml_cs_JiShiWeiCaoZuoJiangZiDongZhiXing = 1251,

        /// <summary>
        /// 计时未操作将自动执行【取消按钮】，倒计时
        /// </summary>
        ConfigSetWnd_xaml_cs_JiShiWeiCaoZuoJiangZiDongZhiXing1 = 1252,

        /// <summary>
        /// 配置的参数有误请检查
        /// </summary>
        ConfigSetWnd_xaml_cs_PeiZhiDeCanShuYouWuQingJianCha = 1253,

        /// <summary>
        /// 列超出设定的最大值
        /// </summary>
        ExcelHelper_cs_LieChaoChuSheDingDeZuiDaZhi = 1254,

        /// <summary>
        /// 列不能小于设定的最小值
        /// </summary>
        ExcelHelper_cs_LieBuNengXiaoYuSheDingDeZuiXiao = 1255,

        /// <summary>
        /// 文件被占用，请关闭后重试
        /// </summary>
        ExcelHelper_cs_WenJianBeiZhanYongQingGuanBiHou = 1256,

        /// <summary>
        /// 文件不是标准Excel格式
        /// </summary>
        ExcelHelper_cs_WenJianBuShiBiaoZhunExce = 1257,

        /// <summary>
        /// 未知的异常
        /// </summary>
        ExcelHelper_cs_WeiZhiDeYiChang = 1258,

        /// <summary>
        /// 文件后缀名有误
        /// </summary>
        ExcelHelper_cs_WenJianHouZhuiMingYouWu = 1259,

        /// <summary>
        /// 创建Sheet失败
        /// </summary>
        ExcelHelper_cs_ChuangJianSheetShiBai = 1260,

        /// <summary>
        /// 密码框
        /// </summary>
        PluginPackage_cs_MiMaKuang = 1261,

        /// <summary>
        /// 选项1
        /// </summary>
        PluginPackage_cs_XuanXiang1 = 1262,

        /// <summary>
        /// 选项2
        /// </summary>
        PluginPackage_cs_XuanXiang2 = 1263,

        /// <summary>
        /// 选项3
        /// </summary>
        PluginPackage_cs_XuanXiang3 = 1264,

        /// <summary>
        /// 选项4
        /// </summary>
        PluginPackage_cs_XuanXiang4 = 1265,

        /// <summary>
        /// Excel文件读取中…
        /// </summary>
        Waiting_xaml_cs_ExcelWenJianDuQuZhong = 1266,

        /// <summary>
        /// 重试
        /// </summary>
        FormInfo_xaml_ZhongShi = 1267,

        /// <summary>
        /// 终止
        /// </summary>
        FormInfo_xaml_ZhongZhi = 1268,

        /// <summary>
        /// 中止
        /// </summary>
        FormInfo_xaml_cs_ZhongZhi = 1269,

        /// <summary>
        /// 计时未操作将自动执行【{state}按钮】，倒计时
        /// </summary>
        FormInfo_xaml_cs_JiShiWeiCaoZuoJiangZiDongZhiXing = 1270,

        /// <summary>
        /// 输入对话框
        /// </summary>
        FormInput_xaml_cs_ShuRuDuiHuaKuang = 1271,

        /// <summary>
        /// 对话框显示异常，请检查配置信息是否正确！
        /// </summary>
        ConfigDialog_cs_DuiHuaKuangXianShiYiChangQingJian = 1272,

        /// <summary>
        /// 功能24_x2F_删除
        /// </summary>
        BaseStyle_xaml_GongNeng24x2FShan = 1273,

        /// <summary>
        /// 导入
        /// </summary>
        Image_xaml_DaoRu = 1274,

        /// <summary>
        /// 导出
        /// </summary>
        Image_xaml_DaoChu = 1275,

        /// <summary>
        /// 清空
        /// </summary>
        Image_xaml_QingKong = 1276,

        /// <summary>
        /// 最大化
        /// </summary>
        Title_xaml_ZuiDaHua = 1277,

        /// <summary>
        /// 向下还原
        /// </summary>
        Title_xaml_XiangXiaHaiYuan = 1278,

        /// <summary>
        /// 将文件拖拽到此或点击添加文件
        /// </summary>
        SubUpload_xaml_JiangWenJianTuoZhuaiDaoCiHuoDianJi = 1279,

        /// <summary>
        /// 附件
        /// </summary>
        UCAnnex_xaml_FuJian = 1280,

        /// <summary>
        /// 文件选择
        /// </summary>
        UCBrowserFile_xaml_WenJianXuanZe = 1281,

        /// <summary>
        /// 不能为空
        /// </summary>
        UCBrowserFile_xaml_BuNengWeiKong = 1282,

        /// <summary>
        /// 复选框
        /// </summary>
        UCCheck_xaml_FuXuanKuang = 1283,

        /// <summary>
        /// 下拉列表
        /// </summary>
        UCComboBox_xaml_XiaLaLieBiao = 1284,

        /// <summary>
        /// 数据表
        /// </summary>
        UCDataTable_xaml_ShuJuBiao = 1285,

        /// <summary>
        /// 复制行
        /// </summary>
        UCDataTable_xaml_FuZhiXing = 1286,

        /// <summary>
        /// 粘贴行
        /// </summary>
        UCDataTable_xaml_NianTieXing = 1287,

        /// <summary>
        /// 向上插入
        /// </summary>
        UCDataTable_xaml_XiangShangChaRu = 1288,

        /// <summary>
        /// 行
        /// </summary>
        UCDataTable_xaml_Xing = 1289,

        /// <summary>
        /// 向下插入
        /// </summary>
        UCDataTable_xaml_XiangXiaChaRu = 1290,

        /// <summary>
        /// 删除
        /// </summary>
        UCDataTable_xaml_ShanChu = 1291,

        /// <summary>
        /// 复制列
        /// </summary>
        UCDataTable_xaml_FuZhiLie = 1292,

        /// <summary>
        /// 粘贴列
        /// </summary>
        UCDataTable_xaml_NianTieLie = 1293,

        /// <summary>
        /// 向左插入
        /// </summary>
        UCDataTable_xaml_XiangZuoChaRu = 1294,

        /// <summary>
        /// 列
        /// </summary>
        UCDataTable_xaml_Lie = 1295,

        /// <summary>
        /// 向右插入
        /// </summary>
        UCDataTable_xaml_XiangYouChaRu = 1296,

        /// <summary>
        /// 删除当前行
        /// </summary>
        UCDataTable_xaml_ShanChuDangQianXing = 1297,

        /// <summary>
        /// 删除当前列
        /// </summary>
        UCDataTable_xaml_ShanChuDangQianLie = 1298,

        /// <summary>
        /// 添加行异常：
        /// </summary>
        UCDataTable_xaml_cs_TianJiaXingYiChang = 1299,

        /// <summary>
        /// 添加列异常：
        /// </summary>
        UCDataTable_xaml_cs_TianJiaLieYiChang = 1300,

        /// <summary>
        /// Excel文件|*.xls;*.xlsx
        /// </summary>
        UCDataTable_xaml_cs_ExcelWenJian = 1301,

        /// <summary>
        /// 导出成功
        /// </summary>
        UCDataTable_xaml_cs_DaoChuChengGong = 1302,

        /// <summary>
        /// 无法进行删除！
        /// </summary>
        UCDataTable_xaml_cs_WuFaJinXingShanChu = 1303,

        /// <summary>
        /// 删除失败
        /// </summary>
        UCDataTable_xaml_cs_ShanChuShiBai = 1304,

        /// <summary>
        /// 无法进行删除!
        /// </summary>
        UCDataTable_xaml_cs_WuFaJinXingShanChu1 = 1305,

        /// <summary>
        /// 插入行异常：
        /// </summary>
        UCDataTable_xaml_cs_ChaRuXingYiChang = 1306,

        /// <summary>
        /// 日期控件
        /// </summary>
        UCDate_xaml_RiQiKongJian = 1307,

        /// <summary>
        /// 多选下拉列表
        /// </summary>
        UCMultiComboBox_xaml_DuoXuanXiaLaLieBiao = 1308,

        /// <summary>
        /// 请选择
        /// </summary>
        UCMultiComboBox_xaml_QingXuanZe = 1309,

        /// <summary>
        /// 请输入密码
        /// </summary>
        UCPasswordLook_xaml_QingShuRuMiMa = 1310,

        /// <summary>
        /// 单选框
        /// </summary>
        UCRedioH_xaml_DanXuanKuang = 1311,

        /// <summary>
        /// 基本属性
        /// </summary>
        UCSeparator_xaml_JiBenShuXing = 1312,

        /// <summary>
        /// 开关
        /// </summary>
        UCSwitch_xaml_KaiGuan = 1313,

        /// <summary>
        /// 表格
        /// </summary>
        UCTable_xaml_BiaoGe = 1314,

        /// <summary>
        /// 输入框
        /// </summary>
        UCTextBox_xaml_ShuRuKuang = 1315,

        /// <summary>
        /// 上传附件
        /// </summary>
        UCUpload_xaml_ShangChuanFuJian = 1316,

        /// <summary>
        /// 对话框
        /// </summary>
        GuiOperator_cs_DuiHuaKuang = 1317,

        /// <summary>
        /// Excel 文件不能为空
        /// </summary>
        Excel_cs_ExcelWenJianBuNeng = 1318,

        /// <summary>
        /// ]}文件未打开，请使用【打开Excel】组件打开
        /// </summary>
        Excel_cs_WenJianWeiDaKaiQingShi = 1319,

        /// <summary>
        /// 使用软件
        /// </summary>
        Excel_cs_ShiYongRuanJian = 1320,

        /// <summary>
        /// 不使用软件
        /// </summary>
        Excel_cs_BuShiYongRuanJian = 1321,

        /// <summary>
        /// 是
        /// </summary>
        Excel_cs_Shi = 1322,

        /// <summary>
        /// 否
        /// </summary>
        Excel_cs_Fou = 1323,

        /// <summary>
        /// ]} 已选择【使用软件】方式打开，请用【关闭Excel】组件关闭后重试或重新打开流程
        /// </summary>
        Excel_cs_YiXuanZeShiYongRuan = 1324,

        /// <summary>
        /// 写入内容不能为空
        /// </summary>
        Excel_cs_XieRuNeiRongBuNengWeiKong = 1325,

        /// <summary>
        /// 文件关闭失败，可能原因文件被占用
        /// </summary>
        Excel_cs_WenJianGuanBiShiBaiKeNengYuan = 1326,

        /// <summary>
        /// 不能合并同一个Excel文件的同一个Sheet
        /// </summary>
        Excel_cs_BuNengHeBingTongYiGeExc = 1327,

        /// <summary>
        /// 输入的参数内容格式不正确（插入几行），应为正整数
        /// </summary>
        Excel_cs_ShuRuDeCanShuNeiRongGeShiBu = 1328,

        /// <summary>
        /// 插入数量已超出 Excel 最大上限
        /// </summary>
        Excel_cs_ChaRuShuLiangYiChaoChuEx = 1329,

        /// <summary>
        /// 输入的参数内容格式不正确（插入几列），应为正整数
        /// </summary>
        Excel_cs_ShuRuDeCanShuNeiRongGeShiBu1 = 1330,

        /// <summary>
        /// 查找内容不能为空
        /// </summary>
        Excel_cs_ChaZhaoNeiRongBuNengWeiKong = 1331,

        /// <summary>
        /// 未找到要执行的组件
        /// </summary>
        Excel_cs_WeiZhaoDaoYaoZhiXingDeZuJian = 1332,

        /// <summary>
        /// ]} 未打开或已被关闭
        /// </summary>
        Excel_cs_WeiDaKaiHuoYiBeiGuan = 1333,

        /// <summary>
        /// Office返回消息：{ex.Message}
        /// </summary>
        Excel_cs_OfficeFanHuiXiaoXi = 1334,

        /// <summary>
        /// 区域参数格式不正确
        /// </summary>
        ExcelUtils_cs_QuYuCanShuGeShiBuZhengQue = 1335,

        /// <summary>
        /// 输入的颜色格式不正确，请查看说明文档
        /// </summary>
        ExcelUtils_cs_ShuRuDeYanSeGeShiBuZhengQue = 1336,

        /// <summary>
        /// 输入的参数内容格式不正确（无效的列表），请查看说明文档
        /// </summary>
        ExcelUtils_cs_ShuRuDeCanShuNeiRongGeShiBu = 1337,

        /// <summary>
        /// 输入的参数内容格式不正确（写入内容），请输入有效的二维列表或查看说明文档
        /// </summary>
        ExcelUtils_cs_ShuRuDeCanShuNeiRongGeShiBu1 = 1338,

        /// <summary>
        /// 行号：{row} 无效，最小输入值为 -{END_ROW}
        /// </summary>
        ExcelUtils_cs_XingHaorowWu = 1339,

        /// <summary>
        /// {row} 不是有效的行号，行号应为小于 1048576 的整数
        /// </summary>
        ExcelUtils_cs_rowBuShiYouXiao = 1340,

        /// <summary>
        /// 列索引：{_columnIndex} 无效，最小输入值为：-{END_COLUMN_INDEX}（列名：{END_COLUMN}）
        /// </summary>
        ExcelUtils_cs_LieSuoYincolu = 1341,

        /// <summary>
        /// {input} 不是有效的列名，列名应为A到XFD之间的字母或者是小于 16384 的正整数
        /// </summary>
        ExcelUtils_cs_inputBuShi = 1342,

        /// <summary>
        /// {columnLetter} 无法转成有效的列名
        /// </summary>
        ExcelUtils_cs_columnLet = 1343,

        /// <summary>
        /// 输入的参数内容格式不正确，请查看说明文档
        /// </summary>
        ExcelUtils_cs_ShuRuDeCanShuNeiRongGeShiBu2 = 1344,

        /// <summary>
        /// 列名参数格式输入不正确，请查看说明文档
        /// </summary>
        ExcelUtils_cs_LieMingCanShuGeShiShuRuBuZheng = 1345,

        /// <summary>
        /// 文件名不能为空
        /// </summary>
        ExcelUtils_cs_WenJianMingBuNengWeiKong = 1346,

        /// <summary>
        /// {name}(副本)
        /// </summary>
        ExcelUtils_cs_nameFuBen = 1347,

        /// <summary>
        /// 区域地址格式不正确
        /// </summary>
        ExcelUtils_cs_QuYuDiZhiGeShiBuZhengQue = 1348,

        /// <summary>
        /// 选择{context.Label}
        /// </summary>
        ExcelForm_cs_XuanZecontext = 1349,

        /// <summary>
        /// 选择区域
        /// </summary>
        ExcelForm_Designer_cs_XuanZeQuYu = 1350,

        /// <summary>
        /// 工作簿中名称为 {sheetName} 的工作表不存在
        /// </summary>
        NonVisualExcel_cs_GongZuoBuZhongMingChenWeis = 1351,

        /// <summary>
        /// {workbook} 未打开
        /// </summary>
        NonVisualExcel_cs_workbook = 1352,

        /// <summary>
        /// 表名不能为空
        /// </summary>
        NonVisualExcel_cs_BiaoMingBuNengWeiKong = 1353,

        /// <summary>
        /// 选择区域参数格式不正确
        /// </summary>
        NonVisualExcel_cs_XuanZeQuYuCanShuGeShiBuZheng = 1354,

        /// <summary>
        /// 工作表的序号不能小于 1 或大于工作簿中已有工作表数量
        /// </summary>
        NonVisualExcel_cs_GongZuoBiaoDeXuHaoBuNengXiaoYu = 1355,

        /// <summary>
        /// 请在可视化模式下打开一个已经配置好规则的文件，读取相应配置规则
        /// </summary>
        NonVisualExcel_cs_QingZaiKeShiHuaMoShiXiaDaKai = 1356,

        /// <summary>
        /// 未知的循环标识
        /// </summary>
        NonVisualExcel_cs_WeiZhiDeXunHuanBiaoShi = 1357,

        /// <summary>
        /// 单独一行
        /// </summary>
        SheetEdit_cs_DanDuYiXing = 1358,

        /// <summary>
        /// 连续多行
        /// </summary>
        SheetEdit_cs_LianXuDuoXing = 1359,

        /// <summary>
        /// 分散多行
        /// </summary>
        SheetEdit_cs_FenSanDuoXing = 1360,

        /// <summary>
        /// 单独一列
        /// </summary>
        SheetEdit_cs_DanDuYiLie = 1361,

        /// <summary>
        /// 连续多列
        /// </summary>
        SheetEdit_cs_LianXuDuoLie = 1362,

        /// <summary>
        /// 分散多列
        /// </summary>
        SheetEdit_cs_FenSanDuoLie = 1363,

        /// <summary>
        /// 隐藏
        /// </summary>
        SheetEdit_cs_YinCang = 1364,

        /// <summary>
        /// 图片
        /// </summary>
        SheetEdit_cs_TuPian = 1365,

        /// <summary>
        /// 删除失败，名称为 {name} 的图片未找到
        /// </summary>
        SheetEdit_cs_ShanChuShiBaiMingChenWei = 1366,

        /// <summary>
        /// 右侧单元格左移
        /// </summary>
        SheetEdit_cs_YouCeDanYuanGeZuoYi = 1367,

        /// <summary>
        /// 下方单元格上移
        /// </summary>
        SheetEdit_cs_XiaFangDanYuanGeShangYi = 1368,

        /// <summary>
        /// 整行
        /// </summary>
        SheetEdit_cs_ZhengXing = 1369,

        /// <summary>
        /// 整列
        /// </summary>
        SheetEdit_cs_ZhengLie = 1370,

        /// <summary>
        /// 复制单元格
        /// </summary>
        SheetEdit_cs_FuZhiDanYuanGe = 1371,

        /// <summary>
        /// 复制区域
        /// </summary>
        SheetEdit_cs_FuZhiQuYu = 1372,

        /// <summary>
        /// 复制已编辑区域
        /// </summary>
        SheetEdit_cs_FuZhiYiBianJiQuYu = 1373,

        /// <summary>
        /// 无效的复制模式
        /// </summary>
        SheetEdit_cs_WuXiaoDeFuZhiMoShi = 1374,

        /// <summary>
        /// 请先用【复制表格内容】组件复制一个区域
        /// </summary>
        SheetEdit_cs_QingXianYongFuZhiBiaoGeNeiRong = 1375,

        /// <summary>
        /// 【执行宏】组件需要依赖 Microsoft Excel或WPS软件，请用【使用软件】方式重新打开文件
        /// </summary>
        SheetEdit_cs_ZhiXingHongZuJianXuYaoYi = 1376,

        /// <summary>
        /// 【自动填充】需要依赖 Microsoft Excel或WPS软件，请用【使用软件】方式重新打开文件
        /// </summary>
        SheetEdit_cs_ZiDongTianChongXuYaoYiLai = 1377,

        /// <summary>
        /// 指定一列
        /// </summary>
        SheetInfo_cs_ZhiDingYiLie = 1378,

        /// <summary>
        /// 指定一行
        /// </summary>
        SheetInfo_cs_ZhiDingYiXing = 1379,

        /// <summary>
        /// 区域
        /// </summary>
        SheetInfo_cs_QuYu = 1380,

        /// <summary>
        /// 已编辑区域
        /// </summary>
        SheetInfo_cs_YiBianJiQuYu = 1381,

        /// <summary>
        /// 查找并替换
        /// </summary>
        SheetInfo_cs_ChaZhaoBingTiHuan = 1382,

        /// <summary>
        /// 第一个
        /// </summary>
        SheetInfo_cs_DiYiGe = 1383,

        /// <summary>
        /// {file} 已存在
        /// </summary>
        Workbook_cs_fileYiCunZai = 1384,

        /// <summary>
        /// {file} 不存在
        /// </summary>
        Workbook_cs_fileBuCunZai = 1385,

        /// <summary>
        /// {file} 是加密文件，请输入打开文件的密码
        /// </summary>
        Workbook_cs_fileShiJiaMi = 1386,

        /// <summary>
        /// 打开文件密码输入错误，请输入正确的打开文件密码
        /// </summary>
        Workbook_cs_DaKaiWenJianMiMaShuRuCuoWu = 1387,

        /// <summary>
        /// {file} 启用了编辑文档权限，请输入正确的编辑权限密码
        /// </summary>
        Workbook_cs_fileQiYongLe = 1388,

        /// <summary>
        /// 全部
        /// </summary>
        Workbook_cs_QuanBu = 1389,

        /// <summary>
        /// 实在RPA
        /// </summary>
        Workbook_cs_ShiZaiRPA = 1390,

        /// <summary>
        /// 工作簿中已存在 {name} 的工作表
        /// </summary>
        Worksheet_cs_GongZuoBuZhongYiCunZain = 1391,

        /// <summary>
        /// 之前
        /// </summary>
        Worksheet_cs_ZhiQian = 1392,

        /// <summary>
        /// 新工作表名称不能为空
        /// </summary>
        Worksheet_cs_XinGongZuoBiaoMingChenBuNengWeiKong = 1393,

        /// <summary>
        /// 新Sheet名称不能为空
        /// </summary>
        Worksheet_cs_XinSheetMingChenBuNeng = 1394,

        /// <summary>
        /// {target} 未打开
        /// </summary>
        Worksheet_cs_targetWei = 1395,

        /// <summary>
        /// 目标文件已存在同名Sheet
        /// </summary>
        Worksheet_cs_MuBiaoWenJianYiCunZaiTongMingS = 1396,

        /// <summary>
        /// 按名称
        /// </summary>
        Worksheet_cs_AnMingChen = 1397,

        /// <summary>
        /// 激活的 Sheet 序号不能小于 1
        /// </summary>
        Worksheet_cs_JiHuoDeSheet = 1398,

        /// <summary>
        /// 按位置
        /// </summary>
        Worksheet_cs_AnWeiZhi = 1399,

        /// <summary>
        /// 第一位
        /// </summary>
        Worksheet_cs_DiYiWei = 1400,

        /// <summary>
        /// 最后一位
        /// </summary>
        Worksheet_cs_ZuiHouYiWei = 1401,

        /// <summary>
        /// 向前移一位
        /// </summary>
        Worksheet_cs_XiangQianYiYiWei = 1402,

        /// <summary>
        /// 向后移一位
        /// </summary>
        Worksheet_cs_XiangHouYiYiWei = 1403,

        /// <summary>
        /// 按序号
        /// </summary>
        Worksheet_cs_AnXuHao = 1404,

        /// <summary>
        /// 数据可视化_{new Random().Next(100,1000)}
        /// </summary>
        GetExcelRule_cs_ShuJuKeShiHuanew = 1405,

        /// <summary>
        /// Excel 数据操作可视化运行出错：{ex.Message}
        /// </summary>
        GetExcelRule_cs_ExcelShuJuCaoZuo = 1406,

        /// <summary>
        /// 调用的Excel 方法不存在
        /// </summary>
        Run_cs_DiaoYongDeExcelFang = 1407,

        /// <summary>
        /// Excel 组件参数不可为空
        /// </summary>
        Run_cs_ExcelZuJianCanShu = 1408,

        /// <summary>
        /// 参数校验失败，请检查传入的参数格式是否正确
        /// </summary>
        Run_cs_CanShuXiaoYanShiBaiQingJianCha = 1409,

        /// <summary>
        /// 未读取到配置信息，请重新选择或取消
        /// </summary>
        ExcelRule_cs_WeiDuQuDaoPeiZhiXinXiQing = 1410,

        /// <summary>
        /// 未找到排序规则
        /// </summary>
        ExcelRule_cs_WeiZhaoDaoPaiXuGuiZe = 1411,

        /// <summary>
        /// 删除行失败；可能原因：单次删除的行数超过 Excel 软件的限制，请尝试在打开Excel 文件时取消选择【使用软件】选项，然后重试
        /// </summary>
        SheetEdit_cs_ShanChuXingShiBaiKeNengYuanYin = 1412,

        /// <summary>
        /// 无效得复制模式
        /// </summary>
        SheetEdit_cs_WuXiaoDeFuZhiMoShi1 = 1413,

        /// <summary>
        /// 请先用复制区域组件进行复制
        /// </summary>
        SheetEdit_cs_QingXianYongFuZhiQuYuZuJianJin = 1414,

        /// <summary>
        /// 代码中未找到有效的 Sub 或 Function
        /// </summary>
        SheetEdit_cs_DaiMaZhongWeiZhaoDaoYouXiaoDe = 1415,

        /// <summary>
        /// 执行宏发生错误。可能原因：1，VBA 代码语法错误; 2，Excel 文件中已存在相同名称的 Sub 或 Function
        /// </summary>
        SheetEdit_cs_ZhiXingHongFaShengCuoWuKeNeng = 1416,

        /// <summary>
        /// 源区域信息填写无效：{sourceRefr}
        /// </summary>
        SheetEdit_cs_YuanQuYuXinXiTianXieWuXiao = 1417,

        /// <summary>
        /// 填充数量不是一个有效的整数
        /// </summary>
        SheetEdit_cs_TianChongShuLiangBuShiYiGeYouXiao = 1418,

        /// <summary>
        /// 向下填充
        /// </summary>
        SheetEdit_cs_XiangXiaTianChong = 1419,

        /// <summary>
        /// 向左填充
        /// </summary>
        SheetEdit_cs_XiangZuoTianChong = 1420,

        /// <summary>
        /// 向上填充
        /// </summary>
        SheetEdit_cs_XiangShangTianChong = 1421,

        /// <summary>
        /// 向右填充
        /// </summary>
        SheetEdit_cs_XiangYouTianChong = 1422,

        /// <summary>
        /// )}不是有效的填充方向
        /// </summary>
        SheetEdit_cs_BuShiYouXiaoDeTianChongFang = 1423,

        /// <summary>
        /// 填充区域信息填写无效
        /// </summary>
        SheetEdit_cs_TianChongQuYuXinXiTianXieWuXiao = 1424,

        /// <summary>
        /// 填充区域必须包含源区域且只能向一个方向填充
        /// </summary>
        SheetEdit_cs_TianChongQuYuBiXuBaoHanYuanQu = 1425,

        /// <summary>
        /// 填充区域必须大于源区域
        /// </summary>
        SheetEdit_cs_TianChongQuYuBiXuDaYuYuanQu = 1426,

        /// <summary>
        /// 当前计算机账户未安装Microsoft Excel 或 WPS，请选择【不使用软件】选项
        /// </summary>
        VisualExcel_cs_DangQianJiSuanJiZhangHuWeiAnZhuang = 1427,

        /// <summary>
        /// 当前计算机账户未安装Microsoft Excel，请选择 WPS 打开或选择【不使用软件】选项
        /// </summary>
        VisualExcel_cs_DangQianJiSuanJiZhangHuWeiAnZhuang1 = 1428,

        /// <summary>
        /// 打开 WPS 失败 可能原因：1，当前计算机账户未安装 WPS，请选择 Microsoft Office 打开或或选择【不使用软件】选项。 2，实在设计器以管理员身份启动，请使用非管理员身份启动设计器
        /// </summary>
        VisualExcel_cs_DaKaiWPSShiBai = 1429,

        /// <summary>
        /// 工作簿中名称为 {sheetName} 工作表不存在
        /// </summary>
        VisualExcel_cs_GongZuoBuZhongMingChenWeis = 1430,

        /// <summary>
        /// 工作簿中索引为 {index} 位置工作表不存在
        /// </summary>
        VisualExcel_cs_GongZuoBuZhongSuoYinWeii = 1431,

        /// <summary>
        /// 工作簿中 {sheetName} 不存在
        /// </summary>
        VisualExcel_cs_GongZuoBuZhongshee = 1432,

        /// <summary>
        /// 格式
        /// </summary>
        VisualExcel_cs_GeShi = 1433,

        /// <summary>
        /// 鼠标左键单击
        /// </summary>
        VisualExcel_cs_ShuBiaoZuoJianDanJi = 1434,

        /// <summary>
        /// 单元格
        /// </summary>
        VisualExcel_cs_DanYuanGe = 1435,

        /// <summary>
        /// 没有打开Excel文件，请打开文件后重新打开拾取窗口
        /// </summary>
        VisualExcel_cs_MeiYouDaKaiExcelWen = 1436,

        /// <summary>
        /// 鼠标左键框选
        /// </summary>
        VisualExcel_cs_ShuBiaoZuoJianKuangXuan = 1437,

        /// <summary>
        /// 表格区域
        /// </summary>
        VisualExcel_cs_BiaoGeQuYu = 1438,

        /// <summary>
        /// 可视化暂不支持该操作，请用非可视化打开文件
        /// </summary>
        VisualExcel_cs_KeShiHuaZanBuZhiChiGaiCaoZuo = 1439,

        /// <summary>
        /// Excel插件 未链接
        /// </summary>
        Workbook_cs_ExcelChaJianWeiLian = 1440,

        /// <summary>
        /// Excel 插件未启用，请打开菜单【开发工具】-》加载项 -》选择 实在RPA 表格助手
        /// </summary>
        Workbook_cs_ExcelChaJianWeiQi = 1441,

        /// <summary>
        /// 实在RPA 表格助手
        /// </summary>
        Workbook_cs_ShiZaiRPABiaoGeZhuShou = 1442,

        /// <summary>
        /// 启用Excel插件异常: {ex.Message}
        /// </summary>
        Workbook_cs_QiYongExcelChaJianYi = 1443,

        /// <summary>
        /// {file} 未打开，请使用【打开Excel】组件打开
        /// </summary>
        Workbook_cs_fileWeiDaKai = 1444,

        /// <summary>
        /// 已存在同名的工作表
        /// </summary>
        Worksheet_cs_YiCunZaiTongMingDeGongZuoBiao = 1445,

        /// <summary>
        /// 目标文件中已存在同名Sheet
        /// </summary>
        Worksheet_cs_MuBiaoWenJianZhongYiCunZaiTongMing = 1446,

        /// <summary>
        /// 请输入要保存的文件名…
        /// </summary>
        FireFoxNode_cs_QingShuRuYaoBaoCunDeWenJianMing = 1447,

        /// <summary>
        /// 保存文件
        /// </summary>
        FireFoxNode_cs_BaoCunWenJian = 1448,

        /// <summary>
        /// 当前页
        /// </summary>
        CapturePageViewModel_cs_DangQianYe = 1449,

        /// <summary>
        /// 指定页数 {value}
        /// </summary>
        CapturePageViewModel_cs_ZhiDingYeShuvalu = 1450,

        /// <summary>
        /// 翻页元素拾取失败
        /// </summary>
        CapturePageViewModel_cs_FanYeYuanSuShiQuShiBai = 1451,

        /// <summary>
        /// 翻页元素编辑发生异常，请重启程序后重试！
        /// </summary>
        CapturePageViewModel_cs_FanYeYuanSuBianJiFaShengYiChang = 1452,

        /// <summary>
        /// 编辑翻页元素失败
        /// </summary>
        CapturePageViewModel_cs_BianJiFanYeYuanSuShiBai = 1453,

        /// <summary>
        /// 翻页元素删除失败
        /// </summary>
        CapturePageViewModel_cs_FanYeYuanSuShanChuShiBai = 1454,

        /// <summary>
        /// 所有页
        /// </summary>
        CapturePageViewModel_cs_SuoYouYe = 1455,

        /// <summary>
        /// 指定页数
        /// </summary>
        CapturePageViewModel_cs_ZhiDingYeShuPage = 1456,

        /// <summary>
        /// 数据异常
        /// </summary>
        CapturePageViewModel_cs_ShuJuYiChang = 1457,

        /// <summary>
        /// 弹出下拉框异常
        /// </summary>
        DataGridViewModel_cs_DanChuXiaLaKuangYiChang = 1458,

        /// <summary>
        /// 获取文本内容异常
        /// </summary>
        DataGridViewModel_cs_HuoQuWenBenNeiRongYiChang = 1459,

        /// <summary>
        /// 获取链接异常
        /// </summary>
        DataGridViewModel_cs_HuoQuLianJieYiChang = 1460,

        /// <summary>
        /// 获取图片内容失败
        /// </summary>
        DataGridViewModel_cs_HuoQuTuPianNeiRongShiBai = 1461,

        /// <summary>
        /// 编辑列元素失败
        /// </summary>
        DataGridViewModel_cs_BianJiLieYuanSuShiBai = 1462,

        /// <summary>
        /// 编辑表格元素失败
        /// </summary>
        DataGridViewModel_cs_BianJiBiaoGeYuanSuShiBai = 1463,

        /// <summary>
        /// 编辑正则异常
        /// </summary>
        DataGridViewModel_cs_BianJiZhengZeYiChang = 1464,

        /// <summary>
        /// 拾取相似元素异常
        /// </summary>
        DataGridViewModel_cs_ShiQuXiangSiYuanSuYiChang = 1465,

        /// <summary>
        /// 编辑表头异常
        /// </summary>
        DataGridViewModel_cs_BianJiBiaoTouYiChang = 1466,

        /// <summary>
        /// 配置信息异常，请查看日志处理
        /// </summary>
        DataGridViewModel_cs_PeiZhiXinXiYiChangQingChaKan = 1467,

        /// <summary>
        /// 操作异常
        /// </summary>
        DataGridViewModel_cs_CaoZuoYiChang = 1468,

        /// <summary>
        /// 列序号超范围 删除失败
        /// </summary>
        DataGridViewModel_cs_LieXuHaoChaoFanWeiShanChuShi = 1469,

        /// <summary>
        /// 数据加载异常
        /// </summary>
        DataGridViewModel_cs_ShuJuJiaZaiYiChang = 1470,

        /// <summary>
        /// 数据加载出现异常
        /// </summary>
        DataGridViewModel_cs_ShuJuJiaZaiChuXianYiChang = 1471,

        /// <summary>
        /// 刷新数据异常
        /// </summary>
        DataGridViewModel_cs_ShuaXinShuJuYiChang = 1472,

        /// <summary>
        /// 编辑表格异常
        /// </summary>
        DataGridViewModel_cs_BianJiBiaoGeYiChang = 1473,

        /// <summary>
        /// 设置窗体位置异常
        /// </summary>
        MainWindowViewModel_cs_SheZhiChuangTiWeiZhiYiChang = 1474,

        /// <summary>
        /// 开始元素拾取异常，请关闭配置窗口后重新打开，再次拾取!
        /// </summary>
        MainWindowViewModel_cs_KaiShiYuanSuShiQuYiChangQing = 1475,

        /// <summary>
        /// 开始元素拾取异常，请关闭配置窗口后重新打开，再次拾取！
        /// </summary>
        MainWindowViewModel_cs_KaiShiYuanSuShiQuYiChangQing1 = 1476,

        /// <summary>
        /// 拾取出现异常
        /// </summary>
        MainWindowViewModel_cs_ShiQuChuXianYiChang = 1477,

        /// <summary>
        /// 浏览器未安装插件，无法采集数据！
        /// </summary>
        MainWindowViewModel_cs_LiuLanQiWeiAnZhuangChaJianWu = 1478,

        /// <summary>
        /// 安装插件
        /// </summary>
        MainWindowViewModel_cs_AnZhuangChaJian = 1479,

        /// <summary>
        /// 继续拾取
        /// </summary>
        MainWindowViewModel_cs_JiXuShiQu = 1480,

        /// <summary>
        /// 拾取数据处理异常
        /// </summary>
        MainWindowViewModel_cs_ShiQuShuJuChuLiYiChang = 1481,

        /// <summary>
        /// 数据采集后展示预览数据发生了异常，请重新拾取！
        /// </summary>
        MainWindowViewModel_cs_ShuJuCaiJiHouZhanShiYuLanShu = 1482,

        /// <summary>
        /// 拾取相似元素并预览数据发生了异常，请重新拾取！
        /// </summary>
        MainWindowViewModel_cs_ShiQuXiangSiYuanSuBingYuLanShu = 1483,

        /// <summary>
        /// 拾取翻页元素发生了异常，请重新拾取！
        /// </summary>
        MainWindowViewModel_cs_ShiQuFanYeYuanSuFaShengLeYi = 1484,

        /// <summary>
        /// 数据获取异常，可编辑未获取到数据的列，或删除重新拾取
        /// </summary>
        MainWindowViewModel_cs_ShuJuHuoQuYiChangKeBianJi = 1485,

        /// <summary>
        /// 加载预览数据异常
        /// </summary>
        MainWindowViewModel_cs_JiaZaiYuLanShuJuYiChang = 1486,

        /// <summary>
        /// 获取数据缺失字段
        /// </summary>
        MainWindowViewModel_cs_HuoQuShuJuQueShiZiDuan = 1487,

        /// <summary>
        /// 当前有字段不存在，请确认!
        /// </summary>
        MainWindowViewModel_cs_DangQianYouZiDuanBuCunZaiQing = 1488,

        /// <summary>
        /// 刷新数据发生异常
        /// </summary>
        MainWindowViewModel_cs_ShuaXinShuJuFaShengYiChang = 1489,

        /// <summary>
        /// 鼠标左键
        /// </summary>
        DataCollectOverlayWnd_xaml_ShuBiaoZuoJian = 1490,

        /// <summary>
        /// 获取采集数据
        /// </summary>
        DataCollectOverlayWnd_xaml_HuoQuCaiJiShuJu = 1491,

        /// <summary>
        /// 取消拾取【采集数据】
        /// </summary>
        DataCollectOverlayWnd_xaml_QuXiaoShiQuCaiJiShuJu = 1492,

        /// <summary>
        /// 取消拾取【翻页按钮】
        /// </summary>
        DataCollectOverlayWnd_xaml_cs_QuXiaoShiQuFanYeAnNiu = 1493,

        /// <summary>
        /// 拾取【翻页按钮】
        /// </summary>
        DataCollectOverlayWnd_xaml_cs_ShiQuFanYeAnNiu = 1494,

        /// <summary>
        /// 取消拾取【同类数据】
        /// </summary>
        DataCollectOverlayWnd_xaml_cs_QuXiaoShiQuTongLeiShuJu = 1495,

        /// <summary>
        /// 拾取【同类数据】
        /// </summary>
        DataCollectOverlayWnd_xaml_cs_ShiQuTongLeiShuJu = 1496,

        /// <summary>
        /// 拾取程序开小差了，请重试一下吧！
        /// </summary>
        DataCollectOverlayWnd_xaml_cs_ShiQuChengXuKaiXiaoChaLeQing = 1497,

        /// <summary>
        /// 拾取过程中发生异常，请重新拾取！
        /// </summary>
        DataCollectOverlayWnd_xaml_cs_ShiQuGuoChengZhongFaShengYiChang = 1498,

        /// <summary>
        /// 拾取元素操作失败,请重试!
        /// </summary>
        DataCollectOverlayWnd_xaml_cs_ShiQuYuanSuCaoZuoShiBaiQing = 1499,

        /// <summary>
        /// 成功识别表格内元素，是否确定采集表格数据？
        /// </summary>
        DataCollectOverlayWnd_xaml_cs_ChengGongShiBieBiaoGeNeiYuanSu = 1500,

        /// <summary>
        /// 知道了
        /// </summary>
        DataCollectOverlayWnd_xaml_cs_ZhiDaoLe = 1501,

        /// <summary>
        /// 编辑表头
        /// </summary>
        DataCollectWnd_xaml_BianJiBiaoTou = 1502,

        /// <summary>
        /// 编辑列数据
        /// </summary>
        DataCollectWnd_xaml_BianJiLieShuJu = 1503,

        /// <summary>
        /// 拾取同类数据
        /// </summary>
        DataCollectWnd_xaml_ShiQuTongLeiShuJu = 1504,

        /// <summary>
        /// 提取文本内容
        /// </summary>
        DataCollectWnd_xaml_TiQuWenBenNeiRong = 1505,

        /// <summary>
        /// 提取链接地址
        /// </summary>
        DataCollectWnd_xaml_TiQuLianJieDiZhi = 1506,

        /// <summary>
        /// 提取图片地址
        /// </summary>
        DataCollectWnd_xaml_TiQuTuPianDiZhi = 1507,

        /// <summary>
        /// 格式化内容
        /// </summary>
        DataCollectWnd_xaml_GeShiHuaNeiRong = 1508,

        /// <summary>
        /// 前移一列
        /// </summary>
        DataCollectWnd_xaml_QianYiYiLie = 1509,

        /// <summary>
        /// 后移一列
        /// </summary>
        DataCollectWnd_xaml_HouYiYiLie = 1510,

        /// <summary>
        /// 删除列
        /// </summary>
        DataCollectWnd_xaml_ShanChuLie = 1511,

        /// <summary>
        /// 表头重命名
        /// </summary>
        DataCollectWnd_xaml_BiaoTouZhongMingMing = 1512,

        /// <summary>
        /// 帮助
        /// </summary>
        DataCollectWnd_xaml_BangZhu = 1513,

        /// <summary>
        /// 位置还原
        /// </summary>
        DataCollectWnd_xaml_WeiZhiHaiYuan = 1514,

        /// <summary>
        /// 底部固定
        /// </summary>
        DataCollectWnd_xaml_DiBuGuDing = 1515,

        /// <summary>
        /// 采集
        /// </summary>
        DataCollectWnd_xaml_CaiJi = 1516,

        /// <summary>
        /// 采集页数
        /// </summary>
        DataCollectWnd_xaml_CaiJiYeShu = 1517,

        /// <summary>
        /// 每页延迟(毫秒)
        /// </summary>
        DataCollectWnd_xaml_MeiYeYanChiHaoMiao = 1518,

        /// <summary>
        /// 拾取翻页元素
        /// </summary>
        DataCollectWnd_xaml_ShiQuFanYeYuanSu = 1519,

        /// <summary>
        /// 清空配置
        /// </summary>
        DataCollectWnd_xaml_QingKongPeiZhi = 1520,

        /// <summary>
        /// 当前页已采集
        /// </summary>
        DataCollectWnd_xaml_DangQianYeYiCaiJi = 1521,

        /// <summary>
        /// 条数据
        /// </summary>
        DataCollectWnd_xaml_TiaoShuJu = 1522,

        /// <summary>
        /// 数据纠错
        /// </summary>
        DataCollectWnd_xaml_ShuJuJiuCuo = 1523,

        /// <summary>
        /// 刷新
        /// </summary>
        DataCollectWnd_xaml_ShuaXin = 1524,

        /// <summary>
        /// 列表仅展示部分数据，实际采集结果以设置的采集页面范围为准
        /// </summary>
        DataCollectWnd_xaml_LieBiaoJinZhanShiBuFenShuJu = 1525,

        /// <summary>
        /// 编辑表格元素
        /// </summary>
        DataCollectWnd_xaml_BianJiBiaoGeYuanSu = 1526,

        /// <summary>
        /// 数据预览
        /// </summary>
        DataCollectWnd_xaml_ShuJuYuLan = 1527,

        /// <summary>
        /// 数据采集中请稍等...
        /// </summary>
        DataCollectWnd_xaml_ShuJuCaiJiZhongQingShaoDeng = 1528,

        /// <summary>
        /// 有意外阻止采集，请保存当前数据再重新进入拾取
        /// </summary>
        DataCollectWnd_xaml_cs_YouYiWaiZuZhiCaiJiQingBao = 1529,

        /// <summary>
        /// 有意外阻止采集，请重新拾取
        /// </summary>
        DataCollectWnd_xaml_cs_YouYiWaiZuZhiCaiJiQingZhong = 1530,

        /// <summary>
        /// 是否取消此次操作?
        /// </summary>
        DataCollectWnd_xaml_cs_ShiFouQuXiaoCiCiCaoZuo = 1531,

        /// <summary>
        /// 还原
        /// </summary>
        DataCollectWnd_xaml_cs_HaiYuan = 1532,

        /// <summary>
        /// 获取点击事件
        /// </summary>
        DataCollectWnd_xaml_cs_HuoQuDianJiShiJian = 1533,

        /// <summary>
        /// 正在同步设备...
        /// </summary>
        LoadWaitingWnd_xaml_ZhengZaiTongBuSheBei = 1534,

        /// <summary>
        /// 拾取数据与已有数据非同类数据时，无法拾取!
        /// </summary>
        WarningWnd_xaml_ShiQuShuJuYuYiYouShuJuFei = 1535,

        /// <summary>
        /// AI分析...
        /// </summary>
        ElementHighlighter_cs_AIFenXi = 1536,

        /// <summary>
        /// 流程编辑
        /// </summary>
        HighlightBorder_cs_LiuChengBianJi = 1537,

        /// <summary>
        /// 智能分析...
        /// </summary>
        HighlightTag_cs_ZhiNengFenXi = 1538,

        /// <summary>
        /// 确 定
        /// </summary>
        CaptureCVForOcrTableWnd_xaml_QueDing = 1539,

        /// <summary>
        /// 存图传入的{DateTime.Now.ToString(
        /// </summary>
        CaptureCVForOcrTableWnd_xaml_cs_CunTuChuanRuDeDate = 1540,

        /// <summary>
        /// 区域拾取
        /// </summary>
        CaptureCVForOcrTableWnd_xaml_cs_QuYuShiQu = 1541,

        /// <summary>
        /// 鼠标位置
        /// </summary>
        InspectInfoWnd_xaml_ShuBiaoWeiZhi = 1542,

        /// <summary>
        /// 界面类型
        /// </summary>
        InspectInfoWnd_xaml_JieMianLeiXing = 1543,

        /// <summary>
        /// 拾取元素
        /// </summary>
        InspectInfoWnd_xaml_ShiQuYuanSu = 1544,

        /// <summary>
        /// 拾取浮窗元素
        /// </summary>
        InspectInfoWnd_xaml_ShiQuFuChuangYuanSu = 1545,

        /// <summary>
        /// 智能检测中
        /// </summary>
        InspectInfoWnd_xaml_ZhiNengJianCeZhong = 1546,

        /// <summary>
        /// 拾取区域
        /// </summary>
        InspectInfoWnd_xaml_cs_ShiQuQuYu = 1547,

        /// <summary>
        /// 智能拾取
        /// </summary>
        InspectorStatusForm_cs_ZhiNengShiQu = 1548,

        /// <summary>
        /// 普通拾取
        /// </summary>
        InspectorStatusForm_cs_PuTongShiQu = 1549,

        /// <summary>
        /// 浮窗拾取
        /// </summary>
        InspectorStatusForm_cs_FuChuangShiQu = 1550,

        /// <summary>
        /// CV拾取
        /// </summary>
        InspectorStatusForm_cs_CVShiQu = 1551,

        /// <summary>
        /// 拾取方式
        /// </summary>
        InspectorStatusForm_Designer_cs_ShiQuFangShi = 1552,

        /// <summary>
        /// 拾取
        /// </summary>
        InspectorStatusForm_Designer_cs_ShiQu = 1553,

        /// <summary>
        /// 智能
        /// </summary>
        InspectorStatusForm_Designer_cs_ZhiNeng = 1554,

        /// <summary>
        /// 普通
        /// </summary>
        InspectorStatusForm_Designer_cs_PuTong = 1555,

        /// <summary>
        /// 浮窗
        /// </summary>
        InspectorStatusForm_Designer_cs_FuChuang = 1556,

        /// <summary>
        /// 切换
        /// </summary>
        InspectorStatusForm_Designer_cs_QieHuan = 1557,

        /// <summary>
        /// 获取当前坐标
        /// </summary>
        MousePosition_xaml_HuoQuDangQianZuoBiao = 1558,

        /// <summary>
        /// 未识别到元素，请重新拾取！
        /// </summary>
        NewOverlayForm_cs_WeiShiBieDaoYuanSuQingZhongXin = 1559,

        /// <summary>
        /// 远程桌面连接
        /// </summary>
        NewOverlayForm_cs_YuanChengZhuoMianLianJie = 1560,

        /// <summary>
        /// Ctrl+单击
        /// </summary>
        NewOverlayForm_cs_CtrlDanJi = 1561,

        /// <summary>
        /// Shift+单击
        /// </summary>
        NewOverlayForm_cs_ShiftDanJi = 1562,

        /// <summary>
        /// CV元素解析失败，截图发生异常，请稍后重试
        /// </summary>
        NewOverlayForm_cs_CVYuanSuJieXiShiBaiJie = 1563,

        /// <summary>
        /// 拾取执行异常，请重试
        /// </summary>
        NewOverlayForm_cs_ShiQuZhiXingYiChangQingZhongShi = 1564,

        /// <summary>
        /// 拾取元素执行异常，请重试
        /// </summary>
        NewOverlayForm_cs_ShiQuYuanSuZhiXingYiChangQing = 1565,

        /// <summary>
        /// 缺少文件地址参数
        /// </summary>
        Capture_cs_QueShaoWenJianDiZhiCanShu = 1566,

        /// <summary>
        /// Windows桌面
        /// </summary>
        Capture_cs_WindowsZhuoMian = 1567,

        /// <summary>
        /// OCR元素
        /// </summary>
        CaptureCV_cs_OCRYuanSu = 1568,

        /// <summary>
        /// 图片保存地址为空
        /// </summary>
        CaptureScreen_cs_TuPianBaoCunDiZhiWeiKong = 1569,

        /// <summary>
        /// 获取失败,拾取的不是SAP表格元素
        /// </summary>
        ClickSapTableCell_cs_HuoQuShiBaiShiQuDeBuShi = 1570,

        /// <summary>
        /// [400012074][元素判断]元素判断失败
        /// </summary>
        IsSimilarElementExist_cs_400012074 = 1571,

        /// <summary>
        /// 流程编辑初始化完成
        /// </summary>
        ProcessCreator_cs_LiuChengBianJiChuShiHuaWanCheng = 1572,

        /// <summary>
        /// 流程编辑模式已启动
        /// </summary>
        ProcessCreator_cs_LiuChengBianJiMoShiYiQiDong = 1573,

        /// <summary>
        /// 重复操作，流程编辑模式已启动
        /// </summary>
        ProcessCreator_cs_ZhongFuCaoZuoLiuChengBianJiMo = 1574,

        /// <summary>
        /// 流程编辑模式已暂停
        /// </summary>
        ProcessCreator_cs_LiuChengBianJiMoShiYiZanTing = 1575,

        /// <summary>
        /// 流程编辑模式已结束
        /// </summary>
        ProcessCreator_cs_LiuChengBianJiMoShiYiJieShu = 1576,

        /// <summary>
        /// 流程编辑模式已重启
        /// </summary>
        ProcessCreator_cs_LiuChengBianJiMoShiYiZhongQi = 1577,

        /// <summary>
        /// 重新拾取失败
        /// </summary>
        ProcessCreator_cs_ZhongXinShiQuShiBai = 1578,

        /// <summary>
        /// 重新拾取已完成
        /// </summary>
        ProcessCreator_cs_ZhongXinShiQuYiWanCheng = 1579,

        /// <summary>
        /// 重新截图已取消
        /// </summary>
        ProcessCreator_cs_ZhongXinJieTuYiQuXiao = 1580,

        /// <summary>
        /// 重新截图已完成
        /// </summary>
        ProcessCreator_cs_ZhongXinJieTuYiWanCheng = 1581,

        /// <summary>
        /// 已切换为截图模式
        /// </summary>
        ProcessCreator_cs_YiQieHuanWeiJieTuMoShi = 1582,

        /// <summary>
        /// 重复调用、截图模式已切换
        /// </summary>
        ProcessCreator_cs_ZhongFuDiaoYongJieTuMoShiYi = 1583,

        /// <summary>
        /// 流程编辑状态:{IsStartFlagForExcel}
        /// </summary>
        ProcessCreator_cs_LiuChengBianJiZhuangTaiIs = 1584,

        /// <summary>
        /// 流程编辑初始化完成(Dummy)
        /// </summary>
        ProcessCreator_cs_LiuChengBianJiChuShiHuaWanCheng1 = 1585,

        /// <summary>
        /// 参数错误
        /// </summary>
        ProcessCreator_cs_CanShuCuoWu = 1586,

        /// <summary>
        /// 截图已完成
        /// </summary>
        ScreenShot_cs_JieTuYiWanCheng = 1587,

        /// <summary>
        /// 坐标参数值非法：{paramModel.Position.X};{paramModel.Position.Y};{paramModel.Position.Width};{paramModel.Position.Height};
        /// </summary>
        ScreenShotExecutor_cs_ZuoBiaoCanShuZhiFeiFap = 1588,

        /// <summary>
        /// 截图范围参数值非法：{paramModel.ShotRange}
        /// </summary>
        ScreenShotExecutor_cs_JieTuFanWeiCanShuZhiFeiFa = 1589,

        /// <summary>
        /// 截图范围参数值非法：{paramModel.ImagePath}
        /// </summary>
        ScreenShotExecutor_cs_JieTuFanWeiCanShuZhiFeiFa1 = 1590,

        /// <summary>
        /// 截图完成
        /// </summary>
        ScreenShotExecutor_cs_JieTuWanCheng = 1591,

        /// <summary>
        /// 截图失败
        /// </summary>
        ScreenShotExecutor_cs_JieTuShiBai = 1592,

        /// <summary>
        /// 点击目标
        /// </summary>
        ActionDataModels_cs_DianJiMuBiao = 1593,

        /// <summary>
        /// 鼠标点击
        /// </summary>
        ActionDataModels_cs_ShuBiaoDianJi = 1594,

        /// <summary>
        /// 鼠标拖拽
        /// </summary>
        ActionDataModels_cs_ShuBiaoTuoZhuai = 1595,

        /// <summary>
        /// 模拟按键
        /// </summary>
        ActionDataModels_cs_MoNiAnJian = 1596,

        /// <summary>
        /// 输入内容
        /// </summary>
        ActionDataModels_cs_ShuRuNeiRong = 1597,

        /// <summary>
        /// 获取文本
        /// </summary>
        ActionDataModels_cs_HuoQuWenBen = 1598,

        /// <summary>
        /// 勾选框操作
        /// </summary>
        ActionDataModels_cs_GouXuanKuangCaoZuo = 1599,

        /// <summary>
        /// 获取子元素
        /// </summary>
        ActionDataModels_cs_HuoQuZiYuanSu = 1600,

        /// <summary>
        /// 下拉框操作
        /// </summary>
        ActionDataModels_cs_XiaLaKuangCaoZuo = 1601,

        /// <summary>
        /// 激活窗口
        /// </summary>
        ActionDataModels_cs_JiHuoChuangKou = 1602,

        /// <summary>
        /// 移动鼠标
        /// </summary>
        ActionDataModels_cs_YiDongShuBiao = 1603,

        /// <summary>
        /// 输入密码
        /// </summary>
        ActionDataModels_cs_ShuRuMiMa = 1604,

        /// <summary>
        /// 获取坐标
        /// </summary>
        ActionDataModels_cs_HuoQuZuoBiao = 1605,

        /// <summary>
        /// 文字识别
        /// </summary>
        ActionDataModels_cs_WenZiShiBie = 1606,

        /// <summary>
        /// 判断存在
        /// </summary>
        ActionDataModels_cs_PanDuanCunZai = 1607,

        /// <summary>
        /// 循环相似元素
        /// </summary>
        ActionDataModels_cs_XunHuanXiangSiYuanSu = 1608,

        /// <summary>
        /// 启动应用
        /// </summary>
        ActionDataModels_cs_QiDongYingYong = 1609,

        /// <summary>
        /// 剪切文本
        /// </summary>
        ActionDataModels_cs_JianQieWenBen = 1610,

        /// <summary>
        /// 打开网页
        /// </summary>
        ActionDataModels_cs_DaKaiWangYe = 1611,

        /// <summary>
        /// 鼠标移到元素
        /// </summary>
        ActionDataModels_cs_ShuBiaoYiDaoYuanSu = 1612,

        /// <summary>
        /// 等待元素
        /// </summary>
        ActionDataModels_cs_DengDaiYuanSu = 1613,

        /// <summary>
        /// 图像OCR
        /// </summary>
        ActionDataModels_cs_TuXiangOCR = 1614,

        /// <summary>
        /// 新建Word
        /// </summary>
        ActionDataModels_cs_XinJianWord = 1615,

        /// <summary>
        /// 新建Excel
        /// </summary>
        ActionDataModels_cs_XinJianExcel = 1616,

        /// <summary>
        /// 打开文件夹
        /// </summary>
        ActionDataModels_cs_DaKaiWenJianJia = 1617,

        /// <summary>
        /// 判断文件夹
        /// </summary>
        ActionDataModels_cs_PanDuanWenJianJia = 1618,

        /// <summary>
        /// 获取pdf文本
        /// </summary>
        ActionDataModels_cs_HuoQupdfWenBen = 1619,

        /// <summary>
        /// 复制文件
        /// </summary>
        ActionDataModels_cs_FuZhiWenJian = 1620,

        /// <summary>
        /// 打开文档
        /// </summary>
        ActionDataModels_cs_DaKaiWenDang = 1621,

        /// <summary>
        /// 打开Excel
        /// </summary>
        ActionDataModels_cs_DaKaiExcel = 1622,

        /// <summary>
        /// 获取路径
        /// </summary>
        ActionDataModels_cs_HuoQuLuJing = 1623,

        /// <summary>
        /// 智能登录
        /// </summary>
        ActionDataModels_cs_ZhiNengDengLu = 1624,

        /// <summary>
        /// 滚动页面
        /// </summary>
        ActionDataModels_cs_GunDongYeMian = 1625,

        /// <summary>
        /// 数据处理
        /// </summary>
        ActionDataModels_cs_ShuJuChuLi = 1626,

        /// <summary>
        /// 登录组件
        /// </summary>
        ActionDataModels_cs_DengLuZuJian = 1627,

        /// <summary>
        /// 更多动作
        /// </summary>
        ActionPopup_xaml_GengDuoDongZuo = 1628,

        /// <summary>
        /// 搜索动作
        /// </summary>
        ActionPopupWindow_xaml_SouSuoDongZuo = 1629,

        /// <summary>
        /// 搜索建议
        /// </summary>
        ActionPopupWindow_xaml_SouSuoJianYi = 1630,

        /// <summary>
        /// 新建
        /// </summary>
        SomeUtils_cs_XinJian = 1631,

        /// <summary>
        /// 网页
        /// </summary>
        ActionPopupViewModel_cs_WangYe = 1632,

        /// <summary>
        /// 元素
        /// </summary>
        ActionPopupViewModel_cs_YuanSu = 1633,

        /// <summary>
        /// 浏览器
        /// </summary>
        ActionPopupViewModel_cs_LiuLanQi = 1634,

        /// <summary>
        /// 文本
        /// </summary>
        ActionPopupViewModel_cs_WenBen = 1635,

        /// <summary>
        /// 点击
        /// </summary>
        ActionPopupViewModel_cs_DianJi = 1636,

        /// <summary>
        /// 循环
        /// </summary>
        ActionPopupViewModel_cs_XunHuan = 1637,

        /// <summary>
        /// 变量
        /// </summary>
        ActionPopupViewModel_cs_BianLiang = 1638,

        /// <summary>
        /// 异常
        /// </summary>
        ActionPopupViewModel_cs_YiChang = 1639,

        /// <summary>
        /// 日志
        /// </summary>
        ActionPopupViewModel_cs_RiZhi = 1640,

        /// <summary>
        /// 智能分析中...
        /// </summary>
        ActionPopupViewModel_cs_ZhiNengFenXiZhong = 1641,

        /// <summary>
        /// 登录区域
        /// </summary>
        ActionPopupViewModel_cs_DengLuQuYu = 1642,

        /// <summary>
        /// 用户名
        /// </summary>
        ActionPopupViewModel_cs_YongHuMing = 1643,

        /// <summary>
        /// 密码
        /// </summary>
        ActionPopupViewModel_cs_MiMa = 1644,

        /// <summary>
        /// 登录
        /// </summary>
        ActionPopupViewModel_cs_DengLu = 1645,

        /// <summary>
        /// 验证码
        /// </summary>
        ActionPopupViewModel_cs_YanZhengMa = 1646,

        /// <summary>
        /// 所有文件(*.*)|*.*
        /// </summary>
        ControlAttachProperty_cs_SuoYouWenJian = 1647,

        /// <summary>
        /// |所有文件(*.*)|*.*
        /// </summary>
        ControlAttachProperty_cs_SuoYouWenJian1 = 1648,

        /// <summary>
        /// 请选择文件
        /// </summary>
        ControlAttachProperty_cs_QingXuanZeWenJian = 1649,

        /// <summary>
        /// 请选择文件路径
        /// </summary>
        ControlAttachProperty_cs_QingXuanZeWenJianLuJing = 1650,

        /// <summary>
        /// 文件保存路径
        /// </summary>
        ControlAttachProperty_cs_WenJianBaoCunLuJing = 1651,

        /// <summary>
        /// 不准确
        /// </summary>
        HoverToggle_xaml_BuZhunQue = 1652,

        /// <summary>
        /// /II.RPA.Plugin.GUI;Component/ProcessCreator/Theme/#思源黑体 CN
        /// </summary>
        HoverToggle_xaml_IIRPAPl = 1653,

        /// <summary>
        /// 其他图标
        /// </summary>
        AnalyzerForCv_cs_QiTaTuBiao = 1654,

        /// <summary>
        /// 任务栏
        /// </summary>
        AnalyzerForExplorer_cs_RenWuLan = 1655,

        /// <summary>
        /// 文本框_Edit
        /// </summary>
        AnalyzerForInput_cs_WenBenKuangEdit = 1656,

        /// <summary>
        /// 文本框
        /// </summary>
        AnalyzerForInput_cs_WenBenKuang = 1657,

        /// <summary>
        /// 选择动作
        /// </summary>
        ElementPopup_xaml_XuanZeDongZuo = 1658,

        /// <summary>
        /// 选择目标
        /// </summary>
        ElementTracker_cs_XuanZeMuBiao = 1659,

        /// <summary>
        /// 其他
        /// </summary>
        ElementTracker_cs_QiTa = 1660,

        /// <summary>
        /// 截图模式
        /// </summary>
        CaptureWindow_xaml_JieTuMoShi = 1661,

        /// <summary>
        /// 保存
        /// </summary>
        CaptureWindow_xaml_BaoCun = 1662,

        /// <summary>
        /// 保存到元素库,并添加到到剪切板
        /// </summary>
        CaptureWindow_xaml_BaoCunDaoYuanSuKuBingTianJia = 1663,

        /// <summary>
        /// 确定截图
        /// </summary>
        CaptureWindow_xaml_QueDingJieTu = 1664,

        /// <summary>
        /// 坐标:
        /// </summary>
        CaptureWindow_xaml_ZuoBiao = 1665,

        /// <summary>
        /// Images\元素点击.png
        /// </summary>
        ColorDictionary_xaml_ImagesYuanSuDian = 1666,

        /// <summary>
        /// Images\鼠标拖拽.png
        /// </summary>
        ColorDictionary_xaml_ImagesShuBiaoTuo = 1667,

        /// <summary>
        /// Images\文本输入.png
        /// </summary>
        ColorDictionary_xaml_ImagesWenBenShu = 1668,

        /// <summary>
        /// Images\模拟键盘.png
        /// </summary>
        ColorDictionary_xaml_ImagesMoNiJian = 1669,

        /// <summary>
        /// Images\鼠标点击.png
        /// </summary>
        ColorDictionary_xaml_ImagesShuBiaoDian = 1670,

        /// <summary>
        /// Images\获取文本.png
        /// </summary>
        ColorDictionary_xaml_ImagesHuoQuWen = 1671,

        /// <summary>
        /// 未识别组件
        /// </summary>
        StyleDictionary_xaml_WeiShiBieZuJian = 1672,

        /// <summary>
        /// 输入文本内容
        /// </summary>
        StyleDictionary_xaml_ShuRuWenBenNeiRong = 1673,

        /// <summary>
        /// 事件无法调用任何订户
        /// </summary>
        CaptureTask_cs_ShiJianWuFaDiaoYongRenHeDingHu = 1674,

        /// <summary>
        /// CV元素拾取任务
        /// </summary>
        CVCaptureTask_cs_CVYuanSuShiQuRenWu = 1675,

        /// <summary>
        /// 开始智能识别...
        /// </summary>
        CVCaptureTask_cs_KaiShiZhiNengShiBie = 1676,

        /// <summary>
        /// 数据采集普通元素拾取任务
        /// </summary>
        DataCollectingCaptureTask_cs_ShuJuCaiJiPuTongYuanSuShiQu = 1677,

        /// <summary>
        /// 普通元素拾取任务
        /// </summary>
        OrdinaryCaptureTask_cs_PuTongYuanSuShiQuRenWu = 1678,

        /// <summary>
        /// 过滤器关键字不符合规范，关键字：{nodeInfo[0]}
        /// </summary>
        FilterHelper_cs_GuoLuQiGuanJianZiBuFuHeGui = 1679,

        /// <summary>
        /// 此方法只使用于ExtendParam数据类型为Dictionary<string,object>的情况
        /// </summary>
        UiElementParams_cs_CiFangFaZhiShiYongYuExt = 1680,

        /// <summary>
        /// - 360极速浏览器
        /// </summary>
        BrowserCommons_cs_360JiSuLiuLanQi = 1681,

        /// <summary>
        /// 校验U盾密码
        /// </summary>
        IENode_cs_XiaoYanUDunMiMa = 1682,

        /// <summary>
        /// 验证U盾密码
        /// </summary>
        IENode_cs_YanZhengUDunMiMa = 1683,

        /// <summary>
        /// );//获取IE内容窗体句柄 IE内部的ClassName为
        /// </summary>
        SwitchToIEWebbrowser_cs_HuoQuIENeiRong = 1684,

        ///// <summary>
        ///// 选择的元素未找到
        ///// </summary>
        //AndroidMobile - 复制_cs_XuanZeDeYuanSuWeiZhaoDao = 1685,

        ///// <summary>
        ///// 入参错误；只能设置为横屏或竖屏
        ///// </summary>
        //AndroidMobile - 复制_cs_RuCanCuoWuZhiNengSheZhiWei = 1686,

        ///// <summary>
        ///// 本地文件不存在
        ///// </summary>
        //AndroidMobile - 复制_cs_BenDiWenJianBuCunZai = 1687,

        /// <summary>
        /// 方法是原来的FindElement方法,即将被弃用
        /// </summary>
        AndroidMobile_cs_FangFaShiYuanLaiDeFind = 1688,

        /// <summary>
        /// 截屏保存的文件路径不存在
        /// </summary>
        AndroidMobile_cs_JiePingBaoCunDeWenJianLuJingBu = 1689,

        /// <summary>
        /// 华为
        /// </summary>
        AndroidMobile_cs_HuaWei = 1690,

        /// <summary>
        /// 荣耀
        /// </summary>
        AndroidMobile_cs_RongYao = 1691,

        /// <summary>
        /// 视频
        /// </summary>
        MobileManager_cs_ShiPin = 1692,

        /// <summary>
        /// 执行流程使手机设备连接异常,已自动关闭设备界面!
        /// </summary>
        MobileManager_cs_ZhiXingLiuChengShiShouJiSheBeiLian = 1693,

        /// <summary>
        /// 异常提醒
        /// </summary>
        MobileManager_cs_YiChangTiXing = 1694,

        /// <summary>
        /// 不属于同一种类型元素
        /// </summary>
        MobileManager_cs_BuShuYuTongYiZhongLeiXingYuanSu = 1695,

        /// <summary>
        /// 元素结构对应版本太老,不支持相似元素操作
        /// </summary>
        MobileManager_cs_YuanSuJieGouDuiYingBanBenTaiLao = 1696,

        /// <summary>
        /// 手机CV通过selector转化元素：selector有误
        /// </summary>
        CVMobileElement_cs_ShouJiCVTongGuosele = 1697,

        /// <summary>
        /// 手机CV通过selector转化元素失败{ex.Message}
        /// </summary>
        CVMobileElement_cs_ShouJiCVTongGuosele1 = 1698,

        /// <summary>
        /// 连接手机
        /// </summary>
        MobileForm_xaml_LianJieShouJi = 1699,

        /// <summary>
        /// /II.RPA.Plugin.Mobile;component/Resource/刷新@2x.png
        /// </summary>
        MobileForm_xaml_IIRPAPl = 1700,

        /// <summary>
        /// /II.RPA.Plugin.Mobile;component/Resource/关闭@2x.png
        /// </summary>
        MobileForm_xaml_IIRPAPl1 = 1701,

        /// <summary>
        /// /II.RPA.Plugin.Mobile;component/Resource/返回@2x.png
        /// </summary>
        MobileForm_xaml_IIRPAPl2 = 1702,

        /// <summary>
        /// /II.RPA.Plugin.Mobile;component/Resource/多任务@2x.png
        /// </summary>
        MobileForm_xaml_IIRPAPl3 = 1703,

        /// <summary>
        /// 是否断开手机设备的连接？
        /// </summary>
        MobileForm_xaml_cs_ShiFouDuanKaiShouJiSheBeiDeLian = 1704,

        /// <summary>
        /// 断开连接？
        /// </summary>
        MobileForm_xaml_cs_DuanKaiLianJie = 1705,

        /// <summary>
        /// 手机设备连接异常,已自动关闭设备界面!
        /// </summary>
        MobileForm_xaml_cs_ShouJiSheBeiLianJieYiChangYi = 1706,

        /// <summary>
        /// 坐标位置:
        /// </summary>
        SelectorToolWnd_xaml_ZuoBiaoWeiZhi = 1707,

        /// <summary>
        /// 窗口类型:
        /// </summary>
        SelectorToolWnd_xaml_ChuangKouLeiXing = 1708,

        /// <summary>
        /// 手机模拟器
        /// </summary>
        SelectorToolWnd_xaml_ShouJiMoNiQi = 1709,

        /// <summary>
        /// 【Ctrl+鼠标左键】
        /// </summary>
        SelectorToolWnd_xaml_CtrlShuBiaoZuoJian = 1710,

        /// <summary>
        /// 发送操作命令...
        /// </summary>
        SendWaiting_xaml_FaSongCaoZuoMingLing = 1711,

        /// <summary>
        /// 请确认手机是否已连接成功
        /// </summary>
        AnalogInput_cs_QingQueRenShouJiShiFouYiLianJie = 1712,

        /// <summary>
        /// 设置文本失败
        /// </summary>
        AnalogInput_cs_SheZhiWenBenShiBai = 1713,

        /// <summary>
        /// 请先连接设备
        /// </summary>
        CheckMobileSelector_cs_QingXianLianJieSheBei = 1714,

        /// <summary>
        /// 未找到匹配元素
        /// </summary>
        CheckMobileSelector_cs_WeiZhaoDaoPiPeiYuanSu = 1715,

        /// <summary>
        /// 手机元素点击失败
        /// </summary>
        ClickMobileElement_cs_ShouJiYuanSuDianJiShiBai = 1716,

        /// <summary>
        /// 当前电脑已存在连接手机
        /// </summary>
        ConnectDevice_cs_DangQianDianNaoYiCunZaiLianJieShou = 1717,

        /// <summary>
        /// 智能元素点击失败
        /// </summary>
        CVClick_cs_ZhiNengYuanSuDianJiShiBai = 1718,

        /// <summary>
        /// 手机中获取文件成功
        /// </summary>
        GetFileFromMobile_cs_ShouJiZhongHuoQuWenJianChengGong = 1719,

        /// <summary>
        /// 拾取成功
        /// </summary>
        GetMobileElementCV_cs_ShiQuChengGong = 1720,

        ///// <summary>
        ///// 执行超时
        ///// </summary>
        //GetMobileElementInfo - 复制_cs_ZhiXingChaoShi = 1721,

        ///// <summary>
        ///// 拾取元素失败
        ///// </summary>
        //GetMobileElementInfo - 复制_cs_ShiQuYuanSuShiBai = 1722,

        /// <summary>
        /// 获取坐标成功
        /// </summary>
        GetMobileElementPoint_cs_HuoQuZuoBiaoChengGong = 1723,

        /// <summary>
        /// 手机截图失败
        /// </summary>
        GetMobileScreenshot_cs_ShouJiJieTuShiBai = 1724,

        /// <summary>
        /// 不属于同一种相似元素
        /// </summary>
        GetMobileSimilarElementInfo_cs_BuShuYuTongYiZhongXiangSiYuanSu = 1725,

        /// <summary>
        /// 设置屏幕方向失败
        /// </summary>
        SetMobileOrientation_cs_SheZhiPingMuFangXiangShiBai = 1726,

        /// <summary>
        /// 智能设置文本失败
        /// </summary>
        SetTextCV_cs_ZhiNengSheZhiWenBenShiBai = 1727,

        /// <summary>
        /// 拾取表格失败:获取表格数据为空
        /// </summary>
        OcrHelper_cs_ShiQuBiaoGeShiBaiHuoQuBiao = 1728,

        /// <summary>
        /// ExecuteCV 返回值解析失败:{ex.ToString()}
        /// </summary>
        AlgJsonAnaylse_cs_ExecuteCV = 1729,

        /// <summary>
        /// 图片读取失败
        /// </summary>
        GetOcrTableDataByImg_cs_TuPianDuQuShiBai = 1730,

        /// <summary>
        /// 本地图片解析处理返回值失败:{_ErrorMsg}
        /// </summary>
        GetOcrTableDataByImg_cs_BenDiTuPianJieXiChuLiFanHui = 1731,

        /// <summary>
        /// 本地图片解析异常:{ex.ToString()}
        /// </summary>
        GetOcrTableDataByImg_cs_BenDiTuPianJieXiYiChang = 1732,

        /// <summary>
        /// ExecuteCV OCR表格解析失败:{ex}
        /// </summary>
        GetOcrTableDataByImg_cs_ExecuteCV = 1733,

        /// <summary>
        /// OCR表格解析未获取目标窗体
        /// </summary>
        GetOcrTableDataBySelector_cs_OCRBiaoGeJieXiWeiHuoQu = 1734,

        /// <summary>
        /// 执行截图
        /// </summary>
        GetOcrTableDataBySelector_cs_ZhiXingJieTu = 1735,

        /// <summary>
        /// 表格解析执行发生异常，详情请观察日志或者联系技术人员处理
        /// </summary>
        GetOcrTableDataBySelector_cs_BiaoGeJieXiZhiXingFaShengYiChang = 1736,

        /// <summary>
        /// 表格拾取执行的解析处理返回值失败:{_ErrorMsg}
        /// </summary>
        GetOcrTableDataBySelector_cs_BiaoGeShiQuZhiXingDeJieXiChu = 1737,

        /// <summary>
        /// 表格解析拾取执行异常:{ex}
        /// </summary>
        GetOcrTableDataBySelector_cs_BiaoGeJieXiShiQuZhiXingYiChang = 1738,

        /// <summary>
        /// 拾取截图异常：{ex}
        /// </summary>
        MakeOcrTableSelector_cs_ShiQuJieTuYiChangex = 1739,

        /// <summary>
        /// 截屏获取区域信息失败
        /// </summary>
        MakeOcrTableSelector_cs_JiePingHuoQuQuYuXinXiShiBai = 1740,

        /// <summary>
        /// 拾取截图
        /// </summary>
        MakeOcrTableSelector_cs_ShiQuJieTu = 1741,

        /// <summary>
        /// 解析表格附加信息失败:{_ErrorMsg}
        /// </summary>
        MakeOcrTableSelector_cs_JieXiBiaoGeFuJiaXinXiShiBai = 1742,

        /// <summary>
        /// 缺少{_Flag1}{_Flag2}
        /// </summary>
        SaasAuthenticationHelper_cs_QueShaoFlag1 = 1743,

        /// <summary>
        /// 请求数据失败. 返回的 HTTP 状态码：{0}
        /// </summary>
        SaasAuthenticationHelper_cs_QingQiuShuJuShiBaiFanHui = 1744,

        /// <summary>
        /// 拾取的不是SAP表格元素不支持获取表格操作
        /// </summary>
        SapNode_cs_ShiQuDeBuShiSAPBiaoGe = 1745,

        /// <summary>
        /// 行号入参错误，必须为整数
        /// </summary>
        SapNode_cs_XingHaoRuCanCuoWuBiXuWei = 1746,

        /// <summary>
        /// 行号必须为大于零的整数
        /// </summary>
        SapNode_cs_XingHaoBiXuWeiDaYuLingDeZheng = 1747,

        /// <summary>
        /// 行号越界
        /// </summary>
        SapNode_cs_XingHaoYueJie = 1748,

        /// <summary>
        /// 列号入参错误，必须为整数
        /// </summary>
        SapNode_cs_LieHaoRuCanCuoWuBiXuWei = 1749,

        /// <summary>
        /// 列号必须为大于零的整数
        /// </summary>
        SapNode_cs_LieHaoBiXuWeiDaYuLingDeZheng = 1750,

        /// <summary>
        /// 列号越界
        /// </summary>
        SapNode_cs_LieHaoYueJie = 1751,

        /// <summary>
        /// 无法获取
        /// </summary>
        GuiTableControlAction_cs_WuFaHuoQu = 1752,

        /// <summary>
        ///  - 搜狗高速浏览器
        /// </summary>
        SogouNode_cs_SouGouGaoSuLiuLanQi = 1753,

        /// <summary>
        /// 解锁插件未安装
        /// </summary>
        RDPUnlock_cs_JieSuoChaJianWeiAnZhuang = 1754,

        /// <summary>
        /// USB Hub驱动未安装
        /// </summary>
        UsbHubHelper_cs_USBHubQuDongWei = 1755,

        /// <summary>
        /// 格式不对
        /// </summary>
        UsbHubHelper_cs_GeShiBuDui = 1756,

        /// <summary>
        /// 通道编号不对
        /// </summary>
        UsbHubHelper_cs_TongDaoBianHaoBuDui = 1757,

        /// <summary>
        /// 命令不对
        /// </summary>
        UsbHubHelper_cs_MingLingBuDui = 1758,

        /// <summary>
        /// 参数不对
        /// </summary>
        UsbHubHelper_cs_CanShuBuDui = 1759,

        /// <summary>
        /// 参数不可为空
        /// </summary>
        LocalClose_cs_CanShuBuKeWeiKong = 1760,

        /// <summary>
        /// 通道号参数不可为空
        /// </summary>
        LocalClose_cs_TongDaoHaoCanShuBuKeWeiKong = 1761,

        /// <summary>
        /// IP地址参数不可为空
        /// </summary>
        NetworkClose_cs_IPDiZhiCanShuBuKeWeiKong = 1762,

        /// <summary>
        /// 旺店通ERP
        /// </summary>
        GuiOperator_cs_WangDianTongERP = 1763,

        /// <summary>
        /// 旺店通ERP旗舰版
        /// </summary>
        GuiOperator_cs_WangDianTongERPQiJianBan = 1764,

        /// <summary>
        /// 生成Selector 失败
        /// </summary>
        WdtErpNodeExtension_cs_ShengChengSelector = 1765,

        /// <summary>
        /// 执行成功
        /// </summary>
        HLResult_cs_ZhiXingChengGong = 1766,

        /// <summary>
        /// 未找到指定的窗口
        /// </summary>
        HLResult_cs_WeiZhaoDaoZhiDingDeChuangKou = 1767,

        /// <summary>
        /// 未找到指定的元素
        /// </summary>
        HLResult_cs_WeiZhaoDaoZhiDingDeYuanSu = 1768,

        /// <summary>
        /// 未标记的元素
        /// </summary>
        HLResult_cs_WeiBiaoJiDeYuanSu = 1769,

        /// <summary>
        /// 参数无效
        /// </summary>
        HLResult_cs_CanShuWuXiao = 1770,

        /// <summary>
        /// 初始化失败
        /// </summary>
        HLResult_cs_ChuShiHuaShiBai = 1771,

        /// <summary>
        /// 找不到窗口 {title}
        /// </summary>
        WindowOperator_cs_ZhaoBuDaoChuangKoutit = 1772,

        /// <summary>
        /// 找不到窗口 {data.Title} ({data.WindowHandle})
        /// </summary>
        WindowOperator_cs_ZhaoBuDaoChuangKoudat = 1773,

        /// <summary>
        /// ,运行监视,实在设计器,
        /// </summary>
        ConstFields_cs_YunXingJianShiShiZaiSheJi = 1774,

        /// <summary>
        /// 仅可以对下拉框元素执行此操作
        /// </summary>
        UIANode_cs_JinKeYiDuiXiaLaKuangYuanSuZhi = 1775,

        /// <summary>
        /// 老逻辑即将移除，主要指的是使用Index标签
        /// </summary>
        WindowsSelectorItem_cs_LaoLuoJiJiJiangYiChuZhuYao = 1776,

        /// <summary>
        /// 超时时间(ms)
        /// </summary>
        ZExplorerAction_cs_ChaoShiShiJianms = 1777,

        /// <summary>
        /// 存在重名插件，请检查插件环境:{pluginInfo.Name},{pluginInfo.Version},{pluginInfo.PluginType}
        /// </summary>
        LibraryLoader_cs_CunZaiZhongMingChaJianQingJianCha = 1778,

        /// <summary>
        /// ErrorMessageConfig 信息配置异常，详情见日志
        /// </summary>
        PluginLoader_cs_ErrorMessa = 1779,

        /// <summary>
        /// 初始化——执行中
        /// </summary>
        PluginLoader_cs_ChuShiHuaZhiXingZhong = 1780,

        /// <summary>
        /// 初始化上下文环境不能为空
        /// </summary>
        ContextValidator_cs_ChuShiHuaShangXiaWenHuanJingBuNeng = 1781,

        /// <summary>
        /// 初始服务类型值不在范围内
        /// </summary>
        ContextValidator_cs_ChuShiFuWuLeiXingZhiBuZaiFan = 1782,

        /// <summary>
        /// 父进程(PID={parentPid})不存在
        /// </summary>
        ContextValidator_cs_FuJinChengPIDp = 1783,

        /// <summary>
        /// 父进程PID格式不正确
        /// </summary>
        ContextValidator_cs_FuJinChengPIDGeShiBuZheng = 1784,

        /// <summary>
        /// 插件加载器加载地址不能为Null或者为空。
        /// </summary>
        ContextValidator_cs_ChaJianJiaZaiQiJiaZaiDiZhiBu = 1785,

        /// <summary>
        /// 文件[{context.PluginLoaderPath}]不存在,或者格式不正确,当前文件夹:{Directory.GetCurrentDirectory()}
        /// </summary>
        ContextValidator_cs_WenJiancontex = 1786,

        /// <summary>
        /// 启动容器
        /// </summary>
        StartForm_Designer_cs_QiDongRongQi = 1787,

        /// <summary>
        /// [Request]请求动作{actionRequest.Action}非法！
        /// </summary>
        PluginIPCService_cs_RequestQing = 1788,

        /// <summary>
        /// Copyright © 2022 实在智能
        /// </summary>
        AssemblyInfo_cs_Copyright = 1789,

        /// <summary>
        /// 远程操作异常
        /// </summary>
        OperateCommand_cs_YuanChengCaoZuoYiChang = 1790,

        /// <summary>
        /// Copyright ©浙江实在智能科技有限公司.All rights reserved
        /// </summary>
        AssemblyInfo_cs_Copyright1 = 1791,

        /// <summary>
        /// 连接信息
        /// </summary>
        DbForm_Designer_cs_LianJieXinXi = 1792,

        /// <summary>
        /// 文件路径:
        /// </summary>
        DbForm_Designer_cs_WenJianLuJing = 1793,

        /// <summary>
        /// 密 码:
        /// </summary>
        DbForm_Designer_cs_MiMa = 1794,

        /// <summary>
        /// 用户名:
        /// </summary>
        DbForm_Designer_cs_YongHuMing = 1795,

        /// <summary>
        /// 数据库端口:
        /// </summary>
        DbForm_Designer_cs_ShuJuKuDuanKou = 1796,

        /// <summary>
        /// 数据库地址:
        /// </summary>
        DbForm_Designer_cs_ShuJuKuDiZhi = 1797,

        /// <summary>
        /// 数据库名:
        /// </summary>
        DbForm_Designer_cs_ShuJuKuMing = 1798,

        /// <summary>
        /// 数据库类型:
        /// </summary>
        DbForm_Designer_cs_ShuJuKuLeiXing = 1799,

        /// <summary>
        /// 执行结果
        /// </summary>
        DbForm_Designer_cs_ZhiXingJieGuo = 1800,

        /// <summary>
        /// 操作
        /// </summary>
        DbForm_Designer_cs_CaoZuo = 1801,

        /// <summary>
        /// 执行SQL查询
        /// </summary>
        DbForm_Designer_cs_ZhiXingSQLChaXun = 1802,

        /// <summary>
        /// 批量执行SQL语句
        /// </summary>
        DbForm_Designer_cs_PiLiangZhiXingSQLYuJu = 1803,

        /// <summary>
        /// 可选
        /// </summary>
        DbForm_Designer_cs_KeXuan = 1804,

        /// <summary>
        /// SQL语句参数:
        /// </summary>
        DbForm_Designer_cs_SQLYuJuCanShu = 1805,

        /// <summary>
        /// 执行SQL语句
        /// </summary>
        DbForm_Designer_cs_ZhiXingSQLYuJu = 1806,

        /// <summary>
        /// SQL语句:
        /// </summary>
        DbForm_Designer_cs_SQLYuJu = 1807,

        /// <summary>
        /// 数据库标识:
        /// </summary>
        DbForm_Designer_cs_ShuJuKuBiaoShi = 1808,

        /// <summary>
        /// 关闭连接
        /// </summary>
        DbForm_Designer_cs_GuanBiLianJie = 1809,

        /// <summary>
        /// 执行全SQL查询
        /// </summary>
        DbForm_Designer_cs_ZhiXingQuanSQLChaXun = 1810,

        /// <summary>
        /// 返回字典:
        /// </summary>
        DbForm_Designer_cs_FanHuiZiDian = 1811,

        /// <summary>
        /// 执行单SQL查询
        /// </summary>
        DbForm_Designer_cs_ZhiXingDanSQLChaXun = 1812,

        /// <summary>
        /// 创建数据库对象
        /// </summary>
        DbForm_Designer_cs_ChuangJianShuJuKuDuiXiang = 1813,

        /// <summary>
        /// D:\\录屏\\录屏结果\\video2rpa\\
        /// </summary>
        Form1_cs_DLuPingLuPing = 1814,

        /// <summary>
        /// 相对于屏幕左上角
        /// </summary>
        Form1_Designer_cs_XiangDuiYuPingMuZuoShangJiao = 1815,

        /// <summary>
        /// 相对于当前激活窗口
        /// </summary>
        Form1_Designer_cs_XiangDuiYuDangQianJiHuoChuangKou = 1816,

        /// <summary>
        /// D:\\录屏\\录屏结果\\video2rpa
        /// </summary>
        Form1_Designer_cs_DLuPingLuPing = 1817,

        /// <summary>
        /// 本地版USB Hub
        /// </summary>
        UsbHubForm_Designer_cs_BenDiBanUSBHub = 1818,

        /// <summary>
        /// 关闭通道
        /// </summary>
        UsbHubForm_Designer_cs_GuanBiTongDao = 1819,

        /// <summary>
        /// 打开通道
        /// </summary>
        UsbHubForm_Designer_cs_DaKaiTongDao = 1820,

        /// <summary>
        /// 通道号:
        /// </summary>
        UsbHubForm_Designer_cs_TongDaoHao = 1821,

        /// <summary>
        /// 获取Com端口
        /// </summary>
        UsbHubForm_Designer_cs_HuoQuComDuanKou = 1822,

        /// <summary>
        /// 网络版USB Hub
        /// </summary>
        UsbHubForm_Designer_cs_WangLuoBanUSBHub = 1823,

        /// <summary>
        /// 端口号:
        /// </summary>
        UsbHubForm_Designer_cs_DuanKouHao = 1824,

        /// <summary>
        /// ***智能视频转RPA浮动提示***
        /// </summary>
        Video2ProcessImp_cs_ZhiNengShiPinZhuanRP = 1825,

        /// <summary>
        /// 智能元素
        /// </summary>
        ExtractViewData_cs_ZhiNengYuanSu = 1826,

        /// <summary>
        /// 配置参数时出错：
        /// </summary>
        HttpHelper_cs_PeiZhiCanShuShiChuCuo = 1827,

        /// <summary>
        /// 仅支持成员访问表达式
        /// </summary>
        JsonHelper_cs_JinZhiChiChengYuanFangWenBiaoDaShi = 1828,

        /// <summary>
        /// 所有对这个类型的引用应该修改为对Microsoft.Extension.Logging.ILogger的引用
        /// </summary>
        Log_cs_SuoYouDuiZheGeLeiXingDeYinYong = 1829,

        /// <summary>
        /// 选项
        /// </summary>
        PluginPackage_cs_XuanXiang = 1830,

        /// <summary>
        /// 分析中...
        /// </summary>
        HighlightTag_cs_Fenxizhong = 1831,
        /// <summary>
        /// 分析中...
        /// </summary>
        IsSimilarElementExist_cs_Airepair = 1832,
        /// <summary>
        /// 分析中...
        /// </summary>
        IsSimilarElementExist_cs_Normal = 1833,
        /// <summary>
        /// 成功识别表格内元素，是否确定采集表格数据？
        /// </summary>
        DataCollectingManagerV2_cs_ChengGongShiBieBiaoGeNeiYuanSu = 1834,
        /// <summary>
        /// 运行错误：
        /// </summary>
        ExecutorTalker_cs_YunXingCuoWu = 1835,
        /// <summary>
        /// engine进程退出，任务执行失败
        /// </summary>
        ExecutorTalker_cs_JinChengTuiChuRenWuZhiXingShiBai = 1836,
        /// <summary>
        /// 谷歌表地址不正确
        /// </summary>
        G_GoogleUrlNotCorrect = 1896,

        /// <summary>
        /// 参数格式不符合要求
        /// </summary>
        G_ParamFormatNotCorrect = 1897,

        /// <summary>
        /// 图像识别失败
        /// </summary>
        Image_CheckFailed = 1898
    }
    #endregion

}
