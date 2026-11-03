using System;

namespace II.RPA.WinAutomation.CommonUtility
{

    public enum eComponentType : int
    {
        /// <summary>
        /// 
        /// </summary>
        None = 000,

        /// <summary>
        /// 元素点击
        /// </summary>
        ElementClick = 001,

        /// <summary>
        /// 输入文本框
        /// </summary>
        SetText = 002,

        /// <summary>
        /// 元素是否可点击
        /// </summary>
        ElementEnable = 003,

        /// <summary>
        /// 获取文本
        /// </summary>
        GetText = 004,

        /// <summary>
        /// 获取CV文本
        /// </summary>
        GetCVText = 005,

        /// <summary>
        /// 获取下拉框选择值
        /// </summary>
        GetElementSelect = 006,

        /// <summary>
        /// 获取勾选框状态值
        /// </summary>
        GetElementCheck = 007,

        /// <summary>
        /// 设置下拉框选择项
        /// </summary>
        SetElementSelect = 008,

        /// <summary>
        /// 设置勾选框状态
        /// </summary>
        SetElementCheck = 009,

        /// <summary>
        /// 获取区域
        /// </summary>
        GetRegion = 010,

        /// <summary>
        /// FindElementWithPicPath
        /// </summary>
        FindElementWithPicPath = 011,

        /// <summary>
        /// 元素判断
        /// </summary>
        IsElementExist = 012,

        /// <summary>
        /// 获取子元素
        /// </summary>
        GetSubElements = 013,

        /// <summary>
        /// 元素截图
        /// </summary>
        ElementShot = 014,

        /// <summary>
        /// 数据爬取
        /// </summary>
        FindRelativeElementsInfo = 015,

        /// <summary>
        /// 屏幕锁屏
        /// </summary>
        RDPLock = 016,

        /// <summary>
        /// 屏幕解锁
        /// </summary>
        RDPUnlock = 017,

        /// <summary>
        /// 执行cmd
        /// </summary>
        ExecuteCmd = 018,

        /// <summary>
        /// 获取DPI比例
        /// </summary>
        GetScaler = 019,

        /// <summary>
        /// 关闭Chrome连接
        /// </summary>
        ChromeRelease = 020,

        /// <summary>
        /// 关闭Java连接
        /// </summary>
        Release = 021,

        /// <summary>
        /// 拾取操作
        /// </summary>
        Capture = 022,

        /// <summary>
        /// 拾取鼠标坐标
        /// </summary>
        CapturePosition = 023,

        /// <summary>
        /// CV文本拾取
        /// </summary>
        CaptureByRegion = 024,

        /// <summary>
        /// 本地OCR拾取
        /// </summary>
        CaptureCV = 025,

        /// <summary>
        /// 鼠标点击OCR文本
        /// </summary>
        ExecuteCV = 026,

        /// <summary>
        /// 获取文本
        /// </summary>
        GetTextByCV = 027,

        /// <summary>
        /// 获取OCR文本
        /// </summary>
        GetOCRResult = 028,

        /// <summary>
        /// 打开等待窗口
        /// </summary>
        ShowWaiting = 029,

        /// <summary>
        /// ParseCV
        /// </summary>
        ParseCV = 030,

        /// <summary>
        /// 获取网页源码
        /// </summary>
        FindAllTree = 031,

        /// <summary>
        /// 操作截图
        /// </summary>
        OperationScreenshot = 032,

        /// <summary>
        /// 等待加载
        /// </summary>
        IsLoaded = 033,

        /// <summary>
        /// 获取tab详情
        /// </summary>
        GetTabDetail = 034,

        /// <summary>
        /// 获取Document
        /// </summary>
        GetDocument = 035,

        /// <summary>
        /// 设置滚动条位置
        /// </summary>
        Scroll = 036,

        /// <summary>
        /// 获取滚动条位置
        /// </summary>
        GetScroll = 037,

        /// <summary>
        /// 停止加载
        /// </summary>
        Stop = 038,

        /// <summary>
        /// 等待加载
        /// </summary>
        Wait = 039,

        /// <summary>
        /// 滚动界面
        /// </summary>
        ScrollIntoViewIfNeeded = 040,

        /// <summary>
        /// 设置属性
        /// </summary>
        SetAttribute = 041,

