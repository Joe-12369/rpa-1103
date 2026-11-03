using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace II.RPA.Plugin.GUI.HighLight.ViewModels
{
    public class HLAgentWndViewModel : ModelBase
    {

        #region 绑定变量
        private string tagIconCode;
        public string TagIconCode
        {
            get { return tagIconCode; }
            set
            {
                tagIconCode = value;
                this.RaisePropertyChanged("TagIconCode");
            }
        }

        private double tagIconSize = 16.0;
        public double TagIconSize
        {
            get { return tagIconSize; }
            set
            {
                tagIconSize = value;
                this.RaisePropertyChanged("TagIconSize");
            }
        }

        private string tagContent;

        public string TagContent
        {
            get { return tagContent; }
            set
            {
                tagContent = value;
                this.RaisePropertyChanged("TagContent");
            }
        }

        private Visibility tagContentVisibility;

        public Visibility TagContentVisibility
        {
            get { return tagContentVisibility; }
            set
            {
                tagContentVisibility = value;
                this.RaisePropertyChanged("TagContentVisibility");
            }
        }

        private Visibility tagPanelVisibility = Visibility.Visible;
        /// <summary>
        /// 是否显示标签 用于校验高亮
        /// </summary>
        public Visibility TagPanelVisibility
        {
            get { return tagPanelVisibility; }
            set
            {
                tagPanelVisibility = value;
                this.RaisePropertyChanged("TagPanelVisibility");
            }
        }

        private Visibility toolPanelVisibility = Visibility.Collapsed;
        /// <summary>
        /// 是否显示功能栏（结束选择基准元素后显示）
        /// </summary>
        public Visibility ToolPanelVisibility
        {
            get { return toolPanelVisibility; }
            set
            {
                toolPanelVisibility = value;
                this.RaisePropertyChanged("ToolPanelVisibility");
            }
        }

        private int borderWidth;

        public int BorderWidth
        {
            get { return borderWidth; }
            set
            {
                borderWidth = value;
                this.RaisePropertyChanged("BorderWidth");
            }
        }

        private int borderHeight;

        public int BorderHeight
        {
            get { return borderHeight; }
            set
            {
                borderHeight = value;
                this.RaisePropertyChanged("BorderHeight");
            }
        }

        private SolidColorBrush borderBackground;

        public SolidColorBrush BorderBackground
        {
            get { return borderBackground; }
            set
            {
                borderBackground = value;
                this.RaisePropertyChanged("BorderBackground");
            }
        }

        private readonly SolidColorBrush SolidColorBrushActive = new SolidColorBrush(Colors.SkyBlue);
        private readonly SolidColorBrush SolidColorBrushTransparent = new SolidColorBrush(Colors.Transparent);

        public void SetBackgroundActive(bool _IsActive)
        {
            if (_IsActive)
            {
                BorderBackground = SolidColorBrushActive;
            }
            else
            {
                BorderBackground = SolidColorBrushTransparent;
            }
        }

        private int tagRowID;

        public int TagRowID
        {
            get { return tagRowID; }
            set
            {
                tagRowID = value;
                this.RaisePropertyChanged("TagRowID");
            }
        }

        private int borderRowID;

        public int BorderRowID
        {
            get { return borderRowID; }
            set
            {
                borderRowID = value;
                this.RaisePropertyChanged("BorderRowID");
            }
        }

        private int toolsRowID;

        public int ToolsRowID
        {
            get { return toolsRowID; }
            set
            {
                toolsRowID = value;
                this.RaisePropertyChanged("ToolsRowID");
            }
        }
        private SolidColorBrush borderBorderBrush;

        public SolidColorBrush BorderBorderBrush
        {
            get { return borderBorderBrush; }
            set
            {
                borderBorderBrush = value;
                this.RaisePropertyChanged("BorderBorderBrush");
            }
        }

        private Thickness toolsMargin;
        public Thickness ToolsMargin
        {
            get { return toolsMargin; }
            set
            {
                toolsMargin = value;
                this.RaisePropertyChanged("ToolsMargin");
            }
        }
        #endregion

    }
}
