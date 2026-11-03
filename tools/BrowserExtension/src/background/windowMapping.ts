import { log } from '@utils/logUtils';

type CallbackFunc = (err: Error | null, result?: any) => void;
type PendingCallback = {
    cb: CallbackFunc;
    timer: ReturnType<typeof setTimeout>;
};
type WindowMap = Record<number, number>;
type WindowFocusSnapshot = {
    focused?: boolean;
};
type WaitForWindowFocusedOptions = {
    attempts?: number;
    stableReads?: number;
    intervalMs?: number;
    sleep?: (ms: number) => Promise<void>;
};

enum MessageType {
    Send = 1,
    Callback = 2,
    NativeMessage = 3,
    NativeMessageCallback = 4
}

interface SendData<T = any> {
    method: string;
    params: T;
    callback?: CallbackFunc | null;
}

interface NativeMessageData {
    MessageId: string;
    Action: string;
    SendData: SendData;
    MessageType: MessageType;
    Timeout: number;
    Timestamp: number;
    connType: string;
    CallbackReslut?: any;
}

interface WindowMappingConfig {
    getNativeConnection: () => any;
}

type WindowInfo = {
    wndId: number;
    url: string;
    title: string;
    left: number;
    top: number;
    width: number;
    height: number;
    right: number;
    bottom: number;
};
type ChromeWindowWithId = chrome.windows.Window & { id: number };

const pendings = new Map<string, PendingCallback>();
const wnd2HwndDict: WindowMap = Object.create(null) as WindowMap;
const registeredSet = new Set<number>();
const retryingSet = new Set<number>();
const MAX_REGISTER_FAILURES = 3;
const registerFailureCount = new Map<number, number>();

let processing = false;
let getNativeConnection: () => any = () => null;

const defaultSleep = (ms: number) => new Promise<void>((resolve) => setTimeout(resolve, ms));

async function isWindowIdValid(wndId: number): Promise<boolean> {
    try {
        await chrome.windows.get(wndId);
        return true;
    } catch {
        return false;
    }
}

function getRegisterFailureCount(wndId: number): number {
    return registerFailureCount.get(wndId) ?? 0;
}

function markRegisterFailure(wndId: number): number {
    const count = getRegisterFailureCount(wndId) + 1;
    registerFailureCount.set(wndId, count);
    return count;
}

function clearRegisterFailure(wndId: number): void {
    registerFailureCount.delete(wndId);
}

function stopRegisterWindowRetry(wndId: number, reason: string, error?: unknown, failureCount = getRegisterFailureCount(wndId)): void {
    log.warn('registerWindow failed too many times, stop retry', { wndId, reason, error, failureCount });
    registeredSet.delete(wndId);
    retryingSet.delete(wndId);
    delete wnd2HwndDict[wndId];
}

function retryRegisterWindow(wndId: number, reason: string, error?: unknown): void {
    registeredSet.delete(wndId);
    const failureCount = markRegisterFailure(wndId);
    if (failureCount >= MAX_REGISTER_FAILURES) {
        stopRegisterWindowRetry(wndId, reason, error, failureCount);
        return;
    }
    log.warn(reason, { wndId, error, failureCount });
    retryingSet.add(wndId);
}

function scheduleActivateThenRetry(delayMs: number): void {
    if (retryingSet.size) setTimeout(activateThenRetry, delayMs);
}

function isNativeReady(): boolean {
    return window.nativeReady === true;
}

function postNativeMessage(message: any): void {
    getNativeConnection()?.postMessage?.(message);
}

function sendCallbackToNative(message: NativeMessageData, result: SendData): void {
    message.MessageType = MessageType.NativeMessageCallback;
    message.CallbackReslut = result;
    postNativeMessage(message);
}

function sendToNative(sendData: SendData): void {
    const data: NativeMessageData = {
        MessageId: crypto.randomUUID(),
        Action: "NativeMessage",
        SendData: sendData,
        MessageType: MessageType.NativeMessage,
        Timeout: 20000,
        Timestamp: Date.now(),
        connType: "nativemsg_tMsg"
    };
    const { callback } = sendData;
    if (callback) {
        const { method } = sendData;
        const id = data.MessageId;
        const timer = setTimeout(() => {
            pendings.delete(id);
            callback(new Error(`Native timeout: ${method}`));
        }, data.Timeout);
        pendings.set(id, { cb: callback, timer });
    }
    postNativeMessage(data);
}

