const extensionV3_Id = 'pcopnibgkbdnlaeagepigbboebdfejmb';
const extensionV2_Id = "nfiffhdmjkohbbolbamdboplicimjnpm";

var EnableTrace = false;
var flag = false;
chrome.storage.local.get(["TRACE_LOG"], (result) => {
    if (result) {
        EnableTrace = result.TRACE_LOG;
    }
});

class ServiceWorkerPage {
    static extensionUrl = chrome.runtime.getURL('iirpa/ServiceWorkerPage.html')
    static extensionWndData =
        {
            state: "minimized",
            type: "popup",
            url: ServiceWorkerPage.extensionUrl
        }
    static currentWindowId;

    static
    {
        ServiceWorkerPage.CreateExtensionTabIfNeed()
    }

    static async GetExtensionWindowId() {
        const tab = await ServiceWorkerPage.GetExtensionTab()
        const windowId = tab ? tab.windowId : null;
        return windowId
    }
    static async GetExtensionTab() {
        const [tab] = await chrome.tabs.query({ url: ServiceWorkerPage.extensionUrl, windowType: 'popup' })
        if (tab) {
            console.log('Found extension tab:', tab);
            return tab
        } else {
            console.warn('No extension tab found.');
            return null
        }
    }
    static async CreateExtensionTabIfNeed(waittime = 1000) {
        if (flag === false) {
            flag = true;
            if (!(await ServiceWorkerPage.GetExtensionTab())) {
                const tab = await chrome.windows.create(ServiceWorkerPage.extensionWndData);
                ServiceWorkerPage.currentWindowId = await ServiceWorkerPage.GetExtensionWindowId()
                await TaskDelay(waittime)
                console.log('New extension window:', tab.id, ServiceWorkerPage.currentWindowId);
            }
        }
    }
}

function Reload() {
    chrome.tabs.query({}, (tabsList) => {
        for (tab of tabsList) {
            if (tab.url && tab.url.indexOf("chrome://") != 0) {
                chrome.tabs.reload(tab.id)
            }
        }
    })
}

chrome.runtime.onInstalled.addListener(async function (details) {
    try {
        console.log('Z-RPA Extension: ' + details.reason)
        Reload();
        DisableV2Extension();
        await ServiceWorkerPage.CreateExtensionTabIfNeed();
    } catch (e) {
        console.error("unable to installed: " + e)
    }
})

chrome.tabs.onCreated.addListener(function (tab) {
    TraceLog(`New tab: ${tab.id}`);
    cleanDuplicateTabs(tab);
})

chrome.windows.onCreated.addListener(async function (window) {
    TraceLog(`New window: ${window}`);
    await ServiceWorkerPage.CreateExtensionTabIfNeed();
});

chrome.windows.onRemoved.addListener(async function (windowid) {
    TraceLog(`Removed window: ${windowid}`);
    if (ServiceWorkerPage.currentWindowId === windowid) {
        console.warn('Extension window was closed!');
        //await ServiceWorkerPage.CreateExtensionTabIfNeed();
    }
});

chrome.management.onEnabled.addListener(async function (extInfo) {
    if (extInfo.id === extensionV3_Id) {
        TraceLog(`enable extension: ${extInfo.id}`);
        await ServiceWorkerPage.CreateExtensionTabIfNeed();
        Reload();
    }
});

function DisableV2Extension() {
    if (chrome.management && chrome.management.get && chrome.management.setEnabled) {
        try {
            chrome.management.get(extensionV2_Id, function (result) {
                if (chrome.runtime.lastError) {
                    console.warn('Error in chrome.management.get:', chrome.runtime.lastError.message);
                }
                else if (result && result.enabled) {
                    chrome.management.setEnabled(extensionV2_Id, false, function () {
                        if (chrome.runtime.lastError) {
                            console.error('Error in chrome.management.setEnabled:', chrome.runtime.lastError.message);
                        }
                        else {
                            console.info('Disable extensionV2!');
                        }
                    });
                }
            });
        }
        catch (e) {
            console.error(e)
        }
    }
}
function cleanDuplicateTabs(targetTab) {
    const targetPath = extensionV3_Id + '/iirpa/ServiceWorkerPage.html';
    if (targetTab.url && targetTab.url.includes(targetPath)) {
        chrome.tabs.query({}, async function (tabs) {
            const matchingTabs = tabs.filter(t => t.url && t.url.includes(targetPath));
            if (matchingTabs.length <= 1) {
                return;
            }
            matchingTabs.sort((a, b) => b.id - a.id);
            let reservedTab = matchingTabs[0];
            for (const tab of matchingTabs) {
                try {
                    const wndInfo = await chrome.windows.get(tab.windowId);
                    if (wndInfo.type === 'popup') {
                        reservedTab = tab;
                        break;
                    }
                } catch (error) {
                    console.error('Error getting window info:', error);
                }
            }
            const tabsToRemove = matchingTabs.filter(tab => tab.id !== reservedTab.id);
            const removePromises = tabsToRemove.map(tab => {
                return new Promise((resolve, reject) => {
                    chrome.tabs.remove(tab.id, () => {
                        if (chrome.runtime.lastError) {
                            reject(chrome.runtime.lastError);
                        } else {
                            resolve();
                        }
                    });
                });
            });

            try {
                await Promise.all(removePromises);
            } catch (error) {
                console.error('Error removing tabs:', error);
            }
        });
    }
}
function TaskDelay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function TraceLog(message) {
    try {
        if (EnableTrace === true) {
            console.log(new Date().toLocaleString() + ":" + message);
        }
    }
    catch (e) {

    }
}
let lastWebPage;
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type === "GET_TRACE_LOG") {
        chrome.storage.local.get(["TRACE_LOG"], (result) => {
            sendResponse({ traceLog: result.TRACE_LOG });
        });
        return true;
    }
    else if (message.type === "SET_TRACE_LOG") {
        EnableTrace = message.value
        sendResponse({ success: true });
        return true;
    }
    else if (message.type === "GET_LASTPAGE") {
        sendResponse(lastWebPage);
        return true;
    }
    else if (message.type === 'GET_TAB_STATUS') {
        const status = tabStatus.get(message.tabId);
        sendResponse(status || { status: 'success', error: null });
        return true;
    }
});

chrome.webNavigation.onCompleted.addListener((details) => {
    lastWebPage = { status: true, url: details.url };
}, { url: [{ schemes: ['http', 'https', 'file'] }] });

chrome.webNavigation.onErrorOccurred.addListener((details) => {
    lastWebPage = { status: false, url: details.url };
}, { url: [{ schemes: ['http', 'https', 'file'] }] });

const tabStatus = new Map();

chrome.webNavigation.onCompleted.addListener((details) => {
    if (details.frameId === 0) { // 只关注主frame
        tabStatus.set(details.tabId, {
            status: 'success',
            error: ''
        });
    }
});

chrome.webNavigation.onErrorOccurred.addListener((details) => {
    if (details.frameId === 0) {
        tabStatus.set(details.tabId, {
            status: 'error',
            error: details.error
        });
    }
});

chrome.tabs.onRemoved.addListener((tabId) => {
    tabStatus.delete(tabId);
});

