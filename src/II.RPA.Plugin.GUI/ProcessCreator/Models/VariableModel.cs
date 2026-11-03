using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace II.RPA.Plugin.GUI.ProcessCreator.Models
{
    public class VariableModel:BaseVariableModel
    {
        private string searchString;
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

        private string lineNumber;
        [JsonProperty("lineNumber")]
        public string LineNumber
        {
            get
            {
                return lineNumber;
            }
            set
            {
                lineNumber = value;
                RaisePropertyChanged("LineNumber");
            }
        }
        private string componentName;
        [JsonProperty("componentName")]
        public string ComponentName
        {
            get
            {
                return componentName;
            }
            set
            {
                componentName = value;
                RaisePropertyChanged("ComponentName");
            }
        }
        public bool HasMember
        {
            get;
            set;
        }
        private bool isExpanded;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                isExpanded = value;
                RaisePropertyChanged("IsExpanded");
            }
        }
        private int memeberCount;
        public int MemeberCount
        {
            get { return memeberCount; }
            set
            {
                memeberCount = value;
                RaisePropertyChanged("MemeberCount");
            }
        }
        private ObservableCollection<SubVariableModel> memeberCollection;
        [JsonProperty("treeNodes")]
        public ObservableCollection<SubVariableModel> MemeberCollection
        {
            get 
            { 
                return memeberCollection; 
            }
            set 
            { 
                memeberCollection = value;
                MemeberCount = memeberCollection != null ? memeberCollection.Count : 0;
                HasMember = MemeberCount != 0;
                //if (HasMember)
                //{
                //    foreach (var variable in memeberCollection)
                //    {
                //        variable.Source = Source;
                //    }
                //}
                MemeberCollectionView = CollectionViewSource.GetDefaultView(MemeberCollection);
                MemeberCollectionView.Filter = ContainsFilter;
            }
        }
        public ICollectionView MemeberCollectionView
        {
            get;
            set;
        }
        private bool ContainsFilter(object item)
        {
            if (item is SubVariableModel variable)
            {
                //variable.TypeString = string.Join("/", variable.VarTypeList.Select(v => v.Label));
                if (string.IsNullOrEmpty(searchString))
                {
                    return true;
                }
                if (variable.Title.Contains(searchString))
                {
                    return true;
                }
            }
            return false;
        }
        public void SetFilter(string aSearchString)
        {
            if (MemeberCollectionView != null)
            {
                searchString = aSearchString;
                MemeberCollectionView.Refresh();
                //TypeString = string.Join("/",VarTypeList.Select(v=>v.Label));
            }
        }
    }
}
