using System.Threading.Tasks;

namespace II.RPA.Core.Abstract
{
    public interface IServiceBoot
    {
        //BootSetting BootSetting 
        //{  
        //    get;
        //    set;
        //}
        int Port
        {
            get;
            set;
        }
        string IP
        {
            get;
            set;
        }
        string BootName
        {
            get;
        }
        Task StartAsync();
        void StopServerOnly();
        Task StopAsync(int aDelayMilliSecond);
        Task ReStartAsync(int aStopDelayMilliSecond);
        //bool SetEndPoint(string anIP,int aPort);

    }
}