function isPageWnd(wnd: chrome.windows.Window): boolean {
    return wnd.type === "normal" || wnd.type === "popup";
}

function hasWindowId(wnd: chrome.windows.Window): wnd is ChromeWindowWithId {
    return typeof wnd.id === 'number';
}

function getWndInfo(wnd: ChromeWindowWithId): WindowInfo {
    const { id: wndId, left = 0, top = 0, width = 0, height = 0, tabs } = wnd;
    const activeTab = tabs?.find(tab => tab.active);
    let url = activeTab?.url ?? "";
    let title = activeTab?.title ?? "";
    if (url.endsWith("/")) url = url.slice(0, -1);
    return { wndId, url, title, left, top, width, height, right: left + width, bottom: top + height };
}

async function waitForWindowFocused(
    wndId: number,
    getWindow: (wndId: number) => Promise<WindowFocusSnapshot>,
    options: WaitForWindowFocusedOptions = {}
): Promise<boolean> {
    const attempts = Math.max(1, options.attempts ?? 6);
    const stableReads = Math.max(1, options.stableReads ?? 2);
    const intervalMs = Math.max(0, options.intervalMs ?? 50);
    const sleep = options.sleep ?? defaultSleep;

    let consecutiveFocusedReads = 0;

    for (let attempt = 0; attempt < attempts; attempt++) {
        try {
            const currentWindow = await getWindow(wndId);
            if (currentWindow?.focused === true) {
                consecutiveFocusedReads += 1;
                if (consecutiveFocusedReads >= stableReads) {
                    return true;
                }
            } else {
                consecutiveFocusedReads = 0;
            }
        } catch {
            consecutiveFocusedReads = 0;
        }

        if (attempt + 1 < attempts) {
            await sleep(intervalMs);
        }
    }

    return false;
}

async function activateThenRetry(): Promise<void> {
    let wndId: number | undefined;
    try {
        if (!isNativeReady()) {
            log.warn('native not ready');
            setTimeout(activateThenRetry, 500);
            return;
        }
        if (processing === true) return;
        processing = true;
        wndId = retryingSet.values().next().value;
        if (wndId === undefined) return;
        if (getRegisterFailureCount(wndId) >= MAX_REGISTER_FAILURES) {
            stopRegisterWindowRetry(wndId, 'retry skipped because failure limit reached');
            scheduleActivateThenRetry(10);
            return;
        }
        retryingSet.delete(wndId);
        await registerWindow(wndId);
    } catch (e) {
        log.error('activate fail', e);
        if (wndId !== undefined) {
            retryingSet.delete(wndId);
        }
    } finally {
        processing = false;
    }
}

function setWindowMap(map?: WindowMap): void {
    map = map ?? wnd2HwndDict;
    if (!map || typeof map !== 'object' || Array.isArray(map)) {
        log.warn('setWindowMap: map must be a plain object');
        return;
    }
    if (Object.keys(map).length === 0) {
        log.debug('setWindowMap: map is empty, skip');
        return;
    }
    const sendData: SendData = {
        method: 'setWindowMap',
        params: map,
        callback: (err, res) => {
            if (err) log.warn('setWindowMap err', err);
            else log.debug('setWindowMap succ', res);
        }
    };
    sendToNative(sendData);
}

