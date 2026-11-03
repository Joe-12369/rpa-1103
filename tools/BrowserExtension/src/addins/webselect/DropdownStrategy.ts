export interface DropdownStrategy {
    strategyName: string;
    order:number;
    validate(element: DropdownElement): boolean;
    getValues(element: DropdownElement): Dropdown;
    setValues(element: DropdownElement, values: any[]): void;
}