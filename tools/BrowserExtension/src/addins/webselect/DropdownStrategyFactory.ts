import { DropdownStrategy } from './DropdownStrategy';

export class DropdownStrategyFactory {
    private strategies: { [key: string]: DropdownStrategy } = {};

    private registerStrategy(name: string, strategy: DropdownStrategy): void {
        this.strategies[name] = strategy;
    }

    createStrategy(name: string | null): DropdownStrategy | null {
        if (name == null) {
            return null;
        }
        const Strategy = this.strategies[name];
        return Strategy;
    }

    registerStrategiesFromObject(strategyObject: { [key: string]: DropdownStrategy }): void {
        for (const name in strategyObject) {
            this.registerStrategy(name, strategyObject[name]);
        }
    }
}
