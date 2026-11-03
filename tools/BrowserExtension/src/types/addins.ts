
export type ObfuscateLevel = "low" | "medium" | "high";
export type AddinKind = "legacy" | "module" | "";
export type AddinType = "background" | "content" | "";


export interface AddinsItem {
    name?: string;
    description?: string;
    type?: AddinType;
    kind?: AddinKind;
    entry?: string;
    enabled?: boolean;
    build?: NormalizedPhase;
    publish?: NormalizedPhase;
    target?: string;
    target_browser?: string[];
}

export interface Defaults extends AddinsItem {
    outputDir: string;
}

export interface AddinsJsonRoot {
    version?: number;
    defaults: Defaults;
    libs?: AddinsItem[];
    items?: AddinsItem[];
}

export interface NormalizedPhase {
    minify: boolean;
    sourcemap: boolean;
    embed: boolean;
    obfuscate: false | ObfuscateLevel
}

export interface NormalizedAddin extends AddinsItem {
    build: NormalizedPhase;
    publish: NormalizedPhase;
    sourceEntry: string;
    outputFileName: string;
    isLegacy: boolean;
    sourceCategory: "libs" | "items";
}

export interface ScanResult {
    items: NormalizedAddin[];
    warnings: string[];
    outputDir: string;
    configPath: string;
}

export interface NormalizedAddinItem {
    name: string;
    description: string;
    type: AddinType;
    kind: AddinKind;
    enabled: boolean;
    entry: string;
    outputFileName: string;
    minify: boolean;
    obfuscate: false | ObfuscateLevel;
    sourcemap: boolean;
    target: string;
    target_browser: string[];
    sourceCategory: "libs" | "items";
    isLegacy: boolean;
    embed: boolean;
    warnings: string[];

}
