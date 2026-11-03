
// src/background/entry.mv3.ts
//@ts-nocheck

import './env.adapter';
import * as Background from './background'
import { log } from '@/utils/logUtils';
import { STORAGE_KEYS } from '@/utils/requestStore';

function onEventRegister() {
    self.addEventListener('install', (event) => {
        log.info('Service Worker 正在安装...');
        self?.skipWaiting();
    });

    self.addEventListener('activate', (event) => {
        log.info(`Service Worker 已激活`);
        event.waitUntil(self.clients.claim());
    });

    chrome.runtime.onInstalled.addListener(async function (details) {
        try {
            log.info('Z-RPA Extension: ', details.reason)
            Background.ReloadTabs();
            chrome.storage.local.remove(['recordingEnabled']);

        } catch (e) {
            log.error("unable to installed: ", e)
        }
    })
}

async function ensureOffscreen() {
    if (!chrome.offscreen) return;

    try {
        if (chrome.offscreen.hasDocument && await chrome.offscreen.hasDocument()) {
            log.info("offscreen already exists");
            startServiceHub(36516);
            return;
        }
    } catch (e) {
        log.warn("check offscreen document failed:", e);
    }

    log.info("create ensureOffscreen");
    chrome.offscreen.createDocument({
        url: 'pages/offscreen.html',
        reasons: ['BLOBS'], // 任意合法 reason（比如 BLOBS/DOM_SCRAPING/CLIPBOARD 等）
        justification: 'Legacy SignalR (jQuery  ASP.NET SignalR 2.x)'
    }, () => {
        if (chrome.runtime.lastError) {
            const message = chrome.runtime.lastError.message || "";
            log.error("create offscreen failed:", message);
            if (message.includes("Only a single offscreen document")) {
                startServiceHub(36516);
            }
        } else {
            log.info("offscreen created");
            startServiceHub(36516);
        }
    });
}

function startServiceHub(port?: number) {
    chrome.runtime.sendMessage({ _svc: 'signalr_start', port });
}

function sendToServiceHub(payload: any) {
    chrome.runtime.sendMessage({ _svc: 'signalr_send', payload });
}

async function getServiceHubEnabled(): Promise<boolean> {
    try {
        const result = await chrome.storage.local.get(STORAGE_KEYS.SERVICE_HUB_ENABLED);
        return result[STORAGE_KEYS.SERVICE_HUB_ENABLED] === true;
    } catch (e) {
        log.error("read ServiceHub setting failed:", e);
        return false;
    }
}

function watchServiceHubSettingRelay() {
    chrome.storage.onChanged.addListener((changes, areaName) => {
        if (areaName !== "local" || !(STORAGE_KEYS.SERVICE_HUB_ENABLED in changes)) return;

        const enabled = changes[STORAGE_KEYS.SERVICE_HUB_ENABLED].newValue === true;
        chrome.runtime.sendMessage({ _svc: 'servicehub_setting_changed', enabled });
    });
}

chrome.runtime.onMessage.addListener((m, _sender, _sendResponse) => {
    if (!m || !m._svc) return;
    switch (m._svc) {
        case 'debugPage_resend':
            Background.received(m.data);
            break;
        case 'signalr_in':
            Background.received(m.data);
            break;
        case 'signalr_status':
            log.info('[signalr-status]', m);
            break;
        case 'signalr_error':
            log.error('[signalr-error]', m);
            break;
        case 'servicehub_get_enabled':
            getServiceHubEnabled().then((enabled) => _sendResponse({ enabled }));
            return true;
        case 'openConsole': {
            chrome?.runtime.openOptionsPage();
            break;
        }
    }
});

function ServiceWorkerInit() {
    log.info('[SW Init] Start');
    try {
        Background.markOnce("__ONINSTALL__", onEventRegister);
        Background.legacyBackgroundBootstrap();
        Background.markOnce("__SERVICEHUB_SETTING_RELAY__", watchServiceHubSettingRelay);
        ensureOffscreen();
    } catch (e) {
        log.error('[SW Init] Error', e);
    }
    log.info('[SW Init] End');
}

ServiceWorkerInit();