        /// <summary>
        /// 获取属性
        /// </summary>
        GetAttribute = 042,

        /// <summary>
        /// 截图
        /// </summary>
        Screenshot = 043,

        /// <summary>
        /// 元素截图
        /// </summary>
        ElementScreenshot = 044,

        /// <summary>
        /// 浏览器截图
        /// </summary>
        WebBrowserScreenshot = 045,

        /// <summary>
        /// 最小化浏览器
        /// </summary>
        Minimized = 046,

        /// <summary>
        /// 最大化浏览器
        /// </summary>
        Maximized = 047,

        /// <summary>
        /// 下载文件(点击按钮)
        /// </summary>
        Download = 048,

        /// <summary>
        /// 下载文件(指定链接)
        /// </summary>
        DownloadUrl = 049,

        /// <summary>
        /// 删除Cookie
        /// </summary>
        RemoveCookie = 050,

        /// <summary>
        /// 设置Cookie
        /// </summary>
        SetCookie = 051,

        /// <summary>
        /// 获取Cookie
        /// </summary>
        GetCookie = 052,

        /// <summary>
        /// 获取Cookies
        /// </summary>
        GetCookies = 053,

        /// <summary>
        /// 前进
        /// </summary>
        GoForward = 054,

        /// <summary>
        /// 后退
        /// </summary>
        GoBack = 055,

        /// <summary>
        /// 获取URL
        /// </summary>
        Url = 056,

        /// <summary>
        /// 获取Title
        /// </summary>
        Title = 057,

        /// <summary>
        /// 关闭浏览器
        /// </summary>
        CloseAll = 058,

        /// <summary>
        /// 关闭网页
        /// </summary>
        Close = 059,

        /// <summary>
        /// 刷新
        /// </summary>
        ReLoad = 060,

        /// <summary>
        /// 打开网页
        /// </summary>
        Create = 061,

        /// <summary>
        /// 跳转网页
        /// </summary>
        Navigate = 062,

        /// <summary>
        /// 获取网页tab对象
        /// </summary>
        Acquire = 063,

        /// <summary>
        /// 执行JS
        /// </summary>
        Executejs = 064,

        /// <summary>
        /// 鼠标移入
        /// </summary>
        MouseHover = 065,

        /// <summary>
        /// 鼠标移动
        /// </summary>
        MouseMove = 066,

        /// <summary>
        /// 鼠标拖拽
        /// </summary>
        MouseDrag = 067,

        /// <summary>
        /// 跨域执行JS
        /// </summary>
        ElementExecutejs = 068,

        /// <summary>
        /// 远程解锁
        /// </summary>
        RemoteUnlocking = 069,

        /// <summary>
        /// 远程取消
        /// </summary>
        RemoteCancel = 070,

        /// <summary>
        /// 测试连接数据库
        /// </summary>
        TestDbConnection = 071,

        /// <summary>
        /// 连接数据库
        /// </summary>
        CreateDbConnection = 072,

        /// <summary>
        /// 执行SQL语句
        /// </summary>
        DbExecuteSql = 073,

        /// <summary>
        /// 执行全SQL查询
        /// </summary>
        DbQueryAll = 074,

        /// <summary>
        /// 执行单SQL查询
        /// </summary>
        DbQuerySingle = 075,

        /// <summary>
        /// 关闭连接
        /// </summary>
        DbCloseConnection = 076,

        /// <summary>
        /// 批量执行SQL语句
        /// </summary>
        DbBatchExecuteSql = 077,

        /// <summary>
        /// 获取表格数据
        /// </summary>
        GetTableData = 078,

        /// <summary>
        /// HID驱动检测
        /// </summary>
        HIDDriveCheck = 079,

        /// <summary>
        /// 数据采集
        /// </summary>
        DataCapture = 080,

        /// <summary>
        /// 锁屏状态
        /// </summary>
        LockState = 081,

        /// <summary>
        /// 获取串口
        /// </summary>
        LocalGetComs = 082,

        /// <summary>
        /// 开启USB通道
        /// </summary>
        LocalOpen = 083,

        /// <summary>
        /// 关闭USB通道
        /// </summary>
        LocalClose = 084,

        /// <summary>
        /// 开启NetworkUSB通道
        /// </summary>
        NetworkOpen = 085,

