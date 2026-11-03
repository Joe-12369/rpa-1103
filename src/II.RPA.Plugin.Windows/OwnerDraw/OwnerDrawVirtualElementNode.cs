using FlaUI.Core;
using FlaUI.Core.Capturing;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Win32Tools;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    /// <summary>
    /// A logical cell backed by an ordinary UIA host and an injected Bridge.
    /// Operations that could otherwise hit the whole host are overridden and
    /// always re-resolve the cell before using physical input.
    /// </summary>
    internal sealed class OwnerDrawVirtualElementNode : UIANode
    {
        private readonly object snapshotGate = new object();
        private readonly UIANode host;
        private readonly OwnerDrawAdapterRegistry registry;
        private readonly OwnerDrawSelector selector;
        private readonly IntPtr hostHwnd;
        private readonly string virtualElementType;
        private int processId;
        private OwnerDrawElementSnapshot snapshot;
        private string selectorJson;

        internal OwnerDrawVirtualElementNode(UIANode host,
            OwnerDrawElementSnapshot snapshot,
            OwnerDrawAdapterRegistry registry = null)
            : base(RequireHost(host).automationElement, host)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (snapshot.Selector == null)
                throw new ArgumentException("Owner-draw snapshot has no selector.", "snapshot");
            if (snapshot.HostHwnd == IntPtr.Zero)
                throw new ArgumentException("Owner-draw snapshot has no host HWND.", "snapshot");

            this.host = host;
            this.snapshot = snapshot;
            selector = snapshot.Selector;
            hostHwnd = snapshot.HostHwnd;
            OwnerDrawWindowProbe.TryGetProcessId(hostHwnd, out processId);
            this.registry = registry ?? OwnerDrawAdapterRegistry.Shared;
            cacheDataSelector = host.cacheDataSelector;
            RootWindow = host.RootWindow;

            SelectorPropsType type = SelectorPropsType.RawElement;
            if (host.automationElement != null)
            {
                type |= host.automationElement.AutomationType == AutomationType.UIA3
                    ? SelectorPropsType.UIA3 : SelectorPropsType.UIA2;
            }
            virtualElementType = ((int)type).ToString();
        }

        public override IGuiNode AutomationElementParent
        {
            get { return host; }
        }

        public override string AutomationId
        {
            get { return string.Empty; }
        }

        public override string Description
        {
            get { return string.Empty; }
        }

        public override string Idx
        {
            get { return string.Empty; }
        }

        public override bool IsTopLevelWindow
        {
            get { return false; }
        }

        public override IGuiNode Parent
        {
            get { return host; }
        }

        public override string ProcessFullPath
        {
            get { return host.ProcessFullPath; }
        }

        public override int ProcessID
        {
            get { return processId; }
            set { processId = value; }
        }

        public override string ProcessName
        {
            get { return host.ProcessName; }
        }

        public override string UserDefinedID
        {
            get { return string.Empty; }
        }

        public override IntPtr WindowHandle
        {
            get { return hostHwnd; }
        }

        public override List<IGuiNode> Children
        {
            get { return new List<IGuiNode>(); }
        }

        public override Rectangle BoundingRectangle
        {
            get { return ResolveCurrent().ScreenBounds; }
        }

        public override string ClassName
        {
            get
            {
                lock (snapshotGate) return snapshot.HostClassName ?? string.Empty;
            }
        }

        public override string ControlType
        {
            get { return KindControlType(); }
        }

        public override string Name
        {
            get
            {
                // UI presentation can use the pick/last-resolve snapshot.  The
                // GetText component always performs a live Bridge read.
                lock (snapshotGate) return snapshot.Text ?? string.Empty;
            }
        }

        public override string Role
        {
            get { return KindControlType(); }
        }

        private string KindControlType()
        {
            // Source type is a stable, language-neutral identity. The picker overlay
            // translates it to a localized label via controlType.xml.
            switch (selector.PbKind)
            {
                case PowerBuilderCellKind.Label:
                    return "Text";
                case PowerBuilderCellKind.Input:
                    return "EditBox";
                default:
                    return "Cell";
            }
        }

        public override II.RPA.WinAutomation.CommonUtility.eGuiType GuiType
        {
            get { return II.RPA.WinAutomation.CommonUtility.eGuiType.Windows; }
        }

        public override bool IsTable
        {
            get { return false; }
        }

        public override string TableNodeSelector
        {
            get { return string.Empty; }
        }

        public override bool IsShow
        {
            get
            {
                lock (snapshotGate)
                {
                    return II.RPA.Plugin.Core.NativeMethods.IsWindow(hostHwnd)
                        && II.RPA.Plugin.Core.NativeMethods.IsWindowVisible(hostHwnd)
                        && snapshot.ScreenBounds.Width > 0
                        && snapshot.ScreenBounds.Height > 0;
                }
            }
        }

        public override bool ChromePageTable
        {
            get { return false; }
        }

        public override string NodeSelector
        {
            get { return GetSelector(false); }
        }

        public override string GetSelector(bool innerTextFirst)
        {
            if (!string.IsNullOrEmpty(selectorJson)) return selectorJson;

            string hostSelector = host.GetSelector(innerTextFirst);
            JToken root = JToken.Parse(hostSelector);
            JArray selectorArray;
            if (root is JArray directArray)
            {
                selectorArray = directArray;
            }
            else if (root is JObject envelope)
            {
                JProperty selectorProperty = envelope.Properties().FirstOrDefault(
                    property => string.Equals(property.Name,
                        ConstFields.SELECTOR_ELEMENT_SELECTOR_TAG,
                        StringComparison.OrdinalIgnoreCase));
                selectorArray = selectorProperty == null
                    ? null : selectorProperty.Value as JArray;
            }
            else
            {
                selectorArray = null;
            }
            if (selectorArray == null)
                throw new InvalidOperationException(
                    "Windows host selector has no selector array.");

            selectorArray.Add(BuildVirtualLeaf());
            selectorJson = root.ToString(Formatting.None);
            return selectorJson;
        }

        public override Point GetClickablePoint()
        {
            Rectangle bounds = ResolveCurrent().ScreenBounds;
            return new Point(bounds.Left + bounds.Width / 2,
                bounds.Top + bounds.Height / 2);
        }

        public override Bitmap ElementToBitmap()
        {
            Rectangle bounds = ResolveCurrent().ScreenBounds;
            using (CaptureImage image = Capture.Rectangle(bounds))
            {
                return new Bitmap(image.Bitmap);
            }
        }

        public override List<IGuiNode> FindAll(IGuiNode fromElement = null,
            int maxResult = 1, int milliseconds = 3000)
        {
            throw Unsupported("find descendants from a virtual Cell");
        }

        public override List<IGuiNode> FindAll(IGuiNode fromElement,
            int maxResult, int milliseconds, bool useCache)
        {
            throw Unsupported("find descendants from a virtual Cell");
        }

        public override string FindElementWithPicPath(string strPicPath)
        {
            throw Unsupported("find an image inside a virtual Cell");
        }

        public override string FindRelativeElementsInfo(string strRelativeSelector,
            ref JArray context)
        {
            throw Unsupported("find relative elements");
        }

        public override IGuiNode GetChildByIdx(int idx)
        {
            throw Unsupported("get child by index");
        }

        public override IGuiNode FindRelativeNode(int position, int offsetX,
            int offsetY)
        {
            throw Unsupported("find relative node");
        }

        public override Rectangle GetRegion(int positionType)
        {
            Rectangle cell = ResolveCurrent().ScreenBounds;
            if (positionType == 1)
            {
                Rectangle parent = host.BoundingRectangle;
                return new Rectangle(cell.X - parent.X, cell.Y - parent.Y,
                    cell.Width, cell.Height);
            }
            if (positionType == 2)
            {
                Rectangle hostAbsolute = host.BoundingRectangle;
                Rectangle hostRelative = host.GetRegion(2);
                int windowX = hostAbsolute.X - hostRelative.X;
                int windowY = hostAbsolute.Y - hostRelative.Y;
                return new Rectangle(cell.X - windowX, cell.Y - windowY,
                    cell.Width, cell.Height);
            }
            return cell;
        }

        public override string GetText()
        {
            OwnerDrawResult<string> result = registry.GetText(hostHwnd, selector);
            if (!result.IsSuccess) throw OperationFailed("get text", result.Code,
                result.Status);
            lock (snapshotGate) snapshot.Text = result.Value ?? string.Empty;
            return result.Value ?? string.Empty;
        }

        public override string GetElementAttribute(string name,
            string apiVersion)
        {
            string key = (name ?? string.Empty).Trim();
            if (key.Equals("selector", StringComparison.OrdinalIgnoreCase))
                return NodeSelector;
            if (key.Equals("name", StringComparison.OrdinalIgnoreCase)
                || key.Equals("text", StringComparison.OrdinalIgnoreCase)
                || key.Equals("gettext", StringComparison.OrdinalIgnoreCase)
                || key.Equals("value", StringComparison.OrdinalIgnoreCase))
                return GetText();
            if (key.Equals("classname", StringComparison.OrdinalIgnoreCase)
                || key.Equals("class", StringComparison.OrdinalIgnoreCase))
                return ClassName;
            if (key.Equals("controltype", StringComparison.OrdinalIgnoreCase)
                || key.Equals("localizedcontroltype",
                    StringComparison.OrdinalIgnoreCase)
                || key.Equals("role", StringComparison.OrdinalIgnoreCase))
                return KindControlType();
            if (key.Equals("bounding", StringComparison.OrdinalIgnoreCase)
                || key.Equals("boundingrectangle",
                    StringComparison.OrdinalIgnoreCase))
            {
                Rectangle bounds = ResolveCurrent().ScreenBounds;
                return new JObject
                {
                    ["x"] = bounds.X,
                    ["y"] = bounds.Y,
                    ["width"] = bounds.Width,
                    ["height"] = bounds.Height,
                }.ToString(Formatting.None);
            }
            if (key.Equals("isenabled", StringComparison.OrdinalIgnoreCase)
                || key.Equals("enableclick", StringComparison.OrdinalIgnoreCase))
            {
                ResolveCurrent();
                return bool.TrueString;
            }

            OwnerDrawSelectorProperty selectorProperty
                = OwnerDrawSelectorCodec.Encode(selector).FirstOrDefault(
                    property => string.Equals(property.Name, key,
                        StringComparison.OrdinalIgnoreCase));
            if (selectorProperty != null) return selectorProperty.Value;
            throw Unsupported("get attribute '" + key + "'");
        }

        public override string GetElementCheck()
        {
            throw Unsupported("get check state");
        }

        public override string GetElementSelect(int selectType = 1)
        {
            throw Unsupported("get selection");
        }

        public override string GetElementSelectEx(string strJson)
        {
            throw Unsupported("get selection options");
        }

        public override string GetTableData(string input)
        {
            throw Unsupported("get table data from a Cell");
        }

        public override string GetEnableClick()
        {
            ResolveCurrent();
            return bool.TrueString;
        }

        public override void SetForeground()
        {
            if (host.RootWindow != null)
            {
                host.RootWindow.Activate();
                return;
            }
            IntPtr root = II.RPA.Plugin.Core.NativeMethods.GetAncestor(hostHwnd,
                GetAncestorFlags.GetRoot);
            new WindowObject(root == IntPtr.Zero ? hostHwnd : root).Activate();
        }

        public override void Focus()
        {
            MouseClick(0, 0, new UiElementClickParams
            {
                operateType = MouseParamDef.SIMULATE_OPT,
                cursorPos = MouseParamDef.CUR_POS_CENTER,
            });
        }

        public override void IntoView()
        {
            // Resolve is the adapter-owned visibility operation.  VCL may
            // position a logical record; PB visual identities fail safely when
            // they are no longer available.  Never invoke host ScrollItem.
            ResolveCurrent();
        }

        public override void MouseMove(int cursorPos, int offsetX = 0,
            int offsetY = 0, bool isUsePerson = false)
        {
            MoveToCurrentCell(cursorPos, offsetX, offsetY, isUsePerson);
        }

        public override void MouseClick(int offsetX, int offsetY,
            UiElementClickParams clickParams = null)
        {
            UiElementClickParams options = clickParams ?? new UiElementClickParams();
            MoveToCurrentCell(options.cursorPos, offsetX, offsetY,
                options.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(options.AidKeyType,
                () => GUIUtils.MouseAction(options.RPAMousePresses,
                    RPAMouseButtons.Left));
        }

        public override void MouseDoubleClick(int offsetX, int offsetY,
            UiElementClickParams clickParams = null)
        {
            UiElementClickParams options = clickParams ?? new UiElementClickParams();
            MoveToCurrentCell(options.cursorPos, offsetX, offsetY,
                options.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(options.AidKeyType,
                () => Mouse.LeftDoubleClick());
        }

        public override void MouseRightClick(int offsetX, int offsetY,
            UiElementClickParams clickParams = null)
        {
            UiElementClickParams options = clickParams ?? new UiElementClickParams();
            MoveToCurrentCell(options.cursorPos, offsetX, offsetY,
                options.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(options.AidKeyType,
                () => GUIUtils.MouseAction(options.RPAMousePresses,
                    RPAMouseButtons.Right));
        }

        public override void MouseRightDoubleClick(int offsetX, int offsetY,
            UiElementClickParams clickParams = null)
        {
            UiElementClickParams options = clickParams ?? new UiElementClickParams();
            MoveToCurrentCell(options.cursorPos, offsetX, offsetY,
                options.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(options.AidKeyType,
                () => Mouse.RightDoubleClick());
        }

        public override void MouseConterClick(int offsetX, int offsetY,
            UiElementClickParams clickParams = null)
        {
            UiElementClickParams options = clickParams ?? new UiElementClickParams();
            MoveToCurrentCell(options.cursorPos, offsetX, offsetY,
                options.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(options.AidKeyType,
                () => GUIUtils.MouseAction(options.RPAMousePresses,
                    RPAMouseButtons.Middle));
        }

        public override void MouseConterDoubleClick(int offsetX, int offsetY,
            UiElementClickParams clickParams = null)
        {
            UiElementClickParams options = clickParams ?? new UiElementClickParams();
            MoveToCurrentCell(options.cursorPos, offsetX, offsetY,
                options.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(options.AidKeyType,
                () => Mouse.DoubleClick(MouseButton.Middle));
        }

        public override void MouseThreeClick(int offsetX, int offsetY,
            UiElementClickParams clickParams = null)
        {
            UiElementClickParams options = clickParams ?? new UiElementClickParams();
            MoveToCurrentCell(options.cursorPos, offsetX, offsetY,
                options.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(options.AidKeyType, () =>
            {
                Mouse.LeftDoubleClick();
                Mouse.LeftClick();
            });
        }

        public override void MouseRightThreeClick(int offsetX, int offsetY,
            UiElementClickParams clickParams = null)
        {
            UiElementClickParams options = clickParams ?? new UiElementClickParams();
            MoveToCurrentCell(options.cursorPos, offsetX, offsetY,
                options.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(options.AidKeyType, () =>
            {
                Mouse.RightDoubleClick();
                Mouse.RightClick();
            });
        }

        public override void MouseConterThreeClick(int offsetX, int offsetY,
            UiElementClickParams clickParams = null)
        {
            UiElementClickParams options = clickParams ?? new UiElementClickParams();
            MoveToCurrentCell(options.cursorPos, offsetX, offsetY,
                options.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(options.AidKeyType, () =>
            {
                Mouse.DoubleClick(MouseButton.Middle);
                Mouse.Click(MouseButton.Middle);
            });
        }

        public override void MouseHover(UiElementHoverParams hoverParams = null)
        {
            UiElementHoverParams options = hoverParams ?? new UiElementHoverParams();
            MoveToCurrentCell(options.cursorPos, options.offsetX, options.offsetY,
                options.isUsePerson);
        }

        public override void SetText(string text, int isReplace, bool isSimulate)
        {
            InputText(text, isReplace, 1, 50, false);
        }

        public override string ClickTableCell(string input)
        {
            throw Unsupported("click table cell");
        }

        public override void SetElementAttribute(string name, string value)
        {
            throw Unsupported("set attribute");
        }

        public override void SetElementCheck(int check)
        {
            throw Unsupported("set check state");
        }

        public override void SetElementMultipleSelect(int selectType,
            string setValue)
        {
            throw Unsupported("set multiple selection");
        }

        public override void SetElementSelect(int selectType, string setValue,
            int matchMode = 0)
        {
            throw Unsupported("set selection");
        }

        public override void SetElementSelect(int selectType, string setValue)
        {
            throw Unsupported("set selection");
        }

        public override void SetElementSelectWithMatchModel(int selectType,
            int matchModel, string setValue)
        {
            throw Unsupported("set selection with match mode");
        }

        public override void SetText(string text, int isReplace, int inputType,
            int interval = 0)
        {
            InputText(text, isReplace, inputType, interval, false);
        }

        public override bool SetText(ParamSetTextModel param)
        {
            if (param == null) throw new ArgumentNullException("param");
            if (string.Equals(selector.AdapterId, OwnerDrawSelectorCodec.VclAdapterId,
                    StringComparison.OrdinalIgnoreCase)
                && selector.IdentityStrength == OwnerDrawIdentityStrength.LogicalCell
                && selector.LogicalRow < 0)
                throw new InvalidOperationException("A VCL header cell is not editable.");

            MouseClick(param.OffsetX, param.OffsetY, new UiElementClickParams
            {
                operateType = MouseParamDef.SIMULATE_OPT,
                cursorPos = param.ElementPosition,
            });
            Thread.Sleep(Math.Max(0, param.FocusTimeout));
            InputAfterFocus(param.text, param.isReplace, param.inputType,
                param.Interval, param.Trigger == 1 || param.ForceSwitchENG == 1);
            if (param.isEnter == 1)
            {
                Thread.Sleep(Math.Max(0, param.delaybeforEnter));
                EnsureInputTargetFocused();
                Keyboard.Type(VirtualKeyShort.RETURN);
            }
            if (param.isVerify != 1) return true;

            Thread.Sleep(Math.Max(100, param.Interval));
            string actual = GetText();
            string expected = param.text ?? string.Empty;
            return param.isReplace == 0
                ? string.Equals(actual, expected, StringComparison.Ordinal)
                : actual.EndsWith(expected, StringComparison.Ordinal);
        }

        public override List<SelectorProps> MatchNextButtonSelector(
            ParamDoElementEditorModel paramModel)
        {
            throw Unsupported("match a data-collection next button");
        }

        public override List<SelectorProps> AutoBulidCollectDataSelector(
            ParamBulidCollectDataSelectorModel paramModel)
        {
            throw Unsupported("build a table data-collection selector");
        }

        public override List<SelectorProps> CompareBulidCollectDataSelector(
            ParamBulidCollectDataSelectorModel paramModel)
        {
            throw Unsupported("compare table data-collection selectors");
        }

        public override string GetNextElementBuildSelector(
            List<string> cssSelectorList)
        {
            throw Unsupported("build a next-page selector");
        }

        public override string GetHtml()
        {
            throw Unsupported("get HTML");
        }

        public override string GetCleanHtml()
        {
            throw Unsupported("get clean HTML");
        }

        public override string GetSubElements(ParamGetSubElements param)
        {
            throw Unsupported("get sub-elements");
        }

        public override string GetParent(ParamGetParent param)
        {
            throw Unsupported("run a data-collection parent query");
        }

        public override string GetSiblings(ParamGetSiblings param)
        {
            throw Unsupported("get siblings");
        }

        public override IGuiNode[] QueryTargetSelectorAll(
            ParamQueryTargetSelectorAllModel param)
        {
            throw Unsupported("query target selectors below a virtual Cell");
        }

        public override string GetElementAttribute_V1(string name,
            bool isIntact, string apiVersion)
        {
            return GetElementAttribute(name, apiVersion);
        }

        public override List<string> GetSelectorAll(List<IGuiNode> guiNodes,
            string sourceSelector)
        {
            throw Unsupported("build relative selectors for a node collection");
        }

        public override List<Rectangle> GetRegionAll(List<IGuiNode> guiNodes)
        {
            if (guiNodes == null) throw new ArgumentNullException("guiNodes");
            return guiNodes.Select(node => node.GetRegion(3)).ToList();
        }

        public override List<string> GetTextAll(List<IGuiNode> guiNodes)
        {
            if (guiNodes == null) throw new ArgumentNullException("guiNodes");
            return guiNodes.Select(node => node.GetText()).ToList();
        }

        public override List<string> GetHtmlAll(List<IGuiNode> guiNodes)
        {
            throw Unsupported("get HTML for a node collection");
        }

        public override List<string> GetAttributeAll(List<IGuiNode> guiNodes,
            string attributeName, bool isIntact, string apiVersion)
        {
            if (guiNodes == null) throw new ArgumentNullException("guiNodes");
            return guiNodes.Select(node => node.GetElementAttribute(attributeName,
                apiVersion)).ToList();
        }

        private void InputText(string text, int isReplace, int inputType,
            int interval, bool switchInputMethod)
        {
            if (string.Equals(selector.AdapterId, OwnerDrawSelectorCodec.VclAdapterId,
                    StringComparison.OrdinalIgnoreCase)
                && selector.IdentityStrength == OwnerDrawIdentityStrength.LogicalCell
                && selector.LogicalRow < 0)
                throw new InvalidOperationException("A VCL header cell is not editable.");
            Focus();
            Thread.Sleep(100);
            InputAfterFocus(text, isReplace, inputType, interval,
                switchInputMethod);
        }

        private void InputAfterFocus(string text, int isReplace, int inputType,
            int interval, bool switchInputMethod)
        {
            EnsureInputTargetFocused();
            // Owner-draw cells have no UIA ValuePattern.  Product input mode 0
            // therefore uses the same safe keystroke route as simulation mode.
            InputMode mode = inputType == 2 ? InputMode.ClipBoard : InputMode.Simulate;
            var action = new Win32Action(hostHwnd);
            action.Input(text ?? string.Empty, mode, isReplace == 0,
                switchInputMethod, Math.Max(0, interval));
        }

        private void EnsureInputTargetFocused()
        {
            uint hostThreadId = II.RPA.Plugin.Core.NativeMethods
                .GetWindowThreadProcessId(hostHwnd, out uint hostProcessId);
            IntPtr foregroundHwnd = II.RPA.Plugin.Core.NativeMethods
                .GetForegroundWindow();
            uint foregroundThreadId = II.RPA.Plugin.Core.NativeMethods
                .GetWindowThreadProcessId(foregroundHwnd,
                    out uint foregroundProcessId);
            IntPtr hostRoot = II.RPA.Plugin.Core.NativeMethods.GetAncestor(
                hostHwnd, GetAncestorFlags.GetRoot);
            IntPtr foregroundRoot = II.RPA.Plugin.Core.NativeMethods.GetAncestor(
                foregroundHwnd, GetAncestorFlags.GetRoot);
            if (hostThreadId == 0 || foregroundThreadId == 0
                || hostProcessId == 0 || hostProcessId != foregroundProcessId)
            {
                throw Unsupported("input because the target process does not own foreground focus");
            }
            if (hostRoot == IntPtr.Zero || hostRoot != foregroundRoot)
            {
                throw Unsupported("input because the target Grid/DataWindow is not in the foreground root window");
            }

            var threadInfo = new II.RPA.Plugin.Core.Extensions.GUITHREADINFO
            {
                cbSize = Marshal.SizeOf(typeof(
                    II.RPA.Plugin.Core.Extensions.GUITHREADINFO)),
            };
            if (!II.RPA.Plugin.Core.Extensions.GetGUIThreadInfo(
                    unchecked((int)foregroundThreadId), ref threadInfo)
                || threadInfo.hwndFocus == IntPtr.Zero
                || !IsSameOrChildWindow(hostHwnd, threadInfo.hwndFocus))
            {
                throw Unsupported("input because the resolved Cell or its editor does not own focus");
            }
        }

        private static bool IsSameOrChildWindow(IntPtr ancestor,
            IntPtr candidate)
        {
            IntPtr current = candidate;
            for (int depth = 0; current != IntPtr.Zero && depth < 64; depth++)
            {
                if (current == ancestor) return true;
                IntPtr parent = II.RPA.Plugin.Core.NativeMethods.GetAncestor(
                    current, GetAncestorFlags.GetParent);
                if (parent == current) break;
                current = parent;
            }
            return false;
        }

        private void MoveToCurrentCell(int cursorPosition, int offsetX,
            int offsetY, bool humanMotion)
        {
            SetForeground();
            Rectangle bounds = ResolveCurrent().ScreenBounds;
            if (!TryGetMouseTarget(bounds, cursorPosition, offsetX, offsetY,
                    out Point point))
            {
                throw new InvalidOperationException(
                    "Owner-draw mouse target is outside the latest resolved Cell bounds; "
                    + "the operation was not forwarded to its host Pane/Grid.");
            }
            EnsureMouseTargetAvailable(point);
            II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(
                humanMotion, point);
            EnsureMouseTargetAvailable(point);
        }

        private void EnsureMouseTargetAvailable(Point point)
        {
            IntPtr pointHwnd = II.RPA.Plugin.Core.NativeMethods.WindowFromPoint(point);
            II.RPA.Plugin.Core.NativeMethods.GetWindowThreadProcessId(hostHwnd,
                out uint hostProcessId);
            II.RPA.Plugin.Core.NativeMethods.GetWindowThreadProcessId(pointHwnd,
                out uint pointProcessId);
            if (pointHwnd == IntPtr.Zero || hostProcessId == 0
                || pointProcessId != hostProcessId
                || !IsSameOrChildWindow(hostHwnd, pointHwnd))
            {
                throw new InvalidOperationException(
                    "Owner-draw Cell is obscured or no longer owns the resolved point; "
                    + "the mouse operation was not sent.");
            }
        }

        private OwnerDrawElementSnapshot ResolveCurrent()
        {
            OwnerDrawResult<OwnerDrawElementSnapshot> result
                = registry.Resolve(hostHwnd, selector);
            if (!result.IsSuccess) throw OperationFailed("resolve", result.Code,
                result.Status);
            if (result.Value == null || result.Value.HostHwnd != hostHwnd
                || result.Value.ScreenBounds.Width <= 0
                || result.Value.ScreenBounds.Height <= 0)
            {
                throw new InvalidOperationException(
                    "Owner-draw resolve returned an invalid or different host cell.");
            }
            lock (snapshotGate) snapshot = result.Value;
            return result.Value;
        }

        private JObject BuildVirtualLeaf()
        {
            var leaf = new SelectorProps
            {
                Name = "Cell",
                Type = virtualElementType,
                Accurate = ConstFields.PROP_ACCURATE_TURE,
                Props = OwnerDrawSelectorCodec.Encode(selector).Select(property =>
                    new Props
                    {
                        Name = property.Name,
                        Value = property.Value,
                        Pattern = ConstFields.PATTERN_EQUAL,
                        Accurate = property.Accurate
                            ? ConstFields.PROP_ACCURATE_TURE
                            : ConstFields.PROP_ACCURATE_FALSE,
                    }).ToList(),
            };
            return JObject.FromObject(leaf);
        }

        private static UIANode RequireHost(UIANode value)
        {
            return value ?? throw new ArgumentNullException("host");
        }

        private static InvalidOperationException OperationFailed(string operation,
            OwnerDrawFailureCode code, string status)
        {
            return new InvalidOperationException("Owner-draw " + operation
                + " failed (" + code + "): " + (status ?? string.Empty));
        }

        private static InvalidOperationException Unsupported(string operation)
        {
            return new InvalidOperationException("Owner-draw Cell does not support "
                + operation + "; the operation was not forwarded to its host Pane/Grid.");
        }

        internal static bool TryGetMouseTarget(Rectangle bounds, int position,
            int offsetX, int offsetY, out Point target)
        {
            Point origin = PointFor(bounds, position);
            long x = (long)origin.X + offsetX;
            long y = (long)origin.Y + offsetY;
            long right = (long)bounds.Left + bounds.Width;
            long bottom = (long)bounds.Top + bounds.Height;
            if (bounds.Width <= 0 || bounds.Height <= 0
                || x < bounds.Left || x >= right
                || y < bounds.Top || y >= bottom
                || x < int.MinValue || x > int.MaxValue
                || y < int.MinValue || y > int.MaxValue)
            {
                target = Point.Empty;
                return false;
            }

            target = new Point((int)x, (int)y);
            return true;
        }

        private static Point PointFor(Rectangle bounds, int position)
        {
            switch (position)
            {
                case MouseParamDef.CUR_POS_LEFT_TOP:
                    return new Point(bounds.Left, bounds.Top);
                case MouseParamDef.CUR_POS_RIGHT_TOP:
                    return new Point(bounds.Right - 1, bounds.Top);
                case MouseParamDef.CUR_POS_LEFT_BOTTOM:
                    return new Point(bounds.Left, bounds.Bottom - 1);
                case MouseParamDef.CUR_POS_RIGHT_BOTTOM:
                    return new Point(bounds.Right - 1, bounds.Bottom - 1);
                case MouseParamDef.CUR_POS_CENTER_TOP:
                    return new Point(bounds.Left + bounds.Width / 2, bounds.Top);
                case MouseParamDef.CUR_POS_CENTER_LEFT:
                    return new Point(bounds.Left, bounds.Top + bounds.Height / 2);
                case MouseParamDef.CUR_POS_CENTER_RIGHT:
                    return new Point(bounds.Right - 1,
                        bounds.Top + bounds.Height / 2);
                case MouseParamDef.CUR_POS_CENTER_BOTTOM:
                    return new Point(bounds.Left + bounds.Width / 2,
                        bounds.Bottom - 1);
                case MouseParamDef.CUR_POS_RANDOM:
                    return bounds.GetRandomPoint();
                default:
                    return new Point(bounds.Left + bounds.Width / 2,
                        bounds.Top + bounds.Height / 2);
            }
        }
    }
}
