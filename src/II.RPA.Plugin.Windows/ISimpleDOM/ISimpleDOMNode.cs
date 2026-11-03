using System.Runtime.InteropServices;

namespace ISimpleDOM
{
    class ISimpleDOMNode_Const
    {
        const ushort NODETYPE_ELEMENT = 1;
        const ushort NODETYPE_ATTRIBUTE = 2;
        const ushort NODETYPE_TEXT = 3;
        const ushort NODETYPE_CDATA_SECTION = 4;
        const ushort NODETYPE_ENTITY_REFERENCE = 5;
        const ushort NODETYPE_ENTITY = 6;
        const ushort NODETYPE_PROCESSING_INSTRUCTION = 7;
        const ushort NODETYPE_COMMENT = 8;
        const ushort NODETYPE_DOCUMENT = 9;
        const ushort NODETYPE_DOCUMENT_TYPE = 10;
        const ushort NODETYPE_DOCUMENT_FRAGMENT = 11;
        const ushort NODETYPE_NOTATION = 12;
    }

    [Guid("1814CEEB-49E2-407F-AF99-FA755A7D2607")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISimpleDOMNode
    {
        int get_nodeInfo(out string nodeName, out short nameSpaceID, out string nodeValue, out uint numChildren, out uint uniqueID, out ushort nodeType);
        int get_attributes(ushort maxAttribs, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 0)] string[] attribNames, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I2, SizeParamIndex = 0)] short[] nameSpaceID, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 0)] string[] attribValues, out ushort numAttribs);
        int get_attributesForNames(ushort numAttribs, [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 0)] string[] attribNames, [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I2, SizeParamIndex = 0)] short[] nameSpaceID, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 0)] string[] attribValues);
        int get_computedStyle(ushort maxStyleProperties, sbyte useAlternateView, out string styleProperties, out string styleValues, out ushort numStyleProperties);
        int get_computedStyleForProperties(ushort numStyleProperties, sbyte useAlternateView, ref string styleProperties, out string styleValues);
        int scrollTo(sbyte placeTopLeft);
        int get_parentNode(out ISimpleDOMNode node);
        int get_firstChild(out ISimpleDOMNode node);
        int get_lastChild(out ISimpleDOMNode node);
        int get_previousSibling(out ISimpleDOMNode node);
        int get_nextSibling(out ISimpleDOMNode node);
        int get_childAt(uint childIndex, out ISimpleDOMNode node);
        int get_innerHTML(out string innerHTML);
        int get_remoteLocalInterface(out object localInterface);
        int get_language(out string language);

        [DispId(1610678278)]
        ISimpleDOMNode parentNode { get; }
        [DispId(1610678279)]
        ISimpleDOMNode firstChild { get; }
        [DispId(1610678280)]
        ISimpleDOMNode lastChild { get; }
        [DispId(1610678281)]
        ISimpleDOMNode previousSibling { get; }
        [DispId(1610678282)]
        ISimpleDOMNode nextSibling { get; }
        [DispId(1610678284)]
        string innerHTML { get; }
        [DispId(1610678285)]
        dynamic remoteLocalInterface { get; }
        [DispId(1610678286)]
        string language { get; }
    }
}