        /// <summary>
        /// 关闭NetworkUSB通道
        /// </summary>
        NetworkClose = 086,

        /// <summary>
        /// 点击SAP单元格
        /// </summary>
        ClickSapTableCell = 087,

        /// <summary>
        /// 远程连接
        /// </summary>
        ReomteConnection = 088,

        /// <summary>
        /// 远程断开
        /// </summary>
        ReomteOff = 089,

        /// <summary>
        /// 关闭FireFox连接
        /// </summary>
        FireFoxRelease = 090,

        /// <summary>
        /// 获取相似元素
        /// </summary>
        GetElementSelectorAll = 091,

        /// <summary>
        /// 获取元素源码
        /// </summary>
        GetHtml = 092,

        /// <summary>
        /// 关闭Edge连接
        /// </summary>
        EdgeRelease = 093,

        /// <summary>
        /// 屏幕截图
        /// </summary>
        CaptureScreen = 094,

        /// <summary>
        /// 配置自定义对话框
        /// </summary>
        ConfigDialog = 095,

        /// <summary>
        /// 激活tab页
        /// </summary>
        Active = 096,

        /// <summary>
        /// 数据采集配置
        /// </summary>
        DataCollectConfiguration = 097,

        /// <summary>
        /// 数据采集
        /// </summary>
        DataCollect = 098,

        /// <summary>
        /// OCR表格解析
        /// </summary>
        OcrTableAnalyse = 099,

        /// <summary>
        /// OCR识别价格金额
        /// </summary>
        GetCVPriceAmount = 100,

        /// <summary>
        /// OCR识别键值对
        /// </summary>
        GetImageByCaptureScreenOnApp = 101,

        /// <summary>
        /// 获取输入法状态
        /// </summary>
        GetInputMethodState = 102,

        /// <summary>
        /// 设置输入法状态
        /// </summary>
        SetInputMethodState = 103,

        /// <summary>
        /// 关闭Sogou连接
        /// </summary>
        SogouRelease = 104,

        /// <summary>
        /// 浏览器截图
        /// </summary>
        CaptureScreenshot = 105,

        /// <summary>
        /// 获取窗口标题列表
        /// </summary>
        GetWindowList = 106,

        /// <summary>
        /// 获取窗口对象
        /// </summary>
        GetWindowData = 107,

        /// <summary>
        /// 激活软件窗口
        /// </summary>
        ActiveWindow = 108,

        /// <summary>
        /// 设置窗口状态
        /// </summary>
        SetWindowState = 109,

        /// <summary>
        /// 设置窗口是否显示
        /// </summary>
        SetWindowShow = 110,

        /// <summary>
        /// 设置窗口范围
        /// </summary>
        GetWindowRect = 111,

        /// <summary>
        /// 移动窗口位置
        /// </summary>
        MoveWindow = 112,

        /// <summary>
        /// 调整窗口大小
        /// </summary>
        SizeWindow = 113,

        /// <summary>
        /// 关闭软件窗口
        /// </summary>
        CloseWindow = 114,

        /// <summary>
        /// 开始监听网络请求
        /// </summary>
        StartInspectNetwork = 115,

        /// <summary>
        /// 获取网络请求
        /// </summary>
        GetInspectNetwork = 116,

        /// <summary>
        /// 结束监听网络请求
        /// </summary>
        EndInspectNetwork = 117,

        /// <summary>
        /// 批量插入数据
        /// </summary>
        BatchInsert = 118,

        /// <summary>
        /// 开始事务
        /// </summary>
        BeginTransaction = 119,

        /// <summary>
        /// 提交事务
        /// </summary>
        CommitTransaction = 120,

        /// <summary>
        /// 回滚事务
        /// </summary>
        RollbackTransaction = 121,

        /// <summary>
        /// 获取父元素
        /// </summary>
        GetParent = 122,

        /// <summary>
        /// 获取兄弟元素
        /// </summary>
        GetSiblingsElements = 123,

        /// <summary>
        /// 校验元素
        /// </summary>
        IsSimilarElementExist = 124,

        /// <summary>
        /// 执行Excel
        /// </summary>
        ExecuteExcel = 125,

        /// <summary>
        /// 释放鼠标模块的鼠标按键操作
        /// </summary>
        MouseRelease = 126,

        /// <summary>
        /// 驱动输入
        /// </summary>
        DriverInput = 127,

