using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace II.RPA.Plugin.Dialog.UCControl
{
    /// <summary>
    /// UCAnnex.xaml 的交互逻辑
    /// </summary>
    public partial class UCAnnex : UserControl, IGetInputModelControl
    {
        private InputModel _inputModel;
        private readonly string _annexPath;
        private static ILogger logger = Logger.OfContext();

        public UCAnnex(string packagePtah)
        {
            _annexPath = System.IO.Path.Combine(packagePtah, "annexFile");
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }

        public void Init(InputModel input)
        {
            try
            {
                if (input != null)
                {
                    _inputModel = input;
                    if (!string.IsNullOrEmpty(input.Name))
                    {
                        this.Name = input.Name;
                    }
                    if (!string.IsNullOrEmpty(input.Label))
                    {
                        this.LAB.Content = input.Label;
                    }
                    if (input.ValueArray != null)
                    {
                        foreach (string option in input.ValueArray)
                        {
                            SubAnnex sub = new SubAnnex(_annexPath, option.ToString());
                            SP.Children.Add(sub);

                        }
                    }
                    if (input.ValueArrayUpgrade != null)
                    {
                        if (input.ValueArrayUpgrade.Any())
                        {
                            foreach (FileInfoModel fileinfo in input.ValueArrayUpgrade)
                            {
                                if (fileinfo != null)
                                {
                                    UCAttachFileControl sub = new UCAttachFileControl(fileinfo.Name, fileinfo.Type);
                                    SP.Children.Add(sub);
                                }
                                else
                                {
                                    logger.LogDebug($"fileinfo is null!");
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Annex Set Value Error");
                throw;
            }
        }

        public InputModel GetInputModel()
        {
            return _inputModel;
        }
    }
}

