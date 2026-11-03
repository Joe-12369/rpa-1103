using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Excel.Utils;
using Newtonsoft.Json;
using System;

namespace II.RPA.Plugin.Excel.Actions.Software
{
    /// <summary>
    /// 使用软件自动化
    /// 设计器.Sheet操作类组件
    /// </summary>
    public partial class SoftwareExcel : IExcel
    {

        /// <summary>
        /// 插入Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="name">Sheet名称</param>  
        /// <param name="position">当前Sheet位置["之前","之后"]</param>  

        void IExcel.Insert(string workbook, string name, string position)
        {
            object wb = GetWorkbook(workbook);
            if (Exist(workbook, name))
            {
                throw new ExcelException("已存在同名的工作表".I18n("Worksheet_31F63129"));
            }
            using (var com = new ComScope())
            {
                object worksheets = com.Track(ComLateBinding.GetProp(wb, "Worksheets"));
                object activeSheet = com.Track(ComLateBinding.GetProp(wb, "ActiveSheet"));
                object newSheet = position.Equals("之前")
                    ? AddWorksheetBefore(com, worksheets, activeSheet)
                    : AddWorksheetAfter(com, worksheets, activeSheet);
                ComLateBinding.SetProp(newSheet, "Name", name);
            }
        }

        /// <summary>
        /// 获取Sheet名
        /// </summary>     
        /// <param name="workbook">文件对象</param>  
        /// <param name="range">获取范围["全部","当前激活页"]</param>  
        void IExcel.GetName(string workbook, string range)
        {
            string strName;
            object wb = GetWorkbook(workbook);
            using (var com = new ComScope())
            {
                if (range.Contains("全部"))
                {
                    object worksheets = com.Track(ComLateBinding.GetProp(wb, "Worksheets"));
                    int worksheetsCount = ComLateBinding.ToInt(ComLateBinding.GetProp(worksheets, "Count"));
                    var arrName = new string[worksheetsCount];
                    for (int i = 1; i <= worksheetsCount; i++)
                    {
                        object _sheet = com.Track(GetExcelItem(worksheets, i));
                        arrName[i - 1] = Convert.ToString(ComLateBinding.GetProp(_sheet, "Name"));
                    }
                    strName = JsonConvert.SerializeObject(arrName);
                }
                else
                {
                    object activeSheet = com.Track(ComLateBinding.GetProp(wb, "ActiveSheet"));
                    strName = Convert.ToString(ComLateBinding.GetProp(activeSheet, "Name"));
                }
            }
            ContextInstance.ActionContext.AddResult("strName", strName);
        }

        /// <summary>
        /// 重命名Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">原Sheet名称</param>  
        /// <param name="newSheet">新Sheet名称</param>            
        void IExcel.Rename(string workbook, string sheet, string newSheet)
        {
            if (string.IsNullOrEmpty(newSheet))
            {
                throw new ExcelException("新工作表名称不能为空".I18n("Worksheet_9725D579"));
            }
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                ComLateBinding.SetProp(_sheet, "Name", newSheet);
            }
        }

