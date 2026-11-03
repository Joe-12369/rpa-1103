// src/background/background.ts'

import { log } from '@utils/logUtils';
import * as requestStore from '@utils/requestStore';
import {
    configureWindowMapping,
    getWindowMap,
    handleWindowMappingNativeDisconnect,
    handleWindowMappingNativeMessage,
    installWindowEventMessage
} from './windowMapping';

export let tryingToReconnect = false;

export let transferHubProxy: any = null;
export let rpa_conn_native: any = null;

export const VERSION: string = __VERSION__;

export const HOST_NAME: string = __NATIVE_HOST_NAME__;

export const CLIENT_FLAG: string = __CLIENT_FLAG__;

export const BUILD_PHASE: boolean = __BUILD_PHASE__;

export function markOnce(key: string, fn: () => void) {
    const k = `__MARK__${key}`;
    if (window[k]) return;
    window[k] = 1;
    try { fn(); } catch (e) {
        log.error(`markOnce ${key} error`, e);
    }
}

export function ReloadTabs() {
    chrome.tabs.query({}, (tabsList) => {
        log.debug("ReloadTabs", tabsList.length);
        for (const tab of tabsList) {
            if (tab.url && tab.url.indexOf("chrome://") !== 0) {
                chrome.tabs.reload(tab.id);
            }
        }
    });
}

const onNativeMessage = (message: any) => {
    let data: any = {};
    try {
        if (message.Action === "NativeMessage") {
            handleWindowMappingNativeMessage(message);
            return;
        }
        if (message.Action === "SendTransferMessage") {
            data = message;
            data.connType = "nativemsg_tMsg";
        } else {
            data.SendData = message;
            data.connType = "servicehub_msg";
        }
        received(data);
    } catch (e) {
        log.error("nativemsg host message exception: " + e);
        responseFail(-1000101, "nativemsg host message exception!", data);
    }
};

const onDisconnect = () => {

    const lastErrMsg = chrome.runtime?.lastError?.message || null;
    const lowerMsg = lastErrMsg ? lastErrMsg.toLowerCase() : '';

    const shouldRetry = !lastErrMsg || (
        !lowerMsg.includes('native messaging host not found') &&
        !lowerMsg.includes('failed to start')
    );

    const record = {
        time: Date.now(),
        reason: lastErrMsg || 'native host disconnected'
    };

    log.error("native host disconnected", record);

    // 解绑监听器与清理变量
    try {
        rpa_conn_native?.onDisconnect.removeListener(onDisconnect);
        rpa_conn_native?.onMessage.removeListener(onNativeMessage);
    } catch (e) {
        log.error("remove native host listeners exception:", e);
    }
    rpa_conn_native = null;

    // 异步保存历史记录
    const STORAGE_KEY = 'native_disconnect_history';
    chrome.storage.local.get([STORAGE_KEY], (res) => {
        try {
            let arr = (res && res[STORAGE_KEY]) ? res[STORAGE_KEY] : [];
            if (!Array.isArray(arr)) arr = [];

            arr.unshift(record);
            if (arr.length > 10) arr = arr.slice(0, 10);

            chrome.storage.local.set({ [STORAGE_KEY]: arr }, () => {
                if (chrome.runtime?.lastError) {
                    log.error("save disconnect record failed:", chrome.runtime.lastError.message);
                } else {
                    log.debug("saved disconnect history, total:", arr.length);
                }
            });
        } catch (e) {
            log.error("process disconnect record failed:", e);
        }
    });

    // 是否阻断重试
    if (!shouldRetry) {
        log.warn('native host reconnect skipped (host not installed/found):', record.reason);
        return;
    }

    // 触发重新连接
    nativeHostInit();
};

export function nativeHostInit() {
    try {
        log.info("native host init");
        if (rpa_conn_native) return;

        rpa_conn_native = chrome.runtime.connectNative(HOST_NAME);
        rpa_conn_native.onMessage.addListener(onNativeMessage);
        rpa_conn_native.onDisconnect.addListener(onDisconnect);
        log.info("native host connected");
    } catch (e) {
        log.error("native host connect exception: " + e);
    }
}

