using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params.DataCollect
{
    public class ColNameStruct
    {
        public int HasSet = 0;//是否进行了值列名的设置 0 为未设置;1 为进行了设置
        public string ColName = string.Empty;
        public ColNameStruct() { }
        public ColNameStruct(int hasSet, string colName)
        {
            HasSet = hasSet;
            ColName = colName;
        }
    }

    /// <summary>
    /// 数据采集最终返回结果类
    /// </summary>
    public class RetDataCollectConfigurationModel
    {
        public RetDataCollectConfigurationModel()
        {
            this.turnPageButtonInfo = new DataCollectConfiguration_TurnPageButtonInfo();
            this.pageInfo = new DataCollectConfiguration_PageInfo();
            this.collectInfo = new DataCollectConfiguration_CollectInfo();
        }
        /// <summary>
        /// 翻页按钮配置信息
        /// </summary>
        public DataCollectConfiguration_TurnPageButtonInfo turnPageButtonInfo { get; set; }
        /// <summary>
        /// 网页配置信息
        /// </summary>
        public DataCollectConfiguration_PageInfo pageInfo { get; set; }
        /// <summary>
        /// 列元素集合配置信息
        /// </summary>
        public DataCollectConfiguration_CollectInfo collectInfo { get; set; }

    }
    public class DataCollectConfiguration_TurnPageButtonInfo
    {
        /// <summary>
        /// selector 版本号
        /// </summary>
        public string nextBtnSelectorVersion { get; set; } = string.Empty;
        public List<SelectorProps> nextBtnSelector { get; set; } = new List<SelectorProps>();

        /// <summary>
        /// 元素截图地址
        /// </summary>
        public string filePath { get; set; } = string.Empty;

        /// <summary>
        /// 延迟拾取，毫秒
        /// </summary>
        public int delay { get; set; } = 1000;

        /// <summary>
        /// 页面垂直滚动距离，px,0无滚动
        /// </summary>
        public int scrollVertical { get; set; } = 0;

        /// <summary>
        /// 0:所有页，1当前页，n 具体页数
        /// </summary>
        public int pageCount { get; set; } = 1;

        public TurnPageConfigType turnPageConfigType { get; set; } = TurnPageConfigType.CurrentPage;
        /// <summary>
        /// 滚动延迟时间 单位毫秒，默认200
        /// </summary>
        public int scrollDelay { get; set; } = 200;

        /// <summary>
        /// 点击元素删除按钮 清空翻页元素信息
        /// </summary>
        public void DeleteTurnPageElement()
        {
            nextBtnSelector = new List<SelectorProps>();
            filePath = string.Empty;
        }

    }
    public class DataCollectConfiguration_PageInfo
    {
        public string url { get; set; } = string.Empty; //网页url
        public string title { get; set; } = string.Empty; //网页 名称
        public string guiType { get; set; } = string.Empty; // 
    }
    public class DataCollectConfiguration_CollectInfo
    {
        public string type { get; set; } = "tag";
        public List<DataCollectConfiguration_CollectInfo_Item> data { get; set; } = new List<DataCollectConfiguration_CollectInfo_Item>();

        /// <summary>
        /// 移动元素次序
        /// </summary>
        /// <param name="_Index">待改动的元素索引</param>
        /// <param name="isForeward">前移或者后移 ture 前移一位 false 后移一位</param>
        public void SetOrder(int _Index, bool isForeward)
        {
            if (data.Count <= _Index)
            {
                return;
            }

            if (_Index == 0 && isForeward == true)
            {
                return;
            }

            if (_Index == data.Count - 1 && isForeward == false)
            {
                return;
            }

            if (isForeward)
            {
                var item = data[_Index];
                data.RemoveAt(_Index);
                data.Insert(_Index - 1, item);
            }
            else
            {
                var item = data[_Index];
                data.RemoveAt(_Index);
                data.Insert(_Index + 1, item);
            }
        }


        /// <summary>
        /// 去除前缀
        /// </summary>
        public void RemoveAllPrefix()
        {
            for (int i = 0; i < data.Count; i++)
            {
                string fullName = data[i].title;
                string nameTail = String.Empty;
                if (fullName.Contains(" "))
                {
                    int Cindex = fullName.IndexOf(" ");
                    nameTail = fullName.Substring(Cindex + 1, fullName.Length - Cindex - 1);
                }

                data[i].title = nameTail;
            }
        }

        /// <summary>
        /// 生成列名队列
        /// </summary>
        /// <returns></returns>
        public List<string> GenerateAllColNameList()
        {
            List<string> _NameList = new List<string>();
            foreach (var item in data)
            {
                _NameList.Add(item.title);
            }
            return _NameList;
        }
    }

    /// <summary>
    /// 列元素 
    /// </summary>
    public class DataCollectConfiguration_CollectInfo_Item
    {
        /// <summary>
        /// selector 版本号
        /// </summary>
        public string version { get; set; } = string.Empty;
        public List<SelectorProps> selector { get; set; } = new List<SelectorProps>();
        /// <summary>
        /// 元素截图地址
        /// </summary>
        public string filePath { get; set; } = string.Empty;
        /// <summary>
        /// 列名
        /// </summary>
        public string title { get; set; } = string.Empty;

        /// <summary>
        /// 列名集合 专门针对普通表格
        /// </summary>
        public List<ColNameStruct> CommonTalbleTitleList = new List<ColNameStruct>();

        /// <summary>
        /// 列编辑所选定的内容类型  文本 链接 or 图片
        /// </summary>
        public CollectType dataType { get; set; } = CollectType.Content;
        public JObject ExtendedInfo { get; set; } = new JObject();
        public string regular { get; set; } = string.Empty;

        public List<RegularConfig> RegularConfigs = new List<RegularConfig>();

        public List<SelectorProps> lastCaptureSelector { get; set; } = new List<SelectorProps>();

        public void SetRegular(string strRegularRegex)
        {
            regular = strRegularRegex;
            int _Index = RegularConfigs.FindIndex(P => P.dataType == dataType);
            if (_Index >= 0)
            {
                RegularConfigs[_Index].regular = strRegularRegex;
            }
            else
            {
                RegularConfigs.Add(new RegularConfig() { dataType = dataType, regular = strRegularRegex });
            }
        }




        public bool GetRegularRegex(ref RegularConfig _RegularConfig)
        {
            _RegularConfig = null;
            if (RegularConfigs.Count > 0)
            {
                _RegularConfig = RegularConfigs.Find(P => P.dataType == dataType);
                if (_RegularConfig != null)
                {
                    return true;
                }
            }

            return false;
        }

        public string GetRegularRegex()
        {
            return regular;
        }

        public string GetRegularRegex(CollectType _CollectType)
        {
            if (RegularConfigs.Count > 0)
            {
                var _RegularConfig = RegularConfigs.Find(P => P.dataType == _CollectType);
                if (_RegularConfig != null)
                {
                    return _RegularConfig.regular;
                }
            }

            return null;
        }
    }

    public class RegularConfig
    {
        /// <summary>
        /// 列编辑所选定的内容类型  文本 链接 or 图片
        /// </summary>
        public CollectType dataType { get; set; } = CollectType.Content;

        public string regular { get; set; } = string.Empty;
    }


}
