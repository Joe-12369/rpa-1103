(() => {
    if (typeof chrome === "undefined" || !chrome.runtime?.id) return;

    function buildPayload() {
        let navigationStart = Date.now();
        let domComplete: number | undefined;
        try {
            const nav = performance.getEntriesByType("navigation")[0] as PerformanceNavigationTiming;
            if (nav) {
                navigationStart = Math.round(nav.startTime + performance.timeOrigin);
                domComplete = Math.round(nav.domComplete + performance.timeOrigin);
            } else if ((performance as any).timing) {
                const t = (performance as any).timing;
                navigationStart = t.navigationStart;
                if (t.domComplete && t.domComplete >= t.navigationStart) domComplete = t.domComplete;
            }
        } catch { }
        const frameLength = window.frames.length;
        const readyComplete = document.readyState === "complete";
        return {
            url: location.href,
            title: document.querySelector("title")?.innerText || document.title,
            navigationStart,
            domComplete,
            loadCompletedAt: readyComplete && domComplete ? domComplete : undefined,
            loadDuration:
                domComplete && navigationStart ? domComplete - navigationStart : undefined,
            status: readyComplete ? "complete" : "loading",
            frames: frameLength
        };
    }

    chrome.runtime.onMessage.addListener(
        (msg: any, _sender: chrome.runtime.MessageSender, sendResponse: (res: any) => void) => {
            if (msg?.type === "PERF_REQUEST") {
                try {
                    sendResponse({ ok: true, data: buildPayload() });
                } catch {
                    sendResponse({ ok: false });
                }
                return true;
            }
            return false;
        }
    );
})();