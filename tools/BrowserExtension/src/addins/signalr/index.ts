// rpa_client.js
//@ts-nocheck
(function () {
    var TAG = '[offscreen:rpa_client]';
    function log() { try { console.log.apply(console, [TAG].concat([].slice.call(arguments))); } catch { } }
    function warn() { try { console.warn.apply(console, [TAG].concat([].slice.call(arguments))); } catch { } }
    function error() { try { console.error.apply(console, [TAG].concat([].slice.call(arguments))); } catch { } }

    var state = {
        libsReady: false,
        pendingPort: null,
        hub: null,
        proxy: null,
        clientFlag: 'Chrome',
        sends: [],
        reconnecting: false,
        keepTick: 0,
        serviceHubEnabled: false,
        manualStop: false,
        generation: 0
    };

    const PORT_RANGE = { start: 36516, end: 36525 };

    function sendRuntimeMessage(message) {
        return new Promise(function (resolve) {
            try {
                chrome.runtime.sendMessage(message, function (response) {
                    var err = chrome.runtime.lastError;
                    if (err) {
                        warn('runtime message failed:', err.message);
                        resolve(null);
                        return;
                    }
                    resolve(response || null);
                });
            } catch (e) {
                warn('runtime message exception:', e);
                resolve(null);
            }
        });
    }

    async function readServiceHubEnabled() {
        try {
            var result = await sendRuntimeMessage({ _svc: 'servicehub_get_enabled' });
            return !!(result && result.enabled === true);
        } catch (e) {
            warn('read ServiceHub setting failed, fallback disabled:', e);
            return false;
        }
    }

    function applyServiceHubEnabled(enabled, reason) {
        state.serviceHubEnabled = enabled === true;
        if (!state.serviceHubEnabled) {
            log('ServiceHub setting disabled');
            stopSignalR(reason || 'disabled');
        } else {
            state.manualStop = false;
            log('ServiceHub setting enabled');
            startSignalR(getSavedPort() || PORT_RANGE.start);
        }
    }

    function stopSignalR(reason) {
        state.manualStop = true;
        state.pendingPort = null;
        state.sends = [];
        state.generation++;

        var hub = state.hub;
        state.hub = null;
        state.proxy = null;

        try { hub && hub.stop(); } catch (e) { warn('hub stop error:', e); }
        try {
            chrome.runtime.sendMessage({ _svc: 'signalr_status', ok: false, phase: 'stopped', reason: reason || 'stop' });
        } catch (e) { }
    }

    /** ========== Queue flush ========== **/
    function flushSends() {
        if (!state.proxy) return;
        var n = state.sends.length;
        if (n) log('flush queued sends:', n);
        while (state.sends.length) {
            var payload = state.sends.shift();
            try {
                state.proxy.invoke('SendTransferMessage', JSON.stringify(payload));
            } catch (e) {
                error('flush invoke error:', e);
            }
        }
    }

    /** ========== Port caching ========== **/
    function saveGoodPort(port) {
        try { localStorage.setItem('rpa_good_port', String(port)); } catch { }
    }
    function getSavedPort() {
        try {
            const val = localStorage.getItem('rpa_good_port');
            if (val) return parseInt(val, 10);
        } catch { }
        return null;
    }

    /** ========== Probe logic ========== **/
    async function probeSignalRPort(port, timeout = 1000) {
        const url = `http://127.0.0.1:${port}/rpa/signalr/negotiate`;
        try {
            const controller = new AbortController();
            const timer = setTimeout(() => controller.abort(), timeout);
            const res = await fetch(url, { signal: controller.signal });
            clearTimeout(timer);

            if (!res.ok) return false;
            const text = await res.text();
            const ok = text.trim().startsWith('{') && text.includes('ConnectionToken');
            if (ok) {
                log(`probe OK on port: ${port}`);
                saveGoodPort(port);
                return true;
            }
            return false;
        } catch (err) {
            log(`probe failed on port ${port}:`, err.message);
            return false;
        }
    }

    async function findAvailablePort(startPort = PORT_RANGE.start, endPort = PORT_RANGE.end) {
        for (let port = startPort; port <= endPort; port++) {
            const ok = await probeSignalRPort(port);
            if (ok) return port;
        }
        return null;
    }

    /** ========== SignalR startup ========== **/
    async function startSignalR(initPort) {
        state.serviceHubEnabled = await readServiceHubEnabled();
        if (!state.serviceHubEnabled) {
            log('ServiceHub disabled, skip startSignalR');
            stopSignalR('disabled');
            return;
        }

        if (!state.libsReady) {
            state.pendingPort = initPort;
            log('libs not ready, remember port:', initPort);
            return;
        }

        state.manualStop = false;
        var generation = ++state.generation;
        log('startSignalR requested, initial port:', initPort);

        let port = await findAvailablePort(getSavedPort() || initPort || PORT_RANGE.start, PORT_RANGE.end);
        if (!port) {
            const errMsg = `no available SignalR service found in ${PORT_RANGE.start}–${PORT_RANGE.end}`;
            error(errMsg);
            chrome.runtime.sendMessage({ _svc: 'signalr_error', phase: 'probe', error: errMsg });
            return;
        }

        state.serviceHubEnabled = await readServiceHubEnabled();
        if (generation !== state.generation) {
            log('startSignalR superseded, skip connect');
            return;
        }
        if (!state.serviceHubEnabled) {
            log('ServiceHub disabled before connect, skip connect');
            stopSignalR('disabled');
            return;
        }

        saveGoodPort(port);
        try { state.hub && state.hub.stop(); } catch (e) { }

        const url = 'http://127.0.0.1:' + port + '/rpa';
        log('startSignalR ->', url);

        state.hub = $.hubConnection(url);
        state.hub.qs = { ClientType: (1 << 2), ClientFlag: state.clientFlag };
        state.proxy = state.hub.createHubProxy('TransferHub');

        // 注册消息接收事件
        state.proxy.on('newTransferMessage', function (data) {
            try {
                if (data) data.connType = 'servicehub';
                var preview = (data && (data.SendData?.method || data.method)) || 'unknown';
                log('recv newTransferMessage:', preview);
                chrome.runtime.sendMessage({ _svc: 'signalr_in', data: data, callback: sendTransferMessage });
            } catch (e) {
                error('onMessage error:', e);
                chrome.runtime.sendMessage({ _svc: 'signalr_error', phase: 'onMessage', error: String(e) });
            }
        });

        // 启动连接
        state.hub
            .start()
            .done(function () {
                if (generation !== state.generation || state.manualStop || !state.serviceHubEnabled) return;
                log('hub started OK on port:', port);
                chrome.runtime.sendMessage({ _svc: 'signalr_status', ok: true, port: port });
                state.hub.disconnectTimeout = 3000;
                flushSends();
            })
            .fail(function (msg) {
                const errMsg = String(msg || '');
                warn('hub start fail:', errMsg);
                chrome.runtime.sendMessage({ _svc: 'signalr_error', phase: 'start', error: `connection failed on port ${port}` });
                error(`connection failed on port ${port}, stop retries.`);
            });

        // 自动重连
        state.hub.disconnected(function () {
            if (generation !== state.generation || state.manualStop || !state.serviceHubEnabled) {
                log(`hub disconnected on port ${port}, skip retry`);
                return;
            }
            warn(`hub disconnected on port ${port}, retry same port in 3s`);
            setTimeout(() => {
                if (generation !== state.generation || state.manualStop || !state.serviceHubEnabled) {
                    log(`retry skipped on port ${port}`);
                    return;
                }
                try { state.hub.stop(); } catch (e) { }
                startSignalR(port);
            }, 3000);
        });
    }

    /** ========== Message send ========== **/
    function sendTransferMessage(payload) {
        if (!state.proxy) {
            state.sends.push(payload);
            log('proxy not ready, push queue. size=', state.sends.length);
            return;
        }
        try {
            var method = (payload && (payload.SendData?.method || payload.method)) || 'unknown';
            log('send invoke ->', method);
            state.proxy.invoke('SendTransferMessage', JSON.stringify(payload));
        } catch (e) {
            error('invoke error:', e);
            chrome.runtime.sendMessage({ _svc: 'signalr_error', phase: 'invoke', error: String(e) });
        }
    }

    /** ========== Heartbeat ========== **/
    (function startKeepAlive() {
        try {
            setInterval(function () {
                state.keepTick++;
                if (state.keepTick % 3 === 1) log('keepalive tick', state.keepTick);
                chrome.runtime.sendMessage({ _svc: 'keepalive', t: Date.now() });
            }, 10000);
            log('keepalive started (20s)');
        } catch (e) {
            error('keepalive error:', e);
        }
    })();

    /** ========== Chrome runtime commands ========== **/
    chrome.runtime.onMessage.addListener(function (m) {
        if (!m || !m._svc) return;
        switch (m._svc) {
            case 'signalr_start':
                log('cmd: signalr_start', m.port || PORT_RANGE.start);
                startSignalR(m.port || PORT_RANGE.start);
                break;
            case 'signalr_send':
                log('cmd: signalr_send ->', m.payload);
                sendTransferMessage(m.payload);
                break;
            case 'signalr_stop':
                log('cmd: signalr_stop');
                stopSignalR('command');
                break;
            case 'servicehub_setting_changed':
                log('cmd: servicehub_setting_changed', m.enabled);
                applyServiceHubEnabled(m.enabled, 'disabled');
                break;
            default:
                log('cmd: unknown', m._svc);
                break;
        }
    });

    /** ========== Wait for libs ========== **/
    (function waitForLibs(retry) {
        retry = retry || 0;
        try {
            if (window.$ && $.hubConnection) {
                state.libsReady = true;
                log('libs_ready: jQuery + SignalR ready');
                chrome.runtime.sendMessage({ _svc: 'signalr_status', ok: true, phase: 'libs_ready' });
                if (state.pendingPort != null) {
                    var p = state.pendingPort;
                    state.pendingPort = null;
                    log('starting with pending port:', p);
                    startSignalR(p);
                }
                return;
            }
        } catch (e) { }
        if (retry > 10) {
            error('libs not ready after retries');
            chrome.runtime.sendMessage({ _svc: 'signalr_error', phase: 'load', error: 'jquery/signalr not ready' });
            return;
        }
        setTimeout(function () { waitForLibs(retry + 1); }, 2000);
    })();
})();
