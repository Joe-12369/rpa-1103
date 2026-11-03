// 此代码在v3 版本无用
function handle(method, params) 
{
    try {
        if (method === 'init') {
            //const result = eval.call(window, params.code)
            return { code: 0, message: 'need init' }
        }
        if(method === 'getCodeId')
        {
            if (typeof window.iirpa === 'undefined') 
            {
                return { code: -2, message: 'need init' }
            }
            else
            {
                result = window.iirpa.codeId;
                return {code: 0, result: result}
            }
        }
        else
        {
            if (typeof window.iirpa === 'undefined') 
            {
                return { code: -2, message: 'need init' }
            }
            else 
            {
                return window.iirpa.handle(method, params)
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


// 监听来自serviceworker的消息
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => 
{
    console.log('onMessage',message,sender);
    var result = handle(message.method,message.params);
    console.log('sendResponse',result);
    sendResponse(result);
    return true;
});
