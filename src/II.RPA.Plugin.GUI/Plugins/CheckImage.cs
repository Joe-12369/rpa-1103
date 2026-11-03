using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.ElementHighlight;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ProcessCreator;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class CheckImage : AsyncElementAbilityBase<ParamCheckImage>
    {
        private static readonly ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ScreenShot;
        private ZbrainService _zbrainService;
        private readonly ElementHighlighter _elementHighlighter;
        private readonly GuiHighlightService guiHighlightService;

        public CheckImage(GuiHighlightService guiHighlightService)
        {
            this.guiHighlightService = guiHighlightService;
            _elementHighlighter = new ElementHighlighter();
        }

        protected override async Task<JMessage> ExecuteAsync(ParamCheckImage paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            var timeout = paramModel.RunParam.Timeout <= 0 ? TimeSpan.FromSeconds(5) : TimeSpan.FromMilliseconds(paramModel.RunParam.Timeout);
            var zbrainService = EnsureZbrainService(timeout);
            var inPayload = paramModel.InParameter;
            string directoryPath = Path.GetDirectoryName(inPayload.ImageSource);
            string fileName = Guid.NewGuid().ToString() + ".png";
            var imagePath = Path.Combine(directoryPath, fileName);
            using (var image = ScreenShotAll())
            {
                image.Save(imagePath, ImageFormat.Png);
            }
            Dictionary<string, object> dic = new Dictionary<string, object>
            {
                ["source"] = imagePath,
                ["template"] = inPayload.ImageSource,
                ["thr"] = inPayload.Similarity,
                ["ori_pix"] = inPayload.Resolution
            };
            if (inPayload.Rect.IsValid())
            {
                dic["domain_area"] = new
                {
                    x = inPayload.Rect.StartX,
                    y = inPayload.Rect.StartY,
                    width = inPayload.Rect.EndX - inPayload.Rect.StartX,
                    height = inPayload.Rect.EndY - inPayload.Rect.StartY,
                };
            }
            else
            {
                dic["domain_area"] = new
                {
                    x = 0,
                    y = 0,
                    width = Screen.PrimaryScreen.Bounds.Size.Width,
                    height = Screen.PrimaryScreen.Bounds.Size.Height,
                };
            }
            var body = dic.ToJson();
            logger.LogDebug($"body:{body}");
            var responseString = Task.Run(() => zbrainService.CheckImageAsync(body)).Result;
            var rectsData = JObject.Parse(responseString)["res"]?.ToObject<List<List<int>>>();
            List<Rectangle> rects = new List<Rectangle>();
            if (rectsData != null && rectsData.Count > 0)
            {
                rects.Add(new Rectangle
                {
                    X = (int)(rectsData[0][0]),
                    Y = (int)(rectsData[0][1]),
                    Width = (int)((rectsData[1][0] - rectsData[0][0])),
                    Height = (int)((rectsData[1][1] - rectsData[0][1]))
                });
            }
            else
            {
                throw new RPAException(eErrorMessageType.Error_ImageCheckFailed);
            }
            await guiHighlightService.HighlightAsync(rects).ConfigureAwait(false);
            //todo show highlight rect
            return new JMessage
            {
                Code = 0,
                Message = eCommonMessageType.JMessage_cs_CaoZuoChengGong.GetMessage(),
                Info = new JObject { { "count", rects.Count } }.ToString()
            };
        }

        protected override void ValidateParameter(ParamCheckImage paramModel) {
            try
            {
                if (paramModel == null) throw new RPAException(eErrorMessageType.Error_ParamsError); return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        public static Image ScreenShotAll()
        {
            Rectangle rc = Screen.PrimaryScreen.Bounds;
            int iWidth = rc.Width;
            int iHeight = rc.Height;
            Image myImage = new Bitmap(iWidth, iHeight);
            Graphics.FromImage(myImage).CopyFromScreen(new Point(0, 0), new Point(0, 0), new Size(iWidth, iHeight));
            return myImage;
        }

        public ZbrainService EnsureZbrainService(TimeSpan timeSpan)
        {
            if (_zbrainService == null)
            {
                _zbrainService = new ZbrainService(GlobleConfigurations.ZBrainServicePath, timeSpan);
            }
            return _zbrainService;
        }
    }
}
