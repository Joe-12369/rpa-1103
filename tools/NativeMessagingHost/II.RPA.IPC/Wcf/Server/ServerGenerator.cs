using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using II.RPA.IPC.Config;

namespace II.RPA.IPC
{
    public class ServerGenerator
    {
        static ServerGenerator()
        {
            Instance = new ServerGenerator();
        }

        private ServerGenerator()
        { }

        public static ServerGenerator Instance
        {
            get;
            private set;
        }

        public IServer Generate(IServerBuilder builder)
        {
            if (builder == null)
            {
                throw new NullReferenceException("The server builder cannot be null!");
            }

            IServer server = builder.Build();

            return server;
        }
    }
}
