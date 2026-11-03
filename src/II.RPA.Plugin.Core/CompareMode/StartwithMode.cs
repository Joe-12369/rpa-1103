namespace II.RPA.Plugin.Core.CompareMode
{
    public class StartwithMode : ICompareMode<string>
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
            return fromObj.StartsWith(toObj);
        }
    }
}
