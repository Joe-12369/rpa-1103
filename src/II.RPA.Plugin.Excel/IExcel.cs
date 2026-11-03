using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace II.RPA.Plugin.Excel
{
    public interface IExcel
    {
        /// <summary>
        /// 执行上下文
        /// </summary>
        ActionContext ActionContext { get; set; }

        /// <summary>
        /// 新建Excel
        /// </summary>      
        /// <param name="path">文件路径</param>  
        /// <param name="name">文件名称</param>  
        /// <param name="software">使用软件["默认","MicrosoftExcel","WPS"]</param>  
        /// <param name="visible">是否可视化["是","否"]</param>  
        /// <param name="maximize">是否最大化["是","否"]</param> 
        /// <param name="show">是否显示软件["是","否"]</param> 
        void New(string path, string name, string software, string visible, string maximize, string show);
        void NewV6(string path, string name, string software, string visible, string maximize, string show);

        /// <summary>
        /// 打开Excel
        /// </summary>      
        /// <param name="file">文件路径</param>  
        /// <param name="software">使用软件["默认","MicrosoftExcel","WPS"]</param>  
        /// <param name="visible">是否可视化["是","否"]</param>  
        /// <param name="maximize">是否最大化["是","否"]</param>  
        /// <param name="password1">打开密码</param>  
        /// <param name="password2">编辑密码</param>  
        /// <param name="show">是否显示软件["是","否"]</param> 
        void Open(string file, string software, string visible, string maximize, string password1, string password2, string show);
        void OpenV5(string file, string software, string visible, string maximize, string password1, string password2, string show);

        /// <summary>
        /// 获取已打开Excel
        /// </summary>     
        /// <param name="file">文件名称</param>  
        void Load(string file);

        /// <summary>
        /// 保存Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="close">保存后关闭["是","否"]</param>  
        void Save(string workbook, string close);

        /// <summary>
        /// 另存为Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="path">另存为路径</param>  
        /// <param name="name">文件名称</param>  
        /// <param name="close">保存后关闭["是","否"]</param>  
        /// <param name="cover">覆盖相同文件["是","否"]</param>  
        void SaveAs(string workbook, string path, string name, string close, string cover);
        void SaveAsV2(string workbook, string path, string name, string close, string cover);

        /// <summary>
        /// 关闭Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>            
        /// <param name="save">是否保存</param>
        /// <param name="close">关闭软件["是","否"]，第一次需求表示是否关闭软件，第二次需求变更为，否：关闭当前，是：关闭所有</param>  
        void Close(string workbook, string save, string close);
        void CloseV2(int closeType, string workbook, int closeAction, string filePath, int cover, int software, int kill);
        void CloseV3(int closeType, string workbook, int closeAction, string filePath, int cover, int software, int kill);

        /// <summary>
        /// 导出Excel为PDF
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="option">Sheet名称["全部","激活页"]</param>  
        /// <param name="path">导出路径</param>  
        /// <param name="name">文件名称</param>  
        /// <param name="cover">覆盖相同文件["是","否"]</param>  
        void Export(string workbook, string option, string path, string name, string cover);
        void ExportV2(string workbook, string option, string path, string name, string cover);

        /// <summary>
        /// 加密Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="password1">打开密码</param>  
        /// <param name="password2">编辑密码</param>  
        void Encrypt(string workbook, string password1, string password2);

        /// <summary>
        /// 获取Excel路径
        /// </summary>     
        /// <param name="workbook">文件对象</param>  
        void GetPath(string workbook);

        /// <summary>
        /// 插入Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="name">Sheet名称</param>  
        /// <param name="position">当前Sheet位置["之前","之后"]</param>  
        void Insert(string workbook, string name, string position);

        /// <summary>
        /// 获取Sheet名
        /// </summary>      
        /// <param name="workbook">文件对象</param>  
        /// <param name="range">获取范围["全部","当前激活页"]</param>  
        void GetName(string workbook, string range);

        /// <summary>
        /// 重命名Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">原Sheet名称</param>  
        /// <param name="newSheet">新Sheet名称</param>  
        void Rename(string workbook, string sheet, string newSheet);

        /// <summary>
        /// 复制Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="newSheet">新Sheet名称</param>  
        void Copy(string workbook, string sheet, string newSheet);

        /// <summary>
        /// 复制Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="target">目标文件对象</param>  
        /// <param name="newSheet">新Sheet名称</param>  
        void CopyV2(string workbook, string sheet, int option, string target, string newSheet, int _override);

        /// <summary>
        /// 合并Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="option">选项</param>
        /// <param name="destWorkbook">目标文件对象</param>  
        /// <param name="destSheet">目标Sheet名称</param>  
        void Merge(string workbook, string sheet, int option, string destWorkbook, string destSheet);

        /// <summary>
        /// 激活Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="mode">激活方式["按名称","按序号"]</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="index">Sheet序号</param>  
        void Active(string workbook, string mode, string sheet, int index);

        /// <summary>
        /// 删除Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        void Delete(string workbook, string sheet);

        /// <summary>
        /// 移动Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="mode">移动方式["按序号","按名称","按位置"]</param>  
        /// <param name="index">移动到</param>  
        /// <param name="name">移动到</param>  
        /// <param name="option">移动位置["之前","之后"]</param>  
        /// <param name="position">移动到["第一位","向前移一位","向后移一位","最后一位"]</param>  
        void Move(string workbook, string sheet, string mode, int index, string name, string option, string position);

        /// <summary>
        /// 读取单元格
        /// </summary>      
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        ///<param name="valueType">读取值方式</param>
        ///<param name="visible">读取隐藏值</param>
        void ReadCell(string workbook, string sheet, string column, string row, int valueType = 0, bool visible = false);


        /// <summary>
        /// 读取区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startRow">起始单元格行号</param>  
        /// <param name="startColumn">起始单元格列名</param>  
        /// <param name="lastRow">结束单元格行号</param>  
        /// <param name="lastColumn">结束单元格列名</param>  
        ///<param name="valueType">读取值方式</param>
        ///<param name="visible">读取隐藏值</param>
        void ReadRange(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, int type = 0, string range = null, int valueType = 0, bool visible = false);
        void ReadRangeV2(string workbook, string sheet, string range, int valueType = 0, bool visible = false);

        /// <summary>
        /// 读取单元格公式
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        void ReadCellFormula(string workbook, string sheet, string column, string row);

        /// <summary>
        /// 读取单元格背景颜色
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>        
        void ReadCellBackground(string workbook, string sheet, string column, string row, int format);

        /// <summary>
        /// 读取行
        /// </summary>      
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="rowIndex">行号</param>  
        ///<param name="valueType">读取值方式</param>
        ///<param name="visible">读取隐藏值</param>
        void ReadRow(string workbook, string sheet, int rowIndex, int valueType = 0, bool visible = false);

        /// <summary>
        /// 读取列
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="columnName">列名</param>  
        ///<param name="valueType">读取值方式</param>
        ///<param name="visible">读取隐藏值</param>
        void ReadColumn(string workbook, string sheet, string columnName, int valueType = 0, bool visible = false);

        /// <summary>
        /// 读取行数
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="range">读取范围["整个Sheet","指定一列"]</param>  
        /// <param name="column">列名</param>  
        void ReadRowCount(string workbook, string sheet, string range, string column);

        /// <summary>
        /// 读取列数
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="range">读取范围["整个Sheet","指定一行"]</param>  
        /// <param name="row">行号</param>  
        void ReadColumnCount(string workbook, string sheet, string range, int row);

        /// <summary>
        /// 获取第一个空白单元格
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="range">获取范围["指定一列","指定一行"]</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        void GetEmptyCell(string workbook, string sheet, string range, string column, int row);

        /// <summary>
        /// 获取第一个空白单元格
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="range">获取范围["指定一列","指定一行"]</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        void GetLastEmptyCell(string workbook, string sheet, string range, string column, int row);

        /// <summary>
        /// 获取已编辑区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>       
        void GetEditRange(string workbook, string sheet);

        /// <summary>
        /// 写入单元格
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="row">行号</param>  
        /// <param name="column">列名</param>  
        /// <param name="content">写入内容</param>            
        void WriteCell(string workbook, string sheet, int row, string column, string content);

        /// <summary>
        /// 写入行
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">起始列名</param>  
        /// <param name="row">起始行号</param>  
        /// <param name="contents">写入内容</param>  
        void WriteRow(string workbook, string sheet, string column, int row, string contents);

        /// <summary>
        /// 删除行
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="mode">删除方式["单独一行","连续多行","分散多行"]</param>  
        /// <param name="row">删除行号</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="rows">删除行号</param>  
        void DeleteRow(string workbook, string sheet, string mode, int row, int startRow, int lastRow, string rows);

        /// <summary>
        /// 写入列
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">起始列名</param>  
        /// <param name="row">起始行号</param>  
        /// <param name="contents">写入内容</param>  
        void WriteColumn(string workbook, string sheet, string column, int row, string contents);

        /// <summary>
        /// 删除列
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="mode">删除方式["单独一列","连续多列","分散多列"]</param>  
        /// <param name="column">删除列名</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="columns">删除列名</param>  
        void DeleteColumn(string workbook, string sheet, string mode, string column, string startColumn, string lastColumn, string columns);

        /// <summary>
        /// 插入行
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="index">在第几行前</param>  
        /// <param name="count">插入几行</param>  
        /// <param name="contents">写入内容</param>  
        /// <param name="directon">插入方向 0 向上 1向下</param>          
        void InsertRow(string workbook, string sheet, int index, int count, string contents, int direction);


        /// <summary>
        /// 插入列
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="index">在第几列前</param>  
        /// <param name="count">插入几列</param>  
        /// <param name="contents">写入内容</param>  
        /// <param name="direction">插入方向：0 向左 1向右</param>  
        void InsertColumn(string workbook, string sheet, int index, int count, string contents, int direction);


        /// <summary>
        /// 隐藏/显示行列
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="mode">隐藏/显示["隐藏","显示"]</param>  
        /// <param name="range">隐藏区域["行","列"]</param>  
        /// <param name="rows">行号</param>  
        /// <param name="columns">列名</param>  
        void Hide(string workbook, string sheet, string mode, string range, string rows, string columns);

        /// <summary>
        /// 插入图片
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="path">图片路径</param>  
        /// <param name="row">插入单元格行号</param>  
        /// <param name="column">插入单元格列名</param>  
        /// <param name="left">向右移动距离</param>  
        /// <param name="top">向下移动距离</param>  
        /// <param name="width">图片宽度</param>  
        /// <param name="height">图片高度</param>  
        void InsertPicture(string workbook, string sheet, string path, int row, string column, float left, float top, int width, int height);

        /// <summary>
        /// 删除图片
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="name">图片名称</param>  
        void DeletePicture(string workbook, string sheet, string name);

        /// <summary>
        /// 写入区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="row">起始行号</param>  
        /// <param name="column">起始列名</param>  
        /// <param name="contents">写入内容</param>  
        void WriteRange(string workbook, string sheet, int row, string column, string contents);

        /// <summary>
        /// 选中区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="lastColumn">结束列名</param>  
        void SelectRange(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, int type = 0, string range = null);
        void SelectRangeV2(string workbook, string sheet, string range);

        /// <summary>
        /// 清除区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="mode">是否清除格式["是","否"]</param>  
        void ClearRange(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, string mode, int type = 0, string range = null);
        void ClearRangeV2(string workbook, string sheet, string range, string mode);
        void ClearRangeV3(string workbook, string sheet, string range, int clearType);

        /// <summary>
        /// 删除区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="mode">删除模式["右侧单元格左移","下方单元格上移","整行","整列"]</param>  
        void DeleteRange(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, string mode, int type = 0, string range = null);
        void DeleteRangeV2(string workbook, string sheet, string range, string mode);

        /// <summary>
        /// 区域背景颜色
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="color">颜色</param>  
        void SetRangeBackground(string workbook, string sheet, string startColumn, int startRow, string lastColumn, int lastRow, string color, int type = 0, string range = null);
        void SetRangeBackgroundV2(string workbook, string sheet, string range, string color);

        /// <summary>
        /// 区域字体颜色
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="color">颜色</param>  
        void SetRangeFont(string workbook, string sheet, string startColumn, int startRow, string lastColumn, int lastRow, string color, int type = 0, string range = null);
        void SetRangeFontV2(string workbook, string sheet, string range, string color);

        /// <summary>
        /// 设置单元格格式
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet名称</param>
        /// <param name="range">区域</param>
        /// <param name="format">格式代码</param>
        void SetCellFormat(string workbook, string sheet, string range, string format);

        /// <summary>
        /// 设置单元格格式 整合版
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet名称</param>
        /// <param name="range">区域</param>
        /// <param name="formatJson">格式Json</param>
        void SetCellFormatEx(string workbook, string sheet, string range, string formatJson);

        /// <summary>
        /// 获取单元格格式 整合版
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet名称</param>
        /// <param name="range">区域</param>
        string GetCellFormatEx(string workbook, string sheet, string range);

        /// <summary>
        /// Excel分列功能
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet名称</param>      
        /// <param name="columnSplitParamJson">分列参数</param>
        void ColumnSplit(string workbook, string sheet, string column, string columnSplitParamJson);

        /// <summary>
        /// Excel分列
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet名称</param>
        /// <param name="column"></param>
        /// <param name="columnSplitParamJson">分列配置</param>
        /// <param name="destination">目标区域</param>
        /// <param name="startRow">开始行号</param>
        /// <param name="endRow">结束行号</param>
        /// <param name="splitDataType">分类数据类型设置（0，常规，1自定义）</param>
        /// <param name="fieldInfo">分列格式</param>
        /// <param name="insertCount">插入列数</param>
        void ColumnSplitV2(string workbook, string sheet, string column, string columnSplitParamJson, string destination, int startRow, int endRow,int splitDataType, string fieldInfo, int insertCount);


        /// <summary>
        /// 复制表格内容
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="mode">复制模式["复制单元格","复制行","复制列","复制区域","复制已编辑区域"]</param>  
        /// <param name="column">单元格列名</param>  
        /// <param name="row">单元格行号</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="startColumn2">起始列名</param>  
        /// <param name="startRow2">起始行号</param>  
        /// <param name="lastColumn2">结束列名</param>  
        /// <param name="lastRow2">结束行号</param>  
        void CopyRange(string workbook, string sheet, string mode, string column, int row, int startRow, int lastRow, string startColumn, string lastColumn, string startColumn2, int startRow2, string lastColumn2, int lastRow2);

        void CopyRangeV2(string workbook, string sheet, string range);
        /// <summary>
        /// 粘贴表格内容
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">单元格列名</param>  
        /// <param name="row">单元格行号</param>  
        void PastRange(string workbook, string sheet, string column, string row);

        /// <summary>
        /// 粘贴表格内容
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">单元格列名</param>  
        /// <param name="row">单元格行号</param>
        /// <param name="pasteType">粘贴类型</param>
        ///<param name="pasteOperation">运算符</param>
        ///<param name="skipBlanks">跳过空白单元格</param>
        ///<param name="transpose">是否转置</param>
        void PastRangeV2(string workbook, string sheet, string column, string row, int pasteType, int pasteOperation, bool skipBlanks, bool transpose);

        /// <summary>
        /// 删除重复
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="columns">待删除列名</param>  
        /// <param name="header">数据包含标题["是","否"]</param>  
        void DeleteDuplicates(string workbook, string sheet, string columns, string header);

        /// <summary>
        /// 设置列宽
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="columns">列名</param>  
        /// <param name="width">列宽</param>  
        /// <param name="isAutoFit">自适应列宽</param>  
        void SetWidth(string workbook, string sheet, string columns, float width, bool isAutoFit = false);

        /// <summary>
        /// 设置行高
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="rows">行号</param>  
        /// <param name="height">行高</param>  
        void SetHeight(string workbook, string sheet, string rows, float height, bool isAutoFit = false);

        /// <summary>
        /// 单元格背景颜色
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        /// <param name="color">颜色</param>  
        void SetCellBackground(string workbook, string sheet, string column, int row, string color);

        /// <summary>
        /// 单元格字体颜色
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        /// <param name="color">颜色</param>  
        void SetCellFont(string workbook, string sheet, string column, int row, string color);

        /// <summary>
        /// 新建透视表
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="rule">透视表规则</param>  
        void InsertPivotTable(string workbook, string rule, string sourceData = "");

        /// <summary>
        /// 刷新透视表
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="pivotTable">透视表</param>
        void RefreshPivotTable(string workbook, string sheet, string pivotTable);

        /// <summary>
        /// 筛选透视表
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="pivotTable">透视表</param>
        /// <param name="field">筛选字段</param>
        /// <param name="fieldValue">筛选字段值</param>
        void FilterPivotTable(string workbook, string sheet, string pivotTable, string field, string fieldValue);

        /// <summary>
        /// 新建透视图
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="rule">透视图规则</param>  
        void InsertPivotView(string workbook, string rule);


        /// <summary>
        /// 数据排序
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="sortOrder">排序方式（升序、降序、自定义）</param>
        /// <param name="sortRange">排序范围</param>
        /// <param name="region">是否扩展区域</param>
        /// <param name="header">首行是否参与排序</param>
        /// <param name="rule">自定义规则</param>
        void Sort(string workbook, string sheet, int sortOrder, string sortRange, int region, int header, string rule);


        /// <summary>
        /// 数据筛选
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="column">列名</param>
        /// <param name="condition">条件选项</param>
        /// <param name="criteria">条件</param>
        /// <param name="on_header">筛选首行</param>
        /// <param name="isMultiFilter">多列筛选</param>
        /// <param name="pre_filterData">筛选数据</param>
        /// <param name="pre_lineNum">行号</param>
        void Filter(string workbook, string sheet, string column, string condition, string criteria, string on_header, string isMultiFilter = "false", string pre_filterData = "", string pre_lineNum = "");

        /// <summary>
        /// Excel 筛选
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="rule">筛选规则</param>
        /// <param name="filterRange">筛选范围</param>
        void ExcelFilter(string workbook, string sheet, string rule, int startRow, int endRow);

        /// <summary>
        /// 切片器选择
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="slicerName">切片器名称</param>
        /// <param name="slicerOptions">切片器选项</param>
        void SetSlicer(string workbook, string slicerName, string slicerOptions);

        /// <summary>
        /// 执行宏
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="script">宏模块</param> 
        /// <param name="mode">执行宏方式</param>  
        /// <param name="code">宏代码</param> 
        /// <param name="args">宏参数</param>  
        void RunMacro(string workbook, string script, string args, int mode, string code);

        /// <summary>
        /// 内容循环
        /// </summary>      
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="startRow">开始行</param>
        /// <param name="endRow">结束行</param>
        /// <param name="startColumn">开始列</param>
        /// <param name="endColumn">结束列</param>
        /// <param name="regionType">循环标识【1:行,2:列,3:区域,4:已编辑区域】</param>
        void Loop(string workbook, string sheet, int startRow, int endRow, string startColumn, string endColumn, int regionType, int valueType);
        void LoopV2(string workbook, string sheet, string range, int valueType);

        /// <summary>
        /// Excel 读取规则
        /// </summary>
        /// <param name="componentId">组件</param>
        /// <returns></returns>
        string GetExcelRule(dynamic componentId);

        /// <summary>
        /// 导出指定行列的图片
        /// </summary>     
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">工作表</param>
        /// <param name="path">保存的文件路径</param>
        /// <param name="range">导出范围 列名 或行号（'A' 或 1）</param>
        /// <exception cref="ExcelException"></exception>
        void GetShapes(string workbook, string sheet, string path, string range);

        /// <summary>
        /// 导出区域为图片
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="range">区域</param>        
        /// <param name="path">路径</param>
        /// <param name="name">文件名</param>
        /// <param name="cover">是否覆盖</param>
        void ExportRangeAsPicture(string workbook, string sheet, string range, string path, string name, bool cover);
        void ExportRangeAsPictureV2(string workbook, string sheet, string range, string path, string name, bool cover);

        /// <summary>
        /// 查找替换
        /// </summary>
        /// <param name="workbook">工作簿对象</param>
        /// <param name="sheet">工作表</param>
        /// <param name="mode">查找模式【查找、查找替换】</param>
        /// <param name="option">区域选项【区域、行、列、已编辑区域】</param>
        /// <param name="range">区域值</param>
        /// <param name="input">查找内容</param>
        /// <param name="replace">替换内容</param>
        /// <param name="wholeWord">全字匹配</param>
        /// <param name="matchRange">查找范围</param>
        /// <param name="outType">输出类型【行号/列名、单元格】</param>      
        void Search(string workbook, string sheet, string mode, string option, string range, object input, object replace, bool wholeWord, string matchRange, int outType);

        /// <summary>
        /// 自动填充
        /// </summary>
        /// <param name="workbook">工作簿对象</param>
        /// <param name="sheet">工作表</param>
        /// <param name="sourceRange">源区域</param>
        /// <param name="destination">目标区域</param>
        void AutoFill(string workbook, string sheet, string sourceRange, string destination);
        void AutoFillV2(string workbook, string sheet, string excelRng, string destination);

        /// <summary>
        /// 取消Excel筛选状态
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheet"></param>
        void EnableExcelFilter(string workbook, string sheet);

        /// <summary>
        /// 清除Excel 筛选条件
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheet"></param>
        /// <param name="option"></param>
        /// <param name="column"></param>
        void ClearExcelFilter(string workbook, string sheet, int option, string column);

        /// <summary>
        /// 设置Excel表格状态
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="table">表格名</param>
        /// <param name="headerRow">标题行</param>
        /// <param name="totalRow">汇总行</param>       
        /// <param name="firstColumn">第一列</param>
        /// <param name="lastColumn">最后一列</param>      
        /// <param name="filterButton">筛选按钮</param>
        void ShowTableStyle(string workbook, string sheet, string table, int headerRow, int totalRow, int firstColumn, int lastColumn, int filterButton);

        /// <summary>
        /// 组合行列
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="group">组合0/取消组合1</param>
        /// <param name="groupType">组合类型：行0/列1</param>
        /// <param name="startRow">开始行</param>
        /// <param name="endRow">结束行</param>
        /// <param name="startColumn">开始列</param>
        /// <param name="endColumn">结束列</param>
        void Group(string workbook, string sheet, int group, int groupType, int startRow, int endRow, string startColumn, string endColumn);

        /// <summary>
        /// 合并单元格
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="merge">合并0/取消合并1</param>
        /// <param name="range">合并区域</param>       
        void MergeCells(string workbook, string sheet, int merge, string range);

        /// <summary>
        /// 刷新表格数据
        /// </summary>
        /// <param name="workbook"></param>
        void RefreshAll(string workbook);

        /// <summary>
        /// Excel 查找替换
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="mode">功能模式（查找0，替换1）</param>
        /// <param name="option">区域选项（行0，列1，区域2，已编辑3）</param>
        /// <param name="range">区域</param>
        /// <param name="row">行号（行选项）</param>
        /// <param name="column">列名（列选项）</param>
        /// <param name="what">查找内容</param>
        /// <param name="lookAt">全字匹配</param>
        /// <param name="lookIn">查找方式</param>
        /// <param name="searchOrder">查找顺序</param>
        /// <param name="searchDirection">查找方向</param>
        /// <param name="matchCase">匹配大小写（否0，是1）</param>
        /// <param name="matchByte">匹配字节（否0，是1）</param>
        /// <param name="matchFormat">匹配格式</param>
        /// <param name="replacement">替换内容</param>
        /// <param name="replaceFormat">替换格式</param>
        /// <param name="matchFirst">查找结果（所有0，第一个1）</param>
        /// <param name="returnType">返回值类型（行号0，位置1）</param>
        void Find(string workbook, string sheet, int mode, int option, string range, int row, string column, string what, int lookAt, int lookIn, int searchOrder, int searchDirection, int matchCase, int matchByte, string matchFormat, string replacement, string replaceFormat, int matchFirst, int returnType);


        /// <summary>
        /// 动态调用
        /// </summary>
        /// <param name="obj">对象类型</param>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="range">区域</param>
        /// <param name="property">属性</param>
        /// <param name="value">值</param>
        void Interop(int obj, string workbook, string sheet, string range, string property, dynamic value);
    }
}
