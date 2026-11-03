interface DropdownElement extends HTMLElement {
    getDropdownPanel(selector: string, validatePostion: boolean): DropdownElement | null;
    simulateClick(): any;
    isVisible(): boolean;
}