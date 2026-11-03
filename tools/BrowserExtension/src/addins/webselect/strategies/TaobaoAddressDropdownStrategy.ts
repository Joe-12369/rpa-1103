import BaseDropdownStrategy from './BaseDropdownStrategy';

export default class TaobaoAddressDropdownStrategy extends BaseDropdownStrategy {
    strategyName = "taobao-addr";
    order = 3;

    constructor() {
        super('div[class^="AddressDialog"]', 'div[class^="UserAddress--userAddressItem"]', "div[class^='UserAddress--detailAddress']");
    }

    validate(element: DropdownElement): boolean {
        if (/^Address--select/.test(element.className)) {
            return true;
        }
        if (/^Address--select/.test(element.parentElement ? element.parentElement.className : "")) {
            return true;
        }
        else {
            return false;
        }
    }
    checkSelected(element: DropdownElement): boolean {
        if (element.className.indexOf('UserAddress--isSelected') != -1) {
            return true;
        }
        return false;
    }

}

