using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Excel.Actions.Software;
using II.RPA.Plugin.Excel.Models;
using II.RPA.Plugin.Excel.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace II.RPA.Plugin.Excel.Forms
{
    /// <summary>
    /// ExcelForm.xaml 的交互逻辑
    /// </summary>
    public partial class ExcelForm : Window
    {
        HookManager hookManager = null;
        public ExcelFormContext Context { get; set; }
        public string Result { get; set; }
        private Dictionary<int, dynamic> excelInstance;

        public ExcelForm(ExcelFormContext excelFormContext)
        {
            //获取所有已打开的Excel实例        
            excelInstance = SoftwareExcel.ContextInstance.GetOpenExcelInstances();
            if (excelInstance == null || excelInstance.Count < 1)
            {
                excelFormContext.ErrorMessage = "没有打开Excel文件，请打开文件后重新打开拾取窗口".I18n("ExcelForm.xaml_A909EF19");
            }
            Context = excelFormContext;
            Context.Title = excelFormContext.Title ?? $"在Excel中选择{excelFormContext.Label}";
            InitializeComponent();
            DataContext = excelFormContext;

        }

        private void BorderClose_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            btn_Cancel_Click(null, null);
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
                hookManager?.Unhook();
                hookManager = null;
                SoftwareExcel.ContextInstance.SelectRuleCallback = null;
            }
            catch
            {

            }

        }

        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = true;
                hookManager?.Unhook();
                hookManager = null;
                SoftwareExcel.ContextInstance.SelectRuleCallback = null;
            }
            catch
            {

            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            hookManager = new HookManager(
            new Action<dynamic>((pId) =>
            {
                if (excelInstance.TryGetValue(pId, out dynamic app))
                {
                    Task.Run(() =>
                    {
                        var activeSheet = app.ActiveSheet;
                        var activeCell = app.Selection;
                        SoftwareExcel.ContextInstance.Application_SheetSelectionChange(activeSheet, activeCell);
                    });
                }
            }),
            new Action<dynamic>((keyCode) =>
            {
                if (keyCode == 0x1B)
                {
                    btn_Cancel_Click(null, null);
                }
            }));
            hookManager.SetHook(IntPtr.Zero);
            SoftwareExcel.ContextInstance.SelectRuleCallback += new Action<object>(result =>
            {
                if (result == null) return;
                if (result is PivotTableRule)
                { 
                    var table = (PivotTableRule)result;
                    if (Context.Arguments == 1)
                    {
                        Context.Value = JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    }
                    else
                    {
                        Context.Value = table.Name;
                    }
                    Dispatcher.Invoke(new Action(() =>
                    {
                        tb_ContentFormat.Text = table.Name?.ToString();
                    }));

                }
                else if (result is SortRule)
                {
                    var sortRule = (SortRule)result;
                    Context.Value = JsonConvert.SerializeObject(sortRule, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    Dispatcher.Invoke(new Action(() =>
                    {
                        tb_ContentFormat.Text = sortRule.Range.range.Trim('"');
                    }));

                }
                else if (result is AutoFilterRule)
                {
                    var autoFilterRule = (AutoFilterRule)result;
                    Context.Value = JsonConvert.SerializeObject(autoFilterRule, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    Dispatcher.Invoke(new Action(() =>
                    {
                        tb_ContentFormat.Text = autoFilterRule.Range;
                    }));
                }
                else
                {
                    Context.Value = result?.ToString();
                    Dispatcher.Invoke(new Action(() =>
                    {
                        tb_ContentFormat.Text = result?.ToString();
                    }));
                    Result = "";
                }
            });


            Left = SystemParameters.WorkArea.Width - Width - 5;
            Top = SystemParameters.WorkArea.Height - Height - 5;
        }


    }
}
