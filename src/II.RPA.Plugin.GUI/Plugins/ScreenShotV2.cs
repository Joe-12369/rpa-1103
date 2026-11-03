using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.Plugin.GUI.ScreenShot;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class ScreenShotV2 : ElementAbilityBase<Params.EmptyElementParameter>
    {
        private static readonly ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ScreenShot;
        private readonly ScreenShoter m_ScreenShoter;
        private readonly ConcurrencyItemContainer<CroppedBitmap> m_SnapResult = new ConcurrencyItemContainer<CroppedBitmap>();
        private readonly ImageDataProvider m_ImageDataProvider;
        public ScreenShotV2()
        {
            m_ScreenShoter = new ScreenShoter();
            m_ImageDataProvider = new ImageDataProvider();
            m_ScreenShoter.Snapped += ScreenShoter_Snapped;
        }

        private void ScreenShoter_Snapped(object sender, FunctionEventArgs<System.Windows.Media.ImageSource> e)
        {
            if (e.IsDone)
            {
                CroppedBitmap image = e.Info as CroppedBitmap;
                m_SnapResult.SetData(image);
            }
            else
            {
                m_SnapResult.SetData(null);
            }
        }

        protected override JMessage Execute(Params.EmptyElementParameter paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            m_ScreenShoter.Start();
            m_SnapResult.WaitForData(-1);

            return m_ScreenShoter.Dispatcher.Invoke(() => 
            {
                try
                {
                    var result = m_SnapResult.TakeData();
                    if (result.SourceRect == Int32Rect.Empty)
                    {
                        return new JMessage
                        {
                            Code = 1,
                            Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage(),
                        };
                    }
                    else
                    {
                        var data = m_ImageDataProvider.Provide(GlobleConfigurations.PackageDirectory);
                        if (string.IsNullOrEmpty(data.ImagePath))
                        {
                            logger.LogError($"Image path is empty!");
                        }
                        if (File.Exists(data.ImagePath))
                        {
                            File.Delete(data.ImagePath);
                            logger.LogWarning($"Delete same image file!");
                        }
                        SavePng(result, data.ImagePath);
                        data.ImgResolution = new Resolution
                        {
                            Width = result.SourceRect.Width,
                            Height = result.SourceRect.Height,
                        };
                        data.DeviceResolution = new Resolution
                        {
                            Width = Screen.PrimaryScreen.Bounds.Width,
                            Height = Screen.PrimaryScreen.Bounds.Height,
                        };
                        return new JMessage
                        {
                            Code = 0,
                            Message = "Done",
                            Info = data.ToJson()
                        };
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                    return new JMessage
                    {
                        Code = 1,
                        Message = eCommonMessageType.ScreenShotExecutor_cs_JieTuShiBai.GetMessage(),
                    };
                }
            });     
        }

        protected override void ValidateParameter(Params.EmptyElementParameter paramModel) {
            /// none
            return;
        }

        public static void SavePng(CroppedBitmap source, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(fs);
            }
        }
    }
}
