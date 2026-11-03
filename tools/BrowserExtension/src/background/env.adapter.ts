// src/background/env.adapter.ts
// @ts-nocheck

import { log } from '@utils/logUtils';
import {
    clearContentInitCache,
    handleInitInjectWithDebugger,
    wrapCodeAsGlobalFunction,
} from './scriptInjection';


// SW 环境 window shim
if (chrome.runtime.getManifest().manifest_version == 3 && typeof (globalThis as any).window === 'undefined') {
    (globalThis as any).window = { __stub: true, IS_SW: true };
}

window.evalCode = async function (code) {
    try {
        log.debug('Init background');
        if (window['__BACKGROUND_INIT__']) {
            log.debug('Background already initialized.');
        }
        delete window['__BACKGROUND_INIT__'];
        clearContentInitCache();
        window['__BACKGROUND_INIT__'] = null;
        const __BACKGROUND_INIT__ = wrapCodeAsGlobalFunction("__BACKGROUND_INIT__", code);
        const result = await attachToServiceWorkerAndSendCommand({
            expression: __BACKGROUND_INIT__,
            includeCommandLineAPI: true,
            returnByValue: true
        });

        var _init_result_ = window.__BACKGROUND_INIT__();
        log.debug('_init_result_=', _init_result_);
        log.info(`Init background done,code:${window?.iirpa?.codeId}`);
    } catch (e) {
        log.error('Init background failed', e);
    }

}

let _attachPromise: Promise<void> = Promise.resolve();
export async function attachToServiceWorkerAndSendCommand(params: any = {}, retryCount = 0) {
    const promise = _attachPromise.then(() => _attachToSWInternal(params, retryCount));
    _attachPromise = promise.catch(() => { });
    return promise;
}
(globalThis as any).attachToServiceWorkerAndSendCommand = attachToServiceWorkerAndSendCommand;

async function _attachToSWInternal(params: any = {}, retryCount = 0): Promise<any> {
    let swTarget = null;
    let attachedByUs = false;
    try {
        let command = "Runtime.evaluate";
        const targets = await chrome.debugger.getTargets();
        if (!Array.isArray(targets)) {
            log.warn('[attachToServiceWorkerAndSendCommand] No targets found');
            return null;
        }

        swTarget = targets.find(target => {
            if (target.type !== 'worker' && target.type !== 'service_worker') return false;

            const currentExtUrl = chrome.runtime.getURL('');
            if (target.url?.startsWith(currentExtUrl)) return true;

            return false;
        });

        if (!swTarget) {
            log.warn('[attachToServiceWorkerAndSendCommand] No serviceworker.js target found');
            return null;
        }

        log.debug('[attachToServiceWorkerAndSendCommand] Found target:', swTarget);

        await new Promise((resolve, reject) => {
            chrome.debugger.attach({ targetId: swTarget.id }, '1.3', () => {
                if (chrome.runtime.lastError) {
                    log.error('[attachToServiceWorkerAndSendCommand] Attach failed:', chrome.runtime.lastError.message);
                    reject(chrome.runtime.lastError.message);
                } else {
                    attachedByUs = true;
                    log.info('[attachToServiceWorkerAndSendCommand] Successfully attached to serviceworker.js')
                    resolve(null);
                }
            });
        });

        // 调用命令
        const result = await new Promise((resolve, reject) => {
            chrome.debugger.sendCommand({ targetId: swTarget.id }, command, params, (response) => {
                if (chrome.runtime.lastError) {
                    log.error('[attachToServiceWorkerAndSendCommand] Command failed:', chrome.runtime.lastError.message);
                    reject(chrome.runtime.lastError.message);
                } else {
                    resolve(response);
                }
            });
        });

        log.debug('[attachToServiceWorkerAndSendCommand] Command result:', result);
        return result;
    } catch (error: any) {
        log.error('[attachToServiceWorkerAndSendCommand]', error);

        let msg = String(error?.message || error || "");
        if ((msg.includes("Another debugger is already attached") || msg.includes("Detached from target")) && retryCount < 10) {
            log.warn(`[attachToServiceWorkerAndSendCommand] Check conflict, retrying (${retryCount + 1}/10)...`);
            await new Promise(r => setTimeout(r, 1000));
            // If we caught this error, we shouldn't throw. Just recurse.
            // The outer Promise wrapper `attachToServiceWorkerAndSendCommand` is awaiting us.
            return await _attachToSWInternal(params, retryCount + 1);
        }

        return null;
    } finally {
        // 分离调试器
        if (swTarget && attachedByUs) {
            await new Promise((resolve) => {
                chrome.debugger.detach({ targetId: swTarget.id }, () => {
                    const err = chrome.runtime.lastError;
                    if (err) {
                        log.error('[attachToServiceWorkerAndSendCommand] Detach failed:', err.message);
                    } else {
                        log.warn('[attachToServiceWorkerAndSendCommand] Successfully detached from serviceworker.js');
                    }
                    resolve(null);
                });
            });
        }
    }
}

