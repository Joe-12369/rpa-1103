using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace II.RPA.Plugin.GUI.ProcessCreator.Models
{
    public class RootModel : ModelBase
    {
        private string searchString;
        public RootType Type
        {
            get;
            set;
        }
        [JsonProperty("variableSource")]
        public RootSource Source
        {
            get;
            set;
        }
        [JsonProperty("label")]
        public string Name
        {
            get;
            set;
        }
        public bool HasMember
        {
            get;
            set;
        }
        public bool HasMatch
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
        private ObservableCollection<VariableModel> variableCollection;
        [JsonProperty("treeData")]
        public ObservableCollection<VariableModel> VariableCollection
        {
            get
            {
                return variableCollection;
            }
            set
            {
                variableCollection = value;
                MemeberCount = variableCollection != null ? variableCollection.Count : 0;
                HasMember = MemeberCount != 0;
                //if (HasMember)
                //{
                //    foreach (var variable in variableCollection)
                //    {
                //        variable.Source = Source;
                //    }
                //}
                VariableCollectionView = CollectionViewSource.GetDefaultView(VariableCollection);
                VariableCollectionView.CollectionChanged += VariableCollectionView_CollectionChanged;
                VariableCollectionView.Filter = ContainsFilter;
            }
        }

        private void VariableCollectionView_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var currentItems = VariableCollectionView.OfType<VariableModel>();
            if (currentItems != null && currentItems.Any())
            {
                MemeberCount = currentItems.Count();
            }
            else
            {
                MemeberCount = 0;
            }
        }

        public ICollectionView VariableCollectionView
        {
            get;
            set;
        }
        private bool ContainsFilter(object item)
        {
            if (item is VariableModel variable)
            {
                if (string.IsNullOrEmpty(searchString))
                {
                    return true;
                }
                if (!string.IsNullOrEmpty(searchString)
                    && variable.Title.Contains(searchString))
                {
                    HasMatch = true;
                    return true;
                }
                else
                {
                    if (variable.HasMember && variable.MemeberCollection.Any(m => m.Title.Contains(searchString)))
                    {
                        HasMatch = true;
                        variable.IsExpanded = true;
                        return true;
                    }
                }
                variable.IsExpanded = false;
            }
            return false;
        }
        public void SetFilter(string aSearchString)
        {
            searchString = aSearchString;
            foreach (var item in VariableCollection)
            {
                item.SetFilter(searchString);
                if (string.IsNullOrEmpty(searchString))
                {
                    item.IsExpanded = true;
                }
            }
            VariableCollectionView.Refresh();
        }
    }
}
