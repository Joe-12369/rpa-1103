using II.RPA.MobileDeviceManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Models
{
    public class DeviceInfo : ModelBase
    {
        private string udid;
        public string Udid
        {
            get
            {
                return udid;
            }
            set
            {
                this.udid = value;
                this.RaisePropertyChanged("Udid");
            }
        }
        private bool isReady;
        [Newtonsoft.Json.JsonIgnore()]
        public bool IsReady
        {
            get
            {
                return isReady;
            }
            set
            {
                this.isReady = value;
                this.RaisePropertyChanged("IsReady");
            }
        }
        private string model;
        public string Model
        {
            get
            {
                return model;
            }
            set
            {
                this.model = value;
                this.RaisePropertyChanged("Model");
            }
        }

        private string version;
        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                this.version = value;
                this.RaisePropertyChanged("Version");
            }
        }

        private string brand;
        public string Brand
        {
            get
            {
                return brand;
            }
            set
            {
                this.brand = value;
                this.RaisePropertyChanged("Brand");
            }
        }

        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                this.name = value;
                this.RaisePropertyChanged("Name");
            }
        }

        private bool isConnected;
        [Newtonsoft.Json.JsonIgnore()]
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                this.isConnected = value;
                this.RaisePropertyChanged("IsConnected");
            }
        }
    }
}
