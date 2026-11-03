
chrome.runtime.onInstalled.addListener(function (details) {
    try {
        console.log('Z-RPA Extension: ' + details.reason)

        chrome.tabs.query({}, (tabsList) => {
            for (tab of tabsList) {
                if (tab.url && tab.url.indexOf("chrome://") != 0) {
                    chrome.tabs.reload(tab.id)
                }
            }
        })
    } catch (e) {
        console.error("unable to installed: " + e)
    }
})

const nativeMsgHost = function () {
    /*  https://developers.chrome.com/extensions/nativeMessaging*/
    const clientFlag = "Web360X";
    const hostName = "ii.rpa.chromenativemsg360x";
    const version = "2.7"
    var traceEnabled = false;
    var rpa_conn_service;
    var transferHubProxy;
    var tryingToReconnect = false;
    var rpa_conn_native = chrome.runtime.connectNative(hostName);
    var rpa_conn_service = $.hubConnection();
    window.RPATraceEnabled = traceEnabled;
    rpa_conn_native.onMessage.addListener((message) => {
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
    })
    function traceLog(message) {
        if (traceEnabled || window.RPATraceEnabled) {
            try {
                console.log(new Date().toLocaleString() + ":" + message);
            } catch (e) { }
        }
    }
    function received(data) {
        var message = data.SendData;
        if (message) {
            traceLog(message.method);
            switch (message.method) {
                case "getVersion": responseOK(version, data); break;
                case "ping": responseOK('pong', data); break;
                case "init":
                    try {
                        //注入
                        traceLog("init code");
                        eval.call(window, message.params.code)
                        responseOK("init fulfill", data)
                    } catch (error) {
                        responseFail(-1, error.stack, data)
                    }
                    break;
                case "getCodeId": responseOK(typeof iirpa === 'undefined' ? '' : iirpa.codeId, data); break;
                case "restore":
                    iirpa = null;
                    responseOK("restore", data);
                    break;
                default:
                    if (typeof iirpa == "undefined" || !iirpa.codeId) {
                        responseFail(-100099, "script not injected!", data)
                    }
                    else if (message.codeId && message.codeId != iirpa.codeId) {
                        responseFail(-1000100, "script required injected!", data)
                    }
                    else {
                        delete message.codeId;
                        iirpa.handle(message, (response) => {
                            data.CallbackReslut = response;
                            responseMessage(data);
                            if (message.method == "tabs.closeAll") {
                                tryingToReconnect = false;
                                rpa_conn_service.disconnectTimeout = 500;
                                rpa_conn_service.stop();
                            }
                        })
                    }
                    break;
            }
        }
        else {
            // console.error('nknown message');
            // console.error(data);
        }

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
