// requestStore111
type RawJSON = any;

export interface SaveEntity {
    id?: number;
    date: string;
    requestTimestamp: number;
    responseTimestamp?: number | null;
    messageId?: string | number | null;
    method?: string;
    requestRaw: RawJSON;
    responseRaw?: RawJSON | null;
    error?: string | null;
    exception?: any;
}

const DB_NAME = 'rpa_request_store';
const STORE_NAME = 'requests';
const DB_VERSION = 2;

export const STORAGE_KEYS = {
    LOG_RETENTION_DAYS: "logRetentionDays",
    RECORDING_ENABLED: "recordingEnabled",
    LOG_LEVEL: "logLevel",
    SERVICE_HUB_ENABLED: "serviceHubEnabled",
    THEME: "ext_theme",
    SIDEBAR_COLLAPSED: "sidebarCollapsed",
} as const;

const DEFAULT_RETENTION_DAYS = 2;
const STORE_RETENTION_DAYS = null;
const DIAGNOSTIC_LOG_LEVEL = 1 | 2 | 4 | 8;
const DEFAULT_LOG_LEVEL = 2 | 8;

let cachedDb: IDBDatabase | null = null;
let recordingEnabledCache: boolean | null = null;

export function getrecordingEnabledCache(): boolean {
    if (recordingEnabledCache === null) {
        InitRecordingEnabled();
    }
    return recordingEnabledCache ?? false;
}

