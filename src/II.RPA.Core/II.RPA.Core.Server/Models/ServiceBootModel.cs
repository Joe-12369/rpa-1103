using System;

namespace II.RPA.Core.Server.Models
{
    public class ServiceBootModel
    {
        public string ServiceBootName
        {
            get;
            set;
        }
        public Type ServiceBootType
        {
            get;
            set;
        }
        public object[] ServiceBootParams
        {
            get;
            set;
        }
    }
}
