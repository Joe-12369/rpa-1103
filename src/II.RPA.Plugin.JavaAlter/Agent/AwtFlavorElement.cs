using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    class AwtFlavorElement : JavaBaseFlavorElement
    {
        private static void SetupRoleMap()
        {
            AwtFlavorElement.classRoleMap["java.awt.Component"] = Role.Unknown;
            AwtFlavorElement.classRoleMap["java.awt.Button"] = Role.Button;
            AwtFlavorElement.classRoleMap["java.awt.Canvas"] = Role.Picture;
            AwtFlavorElement.classRoleMap["java.awt.Checkbox"] = Role.CheckBox;
            AwtFlavorElement.classRoleMap["java.awt.Choice"] = Role.ComboBox;
            AwtFlavorElement.classRoleMap["java.awt.Container"] = Role.Container;
            AwtFlavorElement.classRoleMap["java.awt.Label"] = Role.Text;
            AwtFlavorElement.classRoleMap["java.awt.List"] = Role.List;
            AwtFlavorElement.classRoleMap["java.awt.Scrollbar"] = Role.ScrollBar;
            AwtFlavorElement.classRoleMap["java.awt.TextArea"] = Role.Text;
            AwtFlavorElement.classRoleMap["java.awt.TextField"] = Role.Text;
            AwtFlavorElement.classRoleMap["java.awt.Panel"] = Role.Container;
            AwtFlavorElement.classRoleMap["java.awt.Window"] = Role.Form;
            AwtFlavorElement.classRoleMap["java.awt.Frame"] = Role.Form;
            AwtFlavorElement.classRoleMap["java.awt.Dialog"] = Role.Form;
            AwtFlavorElement.classRoleMap["java.awt.JDialog"] = Role.Form;
            AwtFlavorElement.classRoleMap["java.applet.Applet"] = Role.Container;
            AwtFlavorElement.classRoleMap["java.awt.MenuItem"] = Role.MenuItem;
            AwtFlavorElement.classRoleMap["java.awt.MenuBar"] = Role.MenuBar;
            AwtFlavorElement.classRoleMap["java.awt.Menu"] = Role.ContextMenu;
            AwtFlavorElement.classRoleMap["javax.swing.JFrame"] = Role.Form;
            AwtFlavorElement.classRoleMap["javax.swing.JComponent"] = Role.Unknown;
            AwtFlavorElement.classRoleMap["javax.swing.JButton"] = Role.Button;
            AwtFlavorElement.classRoleMap["javax.swing.JRadioButton"] = Role.RadioButton;
            AwtFlavorElement.classRoleMap["javax.swing.JToggleButton"] = Role.CheckBox;
            AwtFlavorElement.classRoleMap["javax.swing.JCheckBox"] = Role.CheckBox;
            AwtFlavorElement.classRoleMap["javax.swing.JMenuItem"] = Role.MenuItem;
            AwtFlavorElement.classRoleMap["javax.swing.JComboBox"] = Role.ComboBox;
            AwtFlavorElement.classRoleMap["javax.swing.JList"] = Role.List;
            AwtFlavorElement.classRoleMap["javax.swing.JProgressBar"] = Role.ProgressBar;
            AwtFlavorElement.classRoleMap["javax.swing.JSlider"] = Role.Slider;
            AwtFlavorElement.classRoleMap["javax.swing.JSpinner"] = Role.Text;
            AwtFlavorElement.classRoleMap["javax.swing.JLabel"] = Role.Text;
            AwtFlavorElement.classRoleMap["javax.swing.JTree"] = Role.Tree;
            AwtFlavorElement.classRoleMap["javax.swing.JEditorPane"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JTextArea"] = Role.Text;
            AwtFlavorElement.classRoleMap["javax.swing.JTextField"] = Role.Text;
            AwtFlavorElement.classRoleMap["javax.swing.JTextPane"] = Role.Text;
            AwtFlavorElement.classRoleMap["javax.swing.Box"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JPanel"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JSplitPane"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JTabbedPane"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JRootPane"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JLayeredPane"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JScrollPane"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JViewport"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JScrollBar"] = Role.ScrollBar;
            AwtFlavorElement.classRoleMap["javax.swing.JToolBar"] = Role.ToolBar;
            AwtFlavorElement.classRoleMap["javax.swing.JMenuBar"] = Role.MenuBar;
            AwtFlavorElement.classRoleMap["javax.swing.JPopupMenu"] = Role.ContextMenu;
            AwtFlavorElement.classRoleMap["javax.swing.JOptionPane"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JApplet"] = Role.Container;
            AwtFlavorElement.classRoleMap["javax.swing.JSeparator"] = Role.Separator;
            AwtFlavorElement.classRoleMap["javax.swing.JToolTip"] = Role.ToolTip;
            AwtFlavorElement.classRoleMap["javax.swing.JTable"] = Role.Table;
            AwtFlavorElement.classRoleMap["javax.swing.table.JTableHeader"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.ewt.alert.AlertPane"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.ewt.multiLineLabel.MultiLineLabel"] = Role.Text;
            AwtFlavorElement.classRoleMap["oracle.ewt.button.ButtonBar"] = Role.ToolBar;
            AwtFlavorElement.classRoleMap["oracle.ewt.button.ContinuousButton"] = Role.Button;
            AwtFlavorElement.classRoleMap["oracle.ewt.button.PushButton"] = Role.Button;
            AwtFlavorElement.classRoleMap["oracle.ewt.dTree.DTree"] = Role.Tree;
            AwtFlavorElement.classRoleMap["oracle.ewt.EwtComponent"] = Role.Unknown;
            AwtFlavorElement.classRoleMap["oracle.ewt.imageCanvas.ImageCanvas"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.LWDataSourceList"] = Role.List;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.LWCheckbox"] = Role.CheckBox;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.LWComponent"] = Role.Unknown;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.LWContainer"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.LWDataSourceList$Content"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.LWLabel"] = Role.Text;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.lwMenu.LWMenu"] = Role.MenuItem;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.lwMenu.LWMenuBar"] = Role.MenuBar;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.lwMenu.LWMenuItem"] = Role.MenuItem;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.lwMenu.LWPopupMenu"] = Role.ContextMenu;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.LWScrollbar"] = Role.ScrollBar;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.lwWindow.laf.TitleBar"] = Role.TitleBar;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.lwWindow.laf.TitleBar$SystemMenu"] = Role.ContextMenu;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.lwWindow.laf.TitleBar$SystemMenuBar"] = Role.MenuBar;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.lwWindow.LWWindow"] = Role.Form;
            AwtFlavorElement.classRoleMap["oracle.ewt.scrolling.scrollBox.EwtLWScrollbar"] = Role.ScrollBar;
            AwtFlavorElement.classRoleMap["oracle.ewt.scrolling.scrollBox.ScrollBox"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.ewt.tabBar.TabBar"] = Role.TabPageList;
            AwtFlavorElement.classRoleMap["oracle.ewt.tabPanel.TabPanelSheet"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.ewt.toolBar.ToolBar"] = Role.ToolBar;
            AwtFlavorElement.classRoleMap["oracle.forms.ui.DrawnPanel"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.forms.ui.ExtendedFrame"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.lwText.LWTextArea"] = Role.Text;
            AwtFlavorElement.classRoleMap["oracle.forms.ui.FormDesktopContainer"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.forms.ui.FormsTabPanel"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.forms.ui.mdi.MDIContainer"] = Role.Container;
            AwtFlavorElement.classRoleMap["oracle.forms.ui.mdi.MDIWindowMenu"] = Role.ContextMenu;
            AwtFlavorElement.classRoleMap["oracle.forms.ui.VBean"] = Role.Unknown;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.LWChoice"] = Role.ComboBox;
            AwtFlavorElement.classRoleMap["oracle.ewt.lwAWT.lwText.LWTextField"] = Role.Text;
        }

        public override object InvokeAction(Element element, string name, object[] args)
        {
            if (!this.isOra)
            {
                if (element.Role == Role.Button && name == "press")
                {
                    // FlavorElement.AssertArgumentTypes(new Type[0], args);
                    this.jow.InvokeMethod("doClick", Array.Empty<object>());
                    return null;
                }
                if (element.Role == Role.RadioButton && name == "select")
                {
                    // FlavorElement.AssertArgumentTypes(new Type[0], args);
                    this.jow.InvokeSetProperty("checked", true);
                    return null;
                }
                if (element.Role == Role.MenuItem && name == "select")
                {
                    //FlavorElement.AssertArgumentTypes(new Type[0], args);
                    if (this.mappedClazz == "java.awt.MenuItem")
                    {
                        this.jow.InvokeMethod("setState", new object[]
                        {
                            true
                        });
                    }
                    else
                    {
                        this.jow.InvokeSetProperty("checked", true);
                    }
                    return null;
                }
            }
            else if (this.isOra)
            {
                if (element.Role == Role.Button)
                {
                    if (name == "press")
                    {
                        // FlavorElement.AssertArgumentTypes(new Type[0], args);
                        this.jow.InvokeMethod("simulatePush", Array.Empty<object>());
                        return null;
                    }
                }
                else if (element.Role == Role.RadioButton || element.Role == Role.CheckBox)
                {
                    if (name == "select")
                    {
                        // FlavorElement.AssertArgumentTypes(new Type[0], args);
                        this.jow.InvokeMethod("simulatePush", Array.Empty<object>());
                        return null;
                    }
                }
                else if (element.Role == Role.Form)
                {
                    if (name == "activate")
                    {
                        // FlavorElement.AssertArgumentTypes(new Type[0], args);
                        this.jow.InvokeSetProperty("setActive", true);
                        return null;
                    }
                    if (name == "close")
                    {
                        // FlavorElement.AssertArgumentTypes(new Type[0], args);
                        this.jow.InvokeMethod("close", Array.Empty<object>());
                        return null;
                    }
                }
                else if (element.Role == Role.MenuItem && name == "select")
                {
                    // FlavorElement.AssertArgumentTypes(new Type[0], args);
                    this.jow.InvokeMethod("activate", Array.Empty<object>());
                    return null;
                }
            }
            return null;
            // return base.InvokeAction(element, name, args);
        }

        public override object GetAttributeValue(Element element, string name)
        {
            if (!this.isOra)
            {
                if (element.Role == Role.Button)
                {
                    if (name == "pressed")
                    {
                        return this.jow.InvokeGetProperty("pressed");
                    }
                    if (name == "text")
                    {
                        if (this.mappedClazz == "java.awt.Button")
                        {
                            return this.jow.InvokeGetStringProperty("getLabel");
                        }
                        return this.jow.InvokeGetStringProperty("getText");
                    }
                }
                else if (element.Role == Role.CheckBox)
                {
                    if (name == "text")
                    {
                        if (this.mappedClazz == "java.awt.Checkbox")
                        {
                            return this.jow.InvokeGetStringProperty("getLabel");
                        }
                        return this.jow.InvokeGetStringProperty("getText");
                    }
                    else if (name == "checked")
                    {
                        if (this.mappedClazz == "java.awt.Checkbox")
                        {
                            return this.jow.InvokeGetProperty("getState");
                        }
                        return this.jow.InvokeGetProperty("checked");
                    }
                    else if (name == "checkstate")
                    {
                        if (this.mappedClazz == "java.awt.Checkbox")
                        {
                            return ((bool)this.jow.InvokeGetProperty("getState")) ? CheckState.Checked : CheckState.Unchecked;
                        }
                        return ((bool)this.jow.InvokeGetProperty("checked")) ? CheckState.Checked : CheckState.Unchecked;
                    }
                }
                else if (element.Role == Role.ComboBox)
                {
                    if (name == "dropdownvisible")
                    {
                        return this.jow.InvokeGetProperty("isPopupVisible");
                    }
                    if (name == "selecteditemindex")
                    {
                        return this.jow.InvokeGetProperty("getSelectedIndex");
                    }
                    if (name == "selecteditemtext")
                    {
                        return this.jow.InvokeGetStringProperty("getSelectedItem");
                    }
                    if (name == "text")
                    {
                        if (this.mappedClazz == "java.awt.Choice")
                        {
                            return this.jow.InvokeGetStringProperty("getSelectedItem");
                        }
                        return this.jow.InvokeGetStringProperty("combobox");
                    }
                }
                else if (element.Role == Role.Container)
                {
                    if (name == "caption")
                    {
                        if (this.mappedClazz == "java.applet.Applet" || this.mappedClazz == "javax.swing.JApplet")
                        {
                            return this.clazzes[0];
                        }
                        return this.jow.InvokeGetStringProperty("getText");
                    }
                    else if (name == "containertype")
                    {
                        return null;
                    }
                }
                else if (element.Role == Role.List)
                {
                    if (name == "selecteditemindex")
                    {
                        return this.jow.InvokeGetProperty("getSelectedIndex");
                    }
                    if (name == "selecteditemtext")
                    {
                        if (this.mappedClazz == "java.awt.List")
                        {
                            return this.jow.InvokeGetStringProperty("getSelectedItem");
                        }
                        return this.jow.InvokeGetStringProperty("getSelectedValue");
                    }
                }
                else if (element.Role == Role.ProgressBar)
                {
                    if (name == "value")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getValue"));
                    }
                    if (name == "minvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMinimum"));
                    }
                    if (name == "maxvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMaximum"));
                    }
                }
                else if (element.Role == Role.RadioButton)
                {
                    if (name == "text")
                    {
                        return this.jow.InvokeGetStringProperty("getText");
                    }
                    if (name == "checked")
                    {
                        return this.jow.InvokeGetProperty("checked");
                    }
                }
                else if (element.Role == Role.ScrollBar)
                {
                    if (name == "value")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getValue"));
                    }
                    if (name == "minvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMinimum"));
                    }
                    if (name == "maxvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMaximum"));
                    }
                    if (name == "pagesize")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getBlockIncrement"));
                    }
                    //if (name == "style")
                    //{
                    //    string a = this.jow.InvokeGetProperty("getOrientation").ToString();
                    //    if (a == "0")
                    //    {
                    //        return Role.ScrollBarStyle.Horizontal;
                    //    }
                    //    if (a == "1")
                    //    {
                    //        return Role.ScrollBarStyle.Vertical;
                    //    }
                    //    return Role.ScrollBarStyle.Custom;
                    //}
                }
                else if (element.Role == Role.Slider)
                {
                    if (name == "value")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getValue"));
                    }
                    if (name == "minvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMinimum"));
                    }
                    if (name == "maxvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMaximum"));
                    }
                }
                else if (element.Role == Role.TabPage)
                {
                    if (name == "title")
                    {
                        return null;
                    }
                    if (name == "selected")
                    {
                        return null;
                    }
                }
                else if (element.Role == Role.Text)
                {
                    if (name == "caption")
                    {
                        if (this.mappedClazz == "javax.swing.JLabel" || this.mappedClazz == "java.awt.Label")
                        {
                            return this.jow.InvokeGetStringProperty("getText");
                        }
                        return null;
                    }
                    else if (name == "text")
                    {
                        if (this.mappedClazz == "javax.swing.JSpinner")
                        {
                            return this.jow.InvokeGetStringProperty("getValue");
                        }
                        return this.jow.InvokeGetStringProperty("getText");
                    }
                    else if (name == "selectionstart")
                    {
                        object obj = this.jow.InvokeGetProperty("getSelectionStart");
                        if (obj != null)
                        {
                            return (int)obj;
                        }
                        return 0;
                    }
                    else if (name == "selectionlength")
                    {
                        object obj2 = this.jow.InvokeGetProperty("getSelectionStart");
                        object obj3 = this.jow.InvokeGetProperty("getSelectionEnd");
                        if (obj2 != null && obj3 != null)
                        {
                            return (int)obj3 - (int)obj2;
                        }
                        return 0;
                    }
                    else if (name == "selectiontext")
                    {
                        return this.jow.InvokeGetStringProperty("getSelectedText");
                    }
                }
                else if (element.Role == Role.ToolTip)
                {
                    if (name == "text")
                    {
                        return this.jow.InvokeGetStringProperty("getTipText");
                    }
                }
                else if (element.Role == Role.MenuItem)
                {
                    if (name == "text")
                    {
                        if (this.mappedClazz == "java.awt.MenuItem")
                        {
                            return this.jow.InvokeGetStringProperty("getLabel");
                        }
                        return this.jow.InvokeGetStringProperty("getText");
                    }
                    else if (name == "checked")
                    {
                        if (this.mappedClazz == "java.awt.MenuItem")
                        {
                            return this.jow.InvokeGetProperty("getState");
                        }
                        return this.jow.InvokeGetProperty("checked");
                    }
                }
            }
            if (this.isOra)
            {
                if (element.Role == Role.Button)
                {
                    if (name == "pressed")
                    {
                        return this.jow.InvokeGetProperty("isArmed");
                    }
                    if (name == "text")
                    {
                        if (base.ObjectClazz == "oracle.ewt.lwAWT.LWDataSourceChoice$ChoiceButton")
                        {
                            return "dropdown";
                        }
                        return this.jow.InvokeGetStringProperty("getLabel");
                    }
                }
                else if (element.Role == Role.CheckBox)
                {
                    if (name == "text")
                    {
                        return this.jow.InvokeGetStringProperty("getLabel");
                    }
                    if (name == "checked")
                    {
                        return this.jow.InvokeGetProperty("getState");
                    }
                    if (name == "checkstate")
                    {
                        return ((bool)this.jow.InvokeGetProperty("getState")) ? CheckState.Checked : CheckState.Unchecked;
                    }
                }
                else if (element.Role == Role.ComboBox)
                {
                    if (name == "dropdownvisible")
                    {
                        return this.jow.InvokeGetProperty("isDropDownVisible");
                    }
                    if (name == "selecteditemindex")
                    {
                        return this.jow.InvokeGetProperty("getSelectedIndex");
                    }
                    if (name == "selecteditemtext")
                    {
                        return this.jow.InvokeGetStringProperty("getSelectedItem");
                    }
                    if (name == "text")
                    {
                        return this.jow.InvokeGetStringProperty("getSelectedItem");
                    }
                }
                else if (element.Role == Role.Container)
                {
                    if (name == "caption")
                    {
                        if (this.mappedClazz == "oracle.ewt.alert.AlertPane")
                        {
                            return this.jow.InvokeGetStringProperty("getMessage");
                        }
                        if (this.mappedClazz == "oracle.forms.ui.ExtendedFrame")
                        {
                            return this.jow.InvokeGetStringProperty("getTitle");
                        }
                        return null;
                    }
                    else if (name == "containertype")
                    {
                        return null;
                    }
                }
                else if (element.Role == Role.Form)
                {
                    if (name == "title")
                    {
                        return this.jow.InvokeGetStringProperty("getTitle");
                    }
                    if (name == "state")
                    {
                        if ((bool)this.jow.GetProperty("isMinimized"))
                        {
                            return FormWindowState.Minimized;
                        }
                        if ((bool)this.jow.GetProperty("isMaximized"))
                        {
                            return FormWindowState.Maximized;
                        }
                        return FormWindowState.Normal;
                    }
                    else if (name == "isActive")
                    {
                        return (bool)this.jow.GetProperty("isActive");
                    }
                }
                else if (element.Role == Role.List)
                {
                    if (name == "selecteditemindex")
                    {
                        return this.jow.InvokeGetProperty("getSelectedIndex");
                    }
                    if (name == "selecteditemtext")
                    {
                        if (this.mappedClazz == "java.awt.List")
                        {
                            return this.jow.InvokeGetStringProperty("getSelectedItem");
                        }
                        return this.jow.InvokeGetStringProperty("getSelectedValue");
                    }
                }
                else if (element.Role == Role.MenuItem)
                {
                    if (name == "text")
                    {
                        return this.jow.InvokeGetStringProperty("getLabel");
                    }
                    if (name == "checked")
                    {
                        return (bool)(this.jow.InvokeGetProperty("getState") ?? false);
                    }
                }
                else if (element.Role == Role.ProgressBar)
                {
                    if (name == "value")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getValue"));
                    }
                    if (name == "minvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMinimum"));
                    }
                    if (name == "maxvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMaximum"));
                    }
                }
                else if (element.Role == Role.RadioButton)
                {
                    if (name == "text")
                    {
                        return this.jow.InvokeGetStringProperty("getLabel");
                    }
                    if (name == "checked")
                    {
                        return this.jow.InvokeGetProperty("getState");
                    }
                }
                else if (element.Role == Role.ScrollBar)
                {
                    if (name == "value")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getValue"));
                    }
                    if (name == "minvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMinimum"));
                    }
                    if (name == "maxvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMaximum"));
                    }
                    if (name == "pagesize")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getBlockIncrement"));
                    }
                    //if (name == "style")
                    //{
                    //    string a2 = this.jow.InvokeGetProperty("getOrientation").ToString();
                    //    if (a2 == "0")
                    //    {
                    //        return Role.ScrollBarStyle.Horizontal;
                    //    }
                    //    if (a2 == "1")
                    //    {
                    //        return Role.ScrollBarStyle.Vertical;
                    //    }
                    //    return Role.ScrollBarStyle.Custom;
                    //}
                }
                else if (element.Role == Role.Slider)
                {
                    if (name == "value")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getValue"));
                    }
                    if (name == "minvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMinimum"));
                    }
                    if (name == "maxvalue")
                    {
                        return (double)((int)this.jow.InvokeGetProperty("getMaximum"));
                    }
                }
                else if (element.Role == Role.TabPage)
                {
                    if (name == "title")
                    {
                        return null;
                    }
                    if (name == "selected")
                    {
                        return null;
                    }
                }
                else if (element.Role == Role.Text)
                {
                    if (name == "caption")
                    {
                        if (this.mappedClazz == "oracle.ewt.multiLineLabel.MultiLineLabel" || this.mappedClazz == "oracle.ewt.lwAWT.LWLabel" || (this.mappedClazz == "oracle.ewt.lwAWT.lwText.LWTextField" && !(bool)this.jow.InvokeGetProperty("isEditable")))
                        {
                            return this.jow.InvokeGetStringProperty("getText");
                        }
                        return null;
                    }
                    else if (name == "text")
                    {
                        if ((this.mappedClazz == "oracle.ewt.lwAWT.lwText.LWTextField" && (bool)this.jow.InvokeGetProperty("isEditable")) || this.mappedClazz == "oracle.ewt.lwAWT.lwText.LWTextArea")
                        {
                            return this.jow.InvokeGetStringProperty("getText");
                        }
                    }
                    else if (name == "selectionstart")
                    {
                        object obj4 = this.jow.InvokeGetProperty("getSelectionStart");
                        if (obj4 != null)
                        {
                            return (int)obj4;
                        }
                        return 0;
                    }
                    else if (name == "selectionlength")
                    {
                        object obj5 = this.jow.InvokeGetProperty("getSelectionStart");
                        object obj6 = this.jow.InvokeGetProperty("getSelectionEnd");
                        if (obj5 != null && obj6 != null)
                        {
                            return (int)obj6 - (int)obj5;
                        }
                        return 0;
                    }
                    else if (name == "selectiontext")
                    {
                        return this.jow.InvokeGetStringProperty("getSelectedText");
                    }
                }
                else if (element.Role == Role.TitleBar && name == "text")
                {
                    JavaObjectWrapper objectProperty = this.jow.GetObjectProperty("getLWWindow");
                    if (objectProperty != null)
                    {
                        return objectProperty.InvokeGetStringProperty("getTitle");
                    }
                    return null;
                }
            }
            return null;
            //if (element.HasCapability("javaelement"))
            //{ 
            //    if (name == "name")
            //    {
            //        if (this.mappedClazz == "java.applet.Applet" || this.mappedClazz == "javax.swing.JApplet")
            //        {
            //            return null;
            //        }
            //        if (this.isOra && this.mappedClazz == "oracle.ewt.lwAWT.lwMenu.LWMenuItem")
            //        {
            //            string text = this.jow.InvokeGetStringProperty("getActionCommand");
            //            string b = this.jow.InvokeGetStringProperty("getLabel");
            //            if (text != null && text != b)
            //            {
            //                return text;
            //            }
            //        }
            //        string text2 = this.jow.InvokeGetStringProperty("name");
            //        if (text2 != null)
            //        {
            //            if (this.isOra && text2.StartsWith(base.GetShortTypeName()))
            //            {
            //                string input = text2.Substring(base.GetShortTypeName().Length);
            //                if (JavaBaseFlavorElement.allDigits.IsMatch(input))
            //                {
            //                    return null;
            //                }
            //            }
            //            return text2;
            //        }
            //        text2 = this.jow.InvokeGetStringProperty("membername");
            //        if (text2 == null && element.Role == Role.ContextMenu)
            //        {
            //            return this.jow.InvokeGetStringProperty("getLabel");
            //        }
            //        return text2;
            //    }
            //    else
            //    {
            //        if (name == "accessiblename")
            //        {
            //            return this.jow.InvokeGetStringProperty("accname");
            //        }
            //        if (name == "accessibledescription")
            //        {
            //            return this.jow.InvokeGetStringProperty("accdesc");
            //        }
            //    }
            //}
            //return base.GetAttributeValue(element, name);
        }

        public override void SetAttributeValue(Element element, string name, object value)
        {
            if (!this.isOra)
            {
                if (element.Role == Role.CheckBox)
                {
                    if (name == "checked")
                    {
                        FlavorElement.AssertArgumentType(typeof(bool), value);
                        if (this.mappedClazz == "java.awt.Checkbox")
                        {
                            this.jow.InvokeMethod("setState", new object[]
                            {
                                value
                            });
                            return;
                        }
                        this.jow.InvokeSetProperty("checked", value);
                        return;
                    }
                }
                else if (element.Role == Role.ComboBox)
                {
                    if (name == "selecteditemindex")
                    {
                        FlavorElement.AssertArgumentType(typeof(int), value);
                        this.jow.InvokeMethod("setSelectedIndex", new object[]
                        {
                            value
                        });
                        return;
                    }
                    if (name == "selecteditemtext")
                    {
                        FlavorElement.AssertArgumentType(typeof(string), value);
                        return;
                    }
                    if (name == "text")
                    {
                        FlavorElement.AssertArgumentType(typeof(string), value);
                        this.jow.InvokeSetProperty("combobox", value);
                        return;
                    }
                    if (name == "dropdownvisible")
                    {
                        FlavorElement.AssertArgumentType(typeof(bool), value);
                        this.jow.InvokeMethod("setPopupVisible", new object[]
                        {
                            value
                        });
                        return;
                    }
                }
                else if (element.Role == Role.Text)
                {
                    if (name == "text")
                    {
                        FlavorElement.AssertArgumentType(typeof(string), value);
                        if (this.mappedClazz == "javax.swing.JSpinner")
                        {
                            this.jow.InvokeMethod("setValue", new object[]
                            {
                                (int)double.Parse((string)value)
                            });
                            return;
                        }
                        this.jow.InvokeMethod("setText", new object[]
                        {
                            value
                        });
                        return;
                    }
                }
                else if (element.Role == Role.Slider && name == "value")
                {
                    FlavorElement.AssertArgumentType(typeof(double), value);
                    this.jow.InvokeMethod("setValue", new object[]
                    {
                        (int)((double)value)
                    });
                    return;
                }
            }
            else if (this.isOra)
            {
                if (element.Role == Role.CheckBox)
                {
                    if (name == "checked")
                    {
                        FlavorElement.AssertArgumentType(typeof(bool), value);
                        this.jow.InvokeMethod("setState", new object[]
                        {
                            value
                        });
                        return;
                    }
                }
                else if (element.Role == Role.ComboBox)
                {
                    if (name == "selecteditemindex")
                    {
                        FlavorElement.AssertArgumentType(typeof(int), value);
                        this.jow.InvokeMethod("setSelectedIndex", new object[]
                        {
                            value
                        });
                        return;
                    }
                    if (name == "selecteditemtext")
                    {
                        FlavorElement.AssertArgumentType(typeof(string), value);
                        this.jow.InvokeMethod("select", new object[]
                        {
                            value
                        });
                        return;
                    }
                    if (name == "dropdownvisible")
                    {
                        FlavorElement.AssertArgumentType(typeof(bool), value);
                        this.jow.InvokeMethod("setDropDownVisible", new object[]
                        {
                            value
                        });
                        return;
                    }
                }
                else if (element.Role == Role.Text)
                {
                    if (name == "text")
                    {
                        FlavorElement.AssertArgumentType(typeof(string), value);
                        if (this.mappedClazz == "oracle.ewt.lwAWT.lwText.LWTextField" || this.mappedClazz == "oracle.ewt.lwAWT.lwText.LWTextArea")
                        {
                            this.jow.InvokeMethod("setText", new object[]
                            {
                                value
                            });
                        }
                        return;
                    }
                }
                else if (element.Role == Role.ScrollBar)
                {
                    if (name == "value")
                    {
                        FlavorElement.AssertArgumentType(typeof(double), value);
                        this.jow.InvokeMethod("setValue", new object[]
                        {
                            (int)((double)value)
                        });
                        return;
                    }
                }
                else if (element.Role == Role.RadioButton && name == "checked")
                {
                    FlavorElement.AssertArgumentType(typeof(bool), value);
                    this.jow.InvokeMethod("setState", new object[]
                    {
                        value
                    });
                    return;
                }
            }
            base.SetAttributeValue(element, name, value);
        }

        public override string FlavorName => "Form";

        static AwtFlavorElement()
        {
            AwtFlavorElement.SetupRoleMap();
        }

        internal AwtFlavorElement(JavaObjectWrapper jow, FlavorElement containerFlavorElement, FlavorElement rootContainerFlavorElement, int javaChildIndex) : base(jow, containerFlavorElement, rootContainerFlavorElement)
        {
            this.javaChildIndex = javaChildIndex;
        }

        public override Rectangle ElementRectangle
        {
            get
            {
                Rectangle result;
                try
                {
                    object[] array = this.jow.InvokeGetSpecialProperty("rect", Array.Empty<object>());
                    result = new Rectangle((int)array[0], (int)array[1], (int)array[2], (int)array[3]);
                }
                catch (Exception)
                {
                    result = Rectangle.Empty;
                }
                return result;
            }
        }

        public override bool Enabled
        {
            get
            {
                bool result;
                try
                {
                    result = (bool)this.jow.InvokeGetSpecialProperty("enabled", Array.Empty<object>())[0];
                }
                catch (Exception)
                {
                    result = true;
                }
                return result;
            }
        }

        public override bool HasFocus
        {
            get
            {
                bool result;
                try
                {
                    result = (bool)this.jow.InvokeGetSpecialProperty("hasfocus", Array.Empty<object>())[0];
                }
                catch (Exception)
                {
                    result = false;
                }
                return result;
            }
        }

        public override bool Valid
        {
            get
            {
                bool result;
                try
                {
                    result = (bool)this.jow.InvokeGetSpecialProperty("valid", Array.Empty<object>())[0];
                }
                catch (Exception)
                {
                    result = false;
                }
                return result;
            }
        }

        public override bool Visible
        {
            get
            {
                bool result;
                try
                {
                    bool flag = (bool)this.jow.InvokeGetSpecialProperty("visible", Array.Empty<object>())[0];
                    if (flag)
                    {
                        if (this.isOra)
                        {
                            Rectangle elementRectangle = this.ElementRectangle;
                            if (elementRectangle.Width == 0 || elementRectangle.Height == 0)
                            {
                                return false;
                            }
                        }
                        AwtFlavorElement awtFlavorElement = this.containerFlavorElement as AwtFlavorElement;
                        if (awtFlavorElement != null && awtFlavorElement.mappedClazz == "javax.swing.JTabbedPane")
                        {
                            JavaObjectWrapper javaObjectWrapper = (JavaObjectWrapper)awtFlavorElement.jow.InvokeGetProperty("getSelectedTab");
                            if (javaObjectWrapper != null && javaObjectWrapper.Equals(this.jow))
                            {
                                return true;
                            }
                            JavaObjectWrapper javaObjectWrapper2 = (JavaObjectWrapper)awtFlavorElement.jow.InvokeGetProperty("getSelectedComponent");
                            if (javaObjectWrapper2 != null)
                            {
                                return javaObjectWrapper2.Equals(this.jow);
                            }
                            return (int)awtFlavorElement.jow.InvokeGetProperty("getSelectedIndex") == this.javaChildIndex;
                        }
                    }
                    result = flag;
                }
                catch (Exception)
                {
                    result = false;
                }
                return result;
            }
        }

        public override bool ForceGeometricSearchAll
        {
            get
            {
                return this.MapClassToRole(this.clazzes, out this.mappedClazz) == Role.Tree;
            }
        }

        internal Rectangle ClippedRectangle
        {
            get
            {
                Rectangle result;
                try
                {
                    object[] array = this.jow.InvokeGetSpecialProperty("clippedrect", Array.Empty<object>());
                    result = new Rectangle((int)array[0], (int)array[1], (int)array[2], (int)array[3]);
                }
                catch (Exception)
                {
                    result = Rectangle.Empty;
                }
                return result;
            }
        }

        internal bool IsOraScrollContainer
        {
            get
            {
                return this.mappedClazz == "oracle.ewt.scrolling.scrollBox.ScrollBox";
            }
        }

        public override bool EnsureVisible()
        {
            bool result;
            try
            {
                if (this.mappedClazz == "java.applet.Applet" || this.mappedClazz == "javax.swing.JApplet")
                {
                    result = this.rootContainerFlavorElement.EnsureVisible();
                }
                else
                {
                    if (!this.isOra)
                    {
                        this.jow.InvokeSpecial("ensurevisible", Array.Empty<object>());
                    }
                    else if (this.isOra)
                    {
                        AwtFlavorElement awtFlavorElement = this.containerFlavorElement as AwtFlavorElement;
                        while (awtFlavorElement != null && !awtFlavorElement.IsOraScrollContainer)
                        {
                            awtFlavorElement = (awtFlavorElement.ContainerFlavorElement as AwtFlavorElement);
                        }
                        if (awtFlavorElement != null)
                        {
                            Rectangle rectangle = (Rectangle)awtFlavorElement.JavaObject.InvokeGetProperty("getInnerBounds");
                            Rectangle elementRectangle = awtFlavorElement.ElementRectangle;
                            elementRectangle.X += rectangle.X;
                            elementRectangle.Y += rectangle.Y;
                            elementRectangle.Width = rectangle.Width;
                            elementRectangle.Height = rectangle.Height;
                            Rectangle elementRectangle2 = this.ElementRectangle;
                            //Point point = Geometry.CalculateEnsureVisibleScrollPixels(elementRectangle, elementRectangle2);
                            //if (point.IsEmpty)
                            //{
                            //    return true;
                            //}
                            //Point point2 = (Point)awtFlavorElement.JavaObject.InvokeGetProperty("getScrollPosition");
                            //point2.X = Math.Max(0, point2.X + point.X);
                            //point2.Y = Math.Max(0, point2.Y + point.Y);
                            //awtFlavorElement.JavaObject.InvokeMethod("setScrollPosition", new object[]
                            //{
                            //    point2
                            //});
                        }
                    }
                    result = true;
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public override void Focus()
        {
            try
            {
                this.jow.InvokeSpecial("focus", Array.Empty<object>());
            }
            catch (Exception)
            {
            }
        }
        public Role MapClassToRole(string[] clazzes, out string mappedClazz)
        {
            foreach (string text in clazzes)
            {
                Role result;
                if (classRoleMap.TryGetValue(text, out result))
                {
                    mappedClazz = text;
                    if (mappedClazz.StartsWith("oracle.ewt") || mappedClazz.StartsWith("oracle.forms"))
                    {
                        this.isOra = true;
                    }
                    return result;
                }
            }
            mappedClazz = clazzes[0];
            return Role.Unknown;
        }

        private static readonly Dictionary<string, Role> classRoleMap = new Dictionary<string, Role>();

        private readonly int javaChildIndex;

        private bool isOra;
    }
}
