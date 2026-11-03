using II.RPA.Plugin.GUI.ProcessCreator;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamProcessCreatorModel:ParamBase
    {
        [JsonProperty("action")]
        public CreatorActionType CreatorAction
        {
            get;
            set;
        }
        [JsonProperty("cv_server_url")]
        public string CV_Server_Url
        {
            get;
            set;
        }
        [JsonProperty("filepath")]
        public string ResPath
        {
            get;
            set;
        }
        [JsonProperty("componentList")]
        public List<ComponentModel> ComponentList
        {
            get;
            set;
        }
        [JsonProperty("categoryList")]
        public List<CategoryModel> CategoryList
        {
            get;
            set;
        }
        [JsonProperty("elementTypes")]
        public string ElementTypes
        {
            get;
            set;
        }
        [JsonProperty("type")]
        public CaptureModeType CaptureModeType
        {
            get;
            set;
        }
        [JsonProperty("packagepath")]
        public string PackagePath
        {
            get;
            set;
        }
        [JsonProperty("factoryWindowHandle")]
        public string FactoryWindowHandle
        {
            get;
            set;
        }
        [JsonProperty("clientType")]
        public string ClientType
        {
            get;
            set;
        }
        [JsonProperty("IsCvSupport")]
        public int IsCvSupport
        {
            get;
            set;
        }
        [JsonProperty("addition")]
        public string Addition
        {
            get;
            set;
        }
    }
    public enum CreatorActionType
    {
        //creator.start
        START = 0,
        //creator.end
        END = 1,
        //creator.pause
        PAUSE = 2,
        //creator.resume
        RESUME = 3,
        //creator.restart
        RESTART = 4,
        //creator.resetaction
        RESET_ACTION = 5,
        //
        INSTEAD_ELEMENT = 6,
        //
        CANCEL_INSTEAD_ELEMENT = 8,
        //截图模式
        CAPTURE_IMAGE = 7,
        //重新截图
        CAPTURE_IMAGE_ONCETIME = 9,
        //初始化
        INIT = 10,
        //状态查询
        GET_STATUS = 11,
        //启动Excel录制
        START_EXCEL_RECORD = 12,
        //停止Excel录制
        STOP_EXCEL_RECORD = 13,
        //启用cv拾取
        ENABLE_CV = 14,
        //
        DUMMY_INIT = 99,
    }
}
