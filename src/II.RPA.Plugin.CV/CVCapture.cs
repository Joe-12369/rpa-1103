using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;

namespace II.RPA.Plugin.CV
{
    class CVCapture
    {
        static WaitingWnd waitWnd;
        CVObjs cvObjs;
        public string captureTarget { get; set; }
        public string GetScreen()
        {
            var bmp = UiCommon.UIAAutomation.GetDesktop().Capture();
            CVScreenWnd wnd = new CVScreenWnd(bmp);
            string result = string.Empty;

            wnd.ShowDialog();
            captureTarget = wnd.FilePath;
            waitWnd = new WaitingWnd();
            Thread thread = new Thread(() =>
            {
                result = InspectByCV(wnd.FilePath);
                string id = System.IO.Path.GetFileNameWithoutExtension(wnd.FilePath);
                using (System.IO.FileStream fs = System.IO.File.Create(id + ".txt"))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(result);
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                }
                waitWnd.Dispatcher.Invoke(() =>
                {
                    waitWnd.Close();
                });
            });
            thread.Start();
            waitWnd.ShowDialog();
            return result;
        }
        private string InspectByCV(string filePath)
        {
            var img = Image.FromFile(filePath);
            string imgStr = ImageHelper.Bitmap2Base64(new Bitmap(img));

            HttpItem item = new HttpItem();
            item.URL = "http://192.168.1.135:12120/rpa_ocr_service/";
            item.Method = "POST";
            Payload pl = new Payload();
            pl.image = imgStr;
            pl.app = "企业微信";
            string js = JsonHelper.JsonByNewtonJson(pl);
            item.Postdata = js;
            item.Header.Add("CLIENT-USER", GlobleConfigurations.ClientUser);
            item.Header.Add("CLIENT-MAC", GlobleConfigurations.ClientMac);
            var result = new HttpHelper().GetHtml(item);
            return result.Html;
        }
        public void ShowElementWnd(string filepath = null)
        {
            if (filepath != null)
            {
                captureTarget = filepath;
            }
            ParseJson();
            CVElementWnd wnd = new CVElementWnd(captureTarget, cvObjs);
            wnd.ShowDialog();
        }
        public void ParseJson()
        {
            string json = string.Empty;
            string id = System.IO.Path.GetFileNameWithoutExtension(captureTarget);
            using (System.IO.FileStream fs = System.IO.File.OpenRead(captureTarget.Replace("bmp", "txt")))
            {
                byte[] bytes = new byte[fs.Length];
                fs.ReadExactly(bytes);
                json = Encoding.UTF8.GetString(bytes);
            }
            JArray items = (JArray)JsonHelper.JsonDesByNewtonJson(json);
            List<CVItem> cvs = new List<CVItem>();
            foreach (var item in items.Children())
            {
                CVItem cv = JsonHelper.JsonDesByNewtonJson<CVItem>(item.ToString());
                cvs.Add(cv);
            }
            cvObjs = new CVObjs();
            cvObjs.TextResults.Clear();
            cvObjs.TextResults.AddRange(cvs);
        }
  
    }
}
