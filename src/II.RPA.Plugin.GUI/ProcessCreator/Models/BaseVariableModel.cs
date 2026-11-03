using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.ProcessCreator.Models
{
    public class BaseVariableModel : ModelBase
    {
        private bool isSelected;
        public bool IsSelected
        {
            get 
            { 
                return isSelected; 
            }
            set
            {
                isSelected = value;
                RaisePropertyChanged("IsSelected");
            }
        }
        [JsonProperty("disabled")]
        public bool Disabled
        {
            get { return !IsEnabled; }
            set { IsEnabled = !value; }
        }
        private bool isEnabled = true;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set 
            {
                isEnabled = value;
                RaisePropertyChanged("IsEnabled");
            }
        }
        [JsonProperty("title")]
        public string Title
        {
            get;
            set;
        }
        [JsonProperty("key")]
        public string NodeKey
        {
            get;
            set;
        }
        public RootSource Source
        {
            get;
            set;
        }
    }
}
