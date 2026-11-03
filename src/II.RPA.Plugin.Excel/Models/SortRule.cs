namespace II.RPA.Plugin.Excel.Models
{
    /// <summary>
    /// 排序规则
    /// </summary>
    internal class SortRule
    {
        public dynamic Header { get; set; }
        public dynamic Range { get; set; }
        public dynamic MatchCase { get; set; }
        public dynamic Orientation { get; set; }
        public dynamic SortMethod { get; set; }
        public CustomField[] SortFields { get; set; }
    }
    internal class CustomField
    {
        public dynamic CustomOrder { get; set; }
        public dynamic DataOption { get; set; }
        public dynamic Key { get; set; }
        public dynamic Order { get; set; }
        public dynamic Priority { get; set; }
        public dynamic SortOn { get; set; }
        public dynamic SortOnValue { get; set; }
    }
    internal class SortOnValue
    {
        public dynamic Color { get; set; }
        public dynamic Pattern { get; set; }
        public dynamic ColorIndex { get; set; }
    }
}
