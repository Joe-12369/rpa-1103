using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using II.RPA.Plugin.GoogleSheets.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.AppendRequest;

namespace II.RPA.Plugin.GoogleSheets.SheetsWrapper
{
    public class SheetAdapter
    {
        public string SpreadsheetID { get; set; }

        public string TabName { get; private set; }

        public int? SheetID { get; set; }

        public string ServiceAccountEmail { get; set; }

        public string[] Scopes { get; set; } = { SheetsService.Scope.Spreadsheets };

        public SheetsService Service { get; private set; }

        public SheetAdapter(string spreadsheetID, string serviceAccountEmail, string tabName)
        {
            this.SpreadsheetID = spreadsheetID;
            this.ServiceAccountEmail = serviceAccountEmail;
            this.TabName = tabName;
        }
        public void Init(string jsonCredentials)
        {
            var credential = (ServiceAccountCredential)
                   GoogleCredential.FromJson(jsonCredentials).UnderlyingCredential;

            var initializer = new ServiceAccountCredential.Initializer(credential.Id)
            {
                User = this.ServiceAccountEmail,
                Key = credential.Key,
                Scopes = Scopes
            };
            credential = new ServiceAccountCredential(initializer);

            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
            });

            this.Service = service;

            this.UpdateTabName(this.TabName);
        }

        /// <summary>
        /// 获取指定名称的页签
        /// </summary>
        /// <param name="newTabName"></param>
        public void UpdateTabName(string newTabName)
        {
            var spreadsheet = this.Service.Spreadsheets.Get(this.SpreadsheetID);

            var result = spreadsheet.Execute();

            Sheet sheet;

            if (!string.IsNullOrEmpty(newTabName))
            {
                sheet = result.Sheets.Where(s => s.Properties.Title.Equals(newTabName, StringComparison.CurrentCultureIgnoreCase)).First();
            }
            else
            {
                sheet = result.Sheets.First();
            }

            this.SheetID = sheet.Properties.SheetId;

            this.TabName = newTabName;
        }

        /// <summary>
        /// 返回Sheet页签列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllTabNames()
        {
            var spreadsheet = this.Service.Spreadsheets.Get(this.SpreadsheetID);

            var result = spreadsheet.Execute();

            var tabs = result.Sheets.Select(s => s.Properties.Title).ToList();

            return tabs;
        }

        /// <summary>
        /// 返回sheetRange内的所有行[读取表格数据]
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public IList<IList<object>> GetRows(SheetRange range)
        {
            var rangeValue = range.CanSupportA1Notation ? range.A1Notation : range.R1C1Notation;

            GetRequest request =
                    this.Service.Spreadsheets.Values.Get(this.SpreadsheetID, rangeValue);

            request.ValueRenderOption = GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
            request.DateTimeRenderOption = GetRequest.DateTimeRenderOptionEnum.FORMATTEDSTRING;

            ValueRange response = request.Execute();
            return response.Values;
        }
        public int GetRowsCount(string SheetName)
        {
            //var rangeValue = range.CanSupportA1Notation ? range.A1Notation : range.R1C1Notation;

            GetRequest request =
                    this.Service.Spreadsheets.Values.Get(this.SpreadsheetID, SheetName);

            request.ValueRenderOption = GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
            request.DateTimeRenderOption = GetRequest.DateTimeRenderOptionEnum.FORMATTEDSTRING;

            ValueRange response = request.Execute();
            return response.Values.Count;
        }
        /// <summary>
        /// Return a collection of rows formatted values for a given SheetRange input
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public IList<IList<object>> GetRowsFormatted(SheetRange range)
        {
            var rangeValue = range.CanSupportA1Notation ? range.A1Notation : range.R1C1Notation;

            GetRequest request =
                    this.Service.Spreadsheets.Values.Get(this.SpreadsheetID, rangeValue);

            request.ValueRenderOption = GetRequest.ValueRenderOptionEnum.FORMATTEDVALUE;
            request.DateTimeRenderOption = GetRequest.DateTimeRenderOptionEnum.FORMATTEDSTRING;

            ValueRange response = request.Execute();
            return response.Values;
        }

        /// <summary>
        /// 删除列
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public BatchUpdateSpreadsheetResponse DeleteColumn(int col)
        {
            var requests = new List<Request>();

            var request = new Request()
            {
                DeleteDimension = new DeleteDimensionRequest()
                {
                    Range = new DimensionRange()
                    {
                        Dimension = "COLUMNS",
                        StartIndex = col - 1,
                        EndIndex = col,
                        SheetId = this.SheetID,
                    }
                }
            };

            requests.Add(request);

            BatchUpdateSpreadsheetRequest bussr = new BatchUpdateSpreadsheetRequest
            {
                Requests = requests
            };

            var updateRequest = this.Service.Spreadsheets.BatchUpdate(bussr, this.SpreadsheetID);
            return updateRequest.Execute();
        }

        /// <summary>
        /// Deletes a specified column
        /// </summary>
        /// <param name="columnLetter"></param>
        /// <returns></returns>
        public BatchUpdateSpreadsheetResponse DeleteColumn(string columnLetter)
        {
            var columnId = SheetRange.GetColumnIDFromLetters(columnLetter);

            return this.DeleteColumn(columnId);
        }

        /// <summary>
        /// Deletes a specified row
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public BatchUpdateSpreadsheetResponse DeleteRow(int row)
        {
            var requests = new List<Request>();

            var request = new Request()
            {
                DeleteDimension = new DeleteDimensionRequest()
                {
                    Range = new DimensionRange()
                    {
                        Dimension = "ROWS",
                        StartIndex = row - 1,
                        EndIndex = row,
                        SheetId = this.SheetID,
                    }
                }
            };

            requests.Add(request);

            BatchUpdateSpreadsheetRequest bussr = new BatchUpdateSpreadsheetRequest
            {
                Requests = requests
            };

            var updateRequest = this.Service.Spreadsheets.BatchUpdate(bussr, this.SpreadsheetID);
            return updateRequest.Execute();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startRow"></param>
        /// <param name="endRow"></param>
        /// <returns></returns>
        public BatchUpdateSpreadsheetResponse DeleteRows(int startRow, int endRow)
        {
            var requests = new List<Request>();

            var request = new Request()
            {
                DeleteDimension = new DeleteDimensionRequest()
                {
                    Range = new DimensionRange()
                    {
                        Dimension = "ROWS",
                        StartIndex = startRow - 1,
                        EndIndex = endRow,
                        SheetId = this.SheetID,
                    }
                }
            };

            requests.Add(request);

            BatchUpdateSpreadsheetRequest bussr = new BatchUpdateSpreadsheetRequest
            {
                Requests = requests
            };

            var updateRequest = this.Service.Spreadsheets.BatchUpdate(bussr, this.SpreadsheetID);
            return updateRequest.Execute();
        }

        /// <summary>
        /// Inserts a blank new column using the column index as the id (NOTE: 1 is the first index for the column based index)
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public BatchUpdateSpreadsheetResponse InsertBlankColumn(int column)
        {
            if (column < 1)
            {
                throw new ArgumentException("column index value must be 1 or greater");
            }

            var requests = new List<Request>();

            var request = new Request()
            {
                InsertDimension = new InsertDimensionRequest()
                {
                    Range = new DimensionRange()
                    {
                        Dimension = "COLUMNS",
                        StartIndex = column - 1,
                        EndIndex = column,
                        SheetId = this.SheetID,
                    },
                    InheritFromBefore = column > 0,
                }
            };

            requests.Add(request);

            BatchUpdateSpreadsheetRequest bussr = new BatchUpdateSpreadsheetRequest
            {
                Requests = requests
            };

            var updateRequest = this.Service.Spreadsheets.BatchUpdate(bussr, this.SpreadsheetID);
            return updateRequest.Execute();
        }

        /// <summary>
        /// Inserts a blank new column using a letter notation (i.e. B2 as the column id)
        /// </summary>
        /// <param name="columnLetter"></param>
        /// <returns></returns>
        public BatchUpdateSpreadsheetResponse InsertBlankColumn(string columnLetter)
        {
            var columnId = SheetRange.GetColumnIDFromLetters(columnLetter);

            return this.InsertBlankColumn(columnId);
        }

        /// <summary>
        /// Inserts a new blank row
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public BatchUpdateSpreadsheetResponse InsertBlankRow(int row)
        {
            if (row < 1)
            {
                throw new ArgumentException("row index value must be 1 or greater");
            }

            var requests = new List<Request>();

            var request = new Request()
            {
                InsertDimension = new InsertDimensionRequest()
                {
                    Range = new DimensionRange()
                    {
                        Dimension = "ROWS",
                        StartIndex = row - 1,
                        EndIndex = row,
                        SheetId = this.SheetID,
                    },
                    InheritFromBefore = row > 0,
                }
            };

            requests.Add(request);

            BatchUpdateSpreadsheetRequest bussr = new BatchUpdateSpreadsheetRequest
            {
                Requests = requests
            };

            var updateRequest = this.Service.Spreadsheets.BatchUpdate(bussr, this.SpreadsheetID);
            return updateRequest.Execute();
        }

        /// <summary>
        /// Runs a collection of updates as a batch operation in a single call.
        /// 
        /// This is useful to avoid throttling limits with the Google Sheets API
        /// </summary>
        /// <param name="updates"></param>
        /// <returns></returns>
        public BatchUpdateSpreadsheetResponse BatchUpdate(List<BatchUpdateRequestObject> updates)
        {
            BatchUpdateSpreadsheetRequest bussr = new BatchUpdateSpreadsheetRequest();

            var requests = new List<Request>();

            foreach (var update in updates)
            {
                //create the update request for cells from the first row
                var updateCellsRequest = new Request()
                {
                    RepeatCell = new RepeatCellRequest()
                    {
                        Range = new GridRange()
                        {
                            SheetId = this.SheetID,
                            StartColumnIndex = update.Range.StartColumn - 1,
                            StartRowIndex = update.Range.StartRow - 1,
                            EndColumnIndex = update.Range.StartColumn,
                            EndRowIndex = update.Range.StartRow,
                        },
                        Cell = update.Data,
                        Fields = "*"
                    }
                };

                requests.Add(updateCellsRequest);
            }

            bussr.Requests = requests;

            var updateRequest = this.Service.Spreadsheets.BatchUpdate(bussr, this.SpreadsheetID);
            return updateRequest.Execute();
        }
        /// <summary>
        /// 复制粘贴
        /// </summary>
        /// <param name="sourceSheetName"></param>
        /// <param name="destSheetName"></param>
        /// <param name="sourceStart"></param>
        /// <param name="sourceEnd"></param>
        /// <param name="destStart"></param>
        /// <param name="destEnd"></param>
        /// <param name="onlyValue"></param>
        public BatchUpdateSpreadsheetResponse CopyPaste(string sourceSheetName, string destSheetName, string sourceStart, string sourceEnd, string destStart, string destEnd, bool onlyValue)
        {
            #region  根据表名获取sheetId

            var spreadsheet = this.Service.Spreadsheets.Get(this.SpreadsheetID);

            var result = spreadsheet.Execute();

            var tabs = result.Sheets.ToList();

            int sourceSheetId;
            int destSheetId;
            if (tabs.Where(s => s.Properties.Title == sourceSheetName).Count() == 0)
            {
                throw new ArgumentException("source sheet name not exist");
            }
            else
            {
                var tab = tabs.Where(s => s.Properties.Title == sourceSheetName);
                sourceSheetId = tab.First().Properties.SheetId.Value;
            }

            if (tabs.Where(s => s.Properties.Title == destSheetName).Count() == 0)
            {
                throw new ArgumentException("destination sheet name not exist");
            }
            else
            {
                var tab = tabs.Where(s => s.Properties.Title == destSheetName);
                destSheetId = tab.First().Properties.SheetId.Value;
            }
            #endregion

            #region 解析位置信息

            if (!IsValidRange(sourceStart) || !IsValidRange(sourceEnd) || !IsValidRange(destStart) || !IsValidRange(destEnd))
            {
                throw new ArgumentException("入参表格单元格位置不符合规则".I18n("controlType_SheetAdapter_3D2D68B1"));
            }
            var s1 = GetLetters(sourceStart);
            var s2 = GetLetters(sourceEnd);
            var d1 = GetLetters(destStart);
            var d2 = GetLetters(destEnd);

            int s_startRIndex = s1.Item2 - 1;
            int s_startCIndex = SheetRange.GetColumnIDFromLetters(s1.Item1) - 1;
            int s_endRIndex = s2.Item2;
            int s_endCIndex = SheetRange.GetColumnIDFromLetters(s2.Item1);

            int d_startRIndex = d1.Item2 - 1;
            int d_startCIndex = SheetRange.GetColumnIDFromLetters(d1.Item1) - 1;
            int d_endRIndex = d2.Item2;
            int d_endCIndex = SheetRange.GetColumnIDFromLetters(d2.Item1);

            #endregion

            BatchUpdateSpreadsheetRequest bc = new BatchUpdateSpreadsheetRequest();
            Request rq = new Request();

            rq.CopyPaste = new CopyPasteRequest();
            rq.CopyPaste.Source = new GridRange() { SheetId = sourceSheetId, StartColumnIndex = s_startCIndex, StartRowIndex = s_startRIndex, EndRowIndex = s_endRIndex, EndColumnIndex = s_endCIndex };
            rq.CopyPaste.Destination = new GridRange() { SheetId = destSheetId, StartColumnIndex = d_startCIndex, StartRowIndex = d_startRIndex, EndRowIndex = d_endRIndex, EndColumnIndex = d_endCIndex };
            //rq.CopyPaste.Source = new GridRange() { SheetId = sourceSheetId, StartColumnIndex = 0, StartRowIndex = 2, EndRowIndex = 4, EndColumnIndex = 2 };
            //rq.CopyPaste.Destination = new GridRange() { SheetId = destSheetId, StartColumnIndex = 0, StartRowIndex = 2, EndRowIndex = 4, EndColumnIndex = 2 };
            if (!onlyValue)
            {
                rq.CopyPaste.PasteType = "PASTE_NORMAL";
            }
            else
            {
                rq.CopyPaste.PasteType = "PASTE_VALUES";  //只复制值
            }
            rq.CopyPaste.PasteOrientation = "NORMAL";
            bc.Requests = new List<Request>();
            bc.Requests.Add(rq);

            return this.Service.Spreadsheets.BatchUpdate(bc, SpreadsheetID).Execute();
        }
        /// <summary>
        /// 更新sheet名
        /// </summary>
        /// <param name="oldSheetName"></param>
        /// <param name="newSheetName"></param>
        public BatchUpdateSpreadsheetResponse UpdateSheetName(string oldSheetName, string newSheetName)
        {
            #region  根据表名获取sheetId

            var spreadsheet = this.Service.Spreadsheets.Get(this.SpreadsheetID);

            var result = spreadsheet.Execute();

            var tabs = result.Sheets.ToList();

            int sourceSheetId;
            if (tabs.Where(s => s.Properties.Title == oldSheetName).Count() == 0)
            {
                throw new ArgumentException(" sheet name not exist");
            }
            else
            {
                var tab = tabs.Where(s => s.Properties.Title == oldSheetName);
                sourceSheetId = tab.First().Properties.SheetId.Value;
            }
            #endregion


            BatchUpdateSpreadsheetRequest bc = new BatchUpdateSpreadsheetRequest();
            Request rq = new Request();
            rq.UpdateSheetProperties = new UpdateSheetPropertiesRequest();
            rq.UpdateSheetProperties.Properties = new SheetProperties();
            rq.UpdateSheetProperties.Properties.SheetId = sourceSheetId;
            rq.UpdateSheetProperties.Properties.Title = newSheetName;
            rq.UpdateSheetProperties.Fields = "title";
            bc.Requests = new List<Request>();
            bc.Requests.Add(rq);

            return this.Service.Spreadsheets.BatchUpdate(bc, SpreadsheetID).Execute();
        }

        public BatchUpdateSpreadsheetResponse AddNewSheet(string newSheetName)
        {
            BatchUpdateSpreadsheetRequest bc = new BatchUpdateSpreadsheetRequest();
            Request rq = new Request();
            rq.AddSheet = new AddSheetRequest();
            rq.AddSheet.Properties.Title = newSheetName;
            rq.AddSheet.Properties.SheetType = "GRID";
            bc.Requests = new List<Request>();
            bc.Requests.Add(rq);
            return this.Service.Spreadsheets.BatchUpdate(bc, SpreadsheetID).Execute();
        }

        public BatchUpdateSpreadsheetResponse DeleteSheet(string sheetName)
        {
            int sheetId = (int)GetSheetInfo("Sheet2").Properties.SheetId;
            BatchUpdateSpreadsheetRequest bc = new BatchUpdateSpreadsheetRequest();
            Request rq = new Request();
            rq.DeleteSheet = new DeleteSheetRequest();
            rq.DeleteSheet.SheetId = sheetId;
            bc.Requests = new List<Request>();
            bc.Requests.Add(rq);
            return this.Service.Spreadsheets.BatchUpdate(bc, SpreadsheetID).Execute();
        }

        public UpdateValuesResponse UpdateOneCell(string sheetName, string range, string content)
        {
            ValueRange vr = new ValueRange();
            vr.MajorDimension = "ROWS";
            var oblist = new List<object>() { content };
            vr.Values = new List<IList<object>> { oblist };
            var request = this.Service.Spreadsheets.Values.Update(vr, this.SpreadsheetID, string.Format("{0}!{1}", sheetName, range));
            request.ValueInputOption = UpdateRequest.ValueInputOptionEnum.RAW;
            return request.Execute();
        }
        /// <summary>
        /// 插入行数据 demo
        /// </summary>
        public void UpdateCell()
        {
            ValueRange vr = new ValueRange();
            vr.MajorDimension = "ROWS";
            //IList<IList<object>> list = new List<IList<object>>();
            //list.Add(new List<object>() { '1', '3', '4' });
            //vr.Values = list;
            CellData cd = new CellData();
            //cd
            DataValidationRule dvr = new DataValidationRule();
            dvr.Condition = new BooleanCondition();
            dvr.Condition.Type = "BOOLEAN";
            dvr.Condition.Values = new List<ConditionValue>();
            dvr.Condition.Values.Add(new ConditionValue() { UserEnteredValue = "TRUE" });
            cd.DataValidation = dvr;

            var oblist1 = new List<object>() { "My Cell Text2", "MyTest 2" };
            var oblist2 = new List<object>() { "My Cell Text3", "MyTest 3" };
            vr.Values = new List<IList<object>> { oblist1, oblist2 };
            var request = this.Service.Spreadsheets.Values.Update(vr, this.SpreadsheetID, "Sheet1");
            request.ValueInputOption = UpdateRequest.ValueInputOptionEnum.RAW;
            request.Execute();
        }
        /// <summary>
        /// 批量插入checkbox demo
        /// </summary>
        public void RepeatCell()
        {
            RepeatCellRequest rc = new RepeatCellRequest();
            rc.Cell = new CellData();
            DataValidationRule dvr = new DataValidationRule();
            dvr.Condition = new BooleanCondition();
            dvr.Condition.Type = "BOOLEAN";
            dvr.Condition.Values = new List<ConditionValue>();
            dvr.Condition.Values.Add(new ConditionValue() { UserEnteredValue = "TRUE" });
            rc.Cell.DataValidation = dvr;
            rc.Cell.DataValidation = dvr;

            rc.Fields = "DataValidation";
            rc.Range = new GridRange() { SheetId = 0, StartRowIndex = 0, EndRowIndex = 10, StartColumnIndex = 6, EndColumnIndex = 7 };

            BatchUpdateSpreadsheetRequest bc = new BatchUpdateSpreadsheetRequest();
            bc.Requests = new List<Request>();
            Request rq = new Request();
            rq.RepeatCell = rc;
            bc.Requests.Add(rq);
            this.Service.Spreadsheets.BatchUpdate(bc, this.SpreadsheetID).Execute();
        }

        //public UpdateValuesResponse InsertHylink(string range,string content,string hylink,string sheetName)
        //{

        //}
        /// <summary>
        /// 拆分行列
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private Tuple<string, int> GetLetters(string location)
        {
            string letters = string.Empty;
            int resultTmp;
            foreach (char c in location)
            {
                bool result = int.TryParse(c.ToString(), out resultTmp);
                if (result != true)
                {
                    letters += c.ToString();
                }
            }
            string num = location.Replace(letters, "");

            return new Tuple<string, int>(letters, int.Parse(num));
        }

        public bool IsValidRange(string rangleCell)
        {
            return Regex.IsMatch(
              rangleCell,
              "[A-Z]+[0-9]",
              RegexOptions.IgnoreCase);
        }

        public Sheet GetSheetInfo(string sheetName)
        {
            var spreadsheet = this.Service.Spreadsheets.Get(this.SpreadsheetID);

            var result = spreadsheet.Execute();

            var tabs = result.Sheets.ToList();

            int sourceSheetId;
            if (tabs.Where(s => s.Properties.Title == sheetName).Count() == 0)
            {
                throw new ArgumentException(" sheet name not exist");
            }
            else
            {
                var tab = tabs.Where(s => s.Properties.Title == sheetName);
                //sourceSheetId = tab.First().Properties.SheetId.Value;
                return tab.First();
            }
            // return sourceSheetId;
        }
        /// <summary>
        /// 按行、列、区域写入数据
        /// </summary>
        /// <param name="range">写入区域</param>
        /// <param name="value">写入内容</param>
        /// <param name="dimension">写入方式</param>
        /// <returns></returns>
        public UpdateValuesResponse InsertCells(SheetRange range, object value, string dimension, int isFormula = 0)
        {
            var valueRange = new ValueRange();
            valueRange.Values = (IList<IList<object>>)value;
            if (dimension.ToUpper() == "COLUMNS")
                valueRange.MajorDimension = "COLUMNS";
            else
                valueRange.MajorDimension = "ROWS";
            UpdateRequest request = this.Service.Spreadsheets.Values.Update(valueRange, SpreadsheetID, range.A1Notation);
            if (isFormula == 1)//公式
                request.ValueInputOption = (UpdateRequest.ValueInputOptionEnum?)ValueInputOptionEnum.USERENTERED;
            else
                request.ValueInputOption = (UpdateRequest.ValueInputOptionEnum?)ValueInputOptionEnum.RAW;
            UpdateValuesResponse result = request.Execute();
            return result;
        }


        /// <summary>
        /// 切换sheet *暂未实现*
        /// </summary>
        /// <param name="oldSheetName"></param>
        /// <param name="newSheetName"></param>
        public BatchUpdateSpreadsheetResponse ChangeSheet(string oldSheetName, string newSheetName)
        {
            #region  根据表名获取sheetId

            var spreadsheet = this.Service.Spreadsheets.Get(this.SpreadsheetID);

            var result = spreadsheet.Execute();

            var tabs = result.Sheets.ToList();

            int sourceSheetId;
            if (tabs.Where(s => s.Properties.Title == oldSheetName).Count() == 0)
            {
                throw new ArgumentException(" sheet name not exist");
            }
            else
            {
                var tab = tabs.Where(s => s.Properties.Title == oldSheetName);
                sourceSheetId = tab.First().Properties.SheetId.Value;
            }
            #endregion


            BatchUpdateSpreadsheetRequest bc = new BatchUpdateSpreadsheetRequest();
            Request rq = new Request();
            rq.UpdateSheetProperties = new UpdateSheetPropertiesRequest();
            rq.UpdateSheetProperties.Properties = new SheetProperties();
            rq.UpdateSheetProperties.Properties.SheetId = sourceSheetId;
            rq.UpdateSheetProperties.Properties.Index = 2;
            rq.UpdateSheetProperties.Fields = "index";
            bc.Requests = new List<Request>();
            bc.Requests.Add(rq);

            return this.Service.Spreadsheets.BatchUpdate(bc, SpreadsheetID).Execute();
        }
        /// <summary>
        /// 单元格写入公式
        /// </summary>
        /// <param name="sheetName"></param>
        /// <param name="range"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public UpdateValuesResponse UpdateOneCellFormula(string sheetName, string range, string content)
        {
            ValueRange vr = new ValueRange();
            vr.MajorDimension = "ROWS";
            var oblist = new List<object>() { content };
            vr.Values = new List<IList<object>> { oblist };
            var request = this.Service.Spreadsheets.Values.Update(vr, this.SpreadsheetID, string.Format("{0}!{1}", sheetName, range));
            request.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
            return request.Execute();
        }
        /// <summary>
        /// 根据表头获取列数据
        /// </summary>
        /// <param name="sheetName"></param>
        /// <param name="rowNum"></param>
        /// <param name="Titles"></param>
        /// <returns></returns>
        public List<List<object>> GetColumnsByTableTitle(string sheetName, int rowNum, List<string> Titles)
        {
            //先获取行数据
            var rangeValue = $"{sheetName}!R[{rowNum - 1}]";
            GetRequest request = this.Service.Spreadsheets.Values.Get(this.SpreadsheetID, rangeValue);
            request.ValueRenderOption = GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
            request.DateTimeRenderOption = GetRequest.DateTimeRenderOptionEnum.FORMATTEDSTRING;

            ValueRange response = request.Execute();
            var rowContent = response.Values;
            List<List<object>> retList = new List<List<object>>();
            if (rowContent.Count() > 0)
            {
                int maxColumnLength = 1;
                int rowLength = rowContent[0].Count();
                foreach (var title in Titles)
                {
                    for (int num = 0; num < rowLength; num++)
                    {
                        if (title == rowContent[0][num].ToString())
                        {
                            int startColumnIndex = num;
                            int startRowIndex = rowNum;
                            string letter = Num2ABCletter(startColumnIndex + 1);
                            string range = $"{sheetName}!{letter}{startRowIndex}:{letter}";
                            request = this.Service.Spreadsheets.Values.Get(this.SpreadsheetID, range);
                            request.ValueRenderOption = GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
                            request.DateTimeRenderOption = GetRequest.DateTimeRenderOptionEnum.FORMATTEDSTRING;

                            ValueRange ColumnResponse = request.Execute();
                            if (ColumnResponse.Values.Count > 0)
                            {
                                maxColumnLength = maxColumnLength < ColumnResponse.Values.Count ? ColumnResponse.Values.Count : maxColumnLength;
                                IList<IList<object>> values = ColumnResponse.Values;
                                List<object> column = new List<object>();
                                foreach (var value in values)
                                {
                                    if (value.Count() > 0)
                                        column.Add(value[0]);
                                    else
                                        column.Add("");
                                }
                                retList.Add(column);
                            }
                            break;
                        }
                    }
                }
                foreach (var columns in retList)
                {
                    int needAddcolumn = maxColumnLength - columns.Count;
                    for (var i = 0; i < needAddcolumn; i++)
                    {
                        columns.Add("");
                    }
                }
            }
            return retList;
        }
        #region 刷选filterView功能
        //public BatchUpdateSpreadsheetResponse AddFilterView(string sheetName, string filterName, GridRange range)
        public BatchUpdateSpreadsheetResponse AddFilterView()
        {
            int sheetId = (int)GetSheetInfo("Sheet2").Properties.SheetId;
            BatchUpdateSpreadsheetRequest bc = new BatchUpdateSpreadsheetRequest();
            Request rq = new Request();

            IDictionary<string, FilterCriteria> keyValuePairs = new Dictionary<string, FilterCriteria>();
            keyValuePairs.Add("0", new FilterCriteria
            {
                Condition = new BooleanCondition
                {
                    Type = "NUMBER_EQ",
                    Values = new List<ConditionValue> {
                        new ConditionValue{ UserEnteredValue ="1"}
                    }
                }
            });
            AddFilterViewRequest filterrq = new AddFilterViewRequest();

            filterrq.Filter = new FilterView
            {
                Title = "测试刷选器".I18n("SheetAdapter_701B6ACF"),
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 0,
                    EndRowIndex = 3,
                    StartColumnIndex = 0,
                    EndColumnIndex = 3
                },
                Criteria = keyValuePairs
            };
            rq.AddFilterView = filterrq;
            bc.Requests = new List<Request>();
            bc.Requests.Add(rq);

            return this.Service.Spreadsheets.BatchUpdate(bc, SpreadsheetID).Execute();
        }
        public BatchUpdateSpreadsheetResponse UpdateFilterView()
        {
            string filterTitle = "";
            Sheet sheet = GetSheetInfo("Sheet2");
            int sheetId = (int)sheet.Properties.SheetId;
            BatchUpdateSpreadsheetRequest bc = new BatchUpdateSpreadsheetRequest();
            Request rq = new Request();

            IDictionary<string, FilterCriteria> keyValuePairs = new Dictionary<string, FilterCriteria>();
            keyValuePairs.Add("0", new FilterCriteria
            {
                Condition = new BooleanCondition
                {
                    Type = "NUMBER_EQ",
                    Values = new List<ConditionValue> {
                        new ConditionValue{ UserEnteredValue ="4"}
                    }
                }
            });
            UpdateFilterViewRequest filterrq = new UpdateFilterViewRequest();
            filterrq.Fields = "*"; //此字段不能为空
            FilterView filterView = sheet.FilterViews.Where(a => a.Title == filterTitle).FirstOrDefault();
            if (filterView == null)
            {
                throw new Exception("未找到对应的filterView".I18n("controlType_SheetAdapter_ACBA6649"));
            }
            filterrq.Filter = new FilterView
            {
                FilterViewId = filterView.FilterViewId,//Id为必须
                Title = "测试刷选器1".I18n("SheetAdapter_EE03C230"),
                Criteria = keyValuePairs
            };
            rq.UpdateFilterView = filterrq;
            bc.Requests = new List<Request>();
            bc.Requests.Add(rq);

            return this.Service.Spreadsheets.BatchUpdate(bc, SpreadsheetID).Execute();
        }
        public void DownloadSheetsFile(ParamDownloadSheetsModel param)
        {
            string spreadsheetId = SpreadsheetID;
            //if (!param.url.Contains("edit#gid="))
            //{
            //    throw new Exception("url格式有误");
            //}
            //string[] str = param.url.Split('=');
            //string sheetId = str[1];
            //string url = $"https://docs.google.com/spreadsheets/u/1/d/{spreadsheetId}/export?format={param.format}";
            //if (param.isOnlySheet == 1)
            //    url = $"https://docs.google.com/spreadsheets/u/1/d/{spreadsheetId}/export?format={param.format}&id={spreadsheetId}&gid={sheetId}";
            int lastMarkIndex = param.url.LastIndexOf("=");
            string sheetId = param.url.Substring(lastMarkIndex + 1);
            string url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format={param.format}";
            if (param.isOnlySheet == 1)
                url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format={param.format}&id={spreadsheetId}&gid={sheetId}";
            System.Diagnostics.Process.Start("chrome.exe", url);
        }
        #endregion
        #region 数字转换为excel中对应的列字母序号
        private static List<string> Level = new List<string>
        {
            "A","B","C","D","E","F","G","H","I","J",
            "K","L","M","N","O","P","Q","R","S","T",
            "U","V","W","X","Y","Z",
        };
        /// <summary>
        /// 数字转ABC字母
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Num2ABCletter(int value)
        {
            value = value - 1;
            System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
            int remainder = value % 26;//余数
            int front = (value - remainder) / 26;//
            if (front == 0)
            {
                return Level[remainder];
            }
            else if (front < 26)
            {
                return Level[front - 1] + Level[remainder];
            }
            else
            {
                return Num2ABCletter(front) + Level[remainder];
            }
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SheetAdapter<T> : SheetAdapter where T : BaseRecord
    {
        public SheetAdapter(string spreadsheetID, string serviceAccountEmail, string tabName)
            : base(spreadsheetID, serviceAccountEmail, tabName) { }

        public BatchUpdateSpreadsheetResponse AppendRow(T record)
        {
            return this.AppendRows(new List<T>() { record });
        }

        public BatchUpdateSpreadsheetResponse AppendRows(List<T> records)
        {
            var rows = new List<RowData>();

            foreach (var record in records)
            {
                var row = new RowData
                {
                    Values = record.ConvertToCellData(this.TabName).Select(b => b.Data).ToList(),
                };

                rows.Add(row);
            }

            var appendRequest = new AppendCellsRequest
            {
                Fields = "*",
                SheetId = this.SheetID,
                Rows = rows
            };

            Request request = new Request
            {
                AppendCells = appendRequest
            };

            // Wrap it into batch update request.
            BatchUpdateSpreadsheetRequest batchRequst = new BatchUpdateSpreadsheetRequest
            {
                Requests = new[] { request }
            };

            // Finally update the sheet.
            return this.Service.Spreadsheets
                .BatchUpdate(batchRequst, this.SpreadsheetID)
                .Execute();
        }
    }
}