        /// <summary>
        /// 复制Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="newSheet">新Sheet名称</param>            
        void IExcel.Copy(string workbook, string sheet, string newSheet)
        {
            if (string.IsNullOrWhiteSpace(newSheet))
            {
                throw new ExcelException("新Sheet名称不能为空".I18n("Worksheet_352D0C41"));
            }
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                CopyWorksheetAfter(_sheet, _sheet);
                object activeSheet = com.Track(ComLateBinding.GetProp(GetWorkbook(workbook), "ActiveSheet"));
                ComLateBinding.SetProp(activeSheet, "Name", newSheet);
            }
        }

        /// <summary>
        /// 复制Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="option">复制文件选项</param>  
        /// <param name="target">目标文件对象</param>  
        /// <param name="newSheet">新Sheet名称</param>  
        /// <param name="_override">是否覆盖同名sheet</param>          
        void IExcel.CopyV2(string workbook, string sheet, int option, string target, string newSheet, int _override)
        {
            if (option == 1 && !Workbooks.ContainsKey(target))
            {
                throw new ExcelException(Localize($"{target} 未打开", "Worksheet_1ACA35B1"));
            }
            var targetWb = option == 0 ? workbook : target;

            if (string.IsNullOrWhiteSpace(newSheet))
            {
                throw new ExcelException("新Sheet名称不能为空".I18n("Worksheet_352D0C41"));
            }
            ExcelUtils.TryFuncIgnoreException(() =>
            {
                using (var com = new ComScope())
                {
                    object _sheet = com.Track(GetWorksheetByName(targetWb, newSheet));
                    if (_override == 1)
                    {
                        ComLateBinding.Invoke(_sheet, "Delete");
                    }
                    else
                    {
                        throw new ExcelException("目标文件中已存在同名Sheet".I18n("Worksheet_8626D864"));
                    }
                }
            });
            if (option == 0)
            {
                Instance.Copy(workbook, sheet, newSheet);
            }
            else
            {
                using (var com = new ComScope())
                {
                    object wb = GetWorkbook(target);
                    object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                    object targetActiveSheet = com.Track(ComLateBinding.GetProp(wb, "ActiveSheet"));
                    CopyWorksheetAfter(worksheet, targetActiveSheet);
                    object newActiveSheet = com.Track(ComLateBinding.GetProp(wb, "ActiveSheet"));
                    ComLateBinding.SetProp(newActiveSheet, "Name", newSheet);
                }
            }

        }


        /// <summary>
        /// 合并Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="option">复制文件选项</param>  
        /// <param name="destWorkbook">目标文件对象</param>  
        /// <param name="destSheet">新Sheet名称</param>         
        void IExcel.Merge(string workbook, string sheet, int option, string destWorkbook, string destSheet)
        {
            try
            {
                if (workbook == destWorkbook && sheet == destSheet)
                {
                    throw new ExcelException("不能合并同一个Excel文件的同一个Sheet".I18n("Worksheet_2779C014"));
                }
                using (var com = new ComScope())
                {
                    object dest = com.Track(GetWorksheetByName(destWorkbook, destSheet));
                    object worksheet = com.Track(GetWorksheetByName(workbook, sheet));

                    object destUsedRange = com.Track(ComLateBinding.GetProp(dest, "UsedRange"));
                 
                    var addr = ContextInstance.ActionContext.ExcelUsedRange.Address;
                    object range = com.Track(ComLateBinding.GetProp(worksheet, "Range", addr));

                    if (option == 0)
                    {
                        Instance.GetEditRange(destWorkbook, destSheet);
                        var targetRow = ContextInstance.ActionContext.Result.Value<int>("endRowNum");
                        object targetRange = com.Track(ComLateBinding.GetProp(dest, "Range", $"{ContextInstance.ActionContext.ExcelUsedRange.StartColumn}{targetRow + 1}"));
                        ComLateBinding.Invoke(range, "Copy", targetRange);
                    }
                    else
                    {
                        var targetLastCell = GetLastCell(destUsedRange, 2, 2);
                        object targetRange = com.Track(ComLateBinding.GetProp(dest, "Range", $"{ExcelUtils.ColumnIndexToLetter(targetLastCell.Item1 + 1)}1"));
                        ComLateBinding.Invoke(range, "Copy", targetRange);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("HRESULT") || ex.Message.ToLower().Contains("com"))
                {
                    throw new ExcelException(Localize($"合并失败，可能原因：在不同的应用实例下打开了两个同名的文件", "Worksheet_BAD48920"));
                }
                throw ex;

            }

        }

        /// <summary>
        /// 激活Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="mode">激活方式["按名称","按序号"]</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="index">Sheet序号</param>            
        void IExcel.Active(string workbook, string mode, string sheet, int index)
        {
            using (var com = new ComScope())
            {
                if (mode.Equals("按名称"))
                {
                    object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                    ComLateBinding.Invoke(worksheet, "Activate");
                }
                else
                {
                    object worksheet = com.Track(GetWorksheetByIndex(workbook, index));
                    ComLateBinding.Invoke(worksheet, "Activate");
                }
            }
        }

        /// <summary>
        /// 删除Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>            
        void IExcel.Delete(string workbook, string sheet)
        {
            using (var com = new ComScope())
            {
                object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                ComLateBinding.Invoke(worksheet, "Delete");
            }
        }

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
        void IExcel.Move(string workbook, string sheet, string mode, int index, string name, string option, string position)
        {
            using (var com = new ComScope())
            {
                object ws = com.Track(GetWorksheetByName(workbook, sheet));
                if (mode.Equals("按位置"))
                {
                    switch (position)
                    {
                        case "第一位":
                            object firstSheet = com.Track(GetWorksheetByIndex(workbook, 1));
                            MoveWorksheetBefore(ws, firstSheet);
                            break;
                        case "最后一位":
                            object wb = GetWorkbook(workbook);
                            object worksheets = com.Track(ComLateBinding.GetProp(wb, "Worksheets"));
                            object lastSheet = com.Track(GetWorksheetByIndex(workbook, ComLateBinding.ToInt(ComLateBinding.GetProp(worksheets, "Count"))));
                            MoveWorksheetAfter(ws, lastSheet);
                            break;
                        case "向前移一位":
                            object prevSheet = com.Track(ComLateBinding.GetProp(ws, "Previous"));
                            if (prevSheet != null) MoveWorksheetBefore(ws, prevSheet);
                            break;
                        case "向后移一位":
                            object nextSheet = com.Track(ComLateBinding.GetProp(ws, "Next"));
                            if (nextSheet != null) MoveWorksheetAfter(ws, nextSheet);
                            break;
                        default:
                            break;
                    }
                }
                else if (mode.Equals("按序号"))
                {
                    object targetSheet = com.Track(GetWorksheetByIndex(workbook, index));
                    if (ComLateBinding.ToInt(ComLateBinding.GetProp(ws, "Index")) < index)
                    {
                        MoveWorksheetAfter(ws, targetSheet);
                    }
                    else
                    {
                        MoveWorksheetBefore(ws, targetSheet);
                    }
                }
                else//按名称
                {
                    object targetSheet = com.Track(GetWorksheetByName(workbook, name));
                    if (option.Equals("之前"))
                    {
                        MoveWorksheetBefore(ws, targetSheet);
                    }
                    else
                    {
                        MoveWorksheetAfter(ws, targetSheet);
                    }
                }
            }
        }

        private object AddWorksheetBefore(ComScope com, object worksheets, object beforeSheet)
        {
            return com.Track(ComLateBinding.Invoke(worksheets, "Add", beforeSheet, Type.Missing));
        }

        private object AddWorksheetAfter(ComScope com, object worksheets, object afterSheet)
        {
            return com.Track(ComLateBinding.Invoke(worksheets, "Add", Type.Missing, afterSheet));
        }

        private void CopyWorksheetAfter(object worksheet, object afterSheet)
        {
            ComLateBinding.Invoke(worksheet, "Copy", Type.Missing, afterSheet);
        }

        private void MoveWorksheetBefore(object worksheet, object beforeSheet)
        {
            ComLateBinding.Invoke(worksheet, "Move", beforeSheet, Type.Missing);
        }

        private void MoveWorksheetAfter(object worksheet, object afterSheet)
        {
            ComLateBinding.Invoke(worksheet, "Move", Type.Missing, afterSheet);
        }

    }
}