export const PortStorage = {
    async get(key: string) {
        try {
            if (!IS_SW && typeof localStorage !== "undefined") {
                const s = localStorage.getItem(key);
                if (s == null) return undefined;
                try { return JSON.parse(s); } catch { return s; }
            }
            const o = await chrome.storage.local.get(key);
            return o ? o[key] : undefined;
        } catch {
            return undefined;
        }
    },
    async set(key: string, value: any) {
        try {
            if (!IS_SW && typeof localStorage !== "undefined") {
                if (value == null) {
                    localStorage.removeItem(key);
                } else {
                    localStorage.setItem(key, JSON.stringify(value));
                }
                return;
            }
            await chrome.storage.local.set({ [key]: value });
        } catch { /* noop */ }
    }
};

const InjectNamedFunction = async function (tabId, frameId, codeString, debuggerManager, executeWorld) {

    const functionName = `__InjectedFunc_${Date.now()}_${Math.floor(Math.random() * 10000)}`;
    const wrappedFunction = `
        window["${functionName}"] = function() {
            return ${codeString};
        };
    `;
    const commandParam = {
        expression: wrappedFunction,
        includeCommandLineAPI: true,
        userGesture: true,
        returnByValue: true,
    };

    if (executeWorld == "ISOLATED") {
        const extensionOrigin = chrome.runtime.getURL('');
        const contexts = await debuggerManager.getExecutionContexts(tabId, frameId, extensionOrigin);
        if (contexts.length > 0) {
            const ctx = contexts[0];
            log.debug("extension context id:", ctx.id);
            commandParam.contextId = ctx.id;
        } else {
            log.error("can not find extension context:", `${frameId}`);
        }
    } else {
        const maincontexts = await debuggerManager.getMainContexts(tabId, frameId);
        if (maincontexts.length > 0) {
            const ctx = maincontexts[0];
            log.debug("main context id:", ctx.id);
            commandParam.contextId = ctx.id;
        } else {
            log.error("can not find main context:", `${frameId}`);
        }
    }
    try {
        const evalResult = await debuggerManager.sendCommand(tabId, 'Runtime.evaluate', commandParam);
        log.debug("Function injected successfully:", evalResult);
        return { success: true, functionName };
    } catch (err) {
        log.error("Injection error:", err);
        return { success: false, error: err.message };
    }
};
/**
 * SW 环境 polyfill：
 */
