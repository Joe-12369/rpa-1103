//@ts-nocheck
(function () {
    const chromeVersion = window.CHROME_VERSION;

    // javascript-obfuscator:disable
    const CODE_ID = '$@{ExtensionProvider_CODE_ID}';
    // javascript-obfuscator:enable  

    const ERRORS = {
        UNKNOWN: -1,  // 未知异常    
        PERMISSION: 2,// 权限问题
        NO_RESOURCE: 3,  // 无请求资源
        COMMON: 100,  // 通用异常
    }
    const FIREFOX_WEB_REQUEST_TYPE_MAPPING = {
        font: 'Font',
        image: 'Image',
        imageset: 'Image',
        main_frame: 'Document',
        sub_frame: 'Document',
        media: 'Media',
        object: 'Other',
        object_subrequest: 'Other',
        ping: 'Ping',
        script: 'Script',
        stylesheet: 'Stylesheet',
        web_manifest: 'Manifest',
        websocket: 'WebSocket',
        xmlhttprequest: 'XHR',
        other: 'Other'
    }
    class CrossOriginError extends Error {
        constructor(message) {
            super(message || "")
        }
    }
    class HandlerError extends Error {
        constructor(message, code) {
            super(message || "")
            this.code = code || ERRORS.COMMON
        }
    }
    class ContentUnknownError extends Error {
        constructor(message) {
            super(message || "")
        }
    }
    const WRAPPER_API = {
        'debugger': [
            'attach',
            'detach',
            'sendCommand',
            'getTargets'
        ]
    }
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
    async function getTabStatusAsync(tab) {
        if (tab.status === 'complete') {
            const item = tabStatus.get(tab.id);
            if (item && item.status) {
                return item;
            }
            return {
                status: 'success', error: ''
            };
        } else if (tab.status === 'loading') {
            return {
                status: 'loading', error: ''
            };
        } else {
            return {
                status: 'error', error: 'unknow'
            };
        }
    }

    async function withAlertBypass(tabIds,isCloseAlert, actionFn) {

        const ids = Array.isArray(tabIds) ? tabIds : [tabIds];
        if (ids.length === 0) return;

        const validTabIds = new Set();

        if(isCloseAlert){
            // 批量并发挂载 Debugger
            const attachPromises = ids.map(async (targetId) => {
                try {
                    const tab = await chrome.tabs.get(targetId);
                    // 过滤浏览器内置协议页
                    if (tab.url && (tab.url.startsWith('chrome://') || tab.url.startsWith('edge://'))) {
                        return; 
                    }
                    const debuggee = { tabId: targetId };
                    await debuggerManager.attach(tab.id);
                    await chrome.debugger.sendCommand(debuggee, "Page.enable");
                    validTabIds.add(targetId);
                } catch (err) {
                    console.warn(`[Tab ${targetId}] Debugger 挂载跳过 (可能是不支持的页面):`, err.message);
                }
            });
            
            // 等待所有可用目标挂载完毕
            await Promise.allSettled(attachPromises);
        }

        // 这个变量必须单独声明，以便在 finally 块中可以通过引用精准 remove
        const dialogHandler = (source, method, params) => {
            // 只处理属于我们目标集合内、且事件类型为弹窗打开的通知
            if (validTabIds.has(source.tabId) && method === "Page.javascriptDialogOpening") {
                console.log(`[Tab ${source.tabId}] 拦截到原生弹窗，正在执行系统级点击确认...`);
                
                chrome.debugger.sendCommand(source, "Page.handleJavaScriptDialog", {
                    accept: true 
                }).catch(err => console.error("处理弹窗失败:", err));
            }
        };

        if (validTabIds.size > 0) {
            chrome.debugger.onEvent.addListener(dialogHandler);
        }

        try {
            // 如果有 beforeunload 弹窗，Tab 会被挂起不关，同时触发上面的 dialogHandler
            // await chrome.tabs.remove(ids);
            // 执行外部传进来的自定义动作函数
            if (typeof actionFn === 'function') {
                await actionFn(); 
            }

            if (chrome.runtime.lastError) {
                throw new Error(chrome.runtime.lastError.message);
            }

            // 弹窗可能刚冒出来还没来得及被点击，我们的 Debugger 就撤退了。
            if (validTabIds.size > 0) {
                await new Promise(resolve => setTimeout(resolve, 800)); // 800 毫秒通常足够 CDP 捕获并处理弹窗
            }

        } finally {
            if (validTabIds.size > 0&&isCloseAlert) {
                // 注销全局事件监听器
                chrome.debugger.onEvent.removeListener(dialogHandler);
                validTabIds.forEach(targetId => {
                    try{
                     debuggerManager.detach(targetId);}
                    catch{
                    }
                });
            }
        }
    }

    class UniqueId {
        constructor(uniqueId) {
            // uniqueId: {frameId}/{frameId}/{nodeId}/{tagName}, '1/2/66/TD'             
            const tokens = uniqueId.split('/')
            if (tokens.length < 3)
                throw new HandlerError(`无效的 UniqueId, ${uniqueId}`)
            tokens.pop() // remove tag name             
            tokens.pop() // remove node id             
            this.raw = uniqueId
            this.frameIds = tokens.map(m => parseInt(m))
            this.targetFrame = this.frameIds[this.frameIds.length - 1]
        }
    }

    class EventCache {
        constructor(limit) {
            this._pool = []
            this._limit = limit
            this.id = +new Date()
        }

        push(event) {
            if (this._pool.length >= this._limit) {
                this.shift()
            }
            this._pool.push(event)
        }

        shift() {
            return this._pool.shift()
        }

        get() {
            return this._pool
        }
    }

    class HttpEventCache extends EventCache {
        constructor(limit, filter) {
            super(limit)
            const { regFilter, typeFilter } = filter
            Object.assign(this, {
                _bodys: [],
                regFilter: regFilter ? new RegExp(regFilter) : undefined,
                typeFilter
            })
        }

        shift() {
            const removed = super.shift()
            this._bodys = this._bodys.filter(b => b.requestId != removed.requestId)
        }

        async fetchBody(tabId, requestId) {
            try {
                await new Promise(res => setTimeout(res, 1000))
                const bodyCache = {}
                const bodyData = await debuggerManager.getResponseBody(tabId, requestId)
                bodyCache.requestId = requestId
                bodyCache.base64Encoded = !!bodyData.base64Encoded
                bodyCache.body = bodyData.body || ''
                this._bodys.push(bodyCache)
            } catch { }
        }

        getCacheBody(requestId) {
            return this._bodys.filter(b => b.requestId === requestId)[0]
        }

        push(event, tabId) {
            if (this.typeFilter) {
                if (event.type !== this.typeFilter) {
                    return
                }
            }
            if (this.regFilter) {
                if (!(event.response || {}).url || !this.regFilter.test(event.response.url)) {
                    return
                }
            }
            if (event.requestId === event.loaderId) {
                return
            }
            const mapped = {
                requestId: event.requestId,
                url: event.response.url,
                type: event.type,
                status: event.response.status,
                headers: event.response.headers
            }

            this.fetchBody(tabId, event.requestId)
            super.push(mapped);
        }
    }

    class WSEventCache extends EventCache {
        push(event) {
            if (!((event.response || {}).opcode === 1)) {
                return
            }
            const { response: { payloadData }, requestId } = event
            const mapped = {
                requestId,
                payloadData,
                type: 'WebSocket'
            }
            super.push(mapped)
        }
    }

    class WebRequestSendEventCache extends EventCache {
        push(event) {
            const { requestId } = event
            let filter = browser.webRequest.filterResponseData(requestId)
            super.push({
                requestId,
                buffer: streamFilterBuffer(filter)
            })
        }
    }

    class WebRequestReciveEventCache extends EventCache {
        push(event) {
            super.push({
                requestId: event.requestId,
                url: event.url,
                type: FIREFOX_WEB_REQUEST_TYPE_MAPPING[event.type] || event.type,
                status: event.statusCode,
                headers: event.responseHeaders,
            })
        }
    }
    const EVENT_CACHE_FACTORY = {
        'Network.responseReceived': HttpEventCache,
        'Network.webSocketFrameReceived': WSEventCache,
        'Network.webSocketFrameSent': WSEventCache,
    }

    class Debugger {
        constructor(tabId) {
            this.tabId = tabId
            this._ready = undefined
            this._operation = Promise.resolve()
            this._caches = new Map()
            this._executionContexts = new Map();
            //跨域context 
            this._frameMapping = new Map();
        }

        addCache(method, filter) {
            if (!this._caches.get(method)) {
                this._caches.set(method, [])
            }
            const caches = this.getCache(method)
            caches.splice(0, caches.length)
            const factory = EVENT_CACHE_FACTORY[method] || EventCache
            const cache = new factory(5000, filter)
            caches.push(cache)
            return cache.id
        }

        getCache(method) {
            return this._caches.get(method) || []
        }

        hasActiveCaches() {
            return Array.from(this._caches.values()).some(caches => Array.isArray(caches) && caches.length > 0)
        }

        /**
         * 旧发送方法，仅用于初始化或明确知道 session 的场景
         */
        _rawSendCommand(method, commandParam, sessionId) {
            const target = sessionId ? { tabId: this.tabId, sessionId } : { tabId: this.tabId };
            return crx.debugger.sendCommand(target, method, commandParam);
        }
        async _attach() {
            try {
                const targets = await crx.debugger.getTargets();
                const target = targets.find(t => t.tabId === this.tabId);
                if (target && target.attached) {
                    try { await crx.debugger.detach({ tabId: this.tabId }); } catch (e) { }
                }

                await crx.debugger.attach({ tabId: this.tabId }, '1.3');

                // 必须启用 flatten 模式以支持跨域 iframe
                const autoAttachParam = {
                    autoAttach: true,
                    waitForDebuggerOnStart: false,
                    flatten: true,
                    filter: [{ type: 'iframe', exclude: false }]
                };
                await this._rawSendCommand('Target.setAutoAttach', autoAttachParam);

                await this._rawSendCommand('Page.enable', {});
                await this._rawSendCommand('Runtime.enable', {});
                await this._rawSendCommand('Network.enable', {});
                return true;
            } catch (e) {
                this._ready = undefined;
                throw e;
            }
        }

        //处理contextId--> sessionId 转换
        async _sendCommand(method, commandParam) {
            let targetSessionId = undefined;

            // 如果参数中带有 contextId，检查它是否属于某个子 Session
            if (commandParam && commandParam.contextId) {
                const ctx = this._executionContexts.get(commandParam.contextId);
                if (ctx && ctx.sessionId) {
                    targetSessionId = ctx.sessionId;
                    // 跨域 Session 的 Runtime.evaluate 通常不需要 contextId (除非在该 session 内有多个隔离环境)
                    // 这里的处理是为了兼容：如果是在子 session 执行 evaluate，去掉 contextId 避免 -32000
                    if (method === 'Runtime.evaluate') {
                        delete commandParam.contextId;
                    }
                }
            }

            return this._rawSendCommand(method, commandParam, targetSessionId);
        }

        async attach() {
            if (this._ready) {
                return await this._ready
            }
            const attachPromise = this._attach()
            this._ready = attachPromise
            try {
                await attachPromise
                // 修复：_attach 过程中如果触发了 detach，会通过事件监听器将 _ready 置空
                // 所以成功后需要重新确认 _ready 的状态，保证其指向已完成的 Promise
                this._ready = attachPromise
                return true
            } catch (e) {
                if (this._ready === attachPromise) {
                    this._ready = undefined
                }
                throw e
            }
        }

        async detach() {
            if (this._ready) {
                await this._ready
                try {
                    await crx.debugger.detach({ tabId: this.tabId })
                } catch (e) {
                    const msg = String(e && (e.message || e))
                    if (!msg.includes('Detached') && !msg.includes('not attached')) {
                        throw e
                    }
                } finally {
                    this.markDetached()
                }
                // 仅在手动调用detach时清理数据
                this.clear()
            }
        }

        triggerEvent(method, params, sessionId) {
            //将 sessionId 注入 params 方便后面逻辑使用，或者直接处理
            switch (method) {
                case 'Target.attachedToTarget':
                    // 当新 iframe 被挂载时，立即开启其 Runtime 域
                    const subSessionId = params.sessionId;
                    this._rawSendCommand('Runtime.enable', {}, subSessionId).catch(() => { });
                    break;

                case 'Runtime.executionContextCreated':
                    if (params?.context?.id) {
                        const ctx = params.context;
                        // 记录该上下文所属的 sessionId
                        ctx.sessionId = sessionId;
                        this._executionContexts.set(ctx.id, ctx);
                    }
                    break;

                case 'Runtime.executionContextDestroyed':
                    if (params?.executionContextId) {
                        this._executionContexts.delete(params.executionContextId);
                    }
                    break;

                case 'Runtime.executionContextsCleared':
                    this._executionContexts.clear();
                    break;
            }

            this.getCache(method).forEach(c => c.push(params, this.tabId));
        }

        markDetached() {
            this._ready = undefined
        }
        clearContexts() {
            this._executionContexts.clear()
        }
        clear() {
            this._caches.clear()
        }

        async withAttached(action, options = {}) {
            const run = this._operation.catch(() => { }).then(async () => {
                await this.attach()
                try {
                    return await action()
                } finally {
                    if (options.detach !== false && !this.hasActiveCaches()) {
                        await this.detach()
                    }
                }
            })
            this._operation = run.then(
                            () => undefined,
                            () => undefined
                        )
            return await run
        }

        async sendCommand(method, commandParam, options = {}) {
            return await this.withAttached(() => this._sendCommand(method, commandParam), options)
        }

        async getExecutionContexts(frameId, name) {
            return await this.withAttached(async () => {
                const targetOrigin = name && name.endsWith('/') ? name.slice(0, -1) : name;

                let cdpFrameId = this._frameMapping.get(frameId || 0);
                if (!cdpFrameId && frameId) {
                    cdpFrameId = await this._identifyFrame(frameId);
                }

                if (!cdpFrameId) {
                    const { frameTree } = await this._sendCommand('Page.getFrameTree', {});
                    cdpFrameId = frameTree.frame.id;
                }

                // 过滤并排序：带 sessionId Context 优先级最高
                const contexts = Array.from(this._executionContexts.values())
                    .filter(ctx => {
                        const isOriginMatch = !targetOrigin || ctx.origin === targetOrigin;
                        return isOriginMatch && ctx.auxData.frameId === cdpFrameId;
                    })
                    .sort((a, b) => {
                        const scoreA = (a.sessionId ? 10 : 0) + (a.id > 100000 ? 5 : 0);
                        const scoreB = (b.sessionId ? 10 : 0) + (b.id > 100000 ? 5 : 0);
                        return scoreB - scoreA;
                    });

                return contexts;
            }, { detach: false });
        }

        /**
         * 通过一次性注入特征值来同步两个体系的 ID
         */
        async _identifyFrame(intFrameId) {
            const token = `find_${intFrameId}_${Date.now()}`;
            try {
                // 1: 必须指定 world: 'MAIN'
                await chrome.scripting.executeScript({
                    target: { tabId: this.tabId, frameIds: [intFrameId] },
                    world: 'MAIN', // 确保注入到页面原始 JS 环境
                    func: (t) => {
                        window.__CDP_SYNC_TOKEN__ = t;
                        console.debug('Token injected into MAIN world:', t);
                    },
                    args: [token]
                });

                // 2: 遍历时增加延迟或确保上下文最新
                for (const ctx of this._executionContexts.values()) {
                    // 我们只检查 type 为 'default' 的上下文（即 MAIN world）
                    if (ctx.auxData?.isDefault) {
                        const { result } = await this._sendCommand('Runtime.evaluate', {
                            expression: `window.__CDP_SYNC_TOKEN__`,
                            contextId: ctx.id,
                            returnByValue: true // 确保返回原始值
                        });

                        if (result?.value === token) {
                            const foundId = ctx.auxData.frameId;
                            this._frameMapping.set(intFrameId, foundId);

                            this._sendCommand('Runtime.evaluate', {
                                expression: `delete window.__CDP_SYNC_TOKEN__`,
                                contextId: ctx.id
                            }).catch(() => { });

                            return foundId;
                        }
                    }
                }
            } catch (e) {
                console.error("Frame identification failed", e);
            }
            return null;
        }

        async getMainContexts(frameId) {
            return await this.withAttached(async () => {
                let context;
                if (!frameId) {
                    const { frameTree } = await this._sendCommand('Page.getFrameTree', {});
                    const mainFrameId = frameTree.frame.id;
                    console.log('use mainFrameId:', mainFrameId);
                    context = [...this._executionContexts.values()].find(ctx => ctx.auxData.type === 'default'
                        && ctx.auxData.frameId === mainFrameId);
                }
                else {
                    let frames = await crx.webNavigation.getAllFrames(this.tabId)
                    const targetFrame = frames.find(item => item.frameId === frameId);
                    if (targetFrame) {
                        const { frameTree } = await this._sendCommand('Page.getFrameTree', {});
                        function findFrameIdByUrl(tree, url) {
                            if (tree.frame.url === url) {
                                return tree.frame.id;
                            }
                            if (tree.childFrames) {
                                for (const child of tree.childFrames) {
                                    const result = findFrameIdByUrl(child, url); if (result) return result;
                                }
                            }
                            return null;
                        }
                        const matchedFrameId = findFrameIdByUrl(frameTree, targetFrame.url);
                        context = [...this._executionContexts.values()].find(ctx => ctx.auxData.type === 'default'
                            && ctx.auxData.frameId === matchedFrameId);
                    } else {
                        console.error("can not find frameId:", frameId)
                    }
                }
                if (context) {
                    console.log('find main context:', context);
                    return [context]
                } else {
                    console.error('can not find main context');
                    return []
                }
            }, { detach: false });
        }


    }
    class DebuggerManager {
        constructor() {
            this._debuggers = new Map()
        }

        init() {
            this.onEvent()
            this.onDetach()
            this.onNavigation(); // 新增导航监听
        }
        onNavigation() {
            // 当页面发生跳转或刷新时，旧的 frameId 映射将失效
            chrome.webNavigation.onCommitted.addListener((details) => {
                if (details.tabId && this._debuggers.has(details.tabId)) {
                    const debuggee = this.get(details.tabId);
                    // 主框架刷新，清理全部
                    if (details.frameId === 0) {
                        debuggee.clearContexts();
                        console.debug(`Tab ${details.tabId} refreshed, cleared cache.`);
                    } else {
                        // 子框架刷新，只清理该子框架映射
                        debuggee._frameMapping.delete(details.frameId);
                    }
                }
            });
        }
        onEvent() {
            if (chrome.debugger) {
                chrome.debugger.onEvent.addListener((source, method, params) => {
                    const debuggee = this.get(source.tabId);
                    if (debuggee) {
                        // 将 sessionId 作为第三个参数传给 triggerEvent
                        debuggee.triggerEvent(method, params, source.sessionId);
                    }
                });
            }
        }

        onDetach() {
            if (chrome.debugger) {
                // 仅标记断开状态
                chrome.debugger.onDetach.addListener(({ tabId }, reason) => {
                    const debuggee = this._debuggers.get(tabId)
                    if (debuggee) {
                        debuggee.markDetached()
                        debuggee.clear()
                        if (reason === 'target_closed') {
                            this._debuggers.delete(tabId)
                        }
                    }
                })
            }
        }

        check(tabId) {
            return !!this._debuggers.get(tabId)
        }

        get(tabId) {
            let debuggee = this._debuggers.get(tabId)
            if (!debuggee) {
                debuggee = new Debugger(tabId)
                this._debuggers.set(tabId, debuggee);
            }
            return debuggee
        }

        async addCache(tabId, method, params) {
            const debuggee = this.get(tabId)
            const cacheId = debuggee.addCache(method, params)
            await debuggee.attach()
            return cacheId
        }

        getCache(tabId, method) {
            return this.get(tabId).getCache(method).reduce((all, cache) => all.concat(cache.get()), [])
        }

        getCacheBody(tabId, requestId) {
            const cache = this.get(tabId).getCache('Network.responseReceived')[0]
            return cache ? cache.getCacheBody(requestId) : null
        }

        sendCommand(tabId, method, commandParam, options = {}) {
            return this.get(tabId).sendCommand(method, commandParam, options)
        }

        async getResponseBody(tabId, requestId) {
            try {
                return await this.sendCommand(tabId, 'Network.getResponseBody', {
                    requestId
                })
            } catch (error) {
                const parseError = tryParseJSON(error.message)
                if ((parseError || {}).code === -32000) {
                    return {}
                } else {
                    throw error
                }
            }
        }

        detach(tabId) {
            return this.get(tabId).detach()
        }
        async getExecutionContexts(tabId, frameId, name) {
            return this.get(tabId).getExecutionContexts(frameId, name);
        }
        async getMainContexts(tabId, frameId) {
            return this.get(tabId).getMainContexts(frameId);
        }
        async attach(tabId) {
            return this.get(tabId).attach().catch(console.error);
        }
    }
    class WebRequestManager {
        constructor() {
            this._caches = new Map()
            this._isListening = false
            this._requestHandleOption = [
                this.handlerRequest.bind(this),
                { urls: ["<all_urls>"] },
                ["blocking"]
            ]
            this._responseHandleOption = [
                this.handlerResponse.bind(this),
                { urls: ["<all_urls>"] },
                ["responseHeaders"]
            ]
        }

        startListen() {
            browser.webRequest.onBeforeRequest.addListener.apply(browser.webRequest.onBeforeRequest.addListener, this._requestHandleOption)
            browser.webRequest.onResponseStarted.addListener.apply(browser.webRequest.onResponseStarted.addListener, this._responseHandleOption)
            this._isListening = true
        }

        handlerRequest(details) {
            const { tabId } = details
            if (!this._caches.get(tabId)) { return }
            this._caches.get(tabId).request.push(details)
        }

        handlerResponse(details) {
            const { tabId } = details
            if (!this._caches.get(tabId)) { return }
            this._caches.get(tabId).response.push(details)
        }

        _getCache(tabId) {
            return this._caches.get(tabId)
        }

        addCache(tabId) {
            let cache = this._getCache(tabId)
            if (!cache) {
                cache = {
                    request: new WebRequestSendEventCache(200),
                    response: new WebRequestReciveEventCache(200)
                }
                this._caches.set(tabId, cache)
            }
        }

        checkCacheAvailable(tabId) {
            return this._isListening && !!this._getCache(tabId)
        }

        getCache(tabId) {
            if (!this._caches.get(tabId)) return []
            return this._caches.get(tabId).response.get() || []
        }

        async getBody(tabId, requestId, requestType, contentType) {
            const cache = this._caches.get(tabId)
            if (!cache) {
                return {}
            }
            const cacheItem = cache.request.get().filter(r => r.requestId === requestId)[0]
            if (!cacheItem) {
                return {}
            }
            const buffer = cacheItem.buffer
            if (!buffer) {
                return {}
            }
            const blob = new Blob(await buffer, { type: contentType })
            if (['Script', 'Document', 'Stylesheet', 'Manifest', 'XHR'].some(t => t === requestType)) {
                return {
                    body: await blob.text(),
                    base64Encoded: false
                }
            } else {
                return {
                    body: await blobToBase64(blob),
                    base64Encoded: true
                }
            }
        }

        endListen() {
            browser.webRequest.onBeforeRequest.removeListener(this._requestHandleOption[0])
            browser.webRequest.onResponseStarted.removeListener(this._responseHandleOption[0])
            this._caches.clear()
            this._isListening = false
        }
    }
    let contentScript = null
    let downloadData = null
    let frameCache = []
    const utils = {
        pointClientToPage: (pointLike, scale) => {
            pointLike.x = Math.round(pointLike.x / scale)
            pointLike.y = Math.round(pointLike.y / scale)
        },
        pointPageToClient: (pointLike, scale) => {
            pointLike.x *= scale
            pointLike.y *= scale
        },
        rectClientToPage: (rectLike, scale) => {
            rectLike.left = Math.round(rectLike.left / scale)
            rectLike.top = Math.round(rectLike.top / scale)
            rectLike.width = Math.round(rectLike.width / scale)
            rectLike.height = Math.round(rectLike.height / scale)
        },
        rectPageToClient: (rectLike, scale) => {
            rectLike.left *= scale
            rectLike.top *= scale
            rectLike.width *= scale
            rectLike.height *= scale
        },
        clone: (obj) => {
            let copy
            if (null == obj || "object" != typeof obj) return obj
            if (obj instanceof Date) {
                copy = new Date()
                copy.setTime(obj.getTime())
                return copy
            }
            if (obj instanceof Array) {
                copy = []
                for (var i = 0, len = obj.length; i < len; i++) {
                    copy[i] = utils.clone(obj[i])
                }
                return copy
            }
            if (obj instanceof Object) {
                copy = {}
                for (var attr in obj) {
                    if (obj.hasOwnProperty(attr))
                        copy[attr] = utils.clone(obj[attr])
                }
                return copy
            } throw new Error("Unable to copy obj! Its type isn't supported.")
        },
        ignoreEmptyProperties: (obj) => {
            const clone = {}
            for (const propName in obj) {
                if (obj[propName] !== null && obj[propName] !== undefined) {
                    clone[propName] = obj[propName]
                }
            }
            return clone
        },
        delay: (ms) => new Promise(res => setTimeout(res, ms)),
        record: (data) => {
            downloadData = data
        }
    }
    const tryParseJSON = (str) => {
        try {
            return JSON.parse(str)
        } catch { }
    }
    const blobToBase64 = (blob) => {
        return new Promise((res, _) => {
            const reader = new FileReader()
            reader.onloadend = () => res(reader.result)
            reader.readAsDataURL(blob)
        })
    }
    const streamFilterBuffer = (filter) => {
        return new Promise((res, _) => {
            let data = []
            filter.ondata = event => {
                data.push(event.data)
                filter.write(event.data)
            }
            // 忽略错误
            filter.onerror = error => res([])
            filter.onstop = event => {
                res(data)
                filter.close()
            }
        })
    }
    const argToArray = function (arg) { return Array.prototype.slice.call(arg) }
    const wrapperChromeApi = (moduleName, methodName, checkError = true) => {
        let apiRoot = window.browser || window.chrome || globalThis.chrome
        if (!apiRoot || !apiRoot[moduleName] || !apiRoot[moduleName][methodName]) {
            return () => Promise.reject(new HandlerError('No permissions.', ERRORS.PERMISSION))
        }
        if (window.browser) {
            return apiRoot[moduleName][methodName].bind(apiRoot[moduleName])
        }
        return function () {
            return new Promise((res, rej) => {
                chrome[moduleName][methodName].apply(chrome[moduleName], argToArray(arguments).concat([function () {
                    if (checkError && chrome.runtime.lastError) {
                        rej.apply(null, [chrome.runtime.lastError].concat(argToArray(arguments)))
                    }
                    res.apply(null, argToArray(arguments))
                }]))
            })
        }
    }
    const uniqBy = (list, filter) => {
        const ids = Array.from(new Set(list.map(filter)))
        return ids.map(i => list.filter(j => filter(j) === i)[0])
    }
    const crx = Object.assign({
        webNavigation:
        {
            getAllFrames: (tabId) => {
                return new Promise((resolve, reject) => {
                    chrome.webNavigation.getAllFrames({
                        tabId: tabId
                    }, (frames) => {
                        resolve(frames)
                    })
                })
            }
        },
        windows: {
            getAll: (queryInfo) => {
                return new Promise((resolve, reject) => {
                    chrome.windows.getAll(queryInfo, (wnd) => {
                        resolve(wnd)
                    })
                })
            },
            get: (windowId) => {
                return new Promise((resolve, reject) => {
                    chrome.windows.get(windowId, null, (wnd) => {
                        resolve(wnd)
                    })
                })
            },
            getLastFocused: () => {
                return new Promise((resolve, reject) => {
                    chrome.windows.getLastFocused(null, (wnd) => {
                        resolve(wnd)
                    })
                })
            },
            update: (windowId, updateInfo) => {
                return new Promise((resolve, reject) => {
                    chrome.windows.update(windowId, updateInfo, (wnd) => {
                        resolve(wnd)
                    })
                })
            },
            getCurrent: () => {
                return new Promise((resolve, reject) => {
                    chrome.windows.getCurrent(null, (wnd) => {
                        resolve(wnd)
                    })
                })
            }
        },
        cookies: {
            getAll: (details) => {
                return new Promise((resolve, reject) => {
                    chrome.cookies.getAll(details, (cookies) => { resolve(cookies) })
                })
            },
            remove: (details) => {
                return new Promise((resolve, reject) => {
                    chrome.cookies.remove(details, () => { resolve() })
                })
            },
            set: (details) => {
                return new Promise((resolve, reject) => {
                    chrome.cookies.set(details, () => { resolve() })
                })
            }
        },
        downloads: {
            addCreatedEvent: () => {
                chrome.downloads.onCreated.removeListener(utils.record);
                chrome.downloads.onCreated.addListener(utils.record);
            },
            removeCreatedEvent: () => {
                downloadData = null;
                chrome.downloads.onCreated.removeListener(utils.record);
            },
            search: (queryInfo) => {
                return new Promise((resolve, reject) => {
                    chrome.downloads.search(queryInfo, (items) => {
                        resolve(items)
                    })
                })
            },
            searchV1: (queryInfo) => {
                return new Promise((resolve, reject) => {
                    chrome.downloads.search(queryInfo, (items) => {
                        if (downloadData)
                            items = [downloadData]
                        resolve(items)
                        downloadData = null
                    })
                })
            },
            download: (options) => {
                return new Promise((resolve, reject) => {
                    chrome.downloads.download(options, (downloadId) => {
                        resolve(downloadId)
                    })
                })
            }
        },
        tabs: {
            query: (queryInfo) => {
                return new Promise((resolve, reject) => {
                    chrome.tabs.query(queryInfo, (tabs) => {
                        resolve(tabs)
                    })
                })
            },
            create: (createInfo) => {
                return new Promise((resolve, reject) => {
                    chrome.tabs.create(createInfo, (tab) => {
                        resolve(tab)
                    })
                })
            },
            update: (tabId, updateInfo) => {
                return new Promise((resolve, reject) => {
                    chrome.tabs.update(tabId, updateInfo, (tab) => {
                        resolve(tab)
                    })
                })
            },
            get: (tabId) => {
                return new Promise((resolve, reject) => {
                    chrome.tabs.get(tabId, (tab) => { resolve(tab) })
                })
            },
            reload: (tabId, bypassCache) => {
                return new Promise((resolve, reject) => {
                    chrome.tabs.reload(tabId, {
                        bypassCache: bypassCache
                    }, () => {
                        resolve()
                    })
                })
            },
            goForward: (tabId) => {
                return new Promise((resolve, reject) => { chrome.tabs.goForward(tabId, () => { resolve() }) })
            },
            goBack: (tabId) => {
                return new Promise((resolve, reject) => {
                    chrome.tabs.goBack(tabId, () => { resolve() })
                })
            },
            remove: (tabIds) => {
                return new Promise((resolve, reject) => {
                    chrome.tabs.remove(tabIds, () => { resolve() })
                })
            },
            getZoom: (tabId) => {
                return new Promise((resolve, reject) => {
                    chrome.tabs.getZoom(tabId, (factor) => { resolve(factor) })
                })
            },
            executeScriptOnFrame: (tabId, frameId, code) => {
                return new Promise((resolve, reject) => {
                    const details = {
                        frameId: frameId,
                        code: code,
                        matchAboutBlank: true
                    }
                    chrome.tabs.executeScript(tabId, details, (result) => {
                        // result存在以下三种情况:                         
                        // 1. 正常执行返回数组(因为指定了frameId，所以数组中只有一项)                         
                        // 1.1 有返回值即是数组中的第一项                         
                        // 1.2 无返回值数组的第一项为null                         
                        // 2. Javascript执行失败，返回数组，且第一项为null，同1.2                         
                        // 3. 发生跨域错误，这时需要对chrome.runtime.lastError添加判断，否则会在控制台打印错误信息                         
                        if (chrome.runtime.lastError) {
                            reject(new CrossOriginError(chrome.runtime.lastError.message))
                        } else {
                            if (Array.isArray(result)) {
                                if (result.length == 1) {
                                    resolve(result[0])
                                }
                                else { resolve(result) }
                            } else {
                                reject(new Error(`unknown error: fail to execute script on ${frameId}`))
                            }
                        }
                    })
                })
            },
            getFrameIndex: async (tabId, frameId) => {
                let count = 0;
                while (count < 10) {
                    const getFrameIndex = crx.tabs.requestOnFrame(tabId, frameId, 'getFrameIndex');
                    let frameIndex = await crx.tabs.requestWait(getFrameIndex, 200);
                    if (frameIndex == -100 || frameIndex == null) {
                        await utils.delay(100);
                        count++;
                        continue;
                    }
                    return frameIndex;
                }
                return -1;
            },
            requestOnFrames: async (tabId, frameId, method, params, timeout) => {
                // 从最外层Iframe遍历到最内层Iframe                 
                // response存在以下四种情况                 
                // 0: 成功 { code: 0, result: null }                 
                // -1: 继续下次Frame请求(跨域) { code: -1, next: { frameIndex: 111, params: params } }                 
                //     next.params可能存储了上一次请求的结构                 
                // -2: content需要初始化 { code: -2, message: 'need init' }                 
                // 1: 请求失败 { code: 1, error: {code: -1, message: ''} }                 
                // undefined/null: content script注入失败(出现在某些about:blank页面)                  
                if (!params) {
                    params = {}
                }
                frameCache = []
                if (!params.codeId) {
                    params.codeId = CODE_ID
                }
                params.frameId = frameId
                const code = `handle('${method}', ${JSON.stringify(params)})`
                const response = await crx.tabs.executeScriptOnFrame(tabId, frameId, code)
                if (!response) {
                    return null
                } else if (response.code == 0) {
                    return response.result
                } else if (response.code == -1) {
                    // continue to next frame                     
                    let frames = await crx.webNavigation.getAllFrames(tabId)
                    for (const frame of frames) {
                        if (frame.parentFrameId === frameId) {
                            try {
                                let frameIndex = await crx.tabs.getFrameIndex(tabId, frame.frameId);
                                if (frameIndex == null) {
                                    continue;
                                }
                                if (response.next.frameIndexs && response.next.frameIndexs.includes(frameIndex)) {
                                    const request = crx.tabs.requestOnFrame(tabId, frame.frameId, method, response.next.params, timeout)
                                    let results = await crx.tabs.requestWait(request, timeout)
                                    for (const result of results) {
                                        frameCache.push(result)
                                    }
                                    if (response.next.frameIndexs.slice(-1) == frameIndex) {
                                        return frameCache
                                    }
                                }
                            } catch (e) {
                                if (e instanceof CrossOriginError) {
                                    continue
                                    // 忽略跨域异常                                 
                                } else { throw e }
                            }
                        }
                    }
                    throw new Error(`fail to request on frame, cannot call next frame`)
                } else if (response.code == -2) {
                    // need init content                     
                    const requestInit = crx.tabs.requestOnFrame(tabId, frameId, 'init', { code: contentScript }, timeout)
                    await crx.tabs.requestWait(requestInit, timeout);
                    // retry                     
                    const request = crx.tabs.requestOnFrame(tabId, frameId, method, params, timeout)
                    return crx.tabs.requestWait(request, timeout);
                } else {
                    // code === 1                     
                    const error = response.error
                    if (error.code == ERRORS.UNKNOWN) {
                        throw new ContentUnknownError(error.message)
                    } else {
                        throw new HandlerError(error.message, error.code)
                    }
                }
            },
            requestWait: async (actionPromise, timeout) => {
                if (!timeout) {
                    return await Promise.race([actionPromise]);
                }
                const timeoutPromise = new Promise(function (resolve) {
                    setTimeout(function () {
                        resolve(null);
                    }, timeout);
                });
                return await Promise.race([actionPromise, timeoutPromise])
            },
            requestOnFrame: async (tabId, frameId, method, params, timeout) => {

                // 从最外层Iframe遍历到最内层Iframe                 
                // response存在以下四种情况                 
                // 0: 成功 { code: 0, result: null }                
                // -1: 继续下次Frame请求(跨域) { code: -1, next: { frameIndex: 111, params: params } }                 
                //     next.params可能存储了上一次请求的结构                 
                // -2: content需要初始化 { code: -2, message: 'need init' }                 
                // 1: 请求失败 { code: 1, error: {code: -1, message: ''} }                 
                // undefined/null: content script注入失败(出现在某些about:blank页面)                  
                if (!params) {
                    params = {}
                }
                if (!params.codeId) {
                    params.codeId = CODE_ID
                }
                params.frameId = frameId
                const code = `handle('${method}', ${JSON.stringify(params)})`
                const response = await crx.tabs.executeScriptOnFrame(tabId, frameId, code)
                if (!response) {
                    return null
                } else if (response.code == 0) {
                    return response.result
                } else if (response.code == -1) {
                    // continue to next frame                     
                    let frames = await crx.webNavigation.getAllFrames(tabId)
                    for (const frame of frames) {
                        if (frame.parentFrameId === frameId) {
                            try {
                                let frameIndex = await crx.tabs.getFrameIndex(tabId, frame.frameId);
                                if (frameIndex == null) {
                                    continue;
                                }
                                if (response.next.frameIndexs && response.next.frameIndexs.length > 0) {
                                    if (response.next.frameIndexs && response.next.frameIndexs.includes(frameIndex)) {
                                        const request = crx.tabs.requestOnFrame(tabId, frame.frameId, method, response.next.params, timeout)
                                        let results = await crx.tabs.requestWait(request, timeout)
                                        if (!results || results.length < 1) {
                                            continue;
                                        }
                                        else {
                                            return results;
                                        }
                                    }
                                } else {
                                    /*let frameIndex = await crx.tabs.requestOnFrame(tabId, frame.frameId, 'getFrameIndex')*/
                                    if (frameIndex == response.next.frameIndex) {
                                        const request = crx.tabs.requestOnFrame(tabId, frame.frameId, method, response.next.params, timeout)
                                        return await crx.tabs.requestWait(request, timeout);
                                    }
                                }
                            } catch (e) {
                                if (e instanceof CrossOriginError) {
                                    continue
                                    // 忽略跨域异常                                 
                                } else { throw e }
                            }
                        }
                    }
                    throw new Error(`fail to request on frame, cannot call next frame`)
                } else if (response.code == -2) {
                    // need init content                     
                    const requestInit = crx.tabs.requestOnFrame(tabId, frameId, 'init', { code: contentScript }, timeout)
                    await crx.tabs.requestWait(requestInit, timeout);
                    // retry    
                    if (method == "querySelector") {
                        const request = crx.tabs.requestOnFrames(tabId, frameId, method, params, timeout)
                        return crx.tabs.requestWait(request, timeout);
                    } else {
                        const request = crx.tabs.requestOnFrame(tabId, frameId, method, params, timeout)
                        return crx.tabs.requestWait(request, timeout);
                    }
                } else {
                    // code === 1                     
                    const error = response.error
                    if (error.code == ERRORS.UNKNOWN) {
                        throw new ContentUnknownError(error.message)
                    } else {
                        throw new HandlerError(error.message, error.code)
                    }
                }
            },
            requestOnFrameReverse: async (tabId, frameId, method, params) => {
                // 从最内层Iframe遍历到最外层Iframe
                // response存在以下四种情况
                // 0: 成功 { code: 0, result: null }                 
                // -1: 继续下次Frame请求(跨域) { code: -1, next: { params: params } }                 
                //     next.params可能存储了上一次请求的结构                 
                // -2: content需要初始化 { code: -2, message: 'need init' }                 
                // 1: 请求失败 { code: 1, error: {code: -1, message: ''} }                 
                if (!params) {
                    params = {}
                } if (!params.codeId) {
                    params.codeId = CODE_ID
                } params.frameId = frameId
                const code = `handle('${method}', ${JSON.stringify(params)})`
                const response = await crx.tabs.executeScriptOnFrame(tabId, frameId, code)
                if (!response) {
                    return null
                } else if (response.code == 0) {
                    return response.result
                } else if (response.code == -1) {
                    // continue to next frame                     
                    let frames = await crx.webNavigation.getAllFrames(tabId)
                    for (const frame of frames) {
                        if (frame.frameId === frameId) {
                            try {
                                return await crx.tabs.requestOnFrameReverse(tabId, frame.parentFrameId, method, response.next.params)
                            } catch (e) {
                                if (e instanceof CrossOriginError) {
                                    continue
                                    // 忽略跨域异常                                 
                                } else { throw e }
                            }
                        }
                    }
                    throw new Error(`fail to request on frame, cannot call next frame`)
                } else if (response.code == -2) {
                    // need init content 
                    await crx.tabs.requestOnFrame(tabId, frameId, 'init', { code: contentScript })
                    // retry                     
                    return await crx.tabs.requestOnFrameReverse(tabId, frameId, method, params)
                } else {
                    // code === 1                     
                    const error = response.error
                    if (error.code == ERRORS.UNKNOWN) {
                        throw new ContentUnknownError(error.message)
                    } else {
                        throw new HandlerError(error.message, error.code)
                    }
                }
            },
            captureVisibleTab: wrapperChromeApi('tabs', 'captureVisibleTab'),
            captureTab: wrapperChromeApi('tabs', 'captureTab')
        }
    }, (() => {
        const wrapper = {}
        for (const [key, value] of Object.entries(WRAPPER_API)) {
            wrapper[key] = {}
            value.forEach(v => wrapper[key][v] = wrapperChromeApi(key, v))
        }
        return wrapper
    })())
    const debuggerManager = new DebuggerManager()
    debuggerManager.init()
    window.debuggerManager = debuggerManager
    const webRequestManager = new WebRequestManager()
    window.webRequestManager = webRequestManager
    window.crx = crx

    window.iirpa = new function () {
        const response = {
            ok: (content) => {
                return {
                    code: 200,
                    status: 'OK',
                    result: {
                        content: content, error: null
                    }
                }
            },
            fail: (code, message) => {
                return {
                    code: 200,
                    status: 'OK',
                    result: {
                        content: null,
                        error:
                        {
                            code: code,
                            message: message
                        }
                    }
                }
            }
        }
        var isConfig = false
        var isStart = false;
        // handlers中的方法必须全部为异步方法  
        const handlers = {
            dataCapture:
            {
                startConfig: async (params) => {
                    if (!isConfig) {
                        isConfig = true;
                        return await window.startConfig(params.config);
                    } else {
                        return null
                    }
                },
                startCapture: async (params) => {
                    if (!isStart) {
                        isStart = true;
                        return await window.startCapture(params.info);
                    } else {
                        return null
                    }
                },
                closeStart: async (params) => {
                    isStart = false;
                },
                closeConfig: async (params) => {
                    isConfig = false;
                },
                isExecutorJavaScript: async (params) => {
                    // params.tabId
                    // params
                    const result = await crx.tabs.requestOnFrame(params.tabId, 0, "isExecutorJavaScript", params)
                    return result
                },
                initInjectScript: async (params) => {
                    //code : "console.log('abc')"
                    await crx.tabs.requestOnFrame(params.tabId, 0, 'init', { code: params.code })
                },
            },
            windows:
            {
                active: async (params) => {
                    await chrome.windows.update(params.windowId, { focused: true });
                },
                getAll: async (params) => {
                    const result = await crx.windows.getAll({ "populate": true, "windowTypes": params.windowTypes });
                    return result;
                },
                update: async (params) => {
                    // {                    
                    //     windowId: 123                     
                    //     updateInfo: https://developers.chrome.com/extensions/windows#method-update                     
                    // }                     
                    await crx.windows.update(params.windowId, params.updateInfo)
                },
                maximize: async (params) => {
                    var winId = params.windowId;
                    if (!params.windowId || params.windowId < 0) {
                        const wnd = await crx.windows.getCurrent()
                        winId = wnd.id
                    }
                    await crx.windows.update(winId, { 'state': 'maximized' })
                },
                minimized: async (params) => {
                    const wnd = await crx.windows.getCurrent()
                    await crx.windows.update(wnd.id, { 'state': 'minimized' })
                },
            },
            cookies:
            {
                getAll: async (params) => {
                    // {                     
                    //     string\t(optional) url\t                     
                    //     string\t(optional) name\t                     
                    //     string\t(optional) domain\t                     
                    // }                     
                    params = utils.ignoreEmptyProperties(params)
                    const cookies = await crx.cookies.getAll(params)
                    if (chrome.runtime.lastError) {
                        throw new HandlerError(`获取Cookies失败, ${chrome.runtime.lastError.message}`)
                    } else {
                        for (const cookie of cookies) {
                            if (cookie.expirationDate) {
                                cookie.expirationDate = (new Date(cookie.expirationDate * 1000)).toLocaleString()
                            }
                        }
                        return cookies
                    }
                },
                remove: async (params) => {
                    // {                     
                    //     string\turl\t                     
                    //     string\tname\t                     
                    // }                     
                    await crx.cookies.remove(params)
                    if (chrome.runtime.lastError)
                        throw new HandlerError(`删除Cookies失败, ${chrome.runtime.lastError.message}`)
                },
                set: async (params) => {
                    // {                    
                    //     string\turl\t                     
                    //     string\t(optional) name\t                    
                    //     string\t(optional) value\t                     
                    //     string\t(optional) domain\t                     
                    //     string\t(optional) path\t                     
                    //     boolean\t(optional) secure\t                     
                    //     boolean\t(optional) httpOnly\t                     
                    //     string\t(optional) expirationDate                     
                    // }                     
                    params = utils.ignoreEmptyProperties(params)
                    if (params.expirationDate) {
                        params.expirationDate = (new Date(params.expirationDate)).getTime() / 1000
                    }
                    await crx.cookies.set(params)
                    if (chrome.runtime.lastError)
                        throw new HandlerError(`设置Cookies失败, ${chrome.runtime.lastError.message}`)
                }
            },
            tabs:
            {
                getIntoViewPortRect: async (params) => {
                    return await crx.tabs.requestOnFrame(params.tabId, 0, 'getIntoViewPortRect', params)
                },
                create: async (params) => {
                    const result = await crx.tabs.create(params)
                    return result
                },
                scroll: async (params) => {
                    const result = await crx.tabs.requestOnFrame(params.tabId, 0, 'scroll', params)
                    return result
                },
                scroll_V2: async (params) => {
                    const result = await crx.tabs.requestOnFrame(params.tabId, 0, 'scroll_V2', params)
                    return result
                },
                getScroll: async (params) => {
                    const result = await crx.tabs.requestOnFrame(params.tabId, 0, 'getScroll', params)
                    return result
                },
                scrollTo: async (params) => {
                    const result = await crx.tabs.requestOnFrame(params.tabId, 0, 'scrollTo', Object.assign({
                        behavior: 'smooth'
                    }, params))
                    return result
                },
                stop: async (params) => {
                    // {                     
                    //     tabId: 123
                    // }  
                    const result = await crx.tabs.executeScriptOnFrame(params.tabId, 0, 'window.stop()')
                    return result
                },
                getDocument: async (params) => {
                    const result = await crx.tabs.executeScriptOnFrame(params.tabId, 0, 'document.documentElement.innerHTML')
                    return result
                },
                executeScript: async (params) => {
                    // {                     
                    //     tabId: 123,
                    //     code: ......
                    // }  
                    const result = await crx.tabs.requestOnFrame(params.tabId, 0, 'executeScript', params)
                    return result
                },
                getDetail: async (params) => {
                    // {                     
                    //     tabId: 1     
                    //     windowId: 1
                    // }      
                    const tab = await crx.tabs.get(params.tabId)
                    if (tab) {
                        if (!params.windowId || params.windowId <= 0) {
                            params.windowId = tab.windowId;
                        }
                        const win = await crx.windows.get(params.windowId)
                        if (chrome.runtime.lastError) {
                            throw new HandlerError(`获取tab失败, ${chrome.runtime.lastError.message}`)
                        } else {
                            tab["windowdetail"] = win
                            const response = await getTabStatusAsync(tab);
                            if (response && response.status) {
                                tab["load_status"] = response.status
                            }
                            return tab
                        }
                    }
                    return null;
                },
                get: async (params) => {
                    // {                     
                    //     tabId: 1                     
                    // }                     
                    const tab = await crx.tabs.get(params.tabId)
                    if (chrome.runtime.lastError) {
                        throw new HandlerError(`获取tab失败, ${chrome.runtime.lastError.message}`)
                    } else {
                        return tab
                    }
                },
                query: async (params) => {
                    // https://developers.chrome.com/extensions/tabs#method-query                     
                    const tabIds = []
                    const original = params.original;
                    delete params.original;
                    const tabs = await crx.tabs.query(params)
                    for (const tab of tabs) {
                        if (original) {
                            tabIds.push(tab.id)
                        }
                        else if (tab.url && tab.url.indexOf("chrome://") !== 0
                            //&& tab.url.indexOf("chrome-extension://") !== 0
                            && tab.url.indexOf("edge://") !== 0
                            && tab.url.indexOf("se://") !== 0
                            && tab.url.indexOf("about:") !== 0) {
                            tabIds.push(tab.id)
                        }
                    }
                    return tabIds
                },
                active: async (params) => {
                    // {                     
                    //     tabId = 1                     
                    // }
                    const tab = await crx.tabs.get(params.tabId)
                    if (tab) {
                        chrome.windows.update(tab.windowId, { focused: true })
                        chrome.tabs.update(tab.id, { active: true })
                    }
                },
                switch: async (params) => {
                    const tab = await crx.tabs.get(params.tabId)
                    if (tab) {
                        chrome.tabs.update(tab.id, { active: true })
                    }
                },
                reload: async (params) => {
                    // {                     
                    //     tabId: 1                     
                    //     bypassCache: false                     
                    // }                     
                    await crx.tabs.reload(params.tabId, params.bypassCache)
                },
                navigate: async (params) => {
                    // {                     
                    //     tabId: 1                     
                    //     url: '...'                     
                    // }                     
                    await crx.tabs.update(params.tabId, { url: params.url })
                },
                goForward: async (params) => {
                    // {                    
                    //     tabId: 1                     
                    // }                     
                    await crx.tabs.goForward(params.tabId)
                },
                goBack: async (params) => {
                    // {                     
                    //     tabId: 1                     
                    // }                     
                    await crx.tabs.goBack(params.tabId)
                },
                close: async (params) => {
                    // {                     
                    //     tabId = 123,
                    //    isCloseAlert=false                     
                    // }                     
                    const tabId = params.tabId;
                     try {
                        await withAlertBypass([tabId],params.isCloseAlert,async ()=>{
                              await crx.tabs.remove([tabId]);
                            });
                    } catch (error) {
                        throw new HandlerError(`关闭tab失败, ${error.message}`);
                    }
                },
                closeList: async (params) => {                   
                    try {
                        await withAlertBypass(params.tabIds,params.isCloseAlert,async ()=>{
                           await crx.tabs.remove(params.tabIds);
                        });
                    } catch (error) {
                        throw new HandlerError(`关闭tab失败, ${error.message}`);
                    }
                },
                closeOther: async (params) => {
                    const tabs = await crx.tabs.query({})
                    const record = []
                    for (const tab of tabs) {
                        if (tab.url.indexOf("chrome-extension://") !== 0 && params.tabId != tab.id) {
                            record.push(tab.id)
                        }
                    }
                    if (record.length != 0) {
                         try {
                            await withAlertBypass(record,params.isCloseAlert,async ()=>{
                              await crx.tabs.remove(record);
                            });
                        } catch (error) {
                            throw new HandlerError(`关闭批量tab失败, ${error.message}`);
                        }
                    }
                },
                closeRight: async (params) => {
                    const tabs = await crx.tabs.query({})
                    const record = []
                    for (const tab of tabs.reverse()) {
                        if (tab.url.indexOf("chrome-extension://") !== 0) {
                            if (params.tabId != tab.id) {
                                record.push(tab.id)
                            } else {
                                break;
                            }
                        }

                    }
                    if (record.length != 0) {
                         try {
                            await withAlertBypass(record,params.isCloseAlert,async ()=>{
                              await crx.tabs.remove(record);
                            });
                        } catch (error) {
                            throw new HandlerError(`关闭批量tab失败, ${error.message}`);
                        }
                    }
                },
                closeAll: async (params) => {
                    try{

                       if (params.isCloseAlert) {
                            const tabs = await crx.tabs.query({})
                            const tabIds = tabs.map(m => m.id)
                            if (tabIds.length === 0) return
                                // 需要拦截 beforeunload 弹窗时，走 chrome.tabs.remove 路径。
                                // 先创建一个空白页作为占位，避免关闭所有 Tab 后窗口销毁导致 CDP 被提前断开。
                                const blankTab = await crx.tabs.create({ url: 'about:blank', active: false })
                                const blankTabId = blankTab.id

                                // 重新获取当前所有 tab（含空白页），过滤掉空白页后关闭其余
                                const allTabs =  await crx.tabs.query({})
                                const idsToClose = allTabs
                                    .map(t => t.id)
                                    .filter(id => id !== blankTabId)

                                if (idsToClose.length > 0) {
                                    await withAlertBypass(idsToClose, true, async () => {
                                        await crx.tabs.remove(idsToClose)
                                    })
                                }
                       } 
                       else{
                            const windows = await crx.windows.getAll({ windowTypes: ['normal', 'popup'] })
                            for (const win of windows) {
                                await new Promise((resolve, reject) => {
                                    chrome.windows.remove(win.id, () => {
                                        if (chrome.runtime.lastError) {
                                            reject(new HandlerError(`关闭窗口失败, ${chrome.runtime.lastError.message}`))
                                        } else {
                                            resolve()
                                        }
                                    })
                                })
                            }
                        }
                    }
                    catch (error) {
                        throw new HandlerError(`执行 closeAll 失败: ${error.message}`);
                    }
                },
                querySelectorAll: async (params) => {
                    // params.tabId
                    // params.selector
                    // params.relativeType    元素位置类型
                    // params.positionNum     元素定位数字，与relativeType结合使用
                    // params.resultAttrName  返回值属性名，对象层面的逻辑属性，不限于element的属性
                    // params.version             
                    if (!params.index) {
                        params.index = -1 // -1返回所有，否则返回指定索引位置的元素Id    
                    }
                    let frameid = 0;
                    if (params.relateElementId && params.mode && params.mode != "default") {
                        frameid = new UniqueId(params.relateElementId).targetFrame;
                    }
                    var result = await crx.tabs.requestOnFrames(params.tabId, frameid, 'querySelector', params)
                    return result
                },
                querySelector: async (params) => {
                    // params.tabId                     
                    // params.selector                     
                    // params.index                     
                    var result = await crx.tabs.requestOnFrame(params.tabId, 0, 'querySelector', params)
                    if (result && result.length > 0) {
                        return result[0]
                    } else {
                        return null
                    }
                },
                querySelectorByXpathAll: async (params) => {
                    // 查找所有也独立一个函数吧，预留后续兼容什么返回格式等场景
                    if (!params.index) {
                        params.index = -1  
                    }                   
                    var result = await crx.tabs.requestOnFrame(params.tabId, 0, 'querySelectorByXpath', params)
                    return result
                },
                querySelectorByXpath: async (params) => {
                    // params.tabId                     
                    // params.selector                     
                    // params.index                     
                    var result = await crx.tabs.requestOnFrame(params.tabId, 0, 'querySelectorByXpath', params)
                    if (result && result.length > 0) {
                        return result[0]
                    } else {
                        return null
                    }
                },
                queryElementsAll: async (params) => {
                    // params.tabId
                    // params.tagNames   
                    if (!params.frameId) {
                        params.frameId = 0;
                    }
                    let nextParams = utils.clone(params)
                    var result = await crx.tabs.requestOnFrame(nextParams.tabId, 0, 'queryElementsAll', nextParams)
                    let elementDatas = JSON.parse(result);
                    let allframeElementDatas = [];
                    let frames = await crx.webNavigation.getAllFrames(nextParams.tabId)
                    for (const frame of frames) {
                        try {
                            if (frame.frameId == 0) continue;
                            nextParams = utils.clone(params)
                            result = await crx.tabs.requestOnFrame(nextParams.tabId, frame.frameId, 'queryElementsAll', nextParams)
                            Array.prototype.push.apply(allframeElementDatas, JSON.parse(result));
                        }
                        catch { }
                    }
                    return JSON.stringify(elementDatas.concat(allframeElementDatas));
                },
                getViewElementsData: async (params) => {
                    if (!params.frameId) {
                        params.frameId = 0;
                    }
                    let params_clone = utils.clone(params)
                    var result = await crx.tabs.requestOnFrame(params_clone.tabId, 0, 'getViewElementsData', params_clone);
                    let elementsData = JSON.parse(result);
                    elementsData = await getChiidrenData(elementsData, params_clone);
                    return JSON.stringify(elementsData);
                    async function getChiidrenData(elesData, nextParams) {
                        //for (var i = 0; i < elesData.length; i++) {
                        if (!elesData.children || elesData.children.length == 0) {
                            return;
                        }
                        for (let data of elesData.children) {
                            if (!(data.tagName === 'iframe' || data.tagName === 'frame')) {
                                await getChiidrenData(data, nextParams);
                                continue;
                            }
                            const nextParams_clone = utils.clone(nextParams)
                            if (!nextParams_clone.frameTree) {
                                nextParams_clone.frameTree = []
                            }
                            nextParams_clone.frameTree.push(nextParams.frameId);
                            let frameId = new UniqueId(data.uniqueId).targetFrame;
                            let frames = await crx.webNavigation.getAllFrames(nextParams_clone.tabId);
                            for (const frame of frames) {
                                if (frame.parentFrameId != frameId) {
                                    continue;
                                }
                                try {
                                    let frameIndex = await crx.tabs.getFrameIndex(nextParams_clone.tabId, frame.frameId);
                                    if (frameIndex != data.frameIndex) {
                                        continue;
                                    }
                                    nextParams_clone.frameId = frame.frameId;
                                    nextParams_clone.frameRect = data.bounding;
                                    let frameResult = await crx.tabs.requestOnFrame(nextParams_clone.tabId, frame.frameId, 'getViewElementsData', nextParams_clone);
                                    let frameElementsData = JSON.parse(frameResult);
                                    frameElementsData = await getChiidrenData(frameElementsData, nextParams_clone);
                                    if (!!frameElementsData) {
                                        data.children.push(frameElementsData);
                                    }
                                    //Array.prototype.push.apply(data.children, frameElementsData);

                                } catch (e) {

                                }
                            }

                        }
                        return elesData;
                    }
                },
                setAllElementsUniqueId: async (params) => {
                    if (!params.frameId) {
                        params.frameId = 0;
                    }
                    let params_clone = utils.clone(params)
                    var result = await crx.tabs.requestOnFrame(params_clone.tabId, 0, 'setAllElementsUniqueId', params_clone);
                },
                queryCSSSelectorAll: async (params) => {
                    // params.tabId                     
                    // params.selector                     
                    // params.parent                     
                    params.index = -1 // -1返回所有，否则返回指定索引位置的元素Id                     
                    let frameId = 0
                    if (params.parent) {
                        const uid = new UniqueId(params.parent)
                        frameId = uid.targetFrame
                        params.frameTree = uid.frameIds
                    }
                    var result = await crx.tabs.requestOnFrame(params.tabId, frameId, 'queryCSSSelector', params)
                    return result
                },
                queryCSSSelector: async (params) => {
                    // params.tabId                     
                    // params.selector                     
                    // params.index                     
                    // params.parent                     
                    let frameId = 0
                    if (params.parent) {
                        const uid = new UniqueId(params.parent)
                        frameId = uid.targetFrame
                        params.frameTree = uid.frameIds
                    }
                    var result = await crx.tabs.requestOnFrame(params.tabId, frameId, 'queryCSSSelector', params)
                    if (result && result.length > 0) {
                        return result[0]
                    } else { return null }
                },
                elementFromPoint: async (params) => {
                    // {                     
                    //     tabId: 123,                    
                    //     x: 1,                     
                    //     y: 2                    
                    // }                     
                    const origial = utils.clone(params)
                    const zoomFactor = await crx.tabs.getZoom(params.tabId)
                    const scale = await crx.tabs.executeScriptOnFrame(params.tabId, 0, 'window.devicePixelRatio')
                    utils.pointClientToPage(params, scale)
                    params.containers = []
                    params.zoomFactor = zoomFactor

                    if (params.ApiVersion && params.ApiVersion === "v2") {
                        return await crx.tabs.requestOnFrame(params.tabId, 0, 'elementFromPoint_V2', params)
                    }
                    else {
                        return await crx.tabs.requestOnFrame(params.tabId, 0, 'elementFromPoint', params)
                    }
                },
                executeCommand: async (params) => {
                    // {                     
                    //     tabId: 123,                     
                    //     command: 'copy'                     
                    // }                     
                    return await crx.tabs.executeScriptOnFrame(params.tabId, 0, `document.execCommand("${params.command}")`)
                },
                getText: async (params) => {
                    // {                     
                    //     tabId: 123,                     
                    // }                     
                    const code = 'document.documentElement.outerText'
                    return await crx.tabs.executeScriptOnFrame(params.tabId, 0, code)
                },
                getHtml: async (params) => {
                    // {                     
                    //     tabId: 123,                     
                    // }                     
                    const code = 'document.documentElement.outerHTML'
                    return await crx.tabs.executeScriptOnFrame(params.tabId, 0, code)
                },
                executeScriptOnFrame: async (params) => {
                    // {
                    //     tabId: 123,
                    //     code: console.log("123")
                    // }
                    const result = await crx.tabs.executeScriptOnFrame(params.tabId, 0, params.code)
                    if (result === undefined || result === null) {
                        return null
                    } else {
                        return JSON.stringify(result)
                    }
                },
                queryCollectionData: async (params) => {
                    // params.tabId                     
                    // params.selector                     
                    params.index = -1 // -1返回所有，否则返回指定索引位置的元素Id                     
                    var result = await crx.tabs.requestOnFrame(params.tabId, 0, 'queryCollectionData', params)
                    return result
                },
                queryCollectionDataV2: async (params) => {
                    // params.tabId
                    // params.collectionConfig
                    // params.collectionConfig.scrollVerticalDelay
                    // params.collectionConfig.scrollMaxTotalDelay
                    let start_timestamp = new Date().getTime();
                    let lastScroll_timestamp = start_timestamp;
                    let now_timestamp = start_timestamp;
                    let frameId = 0;
                    let targetFrameId = -1;
                    let lastElementId = "";
                    let scrollTop = -100, scrollHeight = -100;
                    let scrollType = params.collectionConfig.scrollType;
                    if (!scrollType) scrollType = 0;
                    do {
                        try {
                            if (!params.colorGuidanceConfig && params.collectionConfig.scrollVerticalDelay > 0) {
                                await utils.delay(100);
                            }
                            now_timestamp = new Date().getTime();
                            if (targetFrameId === -1 || scrollType == 3) {
                                var elementId = await crx.tabs.requestOnFrame(params.tabId, frameId, 'getAndScrollLastElement', params);
                                if (!elementId) {
                                    continue;
                                }
                                if (elementId != lastElementId) {
                                    targetFrameId = new UniqueId(elementId).targetFrame;
                                    lastElementId = elementId;
                                    lastScroll_timestamp = new Date().getTime();
                                }
                            }
                            if (params.colorGuidanceConfig || scrollType == 1) {
                                break;
                            }
                            if (scrollType == 3) {
                                continue;
                            }
                            else {//if (scrollType == 0 || scrollType == 2) 
                                let domAreaInfo = await crx.tabs.requestOnFrame(params.tabId, targetFrameId, 'getDomAreaInfo');
                                if (scrollHeight != domAreaInfo.scrollHeight || scrollTop != domAreaInfo.scrollTop) {
                                    scrollHeight = domAreaInfo.scrollHeight;
                                    scrollTop = domAreaInfo.scrollTop;
                                    let _position = scrollType == 2 ? "distance" : "bottom";
                                    //滚动一次
                                    await crx.tabs.requestOnFrame(params.tabId, targetFrameId, 'scrollTo', {
                                        behavior: 'smooth', position: _position, distance: 500
                                    });
                                    lastScroll_timestamp = new Date().getTime();
                                }
                            }
                        } catch { }
                    } while (params.collectionConfig.scrollVerticalDelay > 0 && (now_timestamp - lastScroll_timestamp) < params.collectionConfig.scrollVerticalDelay)
                    if (targetFrameId === -1) {
                        console.log("未采集到数据");
                        return [];
                    }
                    params.lastElementId = lastElementId;
                    var result = await crx.tabs.requestOnFrame(params.tabId, targetFrameId, 'queryCollectionDataV2', params)
                    if (!result) {
                        console.log("采集数据为空");
                        result = [];
                    }
                    return result;
                },
                scrollAndCollectionData: async (params) => {
                    // params.tabId
                    // params.collectionConfig
                    // params.collectionConfig.scrollVerticalDelay
                    // params.collectionConfig.scrollMaxTotalDelay
                    let start_timestamp = new Date().getTime();
                    let lastScroll_timestamp = start_timestamp;
                    let now_timestamp = start_timestamp;
                    let frameId = 0;
                    let targetFrameId = -1;
                    let scrollTop = -100;
                    let markedElementId = params.markedElementId;
                    let scrollType = params.collectionConfig.scrollType;
                    if (!scrollType) scrollType = 0;
                    if (!markedElementId || scrollType == 3) {
                        do {
                            var elementId = await crx.tabs.requestOnFrame(params.tabId, frameId, 'getAndScrollLastElement', params);
                            if (!elementId) {
                                await utils.delay(100);
                                now_timestamp = new Date().getTime();
                                continue;
                            }
                            markedElementId = elementId;
                            break;
                        } while (params.collectionConfig.scrollVerticalDelay > 0 && (now_timestamp - lastScroll_timestamp) < params.collectionConfig.scrollVerticalDelay)
                    }
                    if (!markedElementId) {
                        return { data: [], markedElementId: "" };
                    }
                    params.markedElementId = markedElementId;
                    targetFrameId = new UniqueId(markedElementId).targetFrame;
                    if (scrollType != 3) {
                        let _position = scrollType == 0 ? "bottom" : "distance";
                        await crx.tabs.requestOnFrame(params.tabId, targetFrameId, 'scrollTo', {
                            behavior: 'smooth', position: _position, distance: 500
                        });
                    }
                    do {
                        let domAreaInfo = await crx.tabs.requestOnFrame(params.tabId, targetFrameId, 'getDomAreaInfo');
                        if (scrollTop != domAreaInfo.scrollTop) {
                            scrollTop = domAreaInfo.scrollTop;
                            await utils.delay(100);
                            now_timestamp = new Date().getTime();
                            continue;
                        }
                        break;
                    } while (params.collectionConfig.scrollVerticalDelay > 0 && (now_timestamp - lastScroll_timestamp) < params.collectionConfig.scrollVerticalDelay)

                    var result = await crx.tabs.requestOnFrame(params.tabId, targetFrameId, 'queryCollectionDataV2', params)
                    if (!result) {
                        console.log("采集数据为空");
                        result = [];
                    }
                    return { data: result, markedElementId: markedElementId };
                },
                getSimilarElement: async (params) => {
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getSimilarElement', params)
                },
                captureVisibleTab: async (params) => {
                    // params.tabId
                    // params.format
                    // params.quality
                    const { windowId } = await crx.tabs.get(params.tabId)
                    return await crx.tabs.captureVisibleTab(windowId, {
                        format: params.format,
                        quality: params.quality
                    })
                },
                captureTab: async (params) => {
                    // params.tabId
                    // params.rect
                    // params.format
                    // params.quality
                    const { tabId, rect, format, quality } = params
                    const options = {
                        format,
                        quality
                    }
                    return await crx.tabs.captureTab(tabId, rect ? Object.assign({ rect }, options) : options)
                }
            },
            downloads:
            {
                addCreatedEvent: async () => {
                    await crx.downloads.addCreatedEvent();
                },
                removeCreatedEvent: async () => {
                    await crx.downloads.removeCreatedEvent();
                },
                search: async (params) => {
                    return await crx.downloads.search(params)
                },
                searchV1: async (params) => {
                    return await crx.downloads.searchV1(params)
                },
                downloadUrl: async (params) => {
                    // params.url                     
                    return await crx.downloads.download({ url: params.url })
                },
            },
            elements:
            {
                getNextElementBuildSelector: async (params) => {
                    // params.innerTextFirst
                    //const element = new UniqueId(params.elementId);
                    var selector = await crx.tabs.requestOnFrame(params.tabId, 0, 'getNextElementBuildSelector', params)
                    const tab = await crx.tabs.get(params.tabId)
                    selector.unshift({
                        name: "tab",
                        props: [{
                            name: "title",
                            value: tab.title,
                            pattern: "equal",
                            accurate: "true"
                        },
                        {
                            name: "url",
                            value: tab.url,
                            pattern: "equal",
                            accurate: "false"
                        }]
                    })
                    return selector;
                },
                getUsableAttributes: async (params) => {
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getUsableAttributes', params)
                },
                getTurningPageElement: async (params) => {
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getTurningPageElement', params)
                },
                getIsTableName: async (params) => {
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getIsTableName', params)
                },
                getTableData: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getTableData', params)
                },
                getAttributes: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getAttributes', params)
                },
                getControlType: async (params) => {
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getControlType', params)
                },
                getProperty: async (params) => {
                    //params.tabId
                    //params.elementId
                    //params.name
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getProperty', params)
                },
                executeScript: async (params) => {
                    let frameId = 0;
                    if (params.elementId) {
                        frameId = (new UniqueId(params.elementId)).targetFrame;
                    }

                    if (window.IS_SW) {
                        const _wrapAsGlobal = (funcName, code) => `(function(){try{self["${funcName}"]=function(){try{return ${code};}catch(innerE){return{__is_err:true,msg:String(innerE.stack||innerE)};}};return{ok:true};}catch(e){return{ok:false,error:String(e.stack||e)};}})()`;

                        try {
                            const injectionCode = _wrapAsGlobal("__DYNAMIC_INIT__", params.code);
                            await self.attachToServiceWorkerAndSendCommand({
                                expression: injectionCode,
                                returnByValue: true
                            });

                            const fnReference = self["__DYNAMIC_INIT__"];
                            if (typeof fnReference !== 'function') throw new Error("__DYNAMIC_INIT__ undefined");

                            const execResults = await chrome.scripting.executeScript({
                                target: { tabId: params.tabId, frameIds: [frameId] },
                                world: params.world ?? "MAIN",
                                func: fnReference,
                                args: []
                            });

                            if (!execResults?.length) return null;

                            const rawResult = execResults[0].result;
                            if (rawResult?.__is_err) {
                                console.error("[executeScript] Inner Error:", rawResult.msg);
                                return null;
                            }

                            return (rawResult !== undefined && rawResult !== null) ? JSON.stringify(rawResult) : null;

                        } catch (err) {
                            console.error('[executeScript] Fatal Error:', err);
                            return null;
                        } finally {
                            delete self["__DYNAMIC_INIT__"];
                        }
                    }

                    // V2 版本
                    const result = await crx.tabs.executeScriptOnFrame(params.tabId, frameId, params.code);
                    return (result === undefined || result === null) ? null : JSON.stringify(result);
                },
                scrollIntoViewIfNeeded_V2: async (params) => {
                    // params.tabId                     
                    // params.elementId               
                    const element = new UniqueId(params.elementId);
                    await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'scrollIntoViewIfNeeded_V2', {
                        elementId: element.raw,
                    })
                },
                scrollIntoViewIfNeeded: async (params) => {
                    // params.tabId                     
                    // params.elementId               
                    const element = new UniqueId(params.elementId);
                    await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'scrollIntoViewIfNeeded', {
                        elementId: element.raw,
                        location: params.location
                    })
                },
                getBounding: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    const element = new UniqueId(params.elementId);
                    const scale = await crx.tabs.executeScriptOnFrame(params.tabId, 0, 'window.devicePixelRatio')
                    const bounding = await crx.tabs.requestOnFrameReverse(params.tabId, element.targetFrame, 'getElementBounding',
                        {
                            elementId: element.raw
                        })
                    utils.rectPageToClient(bounding, scale)
                    return bounding
                },
                getBoundingAll: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    const bounds = []
                    for (let i = 0; i < params.elementIds.length; ++i) {
                        params.elementId = params.elementIds[i]
                        const bounding = await handlers.elements.getBounding(params)
                        bounds.push(bounding)
                    }
                    return bounds;
                },
                buildCssSelector: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    const element = new UniqueId(params.elementId);
                    //添加元素全路径
                    const cs = await crx.tabs.requestOnFrameReverse(params.tabId, element.targetFrame, 'cssSelectorAll',
                        {
                            elementId: element.raw
                        })
                    return cs
                },
                buildSelector: async (params) => {
                    // params.tabId                     
                    // params.elementId                        
                    // params.innerTextFirst
                    const element = new UniqueId(params.elementId);
                    const selector = await crx.tabs.requestOnFrameReverse(params.tabId, element.targetFrame, 'buildSelector',
                        {
                            elementId: element.raw,
                            innerTextFirst: params.innerTextFirst,
                            excludeInnerText: params.excludeInnerText,
                            excludeHiddenEle: params.excludeHiddenEle,
                            validNodeAttributes: params.validNodeAttributes,
                            rawData: params.rawData
                        })
                    //添加xpath
                    //const xPath = await crx.tabs.requestOnFrameReverse(params.tabId, element.targetFrame, 'xPath',
                    //    {
                    //        elementId: element.raw
                    //    })
                    ////添加元素全路径
                    //const cs = await crx.tabs.requestOnFrameReverse(params.tabId, element.targetFrame, 'cssSelectorAll',
                    //    {
                    //        elementId: element.raw
                    //    })
                    // 添加窗口级Node                    
                    const tab = await crx.tabs.get(params.tabId)
                    //let path = ""
                    //let outertext = ""
                    //try {
                    //    path = cs.path
                    //} catch (e) {
                    //    path = cs
                    //}
                    //try {
                    //    outertext = cs.outertext
                    //} catch (e) {
                    //    console.log(e)
                    //}

                    selector.unshift({
                        name: "tab",
                        accurate: "true",
                        //xpath: xPath,
                        //cssSelector: path,
                        //outertext: outertext,
                        //fullSelector: "test",
                        props: [{
                            name: "title",
                            value: tab.title,
                            pattern: "equal",
                            accurate: "true"
                        },
                        {
                            name: "url",
                            value: tab.url,
                            pattern: "equal",
                            accurate: "false"
                        }]
                    })
                    return selector
                },
                getTableContent: async (params) => {
                    // params.tabId                     
                    // params.token                     
                    // params.tokenType                     
                    // params.returnType                     
                    // params.parent                    
                    let frameId = 0
                    if (params.parent) {
                        frameId = (new UniqueId(params.parent)).targetFrame
                    }
                    var result = await crx.tabs.requestOnFrame(params.tabId, frameId, 'getTableContent', params)
                    if (result && result.length > 0) {
                        return result
                    }
                    else {
                        return null
                    }
                },
                click: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    const element = new UniqueId(params.elementId);
                    await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'click', params)
                },
                dblclick: async (params) => {
                    // params.tabId                     
                    // params.elementId
                    const element = new UniqueId(params.elementId);
                    await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'dblclick', params)
                },
                rclick: async (params) => {
                    // params.tabId                     
                    // params.elementId
                    const element = new UniqueId(params.elementId);
                    await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'rclick', params)
                },
                isEditable: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'isEditable', params)
                },
                input: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    // params.value                     
                    // params.replace                     
                    const element = new UniqueId(params.elementId);
                    await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'input', params)
                },
                focus: async (params) => {
                    // params.tabId                     
                    // params.elementId
                    const element = new UniqueId(params.elementId);
                    await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'focus', params)
                },
                getTextAll: async (params) => {
                    // params.tabId                     
                    // params.elementIds       
                    const texts = []
                    for (let i = 0; i < params.elementIds.length; ++i) {
                        params.elementId = params.elementIds[i]
                        const text = await handlers.elements.getText(params)
                        texts.push(text)
                    }
                    return texts;
                },
                getText: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getText', params)
                },
                getHtmlAll: async (params) => {
                    // params.tabId                     
                    // params.elementId
                    const htmls = []
                    for (let i = 0; i < params.elementIds.length; ++i) {
                        params.elementId = params.elementIds[i]
                        const html = await handlers.elements.getHtml(params)
                        htmls.push(html)
                    }
                    return htmls;
                },
                getHtml: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getHtml', params)
                },
                getValue: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getValue', params)
                },
                getSelectOption: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    // params.selected: true/false 
                    // params.optionType: text/value 
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getSelectOption', params)
                },
                getSelectOptions: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    // params.selected: true/false 
                    // params.optionType: text/value 
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getSelectOptions', params)
                },
                setSelectOptions: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    // params.options: [0,1]                     
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'setSelectOptions', params)
                },
                getCheckStatus: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getCheckStatus', params)
                },
                setCheckStatus: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    // params.isChecked: true/false                    
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'setCheckStatus', params)
                },
                setAttribute: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    // params.name: '...'                     
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'setAttribute', params)
                },
                getAttributeAll: async (params) => {
                    // params.tabId                     
                    // params.elementId
                    // params.name: '...' 
                    const attrs = []
                    for (let i = 0; i < params.elementIds.length; ++i) {
                        params.elementId = params.elementIds[i]
                        const attr = await handlers.elements.getAttribute_V1(params)
                        attrs.push(params.apiVersion == "v2" ? attr.value : attr);
                    }
                    return attrs;
                },
                getAttribute_V1: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    // params.name: '...'                     
                    const element = new UniqueId(params.elementId);
                    let method = params.apiVersion == "v2" ? 'getAttribute_V2' : 'getAttribute_V1';
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, method, params)
                },
                getAttribute: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    // params.name: '...'                     
                    const element = new UniqueId(params.elementId);
                    let method = params.apiVersion == "v2" ? 'getAttribute_V2' : 'getAttribute';
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, method, params)
                },
                scrollTopOrLeft: async (params) => {
                    // params.tabId                     
                    // params.elementId
                    // params.scrollTop: 100                    
                    // params.scrollLeft: 200                  
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'scrollTopOrLeft', params)
                },
                isDisplayed: async (params) => {
                    // params.tabId                   
                    // params.elementId                    
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'isDisplayed', params)
                },
                getChildren: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    // params.name: '...'                     
                    const element = new UniqueId(params.elementId);
                    var arrayElementId = await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getChildren', params)
                    return arrayElementId;
                },
                getParent: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    // params.name: '...'                     
                    const element = new UniqueId(params.elementId);
                    var arrayElementId = await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getParent', params)
                    return arrayElementId;
                },
                getSiblings: async (params) => {
                    // params.tabId                     
                    // params.elementId                    
                    // params.name: '...'                     
                    const element = new UniqueId(params.elementId);
                    var arrayElementId = await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getSiblings', params)
                    return arrayElementId;
                },
                getElementShot: async (params) => {
                    const element = new UniqueId(params.elementId);
                    let resultVals = [];
                    var elementRect = await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getElementShot', params);
                    resultVals.push(elementRect);
                    const needPageScrollForElement = await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'needPageScrollForElement', params);
                    resultVals.push(needPageScrollForElement);
                    return resultVals;
                },
                getLineChartData: async (params) => {
                    const element = new UniqueId(params.elementId);
                    const frame = element.targetFrame;
                    const targetName = element.raw.split('/').slice(-1)[0];
                    if (targetName.toLowerCase() != 'canvas') {
                        throw new HandlerError(`获取图表数据失败, 选择的目标元素类型不支持`)
                    }
                    else {
                        // 触发采集，返回 resultKey
                        let res = await crx.tabs.requestOnFrame(params.tabId, frame, 'getLineChartData', params);
                        // 如果返回的是 resultKey，循环读取结果
                        if (res && typeof res === 'string' && res.startsWith('__lineChartData_')) {
                            while (true) {
                                await new Promise(r => setTimeout(r, 200));
                                const result = await crx.tabs.requestOnFrame(params.tabId, frame, 'getChartDataResult', { resultKey: res });
                                if (result !== null) {
                                    return result;
                                }
                            }
                        }
                        return res;
                    }
                },
                getPieChartData: async (params) => {
                    const element = new UniqueId(params.elementId);
                    const frame = element.targetFrame;
                    const targetName = element.raw.split('/').slice(-1)[0];
                    if (targetName.toLowerCase() != 'canvas') {
                        throw new HandlerError(`获取图表数据失败, 选择的目标元素类型不支持`)
                    }
                    else {
                        let res = await crx.tabs.requestOnFrame(params.tabId, frame, 'getPieChartData', params);
                        if (res && typeof res === 'string' && res.startsWith('__pieChartData_')) {
                            while (true) {
                                await new Promise(r => setTimeout(r, 200));
                                const result = await crx.tabs.requestOnFrame(params.tabId, frame, 'getChartDataResult', { resultKey: res });
                                if (result !== null) {
                                    return result;
                                }
                            }
                        }
                        return res;
                    }
                },
                getCleanHtml: async (params) => {
                    // params.tabId                     
                    // params.elementId                     
                    const element = new UniqueId(params.elementId);
                    return await crx.tabs.requestOnFrame(params.tabId, element.targetFrame, 'getCleanHtml', params)
                },
            },
            debugger: {
                sendCommand: async (params) => {
                    // params.tabId                     
                    // params.method
                    // params.commandParam
                    const { tabId, method, commandParam, keepAttached } = params
                    try {
                        return await debuggerManager.sendCommand(tabId, method, commandParam, { detach: !keepAttached })
                    } catch (error) {
                        const parseError = tryParseJSON(error.message)
                        if ((parseError || {}).code === -32000) {
                            throw new HandlerError(error.message, ERRORS.NO_RESOURCE)
                        }
                        throw new HandlerError(error.message, error.code)
                    }
                },
                detach: async (params) => {
                    // params.tabId
                    const { tabId } = params
                    try {
                        return await debuggerManager.detach(tabId)
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                },
                check: async (params) => {
                    // params.tabId
                    const { tabId } = params
                    try {
                        return await debuggerManager.check(tabId)
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                },
                addCache: async (params) => {
                    // params.tabId
                    // params.method
                    // params.filter
                    const { tabId, method, filter } = params
                    try {
                        return await debuggerManager.addCache(tabId, method, filter)
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                },
                getCache: async (params) => {
                    // params.tabId
                    // params.method
                    const { tabId, method } = params
                    try {
                        return await debuggerManager.getCache(tabId, method)
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                },
                getCacheBody: async (params) => {
                    // params.tabId
                    // params.requestId
                    const { tabId, requestId } = params
                    try {
                        return await debuggerManager.getCacheBody(tabId, requestId)
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                }
            },
            webRequest: {
                addCache: async (params) => {
                    // params.tabId
                    const { tabId } = params
                    try {
                        return await webRequestManager.addCache(tabId)
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                },
                getCache: async (params) => {
                    // params.tabId
                    const { tabId } = params
                    try {
                        return await webRequestManager.getCache(tabId)
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                },
                startListen: async () => {
                    try {
                        return await webRequestManager.startListen()
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                },
                endListen: async () => {
                    try {
                        return await webRequestManager.endListen()
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                },
                getBody: async (params) => {
                    // params.tabId
                    // params.requestId
                    // params.contentType
                    // params.type
                    const { tabId, requestId, type, contentType } = params
                    try {
                        return await webRequestManager.getBody(tabId, requestId, type, contentType)
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                },
                checkCache: async (params) => {
                    // params.tabId
                    const { tabId } = params
                    try {
                        return webRequestManager.checkCacheAvailable(tabId)
                    } catch (error) {
                        throw new HandlerError(error.message, error.code)
                    }
                }
            },
            initContentScript: async (params) => {
                //code : "console.log('abc')"
                contentScript = params.code
                if (!contentScript || contentScript == 'null') {
                    console.error("content script is empty");
                    throw new HandlerError("content script is empty", ERRORS.NO_RESOURCE)
                }
            },
            initContentCrawlScript: async (params) => {
                //code : "console.log('abc')"
                contentScript += params.code
                window.contentCrawlScript = params.code
            },
        }
        this.codeId = CODE_ID
        this.handle = function (message, callback) {
            // message            
            // {            
            //     method: \"Load\", 
            //     params: {}  
            // }
            try {
                const tokens = message.method.split('.')
                let handler = handlers
                for (const token of tokens) {
                    handler = handler[token]
                }
                handler.call(window, message.params).then((result) => {
                    callback(response.ok(result))
                }).catch((error) => {
                    if (error instanceof HandlerError) {
                        callback(response.fail(error.code, error.message))
                    }
                    else if (error instanceof ContentUnknownError) {
                        callback(response.fail(ERRORS.UNKNOWN, error.message))
                    }
                    else {
                        callback(response.fail(ERRORS.UNKNOWN, error.stack))
                    }
                })
            }
            catch (error) {
                callback(response.fail(ERRORS.UNKNOWN, error.stack))
            }
        }
    }
})()
