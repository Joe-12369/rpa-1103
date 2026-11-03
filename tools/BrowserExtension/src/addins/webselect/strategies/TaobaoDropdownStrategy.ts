import BaseDropdownStrategy from './BaseDropdownStrategy';

export default class TaobaoDropdownStrategy extends BaseDropdownStrategy {
    strategyName = "taobao";
    order = 2;

    constructor() {
        super('ul[role="menu"]', "li[role='menuitem']");
    }

    validate(element: DropdownElement): boolean {
        if (element.getAttribute('role') === 'combobox') {
            return true;
        }
        if (element.parentElement?.getAttribute('role') === 'combobox') {
            return true;
        }
        else {
            return false;
        }
    }
    checkSelected(element: DropdownElement): boolean {      
        if (element.getAttribute('aria-selected') === 'true') {
            return true;
        }
        return false;
    }
}

