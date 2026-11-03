using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using DevExpress.Utils.Extensions;
using II.RPA.Plugin.Excel.Forms;
using II.RPA.Plugin.Excel.Models;
using II.RPA.Plugin.Excel.Params;
using II.RPA.Plugin.Excel.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace II.RPA.Plugin.Excel.Actions.Software
{
    /// <summary>
    /// Excel 内置对象的读取交互
    /// </summary>
    public partial class SoftwareExcel
    {

        private Func<object, object, object> RuleFunc;

        /// <summary>
        /// 读取Excel 对象配置规则
        /// </summary>
        /// <param name="component"></param>
        /// <returns>组件配置信息</returns>
        string IExcel.GetExcelRule(dynamic component)
        {
            string componentId, args = null;
            if (component is string)
            {
                componentId = component;
            }
            else
            {
                componentId = component.componentId;
                args = component.componentModal.argName;
            }
            string retVal = "";
            ExcelFormContext context = new ExcelFormContext();
            if (componentId.Contains("SetCellFormat") && args == "format")
            {
                context.Label = "格式".I18n("ExcelObject_A8F2B8BA");
                context.Image = "../Forms/CellFormat.png";
                context.InfoMessage = "鼠标左键单击".I18n("ExcelObject_2593D066");
                context.ExcelObject = "单元格".I18n("ExcelObject_B8D22A17");
                context.Version = componentId.Contains("V") ? 2 : 1;

                if (context.Version == 2)
                {
                    var frmCellFormatEx = new ExcelForm(context);
                    RuleFunc += GetCellFormatV2;

                    var diaRet = frmCellFormatEx.ShowDialog();
                    if (diaRet == true)
                    {
                        retVal = frmCellFormatEx.Context.Value;
                    }
                    else
                    {
                        retVal = "";
                    }
                }
                else
                {
                    var frmCellFormat = new ExcelForm(context);
                    RuleFunc += GetCellFormat;

                    var diaRet = frmCellFormat.ShowDialog();
                    if (diaRet == true)
                    {
                        retVal = frmCellFormat.Context.Value;
                    }
                    else
                    {
                        retVal = "";
                    }
                }


                selectRuleCallback = null;
                return JsonConvert.SerializeObject(new JObject() { { "rawField", $"{JsonConvert.ToString(retVal)}" } });

            }
            else if (componentId.Contains("PivotTable"))
            {
                //插入透视表返回规则，刷新、筛选返回名称
                context.Arguments = componentId.Contains("Insert") ? 1 : 2;
                context.Label = "透视表".I18n("ExcelObject_DB0536A6");
                context.Image = "../Forms/PivotTable.png";
                context.InfoMessage = "鼠标左键单击".I18n("ExcelObject_2593D066");
                context.ExcelObject = "透视表".I18n("ExcelObject_DB0536A6");

                var frmCellFormat = new ExcelForm(context);
                RuleFunc += GetPivotTableRule;
                frmCellFormat.ShowDialog();
                selectRuleCallback = null;
                return JsonConvert.SerializeObject(new { rawField = context.Value });
            }
            else if (componentId.Contains("Sort") && args == "rule")
            {
                context.Title = "选择自定义排序规则".I18n("ExcelObject_EC0B8DB4");
                context.Label = "排序范围".I18n("ExcelObject_4AEBD1A1");
                context.Image = "../Forms/Sort.png";
                context.InfoMessage = "鼠标左键单击".I18n("ExcelObject_2593D066");
                context.ExcelObject = "排序表格".I18n("ExcelObject_F74FF0B8");

                var frmCellFormat = new ExcelForm(context);
                RuleFunc += GetSortRule;
                frmCellFormat.ShowDialog();
                selectRuleCallback = null;
                return JsonConvert.SerializeObject(new { rawField = context.Value });
            }
            else if (componentId.Contains("Filter"))
            {
                context.Title = "选择筛选".I18n("ExcelObject_9AD76AEF");
                context.Image = "../Forms/Default.png";
                context.Label = "筛选范围".I18n("ExcelObject_71EE0634");
                context.InfoMessage = "鼠标左键单击".I18n("ExcelObject_2593D066");
                context.ExcelObject = "筛选表格".I18n("ExcelObject_47A0284D");

                var frmCellFormat = new ExcelForm(context);
                RuleFunc += GetFilterRule;
                frmCellFormat.ShowDialog();
                selectRuleCallback = null;
                return JsonConvert.SerializeObject(new { rawField = context.Value });
            }
            else
            {
                context.Label = "区域".I18n("ExcelObject_B141291C");
                context.Image = "../Forms/Range.png";
                context.InfoMessage = "鼠标左键框选".I18n("ExcelObject_21B2DE95");
                context.ExcelObject = "表格区域".I18n("ExcelObject_5F54C458");

                var frmCellFormat = new ExcelForm(context);
                RuleFunc += GetRangeAddress;
                var diaRet = frmCellFormat.ShowDialog();
                if (diaRet == true && !string.IsNullOrWhiteSpace(frmCellFormat.Context?.Value))
                {
                    var _rng = ExcelUtils.GetExcelRange(frmCellFormat.Context?.Value);
                    retVal = JsonConvert.SerializeObject(new ExcelRule { rawField = _rng }, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                }
                selectRuleCallback = null;
                return retVal;

            }

        }


        #region 单元格格式

        private string GetCellFormat(object sheet, object range)
        {
            return Convert.ToString(ComLateBinding.GetProp(range, "NumberFormatLocal"));
        }

        private string GetCellFormatV2(object sheet, object rangeObj)
        {
            logger.LogDebug("GetCellFormatEx 开始获取");

            var cellFormatParams = new CellFormatParams();

            try
            {
                using (var com = new ComScope())
                {
                    object rangeFont = com.Track(ComLateBinding.GetProp(rangeObj, "Font"));
                    object rangeInterior = com.Track(ComLateBinding.GetProp(rangeObj, "Interior"));
                    object rangeBorders = com.Track(ComLateBinding.GetProp(rangeObj, "Borders"));

                    /* ================= Font ================= */

                    cellFormatParams.FontFormat = ExcelUtils.SafeGet(() => Convert.ToString(ComLateBinding.GetProp(rangeFont, "Name")), string.Empty);
                    cellFormatParams.FontSize = ExcelUtils.SafeGet(() => Convert.ToDouble(ComLateBinding.GetProp(rangeFont, "Size")), 0d);

                    var botAndItalicParam = new BotAndItalicParam
                    {
                        Bold = ExcelUtils.SafeGet(() => ComLateBinding.ToBool(ComLateBinding.GetProp(rangeFont, "Bold")), false),
                        Italic = ExcelUtils.SafeGet(() => ComLateBinding.ToBool(ComLateBinding.GetProp(rangeFont, "Italic")), false)
                    };
                    cellFormatParams.BoldAndItalic = botAndItalicParam.GetBotAndItalicDescription();

                    int fontOleColor = ExcelUtils.SafeGet(() => Convert.ToInt32(ComLateBinding.GetProp(rangeFont, "Color")), -1);
                    if (fontOleColor > 0)
                    {
                        var fontColor = System.Drawing.ColorTranslator.FromOle(fontOleColor);
                        cellFormatParams.FontColor = ExcelUtils.TurnColorToCode(fontColor);
                    }
                    else
                    {
                        cellFormatParams.FontColor = string.Empty;
                    }

                    /* ================= Background ================= */

                    int interiorColor = ExcelUtils.SafeGet(() => Convert.ToInt32(ComLateBinding.GetProp(rangeInterior, "Color")), -1);
                    int colorIndex = ExcelUtils.SafeGet(() => Convert.ToInt32(ComLateBinding.GetProp(rangeInterior, "ColorIndex")), -1);

                    if (interiorColor > 0)
                    {
                        var backgroundColor = System.Drawing.ColorTranslator.FromOle(interiorColor);
                        string alpha;

                        if (backgroundColor.Name == "White")
                        {
                            alpha = colorIndex == -4142 ? "0%" : "100%";
                        }
                        else
                        {
                            alpha = $"{(int)(backgroundColor.A * 100.0 / 255)}%";
                        }

                        cellFormatParams.BackgroundColor =
                            $"{ExcelUtils.TurnColorToCode(backgroundColor)},{alpha}";
                    }
                    else
                    {
                        cellFormatParams.BackgroundColor = string.Empty;
                    }

                    /* ================= Text Control ================= */

                    bool wrapText = ExcelUtils.SafeGet(() => ComLateBinding.ToBool(ComLateBinding.GetProp(rangeObj, "WrapText")), false);
                    bool shrinkToFit = ExcelUtils.SafeGet(() => ComLateBinding.ToBool(ComLateBinding.GetProp(rangeObj, "ShrinkToFit")), false);

                    cellFormatParams.TextControl =
                        wrapText ? 1 :
                        shrinkToFit ? 2 : 0;

                    /* ================= Alignment ================= */

                    cellFormatParams.HorizontalAlignment =
                        cellFormatParams.GetEnumHorizontalAlignment(
                            ExcelUtils.SafeGet(() => Convert.ToInt32(ComLateBinding.GetProp(rangeObj, "HorizontalAlignment")), 0));

                    cellFormatParams.VerticalAlignment =
                        cellFormatParams.GetEnumVerticalAlignmentFromXlVAlign(
                            ExcelUtils.SafeGet(() => Convert.ToInt32(ComLateBinding.GetProp(rangeObj, "VerticalAlignment")), 0));

                    /* ================= Number Format ================= */

                    cellFormatParams.DigitalFormat = new DigitalFormatClass
                    {
                        enumDigitalFormat = EnumDigitalFormat.Custom,
                        formatstring = JsonConvert.ToString(
                            ExcelUtils.SafeGet(() => Convert.ToString(ComLateBinding.GetProp(rangeObj, "NumberFormat")), string.Empty))
                    };

                    /* ================= Borders ================= */

                    for (int i = 5; i < 13; i++)
                    {
                        object border = com.Track(ExcelUtils.SafeGet<object>(() => ComLateBinding.GetProp(rangeBorders, "Item", i), null));
                        if (border == null)
                            continue;

                        var borderObj = new Border
                        {
                            Index = i,
                            Color = ExcelUtils.SafeGet(() => Convert.ToInt32(ComLateBinding.GetProp(border, "Color")), 0),
                            ColorIndex = ExcelUtils.SafeGet(() => Convert.ToInt32(ComLateBinding.GetProp(border, "ColorIndex")), -4142),
                            LineStyle = ExcelUtils.SafeGet(() => Convert.ToInt32(ComLateBinding.GetProp(border, "LineStyle")), -4142),
                            Weight = ExcelUtils.SafeGet(() => Convert.ToInt32(ComLateBinding.GetProp(border, "Weight")), 0),
                            TintAndShade = ExcelUtils.SafeGet(() => Convert.ToString(ComLateBinding.GetProp(border, "TintAndShade")), string.Empty)
                        };

                        if (borderObj.Color > 0)
                        {
                            borderObj.RGB_Color =
                                ExcelUtils.ConvertNumberColor(borderObj.Color);
                        }
                        else
                        {
                            borderObj.RGB_Color = "";
                        }

                        cellFormatParams.Borders[i - 5] = borderObj;
                    }

                    return JsonConvert.SerializeObject(cellFormatParams, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"GetCellFormatEx 获取单元格格式异常：{ex}");
                return string.Empty;
            }
        }

        #endregion

        #region 选择区域地址
        private string GetRangeAddress(object sheet, object range)
        {
            return Convert.ToString(ComLateBinding.GetProp(range, "Address"));
        }
        #endregion

        #region 透视表规则
        private object GetPivotTableRule(object worksheet, object currentSelection)
        {
            try
            {
                object table = ComLateBinding.GetProp(currentSelection, "PivotTable");
                // 创建 PivotTableRule 对象
                PivotTableRule pivotTable = new PivotTableRule();

                // 从 table 对象中读取相同属性的值并赋值
                pivotTable.SheetName = ComLateBinding.GetProp(worksheet, "Name");
                pivotTable.Allocation = GetPivotProperty(table, "Allocation");
                pivotTable.AllocationMethod = GetPivotProperty(table, "AllocationMethod");
                pivotTable.AllocationValue = GetPivotProperty(table, "AllocationValue");
                pivotTable.AllocationWeightExpression = GetPivotProperty(table, "AllocationWeightExpression");
                pivotTable.AllowMultipleFilters = GetPivotProperty(table, "AllowMultipleFilters");
                pivotTable.AlternativeText = GetPivotProperty(table, "AlternativeText");
                pivotTable.CacheIndex = GetPivotProperty(table, "CacheIndex");
                pivotTable.CalculatedMembersInFilters = GetPivotProperty(table, "CalculatedMembersInFilters");
                pivotTable.ColumnGrand = GetPivotProperty(table, "ColumnGrand");
                pivotTable.CompactLayoutColumnHeader = GetPivotProperty(table, "CompactLayoutColumnHeader");
                pivotTable.CompactLayoutRowHeader = GetPivotProperty(table, "CompactLayoutRowHeader");
                pivotTable.CompactRowIndent = GetPivotProperty(table, "CompactRowIndent");
                pivotTable.DisplayContextTooltips = GetPivotProperty(table, "DisplayContextTooltips");
                pivotTable.DisplayEmptyColumn = GetPivotProperty(table, "DisplayEmptyColumn");
                pivotTable.DisplayEmptyRow = GetPivotProperty(table, "DisplayEmptyRow");
                pivotTable.DisplayErrorString = GetPivotProperty(table, "DisplayErrorString");
                pivotTable.DisplayFieldCaptions = GetPivotProperty(table, "DisplayFieldCaptions");
                pivotTable.DisplayImmediateItems = GetPivotProperty(table, "DisplayImmediateItems");
                pivotTable.DisplayMemberPropertyTooltips = GetPivotProperty(table, "DisplayMemberPropertyTooltips");
                pivotTable.DisplayNullString = GetPivotProperty(table, "DisplayNullString");
                pivotTable.EnableDataValueEditing = GetPivotProperty(table, "EnableDataValueEditing");
                pivotTable.EnableDrilldown = GetPivotProperty(table, "EnableDrilldown");
                pivotTable.EnableFieldDialog = GetPivotProperty(table, "EnableFieldDialog");
                pivotTable.EnableFieldList = GetPivotProperty(table, "EnableFieldList");
                pivotTable.EnableWizard = GetPivotProperty(table, "EnableWizard");
                pivotTable.EnableWriteback = GetPivotProperty(table, "EnableWriteback");
                pivotTable.ErrorString = GetPivotProperty(table, "ErrorString");
                pivotTable.FieldListSortAscending = GetPivotProperty(table, "FieldListSortAscending");
                pivotTable.GrandTotalName = GetPivotProperty(table, "GrandTotalName");
                pivotTable.HasAutoFormat = GetPivotProperty(table, "HasAutoFormat");
                pivotTable.InGridDropZones = GetPivotProperty(table, "InGridDropZones");
                pivotTable.InnerDetail = GetPivotProperty(table, "InnerDetail");
                pivotTable.LayoutRowDefault = GetPivotProperty(table, "LayoutRowDefault");
                pivotTable.Location = GetPivotProperty(table, "Location");
                pivotTable.ManualUpdate = GetPivotProperty(table, "ManualUpdate");
                pivotTable.MergeLabels = GetPivotProperty(table, "MergeLabels");
                pivotTable.Name = GetPivotProperty(table, "Name");
                pivotTable.NullString = GetPivotProperty(table, "NullString");
                pivotTable.PageFieldOrder = GetPivotProperty(table, "PageFieldOrder");
                pivotTable.PageFieldStyle = GetPivotProperty(table, "PageFieldStyle");
                pivotTable.PageFieldWrapCount = GetPivotProperty(table, "PageFieldWrapCount");
                pivotTable.PivotSelection = GetPivotProperty(table, "PivotSelection");
                pivotTable.PivotSelectionStandard = GetPivotProperty(table, "PivotSelectionStandard");
                pivotTable.PreserveFormatting = GetPivotProperty(table, "PreserveFormatting");
                pivotTable.PrintDrillIndicators = GetPivotProperty(table, "PrintDrillIndicators");
                pivotTable.PrintTitles = GetPivotProperty(table, "PrintTitles");
                pivotTable.RepeatItemsOnEachPrintedPage = GetPivotProperty(table, "RepeatItemsOnEachPrintedPage");
                pivotTable.RowGrand = GetPivotProperty(table, "RowGrand");
                pivotTable.SaveData = GetPivotProperty(table, "SaveData");
                pivotTable.SelectionMode = GetPivotProperty(table, "SelectionMode");
                pivotTable.ShowCellBackgroundFromOLAP = GetPivotProperty(table, "ShowCellBackgroundFromOLAP");
                pivotTable.ShowDrillIndicators = GetPivotProperty(table, "ShowDrillIndicators");
                pivotTable.ShowPageMultipleItemLabel = GetPivotProperty(table, "ShowPageMultipleItemLabel");
                pivotTable.ShowTableStyleColumnHeaders = GetPivotProperty(table, "ShowTableStyleColumnHeaders");
                pivotTable.ShowTableStyleColumnStripes = GetPivotProperty(table, "ShowTableStyleColumnStripes");
                pivotTable.ShowTableStyleRowHeaders = GetPivotProperty(table, "ShowTableStyleRowHeaders");
                pivotTable.ShowTableStyleRowStripes = GetPivotProperty(table, "ShowTableStyleRowStripes");
                pivotTable.ShowValuesRow = GetPivotProperty(table, "ShowValuesRow");
                pivotTable.SmallGrid = GetPivotProperty(table, "SmallGrid");
                pivotTable.SortUsingCustomLists = GetPivotProperty(table, "SortUsingCustomLists");
                pivotTable.SubtotalHiddenPageItems = GetPivotProperty(table, "SubtotalHiddenPageItems");
                // pivotTable.TableStyle = GetPivotProperty(table, "TableStyle");
                object tableStyle2 = GetPivotProperty(table, "TableStyle2");
                pivotTable.TableStyle2 = tableStyle2 == null ? null : ComLateBinding.GetProp(tableStyle2, "Name");
                pivotTable.Tag = GetPivotProperty(table, "Tag");
                pivotTable.TotalsAnnotation = GetPivotProperty(table, "TotalsAnnotation");
                pivotTable.VacatedStyle = GetPivotProperty(table, "VacatedStyle");
                pivotTable.Value = GetPivotProperty(table, "Value");
                pivotTable.ViewCalculatedMembers = GetPivotProperty(table, "ViewCalculatedMembers");
                pivotTable.VisualTotals = GetPivotProperty(table, "VisualTotals");
                pivotTable.VisualTotalsForSets = GetPivotProperty(table, "VisualTotalsForSets");
                string[] _source = Convert.ToString(ComLateBinding.GetProp(table, "SourceData")).Split('!');

                using (var com = new ComScope())
                {
                    object app = com.Track(ComLateBinding.GetProp(worksheet, "Application"));
                    object a1Style = ComLateBinding.Invoke(app, "ConvertFormula", _source[1], -4150, 1);
                    pivotTable.SourceData = $"{_source[0]}!{a1Style}";

                    object pc = com.Track(ComLateBinding.Invoke(table, "PivotCache"));
                    pivotTable.SourceType = Convert.ToInt32(ComLateBinding.GetProp(pc, "SourceType"));

                    object tr1 = com.Track(ComLateBinding.GetProp(table, "TableRange1"));
                    pivotTable.TableDestination = ComLateBinding.GetProp(tr1, "Address");

                    object pivotFields = com.Track(ComLateBinding.Invoke(table, "PivotFields"));
                    pivotTable.Fields = new List<TableField>();
                    object dataFields = com.Track(ComLateBinding.GetProp(table, "DataFields"));
                    foreach (object field in ComLateBinding.ToEnumerable(pivotFields))
                    {
                        com.Track(field);
                        if (Convert.ToInt32(GetPivotFieldValue(field, "Orientation") ?? 0) != 0)
                        {
                            TableField tableField = new TableField();
                            tableField.SourceName = GetPivotFieldValue(field, "SourceName");
                            tableField.Name = GetPivotFieldValue(field, "Name");
                            tableField.Caption = GetPivotFieldValue(field, "Caption");
                            tableField.Orientation = GetPivotFieldValue(field, "Orientation");
                            tableField.NumberFromat = GetPivotFieldValue(field, "NumberFromat");
                            tableField.DatabaseSort = GetPivotFieldValue(field, "DatabaseSort");
                            tableField.DisplayAsTooltip = GetPivotFieldValue(field, "DisplayAsTooltip");
                            tableField.DisplayInReport = GetPivotFieldValue(field, "DisplayInReport");
                            tableField.DragToColumn = GetPivotFieldValue(field, "DragToColumn");
                            tableField.DragToData = GetPivotFieldValue(field, "DragToData");
                            tableField.DragToHide = GetPivotFieldValue(field, "DragToHide");
                            tableField.DragToPage = GetPivotFieldValue(field, "DragToPage");
                            tableField.DragToRow = GetPivotFieldValue(field, "DragToRow");
                            tableField.DrilledDown = GetPivotFieldValue(field, "DrilledDown");
                            tableField.EnableItemSelection = GetPivotFieldValue(field, "EnableItemSelection");
                            tableField.EnableMultiplePageItems = GetPivotFieldValue(field, "EnableMultiplePageItems");
                            tableField.Formula = GetPivotFieldValue(field, "Formula");
                            tableField.Function = GetPivotFieldValue(field, "Function");
                            tableField.Hidden = GetPivotFieldValue(field, "Hidden");
                            tableField.IncludeNewItemsInFilter = GetPivotFieldValue(field, "IncludeNewItemsInFilter");
                            tableField.LayoutBlankLine = GetPivotFieldValue(field, "LayoutBlankLine");
                            tableField.LayoutCompactRow = GetPivotFieldValue(field, "LayoutCompactRow");
                            tableField.LayoutForm = GetPivotFieldValue(field, "LayoutForm");
                            tableField.LayoutPageBreak = GetPivotFieldValue(field, "LayoutPageBreak");
                            tableField.LayoutSubtotalLocation = GetPivotFieldValue(field, "LayoutSubtotalLocation");
                            tableField.MemberPropertyCaption = GetPivotFieldValue(field, "MemberPropertyCaption");
                            tableField.Position = GetPivotFieldValue(field, "Position");
                            tableField.PropertyOrder = GetPivotFieldValue(field, "PropertyOrder");
                            tableField.RepeatLabels = GetPivotFieldValue(field, "RepeatLabels");
                            tableField.ServerBased = GetPivotFieldValue(field, "ServerBased");
                            tableField.ShowAllItems = GetPivotFieldValue(field, "ShowAllItems");
                            tableField.ShowDetail = GetPivotFieldValue(field, "ShowDetail");
                            tableField.StandardFormula = GetPivotFieldValue(field, "StandardFormula");
                            tableField.SubtotalName = GetPivotFieldValue(field, "SubtotalName");
                            tableField.UseMemberPropertyAsCaption = GetPivotFieldValue(field, "UseMemberPropertyAsCaption");
                            tableField.Value = GetPivotFieldValue(field, "Value");
                            try
                            {
                                var subtotals = JsonConvert.SerializeObject(ComLateBinding.GetProp(field, "Subtotals"));
                                tableField.Subtotals = JsonConvert.DeserializeObject<bool[]>(subtotals);
                            }
                            catch (Exception)
                            {
                                tableField.Subtotals = null;
                            }

                            pivotTable.Fields.Add(tableField);
                        }

                    }
                    foreach (object field in ComLateBinding.ToEnumerable(dataFields))
                    {
                        TableField tableField = new TableField();
                        tableField.SourceName = GetPivotFieldValue(field, "SourceName");
                        tableField.Name = GetPivotFieldValue(field, "Name");
                        tableField.Caption = GetPivotFieldValue(field, "Caption");
                        tableField.Orientation = 4;
                        tableField.NumberFromat = GetPivotFieldValue(field, "NumberFromat") ?? "General";
                        tableField.DatabaseSort = GetPivotFieldValue(field, "DatabaseSort");
                        tableField.DisplayAsTooltip = GetPivotFieldValue(field, "DisplayAsTooltip");
                        tableField.DisplayInReport = GetPivotFieldValue(field, "DisplayInReport");
                        tableField.DragToColumn = GetPivotFieldValue(field, "DragToColumn");
                        tableField.DragToData = GetPivotFieldValue(field, "DragToData");
                        tableField.DragToHide = GetPivotFieldValue(field, "DragToHide");
                        tableField.DragToPage = GetPivotFieldValue(field, "DragToPage");
                        tableField.DragToRow = GetPivotFieldValue(field, "DragToRow");
                        tableField.DrilledDown = GetPivotFieldValue(field, "DrilledDown");
                        tableField.EnableItemSelection = GetPivotFieldValue(field, "EnableItemSelection");
                        tableField.EnableMultiplePageItems = GetPivotFieldValue(field, "EnableMultiplePageItems");
                        tableField.Formula = GetPivotFieldValue(field, "Formula");
                        tableField.Function = GetPivotFieldValue(field, "Function");
                        tableField.Hidden = GetPivotFieldValue(field, "Hidden");
                        tableField.IncludeNewItemsInFilter = GetPivotFieldValue(field, "IncludeNewItemsInFilter");
                        tableField.LayoutBlankLine = GetPivotFieldValue(field, "LayoutBlankLine");
                        tableField.LayoutCompactRow = GetPivotFieldValue(field, "LayoutCompactRow");
                        tableField.LayoutForm = GetPivotFieldValue(field, "LayoutForm");
                        tableField.LayoutPageBreak = GetPivotFieldValue(field, "LayoutPageBreak");
                        tableField.LayoutSubtotalLocation = GetPivotFieldValue(field, "LayoutSubtotalLocation");
                        tableField.MemberPropertyCaption = GetPivotFieldValue(field, "MemberPropertyCaption");
                        tableField.Position = GetPivotFieldValue(field, "Position");
                        tableField.PropertyOrder = GetPivotFieldValue(field, "PropertyOrder");
                        tableField.RepeatLabels = GetPivotFieldValue(field, "RepeatLabels");
                        tableField.ServerBased = GetPivotFieldValue(field, "ServerBased");
                        tableField.ShowAllItems = GetPivotFieldValue(field, "ShowAllItems");
                        tableField.ShowDetail = GetPivotFieldValue(field, "ShowDetail");
                        tableField.StandardFormula = GetPivotFieldValue(field, "StandardFormula");
                        tableField.SubtotalName = GetPivotFieldValue(field, "SubtotalName");
                        tableField.UseMemberPropertyAsCaption = GetPivotFieldValue(field, "UseMemberPropertyAsCaption");
                        tableField.Value = GetPivotFieldValue(field, "Value");
                        try
                        {
                            var subtotals = JsonConvert.SerializeObject(ComLateBinding.GetProp(field, "Subtotals"));
                            tableField.Subtotals = JsonConvert.DeserializeObject<bool[]>(subtotals);
                        }
                        catch (Exception)
                        {
                            tableField.Subtotals = null;
                        }
                        //https://learn.microsoft.com/en-us/office/vba/api/excel.xlpivotfieldorientation
                        if (Convert.ToInt32(GetPivotFieldValue(field, "Orientation") ?? 0) == 4)
                        {
                            tableField.BaseField = GetPivotFieldValue(field, "BaseField");
                            tableField.Calculation = GetPivotFieldValue(field, "Calculation");
                            tableField.CurrentPageName = GetPivotFieldValue(field, "CurrentPageName");
                        }
                        //GetPivotValue(tableField, filter);
                        pivotTable.Fields.Add(tableField);
                    }
                } // End ComScope

                return pivotTable;
            }
            catch (Exception)
            {

                return null;
            }

        }
        private object GetPivotProperty(object table, string name)
        {
            try
            {
                return ComLateBinding.GetProp(table, name.Trim());
            }
            catch (Exception ex)
            {
                logger.LogError($"Get Property Exception:{ex}");
                return null;
            }

        }
        private object GetPivotFieldValue(object field, string name)
        {
            try
            {
                return ComLateBinding.GetProp(field, name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex);
                return null;
            }

        }
        private object GetPivotItem(object field, string name)
        {
            try
            {
                return ComLateBinding.GetProp(field, name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex);
                return null;
            }

        }

        #endregion

        #region 排序规则
        private object GetSortRule(object sheet, object range)
        {
            using (var com = new ComScope())
            {
                object sort = com.Track(ComLateBinding.GetProp(sheet, "Sort"));
                object fields = com.Track(ComLateBinding.GetProp(sort, "SortFields"));
                int fieldCount = ComLateBinding.ToInt(ComLateBinding.GetProp(fields, "Count"));
                if (fieldCount > 0)
                {
                    object rng = com.Track(ComLateBinding.GetProp(sort, "Rng"));
                    SortRule rule = new SortRule
                    {
                        Header = ComLateBinding.GetProp(sort, "Header"),
                        Range = ExcelUtils.GetExcelRange(Convert.ToString(ComLateBinding.GetProp(rng, "Address"))),
                        MatchCase = ComLateBinding.GetProp(sort, "MatchCase"),
                        Orientation = ComLateBinding.GetProp(sort, "Orientation"),
                        SortMethod = ComLateBinding.GetProp(sort, "SortMethod"),
                        SortFields = new CustomField[fieldCount]
                    };
                    for (int i = 0; i < fieldCount; i++)
                    {
                        object field = com.Track(ComLateBinding.GetItem(fields, i + 1));
                        object fKey = com.Track(ComLateBinding.GetProp(field, "Key"));

                        rule.SortFields[i] = new CustomField
                        {
                            CustomOrder = ComLateBinding.GetProp(field, "CustomOrder"),
                            DataOption = ComLateBinding.GetProp(field, "DataOption"),
                            Key = ComLateBinding.ToInt(ComLateBinding.GetProp(sort, "Orientation")) == 1
                                ? ComLateBinding.GetProp(fKey, "Column")
                                : ComLateBinding.GetProp(fKey, "Row"),
                            Order = ComLateBinding.GetProp(field, "Order"),
                            Priority = ComLateBinding.GetProp(field, "Priority"),
                            SortOn = ComLateBinding.GetProp(field, "SortOn")
                        };
                        try
                        {
                            object sortOnValue = com.Track(ComLateBinding.GetProp(field, "SortOnValue"));
                            rule.SortFields[i].SortOnValue = new SortOnValue();
                            rule.SortFields[i].SortOnValue.Color = ComLateBinding.GetProp(sortOnValue, "Color");
                            if (Convert.ToInt32(rule.SortFields[i].SortOn) == 1)
                            {
                                rule.SortFields[i].SortOnValue.Pattern = ComLateBinding.GetProp(sortOnValue, "Pattern");
                            }
                            if (Convert.ToInt32(rule.SortFields[i].SortOn) == 2)
                            {
                                rule.SortFields[i].SortOnValue.ColorIndex = ComLateBinding.GetProp(sortOnValue, "ColorIndex");
                            }

                        }
                        catch
                        {
                            continue;
                        }

                    }
                    return rule;
                }
                return null;
            }
        }
        #endregion

        #region 筛选规则
        private object GetFilterRule(object sheet, object range)
        {
            using (var com = new ComScope())
            {
                int inTable = 0;
                object filterObject = null;

                object autoFilter = com.Track(ExcelUtils.SafeGet<object>(() => ComLateBinding.GetProp(sheet, "AutoFilter"), null));
                if (autoFilter != null)
                {
                    inTable = 0;
                    filterObject = autoFilter;
                }
                else
                {
                    object listObject = com.Track(ExcelUtils.SafeGet<object>(() => ComLateBinding.GetProp(range, "ListObject"), null));
                    if (listObject != null)
                    {
                        object listAutoFilter = com.Track(ExcelUtils.SafeGet<object>(() => ComLateBinding.GetProp(listObject, "AutoFilter"), null));
                        if (listAutoFilter != null)
                        {
                            try
                            {
                                object listObjects = com.Track(ComLateBinding.GetProp(sheet, "ListObjects"));
                                int listObjectCount = ComLateBinding.ToInt(ComLateBinding.GetProp(listObjects, "Count"));
                                for (int i = 1; i <= listObjectCount; i++)
                                {
                                    object table = com.Track(ComLateBinding.GetItem(listObjects, i));
                                    if (Convert.ToString(ComLateBinding.GetProp(table, "Name")) == Convert.ToString(ComLateBinding.GetProp(listObject, "Name")))
                                    {
                                        inTable = i;
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning($"获取表格筛选对象索引异常：{ex.Message}");
                                inTable = 1;
                            }

                            filterObject = listAutoFilter;
                        }
                    }
                }

                if (filterObject == null)
                {
                    logger.LogWarning("当前选择区域未启用筛选功能");
                    return null;
                }

                object filters = com.Track(ComLateBinding.GetProp(filterObject, "Filters"));
                int filterCount = ComLateBinding.ToInt(ComLateBinding.GetProp(filters, "Count"));
                if (filterCount > 0)
                {
                    object excelRange = com.Track(ComLateBinding.GetProp(filterObject, "Range"));
                    object sort = com.Track(ComLateBinding.GetProp(filterObject, "Sort"));
                    object filterCols = com.Track(ComLateBinding.GetProp(excelRange, "Columns"));
                    object filterRows = com.Track(ComLateBinding.GetProp(excelRange, "Rows"));
                    int rangeColumn = ComLateBinding.ToInt(ComLateBinding.GetProp(excelRange, "Column"));
                    int rangeRow = ComLateBinding.ToInt(ComLateBinding.GetProp(excelRange, "Row"));
                    AutoFilterRule rule = new AutoFilterRule
                    {
                        InTable = inTable,
                        Range = ComLateBinding.GetProp(excelRange, "Address"),
                        StartColumn = ExcelUtils.ColumnIndexToLetter(rangeColumn),
                        EndColumn = ExcelUtils.ColumnIndexToLetter(rangeColumn + ComLateBinding.ToInt(ComLateBinding.GetProp(filterCols, "Count")) - 1),
                        StartRow = rangeRow,
                        EndRow = rangeRow == 1 ? -1 : rangeRow + ComLateBinding.ToInt(ComLateBinding.GetProp(filterRows, "Count")) - 1,
                        SortMethod = sort == null ? null : ComLateBinding.GetProp(sort, "SortMethod"),
                        FilterMode = ComLateBinding.GetProp(filterObject, "FilterMode"),
                        Filters = new ExcelFilters[filterCount]
                    };
                    for (int i = 0; i < filterCount; i++)
                    {
                        object filter = com.Track(ComLateBinding.GetItem(filters, i + 1));
                        rule.Filters[i] = new ExcelFilters();
                        rule.Filters[i].Criteria1 = GetExcelProperty(filter, "Criteria1");
                        rule.Filters[i].Criteria2 = GetExcelProperty(filter, "Criteria2");
                        rule.Filters[i].On = GetExcelProperty(filter, "On");
                        rule.Filters[i].Operator = GetExcelProperty(filter, "Operator");
                    }
                    return rule;
                }
                return null;
            }
        }
        #endregion 
    }
}
