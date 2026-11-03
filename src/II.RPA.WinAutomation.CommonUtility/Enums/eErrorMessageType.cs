namespace II.RPA.WinAutomation.CommonUtility
{

    /// <summary>
    /// 错误信息码 枚举，具体错误码的后3位: XXX。
    /// 枚举项 命名规则：Error_错误细节名。
    /// 枚举值 命名规则：3位代表错误细节，确保3位数，保持视觉风格
    /// </summary>
    #region 新版本报错枚举
    public enum eErrorMessageType : int
    {
        /// <summary>
        ///  
        /// </summary>
        Error_None = 000,

        /// <summary>
        /// 元素不匹配，请确保元素可见且支持此操作。
        /// </summary>
        Error_NotFound = 001,

        /// <summary>
        /// 点击操作执行失败，请重试。
        /// </summary>
        Error_ClickError = 002,

        /// <summary>
        /// 通信失败，请检查网络后重试。
        /// </summary>
        Error_HostError = 003,

        /// <summary>
        /// 索引越界，请检查索引参数设置是否已超过上限。若仍未解决，请联系运营人员。
        /// </summary>
        Error_IndexOverflow = 004,

        /// <summary>
        /// 数据格式错误，请修改参数格式。
        /// </summary>
        Error_DataFormat = 005,

        /// <summary>
        /// 无法输入
        /// </summary>
        Error_NotEnter = 006,

        /// <summary>
        /// 软件未安装
        /// </summary>
        Error_UNINSTALLED = 007,

        /// <summary>
        /// 未匹配到页面，请保证待匹配的页面未被关闭再重试，或联系运营人员。
        /// </summary>
        Error_PageNotFound = 008,

        /// <summary>
        /// 打开网页失败，请检查网络后重试，或联系运营人员。
        /// </summary>
        Error_CreateFail = 009,

        /// <summary>
        /// 创建浏览器失败
        /// </summary>
        Error_CreateChromeFail = 010,

        /// <summary>
        /// 操作等待超时，请增加属性中的“超时时间”后重试，或联系运营人员。
        /// </summary>
        Error_WaitTimeout = 011,

        /// <summary>
        /// 浏览器连接异常，请重启浏览器或设计器后重试，或联系运营人员。
        /// </summary>
        Error_PipeNameError = 012,

        /// <summary>
        /// 操作失败，请确保元素可见且支持此操作，或查看日志排查
        /// </summary>
        Error_OperationFail = 013,

        /// <summary>
        /// 该元素不支持此操作，请重新选择正确的元素。
        /// </summary>
        Error_ElementNotOperation = 014,

        /// <summary>
        /// 未找到下载文件，请确认下载文件存在再重试。
        /// </summary>
        Error_NotFoundDownloadFile = 015,

        /// <summary>
        /// 获取下载信息失败，请重新下载并更新【实在智能 RPA 插件】。
        /// </summary>
        Error_DownloadError = 016,

        /// <summary>
        /// 下载超时，请增加属性中的“超时时间”后重试，或联系运营人员。
        /// </summary>
        Error_DownloadTimeOut = 017,

        /// <summary>
        /// 获取子元素失败，请确保元素可见且支持此操作再重试，或联系运营人员。
        /// </summary>
        Error_GetChildElementError = 018,

        /// <summary>
        /// 设置文本失败，请确保元素可见且支持此操作再重试，或联系运营人员。
        /// </summary>
        Error_SetTextError = 019,

        /// <summary>
        /// 环境异常
        /// </summary>
        Error_AbnormalEnvironment = 020,

        /// <summary>
        /// 获取文本失败，请确保元素可见且支持此操作。
        /// </summary>
        Error_GetTextError = 021,

        /// <summary>
        /// 获取元素选择失败，请确保元素可见且支持此操作。
        /// </summary>
        Error_GetSelectError = 022,

        /// <summary>
        /// 参数配置有误，请重新配置。
        /// </summary>
        Error_ParamsError = 023,

        /// <summary>
        /// 设置元素选择失败，请确保元素可见且支持此操作。
        /// </summary>
        Error_SetSelectError = 024,

        /// <summary>
        /// 获取元素勾选失败，请确保元素可见且支持此操作。
        /// </summary>
        Error_GetElementCheckError = 025,

        /// <summary>
        /// 设置元素勾选失败，请确保元素可见且支持此操作。
        /// </summary>
        Error_SetElementCheckError = 026,

        /// <summary>
        /// 运行异常，请重启客户端再操作。若仍未解决，请联系运营人员。
        /// </summary>
        Error_ConfigCVSever = 027,

        /// <summary>
        /// 元素拾取失败，请重新拾取。
        /// </summary>
        Error_ElementCaptureError = 028,

        /// <summary>
        /// 元素查找失败，请保证待操作的界面未被关闭或处于可见状态。
        /// </summary>
        Error_NotFoundElement = 029,

        /// <summary>
        /// 元素移动到可见位置失败元素移动到可见位置失败
        /// </summary>
        Error_ScrollIntoViewIfNeededError = 030,

        /// <summary>
        /// 设置属性失败，请确保元素可见且支持此操作。
        /// </summary>
        Error_SetAttributeError = 031,

        /// <summary>
        /// 获取属性失败，请确保元素可见且支持此操作。
        /// </summary>
        Error_GetAttributeError = 032,

        /// <summary>
        /// 窗口激活失败，请确保待拾取的界面未被关闭或处于可见状态。
        /// </summary>
        Error_WindowActiveError = 033,

        /// <summary>
        /// 元素是否可点击验证失败，请确保元素可见且支持此操作。
        /// </summary>
        Error_ElementIsEnable = 034,

        /// <summary>
        /// 获取元素范围失败，请确保元素可见且支持此操作。
        /// </summary>
        Error_GetRegionError = 035,

        /// <summary>
        /// 双击元素操作失败，请确保元素可见且支持此操作。
        /// </summary>
        Error_DoubleClickError = 036,

        /// <summary>
        /// 网络请求错误，请检查网络后重试，或联系运营人员。
        /// </summary>
        Error_RequestError = 037,

        /// <summary>
        /// CV拾取失败，请重启客户端再操作。若仍未解决，请联系运营人员。
        /// </summary>
        Error_CVCaptureError = 038,

        /// <summary>
        /// 加载错误
        /// </summary>
        Error_LoadError = 039,

        /// <summary>
        /// 获取Document失败
        /// </summary>
        Error_GetDocumentError = 040,

        /// <summary>
        /// 文件已存在
        /// </summary>
        Error_FileExist = 041,

        /// <summary>
        /// 文件路径为空
        /// </summary>
        Error_PathNull = 042,

        /// <summary>
        /// 元素截图失败，请确保元素可见再重试，或联系运营人员。
        /// </summary>
        Error_ScreenshotError = 043,

        /// <summary>
        /// 设置滚动条失败，请确保元素可见且支持此操作再重试，或联系运营人员。
        /// </summary>
        Error_SetScrollError = 044,

        /// <summary>
        /// 获取滚动条失败，请确保元素可见且支持此操作再重试，或联系运营人员。
        /// </summary>
        Error_GetScrollError = 045,

        /// <summary>
        /// 页面停止错误
        /// </summary>
        Error_StopError = 046,

        /// <summary>
        /// 等待加载超时或未响应，请增加“超时时间”再重试。
        /// </summary>
        Error_WaitError = 047,

        /// <summary>
        /// 文件保存的路径错误，请检查此路径是否真实存在再重试。
        /// </summary>
        Error_SavePathError = 048,

        /// <summary>
        /// 最小化错误，请确保应用未关闭的情况下再重试，或查看日志排查
        /// </summary>
        Error_MinimizedError = 049,

        /// <summary>
        /// 删除Cookie错误，请确保元素可见且支持此操作。
        /// </summary>
        Error_RemoveCookieError = 050,

        /// <summary>
        /// 最大化错误，请确保应用未关闭再重试。
        /// </summary>
        Error_MaximizedError = 051,

        /// <summary>
        /// 设置Cookie错误，请确保界面可见且元素支持此操作再重试，或联系运营人员。
        /// </summary>
        Error_SetCookieError = 052,

        /// <summary>
        /// 获取Cookie错误，请确保界面可见且元素支持此操作再重试。
        /// </summary>
        Error_GetCookieError = 053,

        /// <summary>
        /// 网页前进错误
        /// </summary>
        Error_ForwardError = 054,

        /// <summary>
        /// 网页后退错误
        /// </summary>
        Error_BackError = 055,

        /// <summary>
        /// 获取Url错误
        /// </summary>
        Error_GetUrlError = 056,

        /// <summary>
        /// 获取Title错误
        /// </summary>
        Error_GetTitleError = 057,

        /// <summary>
        /// 关闭错误
        /// </summary>
        Error_PageCloseError = 058,

        /// <summary>
        /// 刷新错误
        /// </summary>
        Error_ReloadError = 059,

        /// <summary>
        /// 链接格式错误
        /// </summary>
        Error_UrlError = 060,

        /// <summary>
        /// 跳转失败
        /// </summary>
        Error_NavigateError = 061,

        /// <summary>
        /// JavaScript代码错误
        /// </summary>
        Error_JavaScriptCodeError = 062,

        /// <summary>
        /// 执行JavaScript代码失败，请确保环境正常且代码输入正确再重试。
        /// </summary>
        Error_ExecuteJavaScriptCodeError = 063,

        /// <summary>
        /// 移动到元素失败，请确保界面可见且元素支持此操作再重试。
        /// </summary>
        Error_MoveElementError = 064,

        /// <summary>
        /// 获取子元素失败，请确保界面可见且元素支持此操作再重试。
        /// </summary>
        Error_GetSubElementError = 065,

        /// <summary>
        /// CV解析服务异常，请重启所有RPA客户端后再重试。
        /// </summary>
        Error_CVElementServiceError = 066,

        /// <summary>
        /// CV使用数据异常，请重启所有RPA客户端后再重试。
        /// </summary>
        Error_CVElementServiceDataUseError = 067,

        /// <summary>
        /// CV调用服务异常，请确保网络通畅再重试，或联系运营人员。
        /// </summary>
        Error_CVElementMatchingServiceError = 068,

        /// <summary>
        /// 智能拾取查找失败，请确保待拾取的界面和元素未被关闭或处于可见状态。
        /// </summary>
        Error_CVElementMatchingError = 069,

        /// <summary>
        /// 智能拾取查找失败，请确保待拾取的界面和元素未被关闭或处于可见状态。
        /// </summary>
        Error_CVNotMatchingElementError = 070,

        /// <summary>
        /// 语法错误，请检查并输入正确语法后重试。
        /// </summary>
        Error_SyntaxError = 071,

        /// <summary>
        /// 元素范围不可见，请确保待拾取的界面和元素未被关闭或处于可见状态。
        /// </summary>
        Error_ElementNotVisible = 072,

        /// <summary>
        /// 鼠标移动失败，请重试，或联系运营人员。
        /// </summary>
        Error_MouseMoveError = 073,

        /// <summary>
        /// 元素判断失败，请重试，或联系运营人员。
        /// </summary>
        Error_ElementDeterminationFailed = 074,

        /// <summary>
        /// 元素校验失败，请确保待拾取的界面和元素未被关闭或处于可见状态。
        /// </summary>
        Error_ElementValidationFailed = 075,

        /// <summary>
        /// OCR离线解析服务调用异常，请检查服务地址是否可以正常调用后再重试。
        /// </summary>
        Error_OCROffline_ServiceError = 076,

        /// <summary>
        /// OCR离线解析服务未匹配到元素，请重启所有RPA客户端再重试。
        /// </summary>
        Error_OCROffline_NotMatchingElementError = 077,

        /// <summary>
        /// 测试连接数据库失败，请确保数据库可用且连接配置正确。
        /// </summary>
        Error_Database_TestDbConnectionError = 078,

        /// <summary>
        /// 连接数据库失败，请确保数据库可用且连接配置正确。
        /// </summary>
        Error_Database_CreateDbConnectionError = 079,

        /// <summary>
        /// 执行SQL语句失败，请确保数据连接对象正确，且SQL语句语法正确。
        /// </summary>
        Error_Database_ExecuteSqlError = 080,

        /// <summary>
        /// 执行全SQL查询失败，请确保数据连接对象正确，且SQL语句语法正确。
        /// </summary>
        Error_Database_QueryAllError = 081,

        /// <summary>
        /// 执行单SQL查询失败，请确保数据连接对象正确，且SQL语句语法正确。
        /// </summary>
        Error_Database_QuerySingleError = 082,

        /// <summary>
        /// 关闭连接失败，请确保数据连接对象正确。
        /// </summary>
        Error_Database_CloseConnectionError = 083,

        /// <summary>
        /// 批量执行SQL语句失败，请确保数据连接对象正确，且SQL语句语法正确。
        /// </summary>
        Error_Database_BatchExecuteSqlError = 084,

        /// <summary>
        /// 关闭网页失败，请确保待操作对象状态正常，可关闭重启后再重试。
        /// </summary>
        Error_CloseFail = 085,

        /// <summary>
        /// 关闭浏览器失败，请确保待操作对象状态正常，可关闭重启后再重试。
        /// </summary>
        Error_CloseAllFail = 086,

        /// <summary>
        /// 获取tab失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_AcquireFail = 087,

        /// <summary>
        /// 激活失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_ActiveFail = 088,

        /// <summary>
        /// 下载失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_DownloadFail = 089,

        /// <summary>
        /// 执行JS失败，请重试，或联系运营人员。
        /// </summary>
        Error_ExecutejsFail = 090,

        /// <summary>
        /// 获取cookie失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_GetCookiesFail = 091,

        /// <summary>
        /// 获取网页源码失败，请确保待操作对象状态正常，可关闭重启后再重试。
        /// </summary>
        Error_GetDocumentFail = 092,

        /// <summary>
        /// 获取滚动条位置失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_GetScrollFail = 093,

        /// <summary>
        /// 获取tab信息失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_GetTabDetailFail = 094,

        /// <summary>
        /// 获取title失败，请确保待操作对象状态正常，可关闭重启后再重试。
        /// </summary>
        Error_GetTitleFail = 095,

        /// <summary>
        /// 获取url失败，请确保待操作对象状态正常，可关闭重启后再重试。
        /// </summary>
        Error_GetUrlFail = 096,

        /// <summary>
        /// 后退失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_GoBackFail = 097,

        /// <summary>
        /// 前进失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_GoForwardFail = 098,

        /// <summary>
        /// 加载失败，请重试。
        /// </summary>
        Error_IsLoadedFail = 099,

        /// <summary>
        /// 最大化失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_MaximizedFail = 100,

        /// <summary>
        /// 最小化失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_MinimizedFail = 101,

        /// <summary>
        /// 跳转失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_NavigateFail = 102,

        /// <summary>
        /// 刷新失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_ReLoadFail = 103,

        /// <summary>
        /// 删除cookie失败，请重试。
        /// </summary>
        Error_RemoveCookieFail = 104,

        /// <summary>
        /// 浏览器截图失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_ScreenshotFail = 105,

        /// <summary>
        /// 设置滚动位置失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_ScrollFail = 106,

        /// <summary>
        /// 设置cookie失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_SetCookieFail = 107,

        /// <summary>
        /// 停止加载失败，请重试。
        /// </summary>
        Error_StopFail = 108,

        /// <summary>
        /// 等待加载失败，请重试。
        /// </summary>
        Error_WaitFail = 109,

        /// <summary>
        /// 获取表格数据失败，请重试，或联系运营人员。
        /// </summary>
        Error_GetTableDataError = 110,

        /// <summary>
        /// 获取HID驱动检测失败，请重试，或联系运营人员。
        /// </summary>
        Error_HIDDriveCheck = 111,

        /// <summary>
        /// 拾取鼠标坐标失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_CapturePositionError = 112,

        /// <summary>
        /// 鼠标拖拽失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_MouseDragError = 113,

        /// <summary>
        /// 数据采集失败，请确保待操作对象状态正常再重试，或联系运营人员。
        /// </summary>
        Error_DataCaptureError = 114,

        /// <summary>
        /// 获取本地USB串口信息异常，请重试，或联系运营人员。
        /// </summary>
        Error_LocalGetComsError = 115,

        /// <summary>
        /// 开启本地USB通道异常，请重试，或联系运营人员。
        /// </summary>
        Error_LocalOpenError = 116,

        /// <summary>
        /// 关闭本地USB通道异常，请重试，或联系运营人员。
        /// </summary>
        Error_LocalCloseError = 117,

        /// <summary>
        /// 设置内容格式不正确，请输入正确的数组格式。
        /// </summary>
        Error_NotArrayError = 118,

        /// <summary>
        /// 创建实在浏览器失败，请重试，或联系运营人员。
        /// </summary>
        Error_CreateZExplorer = 119,

        /// <summary>
        /// 未找到与CV元素匹配的应用程序，请确保待操作的界面和元素未被关闭或处于可见状态再重试，或联系运营人员。
        /// </summary>
        Error_CVAppNotFoundError = 120,

        /// <summary>
        /// 未选择对比元素，无法获取相似元素。
        /// </summary>
        Error_ElementNotType_Error = 121,

        /// <summary>
        /// 暂不支持所选元素类型，无法获取相似元素。
        /// </summary>
        Error_ElementNonsupport_Error = 122,

        /// <summary>
        /// 指定元素与所选元素属不同页面，无法获取相似元素
        /// </summary>
        Error_ElementPlacement_Error = 123,

        /// <summary>
        /// 指定元素与所选元素属不同层级，无法获取相似元素
        /// </summary>
        Error_ElementLevel_Error = 124,

        /// <summary>
        /// 指定元素与所选元素属不同节点，无法获取相似元素
        /// </summary>
        Error_ElementNode_Error = 125,

        /// <summary>
        /// 行号错误，格式应该为：“1”或“1-10”
        /// </summary>
        Error_TableRowNO = 126,

        /// <summary>
        /// 行数超出表格限制。
        /// </summary>
        Error_TableRowExceed = 127,

        /// <summary>
        /// 起始行号不能大于截止行号
        /// </summary>
        Error_TableStartRowExceed = 128,

        /// <summary>
        /// 列号错误，格式应该为：“1”或“1-10”
        /// </summary>
        Error_TableColumnNo = 129,

        /// <summary>
        /// 列数超出表格限制。
        /// </summary>
        Error_TableColumnExceed = 130,

        /// <summary>
        /// 起始列号不能大于截止列号
        /// </summary>
        Error_TableStartColumnExceed = 131,

        /// <summary>
        /// 列号必须为正整数。
        /// </summary>
        Error_TableColumnNonzero = 132,

        /// <summary>
        /// 指定元素与所选元素属不同类型，无法获取相似元素。
        /// </summary>
        Error_ElementType_Error = 133,

        /// <summary>
        /// 格式错误，请检查并输入正确的格式。
        /// </summary>
        Error_Format_Error = 134,

        /// <summary>
        /// 起始行号或列号必须为正整数。
        /// </summary>
        Error_StartNO_Error = 135,

        /// <summary>
        /// 截止行号或列号必须为正整数。
        /// </summary>
        Error_EndNO_Error = 136,

        /// <summary>
        /// 屏幕截图操作失败，请重试。
        /// </summary>
        Error_CaptureScreen_Error = 137,

        /// <summary>
        /// Excel被占用或操作权限不足，请关闭Excel程序，再以管理员权限打开重试。
        /// </summary>
        Error_Excel_ExecuteError = 138,

        /// <summary>
        /// 未获取到CV元素匹配的对应数据，请确保流程操作规范或重新拾取！
        /// </summary>
        Error_CVNoFoundStaticeDatatError = 139,

        /// <summary>
        /// 图片路径错误，请选择正确路径再重试。
        /// </summary>
        Error_PictureNotFound = 140,

        /// <summary>
        /// 图片读取失败，请确保图片文件存在再重试。
        /// </summary>
        Error_PictureOpenWrong = 141,

        /// <summary>
        /// 不支持的图片格式（目前仅支持png，jpg，jpeg）。
        /// </summary>
        Error_ImageFormatNotSupported = 142,

        /// <summary>
        /// 获取表格失败，请确保表格存在且清晰再重试。
        /// </summary>
        Error_GetNoTable = 143,

        /// <summary>
        /// 获取表格数据为空，请确保表格存在且清晰再重试。
        /// </summary>
        Error_GetTableDataEmpty = 144,

        /// <summary>
        /// 获取表格信息失败，请确保表格存在且清晰再重试。
        /// </summary>
        Error_GetTableDataFail = 145,

        /// <summary>
        /// 获取表格数据为空，请确保表格存在且清晰再重试。
        /// </summary>
        Error_GetTableDataEmptyExcute = 146,

        /// <summary>
        /// 获取表格信息失败，请确保表格存在且清晰再重试。
        /// </summary>
        Error_GetTableDataFailExcute = 147,

        /// <summary>
        /// 屏幕截图查找失败，请确保截图窗口未被关闭，同时窗口元素处于可见状态且窗口位置不变，再操作重试。若仍未解决，请联系运营人员。
        /// </summary>
        Error_GetCVText_Error = 148,

        /// <summary>
        /// 屏幕截图查找失败，请确保截图窗口未被关闭，同时窗口元素处于可见状态且窗口位置不变，再操作重试。若仍未解决，请联系运营人员。
        /// </summary>
        Error_GetCVPriceAmount_Error = 149,

        /// <summary>
        /// 连接设备失败，请检查：1.Appium服务器是否已启动；2.Appium服务器地址是否配置正确；3.移动设备是否连接；4.系统或者系统版本与连接的手机无法匹配
        /// </summary>
        Error_MobileConnect = 150,

        /// <summary>
        /// 关闭手机连接失败，请重试。
        /// </summary>
        Error_MobileDisconnect = 151,

        /// <summary>
        /// 获取截图失败，请检查：1.保存图片的路径不存在或无管理员权限；2.移动设备是否连接正常
        /// </summary>
        Error_GetScreenshot = 152,

        /// <summary>
        /// 元素查找失败，请检查：1.页面元素是否已变动或包含该页面元素的页面是否处于当前页；2.移动设备是否连接正常
        /// </summary>
        Error_MobileElementNoFound = 153,

        /// <summary>
        /// 点击按键失败，请检查后重试。
        /// </summary>
        Error_PressKey = 154,

        /// <summary>
        /// 触摸滑动失败，请检查坐标所在区域是否支持滑动操作。
        /// </summary>
        Error_TouchSlid = 155,

        /// <summary>
        /// 读取屏幕方向失败，请检查后重试。
        /// </summary>
        Error_GetOrientation = 156,

        /// <summary>
        /// 设置屏幕方向失败，请检查后重试。
        /// </summary>
        Error_SetOrientation = 157,

        /// <summary>
        /// 获取剪切板内容失败，请检查后重试。
        /// </summary>
        Error_GetClipboard = 158,

        /// <summary>
        /// 设置剪切板内容失败，请检查后重试。
        /// </summary>
        Error_SetClipboard = 159,

        /// <summary>
        /// 获取UI树失败，请检查后重试。
        /// </summary>
        Error_GetUiTree = 160,

        /// <summary>
        /// 文件推送失败，请检查后重试。
        /// </summary>
        Error_PushFile = 161,

        /// <summary>
        /// 从手机获取文件失败，请检查后重试。
        /// </summary>
        Error_GetFileFromMobile = 162,

        /// <summary>
        /// 文件未找到，请检查后重试。
        /// </summary>
        Error_FileNotFound = 163,

        /// <summary>
        /// 拨号失败，请检查后重试。
        /// </summary>
        Error_Dial = 164,

        /// <summary>
        /// 获取设备信息失败，请检查后重试。
        /// </summary>
        Error_GetConnectDetail = 165,

        /// <summary>
        /// 获取属性值失败，请检查后重试。
        /// </summary>
        Error_GetAttribute = 166,

        /// <summary>
        /// 获取子元素失败，请检查后重试。
        /// </summary>
        Error_GetElementChild = 167,

        /// <summary>
        /// 获取屏幕坐标失败，请检查后重试。
        /// </summary>
        Error_GetScreenPoint = 168,

        /// <summary>
        /// 元素校验失败，请检查后重试。
        /// </summary>
        Error_CheckSelector = 169,

        /// <summary>
        /// 获取相似元素失败，请检查后重试。
        /// </summary>
        Error_GetSimilarElementList = 170,

        /// <summary>
        /// 拾取相似元素失败，请检查后重试。
        /// </summary>
        Error_GetSimilarElementInfo = 171,
        /// <summary>
        /// 获取元素对象失败，请检查后重试。
        /// </summary>
        Error_GetMobileElement = 184,


        #region 新增手机异常提醒部分从【550】开始
        /// <summary>
        /// 判断是否安装手机插件及连接的手机失败
        /// </summary>
        Error_GetPluginInstallAndDevicesStatus = 550,
        /// <summary>
        /// 手机插件未安装
        /// </summary>
        Error_NotInstallMobilePlugin = 551,

        /// <summary>
        /// 打开APP应用失败，请检查包名是否正确或移动设备是否连接正常。
        /// </summary>
        Error_ActivateApp = 552,
        /// <summary>
        /// 关闭APP应用失败，请检查包名是否正确或移动设备是否连接正常。
        /// </summary>
        Error_CloseApp = 553,
        /// <summary>
        /// 手机连接服务异常，请检查后重试。
        /// </summary>
        Error_ConnectFail = 554,
        /// <summary>
        /// 手机传输文件异常
        /// </summary>
        Error_MobileTransferFile = 555,
        /// <summary>
        /// 手机屏幕解锁异常
        /// </summary>
        Error_MobileScreenUnlock = 556,
        /// <summary>
        /// 路径不存在，请检查后重试（勾选自动创建后不报错）
        /// </summary>
        Error_MobileFilePathNotExists = 557,
        /// <summary>
        /// 文件无操作权限，请检查操作系统权限后重试
        /// </summary>
        Error_MobileFileAccessDenied = 558,
        /// <summary>
        /// 文件传输超时，请调整组件超时时间后重试
        /// </summary>
        Error_MobileFileTransferTimeout = 559,
        /// <summary>
        /// 存在相同文件，请检查后重试（勾选自动覆盖后不报错）
        /// </summary>
        Error_MobileFileAlreadyExists = 560,
        /// <summary>
        /// 文件或路径格式错误，请检查后重试
        /// </summary>
        Error_MobileFilePathInvalid = 561,

        /// <summary>
        /// 未检测到页面信息，请检查后重试
        /// </summary>
        Error_MobilePageNotInvalid = 562,
        /// <summary>
        /// 未找到元素图像，请检查后重试
        /// </summary>
        Error_MobileElementImageNotInvalid = 563,
        #endregion

        /// <summary>
        /// 检测到点击动作未生效，请检查目标元素是否可以正常点击。（如运行正常，可在高级属性中关闭校验）
        /// </summary>
        Error_ActionCheckFail = 172,

        /// <summary>
        /// 暂未选择对比元素，无法修复元素
        /// </summary>
        Error_ElementRenovateNotType_Error = 173,

        /// <summary>
        /// 暂不支持所选元素类型，无法修复元素
        /// </summary>
        Error_ElementRenovateNonsupport_Error = 174,

        /// <summary>
        /// 指定元素与所选元素属不同窗口，无法修复元素
        /// </summary>
        Error_ElementRenovatePlacement_Error = 175,

        /// <summary>
        /// 指定元素与所选元素属不同层级，无法修复元素
        /// </summary>
        Error_ElementRenovateLevel_Error = 176,

        /// <summary>
        /// 指定元素与所选元素属不同节点，无法修复元素
        /// </summary>
        Error_ElementRenovateNode_Error = 177,
        /// <summary>
        /// 获取页面中所有元素。
        /// </summary>
        Error_GetAllElementsInWindowError = 178,
        /// <summary>
        /// 当前页面未匹配到目标，请重试。
        /// </summary>
        Error_GetElementSelectorError = 179,
        /// <summary>
        /// 获取应用地址。
        /// </summary>
        Error_GetApplicationPathError = 180,
        /// <summary>
        /// 获取网页元素异常
        /// </summary>
        Error_GetElementError = 181,

        /// <summary>
        /// IE或IE内核浏览器不支持XPath查找元素
        /// </summary>
        Error_NotSupertXpath = 182,

        /// <summary>
        /// 元素截图区域大于可视区域，建议使用浏览器长截图
        /// </summary>
        Error_IntoViewAreaNotContainElementArea = 183,

        /// <summary>
        /// 【按选项值】设置方式仅支持Web下拉框，不支持Windows软件下拉框，建议使用【按选项内容】或【按选项顺序】设置下拉菜单。
        /// </summary>
        Error_SetElementSelectWinNotSupportValue = 300,

        /// <summary>
        /// 元素编辑发生错误，请重启程序再重试。
        /// </summary>
        Error_DataCollection_DoElementEditor = 301,

        /// <summary>
        /// 变量编译过程发生异常，如使用局部变量或特殊字符，请调整后重试。
        /// </summary>
        Error_DataCollection_DoStringCompile = 302,

        /// <summary>
        /// 不支持的数据类型，请重新采集。
        /// </summary>
        Error_DataCollection_GUINoSupport = 303,

        /// <summary>
        /// 数据采集执行异常，请重启客户端重试，或联系运营人员。
        /// </summary>
        Error_DataCollection = 304,

        /// <summary>
        /// 拾取数据与已有数据非同类数据，无法拾取。
        /// </summary>
        Error_DataCollection_SimileCaptureDifferentElementTypes = 305,

        /// <summary>
        /// 已采集表格数据，无法继续采集其他类型的数据！如需重新采集，请点击【清空】按钮。
        /// </summary>
        Error_DataCollection_CapturedTableElement = 306,

        /// <summary>
        /// 未找到下一页按钮，若采集未完成，请编辑下一页按钮；若采集已正常完成，则忽略此信息。
        /// </summary>
        Error_DataCollection_NextBtnNotFound = 307,

        /// <summary>
        /// 应用截图查找失败，请确保截图窗口未被关闭，同时窗口元素处于可见状态，或查看日志排查
        /// </summary>
        Error_GetImageByCaptureScreenOnApp_Error = 308,

        /// <summary>
        /// 选择采集类型与采集目标元素类型不符，无法采集此类元素数据。
        /// </summary>
        Error_DataCollection_ElementTypeError = 309,

        /// <summary>
        /// 选择采集类型为非表格元素，无法采集此类元素数据。
        /// </summary>
        Error_DataCollection_ElementNotTableError = 310,

        /// <summary>
        /// 无法跨页面采集数据，返回上个页面继续采集或【清空】后重新采集。
        /// </summary>
        Error_DataCollection_DifferentPages = 311,

        /// <summary>
        /// 搜狗自动化服务未启动
        /// </summary>
        Error_SogouServiceNotFound = 312,

        /// <summary>
        /// 未检测到搜狗浏览器插件已安装
        /// </summary>
        Error_SogouExtensionUnInstalled = 313,

        /// <summary>
        /// 搜狗浏览器插件链接超时，请确认是否安装并启用，或增加超时时间
        /// </summary>
        Error_SogouExtensionUnopen = 314,

        /// <summary>
        /// 打开Sogou浏览器失败
        /// </summary>
        Error_SogouOpenFail = 315,

        /// <summary>
        /// 搜狗浏览器未安装
        /// </summary>
        Error_SogouNotInstall = 316,
        Error_SogouBrowserNotRun = 317,

        /// <summary>
        /// 缺少浏览器权限，请重新安装对应浏览器插件
        /// </summary>
        Error_ExtensionpPermissionError = 322,

        /// <summary>
        /// 仅支持Chrome浏览器截图
        /// </summary>
        Error_OnlyImplementedOnChrome = 323,

        /// <summary>
        /// 获取桌面窗口标题失败
        /// </summary>
        Error_GetWindowList = 324,

        /// <summary>
        /// 激活桌面窗口失败
        /// </summary>
        Error_ActiveWindow = 325,

        /// <summary>
        /// 未找到目标窗口，请确保待操作的窗口未被关闭
        /// </summary>
        Error_GetWindowData = 326,

        /// <summary>
        /// 设置桌面窗口状态失败
        /// </summary>
        Error_SetWindowState = 327,

        /// <summary>
        /// 设置桌面窗口是否显示失败
        /// </summary>
        Error_SetWindowShow = 328,

        /// <summary>
        /// 获取桌面窗口范围失败
        /// </summary>
        Error_GetWindowRect = 329,

        /// <summary>
        /// 移动桌面窗口失败
        /// </summary>
        Error_MoveWindow = 330,

        /// <summary>
        /// 设置桌面窗口区域失败
        /// </summary>
        Error_SizeWindow = 331,

        /// <summary>
        /// 关闭桌面窗口失败
        /// </summary>
        Error_CloseWindow = 332,

        /// <summary>
        /// 窗口最小化无法移动窗口
        /// </summary>
        Error_MinimizeWindowNotMove = 333,

        /// <summary>
        /// 窗口最小化无法修改窗口尺寸
        /// </summary>
        Error_MinimizeWindowNotSize = 334,

        /// <summary>
        /// 窗口最小化无法激活窗口
        /// </summary>
        Error_MinimizeWindowNotActive = 335,

        /// <summary>
        /// 窗口不存在
        /// </summary>
        Error_WindowNotFind = 336,


        /// <summary>
        /// 开始监听网络请求失败
        /// </summary>
        Error_StartInspectNewwork = 337,

        /// <summary>
        /// 获取网络请求失败
        /// </summary>
        Error_GetInspectNewwork = 338,

        /// <summary>
        /// 结束监听网络请求失败
        /// </summary>
        Error_EndInspectNewwork = 339,

        /// <summary>
        /// 获取结果失败，目标网页对象未被监听，请对该网页对象运行 “启动网页监听” 组件后重试
        /// </summary>
        Error_Inspect_Not_Ready = 340,

        /// <summary>
        /// 开始事务发生异常，请检查数据库连接。
        /// </summary>
        Error_DataBase_BeginTransaction_Error = 341,

        /// <summary>
        /// 开始事务发生异常，数据库对象已存在执行中事务。
        /// </summary>
        Error_DataBase_Transaction_ExistsError = 342,

        /// <summary>
        /// 提交事务发生异常，请检查待提交事务是否正确。
        /// </summary>
        Error_DataBase_Transaction_NotExistsError = 343,

        /// <summary>
        /// 插入数据发生异常，部分未插入成功，已回滚数据。
        /// </summary>
        Error_DataBase_BatchInsert_NotAllSuccessError = 344,

        /// <summary>
        /// 插入数据发生异常。
        /// </summary>
        Error_DataBase_BatchInsert_Error = 345,

        /// <summary>
        /// 获取父元素失败，请确保元素可见且支持此操作再重试，或联系运营人员。
        /// </summary>
        Error_GetParent_Error = 346,

        /// <summary>
        /// 获取兄弟元素失败，请确保元素可见且支持此操作再重试，或联系运营人员。
        /// </summary>
        Error_GetSiblingsElements_Error = 347,

        /// <summary>
        /// 获取第几个元素输入值错误，请输入大于0的整数。
        /// </summary>
        Error_GetSubElements_IndexValFail_Error = 348,

        /// <summary>
        /// SQL语句语法错误，请参考示例：INSERT INTO table_name(column1,column2)
        /// </summary>
        Error_DataBase_BatchInsert_SqlFormat_Error = 349,

        /// <summary>
        /// 批量插入数据为空，请检查数据是否正确。
        /// </summary>
        Error_DataBase_BatchInsert_InvalidData_Error = 350,

        /// <summary>
        /// 批量插入数据与SQL语句列数不一致，请检查数据是否正确。
        /// </summary>
        Error_DataBase_BatchInsert_ColumnsNotMatch_Error = 351,

        /// <summary>
        /// 获取层级输入值错误，请输入大于0的整数。
        /// </summary>
        Error_GetParentElements_LevelValFail_Error = 352,

        /// <summary>
        /// 不符合高级选择器语法，请查看使用帮助。
        /// </summary>
        Error_FilterFormatFail_Error = 353,

        /// <summary>
        /// 查找360安全浏览器启动路径失败，请确保软件正确安装
        /// </summary>
        Error_360CantFind_Error = 354,

        /// <summary>
        /// 网页加载超时，请检查网络是否正常。可在组件高级属性中配置加载或超时规则。
        /// </summary>
        Error_Webbrowser_Create_LoadingTimeout_Error = 355,

        /// <summary>
        /// 等待网页加载失败，请检查网页状态。
        /// </summary>
        Error_Webbrowser_Create_NoNewTab_Error = 356,

        /// <summary>
        /// 元素截图失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_ElementShot_OperationTomeout_Error = 357,

        /// <summary>
        /// 操作元素失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_ScrollIntoViewIfNeeded_OperationTomeout_Error = 358,

        /// <summary>
        /// 获取元素坐标失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_GetRegion_OperationTomeout_Error = 359,

        /// <summary>
        /// 设置属性失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_SetAttribute_OperationTomeout_Error = 360,

        /// <summary>
        /// 获取属性失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_GetAttribute_OperationTomeout_Error = 361,

        /// <summary>
        /// 获取元素失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_GetSubElements_OperationTomeout_Error = 362,

        /// <summary>
        /// 操作元素失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_ElementEnable_OperationTomeout_Error = 363,

        /// <summary>
        /// 操作元素失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_MouseHover_OperationTomeout_Error = 364,

        /// <summary>
        /// 获取SAP表格元素失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_GetTableData_OperationTomeout_Error = 365,

        /// <summary>
        /// 双击SAP表格元素失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_ClickSapTableCell_OperationTomeout_Error = 366,

        /// <summary>
        /// 操作下拉框执行失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_OperationSelect_OperationTomeout_Error = 367,

        /// <summary>
        /// 操作勾选框执行失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_OperationCheck_OperationTomeout_Error = 368,

        /// <summary>
        /// 操作失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_ElementClick_OperationTomeout_Error = 369,

        /// <summary>
        /// 获取文本失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_GetText_OperationTomeout_Error = 370,

        /// <summary>
        /// 输入文本失败，请增加属性中的时间值，或修改文本内容。
        /// </summary>
        Error_SetText_OperationTomeout_Error = 371,

        /// <summary>
        /// 获取元素失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_GetParent_OperationTomeout_Error = 372,

        /// <summary>
        /// 获取元素失败，请增加属性中的时间值，或重新拾取元素。
        /// </summary>
        Error_GetSiblingsElements_OperationTomeout_Error = 373,

        /// <summary>
        /// QT 拾取初始化失败
        /// </summary>
        Error_QtInit = 378,
        /// <summary>
        /// QT 拾取服务链接异常
        /// </summary>
        Error_QtConnect = 379,

        /// <summary>
        /// 暂不支持此操作
        /// </summary>
        Error_NotSupport = 380,

        /// <summary>
        /// 浏览器用户目录被占用,请关闭浏览器进程后重试。
        /// </summary>
        Error_WebBrowser_Create_Occupation = 381,



        /// <summary>
        /// 操作超时
        /// </summary>
        Error_PluginRun_TimeOut = 401,

        /// <summary>
        /// 设置的超时时间过短，请重新调整超时时间
        /// </summary>
        Error_PluginRun_TimeOutTooShort = 402,

        /// <summary>
        /// 操作失败，已超时，请增加属性中的时间值。
        /// </summary>
        Error_PluginRun_Element_TimeOut = 403,

        /// <summary>
        /// 请求失败，请重试。若仍未解决，请联系运营人员。
        /// </summary>
        Error_PluginRun_ServerStatusInvalid = 404,

        /// <summary>
        /// 正在初始化、请稍后重试
        /// </summary>
        Error_PluginRun_ServerStatusUninit = 405,

        /// <summary>
        /// 360极速未运行
        /// </summary>
        Error_360X_NotRun = 406,
        /// <summary>
        /// 请确认所有要采集的列元素位于同一iframe内。
        /// </summary>
        Error_DataCollectionV2_DiffIframe = 407,
        /// <summary>
        /// 无法跨页面采集，返回原页面【继续采集】或清空并重新采集
        /// </summary>
        Error_DataCollectionV2_DifferentPages = 408,
        /// <summary>
        /// 不是原采集页面
        /// </summary>
        Error_DataCollectionV2_NotCollectionPage = 409,



        #region OutLook组件

        /// <summary>
        /// 发送邮件异常
        /// </summary>
        Error_SendMail = 410,
        /// <summary>
        /// 回复邮件异常
        /// </summary>
        Error_ReplyMail = 411,
        /// <summary>
        /// 操作邮件异常
        /// </summary>
        Error_OperateMail = 412,
        /// <summary>
        /// 查找邮件异常
        /// </summary>
        Error_CheckMail = 413,
        /// <summary>
        /// 未查找到符合条件邮件
        /// </summary>
        Error_NotFoundMail = 414,
        /// <summary>
        /// 未查找到符合条件文件夹
        /// </summary>
        Error_NotFoundMailFolder = 415,
        /// <summary>
        /// 邮件未找到符合条件的附件
        /// </summary>
        Error_NotFoundMailAttachment = 416,
        /// <summary>
        /// 未查找到符合条件的目标文件夹
        /// </summary>
        Error_NotFoundMailDestinationFolder = 417,
        /// <summary>
        /// 未查找到匹配的邮箱账户
        /// </summary>
        Error_NotFoundMailAccount = 418,
        /// <summary>
        /// 附件不存在
        /// </summary>
        Error_NotExistMailAttachment = 419,
        #endregion

        /// <summary>
        ///  Internet Explorer模式下的选项卡不支持此功能。
        /// </summary>
        Error_NotSupportIEMode = 430,


        #region [Download]
        /// <summary>
        /// 等待询问下载弹窗超时
        /// </summary>
        Error_WaittingSaveDialog_Timeout_Error = 431,

        /// <summary>
        /// 下载失败，请设置浏览器关闭下载弹窗
        /// </summary>
        Error_NotSupportDownloadMode_Error = 432,

        /// <summary>
        /// 存储路径不存在，且无权限自动创建
        /// </summary>
        Error_PathNotOperationPermission_Error = 433,

        /// <summary>
        /// 创建文件失败
        /// </summary>
        Error_CreateDirectory_Error = 434,

        /// <summary>
        /// 文件夹名称语法不正确，不能包含\/:*?”<>|等符号
        /// </summary>
        Error_FileNameNotSupport_Error = 435,

        /// <summary>
        /// 无法获取浏览器是否'询问下载'设置
        /// </summary>
        Error_UnavailableDownloadWindow = 436,

        /// <summary>
        /// 文件已存在，并且无法被覆盖，请检查是否被占用
        /// </summary>
        Error_FileOccupancy = 437,

        /// <summary>
        /// 下载中断，检测到浏览器被关闭
        /// </summary>
        Error_BrowserQuit_Download = 438,

        /// <summary>
        /// 文件存储失败，可能原因外部干扰导致系统中断
        /// </summary>
        Error_FileOperateFail = 439,

        /// <summary>
        /// 存储路径无访问权限
        /// </summary>
        Error_PathNoAccess_Error = 440,

        /// <summary>
        /// 下载异常，检查浏览器是否设置了第三方下载工具
        /// </summary>
        Error_OtherSoftwareEffects = 441,

        /// <summary>
        /// 未获取到下载数据，无法等待下载完成
        /// </summary>
        Error_NotDownloadData = 442,


        #endregion

        #region 窗口补充
        /// <summary>
        /// 等待窗口失败
        /// </summary>
        Error_WaitWindow = 443,

        /// <summary>
        /// 判断窗口存在失败
        /// </summary>
        Error_WindowIsExists = 444,
        #endregion

        /// <summary>
        /// 本地磁盘内存不足
        /// </summary>
        Error_OutOfMemory = 445,

        /// <summary>
        /// 无法获取浏览器默认下载路径设置
        /// </summary>
        Error_UnavailableDownloadDefaultDirectory = 446,

        /// <summary>
        /// 无法获取浏览器默认文件名称，请自定义文件名
        /// </summary>
        Error_UnavailableDownloadDefaultFileName = 447,

        /// <summary>
        /// 查找IE下载浮层元素失败
        /// </summary>
        Error_NotFindIEBar = 448,

        /// <summary>
        ///  内嵌IE模式
        /// </summary>
        Error_EmbeddedIEMode = 449,

        /// <summary>
        /// 定位方式CSS选择器或XPath选择器时，父元素仅支持网页元素
        /// </summary>
        Error_NotSupportParentElement = 450,
        /// <summary>
        /// 父元素下未找到该相似元素
        /// </summary>
        Error_NotFoundElementInParent = 451,
        /// <summary>
        /// 目标元素与父元素类型不一致
        /// </summary>
        Error_NotParentElementType = 452,
        /// <summary>
        /// 配置chatrpa数据采集异常
        /// </summary>
        Error_ChatDataCollectConfig = 453,
        /// <summary>
        /// 配置chatrpa循环相似元素异常
        /// </summary>
        Error_ChatCyclicSimilarElementConfig = 454,
        /// <summary>
        /// 获取元素信息功能异常
        /// </summary>
        Error_GetElementInfoError = 455,
        /// <summary>
        /// 暂不支持获取所选元素的指定信息
        /// </summary>
        Error_GetElementInfo_NotImplemented_Error = 456,

        /// <summary>
        /// 切换网页失败，请确保待网页对象状态正常再重试。
        /// </summary>
        Error_SwitchTabFail = 457,

        /// <summary>
        /// 智能拾取查找失败
        /// </summary>
        Error_CVNotMatchingElementCommonError = 458,

        /// <summary>
        /// 上传文件失败，请确保待操作对象状态正常再重试。
        /// </summary>
        Error_FileUploadFail = 459,

        /// <summary>
        /// 文件夹不存在或路径无效
        /// </summary>
        Error_DirectoryNotSupportOrExist_Error = 460,
        /// <summary>
        /// 文件不存在或路径无效
        /// </summary>
        Error_FilePathNotSupportOrExist_Error = 461,
        /// <summary>
        /// 未匹配到浏览器窗口，请保证待匹配的浏览器窗口未被关闭再重试
        /// </summary>
        Error_BrowserWindowTypeCheck_Error = 462,
        /// <summary>
        /// 等待询问上传弹窗超时
        /// </summary>
        Error_WaittingUploadSaveDialog_Timeout_Error = 463,
        /// <summary>
        /// 检测到当前浏览器插件版本较低，请前往设置中心升级
        /// </summary>
        Error_BrowserPluginVersionTooLow = 464,

        /// <summary>
        /// 选项值未找到，请确认选项值存在且配置无误。
        /// </summary>
        Error_SelectValueIsNotValid = 465,
        Error_IEAsyncExecutejsError = 466,

        /// <summary>
        /// 元素类型不支持操作
        /// </summary>
        Error_NotMatchNodeType_Error = 467,

        /// <summary>
        /// 图片识别失败
        /// </summary>
        Error_ImageCheckFailed = 468,

        /// <summary>
        /// 文件(夹)名称或路径过长
        /// </summary>
        Error_PathCharTooLong_Error = 469,
        /// <summary>
        /// 批量文件(夹)路径不属于同一个工作目录
        /// </summary>
        Error_DiffWorkDir_Error = 470,

        /// <summary>
        ///  AI+精简selector 构建失败
        /// </summary>
        Error_SmartSelector_BuildFail = 471,
        /// <summary>
        /// AI+精简selector 资源点不足
        /// </summary>
        Error_SmartSelector_Insufficient_Resource_Points = 472,
        /// <summary>
        /// 当前元素在新版本客户端获取，请升级客户端版本后重试
        /// </summary>
        Error_SelectorVersion_NotSupported = 474,
        /// <summary>
        /// 未匹配到目标图像或元素，请检查后重试
        /// </summary>
        Error_NotMatch_Image_Or_Element = 473,

        /// <summary>
        /// 360se浏览器下载对话框结构可能更新，请联系管理员
        /// </summary>
        Error_360se_TreeStructure_NotMatch = 475,

        /// <summary>
        /// 浏览器消息传输超时，请检查连接状态或调整操作超时时间后重试。
        /// </summary>
        Error_TransferMsg_Timeout = 476,

        /// <summary>
        /// 浏览器脚本初始化失败，请检查浏览器扩展状态后重试。
        /// </summary>
        Error_CrxScriptInitializationError = 477,

        /// <summary>
        /// 浏览器扩展执行命令返回未分类错误。
        /// </summary>
        Error_BrowserExtensionCommandError = 478,

        /// <summary>
        /// 重启更新自定义浏览器
        /// </summary>
        Error_ZEXPLORE_NEED_UPDATE = 951,

        /// 浏览器进程启动失败，请检查浏览器安装路径和管理员权限设置
        /// </summary>
        Error_ProcessRefuseAccess = 950,

        /// <summary>
        /// 浏览器插件无法访问，请检查浏览器启动权限
        /// </summary>
        Error_ExtensionRefuseAccess = 949,

        /// <summary>
        /// Webview2运行时太低
        /// </summary>
        Error_WebView2VersionLow = 952,

        /// <summary>
        /// 检测到未安装Webview2
        /// </summary>
        Error_WebView2UnInstalled = 953,

        /// <summary>
        /// 检测到未安装【屏幕解锁插件】，请在【设置中心】>【工具插件】中安装。
        /// </summary>
        Error_UnlockScreenExtensionUnInstalled = 954,

        /// <summary>
        /// 检测到未安装【实在智能 RPA 插件】，请在【设置中心】>【工具插件】中安装。
        /// </summary>
        Error_360XExtensionUnInstalled = 955,

        /// <summary>
        /// 检测到未启用【实在智能 RPA 插件】，请确认目标网页已打开，并在浏览器中已启用此插件。
        /// </summary>
        Error_360XExtensionUnopen = 956,

        /// <summary>
        /// 检测到未安装【模拟硬件输入插件】
        /// </summary>
        Error_InterceptorUnInstalled = 957,

        Error_ServerHubUnopen = 958,
        /// <summary>
        /// 功能未实现
        /// </summary>
        Error_NotImplemented_Error = 959,

        /// <summary>
        /// 检测到未安装【实在智能 RPA 插件】，请在【设置中心】>【工具插件】中安装。
        /// </summary>
        Error_360ExtensionUnInstalled = 960,

        /// <summary>
        /// 检测到未启用【实在智能 RPA 插件】，请确认目标网页已打开，并在浏览器中已启用此插件。
        /// </summary>
        Error_360ExtensionUnopen = 961,

        /// <summary>
        /// 检测到未安装【实在智能 RPA 插件】，请在【设置中心】>【工具插件】中安装。
        /// </summary>
        Error_FireFoxExtensionUnInstalled = 962,

        /// <summary>
        /// 检测到未启用【实在智能 RPA 插件】，请确认目标网页已打开，并在浏览器中已启用此插件。
        /// </summary>
        Error_FireFoxExtensionUnopen = 963,

        /// <summary>
        /// 检测到未安装【实在智能 RPA 插件】，请在【设置中心】>【工具插件】中安装。
        /// </summary>
        Error_EdgeExtensionUnInstalled = 964,

        /// <summary>
        /// 检测到未启用【实在智能 RPA 插件】，请确认目标网页已打开，并在浏览器中已启用此插件。
        /// </summary>
        Error_EdgeExtensionUnopen = 965,

        /// <summary>
        /// 操作了正在运行中的功能，请先结束上一个操作，若依然存在问题请重流程后重试。
        /// </summary>
        Error_CannotParallelExcuteError = 966,

        /// <summary>
        /// 无法访问，请确保目录存在，且具有写入权限。
        /// </summary>
        Error_UnauthorizedAccessError = 967,

        /// <summary>
        /// 解锁插件检测失败，请联系运营人员。
        /// </summary>
        Error_CpUnlockPlugCheck = 968,

        /// <summary>
        /// 解锁失败，请联系运营人员。
        /// </summary>
        Error_CpUnLock = 969,

        /// <summary>
        /// 未成功匹配功能{0}，若重启程序后依然未成功加载，请在组件市场下载，或联系运营人员。
        /// </summary>
        Error_PluginGetAbility_NotFoundError = 970,

        /// <summary>
        /// 无权限，请以管理员身份运行设计器后重试
        /// </summary>
        Error_CaptureUnauthorizedAccessError = 971,

        /// <summary>
        /// 未正确初始化，请重启客户端再重试。若仍未解决，请联系运营人员。
        /// </summary>
        Error_PluginGetAbilityError = 972,

        /// <summary>
        /// 插件化功能运行失败，请联系运营人员。
        /// </summary>
        Error_PluginRunError = 973,

        /// <summary>
        /// 未成功加载功能补丁包{0}，若重启程序后依然未成功加载，请在组件市场下载，或联系运营人员。
        /// </summary>
        Error_PluginAbilityNotFoundError = 974,

        /// <summary>
        /// 远程断开失败，请重试，或联系运营人员。
        /// </summary>
        Error_LocalRemoteOff = 975,

        /// <summary>
        /// 断开远程连接失败，IP和用户名不能为空。
        /// </summary>
        Error_LocalRemoteOff_AccountOrIp = 976,

        /// <summary>
        /// 远程连接失败，请确认网络和环境无误再重试，或联系运营人员。
        /// </summary>
        Error_LocalRemoteConnection = 977,

        /// <summary>
        /// 连接失败，请确认账号密码无误且网络通畅再重试。
        /// </summary>
        Error_LocalRemoteConnection_PW = 978,

        /// <summary>
        /// 未找到远程计算机，请检查远程计算机是否已启动/网络是否正常/用户是否存在。
        /// </summary>
        Error_LocalRemoteConnection_Socket = 979,

        /// <summary>
        /// 远程连接失败，IP和账号不能为空。
        /// </summary>
        Error_LocalRemoteConnection_AccountOrIp = 980,

        /// <summary>
        /// 实在浏览器未正常打开，请确保浏览器已正常打开再重试。
        /// </summary>
        Error_ZEXPLORERNOTFOUND = 981,

        /// <summary>
        /// 远程取消失败，IP和账号不能为空。
        /// </summary>
        Error_RemoteCancel_AccountOrIp = 982,

        /// <summary>
        /// 远程取消失败，请确保环境无误再重试，或联系运营人员。
        /// </summary>
        Error_RemoteCancel = 983,

        /// <summary>
        /// 远程解锁失败，请确保环境无误再重试，或联系运营人员。
        /// </summary>
        Error_RemoteUnLock = 984,

        /// <summary>
        /// 屏幕锁屏状态获取失败，请联系运营人员。
        /// </summary>
        Error_Lock_State = 985,

        /// <summary>
        /// 远程解锁失败，IP、账号和密码等不能为空。
        /// </summary>
        Error_RemoteUnLock_AccountOrPwd = 986,

        /// <summary>
        /// 使用数据采集前，请先重装RPA插件。
        /// </summary>
        Error_ResetExtension = 987,

        /// <summary>
        /// 屏幕锁屏失败，请确保环境无误再重试，或联系运营人员。
        /// </summary>
        Error_RDPLock = 988,

        /// <summary>
        /// 屏幕锁屏失败，RDP远程连接失败。请检查本电脑是否支持远程桌面，建议使用Win10及以上专业版或旗舰版系统。
        /// </summary>
        Error_RDPLock_RdpError = 989,

        /// <summary>
        /// 屏幕锁屏失败，账号和密码不能为空。
        /// </summary>
        Error_ScreenLock_AccountOrPwd = 990,

        /// <summary>
        /// 屏幕解锁失败，请确保环境无误再重试，请联系运营人员。
        /// </summary>
        Error_RDPUnlockError = 991,

        /// <summary>
        /// 启动自动解锁程序失败或链接解锁程序失败，请重试，或联系运营人员。
        /// </summary>
        Error_RDPLock_UnlockNotRun = 992,

        /// <summary>
        /// 检测到未安装【Java插件】，请在【设置中心】>【工具插件】中安装。
        /// </summary>
        Error_JavaExtensionUnInstalled = 993,

        /// <summary>
        /// 检测到Chrome浏览器未启动，请确认已正常启动Chrome浏览器。
        /// </summary>
        Error_ChromeUnopen = 994,

        /// <summary>
        /// 检测到未安装【实在智能 RPA 插件】，请在【设置中心】>【工具插件】中安装。
        /// </summary>
        Error_ChromeExtensionUnInstalled = 995,

        /// <summary>
        /// 检测到未启用【实在智能 RPA 插件】，请确认目标网页已打开，并在浏览器中已启用此插件。
        /// </summary>
        Error_ChromeExtensionUnopen = 996,

        /// <summary>
        /// 该使用方式暂不支持
        /// </summary>
        Error_NotImplemented = 997,

        /// <summary>
        /// 用户取消
        /// </summary>
        Error_USERCLOSE = 998,

        /// <summary>
        /// 未知错误
        /// </summary>
        Error_UNKOWN = 999,



    }
    #endregion
}
