
import { DropdownStrategyManager } from './DropdownStrategyManager';
const strategyManager = new DropdownStrategyManager();
function validate(element: DropdownElement, click: boolean = false): ResponseResult {
    return strategyManager.validateDropdown(element, click);
}
function getValues(element: DropdownElement, strategyName: string | null = null): ResponseResult {
    let result = strategyManager.getDropdownValues(element, strategyName);

    if (result && result.success && (result as any).result && (result as any).result.items && (result as any).result.items.length > 0) {
        setTimeout(() => {
            try {
                if (lastSimulatedDropdown && lastSimulatedDropdown.isConnected) {
                    ["mousedown", "mouseup", "click"].forEach(type => {
                        var event = new MouseEvent(type, {
                            'view': window,
                            'bubbles': true,
                            'cancelable': true
                        });
                        lastSimulatedDropdown!.dispatchEvent(event);
                    });
                }
            } catch (e) { }
        }, 100);
    }

    return result;
}
function setValues(element: DropdownElement, values: any[], strategyName: string | null = null): ResponseResult {
    return strategyManager.setDropdownValues(element, strategyName, values);
}

const globalWindow = (window as any);
globalWindow.webselect = {
    validate,
    getValues,
    setValues,
};

let lastSimulatedDropdown: HTMLElement | null = null;
globalWindow.HTMLElement.prototype.simulateClick = function () {
    const dropdown: DropdownElement = this as DropdownElement;

    if (lastSimulatedDropdown && lastSimulatedDropdown !== dropdown && lastSimulatedDropdown.isConnected) {
        try {
            ["mousedown", "mouseup", "click"].forEach(type => {
                var event = new MouseEvent(type, { 'view': window, 'bubbles': true, 'cancelable': true });
                lastSimulatedDropdown!.dispatchEvent(event);
            });
        } catch (e) { }
    }
    lastSimulatedDropdown = dropdown;

    ["mousedown", "mouseup", "click"].forEach(type => {
        var event = new MouseEvent(type, {
            'view': window,
            'bubbles': true,
            'cancelable': true
        });
        dropdown.dispatchEvent(event);
    });
};
globalWindow.HTMLElement.prototype.getDropdownPanel = function (targetSelector: string, validatePosition: boolean = true) {
    const element: HTMLElement = this as HTMLElement;
    let elements = element.ownerDocument.querySelectorAll<DropdownElement>(targetSelector);;
    var owns = element.getAttribute("select-owns");
    if (owns) {
        let ownsPanel = element.ownerDocument.getElementById(owns);
        console.debug('dropdown panel owns:', ownsPanel);
        if (ownsPanel) {
            if (ownsPanel.matches(targetSelector)) {
                return ownsPanel;
            }
            else {
                elements = ownsPanel.querySelectorAll<DropdownElement>(targetSelector);;
            }
        }
        else {
            console.debug(`No visible element found for owns: ${owns}`);
            return null;
        }
    }
    console.debug('find dropdown panel length:', elements.length);
    let bestPanel: HTMLElement | null = null;
    let minDistance = Infinity;

    for (var i = 0; i < elements.length; i++) {
        let panel = elements[i];
        if (panel.isVisible()) {
            if (!validatePosition) return panel;
            let elementRect = element.getBoundingClientRect();
            let panelRect = panel.getBoundingClientRect();

            // 水平重叠间距
            let dx = Math.max(0, elementRect.left - panelRect.right, panelRect.left - elementRect.right);
            // 真实下拉框边缘应该是贴合在一起的（比如它的上边缘贴着输入框的下边缘，或者它的下边缘贴着输入框的上边缘）
            // 纯粹重叠会导致判断失败，所以改用边缘贴合距离
            let verticalGap = Math.min(
                Math.abs(panelRect.top - elementRect.bottom), // 正常向下展开
                Math.abs(panelRect.bottom - elementRect.top)  // 向上展开
            );

            let distance = Math.sqrt(dx * dx + verticalGap * verticalGap);

            console.debug(`panel [${i}] bounding rect:`, panelRect, 'distance:', distance, 'dx:', dx, 'verticalGap:', verticalGap, 'html block:', panel.outerHTML.substring(0, 150));

            // 严格对齐判断：正确的下拉选项框必定紧贴着操作面板边界
            // 如果垂直贴合缝隙超过80px（稍微放宽数值以防止带较大的margin或padding的组件），则必然不属于它
            if (dx > 50 || verticalGap > 80) {
                console.debug(`panel [${i}] is not edge-aligned (dx:${dx}, gap:${verticalGap}), ignoring.`);
                continue;
            }

            if (distance < minDistance) {
                minDistance = distance;
                bestPanel = panel;
            }
        }
    }

    if (bestPanel) {
        console.debug('selected best panel distance:', minDistance, bestPanel);
        return bestPanel;
    }

    console.debug(`No visible element found for selector: ${targetSelector}`);
    return null;
}
globalWindow.HTMLElement.prototype.isVisible = function () {
    if (!this) return false;

    // 必须有实际的渲染大小
    const rect = this.getBoundingClientRect();
    if (rect.width > 0 && rect.height > 0) {
        // 补充判断透明度和 visibility，防止占位但不可见
        const style = getComputedStyle(this);
        if (style.visibility === 'hidden' || style.opacity === '0') {
            return false;
        }
        return true;
    }

    // 对于长宽为 0 的元素，无论其 CSS 如何，都视为未渲染或不可见
    return false;
};
