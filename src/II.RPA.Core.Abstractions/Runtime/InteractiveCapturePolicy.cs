namespace II.RPA.Core.Runtime;

/// <summary>Identifies operations that wait for the user to finish or cancel a capture.</summary>
public static class InteractiveCapturePolicy
{
    public static bool IsClientAction(string? action) => action is
        "uiselector" or "comboSelectUi" or "agentSelectUi" or "cvselectUi" or
        "uiselectorOcr" or "makeOcrTableSelector" or "screenshotSelectUi" or
        "captureSimilarElements" or "captureRenovateElements" or "CaptureWindow" or
        "mouseRecord" or "mouseRecordPhone" or "phoneSelector" or "phoneSelectorCv";

    public static bool IsPluginAction(string? plugin, string? action) =>
        action == "Excute" && plugin is (
            "Element.Capture" or "Element.CaptureForAgent" or "Element.CaptureByRegion" or
            "Element.CaptureCV" or "Element.CaptureSimilar" or "Element.CaptureRenovate" or
            "Element.CaptureWindow" or "Element.CaptureScreen" or "Element.CapturePosition" or
            "Element.DeepCapture" or "OcrOffline.MakeOcrTableSelector" or
            "Mobile.GetMobileElementInfo" or "Mobile.GetMobileElementCV" or
            "Mobile.GetMobileSimilarElementInfo" or "Mobile.GetMobileElementPoint") ||
        plugin == "Mouse.MouseMove" && action == "Config";
}
