using System.Collections.Generic;

namespace II.RPA.Plugin.Excel.Models
{
    internal class TableField
    {
        public dynamic Name { get; set; }
        public dynamic SourceName { get; set; }
        public dynamic Caption { get; set; }
        public bool[] Subtotals { get; set; }
        public dynamic Orientation { get; set; }
        public dynamic NumberFromat { get; set; }

        //data field
        public dynamic BaseField { get; set; }
        //datafield
        public dynamic Calculation { get; set; }
        //datafield 
        public dynamic CurrentPageName { get; set; }

        public dynamic DatabaseSort { get; set; }
        public dynamic DisplayAsTooltip { get; set; }
        public dynamic DisplayInReport { get; set; }
        public dynamic DragToColumn { get; set; }
        public dynamic DragToData { get; set; }
        public dynamic DragToHide { get; set; }
        public dynamic DragToPage { get; set; }
        public dynamic DragToRow { get; set; }
        public dynamic DrilledDown { get; set; }
        public dynamic EnableItemSelection { get; set; }
        public dynamic EnableMultiplePageItems { get; set; }
        public dynamic Formula { get; set; }
        public dynamic Function { get; set; }
        public dynamic Hidden { get; set; }
        public dynamic IncludeNewItemsInFilter { get; set; }
        public dynamic LayoutBlankLine { get; set; }
        public dynamic LayoutCompactRow { get; set; }
        public dynamic LayoutForm { get; set; }
        public dynamic LayoutPageBreak { get; set; }
        public dynamic LayoutSubtotalLocation { get; set; }
        public dynamic MemberPropertyCaption { get; set; }
        public dynamic Position { get; set; }
        public dynamic PropertyOrder { get; set; }
        public dynamic RepeatLabels { get; set; }
        public dynamic ServerBased { get; set; }
        public dynamic ShowAllItems { get; set; }
        public dynamic ShowDetail { get; set; }
        public dynamic StandardFormula { get; set; }
        public dynamic SubtotalName { get; set; }
        public dynamic UseMemberPropertyAsCaption { get; set; }
        public dynamic Value { get; set; }
        public TableFieldItem[] FieldItem { get; set; }
    }
    internal class TableFieldItem
    {
        public dynamic Caption { get; set; }
        public TableFieldItem ChildItems { get; set; }
        public dynamic Createor { get; set; }
        public dynamic DataRange { get; set; }
        public dynamic DrilledDown { get; set; }
        public dynamic Formula { get; set; }
        public dynamic IsCalculated { get; set; }
        public dynamic LabelRange { get; set; }
        public dynamic Name { get; set; }
        public dynamic Parent { get; set; }
        public dynamic ParentItem { get; set; }
        public dynamic ParentShowDetail { get; set; }
        public dynamic Position { get; set; }
        public dynamic RecordCount { get; set; }
        public dynamic ShowDetail { get; set; }
        public dynamic SourceName { get; set; }
        public dynamic SourceNameStandard { get; set; }
        public dynamic StandardFormula { get; set; }
        public dynamic Value { get; set; }
        public dynamic Visible { get; set; }
    }
    /// <summary>
    /// 透视表规则
    /// </summary>
    internal class PivotTableRule
    {
        public dynamic Allocation { get; set; }
        public dynamic AllocationMethod { get; set; }
        public dynamic AllocationValue { get; set; }
        public dynamic AllocationWeightExpression { get; set; }
        public dynamic AllowMultipleFilters { get; set; }
        public dynamic AlternativeText { get; set; }
        public dynamic CacheIndex { get; set; }
        public dynamic CalculatedMembersInFilters { get; set; }
        public dynamic ColumnGrand { get; set; }
        public dynamic CompactLayoutColumnHeader { get; set; }
        public dynamic CompactLayoutRowHeader { get; set; }
        public dynamic CompactRowIndent { get; set; }
        public dynamic DisplayContextTooltips { get; set; }
        public dynamic DisplayEmptyColumn { get; set; }
        public dynamic DisplayEmptyRow { get; set; }
        public dynamic DisplayErrorString { get; set; }
        public dynamic DisplayFieldCaptions { get; set; }
        public dynamic DisplayImmediateItems { get; set; }
        public dynamic DisplayMemberPropertyTooltips { get; set; }
        public dynamic DisplayNullString { get; set; }
        public dynamic EnableDataValueEditing { get; set; }
        public dynamic EnableDrilldown { get; set; }
        public dynamic EnableFieldDialog { get; set; }
        public dynamic EnableFieldList { get; set; }
        public dynamic EnableWizard { get; set; }
        public dynamic EnableWriteback { get; set; }
        public dynamic ErrorString { get; set; }
        public dynamic FieldListSortAscending { get; set; }
        public dynamic GrandTotalName { get; set; }
        public dynamic HasAutoFormat { get; set; }
        public dynamic InGridDropZones { get; set; }
        public dynamic InnerDetail { get; set; }
        public dynamic LayoutRowDefault { get; set; }
        public dynamic Location { get; set; }
        public dynamic ManualUpdate { get; set; }
        public dynamic MergeLabels { get; set; }
        public dynamic Name { get; set; }
        public dynamic SheetName { get; set; }
        public dynamic NullString { get; set; }
        public dynamic PageFieldOrder { get; set; }
        public dynamic PageFieldStyle { get; set; }
        public dynamic PageFieldWrapCount { get; set; }
        public dynamic PivotSelection { get; set; }
        public dynamic PivotSelectionStandard { get; set; }
        public dynamic PreserveFormatting { get; set; }
        public dynamic PrintDrillIndicators { get; set; }
        public dynamic PrintTitles { get; set; }
        public dynamic RepeatItemsOnEachPrintedPage { get; set; }
        public dynamic RowGrand { get; set; }
        public dynamic SaveData { get; set; }
        public dynamic SelectionMode { get; set; }
        public dynamic ShowCellBackgroundFromOLAP { get; set; }
        public dynamic ShowDrillIndicators { get; set; }
        public dynamic ShowPageMultipleItemLabel { get; set; }
        public dynamic ShowTableStyleColumnHeaders { get; set; }
        public dynamic ShowTableStyleColumnStripes { get; set; }
        public dynamic ShowTableStyleRowHeaders { get; set; }
        public dynamic ShowTableStyleRowStripes { get; set; }
        public dynamic ShowValuesRow { get; set; }
        public dynamic SmallGrid { get; set; }
        public dynamic SourceData { get; set; }
        public dynamic SourceType { get; set; }
        public dynamic TableDestination { get; set; }
        public dynamic Version { get; set; }
        public dynamic SortUsingCustomLists { get; set; }
        public dynamic SubtotalHiddenPageItems { get; set; }
        public dynamic TableStyle { get; set; }
        public dynamic TableStyle2 { get; set; }
        public dynamic Tag { get; set; }
        public dynamic TotalsAnnotation { get; set; }
        public dynamic VacatedStyle { get; set; }
        public dynamic Value { get; set; }
        public dynamic ViewCalculatedMembers { get; set; }
        public dynamic VisualTotals { get; set; }
        public dynamic VisualTotalsForSets { get; set; }
        public List<TableField> Fields { get; set; }

    }
}
