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
