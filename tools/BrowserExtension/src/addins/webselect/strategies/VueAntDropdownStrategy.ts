import BaseDropdownStrategy from './BaseDropdownStrategy';

export default class VueAntDropdownStrategy extends BaseDropdownStrategy {
    strategyName = "antdv";
    order = 1;

    constructor() {
        super('div[class*="ant-select-dropdown"]', 'div[class*="ant-select-item ant-select-item-option"]', 'div[class*="ant-select-item-option-content"]');
    }

    validate(element: DropdownElement): boolean {
        if (element.classList.contains("ant-select-selector")) {
            return true;
        }
        if (element.parentElement?.classList.contains("ant-select-selector")) {
            return true;
        }
        else {
            return false;
        }
    }
}
