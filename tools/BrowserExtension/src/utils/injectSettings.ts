export const INJECT_TIMEOUT_STORAGE_KEYS = {
    TARGET_FRAME_TIMEOUT_MS: "injectTargetFrameTimeoutMs",
    WARMUP_FRAME_TIMEOUT_MS: "injectWarmupFrameTimeoutMs",
} as const;

export const DEFAULT_INJECT_TIMEOUT_SETTINGS = {
    targetFrameTimeoutMs: 5000,
    warmupFrameTimeoutMs: 3000,
} as const;

const MIN_TIMEOUT_MS = 500;
const MAX_TIMEOUT_MS = 60000;

function normalizeTimeout(value: unknown, fallback: number): number {
    if (typeof value !== "number" || !Number.isFinite(value)) return fallback;
    const rounded = Math.round(value);
    return Math.min(MAX_TIMEOUT_MS, Math.max(MIN_TIMEOUT_MS, rounded));
}

export function normalizeInjectTimeoutSettings(raw: Record<string, unknown> = {}) {
    return {
        targetFrameTimeoutMs: normalizeTimeout(
            raw[INJECT_TIMEOUT_STORAGE_KEYS.TARGET_FRAME_TIMEOUT_MS],
            DEFAULT_INJECT_TIMEOUT_SETTINGS.targetFrameTimeoutMs
        ),
        warmupFrameTimeoutMs: normalizeTimeout(
            raw[INJECT_TIMEOUT_STORAGE_KEYS.WARMUP_FRAME_TIMEOUT_MS],
            DEFAULT_INJECT_TIMEOUT_SETTINGS.warmupFrameTimeoutMs
        ),
    };
}

export async function getInjectTimeoutSettings() {
    try {
        const result = await chrome.storage.local.get(Object.values(INJECT_TIMEOUT_STORAGE_KEYS));
        return normalizeInjectTimeoutSettings(result);
    } catch {
        return { ...DEFAULT_INJECT_TIMEOUT_SETTINGS };
    }
}
