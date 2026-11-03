using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Observable.Scheduling
{
    public interface IControllable
    {
        void Start();

        void Stop();

        void Restart();
    }
}
