const CODE_ID = "5087f47c1da6ef46c35e08c9ec1a9e2db4fc24a4"

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


const WRAPPER_API = {
    'debugger': [
        'attach',
        'detach',
        'sendCommand',
        'getTargets'
    ]
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

const wrapperChromeApi = (moduleName, methodName, checkError = true) => {
    let apiRoot = chrome
    if (!apiRoot || !apiRoot[moduleName] || !apiRoot[moduleName][methodName]) {
        return () => Promise.reject(new HandlerError('No permissions.', ERRORS.PERMISSION))
    }
    if (apiRoot) {
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

function iirpa_invoke(method, params) {
    if (typeof window.iirpa === 'undefined') {
        return {
            code: 1,
            message: 'window.iirpa is undefined'
        };
    }
    else {
        try {
            const result = window.iirpa.handle(method, params)
            return result;
        }
        catch (e) {
            console.error('iirpa_invoke error:', e)
            return {
                code: 1,
                error:
                {
                    code: -1, // ERRORS.UNKNOWN
                    message: e.stack
                }
            }
        }
    }

}

async function getTabStatusAsync(tab) {
    if (tab.status === 'complete') {
        const response = await chrome.runtime.sendMessage({
            type: 'GET_TAB_STATUS',
            tabId: tab.id
        });
        if (response && response.status) {
            return response;
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

const crx = Object.assign(
    {
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
                    const tabCreateExeFunc = () => {
                        chrome.tabs.create(createInfo, (tab) => {
                            if (chrome.runtime.lastError) {
                                reject(new Error(chrome.runtime.lastError.message));
                            } else {
                                resolve(tab);
                            }
                        })
                    }
                    //v3版本无痕模式需要额外处理
                    chrome.windows.getAll(null, (windows) => {
                        const incognitoWnd = windows.find(wnd => wnd.incognito)
                        if (incognitoWnd) {
                            createInfo.windowId = incognitoWnd.id
                        }
                        tabCreateExeFunc()
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
                return new Promise(async (resolve, reject) => {
                    const commandParam =
                    {
                        expression: code,
                        includeCommandLineAPI: true,
                        executionWorld: "ISOLATED",
                        returnByValue: true,
                    }
                    // https://chromedevtools.github.io/devtools-protocol/tot/Runtime/#method-evaluate
                    const retValue = await debuggerManager.sendCommand(tabId, 'Runtime.evaluate', commandParam)

                    if (retValue) {
                        console.log("Evaluate result:", retValue)
                        if (retValue.result != undefined) {
                            if (retValue.result.value === undefined) {
                                resolve(null)
                            }
                            else {
                                resolve(retValue.result.value)
                            }
                        }
                        else {
                            if (retValue.exceptionDetails != undefined) {
                                resolve(retValue.exceptionDetails.text);
                            }
                            else {
                                resolve(retValue);
                            }
                        }
                    }
                    else {
                        if (chrome.runtime.lastError) {
                            reject(new CrossOriginError(chrome.runtime.lastError.message))
                        }
                        else {
                            reject(new Error(`unknown error: fail to execute script by debugger on ${tabId}`))
                        }
                    }
                })
            },
            getFrameIndex: async (tabId, frameId) => {
                let count = 0;
                while (count < 10) {
                    const getFrameIndex = crx.tabs.requestOnFrame(tabId, frameId, 'getFrameIndex');
                    let frameIndex = await crx.tabs.requestWait(getFrameIndex, 200);
                    if (frameIndex == -100) {
                        await utils.delay(100);
                        count++;
                        continue;
                    }
                    return frameIndex;
                }
                return -1;
            },
            executeFunctionOnFrame: (tabId, frameId, targetfunc, targetargs, now = false, exeworld = "ISOLATED") => {
                return new Promise((resolve, reject) => {

                    console.log('executeScript exeworld:', exeworld);
                    chrome.scripting.executeScript(
                        {
                            target:
                            {
                                tabId: tabId,
                                frameIds: [frameId]
                                //allFrames:[frameId],
                            },
                            func: targetfunc,
                            args: targetargs,

                            //将脚本注入目标上下文。
                            //默认情况下，脚本将在 document_idle 运行，如果页面已加载，则立即运行。
                            //如果设置了 injectImmediately 属性，脚本将无需等待即可注入，即使页面尚未加载完毕也是如此。
                            //如果脚本的计算结果是一个 promise，则浏览器将等待该 promise 产生结果并返回结果值。
                            //chrome 102 之后才有此参数
                            injectImmediately: now,

                            // "ISOLATED"
                            // 指定隔离环境，即此扩展程序独有的执行环境。

                            // "MAIN"
                            // 指定 DOM 的主域，即与托管网页的 JavaScript 共享的执行环境。
                            world: exeworld,
                        })
                        .then((injectionResults) => {
                            for (const { frameId, result } of injectionResults) {
                                if (chrome.runtime.lastError) {
                                    reject(new CrossOriginError(chrome.runtime.lastError.message))
                                }
                                else {
                                    console.log('executeScript result:', result);
                                    if (result) {
                                        resolve(result)
                                    }
                                    else {
                                        resolve(null)
                                    }
                                }
                            }
                        });
                })
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
                // const code = `handle('${method}', ${JSON.stringify(params)})`
                // const response = await crx.tabs.executeScriptOnFrame(tabId, frameId, code)

                const response = await crx.tabs.executeFunctionOnFrame(tabId, frameId, iirpa_invoke, [method, params]);

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

                //目前采用静态加载本地js的方式、咱不需要init
                if (method == 'init') {
                    //https://stackoverflow.com/questions/70282390/chrome-debugger-commands-doesnt-affect-iframes
                    //debuggerManager.sendCommand(tabId, 'Runtime.enable')
                    console.error('Extension 3.0 does not support init!');
                }

                const response = await crx.tabs.executeFunctionOnFrame(tabId, frameId, iirpa_invoke, [method, params]);

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
                                }
                                else {
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
                    // const error = response.error
                    // throw new HandlerError(error.message, error.code)
                    //need init content                  
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

                const response = await crx.tabs.executeFunctionOnFrame(tabId, frameId, iirpa_invoke, [method, params]);

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

class Debugger {
    constructor(tabId) {
        this.tabId = tabId
        this._ready = undefined
        this._caches = new Map()
        this._executionContexts = new Map();
        console.log("start debugger listener", tabId);

        chrome.debugger.onEvent.addListener((source, method, params) => {
            //if (source.tabId !== this.tabId) return;

            switch (method) {
                case 'Runtime.executionContextCreated':
                    if (params?.context?.id) {
                        this._executionContexts.set(params.context.id, params.context);
                    } else {
                        console.error("Runtime.executionContextCreated: Missing context.id", params);
                    }
                    break;

                case 'Runtime.executionContextDestroyed':
                    if (params?.executionContextId) {
                        this._executionContexts.delete(params.executionContextId);
                    } else {
                        console.error("Runtime.executionContextDestroyed: Missing executionContextId", params);
                    }
                    break;

                case 'Runtime.executionContextsCleared':
                    this._executionContexts.clear();
                    console.log("All execution contexts cleared");
                    break;
            }
        });

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

    async _attach() {
        try {
            try {
                await chrome.debugger.attach({ tabId: this.tabId }, '1.3', null);
                console.log("attach ok", this.tabId);
            }
            catch (e) {
                if (!(e.message && e.message.startsWith('Another debugger is already attached'))) {
                    throw e
                }
            }
            this._sendCommand('Page.enable', {})
            this._sendCommand('Runtime.enable', {})
            this._sendCommand('Network.enable', {})
        } catch (e) {
            this._ready = undefined
            throw e
        }
    }

    _sendCommand(method, commandParam) {
        return chrome.debugger.sendCommand({ tabId: this.tabId }, method, commandParam);
        //return crx.debugger.sendCommand({ tabId: this.tabId }, method, commandParam)
    }

    async attach() {
        if (this._ready) {
            return await this._ready
        }
        this._ready = this._attach()
        return await this._ready
    }

    async detach() {
        if (this._ready) {
            await this._ready
            await chrome.debugger.detach({ tabId: this.tabId })
            this.markDetached()
            // 仅在手动调用detach时清理数据
            this.clear()
        }
    }

    triggerEvent(method, params) {
        this.getCache(method).forEach(c => c.push(params, this.tabId))
    }

    markDetached() {
        this._ready = undefined
    }

    clear() {
        this._caches.clear()
    }

    async sendCommand(method, commandParam) {
        await this.attach()
        return await this._sendCommand(method, commandParam)
    }
    async getExecutionContexts(frameId, name) {
        if (name) {
            let extensionContext;
            if (!frameId) {
                const { frameTree } = await this.sendCommand('Page.getFrameTree');
                const mainFrameId = frameTree.frame.id;
                console.log('use mainFrameId:', mainFrameId);
                extensionContext = [...this._executionContexts.values()].find(ctx => ctx.origin === name
                    && ctx.auxData.type === 'isolated'
                    && ctx.auxData.frameId === mainFrameId);
            }
            else {
                let frames = await crx.webNavigation.getAllFrames(this.tabId)
                const targetFrame = frames.find(item => item.frameId === frameId);
                if (targetFrame) {
                    const { frameTree } = await this.sendCommand('Page.getFrameTree');
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
                    extensionContext = [...this._executionContexts.values()].find(ctx => ctx.origin === name
                        && ctx.auxData.frameId === matchedFrameId);
                } else {
                    console.error("can not find frameId:", frameId)
                }
            }
            if (extensionContext) {
                console.log('find extension context:', extensionContext);
                return [extensionContext]
            } else {
                console.error('can not find extension context');
                return []
            }
        }
        else {
            return Array.from(this._executionContexts.values());
        }
    }

    async getMainContexts(frameId) {
        let context;
        if (!frameId) {
            const { frameTree } = await this.sendCommand('Page.getFrameTree');
            const mainFrameId = frameTree.frame.id;
            console.log('use mainFrameId:', mainFrameId);
            context = [...this._executionContexts.values()].find(ctx => ctx.auxData.type === 'default'
                && ctx.auxData.frameId === mainFrameId);
        }
        else {
            let frames = await crx.webNavigation.getAllFrames(this.tabId)
            const targetFrame = frames.find(item => item.frameId === frameId);
            if (targetFrame) {
                const { frameTree } = await this.sendCommand('Page.getFrameTree');
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
    }

}

class DebuggerManager {
    constructor() {
        this._debuggers = new Map()
    }

    init() {
        this.onEvent()
        this.onDetach()
    }

    onEvent() {
        if (chrome.debugger) {
            chrome.debugger.onEvent.addListener(({ tabId }, method, params) => {
                const debuggee = this.get(tabId)
                if (debuggee) {
                    debuggee.triggerEvent(method, params)
                }
            })
        }
    }

    onDetach() {
        if (chrome.debugger) {
            // 仅标记断开状态
            chrome.debugger.onDetach.addListener(({ tabId }, reason) => {
                const debuggee = this.get(tabId)
                if (debuggee) {
                    switch (reason) {
                        case 'target_closed':
                        case 'canceled_by_user':
                        default:
                            debuggee.markDetached()
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

    addCache(tabId, method, params) {
        return this.get(tabId).addCache(method, params)
    }

    getCache(tabId, method) {
        return this.get(tabId).getCache(method).reduce((all, cache) => all.concat(cache.get()), [])
    }

    getCacheBody(tabId, requestId) {
        const cache = this.get(tabId).getCache('Network.responseReceived')[0]
        return cache ? cache.getCacheBody(requestId) : null
    }

    sendCommand(tabId, method, commandParam) {
        return this.get(tabId).sendCommand(method, commandParam)
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
        return await this.get(tabId).getExecutionContexts(frameId, name);
    }
    async getMainContexts(tabId, frameId) {
        return await this.get(tabId).getMainContexts(frameId);
    }
    async attach(tabId) {
        return await this.get(tabId).attach().catch(console.error);
    }

}


const debuggerManager = new DebuggerManager()
debuggerManager.init()

window.debuggerManager = debuggerManager
window.crx = crx


const ExecuteEvalFunc = function (tabId, frameId, codeString, debuggerManager, runNow = true, domain = "ISOLATED") {
    const EvalResultHandle = function (retValue) {
        console.log("EvalResultHandle result:", retValue)
        if (retValue) {
            if (retValue.result != undefined) {
                if (retValue.result.value === undefined) {
                    return null;
                }
                else {
                    return retValue.result.value;
                }
            }
            else {
                if (retValue.exceptionDetails != undefined) {
                    return retValue.exceptionDetails.text;
                }
                else {
                    return retValue;
                }
            }
        }
        return retValue;
    }

    return new Promise(async (resolve, reject) => {
        const commandParam =
        {
            expression: codeString,
            includeCommandLineAPI: true,
            executionWorld: domain,
            userGesture: true,
            returnByValue: true,
            //executionWorld: "MAIN"
        }
        // https://chromedevtools.github.io/devtools-protocol/tot/Runtime/#method-evaluate
        var evalResult = await debuggerManager.sendCommand(tabId, 'Runtime.evaluate', commandParam);
        console.log("Runtime.evaluate result:", evalResult)
        chrome.scripting.executeScript(
            {
                target:
                {
                    tabId: tabId,
                    frameIds: [frameId]
                },
                func: EvalResultHandle,
                args: [evalResult],
                injectImmediately: runNow,
                world: domain,
            })
            .then((injectionResults) => {
                for (const { frameId, result } of injectionResults) {
                    if (chrome.runtime.lastError) {
                        reject(new CrossOriginError(chrome.runtime.lastError.message))
                    }
                    else {
                        console.log('ExecuteEvalFunc result:', result);
                        resolve(result);
                    }
                }
            });
    })
}


const ExecuteEvalCodeNew = async function (tabId, frameId, codeString, debuggerManager) {
    const wrappedCode = `
        (function() {
            try {
                const result = (function() {
                    ${codeString}
                })();
                console.log("Evaluated result:", result);
                return { success: true, result };
            } catch (err) {
                return { success: false, error: err.message };
            }
        })()
    `;

    const commandParam = {
        expression: wrappedCode,
        includeCommandLineAPI: true,
        userGesture: true,
        returnByValue: true,
    };

    try {
        const evalResult = await debuggerManager.sendCommand(tabId, 'Runtime.evaluate', commandParam);
        console.log("Runtime.evaluate result:", evalResult);
        return evalResult.result?.value ?? { success: false, error: 'No result returned' };
    } catch (err) {
        console.error("Runtime.evaluate error:", err);
        return { success: false, error: err.message };
    }
};

const ExecuteEvalFuncNew = function (tabId, frameId, codeString, runNow = true, domain = "MAIN") {
    return new Promise((resolve, reject) => {
        chrome.scripting.executeScript({
            target: {
                tabId: tabId,
                frameIds: [frameId],
            },
            func: new Function(`
                try {
                    const result = (function() {
                        ${codeString}
                    })();
                    console.log("Executed result:", result);
                    return { success: true, result };
                } catch (err) {
                    return { success: false, error: err.message };
                }
            `),
            injectImmediately: runNow,
            world: domain,
        }, (results) => {
            if (chrome.runtime.lastError) {
                reject(new Error(chrome.runtime.lastError.message));
            } else {
                const result = results?.[0]?.result;
                console.log("ExecuteEvalFunc result:", result);
                resolve(result);
            }
        });
    });
};
const InjectNamedFunction = async function (tabId, frameId, codeString, debuggerManager, executeWorld) {

    await debuggerManager.attach(tabId);
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
        const contexts = await debuggerManager.getExecutionContexts(tabId, frameId, 'chrome-extension://pcopnibgkbdnlaeagepigbboebdfejmb');
        if (contexts.length > 0) {
            const ctx = contexts[0];
            console.log("extension context id:", ctx.id);
            commandParam.contextId = ctx.id;
        } else {
            console.error("can not find extension context:", `${frameId}`);
        }
    } else {
        const maincontexts = await debuggerManager.getMainContexts(tabId, frameId);
        if (maincontexts.length > 0) {
            const ctx = maincontexts[0];
            console.log("main context id:", ctx.id);
            commandParam.contextId = ctx.id;
        } else {
            console.error("can not find main context:", `${frameId}`);
        }
    }
    try {
        const evalResult = await debuggerManager.sendCommand(tabId, 'Runtime.evaluate', commandParam);
        console.log("Function injected successfully:", evalResult);
        return { success: true, functionName };
    } catch (err) {
        console.error("Injection error:", err);
        return { success: false, error: err.message };
    }
};


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
                const wnd = await crx.windows.getLastFocused()
                winId = wnd.id
            }
            await crx.windows.update(winId, { 'state': 'maximized' })
        },
        minimized: async (params) => {
            const wnd = await crx.windows.getLastFocused()
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
            const result = await crx.tabs.create({ "url": params.url })
            if (result.id) {
                await debuggerManager.attach(result.id)
            }
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
            const result = await crx.tabs.executeScriptOnFrame(params.tabId, 0, "window.stop()")
            return result
        },
        getDocument: async (params) => {
            const result = await crx.tabs.executeFunctionOnFrame(params.tabId, 0, function () { return document.documentElement.innerHTML })
            return result
        },
        executeScript: async (params) => {
            const result = await crx.tabs.requestOnFrame(params.tabId, 0, 'executeScript', params)
            return result
        },
        getDetail: async (params) => {
            const tab = await crx.tabs.get(params.tabId)
            if (tab) {
                await debuggerManager.attach(tab.id);
                if (!params.windowId || params.windowId <= 0) {
                    params.windowId = tab.windowId;
                }
                const win = await crx.windows.get(params.windowId)
                if (chrome.runtime.lastError) {
                    throw new HandlerError(`获取tab失败, ${chrome.runtime.lastError.message}`)
                } else {
                    tab["windowdetail"] = win

                    let getResponse = getTabStatusAsync(tab);
                    let response = await crx.tabs.requestWait(getResponse, 500);
                    if (response && response.status) {
                        console.log("load_status:", response, tab.id);
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
                    await debuggerManager.attach(tab.id);
                }
                else if (tab.url
                    && tab.url.indexOf("chrome://") !== 0
                    && (tab.url.indexOf("chrome-extension://") !== 0 || tab.url.indexOf("hehijbfgiekmjfkfjpbkbammjbdenadd") !== -1)
                    && tab.url.indexOf("edge://") !== 0
                    && tab.url.indexOf("se://") !== 0
                    && tab.url.indexOf("about:") !== 0) {
                    tabIds.push(tab.id)
                    await debuggerManager.attach(tab.id);
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
                await debuggerManager.attach(tab.id);
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
            //     tabId = 123                     
            // }                     
            await crx.tabs.remove([params.tabId])
            if (chrome.runtime.lastError) {
                throw new HandlerError(`关闭tab失败, ${chrome.runtime.lastError.message}`)
            }
        },
        closeList: async (params) => {
            // {                     
            //     tabId = 123                     
            // }                     
            await crx.tabs.remove(params.tabIds)
            if (chrome.runtime.lastError) {
                throw new HandlerError(`关闭tab失败, ${chrome.runtime.lastError.message}`)
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
                await crx.tabs.remove(record)
                if (chrome.runtime.lastError) {
                    throw new HandlerError(`关闭tab失败, ${chrome.runtime.lastError.message}`)
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
                await crx.tabs.remove(record)
                if (chrome.runtime.lastError) {
                    throw new HandlerError(`关闭tab失败, ${chrome.runtime.lastError.message}`)
                }
            }
        },
        closeAll: async (params) => {
            const tabs = await crx.tabs.query(params)
            const tabIds = tabs.map(m => m.id)
            await crx.tabs.remove(tabIds)
            if (chrome.runtime.lastError) {
                throw new HandlerError(`关闭tab失败, ${chrome.runtime.lastError.message}`)
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
            var result = await crx.tabs.requestOnFrames(params.tabId, 0, 'querySelector', params)
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
            const origial = utils.clone(params)
            const zoomFactor = await crx.tabs.getZoom(params.tabId)
            const scale = await crx.tabs.executeFunctionOnFrame(params.tabId, 0, function () { return window.devicePixelRatio })
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
            //https://developer.mozilla.org/en-US/docs/Web/API/Document/execCommand
            return await crx.tabs.executeFunctionOnFrame(params.tabId, 0, function () { return document.execCommand(params.command) })
        },
        getText: async (params) => {
            return await crx.tabs.executeFunctionOnFrame(params.tabId, 0, function () { return document.documentElement.outerText })
        },
        getHtml: async (params) => {
            return await crx.tabs.executeFunctionOnFrame(params.tabId, 0, function () { return document.documentElement.outerHTML })
        },
        executeScriptOnFrame: async (params) => {

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
            //params.tabId
            //params.code: 'alert('a')',
            //params.elementId
            // https://developer.chrome.com/extensions/tabs#method-executeScript

            try {
                console.log("===start excutejs===")
                let frameId = 0
                if (params.elementId) {
                    //elementId get frameId
                    frameId = (new UniqueId(params.elementId)).targetFrame;
                }
                let result = null;
                console.log("exeworld from param:", params.world)
                const executeWorld = params.world === "ISOLATED" ? "ISOLATED" : "MAIN";
                console.log(`final exeworld: ${executeWorld}`);

                const injectResult = await InjectNamedFunction(
                    params.tabId,
                    frameId,
                    params.code,
                    debuggerManager,
                    executeWorld
                );

                if (injectResult.functionName) {
                    const fnName = injectResult.functionName;

                    result = await crx.tabs.executeFunctionOnFrame(
                        params.tabId,
                        frameId,
                        (name) => {
                            try {
                                console.log("start execute", window[name]);
                                return window[name]();
                            } catch (err) {
                                console.error("Execution error:", err.message);
                            }
                        },
                        [fnName],
                        true,
                        executeWorld
                    );
                }

                if (result === undefined || result === null) {
                    return null
                }
                else {
                    console.log("executeScript successfully:", result);
                    return JSON.stringify(result)
                }
            }
            finally {
                console.log("===end excutejs===")
            }
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
            const scale = await crx.tabs.executeFunctionOnFrame(params.tabId, 0, function () { return window.devicePixelRatio })
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
        }
    },
    debugger: {
        sendCommand: async (params) => {
            // params.tabId                     
            // params.method
            // params.commandParam
            const { tabId, method, commandParam } = params
            try {
                return await debuggerManager.sendCommand(tabId, method, commandParam)
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
        },
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
        contentScript = params.code;
    },
    initContentCrawlScript: async (params) => {
        //code : "console.log('abc')"
        contentScript += params.code
        window.contentCrawlScript = params.code
    },
}

const messagehandle = function (message, callback) {
    try {
        const tokens = message.method.split('.')
        let handler = handlers
        for (const token of tokens) {
            handler = handler[token]
        }
        handler(message.params).then((result) => {
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



const nativeHost = function () {
    const hostName = "ii.rpa.chromenativemsg";
    const version = "4.0.0.134"
    const clientFlag = "Chrome";
    var traceEnabled = false;
    var rpa_conn_service;
    var rpa_conn_native;

    let retryCount = 0;
    const maxRetries = 10;
    const retryDelay = 1000;

    connectNative();
    var rpa_conn_service = $.hubConnection();

    function connectNative() {
        try {
            rpa_conn_native = null;
            rpa_conn_native = chrome.runtime.connectNative(hostName);
            rpa_conn_native.onDisconnect.addListener(onDisconnected);
            rpa_conn_native.onMessage.addListener(onMessage);
            retryCount = 0;
            console.log("Native host connected");
        } catch (err) {
            console.error("connectNative exception: " + err);
        }
    }

    //log
    function traceLog(message) {
        if (traceEnabled) {
            try {
                console.log(new Date().toLocaleString() + ":" + message);
            } catch (e) { }
        }
    }

    //received
    function received(data) {
        var message = data.SendData;
        if (message) {
            traceLog(message.method);
            switch (message.method) {
                case "getVersion":
                    responseOK(version, data);
                    break;
                case "ping":
                    responseOK('pong', data);
                    break;
                case "init":
                    try {
                        //v3版本暂不注入
                        traceLog("init code");
                        responseOK("init fulfill", data)
                    } catch (error) {
                        responseFail(-1, error.stack, data)
                    }
                    break;
                case "getCodeId":
                    responseOK(value.message.result, data);
                    break;
                case "restore":
                    responseOK("restore", data);
                    break;
                case "popupWorkerPage":
                    popupWorkerPage(data);
                    break;
                case "initContentScript":
                    //evalWithDebugger(message.params.code)
                    messagehandle(message, (response) => {
                        data.CallbackReslut = response;
                        responseMessage(data);
                    })
                    responseMessage(data);
                    break;
                case "initContentCrawlScript":
                    //evalWithDebugger(message.params.code)
                    messagehandle(message, (response) => {
                        data.CallbackReslut = response;
                        responseMessage(data);
                    })
                    responseMessage(data);
                    break;
                default:
                    messagehandle(message, (response) => {
                        data.CallbackReslut = response;

                        responseMessage(data);

                        if (message.method == "tabs.closeAll") {
                            tryingToReconnect = false;
                            rpa_conn_service.disconnectTimeout = 500;
                            rpa_conn_service.stop();
                        }
                    })
                    break;
            }
        }
        else {
            // console.error('nknown message');
            // console.error(data);
        }

    }

    function onDisconnected() {
        const errorMsg = chrome.runtime.lastError?.message || "unknown error";
        console.warn("Disconnected from native host: " + errorMsg);

        if (retryCount < maxRetries) {
            retryCount++;
            console.log(`Retrying connection in ${retryDelay} ms (attempt ${retryCount}/${maxRetries})`);
            setTimeout(connectNative, retryDelay);
        } else {
            console.error("Max retries reached. Stop reconnecting.");
        }
    }
    function onMessage(message) {
        try {
            var data = {};
            if (message.Action && message.Action == "SendTransferMessage") {
                data = message;
                data.connType = "nativemsg_tMsg";
            }
            else {
                data.SendData = message;
                data.connType = "nativemsg";
            }
            received(data);
        }
        catch (e) {
            traceLog("nativemsghost message exception: " + e)
            responseFail(-1000101, "nativemsghost message exception!", data)
        }
    }
    const popupWorkerPage = function (data) {
        chrome.tabs.getCurrent().then(async (tab) => {
            var tabs = await chrome.tabs.query({ windowId: tab.windowId });
            if (tabs.length == 1) {
                responseOK("singlepopup", data);
                return;
            }
            var wnd = await chrome.windows.create({
                tabId: tab.id,
                type: "popup",
                width: 100,
                height: 5,
                focused: true
            })
            await chrome.windows.update(wnd.id, {
                width: 1,
                height: 1,
                left: -1000,
                top: -1000
            })
            responseOK("success", data);
        }).catch((error) => {
            responseFail(ERRORS.UNKNOWN, error, data);
        });
    }


    function responseOK(content, data) {
        data.CallbackReslut = {
            code: 200,
            status: 'OK',
            result: {
                content: content,
                error: null
            }
        };
        responseMessage(data);
    }

    function responseFail(code, message, data) {
        data.CallbackReslut = {
            code: 200,
            status: 'OK',
            result: {
                content: null,
                error: {
                    code: code,
                    message: message
                }
            }
        }
        responseMessage(data);
    }

    function responseMessage(data) {
        console.log(data)
        if (data.connType == "servicehub") {
            data.SendData = "";
            data.MessageType = 2;//cakkback
            transferHubProxy.invoke('SendTransferMessage', JSON.stringify(data));
        }
        else if (data.connType == "nativemsg_tMsg") {
            data.SendData = "";
            data.MessageType = 2;//cakkback
            rpa_conn_native.postMessage(data);
        }
        else {
            rpa_conn_native.postMessage(data.CallbackReslut);
        }
    }

    function InitConnection(count) {
        traceLog('start connecting')
        count = count % 10;
        var servicePort = localStorage.getItem("iirpaSeviceHubPort");
        if (!servicePort) {
            servicePort = 36516 + count;
            count++;
        }
        rpa_conn_service.url = "http://127.0.0.1:" + servicePort + "/rpa";
        rpa_conn_service.qs = { "ClientType": (1 << 2), "ClientFlag": clientFlag };
        transferHubProxy = rpa_conn_service.createHubProxy('TransferHub');
        transferHubProxy.on('newTransferMessage', function (data) {
            try {
                data.connType = "servicehub";
                received(data);
            }
            catch (e) {
                traceLog("servicehub message exception: " + e)
                responseFail(-1000102, "servicehub message exception!", data)
            }
        });
        rpa_conn_service.start().fail(function (msg) {
            console.log(new Date().toLocaleString() + ':server connect fail!');
            traceLog('server connect fail!', msg);
            var initTimeout = setTimeout(function () {
                clearTimeout(initTimeout);
                InitConnection(count);
            }, 1000);
        }).done(function () {
            tryingToReconnect = true;
            console.log(new Date().toLocaleString() + ': server connected!')
            localStorage.setItem("iirpaSeviceHubPort", servicePort);
            rpa_conn_service.disconnectTimeout = 3000;
        })
        rpa_conn_service.disconnected(function () {
            if (tryingToReconnect) {
                var disTimeout = setTimeout(function () {
                    clearTimeout(disTimeout);
                    try {
                        rpa_conn_service.stop();
                    } catch { }
                    rpa_conn_service.start()
                }, 1000)
            }
        });
    }
    InitConnection(0);
}();