        /// <summary>
        /// 流程编辑
        /// </summary>
        ProcessCreator = 128,

        /// <summary>
        /// 屏幕截图
        /// </summary>
        ScreenShot = 129,

        /// <summary>
        /// 获取应用地址
        /// </summary>
        GetApplicationPath = 130,
        /// <summary>
        /// 获取页面中所有元素
        /// </summary>
        GetAllElementsInWindow = 131,
        /// <summary>
        /// 获取元素选择器
        /// </summary>
        GetElementSelector = 132,
        /// <summary>
        /// 获取元素
        /// </summary>
        GetElement = 133,

        /// <summary>
        /// 获取相似元素列表
        /// </summary>
        GetSimilarElementList = 134,
        /// <summary>
        /// 清除chatrpa极速模式产生的缓存元素
        /// </summary>
        ClearChatRpaCacheElement = 135,
        /// <summary>
        /// 数据采集配置
        /// </summary>
        DataCollectConfig = 136,
        /// <summary>
        /// 循环相似元素配置
        /// </summary>
        CyclicSimilarElementConfig = 137,

        /// <summary>
        /// 截图拾取元素
        /// </summary>
        ScreenshotCapture = 138,
        /// <summary>
        /// 获取支持的浏览器列表
        /// </summary>
        GetSupportedBrowserList = 139,
        /// <summary>
        /// 等待窗口
        /// </summary>
        WaitWindow = 140,

        /// <summary>
        /// 判断窗口存在
        /// </summary>
        WindowIsExists = 141,
        /// <summary>
        /// 获取元素信息
        /// </summary>
        GetElementInfo = 142,

        /// <summary>
        ///获取窗口列表
        /// </summary>
        GetWindowDataList = 143,
        /// <summary>
        ///切换网页
        /// </summary>
        SwitchTab = 144,
        /// <summary>
        ///释放GUI资源
        /// </summary>
        ReleaseGuiResources = 145,

        /// <summary>
        ///获取RPA＋AI精简selector
        /// </summary>
        SmartSelector = 146,
        
        /// <summary>
        /// 连接手机
        /// </summary>
        ConnectMobile = 500,

        /// <summary>
        /// 点击元素
        /// </summary>
        ClickMobileElement = 501,

        /// <summary>
        /// 点击屏幕
        /// </summary>
        ClickMobileScreen = 502,

        /// <summary>
        /// 点击按键
        /// </summary>
        PressMobileKey = 503,

        /// <summary>
        /// 填写输入框
        /// </summary>
        SetMobileText = 504,

        /// <summary>
        /// 触摸滑动
        /// </summary>
        TouchSlideMobile = 505,

        /// <summary>
        /// 获取元素信息
        /// </summary>
        GetMobileElementInfo = 506,

        /// <summary>
        /// 元素截图
        /// </summary>
        GetMobileElementScreenshot = 507,

        /// <summary>
        /// 截屏(手机)
        /// </summary>
        GetMobileScreenshot = 508,

        /// <summary>
        /// 获取剪切板内容
        /// </summary>
        GetFromMobileClipboard = 509,

        /// <summary>
        /// 设置剪切板内容
        /// </summary>
        SetMobileClipboard = 510,

        /// <summary>
        /// 获取屏幕方向
        /// </summary>
        GetMobileOrientation = 511,

        /// <summary>
        /// 设置屏幕方向
        /// </summary>
        SetMobileOrientation = 512,

        /// <summary>
        /// 获取UI树
        /// </summary>
        GetMobileUiTree = 513,

        /// <summary>
        /// 获取详细连接信息
        /// </summary>
        GetConnectDetailInfo = 514,

        /// <summary>
        /// 将文件推送到手机
        /// </summary>
        PushFileToMobile = 515,

        /// <summary>
        /// 从手机中获取文件
        /// </summary>
        GetFileFromMobile = 516,

        /// <summary>
        /// 关闭手机连接
        /// </summary>
        DisconnectMobile = 517,

        /// <summary>
        /// 手机拨号
        /// </summary>
        Dial = 518,

        /// <summary>
        /// 获取连接设备信息
        /// </summary>
        GetConnectDetail = 519,

        /// <summary>
        /// 获取属性信息
        /// </summary>
        GetElementAttribute = 520,

