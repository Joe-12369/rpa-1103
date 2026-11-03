using System;
using System.Runtime.InteropServices;

namespace ISimpleDOM
{
    [ComConversionLoss]
    [Guid("0D68D6D0-D93D-4D08-A30D-F00DD1F45B24")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISimpleDOMDocument
    {
        int get_URL(out string url);
        int get_title(out string title);
        int get_mimeType(out string mimeType);
        int get_docType(out string docType);
        int get_nameSpaceURIForID(short nameSpaceID, out string nameSpaceURI);
        int set_alternateViewMediaTypes(ref string value);

        [DispId(1610678272)]
        string URL { get; }
        [DispId(1610678273)]
        string title { get; }
        [DispId(1610678274)]
        string mimeType { get; }
        [DispId(1610678275)]
        string docType { get; }
        [DispId(1610678277)]
        IntPtr alternateViewMediaTypes { set; }
    }
}