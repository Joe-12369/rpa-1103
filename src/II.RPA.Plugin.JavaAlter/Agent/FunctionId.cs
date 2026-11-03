using System;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    internal enum FunctionId
    {
        FuncGetWindows = 0,
        FuncPing = 1,
        FuncGetVersion = 16,
        FuncGetWindow = 2,
        FuncGetApplet=3,
        FuncGetChildren=4,
        FuncGetProperty=5,
        FuncSetProperty=6,
        FuncGetSpecialProperty=7,
        FuncInvoke=8,
        FuncInvokeSpecial=9,
        FuncGetChildItems=10,
        FuncGetItemProperty=11,
        FuncGetItemSpecialProperty=12,
        FuncSetItemSpecialProperty=13,
        FuncGetPropertyAndMethodNames=14,
        FuncGetField=15,

        FuncGetElementByPoint = 99,
        FuncGetElementXpath = 98,
        FuncQueryElementFromXpath = 97,
        FuncTriggerEvent=96
    }
}
