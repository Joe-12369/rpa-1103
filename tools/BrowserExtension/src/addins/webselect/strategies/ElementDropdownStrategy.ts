import BaseDropdownStrategy from './BaseDropdownStrategy';

export default class ElementDropdownStrategy extends BaseDropdownStrategy {
    strategyName = "element";
    order = 2;

    constructor() {
        super('ul[class*="el-select-dropdown__list"]', 'li[class*="el-select-dropdown__item"]');
    }

    validate(element: DropdownElement): boolean {
        if (element.classList.contains("el-select")) {
            return true;
        }
        if (element.parentElement?.classList.contains("el-select")) {
            return true;
        }
        if (element.parentElement?.parentElement?.classList.contains("el-select")) {
            return true;
        }
        else {
            return false;
        }
    }
    checkSelected(element: DropdownElement): boolean {
        return element.classList.contains('selected');
    }
}
