namespace II.RPA.Plugin.Core.CompareMode
{
    public class ContainsMode : ICompareMode<string>
    {
        public bool Compare(string fromObj, string toObj)
        {
            if (string.IsNullOrEmpty(fromObj) && string.IsNullOrEmpty(toObj))
            {
                return false;
            }
            if (string.IsNullOrEmpty(fromObj) || string.IsNullOrEmpty(toObj))
            {
                return false;
            }
            return fromObj.Contains(toObj);
        }
    }
}
