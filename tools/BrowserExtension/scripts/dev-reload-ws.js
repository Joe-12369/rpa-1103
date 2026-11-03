// Node ESM
import { WebSocketServer } from 'ws';
import chokidar from 'chokidar';
import path from 'node:path';
import url from 'node:url';

const __dirname = path.dirname(url.fileURLToPath(import.meta.url));
const EXT_DIR = path.resolve(__dirname, '../dist/extensions'); // 根扩展输出目录
const PORT = 5799;

const wss = new WebSocketServer({ port: PORT });
wss.on('connection', () => console.log('[reload] client connected'));

chokidar.watch([
    path.join(EXT_DIR, "**/pages/*.html"),
    path.join(EXT_DIR, "**/scripts/options.js"),
    path.join(EXT_DIR, "**/scripts/popup.js")
], { ignoreInitial: true })
    .on("all", () => {
        for (const c of wss.clients) c.send("page-reload");
        console.log("[reload] page refresh");
    });

console.log(`[reload] WS listening : ws://localhost:${PORT}`);