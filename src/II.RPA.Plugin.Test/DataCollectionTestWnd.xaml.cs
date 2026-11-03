using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.ComponentModel;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Plugin.Core;
using System.IO;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System.Data;
using II.RPA.Logging;
using System.Diagnostics;
using II.RPA.Plugin.GUI.Commons.Params;

namespace II.RPA.Plugin.Test
{
    /// <summary>
    /// DataCollectionTestWnd.xaml 的交互逻辑
    /// </summary>
    public partial class DataCollectionTestWnd : Window
    {
        private object _pluginLoaderObject = null;
        private Type _pluginLoaderType = null;
        private MethodInfo methodRun = null;
        private MethodInfo methodRegisterMessageServiceSendMessage = null;
        private DataCollectionTestViewModel viewMode;

        public DataCollectionTestWnd(object pluginLoaderObject, Type pluginLoaderType)
        {
            InitializeComponent();
            _pluginLoaderObject = pluginLoaderObject;
            _pluginLoaderType = pluginLoaderType;
            methodRun = _pluginLoaderType.GetMethod("RunAsync");
            methodRegisterMessageServiceSendMessage = _pluginLoaderType.GetMethod("RegisterMessageServiceSendMessage");
            Func<TerminalType, string, object, string> func = SendMessageToTerminal;
            methodRegisterMessageServiceSendMessage.Invoke(_pluginLoaderObject,
               new[] {
                   func
               });
            viewMode = new DataCollectionTestViewModel();
            this.DataContext = viewMode;
        }

