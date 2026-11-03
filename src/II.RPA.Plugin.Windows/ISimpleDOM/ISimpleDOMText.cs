using System.Runtime.InteropServices;

namespace ISimpleDOM
{
    [Guid("4E747BE5-2052-4265-8AF0-8ECAD7AAD1C0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISimpleDOMText
    {
        int get_domText(out string domText);
        int get_clippedSubstringBounds(uint startIndex, uint endIndex, out int x, out int y, out int width, out int height);
        int get_unclippedSubstringBounds(uint startIndex, uint endIndex, out int x, out int y, out int width, out int height);
        int scrollToSubstring(uint startIndex, uint endIndex);
        int get_fontFamily(out string fontFamily);

        [DispId(1610678272)]
        string domText { get; }
        [DispId(1610678276)]
        string fontFamily { get; }
    }
}