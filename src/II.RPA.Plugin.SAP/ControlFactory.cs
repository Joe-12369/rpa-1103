using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.SAP.Controls;
using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.SAP
{
    class ControlFactory
    {
        public static Rectangle GetLocation(ApplicationContext context)
        {
            Rectangle Rectangle = new Rectangle();
            ISapControl control = GetControl(context.component,context.ext);
            if (control != null)
            {
                Rectangle= control.GetRectangle(context);
            }

            //这里需要有个坐标转换；
            if (Rectangle != Rectangle.Empty)
            {
                Rectangle = DpiTransform.TransformBackRectangle(Rectangle);
             }

            return Rectangle;
        }

        private static readonly Dictionary<int, Func<GuiComponent, string, ISapControl>> _controlMap
            = new Dictionary<int, Func<GuiComponent, string, ISapControl>>
        {
            [(int)ComponentType.GuiBox] = (c, e) => new GuiBoxAction(),
            [(int)ComponentType.GuiButton] = (c, e) => new GuiButtonAction(),
            [(int)ComponentType.GuiCheckBox] = (c, e) => new GuiCheckBoxAction(),
            [(int)ComponentType.GuiComboBox] = (c, e) => new GuiComboBoxAction(),
            [(int)ComponentType.GuiCTextField] = (c, e) => new GuiCTextFieldAction(),
            [(int)ComponentType.GuiCustomControl] = (c, e) => new GuiCustomControlAction(),
            [(int)ComponentType.GuiDialogShell] = (c, e) => new GuiDialogShellAction(),
            [(int)ComponentType.GuiDockShell] = (c, e) => new GuiDockShellAction(),
            [(int)ComponentType.GuiFrameWindow] = (c, e) => new GuiFrameWindowAction(),
            [(int)ComponentType.GuiGOSShell] = (c, e) => new GuiGOSShellAction(),
            [(int)ComponentType.GuiLabel] = (c, e) => new GuiLabelAction(),
            [(int)ComponentType.GuiMainWindow] = (c, e) => new GuiMainWindowAction(),
            [(int)ComponentType.GuiMenu] = (c, e) => new GuiMenuAction(),
            [(int)ComponentType.GuiMenubar] = (c, e) => new GuiMenubarAction(),
            [(int)ComponentType.GuiModalWindow] = (c, e) => new GuiModalWindowAction(),
            [(int)ComponentType.GuiOkCodeField] = (c, e) => new GuiOKCodeAction(),
            [(int)ComponentType.GuiPasswordField] = (c, e) => new GuiPasswordFieldAction(),
            [(int)ComponentType.GuiRadioButton] = (c, e) => new GuiRadioButtonAction(),
            [(int)ComponentType.GuiScrollContainer] = (c, e) => new GuiScrollContainerAction(),
            [(int)ComponentType.GuiSimpleContainer] = (c, e) => new GuiSimpleContainerAction(),
            [(int)ComponentType.GuiTab] = (c, e) => new GuiTabAction(),
            [(int)ComponentType.GuiTableControl] = CreateGuiTableControlControl,
            [(int)ComponentType.GuiTabStrip] = (c, e) => new GuiTabStripAction(),
            [(int)ComponentType.GuiTextField] = (c, e) => new GuiTextAction(),
            [(int)ComponentType.GuiTitlebar] = (c, e) => new GuiTitlebarAction(),
            [(int)ComponentType.GuiToolbar] = (c, e) => new GuiToolbarAction(),
            [(int)ComponentType.GuiTSatusbar] = (c, e) => new GuiStatusbarAction(),
            [(int)ComponentType.GuiUserArea] = (c, e) => new GuiUserAreaAction(),
            [(int)ComponentType.GuiContextMenu] = (c, e) => new GuiContextMenuAction(),
            [(int)ComponentType.GuiContainerShell] = (c, e) => new GuiContainerShellAction(),
            [(int)ComponentType.GuiShell] = CreateGuiShellControl,
        };

        private static ISapControl CreateGuiTableControlControl(GuiComponent component, string ext)
        {
            if (!string.IsNullOrEmpty(ext)
                && component is GuiTableControl
                && GuiTableCellAction.IsTableCell(ext, out int tableRow, out int tableColumn))
            {
                return new GuiTableCellAction(component, tableRow, tableColumn);
            }
            if (!string.IsNullOrEmpty(ext)
                && component is GuiTableControl
                && GuiGridCellAction.IsGridRow(ext, out int tableRowIndex))
            {
                return new GuiTableRowAction(component, tableRowIndex);
            }
            if (!string.IsNullOrEmpty(ext)
                && component is GuiTableControl
                && GuiTableCellAction.IsTableColumn(ext, out int tableColumnIndex))
            {
                return new GuiTableColumnAction(component, tableColumnIndex);
            }

            return new GuiTableControlAction();
        }

        private static ISapControl CreateGuiShellControl(GuiComponent component, string ext)
        {
            if (!string.IsNullOrEmpty(ext)
                && component is GuiGridView
                && GuiGridCellAction.IsGridCell(ext, out int row, out string column))
            {
                return new GuiGridCellAction(component, row, column);
            }
            if (!string.IsNullOrEmpty(ext)
                && component is GuiGridView
                && GuiGridCellAction.IsGridRow(ext, out int rowIndex))
            {
                return new GuiGridRowAction(component, rowIndex);
            }
            if (!string.IsNullOrEmpty(ext)
                && component is GuiGridView
                && GuiGridCellAction.IsGridColumn(ext, out string columnName))
            {
                return new GuiGridColumnAction(component, columnName);
            }

            GuiTree tree = component as GuiTree;
            if (tree != null)
            {
                return new GuiTreeAction();
            }

            return new GuiShellAction();
        }

        public static ISapControl GetControl(GuiComponent component, string ext = null)
        {
            if (_controlMap.TryGetValue(component.TypeAsNumber, out var factory))
                return factory(component, ext);
            return null;
        }
        public enum ComponentType
        {
            GuiUnknown = -1,
            GuiComponent = 0,
            GuiVComponent = 1,
            GuiVContainer = 2,
            GuiApplication = 10,
            GuiConnection = 11,
            GuiSession = 12,
            GuiFrameWindow = 20,
            GuiMainWindow = 21,
            GuiModalWindow = 22,
            GuiLabel = 30,
            GuiTextField = 31,
            GuiCTextField = 32,
            GuiPasswordField = 33,
            GuiComboBox = 34,
            GuiOkCodeField = 35,
            GuiButton = 40,
            GuiRadioButton = 41,
            GuiCheckBox = 42,
            GuiCustomControl = 50,
            GuiContainerShell = 51,
            GuiBox = 62,
            GuiContainer = 70,
            GuiSimpleContainer = 71,
            GuiScrollContainer = 72,
            GuiUserArea = 74,
            GuiTableControl = 80,
            GuiTabStrip = 90,
            GuiTab = 91,
            GuiScrollbar = 100,
            GuiToolbar = 101,
            GuiTitlebar = 102,
            GuiTSatusbar = 103,
            GuiMenu = 110,
            GuiMenubar = 111,
            GuiShell = 122,
            GuiGOSShell = 123,
            GuiSplitterShell = 124,
            GuiDialogShell = 125,
            GuiDockShell = 126,
            GuiContextMenu = 127,
            GuiVHViewSwitch = 129,
            GuiTableRow = 82,
            GuiStatusPane = 43,
            GuiStatusBarLink = 130,
            GuiSplitterContainer = 75,
            GuiSessionInfo = 121,
            GuiListContainer = 73,
            GuiComponentCollection = 128,
            GuiCollection = 120
        }

        internal static ITable GetTable(GuiComponent component)
        {
            ITable control = null;
            switch (component.TypeAsNumber)
            {
                case (int)ComponentType.GuiTableControl:
                    control = new GuiTableControlAction();
                    break;
                case (int)ComponentType.GuiShell:
                    control = new GuiGridViewAction();
                    break;
                default:
                    break;
            }
            return control;
        }
    }
}
