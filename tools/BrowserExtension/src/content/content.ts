//src/content/content.ts
//@ts-nocheck
import "./perfProbe";

function isRuntimeAvailable() {
    try {
        return typeof chrome !== "undefined"
            && !!chrome.runtime?.id
            && typeof chrome.runtime.sendMessage === "function";
    } catch {
        return false;
    }
}

function disableOpenConsoleHotkey() {
    try {
        document.removeEventListener('keydown', onOpenConsoleHotkey);
    } catch { }
}

function sendOpenConsoleMessage() {
    try {
        if (!isRuntimeAvailable()) {
            disableOpenConsoleHotkey();
            return;
        }

        chrome.runtime.sendMessage({ _svc: 'openConsole' }, () => {
            try {
                if (chrome.runtime.lastError) {
                    disableOpenConsoleHotkey();
                }
            } catch {
                disableOpenConsoleHotkey();
            }
        });
    } catch {
        disableOpenConsoleHotkey();
    }
}

function onOpenConsoleHotkey(event) {
    if (event.ctrlKey && event.key.toLowerCase() === 'y') {
        event.preventDefault();
        sendOpenConsoleMessage();
    }
}

document.addEventListener('keydown', onOpenConsoleHotkey);

function handle(method, params) {
    try {
        if (method === 'init') {
            const result = eval.call(window, params.code)
            return { code: 0, result: result }
        }
        else {
            if (typeof iirpa === 'undefined' || iirpa.codeId != params.codeId) {
                return { code: -2, message: 'need init' }
            }
            else {
                return iirpa.handle(method, params)

            }
        }
    }
    catch (error) {
        return {
            code: 1,
            error: {
                code: -1, // ERRORS.UNKNOWN
                message: error.stack
            }
        }
    }
}
