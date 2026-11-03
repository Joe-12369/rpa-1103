using II.RPA.Plugin.Dialog.Depends;
using II.RPA.Plugin.Dialog.Forms;
using II.RPA.Plugin.Dialog.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text;

namespace II.RPA.Plugin.Dialog.UCControl
{
    /// <summary>
    /// Interaction logic for UCDataTableWithSheet.xaml
    /// </summary>
    public partial class UCDataTableWithSheet : UserControl
    {
        private InputModel _inputModel;
        private readonly ExeMode _exeModel; 
        private static ILogger logger = Logger.OfContext();

        public UCDataTableWithSheet()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }
        public UCDataTableWithSheet(ExeMode exeMode)
        {
            InitializeComponent();
            _exeModel = exeMode;
        }
        public void Init(InputModel input)
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

                var listTab = ExcelHelper.ImportSheetData(input.Value);
                foreach (var item in listTab)
                {
                    DataSheets.Items.Add(item);
                }
                //if (readMessage.Code)
                //{
                //    // this.TABLE.ItemsSource = _dataTable.DefaultView;
                //}
                //else
                //{
                //    logger.LogError("数据表加载异常：" + readMessage.Message);
                //    throw new Exception(readMessage.Message);
                //}
            }
        }
    }

}
