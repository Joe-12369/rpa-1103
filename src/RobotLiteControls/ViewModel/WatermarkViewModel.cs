using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotLiteControls.ViewModel
{
   public class WatermarkViewModel:ViewModelBase
    {
        public WatermarkViewModel()
        {
            this.WatermarkText = "请输入";
        }
        #region Text Property

        /// <summary>
        /// Private member backing variable for <see cref="Text" />
        /// </summary>
        private String _Text = null;

        /// <summary>
        /// Gets and sets the text associated with our demo textbox
        /// </summary>
        public String Text
        {
            get
            {
                if (_Text == null)
                { _Text = String.Empty; }

                return _Text;
            }
            set { Set(() => Text, ref _Text, value); }
        }

        #endregion

        #region WatermarkText Property

        /// <summary>
        /// Private member backing variable for <see cref="WatermarkText" />
        /// </summary>
        private String _WatermarkText = null;

        /// <summary>
        /// Gets and sets the text of the watermark
        /// </summary>
        public String WatermarkText
        {
            get
            {
                if (_WatermarkText == null)
                { _WatermarkText = String.Empty; }

                return _WatermarkText;
            }
            set { Set(() => WatermarkText, ref _WatermarkText, value); }
        }

        #endregion
    }
}