/* ---------------- message receive ---------------- */
export function received(data: any) {
    requestStore.saveRequest(structuredClone(data)).catch((err) => {
        log.error("saveRequest failed:", err);
    });

    const message = data.SendData;
    if (!message) return;

    try {
        log.info('[request]', message.method);
        switch (message.method) {
            case "getVersion":
                responseOK(VERSION, data);
                break;
            case "ping":
                responseOK('pong', data);
                break;
            case "init": {
                try {
                    if (__BUILD_PHASE__) {
                        const match = message.params.code.match(/\b[a-f0-9]{32}\b/g);
                        const CODE_ID = match ? match[0] : null;
                        window.iirpa.codeId = CODE_ID;
                        log.info('Code ID', CODE_ID);
                        responseOK("init fulfill", data);
                        break;
                    }
                    window.evalCode(message.params.code);
                    responseOK("init fulfill", data);
                } catch (error: any) {
                    responseFail(-1, error?.stack || String(error), data);
                }
                break;
            }
            case "getCodeId":
                responseOK(typeof window.iirpa === 'undefined' ? '' : window.iirpa.codeId, data);
                break;
            case "restore":
                window.iirpa = null;
                responseOK("restore", data);
                break;
            case "getWindowMap":
                responseOK(getWindowMap(), data);
                break;
            case "cookies":
                chrome.cookies.getAll({ url: message.url }, (_cookies) => { responseOK("cookies", _cookies); });
                break;
            case "navigation":
                const navListener = (_details: any) => { responseOK("navigation", _details); };
                chrome.webNavigation.onCompleted.addListener(navListener);
                break;
            case "downloads":
                chrome.downloads.search({ limit: 1 }, (_results) => { responseOK("downloads", _results); });
                break;
            default:
                const iirpaObj = window.iirpa;
                if (typeof iirpaObj === "undefined" || !iirpaObj?.codeId) {
                    responseFail(-100099, "script not injected!", data);
                } else if (message.codeId && message.codeId !== iirpaObj.codeId) {
                    responseFail(-1000100, "script required injected!", data);
                } else {
                    delete message.codeId;
                    iirpaObj.handle(message, (response: any) => {
                        data.CallbackReslut = response;
                        responseMessage(data);
                        if (message.method === "tabs.closeAll") {
                            tryingToReconnect = false;
                            try {
                                chrome.runtime.sendMessage({ _svc: 'signalr_stop' });
                            } catch (e) {
                                log.error("stop signalr exception: " + e);
                            }
                        }
                    });
                }
                break;
        }
    } catch (e) {
        log.error("received exception: " + e);
        responseFail(-1000110, "received exception", data);
    }
}

/* ---------------- response wrapper ---------------- */
export function responseOK(content: any, data: any) {
    data.CallbackReslut = {
        code: 200,
        status: 'OK',
        result: { content, error: null }
    };
    responseMessage(data);
}

export function responseFail(code: number, message: string, data: any) {
    data.CallbackReslut = {
        code: 200,
        status: 'OK',
        result: { content: null, error: { code, message } }
    };
    responseMessage(data);
}

export function responseMessage(data: any) {
    requestStore.writeResponse(data).catch((err) => {
        log.error("save response failed:", err);
    });
    log.info("[response]", data?.CallbackReslut.result.error?.message ?? 'OK');
    try {
        data.SendData = "";
        data.MessageType = 2;
        if (data.connType === "servicehub") {
            chrome.runtime.sendMessage({ _svc: 'signalr_send', payload: data });
        } else if (data.connType === "nativemsg_tMsg") {
            data.SendData = "";
            data.MessageType = 2;
            rpa_conn_native?.postMessage(data);
        } else {
            rpa_conn_native?.postMessage(data);
        }
    } catch (e) {
        log.error("[response exception]", e);
    }
}

/* ---------------- unified bootstrap ---------------- */
export function legacyBackgroundBootstrap() {
    if (__BUILD_PHASE__) {
        log.setLevel(0);
    }
    log.info("legacy background bootstrap start...");
    window.CHROME_VERSION = (navigator.userAgent.match(/Chrom(e|ium)\/(\d+)\./)?.[2] && parseInt(RegExp.$2, 10)) || null;
    window.ClientFlag = CLIENT_FLAG;
    configureWindowMapping({
        getNativeConnection: () => rpa_conn_native
    });
    nativeHostInit();
    installWindowEventMessage();
    requestStore.initInRequestService(log);
    log.info("legacy background bootstrap completed");
}