function formatDateYYYYMMDD(d: Date): string {
    const y = d.getFullYear();
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${day}`;
}

function openDb(): Promise<IDBDatabase> {
    if (cachedDb) return Promise.resolve(cachedDb);
    return new Promise((resolve, reject) => {
        const req = indexedDB.open(DB_NAME, DB_VERSION);
        req.onupgradeneeded = (ev) => {
            const db = (ev.target as IDBOpenDBRequest).result;
            let store: IDBObjectStore;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
                store = db.createObjectStore(STORE_NAME, { keyPath: 'id', autoIncrement: true });
                store.createIndex('date', 'date', { unique: false });
                store.createIndex('requestTimestamp', 'requestTimestamp', { unique: false });
                store.createIndex('messageId', 'messageId', { unique: false });
                store.createIndex('method', 'method', { unique: false }); // [新增] 创建 method 索引
            } else {
                const tx = (ev.target as IDBOpenDBRequest).transaction!;
                store = tx.objectStore(STORE_NAME);
                try {
                    if (!store.indexNames.contains('messageId'))
                        store.createIndex('messageId', 'messageId', { unique: false });
                } catch { }
                try {
                    if (!store.indexNames.contains('requestTimestamp'))
                        store.createIndex('requestTimestamp', 'requestTimestamp', { unique: false });
                } catch { }
                try {
                    if (!store.indexNames.contains('method'))
                        store.createIndex('method', 'method', { unique: false }); // [新增] 升级时添加索引
                } catch { }
            }
        };
        req.onsuccess = () => {
            cachedDb = req.result;
            cachedDb.onversionchange = () => cachedDb?.close();
            resolve(cachedDb);
        };
        req.onerror = () => reject(req.error);
    });
}

// ====== 配置读写函数 ======

export async function InitRecordingEnabled() {
    try {
        const result = await chrome.storage.local.get(STORAGE_KEYS.RECORDING_ENABLED);
        recordingEnabledCache = result[STORAGE_KEYS.RECORDING_ENABLED] === true;
    } catch {
        console.warn("InitRecordingEnabled failed, default to false");
        recordingEnabledCache = false;
    }
}

export async function setRecordingEnabled(enabled: boolean): Promise<void> {
    try {
        console.log("setRecordingEnabled", enabled);
        await chrome.storage.local.set({ [STORAGE_KEYS.RECORDING_ENABLED]: enabled });
        recordingEnabledCache = enabled;
    } catch { }
}

export async function isDiagnosticLogEnabled(): Promise<boolean> {
    try {
        const result = await chrome.storage.local.get(STORAGE_KEYS.LOG_LEVEL);
        return result[STORAGE_KEYS.LOG_LEVEL] === true;
    } catch {
        return false;
    }
}

async function getRetentionDays(): Promise<number> {
    try {
        if (STORE_RETENTION_DAYS !== null) {
            return STORE_RETENTION_DAYS;
        }
        const result = await chrome.storage.local.get(STORAGE_KEYS.LOG_RETENTION_DAYS);
        const n = result[STORAGE_KEYS.LOG_RETENTION_DAYS];
        if (typeof n === 'number' && n >= 1) return n;
        return DEFAULT_RETENTION_DAYS;
    } catch {
        return DEFAULT_RETENTION_DAYS;
    }
}

// ====== 数据库操作 ======

async function deleteExpiredEntries(db: IDBDatabase, retentionDays: number): Promise<void> {
    const today = new Date();
    const earliest = new Date(today.getFullYear(), today.getMonth(), today.getDate() - (retentionDays - 1));
    const earliestStr = formatDateYYYYMMDD(earliest);

    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, 'readwrite');
        const store = tx.objectStore(STORE_NAME);
        const index = store.index('date');
        const range = IDBKeyRange.upperBound(earliestStr, true);
        const cursorReq = index.openCursor(range);

        cursorReq.onsuccess = () => {
            const cursor = cursorReq.result;
            if (cursor) {
                cursor.delete();
                cursor.continue();
            }
        };
        cursorReq.onerror = () => reject(cursorReq.error);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

/**
 * 初始化存储
 */
export async function initRequestStore(): Promise<void> {
    const db = await openDb();
    const retention = await getRetentionDays();
    console.debug(`[requestStore] initialized. retentionDays=${retention}`);
    await deleteExpiredEntries(db, retention);
}

// ====== 请求/响应 ======

function extractMessageId(req: RawJSON): string | number | null {
    if (!req || typeof req !== 'object') return null;
    const keys = ['MessageId'];
    for (const k of keys) {
        if (k in req) return req[k];
    }
    return null;
}

export async function saveRequest(requestJson: RawJSON): Promise<SaveEntity | undefined> {
    if (!recordingEnabledCache) return undefined;

    const db = await openDb();
    const now = new Date();
    const messageId = extractMessageId(requestJson);
    if (requestJson?.SendData?.method.startsWith('init')) {
        delete requestJson.SendData?.params?.code;
    }

    // [新增] 提取 method
    const methodStr = requestJson?.SendData?.method;

    const entity: SaveEntity = {
        date: formatDateYYYYMMDD(now),
        requestTimestamp: now.getTime(),
        responseTimestamp: null,
        messageId: messageId ?? null,
        method: typeof methodStr === 'string' ? methodStr : undefined, // [新增] 保存 method
        requestRaw: requestJson,
        responseRaw: null,
    };

    return new Promise<SaveEntity>((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, 'readwrite');
        const store = tx.objectStore(STORE_NAME);
        const addReq = store.add(entity as any);
        addReq.onsuccess = () => {
            entity.id = addReq.result as number;
        };
        addReq.onerror = () => reject(addReq.error);
        tx.oncomplete = () => resolve(entity);
        tx.onerror = () => reject(tx.error);
    });
}
export async function writeResponse(responseJson: RawJSON, extraData: any = null): Promise<SaveEntity | undefined> {
    if (!recordingEnabledCache) return undefined;

    const messageId = extractMessageId(responseJson);
    if (messageId == null) {
        throw new Error('messageId is required');
    }

    const db = await openDb();
    return new Promise<SaveEntity>((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, 'readwrite');
        const store = tx.objectStore(STORE_NAME);
        const index = store.index('messageId');
        const range = IDBKeyRange.only(messageId as any);
        const getAllReq = index.getAll(range);

        getAllReq.onsuccess = () => {
            const list = getAllReq.result as SaveEntity[] | undefined;
            if (!list || list.length === 0) {
                return reject(new Error('no matching request found for messageId'));
            }

            const foundEntity = list.reduce((a, b) =>
                (a.requestTimestamp >= b.requestTimestamp ? a : b)
            );

            foundEntity.responseRaw = responseJson?.CallbackReslut;
            foundEntity.error = chrome && chrome.runtime && chrome.runtime.lastError && chrome.runtime.lastError.message ? chrome.runtime.lastError.message : null;
            foundEntity.exception = extraData?.exception || null;
            foundEntity.responseTimestamp = Date.now();
            const putReq = store.put(foundEntity);
            putReq.onsuccess = () => resolve(foundEntity);
            putReq.onerror = () => reject(putReq.error);
        };
        getAllReq.onerror = () => reject(getAllReq.error);
        tx.onerror = () => reject(tx.error);
    });
}

export async function writeResponseById(id: number, responseJson: RawJSON): Promise<SaveEntity> {
    const db = await openDb();
    return new Promise<SaveEntity>((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, 'readwrite');
        const store = tx.objectStore(STORE_NAME);
        const getReq = store.get(id);
        getReq.onsuccess = () => {
            const entity = getReq.result as SaveEntity | undefined;
            if (!entity) return reject(new Error('entity not found'));
            entity.responseRaw = responseJson;
            entity.responseTimestamp = Date.now();
            const putReq = store.put(entity);
            putReq.onsuccess = () => resolve(entity);
            putReq.onerror = () => reject(putReq.error);
        };
        getReq.onerror = () => reject(getReq.error);
    });
}

// ====== 工具函数 ======

export async function clearAllRequests(): Promise<void> {
    const db = await openDb();
    return new Promise<void>((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, 'readwrite');
        const store = tx.objectStore(STORE_NAME);
        const req = store.clear();
        req.onsuccess = () => resolve();
        req.onerror = () => reject(req.error);
        tx.onerror = () => reject(tx.error);
    });
}

export async function getTotalStoreSizeBytes(): Promise<number> {
    const db = await openDb();
    return new Promise<number>((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, 'readonly');
        const store = tx.objectStore(STORE_NAME);
        const getAllReq = store.getAll();

        getAllReq.onsuccess = () => {
            const list = getAllReq.result as any[] || [];
            try {
                const encoder = new TextEncoder();
                let total = 0;
                for (const item of list) {
                    const s = JSON.stringify(item);
                    total += encoder.encode(s).length;
                }
                resolve(total);
            } catch (err) {
                reject(err);
            }
        };
        getAllReq.onerror = () => reject(getAllReq.error);
        tx.onerror = () => reject(tx.error);
    });
}

export async function getRequestsByDate(
    dateOrStr: string | Date,
    pageSize: number = 20,
    currentPage: number = 1,
    methodFilter?: string
): Promise<{ total: number; list: SaveEntity[] }> {
    const db = await openDb();
    const dateStr = typeof dateOrStr === 'string' ? dateOrStr : formatDateYYYYMMDD(dateOrStr);
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, 'readonly');
        const store = tx.objectStore(STORE_NAME);
        const index = store.index('date');
        const range = IDBKeyRange.only(dateStr);

        // 如果没有过滤条件，走原来的快速路径
        if (!methodFilter) {
            const countReq = index.count(range);

            countReq.onsuccess = () => {
                const total = countReq.result;
                if (total === 0) {
                    resolve({ total: 0, list: [] });
                    return;
                }

                const list: SaveEntity[] = [];
                const startIndex = (currentPage - 1) * pageSize;

                if (startIndex >= total) {
                    resolve({ total, list: [] });
                    return;
                }

                const cursorReq = index.openCursor(range, 'prev');
                let hasAdvanced = false;

                cursorReq.onsuccess = (e) => {
                    const cursor = (e.target as IDBRequest).result as IDBCursorWithValue;
                    if (!cursor) {
                        resolve({ total, list });
                        return;
                    }

                    if (startIndex > 0 && !hasAdvanced) {
                        hasAdvanced = true;
                        cursor.advance(startIndex);
                        return;
                    }

                    list.push(cursor.value);

                    if (list.length < pageSize) {
                        cursor.continue();
                    } else {
                        resolve({ total, list });
                    }
                };
                cursorReq.onerror = () => reject(cursorReq.error);
            };
            countReq.onerror = () => reject(countReq.error);
            return;
        }

        // 如果有过滤条件，需要遍历统计
        const cursorReq = index.openCursor(range, 'prev');
        const list: SaveEntity[] = [];
        let total = 0;
        let skipped = 0;
        const startIndex = (currentPage - 1) * pageSize;
        const filterLower = methodFilter.toLowerCase();

        cursorReq.onsuccess = (e) => {
            const cursor = (e.target as IDBRequest).result as IDBCursorWithValue;
            if (!cursor) {
                resolve({ total, list });
                return;
            }

            const val = cursor.value as SaveEntity;
            // [修改] 优先使用顶层 method 字段，兼容旧数据
            const method = (val.method || String(val.requestRaw?.SendData?.method ?? '')).toLowerCase();

            if (method.includes(filterLower)) {
                total++;
                if (skipped < startIndex) {
                    skipped++;
                } else if (list.length < pageSize) {
                    list.push(val);
                }
            }

            cursor.continue();
        };
        cursorReq.onerror = () => reject(cursorReq.error);
        tx.onerror = () => reject(tx.error);
    });
}

export async function getRetentionDates(): Promise<string[]> {
    const retention = await getRetentionDays();
    const daysCount = retention;
    const today = new Date();
    const arr: string[] = [];
    for (let i = 0; i < daysCount; i++) {
        const d = new Date(today.getFullYear(), today.getMonth(), today.getDate() - i);
        arr.push(formatDateYYYYMMDD(d));
    }
    return arr;
}

// 监听设置变更
export function watchSettingsChanges(
    callback: (changes: {
        recordingEnabled?: boolean;
        diagnosticLogEnabled?: boolean;
        logRetentionDays?: number;
    }) => void
): () => void {
    const listener = (changes: Record<string, chrome.storage.StorageChange>, areaName: string) => {
        if (areaName !== 'local') return;

        const result: {
            recordingEnabled?: boolean;
            diagnosticLogEnabled?: boolean;
            logRetentionDays?: number;
        } = {};

        if (STORAGE_KEYS.RECORDING_ENABLED in changes) {
            result.recordingEnabled = changes[STORAGE_KEYS.RECORDING_ENABLED].newValue;
        }
        if (STORAGE_KEYS.LOG_LEVEL in changes) {
            result.diagnosticLogEnabled = changes[STORAGE_KEYS.LOG_LEVEL].newValue;
        }
        if (STORAGE_KEYS.LOG_RETENTION_DAYS in changes) {
            result.logRetentionDays = changes[STORAGE_KEYS.LOG_RETENTION_DAYS].newValue;
        }

        if (Object.keys(result).length > 0) {
            callback(result);
        }
    };

    chrome.storage.onChanged.addListener(listener);
    return () => chrome.storage.onChanged.removeListener(listener);
}

export function initInRequestService(log: { setLevel: (level: number) => void }) {
    InitRecordingEnabled();
    initRequestStore().catch(console.error);
    isDiagnosticLogEnabled()
        .then((enabled) => {
            if (enabled) {
                log.setLevel(DIAGNOSTIC_LOG_LEVEL);
            }
        })
        .catch(() => {
            log.setLevel(DEFAULT_LOG_LEVEL);
        });
    watchSettingsChanges(({ diagnosticLogEnabled, recordingEnabled }) => {
        if (diagnosticLogEnabled !== undefined) {
            log.setLevel(diagnosticLogEnabled ? DIAGNOSTIC_LOG_LEVEL : DEFAULT_LOG_LEVEL);
        }
        if (recordingEnabled !== undefined) {
            recordingEnabledCache = recordingEnabled;
        }
    });
}
