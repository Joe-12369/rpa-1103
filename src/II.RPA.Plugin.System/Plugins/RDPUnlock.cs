using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.RPASystem.Helper;
using II.RPA.Plugin.RPASystem.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.IO;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class RDPUnlock : SystemComponentBase<ParamRDPUnlockModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.RDPUnlock;
        private const string _dataPath = @"Zbot\AutoLogin";
        private const string _dataFile = @"ZCredential.txt";
        protected override void ValidateParameter(ParamRDPUnlockModel paramModel)
        {
            //try
            //{
                if (string.IsNullOrEmpty(paramModel.UserName) || string.IsNullOrEmpty(paramModel.Pwd))
                {
                    throw new RPAException(eErrorMessageType.Error_ScreenLock_AccountOrPwd);
                }
            //}
            //catch (Exception ex)
            //{
            //    Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"参数格式不符合要求：{jsonParam}");
            //    return false;
            //}
        }
        protected override JMessage Execute(ParamRDPUnlockModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                if (!RegisHelper.CheckPlugState())
                {
                    throw new RPAException(eErrorMessageType.Error_UnlockScreenExtensionUnInstalled, eComponentType.RDPUnlock);
                }

                //if (ScreenStateHelper.GetScreenState() == "Locked")
                {
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), _dataPath);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    string filePath = Path.Combine(path, _dataFile);
                    using (var file = File.CreateText(filePath))
                    {
                        file.WriteLine(paramModel.UserName);
                        file.WriteLine(EncryptUtils.Base64Encrypt(paramModel.Pwd));
                    }
                    User32Helper.LockWorkStation();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_CpUnLock, ref msg);
            }

            return msg;
        }
    }
}