        /// <summary>
        /// 获取子元素信息
        /// </summary>
        GetElementChild = 521,

        /// <summary>
        /// 获取手机屏幕坐标
        /// </summary>
        GetScreenPoint = 522,

        /// <summary>
        /// 手机CV拾取
        /// </summary>
        MobileCaptureCV = 523,

        /// <summary>
        /// 手机CV点击
        /// </summary>
        MobileClickCV = 524,

        /// <summary>
        /// 手机CV设置文本
        /// </summary>
        MobileSetTextCV = 525,

        /// <summary>
        /// 手机模拟输入
        /// </summary>
        MobileAnalogInput = 526,

        /// <summary>
        /// 获取相似元素
        /// </summary>
        MobileSimilarElement = 527,

        /// <summary>
        /// 拾取相似元素
        /// </summary>
        MobileCaptureSimilarElement = 528,

        /// <summary>
        /// 元素是否存在
        /// </summary>
        MobileElementIsExist = 529,
        /// <summary>
        /// 判断是否安装手机插件及连接的手机
        /// </summary>
        GetPluginInstallAndDevicesStatus = 550,

        /// <summary>
        /// C#端提供UI
        /// </summary>
        SharpUIProivder = 530,


        /// <summary>
        /// 取消拾取
        /// </summary>
        CancelCapture = 531,
        AddCredential = 532,
        DeleteCredential = 533,
        ReadCredential = 534,

        /// <summary>
        /// OutLook
        /// </summary>
        SendMail = 535,
        CheckMail = 536,
        ReplyMail = 537,
        OperateMail = 538,
        CheckAccount = 539,
        Downloader = 540,

        /// <summary>
        /// 打开APP应用
        /// </summary>
        MobileActivateApp = 541,
        /// <summary>
        /// 关闭APP应用
        /// </summary>
        MobileCloseApp = 542,
        /// <summary>
        /// 上传文件
        /// </summary>
        FileUploader = 543,
        /// <summary>
        /// 获取元素对象（手机）
        /// </summary>
        GetMobileElement = 544,
        /// <summary>
        /// 上传文件对话框处理
        /// </summary>
        FileUploadDialog = 545,
        /// <summary>
        /// 下载对话框处理
        /// </summary>
        DownloadDialog = 546,
        /// <summary>
        /// 手机文件传输
        /// </summary>
        MobileTransferFile = 547,
        /// <summary>
        /// 手机屏幕解锁
        /// </summary>
        MobileScreenUnlock = 548,

        /// <summary>
        /// 获取折线图图表数据
        /// </summary>
        GetLineAndBarChartData = 549,
        /// <summary>
        /// 获取饼状图图表数据
        /// </summary>
        GetPieChartData = 551,

        #region 谷歌表 600以后
        [Component("拷贝粘贴谷歌表")]
        CopyPasteSheet = 600,
        [Component("选中区域")]
        SelectArea = 601,
        [Component("谷歌表重命名sheet页")]
        RenameSheetName = 602,
        [Component("谷歌表获取当前激活sheet页名称")]
        GetActivedSheetName = 603,
        [Component("谷歌表新增sheet页")]
        AddNewSheet = 604,
        [Component("谷歌表选中sheet页")]
        SelectSheet = 605,
        [Component("谷歌表删除sheet页")]
        DeleteSheet = 606,
        [Component("谷歌表获取所有sheet页名称")]
        GetAllSheetsName = 607,
        [Component("获取列上第一个可用行")]
        GetFirstCanUseRow = 608,
        [Component("读取谷歌表内容")]
        GetAllData = 609,
        [Component("写入内容到谷歌表单元格")]
        WriteToSheetCell = 610,
        [Component("写入内容至谷歌表行、列、区域")]
        WriteToSheetRegion = 611,
        [Component("新建过滤视图")]
        NewFilterView = 612,
        [Component("单元格插入超链接")]
        InsertHylink = 613,
        [Component("获取sheet页行数")]
        GetSheetRowCount = 614,
        [Component("修改sheet名")]
        RenameSheet = 615,
        [Component("写入单元格公式")]
        WriteToSheetCellFormula = 616,
        [Component("按表头读取列数据")]
        GetColumnsByTableTitle = 617,
        #endregion
    }
    internal class ComponentAttribute : Attribute
    {
        public string Name { get; set; }
        public ComponentAttribute(string name = "")
        {
            Name = name;
        }
    }
    public enum SystemTextEnum
    {
        #region 系统或者APP相关的特定元素名称文本 800起始