        private const string FACTORY_ACTION_COLLECTDATA_GETCOLLECTRESULT = "collectData.getCollectResult";
        private const string FACTORY_ACTION_SHOWWINDOWDIALOG = "showWindowDialog";
        private const string FACTORY_ACTION_COLLECTDATA_RECEIVEEXCEPTION = "collectData.receiveException";
        private const string FACTORY_ACTION_COLLECTDATA_RECEIVEESCCOMMAND = "collectData.receiveEscCommand";
        public string SendMessageToTerminal(TerminalType terminalType, string action, object objData)
        {
            var ret = objData.ToString();
            
            viewMode.ResultStr = ConvertJsonString(ret);
            switch (action)
            {
                case FACTORY_ACTION_COLLECTDATA_GETCOLLECTRESULT:
                    if (string.IsNullOrEmpty(viewMode.ConfigData))
                    {
                        viewMode.ConfigData = viewMode.ResultStr;
                    }
                    else {
                        var configDataObj = JObject.Parse(viewMode.ConfigData);
                        var retObj = JObject.Parse(viewMode.ConfigData);
                        ((JArray)configDataObj[@"collectionConfig\columnSelectorArray"]).Add(retObj[@"collectionConfig\columnSelectorArray"][0]);
                        ((JArray)configDataObj[@"collectionConfig\columnConfigArray"]).Add(retObj[@"collectionConfig\columnConfigArray"][0]);
                        viewMode.ConfigData = configDataObj.ToString();
                    }
                    break;
                case FACTORY_ACTION_SHOWWINDOWDIALOG:
                    JObject jObj = JObject.Parse(ret);
                    MessageBoxResult result = MessageBox.Show(jObj["content"]?.ToString(), jObj["title"]?.ToString(), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    JMessage message = new JMessage();
                    if (result == MessageBoxResult.Yes)
                    {
                        message.Info = new JObject() { { "eventId", "UseTableDialog_CollectTable" } }.ToString();
                    }
                    else
                    {
                        message.Info = new JObject() { { "eventId", "UseTableDialog_CollectElement" } }.ToString();
                    }
                    return message.ToJsonNoneFormating();
                    break;
                    //case FACTORY_ACTION_COLLECTDATA_GETCOLLECTRESULT:
                    //case FACTORY_ACTION_COLLECTDATA_GETCOLLECTRESULT:
                    //    this.WindowState = WindowState.Minimized;
                    //    break;

            }
            return ret;
        }

        private void statCapture_Click(object sender, RoutedEventArgs e)
        {
            //this.WindowState = WindowState.Minimized;
            var ret = RunPluginAction("Element.DataCollectCapture", new
            {
                actionCmd = "Start",
                similarSelector = new List<object>() { }
            });
        }

        private void endCapture_Click(object sender, RoutedEventArgs e)
        {
            var ret = RunPluginAction("Element.DataCollectCapture", new
            {
                actionCmd = "Stop",
                similarSelector = new List<object>() { }
            });
        }

        private void colectionData_Click(object sender, RoutedEventArgs e)
        {
            string pathStr = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datajson");
            if (string.IsNullOrEmpty(viewMode.ParamStr))
            {
                var param = new
                {
                    RequestId = "RequestId",
                    DataFilePath = pathStr,
                    CollectDataCount = 0,
                    TurnPageCount = 1,
                    DataCollectConfig = viewMode.ConfigData.ToObject<RetDataCollectConfigurationModel_v2>(),
                    ColorGuidanceConfig = new ColorGuidanceConfig()
                    {
                        ActionCmd = "preview",
                        RowIndex = -1,
                        ColumnIndex = -1
                    }
                };
                viewMode.ParamStr = param.ToJson();
            }
            RunColectionData(pathStr);
        }
        private void colectionData_run_Click(object sender, RoutedEventArgs e)
        {
            string pathStr = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datajson");
            if (string.IsNullOrEmpty(viewMode.ParamStr))
            {
                var param = new
                {
                    RequestId = "RequestId",
                    DataFilePath = pathStr,
                    CollectDataCount = 0,
                    TurnPageCount = 1,
                    DataCollectConfig = viewMode.ConfigData.ToObject<RetDataCollectConfigurationModel_v2>(),
                };
                viewMode.ParamStr = param.ToJson();
            }
            RunColectionData(pathStr);
        }
        private void colectionData_WaterfallFlow_Click(object sender, RoutedEventArgs e)
        {
            string pathStr = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datajson");
            if (string.IsNullOrEmpty(viewMode.ParamStr))
            {
                var param = new
                {
                    RequestId = "RequestId",
                    DataFilePath = pathStr,
                    CollectDataCount = int.Parse(ColectionCount.Text),
                    TurnPageCount = 0,
                    DataCollectConfig = viewMode.ConfigData.ToObject<RetDataCollectConfigurationModel_v2>()
                };
                viewMode.ParamStr = param.ToJson();
            }
            RunColectionData(pathStr);
        }
        private void RunColectionData(string pathStr)
        {
            var ret = RunPluginAction("Element.DataCollect_v2", JObject.Parse(viewMode.ParamStr));


            JMessage msg = ret.ToObject<JMessage>();
            var fileList = msg.Info.ToObject<string[]>();
            if (fileList == null || fileList.Length == 0)
            {
                return;
            }
            var collectDataStr = File.ReadAllText(System.IO.Path.Combine(pathStr, fileList.First()));
            var collectDataList = collectDataStr.ToObject<List<List<string>>>();
            if (collectDataList?.Count > 0)
            {
                DataTable dt = new DataTable("newtable");
                for (int i = 0; i < collectDataList[0].Count; i++)
                {
                    var cName = string.IsNullOrEmpty(collectDataList[0][i]) ? $"column{i}" : collectDataList[0][i];
                    dt.Columns.Add(cName, System.Type.GetType("System.String"));
                }
                for (int i = 1; i < collectDataList.Count; i++)
                {
                    DataRow row = dt.NewRow();
                    row.ItemArray = collectDataList[i].ToArray();
                    dt.Rows.Add(row);
                }
                dgCollectData.DataContext = dt;
            }
        }

        private string RunPluginAction(string plugin, object paramObj)
        {
            viewMode.ResultStr = "正在执行中。。。";
            string result = methodRun.Invoke(_pluginLoaderObject,
                new object[] {
                    plugin, "Excute",paramObj.ToJson()
                }).ToString();
            viewMode.ResultStr = ConvertJsonString(result);
            return result;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            endCapture_Click(null, null);
        }

        #region JSON格式化方法
        private string ConvertJsonString(string str)
        {
            //格式化json字符串
            JsonSerializer serializer = new JsonSerializer();
            using (TextReader tr = new StringReader(str))
            {

                using (JsonTextReader jtr = new JsonTextReader(tr))
                {
                    object obj = serializer.Deserialize(jtr);
                    if (obj != null)
                    {
                        using (StringWriter textWriter = new StringWriter())
                        {

                            using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                            {
                                Formatting = Formatting.Indented,
                                Indentation = 4,
                                IndentChar = ' '
                            })
                            {
                                serializer.Serialize(jsonWriter, obj);
                                return textWriter.ToString();
                            }
                        }
                    }
                    else
                    {
                        return str;
                    }
                }
            }
        }

        #endregion

       
    }


    public class DataCollectionTestViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private string paramStr = string.Empty;
        public string ParamStr
        {
            get { return paramStr; }
            set
            {
                paramStr = value;
                this.RaisePropertyChanged("ParamStr");
            }
        }
        private string resultStr = string.Empty;
        public string ResultStr
        {
            get { return resultStr; }
            set
            {
                resultStr = value;
                this.RaisePropertyChanged("ResultStr");
            }
        }
        private string configData = string.Empty;
        public string ConfigData
        {
            get { return configData; }
            set
            {
                configData = value;
                this.RaisePropertyChanged("ConfigData");
            }
        }
        private DataView collectData = null;
        public DataView CollectData
        {
            get { return collectData; }
            set
            {
                collectData = value;
                this.RaisePropertyChanged("CollectData");
            }
        }

    }
}
