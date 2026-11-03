namespace II.RPA.Plugin.Core.CompareMode
{
    public interface ICompareMode<T>
    {
       bool Compare(T fromObj,T toObj);
    }
}
