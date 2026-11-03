using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Logging.Configurations
{
    public class RaceObject
    {
        public DateTime SuccessTime { get; private set; }

        public void SetSuccess()
        {
            SuccessTime = DateTime.Now;
        }

        public bool IsExpired(TimeSpan timeSpan)
        {
            return DateTime.Now - SuccessTime > timeSpan;
        }
    }
}