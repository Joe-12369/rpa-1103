using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using DevExpress.Spreadsheet;
using II.RPA.Plugin.Excel.Utils;
using Newtonsoft.Json;
using System.Linq;

namespace II.RPA.Plugin.Excel.Actions.NonSoftware
{
    /// <summary>
    /// 不使用软件自动化
    /// 设计器.Sheet操作类组件
    /// </summary>
    public partial class NonSoftware : IExcel
    {

        /// <summary>
        /// 插入Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="name">Sheet名称</param>  
        /// <param name="position">当前Sheet位置["之前","之后"]</param>  
        void IExcel.Insert(string workbook, string name, string position)
        {
            var wb = Workbooks[workbook];
            if (wb.Worksheets.Contains(name))
            {
                throw new ExcelException(Localize($"工作簿中已存在 {name} 的工作表", "Worksheet_9A404BBA"));
            }
            var activeIndex = wb.Worksheets.ActiveWorksheet.Index;
            if (position.Equals("之前"))
            {
                Workbooks[workbook].Worksheets.Insert(activeIndex, name);
            }
            else
            {
                Workbooks[workbook].Worksheets.Insert(activeIndex + 1, name);
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
            if (range.Equals("全部"))
            {
                var sheetNames = from sheet in Workbooks[workbook].Worksheets
                                 select sheet.Name;
                strName = JsonConvert.SerializeObject(sheetNames);
            }
            else
            {
                strName = Workbooks[workbook].Worksheets.ActiveWorksheet?.Name;
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
            var _sheet = GetWorksheetByName(workbook, sheet);
            if (string.IsNullOrEmpty(newSheet))
            {
                throw new ExcelException("新工作表名称不能为空".I18n("Worksheet_9725D579"));
            }
            _sheet.Name = newSheet;
        }

        /// <summary>
        /// 复制Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="newSheet">新Sheet名称</param>  
        void IExcel.Copy(string workbook, string sheet, string newSheet)
        {
            var wb = Workbooks[workbook];
            if (string.IsNullOrWhiteSpace(newSheet))
            {
                throw new ExcelException("新Sheet名称不能为空".I18n("Worksheet_352D0C41"));
            }
            wb.Worksheets.Add(newSheet).CopyFrom(GetWorksheetByName(workbook, sheet));
        }

        /// <summary>
        /// 复制Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="option">复制目标选项</param>  
        /// <param name="target">目标文件对象</param>  
        /// <param name="newSheet">新Sheet名称</param>  
        /// <param name="_override">覆盖同名Sheet</param>  
        void IExcel.CopyV2(string workbook, string sheet, int option, string target, string newSheet, int _override)
        {
            if (!Workbooks.ContainsKey(target))
            {
                throw new ExcelException(Localize($"{target} 未打开", "Worksheet_1ACA35B1"));
            }
            if (string.IsNullOrWhiteSpace(newSheet))
            {
                throw new ExcelException("新Sheet名称不能为空".I18n("Worksheet_352D0C41"));
            }
            var targetWb = option == 0 ? workbook : target;
            if (ExistSheet(targetWb, newSheet))
            {
                if (_override == 1)
                {
                    Instance.Delete(targetWb, newSheet);
                }
                else
                {
                    throw new ExcelException("目标文件已存在同名Sheet".I18n("Worksheet_63194563"));
                }
            }
            if (option == 0)
            {
                Instance.Copy(workbook, sheet, newSheet);
            }
            else
            {
                Workbooks[target].Worksheets.Add(newSheet).CopyFrom(GetWorksheetByName(workbook, sheet));
                Workbooks[target].SaveDocument(target);
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
            Worksheet target = GetWorksheetByName(destWorkbook, destSheet);

            Worksheet source = GetWorksheetByName(workbook, sheet);
            if (option == 0)
            {
                target.Range[$"{ExcelUtils.ColumnIndexToLetter(source.GetDataRange().LeftColumnIndex + 1)}{target.GetDataRange().BottomRowIndex + 2}"].CopyFrom(source.GetUsedRange(), pasteOptions: PasteSpecial.All);
            }
            else
            {
                var _column = target.GetDataRange().RightColumnIndex;
                target.Range[$"{ExcelUtils.ColumnIndexToLetter(_column + 2)}1"].CopyFrom(source.GetUsedRange(), pasteOptions: PasteSpecial.All);
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
            var wb = Workbooks[workbook];
            if (mode.Equals("按名称"))
            {
                wb.Worksheets.ActiveWorksheet = GetWorksheetByName(workbook, sheet);
            }
            else
            {
                if (index < 1)
                {
                    throw new ExcelException("激活的 Sheet 序号不能小于 1".I18n("Worksheet_B4375A4F"));
                }
                wb.Worksheets.ActiveWorksheet = GetWorksheetByIndex(workbook, index);
            }
        }

        /// <summary>
        /// 删除Sheet
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        void IExcel.Delete(string workbook, string sheet)
        {
            Workbooks[workbook].Worksheets.Remove(GetWorksheetByName(workbook, sheet));
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
            var ws = GetWorksheetByName(workbook, sheet);
            if (mode.Equals("按位置"))
            {
                switch (position)
                {
                    case "第一位": ws.MoveToBeginning(); break;
                    case "最后一位": ws.MoveToEnd(); break;
                    case "向前移一位": ws.Move(ws.Index - 1); break;
                    case "向后移一位": ws.Move(ws.Index + 1); break;
                    default:
                        break;
                }
            }
            else if (mode.Equals("按序号"))
            {
                GetWorksheetByIndex(workbook, index);
                ws.Move(index - 1);
            }
            else//按名称
            {
                GetWorksheetByName(workbook, name);
                if (option.Equals("之前"))
                {
                    ws.MoveBefore(name);
                }
                else
                {
                    ws.MoveAfter(name);
                }
            }
        }
    }
}