import BaseDropdownStrategy from './BaseDropdownStrategy';

export default class BootstrapDropdownStrategy extends BaseDropdownStrategy {
    strategyName = "bootstrap";
    order = 1;

    constructor() {
        super('ul[class*="dropdown-menu"]', 'li');
    }


    validate(element: DropdownElement): boolean {
        return [element, element.parentElement, element.parentElement?.parentElement].some(
            ele => {
                if (ele?.classList.contains("dropdown-toggle")) {
                    if (ele?.parentElement?.id) {
                        element.setAttribute("select-owns", ele.parentElement.id);
                    }
                    else {
                        let _owns = `rpa_${Date.now()}`;
                        ele.parentElement?.setAttribute("id", _owns);
                        element.setAttribute("select-owns", _owns);
                    }                  
                    return true;
                }
                return false;
            }
        );

    }
}
