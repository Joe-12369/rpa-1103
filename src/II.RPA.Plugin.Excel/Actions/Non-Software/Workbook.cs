using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using DevExpress.Spreadsheet;
using DevExpress.XtraPrinting;
using DevExpress.XtraSpreadsheet;
using II.RPA.Plugin.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace II.RPA.Plugin.Excel.Actions.NonSoftware
{
    /// <summary>
    /// 不使用软件自动化
    /// 设计器.文档操作类组件
    /// </summary>
    public partial class NonSoftware : IExcel
    {
        private Dictionary<string, Tuple<string, string>> EncryptedWorkbook;

        /// <summary>
        /// 新建Excel
        /// </summary>       
        /// <param name="path">文件路径</param>  
        /// <param name="name">文件名称</param>  
        /// <param name="software">使用软件["默认","MicrosoftExcel","WPS"]</param>  
        /// <param name="visible">是否可视化["是","否"]</param>  
        /// <param name="maximize">是否最大化["是","否"]</param>  
        /// <param name="show">是否显示软件["是","否"]</param>  
        void IExcel.New(string path, string name, string software, string visible, string maximize, string show)
        {
            var file = Instance.ActionContext["__IsShareFile"] ? $"{path.TrimEnd('\\')}\\{name}" : ExcelUtils.GetAbsolutePath(path, name);
            CreateWorkbook(file);
        }

        private void CreateWorkbook(string file)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(file))) file += ".xlsx";

            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(file))
            {
                //add by wenyi :ZOCO-23882
                if (ActionContext.ActionParams.TryGetValue("override", out dynamic _override) && _override == 1)
                {
                    try
                    {
                        FileSystem.DeleteFile(
                            file,
                            UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin
                        );
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "文件无法被移除");
                        //文件无法被移除
                        throw new ExcelException(Localize($"{file} 已存在", "Workbook_AAC619A3"));
                    }
                    // 极端情况下 Shell 未删除成功
                    if (File.Exists(file))
                    {
                        throw new ExcelException(Localize($"{file} 已存在", "Workbook_AAC619A3"));
                    }
                }
                else
                {
                    throw new ExcelException(Localize($"{file} 已存在", "Workbook_AAC619A3"));
                }
            }
            var wb = new Workbook();
            wb.SaveDocument(file);
            Workbooks[file] = wb;
            ContextInstance.ActionContext.AddResult("workbook", file);
        }

        void IExcel.NewV6(string path, string name, string software, string visible, string maximize, string show)
        {
            var file = ExcelUtils.GetAbsolutePathV2(path, name);
            CreateWorkbook(file);
        }

        /// <summary>
        /// 打开Excel
        /// </summary>      
        /// <param name="file">文件路径</param>  
        /// <param name="software">使用软件["默认","MicrosoftExcel","WPS"]</param>  
        /// <param name="visible">是否可视化["是","否"]</param>  
        /// <param name="maximize">是否最大化["是","否"]</param>  
        /// <param name="password1">打开密码</param>  
        /// <param name="password2">编辑密码</param>  
        /// <param name="show">是否显示软件["是","否"]</param>  
        void IExcel.Open(string file, string software, string visible, string maximize, string password1, string password2, string show)
        {
            file = Instance.ActionContext["__IsShareFile"] ? file : ExcelUtils.GetAbsolutePath(null, file);
            OpenWorkbookFromPath(file, password1, password2);
        }

        private void OpenWorkbookFromPath(string file, string password1, string password2)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(file))) file += ".xlsx";

            if (!File.Exists(file))
            {
                throw new ExcelException(Localize($"{file} 不存在", "Workbook_EAB43890"));
            }
            if (Workbooks.ContainsKey(file))
            {
                Instance.Close(file, "否".I18n("Workbook_FBDDAA81"), "否".I18n("Workbook_FBDDAA81"));
            }

            var autoUpdateLink = Instance.ActionContext.ActionParams.TryGetValue("autoUpdateLink", out dynamic _value) && _value> 0;
            var upateLink = autoUpdateLink ? UpdateLinksMode.Always : UpdateLinksMode.Never;
            try
            {
                var wb = new Workbook();
                wb.DocumentSettings.UpdateLinks = upateLink;

                wb.EncryptedFilePasswordRequest += (object sender, EncryptedFilePasswordRequestEventArgs e) =>
                {
                    e.Password = ExcelUtils.DecodeBase64(password1);
                    e.Handled = true;
                };
                wb.EncryptedFilePasswordCheckFailed += (object sender, EncryptedFilePasswordCheckFailedEventArgs e) =>
                {
                    switch (e.Error)
                    {
                        case SpreadsheetDecryptionError.PasswordRequired:
                            e.TryAgain = false;
                            e.Handled = true;
                            throw new ExcelException(Localize($"{file} 是加密文件，请输入打开文件的密码", "Workbook_59126B49"));
                        case SpreadsheetDecryptionError.WrongPassword:
                            e.TryAgain = false;
                            e.Handled = true;
                            throw new ExcelException(Localize($"打开文件密码输入错误，请输入正确的打开文件密码", "Workbook_CFF4DC93"));
                    }
                };
                wb.InvalidFormatException += (object sender, SpreadsheetInvalidFormatExceptionEventArgs e) =>
                {
                    logger.LogError(e.Exception);
                    throw new ExcelException("打开文件异常，无法解析该文件，请确认文件是否已损坏！".I18n("Workbook_F41ED93"));
                };
                //是否自动计算公式
                var calculation = ActionContext.GetActionArgs("Calculation");
                if (calculation != null)
                {
                    try
                    {
                        if (calculation == "-4135")
                        {
                            wb.DocumentSettings.Calculation.Mode = CalculationMode.Manual;
                        }
                        else if (calculation == "-4105")
                        {
                            wb.DocumentSettings.Calculation.Mode = CalculationMode.Automatic;
                        }
                        else if (calculation == "2")
                        {
                            wb.DocumentSettings.Calculation.Mode = CalculationMode.AutomaticExceptTables;
                        }
                        else
                        {
                        }

                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"set calculation error:{ex}");
                    }
                }
                //创建文件副本
                file = CreateFileCopyWithPrefix(file);
                wb.LoadDocument(file);
                var wpOptions = wb.DocumentSettings.WriteProtection;
                if (wpOptions.IsPasswordProtected)
                {
                    if (!wpOptions.CheckPassword(ExcelUtils.DecodeBase64(password2)))
                    {
                        throw new ExcelException(Localize($"{file} 启用了编辑文档权限，请输入正确的编辑权限密码", "Workbook_5777EDF1"));
                    }
                }
                Workbooks[file] = wb;
                ContextInstance.ActionContext.AddResult("workbook", file);

                if (!string.IsNullOrWhiteSpace(password1) || !string.IsNullOrWhiteSpace(password2))
                {
                    if (EncryptedWorkbook == null)
                    {
                        EncryptedWorkbook = new Dictionary<string, Tuple<string, string>>();
                    }
                    EncryptedWorkbook[file] = new Tuple<string, string>(password1, password2);
                }
            }
            catch (OutOfMemoryException ex)
            {
                logger.LogError(ex);
                throw new ExcelException(Localize($"文件过大，无法打开，请选择 Microsoft Office 或 WPS Office 选项打开文件", "Workbook_175C8BAB"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException(ex.Message);
            }
        }

        void IExcel.OpenV5(string file, string software, string visible, string maximize, string password1, string password2, string show)
        {
            file = ExcelUtils.GetAbsolutePathV2(null, file);
            OpenWorkbookFromPath(file, password1, password2);
        }

        /// <summary>
        /// 获取已打开Excel
        /// </summary>      
        /// <param name="file">文件名称</param>  
        void IExcel.Load(string file)
        {
            if (ActionContext.ActionParams.TryGetValue("option", out dynamic option) && option == 0)
            {
                throw new ExcelException("【获取激活的Excel】需要依赖 Microsoft Excel 或 WPS软件".I18n("Workbook_7450591F"));
            }
            else
            {
                if (Workbooks.ContainsKey(file))
                {
                    ContextInstance.ActionContext.AddResult("workbook", file);
                }
                else
                {
                    throw new ExcelException(Localize($"{file} 未打开，请使用【打开Excel】组件打开", "Workbook_84F856F4"));
                }
            }
        }

        /// <summary>
        /// 保存Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="close">保存后关闭["是","否"]</param>  
        void IExcel.Save(string workbook, string close)
        {
            if (Workbooks.ContainsKey(workbook))
            {
                var wb = Workbooks[workbook];
                if (Encryptions.ContainsKey(workbook))
                {
                    wb.SaveDocument(workbook, Encryptions[workbook]);
                }
                else
                    wb.SaveDocument(workbook);

                ExcelUtils.TryFuncIgnoreException(() =>
                {
                    if (Path.GetFileName(workbook).StartsWith("sz~!_") && File.Exists(workbook))
                    {
                        File.Copy(workbook, workbook.Replace("sz~!_", ""), true);
                    }
                });

                if (close.Equals("是"))
                    Instance.Close(workbook, "否".I18n("Workbook_FBDDAA81"), "否".I18n("Workbook_FBDDAA81"));
            }
        }
        void SaveAs(string workbook, string file, string close, string cover)
        {
            if (File.Exists(file))
            {
                if (cover.Equals("是"))
                    File.Delete(file);
                else
                    file = ExcelUtils.GetDuplicateFileName(file);
            }
            Workbooks[workbook].SaveDocument(file);
            Instance.Close(workbook, "是".I18n("Workbook_73EDDB14"), "否".I18n("Workbook_FBDDAA81"));
            if (EncryptedWorkbook?.TryGetValue(workbook, out var encryptedWorkbook) == true)
            {
                EncryptedWorkbook.Remove(workbook);
                Instance.Open(file, "", "", "", encryptedWorkbook.Item1, encryptedWorkbook.Item2, "");
            }
            else
            {
                Instance.Open(file, "", "", "", "", "", "");
            }
        }
        /// <summary>
        /// 另存为Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="path">另存为路径</param>  
        /// <param name="name">文件名称</param>  
        /// <param name="close">保存后关闭["是","否"]</param>  
        /// <param name="cover">覆盖相同文件["是","否"]</param>  
        void IExcel.SaveAs(string workbook, string path, string name, string close, string cover)
        {
            var file = Instance.ActionContext["__IsShareFile"] ? $"{path.TrimEnd('\\')}\\{name}" : ExcelUtils.GetAbsolutePath(path, name);          
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            SaveAs(workbook, file, close, cover);
        }

        void IExcel.SaveAsV2(string workbook, string path, string name, string close, string cover)
        {
            var file = ExcelUtils.GetAbsolutePathV2(path, name);
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            SaveAs(workbook, file, close, cover);
        }

        /// <summary>
        /// 关闭Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>            
        /// <param name="save">是否保存</param>
        /// <param name="close">关闭软件["是","否"]，第一次需求表示是否关闭软件，第二次需求变更为，否：关闭当前，是：关闭所有</param>  
        void IExcel.Close(string workbook, string save, string close)
        {
            if (save.Equals("是"))
            {
                Instance.Save(workbook, close);
            }
            if (close.Equals("否"))
            {
                //关闭当前
                if (Encryptions.ContainsKey(workbook))
                {
                    Encryptions.Remove(workbook);
                }
                Workbooks[workbook].Dispose();
                Workbooks.Remove(workbook);
                ExcelPlugin.Instance.Remove(workbook);

                ExcelUtils.TryFuncIgnoreException(() =>
                {
                    if (Path.GetFileName(workbook).StartsWith("sz~!_") && File.Exists(workbook))
                    {
                        File.Delete(workbook);
                    }
                });
            }
            else
            {
                //关闭所有
                for (int i = 0; i < Workbooks.Count; i++)
                {
                    try
                    {
                        var item = Workbooks.ElementAt(i);
                        if (Encryptions.ContainsKey(item.Key))
                        {
                            Encryptions.Remove(workbook);
                        }
                        Workbooks[item.Key].Dispose();
                        ExcelPlugin.Instance.Remove(item.Key);

                        ExcelUtils.TryFuncIgnoreException(() =>
                        {
                            if (Path.GetFileName(item.Key).StartsWith("sz~!_") && File.Exists(workbook))
                            {
                                File.Delete(item.Key);
                            }
                        });
                    }
                    catch (System.Exception ex)
                    {
                        logger.LogError(ex);
                        continue;
                    }
                }
                Workbooks.Clear();
            }
        }

        /// <summary>
        /// 关闭组件V2
        /// </summary>
        /// <param name="closeType">关闭类型（文件、软件）</param>
        /// <param name="workbook">文件</param>
        /// <param name="closeAction">关闭前操作（保存，不保存，另存为）</param>
        /// <param name="filePath">另存为路径</param>
        /// <param name="cover">是否覆盖</param>      
        /// <param name="software">关闭的软件</param>
        /// <param name="kill">杀掉进程</param>
        void IExcel.CloseV2(int closeType, string workbook, int closeAction, string filePath, int cover, int software, int kill)
        {
            if (closeAction == 0)
            {
                //保存
                Instance.Close(workbook, "是".I18n("Workbook_73EDDB14"), "否".I18n("Workbook_FBDDAA81"));
            }
            else if (closeAction == 1)
            {
                //不保存
                Instance.Close(workbook, "否".I18n("Workbook_FBDDAA81"), "否".I18n("Workbook_FBDDAA81"));
            }
            else
            {
                //另存为               
                SaveAs(workbook, filePath, "是", cover == 1 ? "是" : "否");
            }
        }

        void IExcel.CloseV3(int closeType, string workbook, int closeAction, string filePath, int cover, int software, int kill)
        {
            if (closeAction == 0)
            {
                Instance.Close(workbook, "是".I18n("Workbook_73EDDB14"), "否".I18n("Workbook_FBDDAA81"));
            }
            else if (closeAction == 1)
            {
                Instance.Close(workbook, "否".I18n("Workbook_FBDDAA81"), "否".I18n("Workbook_FBDDAA81"));
            }
            else
            {
                var file = ExcelUtils.GetAbsolutePathV2(null, filePath);
                var dir = Path.GetDirectoryName(file);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                SaveAs(workbook, file, "是", cover == 1 ? "是" : "否");
            }
        }

        /// <summary>
        /// 导出Excel为PDF
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="option">Sheet名称["全部","激活页"]</param>  
        /// <param name="path">导出路径</param>  
        /// <param name="name">文件名称</param>  
        /// <param name="cover">覆盖相同文件["是","否"]</param>  
        void IExcel.Export(string workbook, string option, string path, string name, string cover)
        {
            if (!name.EndsWith(".pdf"))
            {
                name += ".pdf";
            }
            var file = ExcelUtils.GetAbsolutePath(path, name);
            if (File.Exists(file))
            {
                if (cover.Equals("否"))
                    file = ExcelUtils.GetDuplicateFileName(file);
                else
                    File.Delete(file);
            }
            PdfExportOptions options = new PdfExportOptions();
            options.ImageQuality = PdfJpegImageQuality.Medium;
            if (option.Equals("全部"))
            {
                Workbooks[workbook].ExportToPdf(file, options);
            }
            else
            {
                Workbooks[workbook].ExportToPdf(file, options, Workbooks[workbook].Worksheets.ActiveWorksheet.Name);
            }
        }

        void IExcel.ExportV2(string workbook, string option, string path, string name, string cover)
        {
            if (!name.EndsWith(".pdf"))
            {
                name += ".pdf";
            }
            var file = ExcelUtils.GetAbsolutePathV2(path, name);
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(file))
            {
                if (cover.Equals("否"))
                    file = ExcelUtils.GetDuplicateFileName(file);
                else
                    File.Delete(file);
            }
            PdfExportOptions options = new PdfExportOptions();
            options.ImageQuality = PdfJpegImageQuality.Medium;
            if (option.Equals("全部"))
            {
                Workbooks[workbook].ExportToPdf(file, options);
            }
            else
            {
                Workbooks[workbook].ExportToPdf(file, options, Workbooks[workbook].Worksheets.ActiveWorksheet.Name);
            }
        }

        /// <summary>
        /// 加密Excel
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="password1">打开密码</param>  
        /// <param name="password2">编辑密码</param>  

        void IExcel.Encrypt(string workbook, string password1, string password2)
        {
            var wb = Workbooks[workbook];
            if (!string.IsNullOrEmpty(password1))
            {
                Encryptions[workbook] = new EncryptionSettings
                {
                    Type = EncryptionType.Compatible,
                    Password = ExcelUtils.DecodeBase64(password1)
                };
            }
            if (!string.IsNullOrEmpty(password2))
            {
                var wpOptions = wb.DocumentSettings.WriteProtection;
                wpOptions.SetPassword(ExcelUtils.DecodeBase64(password2));
                wpOptions.UserName = "实在RPA".I18n("Workbook_441175B6");
            }
        }

        /// <summary>
        /// 获取Excel路径
        /// </summary>      
        /// <param name="workbook">文件对象</param>  
        void IExcel.GetPath(string workbook)
        {
            ContextInstance.ActionContext.AddResult("strPath", Workbooks[workbook].Path);
        }

    }
}