async function registerWindow(wndId: number): Promise<void> {
    if (registeredSet.has(wndId)) return;
    if (!isNativeReady()) {
        log.warn('native not ready,add to retryset', wndId);
        retryingSet.add(wndId);
        setTimeout(activateThenRetry, 500);
        return;
    }
    registeredSet.add(wndId);
    chrome.windows.update(wndId, {
        focused: true,
        drawAttention: true
    }).then(async updatedwnd => {
        if (!hasWindowId(updatedwnd)) {
            retryRegisterWindow(wndId, 'window id missing after update', updatedwnd);
            scheduleActivateThenRetry(100);
            return;
        }
        const focusedReady = await waitForWindowFocused(
            updatedwnd.id,
            async (id) => chrome.windows.get(id),
            { attempts: 6, stableReads: 2, intervalMs: 50 }
        );
        if (!focusedReady) {
            log.warn('window focus not stable before register', updatedwnd.id);
            registeredSet.delete(updatedwnd.id);
            retryingSet.add(updatedwnd.id);
            scheduleActivateThenRetry(100);
            return;
        }
        const sendData: SendData = {
            method: 'registerWindow',
            params: getWndInfo(updatedwnd),
            callback: async (err, res) => {
                if (err) {
                    retryRegisterWindow(updatedwnd.id, '注册失败', err);
                } else {
                    log.debug('注册成功', res);
                    const { wndId, wndHandle } = res;
                    const curWnd = await chrome.windows.get(wndId);
                    if (curWnd.focused === true) {
                        wnd2HwndDict[wndId] = wndHandle;
                        clearRegisterFailure(wndId);
                        setWindowMap({ [wndId]: wndHandle });
                    } else {
                        retryRegisterWindow(wndId, '状态不匹配', res);
                    }
                }
                scheduleActivateThenRetry(10);
            }
        };
        sendToNative(sendData);
    }).catch(async error => {
        registeredSet.delete(wndId);
        if (await isWindowIdValid(wndId)) {
            retryRegisterWindow(wndId, '窗口激活失败，继续注册:', error);
        } else {
            log.warn('window id invalid during registerWindow update, skip retry', { wndId, error });
            retryingSet.delete(wndId);
            clearRegisterFailure(wndId);
            delete wnd2HwndDict[wndId];
        }
        scheduleActivateThenRetry(100);
    });
}

function removeWindow(wndId: number): void {
    const sendData: SendData = {
        method: 'removeWindow',
        params: { wndId },
        callback: (error, res) => {
            if (error) log.warn('移除失败', error);
            else log.debug('移除成功', res);
        }
    };
    sendToNative(sendData);
}

function onWndCreated(wnd: chrome.windows.Window): void {
    log.info('WindowCreated', wnd.id);
    if (!isPageWnd(wnd)) return;
    if (hasWindowId(wnd)) {
        registerWindow(wnd.id);
    }
}

function onWndRemoved(wndId: number): void {
    const shouldNotifyNative = registeredSet.has(wndId) || wnd2HwndDict[wndId] !== undefined;
    registeredSet.delete(wndId);
    retryingSet.delete(wndId);
    clearRegisterFailure(wndId);
    delete wnd2HwndDict[wndId];
    if (!shouldNotifyNative) return;
    log.info('WindowRemoved', wndId);
    removeWindow(wndId);
}

export function configureWindowMapping(config: WindowMappingConfig) {
    getNativeConnection = config.getNativeConnection;
    window.nativeReady = false;
}

export function getWindowMap(): WindowMap {
    return wnd2HwndDict;
}

export function handleWindowMappingNativeDisconnect(): void {
    window.nativeReady = false;

    for (const [id, pending] of Array.from(pendings.entries())) {
        try {
            pendings.delete(id);
            clearTimeout(pending.timer);
            pending.cb(new Error('Native host disconnected'));
        } catch (e) {
            log.error('handleWindowMappingNativeDisconnect failed', e);
        }
    }
}

export function handleWindowMappingNativeMessage(message: NativeMessageData): void {
    if (message?.MessageId && message?.MessageType === MessageType.NativeMessageCallback) {
        const { MessageId: id, CallbackReslut: callbackData } = message;
        if (pendings.has(id)) {
            const { cb, timer } = pendings.get(id)!;
            pendings.delete(id);
            clearTimeout(timer);
            if (callbackData?.error) {
                cb(new Error(callbackData.error));
            } else {
                cb(null, callbackData);
            }
        }
        return;
    }

    try {
        switch (message?.SendData?.method) {
            case "NMHReady":
                window.nativeReady = true;
                setWindowMap();
                break;
            case "KeepLive":
                sendCallbackToNative(message, { method: 'KeepLive', params: null });
                break;
        }
    } catch (e) {
        log.info("received exception: " + e);
    }
}

export async function installWindowEventMessage(): Promise<void> {
    const wnds = await chrome.windows.getAll({ populate: true });
    const pageWnds = wnds.filter(isPageWnd);
    pageWnds.forEach((wnd) => {
        if (hasWindowId(wnd)) {
            retryingSet.add(wnd.id);
        }
    });
    activateThenRetry();
    log.info('installWindowEventMessage');
    chrome.windows.onCreated.addListener(onWndCreated);
    chrome.windows.onRemoved.addListener(onWndRemoved);
}
