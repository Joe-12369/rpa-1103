using System;
using System.Runtime.InteropServices;

namespace UIAutomationInterop
{
    [ComImport]
    [Guid("8609c4ec-4a1a-4d88-a357-5a66e060e1cf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IUIAutomationRegistrar
    {
        int RegisterProperty(ref UIAutomationPropertyInfo property, out int propertyId);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UIAutomationPropertyInfo
    {
        public Guid guid;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pProgrammaticName;
        public UIAutomationType type;
    }

    public enum UIAutomationType
    {
        UIAutomationType_Int = 0x1,
        UIAutomationType_Bool = 0x2,
        UIAutomationType_String = 0x3,
        UIAutomationType_Double = 0x4,
        UIAutomationType_Point = 0x5,
        UIAutomationType_Rect = 0x6,
        UIAutomationType_Element = 0x7,
        UIAutomationType_Array = 0x10000,
        UIAutomationType_Out = 0x20000,
        UIAutomationType_IntArray = (UIAutomationType_Int | UIAutomationType_Array),
        UIAutomationType_BoolArray = (UIAutomationType_Bool | UIAutomationType_Array),
        UIAutomationType_StringArray = (UIAutomationType_String | UIAutomationType_Array),
        UIAutomationType_DoubleArray = (UIAutomationType_Double | UIAutomationType_Array),
        UIAutomationType_PointArray = (UIAutomationType_Point | UIAutomationType_Array),
        UIAutomationType_RectArray = (UIAutomationType_Rect | UIAutomationType_Array),
        UIAutomationType_ElementArray = (UIAutomationType_Element | UIAutomationType_Array),
        UIAutomationType_OutInt = (UIAutomationType_Int | UIAutomationType_Out),
        UIAutomationType_OutBool = (UIAutomationType_Bool | UIAutomationType_Out),
        UIAutomationType_OutString = (UIAutomationType_String | UIAutomationType_Out),
        UIAutomationType_OutDouble = (UIAutomationType_Double | UIAutomationType_Out),
        UIAutomationType_OutPoint = (UIAutomationType_Point | UIAutomationType_Out),
        UIAutomationType_OutRect = (UIAutomationType_Rect | UIAutomationType_Out),
        UIAutomationType_OutElement = (UIAutomationType_Element | UIAutomationType_Out),
        UIAutomationType_OutIntArray = ((UIAutomationType_Int | UIAutomationType_Array) | UIAutomationType_Out),
        UIAutomationType_OutBoolArray = ((UIAutomationType_Bool | UIAutomationType_Array) | UIAutomationType_Out),
        UIAutomationType_OutStringArray = ((UIAutomationType_String | UIAutomationType_Array) | UIAutomationType_Out),
        UIAutomationType_OutDoubleArray = ((UIAutomationType_Double | UIAutomationType_Array) | UIAutomationType_Out),
        UIAutomationType_OutPointArray = ((UIAutomationType_Point | UIAutomationType_Array) | UIAutomationType_Out),
        UIAutomationType_OutRectArray = ((UIAutomationType_Rect | UIAutomationType_Array) | UIAutomationType_Out),
        UIAutomationType_OutElementArray = ((UIAutomationType_Element | UIAutomationType_Array) | UIAutomationType_Out)
    }

    [ComImport]
    [Guid("6e29fabf-9977-42d1-8d0e-ca7e61ad87e6")]
    [ClassInterface(ClassInterfaceType.None)]
    public class CUIAutomationRegistrar
    {
    }
}