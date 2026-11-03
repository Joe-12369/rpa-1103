namespace II.RPA.Plugin.Core.CompareMode
{
    public class SmallerMode : ICompareMode<decimal>
    {
        public bool Compare(decimal fromObj, decimal toObj)
        {
            return fromObj < toObj;
        }
    }
}