        /// <summary>
        /// 确认另存为 浏览器重复保存同名图形文件时 弹出的提示框名称
        /// </summary>
        Common_Chrome_ComfirmSaveAs = 800,

        /// <summary>
        /// 网页报警时 alert() 弹窗 XXXX(空格)显示
        /// </summary>
        Common_Chrome_DisplayWarning = 801,

        /// <summary>
        /// 下载可疑程序时 Chrome检测提示是否保留
        /// </summary>
        Common_Chrome_Keep = 802,

        /// <summary>
        /// 上传文件时，弹窗上的打开按钮（例如邮箱添加附件）
        /// </summary>
        Common_Chrome_Open = 803,

        /// <summary>
        /// Ctr+P 右侧打印设置页（也有可能是打印按钮） 目前chrome两者名称中英文相同
        /// </summary>
        Common_Chrome_Print = 804,

        /// <summary>
        /// 另存为弹窗名称  例如浏览器另存为图片的弹窗
        /// </summary>
        Common_Chrome_SaveAs = 805,

        /// <summary>
        /// 上传文件时，弹窗的窗体名称（例如邮箱添加附件）
        /// </summary>
        Common_Chrome_UploadPanel = 806,

        /// <summary>
        /// 确认另存为 浏览器重复保存同名图形文件时 弹出的提示框名称
        /// </summary>
        Common_FireFox_ComfirmSaveAs = 807,

        /// <summary>
        /// 确认另存为确认按钮 浏览器重复保存同名图形文件时 弹出的提示框上的确认按钮
        /// </summary>
        Common_FireFox_ComfirmSaveAsYesButton = 808,

        /// <summary>
        /// 网页报警时 alert() 弹窗 XXXX(空格)显示
        /// </summary>
        Common_FireFox_DisplayWarning = 809,

        /// <summary>
        /// 文件弹窗（上传）的文件名称输入框
        /// </summary>
        Common_FireFox_FileName = 810,

        /// <summary>
        /// 上传文件时，弹窗的窗体名称（例如邮箱添加附件）
        /// </summary>
        Common_FireFox_FileUploadPanel = 811,

        /// <summary>
        /// 下载可疑程序时 浏览器检测提示是否保留（测试目前只有chrome有）
        /// </summary>
        Common_FireFox_Keep = 812,

        /// <summary>
        /// 上传/打开文件时，弹窗的打开按钮（例如邮箱添加附件）
        /// </summary>
        Common_FireFox_Open = 813,

        /// <summary>
        /// Ctr+P 右侧打印按钮
        /// </summary>
        Common_FireFox_Print = 814,

        /// <summary>
        /// Ctr+P 右侧打印设置页
        /// </summary>
        Common_FireFox_PrintPanel = 815,

        /// <summary>
        /// 另存为弹窗的保存按钮
        /// </summary>
        Common_FireFox_Save = 816,

        /// <summary>
        /// 另存为弹窗
        /// </summary>
        Common_FireFox_SaveAs = 817,

        /// <summary>
        /// 空白页
        /// </summary>
        Common_IE_BlankPage = 818,

        /// <summary>
        /// 远程桌面连接
        /// </summary>
        Common_Sys_RemoteDestopConnection = 819,

        /// <summary>
        /// 确认另存为确认按钮 浏览器重复保存同名图形文件时 弹出的提示框上的确认按钮
        /// </summary>
        Common_Chrome_ComfirmSaveAsYesButton = 820,

        /// <summary>
        /// 文件弹窗（上传）的文件名称输入框
        /// </summary>
        Common_Chrome_FileName = 821,

        /// <summary>
        /// 另存为弹窗的保存按钮
        /// </summary>
        Common_Chrome_Save = 822,

        /// <summary>
        /// 实在浏览器
        /// </summary>
        Common_ZhangYuBrowser = 823,

        /// <summary>
        /// 企业微信
        /// </summary>
        Common_App_EntrepriseWechat = 824,

        /// <summary>
        /// 实在（文本）
        /// </summary>
        Common_ShiZai = 825,
        #endregion
    }

}
