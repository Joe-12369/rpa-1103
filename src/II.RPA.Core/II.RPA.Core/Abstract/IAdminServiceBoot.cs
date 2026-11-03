using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Abstract
{
    public interface IAdminServiceBoot
    {
        void AuthToManageServer(IServerContext aServerContext);
    }
}
