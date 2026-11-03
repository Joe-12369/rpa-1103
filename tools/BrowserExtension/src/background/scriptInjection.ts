// src/background/scriptInjection.ts
// @ts-nocheck

import { getInjectTimeoutSettings } from "@/utils/injectSettings";
import { log } from "@utils/logUtils";

const CONTENT_INIT_CODE_ID_KEY = "__CONTENT_INIT_CODE_ID__";

export const wrapCodeAsGlobalFunction = (n, c, p = `_${Date.now()}_`, t = '(function(){try{window["' + n + '"]=function(){' + p + '};return{ok:true};}catch(e){try{return{ok:false,error:String(e&&(e.stack||e.message||e))};}catch(ee){return{ok:false,error:"unknown"};}}})()', i = t.indexOf(p)) => i < 0 ? (() => { throw new Error() })() : t.slice(0, i) + c + t.slice(i + p.length);

export function clearContentInitCache() {
    delete window["__CONTENT_INIT__"];
    delete window[CONTENT_INIT_CODE_ID_KEY];
}

function errorMessage(e) {
    return String(e && (e.stack || e.message || e));
}

function withTimeout(promise, ms, timeoutReason) {
    let timer;
    const timeout = new Promise((_, reject) => {
        timer = setTimeout(() => reject(new Error(timeoutReason)), ms);
    });
    return Promise.race([promise, timeout]).finally(() => clearTimeout(timer));
}

function normalizeFrameIds(frameIds) {
    const ids = Array.isArray(frameIds) && frameIds.length ? frameIds : [0];
    return Array.from(new Set(ids.filter(id => typeof id === "number" && id >= 0)));
}

function wrapContentInitCode(code, codeId) {
    const codeIdLiteral = JSON.stringify(codeId || "");
    return `if (${codeIdLiteral} && window.iirpa && typeof window.iirpa.handle === "function" && window.iirpa.codeId === ${codeIdLiteral}) { return { code: 0, skipped: true }; }\n${code}`;
}

async function safeGetAllFrames(tabId) {
    return await new Promise((resolve) => {
        try {
            chrome.webNavigation.getAllFrames({ tabId }, (frames) => {
                const err = chrome.runtime.lastError;
                if (err) {
                    log.warn("[inject script] getAllFrames failed", { tabId, error: err.message });
                    resolve([]);
                    return;
                }
                resolve(Array.isArray(frames) ? frames : []);
            });
        } catch (e) {
            log.warn("[inject script] getAllFrames exception", { tabId, error: errorMessage(e) });
            resolve([]);
        }
    });
}

async function injectInitToFrames(tabId, frameIds, initFunc, timeoutMs, label) {
    const ids = normalizeFrameIds(frameIds);
    if (!ids.length) {
        throw new Error(`${label} has no valid frameIds`);
    }

    return await Promise.all(ids.map(async (frameId) => {
        const start = Date.now();
        const resp = await withTimeout(
            chrome.scripting.executeScript({
                target: { tabId, frameIds: [frameId] },
                world: "ISOLATED",
                func: initFunc,
                args: []
            }),
            timeoutMs,
            `${label} timeout, tabId=${tabId}, frameId=${frameId}`
        );
        log.debug(`[inject script] ${label} frame resp=`, {
            tabId,
            frameId,
            elapsed: Date.now() - start,
            resp
        });
        return resp;
    }));
}

async function warmupAllFramesBestEffort(tabId, injectedFrameIds, initFunc, warmupTimeoutMs) {
    try {
        const injected = new Set(normalizeFrameIds(injectedFrameIds));
        const frames = await safeGetAllFrames(tabId);
        const targets = frames.filter(frame => typeof frame?.frameId === "number" && !injected.has(frame.frameId));

        if (!targets.length) {
            log.debug("[inject script] warmup skipped, no extra frames", { tabId });
            return;
        }

        log.debug("[inject script] warmup frames=", targets.map(frame => ({
            frameId: frame.frameId,
            parentFrameId: frame.parentFrameId,
            url: frame.url
        })));

        await Promise.allSettled(targets.map(async (frame) => {
            try {
                await injectInitToFrames(tabId, [frame.frameId], initFunc, warmupTimeoutMs, "warmup");
            } catch (e) {
                log.warn("[inject script] warmup skipped", {
                    tabId,
                    frameId: frame.frameId,
                    parentFrameId: frame.parentFrameId,
                    url: frame.url,
                    error: errorMessage(e)
                });
            }
        }));
    } catch (e) {
        log.warn("[inject script] warmup failed", { tabId, error: errorMessage(e) });
    }
}

export async function handleInitInjectWithDebugger(target, params, done, deps) {
    log.debug("[inject script] target=", target);
    const dbg = window.debuggerManager;
    if (!dbg || typeof dbg.sendCommand !== "function") {
        log.error("[inject script] debuggerManager not available");
        done({ code: 1, error: { code: -1, message: "debuggerManager unavailable" } });
        return;
    }
    const attachToServiceWorkerAndSendCommand = deps?.attachToServiceWorkerAndSendCommand;
    if (typeof attachToServiceWorkerAndSendCommand !== "function") {
        log.error("[inject script] attachToServiceWorkerAndSendCommand not available");
        done({ code: 1, error: { code: -1, message: "attachToServiceWorkerAndSendCommand unavailable" } });
        return;
    }

    const tabId = target?.tabId;
    if (tabId == null) {
        log.warn("[inject script] target.tabId missing");
        done({ code: 1, error: { code: -1, message: "missing tabId" } });
        return;
    }

    try {
        const targetFrameIds = normalizeFrameIds(target?.frameIds);
        const contentInitCodeId = params?.codeId || "";
        const timeoutSettings = await getInjectTimeoutSettings();

        if (!window["__CONTENT_INIT__"] || window[CONTENT_INIT_CODE_ID_KEY] !== contentInitCodeId) {
            const __CONTENT_INIT__ = wrapCodeAsGlobalFunction("__CONTENT_INIT__", wrapContentInitCode(params.code, contentInitCodeId));
            await attachToServiceWorkerAndSendCommand({
                expression: __CONTENT_INIT__,
                includeCommandLineAPI: true,
                returnByValue: true
            });
        }

        if (typeof window["__CONTENT_INIT__"] !== "function") {
            throw new Error("Failed to initialize __CONTENT_INIT__ via debugger");
        }
        window[CONTENT_INIT_CODE_ID_KEY] = contentInitCodeId;

        await injectInitToFrames(tabId, targetFrameIds, window["__CONTENT_INIT__"], timeoutSettings.targetFrameTimeoutMs, "target");
        done({ code: 0, message: "" });
        warmupAllFramesBestEffort(tabId, targetFrameIds, window["__CONTENT_INIT__"], timeoutSettings.warmupFrameTimeoutMs);
    } catch (e) {
        log.error("Init contentscript failed", e);
        done({ code: 1, error: { code: -1, message: errorMessage(e) } });
    }
}
