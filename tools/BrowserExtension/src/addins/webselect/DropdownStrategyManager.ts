import { DropdownStrategyFactory } from './DropdownStrategyFactory';
import { DropdownStrategy } from './DropdownStrategy';
import domainStrategyMap from './DomainStrategyMap';
import { loadStrategies } from './LoadStrategies';

export class DropdownStrategyManager {
    private strategyFactory: DropdownStrategyFactory;
    private domainStrategyMap: { [domain: string]: string };
    private strategies: { [key: string]: DropdownStrategy } = {};
    constructor() {
        this.strategyFactory = new DropdownStrategyFactory();
        this.domainStrategyMap = domainStrategyMap;
        this.strategies = loadStrategies();
        this.registerStrategies(this.strategies);
    }

    private registerStrategies(strategies: { [key: string]: DropdownStrategy }): void {
        this.strategyFactory.registerStrategiesFromObject(strategies);
    }

    private getStrategyForDomain(domain: string): string | null {
        return this.domainStrategyMap[domain] || null;
    }

    private getCurrentDomain(): string {
        return window.location.hostname;
    }

    private findStrategy(element: DropdownElement, strategyName: string | null): DropdownStrategy | null {
        if (!strategyName) {
            const domain = this.getCurrentDomain();
            strategyName = this.getStrategyForDomain(domain);
        }

        let strategy = this.strategyFactory.createStrategy(strategyName);
        if (strategy && strategy.validate(element)) {
            return strategy;
        }

        for (const item in this.strategies) {
            if (strategyName === item) {
                continue;
            }
            strategy = this.strategyFactory.createStrategy(item);
            if (strategy?.validate(element)) {
                return strategy;
            }
        }
        return null;
    }

    public validateDropdown(element: DropdownElement, click: boolean = false): ResponseResult {
        let strategy = this.findStrategy(element, null);
        if (strategy != null && strategy.validate(element)) {
            console.log("find dropdown strategy:", strategy?.strategyName);
            return { success: true, result: strategy.strategyName };
        }
        else {
            return { success: false, message: "dropdown element not found" }
        }
    }

    public getDropdownValues(element: DropdownElement, strategyName: string | null): ResponseResult {
        console.debug("%get dropdown values: " + Date(), "color:red;font-size:18px");
        try {
            const strategy = this.findStrategy(element, strategyName);
            if (strategy == null) {
                return { success: false, message: `${strategyName} strategy validation failed` };
            }
            const values = strategy.getValues(element);
            return { success: true, result: values };
        } catch (error) {
            console.error('get dropdown values error:', error);
            //显示下拉框
            element.simulateClick();
            return {
                success: false,
                message: error instanceof Error ? error.message : "Unknown error"
            };
        }
    }

    public setDropdownValues(element: DropdownElement, strategyName: string | null, values: any[]): ResponseResult {
        console.debug("%cset dropdown values: " + Date(), "color:red;font-size:18px");
        try {
            const strategy = this.findStrategy(element, strategyName);
            if (strategy == null) {
                return { success: false, message: `${strategyName} validation failed` };
            }
            strategy.setValues(element, values);
            return { success: true };
        } catch (error) {
            console.error('set dropdown values error:', error);
            //显示下拉框
            element.simulateClick();
            return {
                success: false,
                message: error instanceof Error ? error.message : "Unknown error"
            };
        }
    }
}
