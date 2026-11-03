import { DropdownStrategy } from '../DropdownStrategy';

export default class BaseDropdownStrategy implements DropdownStrategy {
    strategyName = "base";
    order = 0;

    protected dropdownClass: string;
    protected dropdownItemClass: string;
    protected dropdownItemClickClass: string;

    constructor(dropdownClass: string = "[role='listbox']", dropdownItemClass: string = "[role='option']", dropdownItemClickClass: string = "") {
        this.dropdownClass = dropdownClass;
        this.dropdownItemClass = dropdownItemClass;
        this.dropdownItemClickClass = dropdownItemClickClass;
    }

    validate(element: DropdownElement): boolean {
        return [element, element.parentElement, element.parentElement?.parentElement].some(
            ele => {
                if (ele?.getAttribute('role') == 'combobox') {
                    let owns = ele.getAttribute("aria-owns") ?? ele.parentElement?.getAttribute("aria-owns") ?? "";
                    console.log('validate owns', owns);
                    element.setAttribute("select-owns", ele.getAttribute("aria-owns") ?? ele.parentElement?.getAttribute("aria-owns") ?? "");
                    return true;
                }
                return false;
            }
        );
    }
    getValues(element: DropdownElement): Dropdown {
        const dropdown: Dropdown = { items: [], selected: [] };
        let dropdownMenu = element.getDropdownPanel(this.dropdownClass, this.strategyName != "taobao-addr");
        const menuItems = dropdownMenu?.querySelectorAll<DropdownElement>(this.dropdownItemClass);
        if (menuItems == null) {
            throw new Error(`${dropdownMenu},items not found`);
        }
        menuItems.forEach(item => {
            const text = item.innerText.trim();
            dropdown.items.push(text);
            if (this.checkSelected(item)) {
                dropdown.selected.push(text);
            }
        });
        return dropdown;
    }
    setValues(element: DropdownElement, values: any[]): void {
        let dropdownMenu = element.getDropdownPanel(this.dropdownClass, this.strategyName != "taobao-addr");
        const menuItems = dropdownMenu?.querySelectorAll<DropdownElement>(this.dropdownItemClass);
        if (menuItems == null) {
            throw new Error(`${dropdownMenu},items not found`);
        }

        values.forEach(item => {
            if (item >= 0 && menuItems?.[item]) {
                if (this.dropdownItemClickClass == "") {
                    menuItems?.[item].simulateClick();
                }
                else {
                    menuItems?.[item].querySelector<DropdownElement>(this.dropdownItemClickClass)?.simulateClick();
                }

            }
        });
    }

    checkSelected(element: DropdownElement): boolean {
        if (element.getAttribute('aria-selected') == "true") return true;
        if (element.className.indexOf('-selected') > 0) return true;
        if (element.className.indexOf('-active') > 0) return true;
        return false;
    }  
}
