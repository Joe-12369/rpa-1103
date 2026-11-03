using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.OcrOffline.Utils
{
    public class OcrTableError
    {
        private static ILogger logger = Logger.OfContext();

        public static JMessage GetErrMessage(eErrorMessageType _eErrorMessageType, string _LogErrMsg = "")
        {
            JMessage msg = new JMessage();

            var rpaEx = new RPAException(_eErrorMessageType, eComponentType.CaptureCV);
            msg.Code = rpaEx.ErrorCode;
            msg.UnifyCode = rpaEx.UnifyErrorCode;
            if (msg.Code == 0)
            {
                msg.Code = rpaEx.ErrorCode;
                msg.UnifyCode = rpaEx.UnifyErrorCode;
            }

            if (!String.IsNullOrEmpty(_LogErrMsg))
            {
                logger.LogError(_LogErrMsg);
            }

            msg.Message = rpaEx.ErrorCodeSuffixMessage;
            return msg;
        }


    }
}
