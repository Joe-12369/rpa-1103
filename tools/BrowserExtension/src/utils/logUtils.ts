//@ts-nocheck

export enum LogLevelFlags {
    DEBUG = 1 << 0, // 1
    INFO = 1 << 1,  // 2
    WARN = 1 << 2,  // 4
    ERROR = 1 << 3, // 8  
}

let currentLevel = LogLevelFlags.INFO | LogLevelFlags.ERROR;

const PREFIXES: Record<keyof typeof LogLevelFlags, string> = {
    DEBUG: '[DEBUG]',
    INFO: '[INFO] ',
    WARN: '[WARN] ',
    ERROR: '[ERR]  ',
};

const STYLES: Record<keyof typeof LogLevelFlags, string> = {
    DEBUG: 'color: gray; font-style: italic;',
    INFO: 'color: deepskyblue;',
    WARN: 'color: orange; font-weight: bold;',
    ERROR: 'color: red; font-weight: bold;',
};

function formatTime(): string {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const seconds = String(now.getSeconds()).padStart(2, '0');
    const ms = String(now.getMilliseconds()).padStart(3, '0');
    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}.${ms}`;
}

function output(levelKey: keyof typeof LogLevelFlags, args: any[]) {
    if (!(currentLevel & LogLevelFlags[levelKey])) return;

    const prefix = PREFIXES[levelKey];
    const style = STYLES[levelKey];
    const timeTag = formatTime();
    const fullPrefix = `${prefix} ${timeTag}`;

    console.info(`%c${fullPrefix}`, style, ...args);
}

export const log = {
    setLevel(flags: number) {
        if (flags === 0) {
            currentLevel = LogLevelFlags.DEBUG | LogLevelFlags.INFO | LogLevelFlags.WARN | LogLevelFlags.ERROR;
        } else {
            currentLevel = flags;
        }

        const prefix = PREFIXES.INFO;
        const style = STYLES.INFO;
        const timeTag = formatTime();
        console.info(`%c${prefix} ${timeTag} [Log] Log level set to ${currentLevel}`, style);
    },
    getLevel() {
        output('Log', 'current log level is', currentLevel);
    },


    debug(...args: any[]) {
        try {
            output('DEBUG', args);
        } catch (e) {
            console.info('%c[ERR]  %s debug logging failed', 'color: red; font-weight: bold;', formatTime(), e);
        }
    },

    info(...args: any[]) {
        try {
            output('INFO', args);
        } catch (e) {
            console.info('%c[ERR]  %s info logging failed', 'color: red; font-weight: bold;', formatTime(), e);
        }
    },

    warn(...args: any[]) {
        try {
            output('WARN', args);
        } catch (e) {
            console.info('%c[ERR]  %s warn logging failed', 'color: red; font-weight: bold;', formatTime(), e);
        }
    },

    error(...args: any[]) {
        try {
            output('ERROR', args);
        } catch (e) {
            console.info('%c[ERR]  %s error logging failed', 'color: red; font-weight: bold;', formatTime(), e);
        }
    }
};