using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.ZBrainProxyProvider
{
    public class ZBrainResponse<T>
    {
        public string Code { get; set; } = "0";

        public string Status { get; set; } = "success";

        public string Message { get; set; } = string.Empty;

        public T Result { get; set; } = default;
    }
}
