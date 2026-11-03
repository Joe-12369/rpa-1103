using Newtonsoft.Json;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.ProcessCreator.Models
{
    public class SubVariableModel:BaseVariableModel
    {
        private string typeString;
        public string TypeString
        {
            get
            {
                return typeString;
            }
            set
            {
                typeString = value;
                RaisePropertyChanged("TypeString");
            }
        }

        [JsonProperty("varType")]
        public List<VarTypeModel> VarTypeList
        {
            get;
            set;
        }
        [JsonProperty("name")]
        public string Name
        {
            get;
            set;
        }
    }
}
