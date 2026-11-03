import BaseDropdownStrategy from './BaseDropdownStrategy';

export default class TaxDropdownStrategy extends BaseDropdownStrategy {
    strategyName = "chinatax";
    order = 5;

    constructor() {
        super('ul[class^="dropdown-menu"]', 'a[role="option"]');
    }

    validate(element: DropdownElement): boolean {
        if (element.getAttribute('data-toggle') == "dropdown") {
            return true;
        }
        if (element.parentElement?.getAttribute('data-toggle') == "dropdown") {
            return true;
        }
        else {
            return false;
        }
    }

}

