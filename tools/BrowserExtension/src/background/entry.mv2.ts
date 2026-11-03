// src/background/entry.mv2.ts
// @ts-nocheck

import { log } from '@utils/logUtils';
import * as Background from './background'


window.evalCode = async function (code) {
    window.eval.call(window, code);
}

async function initSignalR(count = 0) {
    log.info('start connecting service hub, attempt:', count);
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
            Background.received(data);
        }
        catch (e) {
            log.error("servicehub message exception: " + e)
            responseFail(-1000102, "servicehub message exception!", data)
        }
    });
    rpa_conn_service.start().fail(function (msg) {
        log.error('server connect fail!', msg);
        var initTimeout = setTimeout(function () {
            clearTimeout(initTimeout);
            initSignalR(count);
        }, 1000);
    }).done(function () {
        tryingToReconnect = true;
        log.info('server connected!')
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

Background.legacyBackgroundBootstrap();
await initSignalR(0);