if (chrome.scripting && !chrome.tabs.executeScript) {
    chrome.tabs.executeScript = function (tabId, details, callback) {
        // 参数规范化处理
        let finalTabId = tabId;
        let finalDetails = details;
        let finalCallback = callback;

        if (typeof finalTabId === 'object') {
            // executeScript(details, callback)
            finalCallback = finalDetails as any;
            finalDetails = finalTabId;
            finalTabId = undefined;
        } else if (typeof finalDetails === 'function') {
            // executeScript(tabId, callback)
            finalCallback = finalDetails as any;
            finalDetails = finalTabId;
            finalTabId = undefined;
        }

        // 验证details
        if (!finalDetails || typeof finalDetails !== 'object') {
            const error = new Error('details must be an object');
            if (typeof finalCallback === 'function') {
                finalCallback(undefined);
            }
            return Promise.reject(error);
        }

        log.debug("tabs.executeScript:", { tabId: finalTabId, details: finalDetails });

        const target = {
            tabId: finalTabId,
            frameIds: finalDetails.frameId != null ? [finalDetails.frameId] : [0]
        };

        const world = 'ISOLATED'; // MAIN 或 ISOLATED        

        const done = (arr: any) => {
            if (!finalCallback) return;
            if (Array.isArray(arr)) {
                finalCallback(arr);
            } else {
                finalCallback([arr]);
            }
        };


        const executePromise = async (): Promise<any[] | undefined> => {
            return new Promise((resolve) => {
                const promiseDone = (arr: any) => {
                    if (Array.isArray(arr)) {
                        resolve(arr);
                    } else {
                        resolve([arr]);
                    }
                };

                executeInternal(promiseDone);
            });
        };

        const executeInternal = (doneCallback: (result: any) => void) => {
            if (finalDetails?.file || (Array.isArray(finalDetails?.files) && finalDetails.files.length)) {
                chrome.scripting.executeScript({
                    target,
                    files: finalDetails.files || [finalDetails.file],
                    world
                }).then(res => doneCallback(res.map(x => x.result))).catch(() => doneCallback(null));
                return;
            }

            const code = String(finalDetails?.code ?? '').trim();

            // 处理 handle() 调用
            const mHandle = code.match(/^handle\(\s*(['"])(.*?)\1\s*,\s*(\{[\s\S]*\})\s*\)\s*$/);
            if (mHandle) {
                const method = mHandle[2];
                let params: any = {};
                try {
                    params = JSON.parse(mHandle[3]);
                } catch (e) {
                    log.warn('[SW][mHandle] JSON.parse params failed, fallback empty', e);
                    params = {};
                }

                log.debug('[SW][mHandle] method=', method, params);

                if (method === 'init' && typeof params.code === 'string') {
                    handleInitInjectWithDebugger(target, params, doneCallback, { attachToServiceWorkerAndSendCommand });
                } else {
                    if (!window['__CONTENT_INIT__']) {
                        log.info('[SW]switch runtime version, return need init');
                        doneCallback([{ code: -2, message: 'need init' }]);
                        return;
                    }
                    (async () => {
                        try {
                            const resArr = await chrome.scripting.executeScript({
                                target,
                                world: world,
                                func: async (m, p) => {
                                    try {
                                        if (typeof (window as any).iirpa === 'undefined' || !(window as any).iirpa || typeof (window as any).iirpa.handle !== 'function') {
                                            return { ok: false, kind: 'no_iirpa' };
                                        }
                                        try {
                                            const val = (window as any).iirpa.handle(m, p);
                                            const awaited = (val && typeof val.then === 'function') ? await val : val;
                                            return { ok: true, result: awaited };
                                        } catch (innerErr) {
                                            return { ok: false, kind: 'thrown', error: String(innerErr && (innerErr.stack || innerErr.message || innerErr)) };
                                        }
                                    } catch (e) {
                                        return { ok: false, kind: 'eval_error', error: String(e && (e.stack || e.message || e)) };
                                    }
                                },
                                args: [method, params]
                            });
                            const item = Array.isArray(resArr) && resArr.length
                                ? (('result' in resArr[0]) ? resArr[0].result : resArr[0])
                                : resArr;

                            if (item.ok) {
                                log.debug('[SW][mHandle] return=', item.result);
                                doneCallback([item.result]);
                                return;
                            }

                            if (item.kind === 'no_iirpa') {
                                log.warn('[SW][mHandle] iirpa not exist; return need init');
                                doneCallback([{ code: -2, message: 'need init' }]);
                                return;
                            }

                            if (item.kind === 'thrown' || item.kind === 'eval_error') {
                                log.error('[SW][mHandle] iirpa.handle threw exception', item.error);
                                doneCallback([{ code: 1, error: { code: -1, message: String(item.error) } }]);
                                return;
                            }

                            log.warn('[SW][mHandle] unknown response', item);
                            doneCallback([{ code: 1, error: { code: -1, message: 'unknown response' } }]);
                            return;
                        } catch (e) {
                            log.error('[SW][mHandle] return error', e);
                            doneCallback(null);
                        }
                    })();
                }
                return;
            }

            // 处理特殊代码片段
            if (code === 'document.documentElement.outerHTML' || code === 'document.documentElement.outerText') {
                const prop = code.endsWith('outerHTML') ? 'outerHTML' : 'outerText';
                chrome.scripting.executeScript({
                    target,
                    world,
                    func: (propName) => { try { return (document.documentElement as any)[propName]; } catch { return null; } },
                    args: [prop]
                }).then(res => doneCallback(res.map(x => x.result))).catch(() => doneCallback(null));
                return;
            }

            if (code === 'window.stop()') {
                chrome.scripting.executeScript({
                    target,
                    world,
                    func: () => { try { (window as any).stop?.(); } catch { } return null; }
                }).then(res => doneCallback(res.map(x => x.result))).catch(() => doneCallback(null));
                return;
            }

            const mExec = code.match(/^document\.execCommand\(\s*(['"])(.*?)\1\s*\)\s*$/);
            if (mExec) {
                const cmd = mExec[2];
                chrome.scripting.executeScript({
                    target,
                    world,
                    func: (c) => { try { return document.execCommand(c); } catch { return false; } },
                    args: [cmd]
                }).then(res => doneCallback(res.map(x => x.result))).catch(() => doneCallback(null));
                return;
            }

            if (code === 'window.devicePixelRatio') {
                chrome.scripting.executeScript({
                    target,
                    world,
                    func: () => { try { return (window as any).devicePixelRatio; } catch { return 1; } }
                }).then(res => doneCallback(res.map(x => x.result))).catch(() => doneCallback(null));
                return;
            }

            // 使用debuggerManager执行代码
            const dbg = (window as any).debuggerManager;
            if (dbg?.sendCommand) {
                (async () => {
                    try {
                        let ret: any;
                        let commandParam: any = {
                            expression: code,
                            includeCommandLineAPI: true,
                            returnByValue: true
                        };
                        if (finalDetails.frameId && finalDetails.frameId > 0) {
                            log.debug('executeScript via debuggerManager:', { tabId: finalTabId, frameId: finalDetails.frameId });
                            const contexts = await dbg.getExecutionContexts(finalTabId, finalDetails.frameId, 'chrome-extension://pcopnibgkbdnlaeagepigbboebdfejmb');
                            if (contexts.length > 0) {
                                const ctx = contexts[0];
                                log.debug("extension context id:", ctx.id);
                                commandParam.contextId = ctx.id;
                            } else {
                                log.error("can not find extension context:", finalDetails.frameId);
                            }
                            if (!commandParam.contextId) log.error('Frame context not ready');
                            ret = await dbg.sendCommand(finalTabId, 'Runtime.evaluate', commandParam);
                        } else {
                            ret = await dbg.sendCommand(finalTabId, 'Runtime.evaluate', commandParam);
                        }

                        if (ret?.result !== undefined) {
                            const val = (ret.result && 'value' in ret.result) ? ret.result.value : ret.result;
                            doneCallback([val]);
                        } else if (ret?.exceptionDetails) {
                            doneCallback(String(ret.exceptionDetails.text || 'exception'));
                        } else {
                            doneCallback(null);
                        }
                    } catch (e) {
                        log.error('executeScript via debuggerManager failed', e);
                        doneCallback(null);
                    }
                })();
                return;
            }

            // 不支持的代码类型
            try {
                const preview = code.length > 200 ? (code.slice(0, 200) + '...') : code;
                log.warn('[SW][executeScript][UNSUPPORTED_CODE]', {
                    tabId: finalTabId,
                    frameId: finalDetails.frameId,
                    length: code.length,
                    preview
                });
            } catch { }

            doneCallback(null);
        };

        // 处理回调
        if (typeof finalCallback === 'function') {
            executeInternal(done);
            return undefined;
        }

        // Promise模式
        return executePromise().then(results => {
            return results && results.length > 0 ? results : undefined;
        });
    };
}


if (chrome.scripting && !chrome.tabs.insertCSS) {
    chrome.tabs.insertCSS = (tabId, details, cb) => {
        chrome.scripting.insertCSS({
            target: {
                tabId,
                frameIds: (details as any)?.frameId != null ? [(details as any).frameId] : undefined
            },
            css: (details as any).code,
            files: (details as any).file ? [(details as any).file] : undefined
        }).then(() => cb && cb()).catch(() => cb && cb());
    };
}
