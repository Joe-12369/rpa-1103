using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using MSHTML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.IE.V2.Extentions
{
    internal static class IHTMLElement2Ext
    {
        private static ILogger logger = Logger.OfContext();
        public static HTMLRectEX GetOffsetByPadding(this IHTMLElement2 element)
        {
            var paddingLeft = element.currentStyle.paddingLeft;
            var paddingTop = element.currentStyle.paddingTop;
            //var borderleft = element.currentStyle.borderLeftWidth;
            //var bordertop = element.currentStyle.borderTopWidth;
            double.TryParse(paddingLeft?.Replace("px", ""), out double framePanddingleft);
            double.TryParse(paddingTop?.Replace("px", ""), out double framePpanddingtop);
            //double.TryParse(borderleft?.Replace("px", ""), out double frameborderleft);
            //double.TryParse(bordertop?.Replace("px", ""), out double framebordertop);
            return new HTMLRectEX()
            {
                Top = (int)framePpanddingtop,
                Left = (int)framePanddingleft
            };
        }
    }
}
