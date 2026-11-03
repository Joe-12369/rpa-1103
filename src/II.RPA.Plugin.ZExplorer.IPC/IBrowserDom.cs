using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.ZExplorer.IPC
{
    public interface IBrowserDom
    {
        string Ping();

        string ElementFromPoint(string param);

        string ElementBounding(string param);

        string GetHtml(string param);

        string SetAttribute(string param);

        string GetProperty(string param);

        string ExecuteScript(string param);

        string IsDisplayed(string param);

        string GetAttribute(string param);

        string SetSelectOptions(string param);

        string GetSelectOption(string param);

        string GetSelectOptions(string param);
        string GetChildren(string param);
        string GetCheckStatus(string param);

        string SetCheckStatus(string param);

        void Focus(string param);

        string IsEditable(string v, string param);

        string BuildSelector(string param);

        void Input(string param);

        void RightClick(string param);

        void ScrollIntoViewIfNeeded(string param);

        void Click(string param);

        void DoubleClick(string param);

        string GetText(string param);

        string QuerySelector(string param);        
    }
}
