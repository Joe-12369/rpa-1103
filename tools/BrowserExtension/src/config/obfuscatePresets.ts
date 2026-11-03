export const OBFUSCATE_PRESETS = {
    low: {
        compact: true,
        controlFlowFlattening: false,
        deadCodeInjection: false,
        renameGlobals: false,
        stringArray: false
    },
    medium: {
        compact: true,
        controlFlowFlattening: false,
        deadCodeInjection: false,
        stringArray: true,
        stringArrayRotate: true,
        stringArrayThreshold: 0.75
    },
    high: {
        compact: true,
        controlFlowFlattening: true,
        controlFlowFlatteningThreshold: 0.8,
        deadCodeInjection: true,
        deadCodeInjectionThreshold: 0.4,
        stringArray: true,
        stringArrayRotate: true,
        splitStrings: true,
        splitStringsChunkLength: 6,
        renameGlobals: true
    }
} as const;

export type ObfuscateLevel = keyof typeof OBFUSCATE_PRESETS;