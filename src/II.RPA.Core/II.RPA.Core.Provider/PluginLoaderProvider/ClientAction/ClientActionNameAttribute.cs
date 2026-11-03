using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    public class ClientActionNameAttribute : Attribute
    {
        public ClientActionNameAttribute(string aName)
        {
            ClientAction = aName;
        }
        public string ClientAction
        {
            get;
            private set;
        }
    }
}
