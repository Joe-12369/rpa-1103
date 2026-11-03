using II.RPA.Plugin.Core.PluginBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DesktopInput.Mouse.Config
{
    public class MouseConfigurations : ConfigurationBase<MouseConfigurations>
    {
        private MouseConfigurations()
        { 
        }
        /// <summary>
        /// 移动速度
        /// </summary>
        public double MovingSpeed { get; set; } = 10;
        //匀速
        public double UniformSpeed { get; set; } = 0.5;
        //变速
        public List<double> VariableSpeed { get; set; }
        //移动速度临界值
        public int CriticalSpeed { get; set; } = 60;
        //移动像素临界值
        public double CriticaPixel { get; set; } = 3.0;
        //随机移动分成段数
        public int Step { get; set; } = 5;
        //随机偏移最大像素
        public int RandomMoveMax { get; set; } = 200;
        //随机偏移最小像素
        public int RandomMoveMin { get; set; } = -200;
        //随机最大速度
        public double RandomSpeedMax { get; set; } = 3.0;
        //随机最小速度
        public double RandomSpeedMin { get; set; } = 0.1;
    }
}
