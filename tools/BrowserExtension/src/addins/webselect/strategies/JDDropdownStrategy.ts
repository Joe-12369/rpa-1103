import BaseDropdownStrategy from './BaseDropdownStrategy';

export default class JDDropdownStrategy extends BaseDropdownStrategy {
    strategyName = "jingdong";
    order = 4;

    constructor() {
        super("div[class='ui-switchable-panel ui-switchable-panel-selected'", 'li', 'a');
    }

    validate(element: DropdownElement): boolean {
        if (/^ui-switchable-item/.test(element.className)) {
            return true;
        }
        if (/^ui-switchable-item/.test(element.parentElement?.className ?? "")) {
            return true;
        }
        else {
            return false;
        }
    }
    checkSelected(element: DropdownElement): boolean {
        return element.className == "ui-area-current";
    }

}

