class ElementNotFoundError extends Error {
    constructor(name: string) {
        super(`${name} Dropdown element not found`);
        this.name = "ElementNotFoundError";
    }
}

