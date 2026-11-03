import { DropdownStrategy } from './DropdownStrategy';

export function loadStrategies(): { [key: string]: DropdownStrategy } {
    const strategies: { [key: string]: DropdownStrategy } = {};

    const modules = import.meta.glob('./strategies/*.ts', { eager: true });

    for (const path in modules) {
        const strategyModule = modules[path] as { default: new () => DropdownStrategy };
        const strategyClass = new strategyModule.default();
        strategies[strategyClass.strategyName] = strategyClass;
    }

    return strategies;
}