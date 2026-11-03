using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.Common
{
    public class GrpcPollyPolicyOptions
    {
        public int CircuitBreakerCount
        {
            get;
            set;
        }
        public TimeSpan CircuitBreakerTime
        {
            get;
            set;
        }
        public TimeSpan Timeout
        {
            get;
            set;
        }
        public int RetryCount
        {
            get;
            set;
        }
        public TimeSpan RetryInterval
        {
            get;
            set;
        }

    }
}
