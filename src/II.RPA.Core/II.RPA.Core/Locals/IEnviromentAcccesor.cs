using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Locals
{
    public interface IEnviromentAccesor
    {
        public void KillProcessAndChildren(
            int pid,
            bool ignoreRoot = false);
    }
}
