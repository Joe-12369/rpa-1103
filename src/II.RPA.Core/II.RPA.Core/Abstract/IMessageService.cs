using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Abstract
{
    public interface IMessageService
    {
        IMessageNotify VisualDataMessageNofify
        {
            get;
            set;
        }
    }
}
