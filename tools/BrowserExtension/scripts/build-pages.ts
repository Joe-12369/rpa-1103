// scripts/build-pages.ts
import { build as viteBuild, InlineConfig, PluginOption } from 'vite';
import path from 'path';
import fs from 'fs';
import { spawn } from 'child_process';
import os from 'os';
import { WebSocketServer } from 'ws';
import chokidar from 'chokidar';

const WATCH_EXT_NAME = 'RPAChromeExtension';
const WATCH_ADDRESS = `chrome-extension://pcopnibgkbdnlaeagepigbboebdfejmb/pages/options.html`;


function injectLiveReloadPlugin(): PluginOption {
    return {
        name: 'inject-reload',
        transformIndexHtml(html) {
            const liveReloadScript = `
  <script type="module">
  try {
    const ws = new WebSocket('ws://localhost:5174');
    ws.onmessage = (event) => {
      if (event.data === 'reload') {
        console.log('[live-reload] reloading...');
        location.reload();
      }
    };
  } catch (e) {
    console.warn('Live reload connection failed');
  }
  </script>`;
            return html.replace('</body>', `${liveReloadScript}</body>`);
        },
    } as PluginOption;
}

const renameHtmlPlugin = () => ({
    name: 'rename-html',
    generateBundle(_options: any, bundle: any) {
        const renameMap: Record<string, string> = {
            'src/pages/options/index.html': 'options.html',
            'src/pages/popup/index.html': 'popup.html',
        };

        for (const fileName in bundle) {
            const file = bundle[fileName];
            if (file.fileName?.endsWith('.html')) {
                const targetName = renameMap[fileName];
                if (targetName) {
                    bundle[targetName] = {
                        ...file,
                        fileName: targetName,
                    };
                    delete bundle[fileName];
                }
            }
        }
    },
});

const copyOnWritePlugin = (srcDir: string, destDir: string) => ({
    name: 'copy-on-write',
    async writeBundle() {
        console.log(`📤 Copying build output from ${srcDir} to ${destDir}`);
        await copyDirRecursive(srcDir, destDir);
        console.log(`✅ Copy completed.`);
    },
});

async function copyDirRecursive(src: string, dest: string) {
    const fsp = fs.promises;
    try {
        const stat = await fsp.stat(src).catch(() => null);
        if (!stat || !stat.isDirectory()) return;
        await fsp.mkdir(dest, { recursive: true });
        const entries = await fsp.readdir(src, { withFileTypes: true });
        for (const entry of entries) {
            const srcPath = path.join(src, entry.name);
            const destPath = path.join(dest, entry.name);
            if (entry.isDirectory()) {
                await copyDirRecursive(srcPath, destPath);
            } else {
                await fsp.copyFile(srcPath, destPath);
            }
        }
    } catch (err) {
        console.warn(`⚠️ copyDirRecursive failed: ${String(err)}`);
    }
}

async function buildPages(mode: 'build' | 'publish' | 'watch') {
    const cwd = process.cwd();
    const isPublish = mode === 'publish';
    const isWatch = mode === 'watch';

    const outputRoot = isWatch
        ? path.join(cwd, 'dist', 'extensions', WATCH_EXT_NAME, 'pages')
        : path.join(cwd, 'pages');

    const pagesConfig: InlineConfig = {
        root: cwd,
        // plugins: [...(isWatch ? [injectLiveReloadPlugin()] : [])],
        build: {
            outDir: outputRoot,
            emptyOutDir: !isWatch,
            copyPublicDir: false,
            watch: isWatch ? {} : null,
            sourcemap: !isPublish,
            minify: isPublish ? 'esbuild' : false,
            chunkSizeWarningLimit: 1000,
            rollupOptions: {
                input: {
                    options: path.resolve(cwd, 'src/pages/options/index.html'),
                    popup: path.resolve(cwd, 'src/pages/popup/index.html'),
                },
                output: {
                    format: 'es',
                    entryFileNames: `assets/[name]-[hash].js`,
                    chunkFileNames: `assets/[name]-[hash].js`,
                    assetFileNames: (assetInfo) => {
                        const ext = path.extname(assetInfo.name ?? '');
                        const base = 'assets';
                        if (ext === '.css') return `${base}/css/[name]-[hash][extname]`;
                        return `${base}/[name]-[hash][extname]`;
                    },
                },
                plugins: [renameHtmlPlugin(),
                ...(isWatch ? [copyOnWritePlugin(outputRoot, path.join(cwd, 'dist', 'extensions', WATCH_EXT_NAME))] : []),
                ],
            },
        },
    };

    console.log(`🚀 Building pages in ${mode} mode...`);
    await viteBuild(pagesConfig);
    console.log(`✅ Pages build complete in ${mode} mode.`);

    if (isWatch) {
        console.log(`👀 Watching for changes...`);
        console.log(`🔗 Debug address: ${WATCH_ADDRESS}`);

        openChrome(WATCH_ADDRESS);

        // // 启动 WebSocket 服务器
        // const wss = new WebSocketServer({ port: 5174 });
        // wss.on('connection', (ws) => console.log('🔌 Client connected to live reload'));

        // // 监控输出文件变化
        // const watcher = chokidar.watch(outputRoot + "\\assets", { ignoreInitial: true });
        // watcher.on('all', () => {
        //     console.log('♻️  Files updated, notifying clients...');
        //     wss.clients.forEach((ws) => ws.send('reload'));
        // });
    }
}

function openChrome(url: string) {
    const platform = os.platform();
    let command: string;
    let args: string[];

    if (platform === 'win32') {
        command = 'cmd';
        args = ['/c', 'start', 'chrome', '--app=' + url];
    } else if (platform === 'darwin') {
        command = 'open';
        args = ['-a', 'Google Chrome', url];
    } else {
        command = 'google-chrome';
        args = ['--app=' + url];
    }

    try {
        spawn(command, args, { detached: true, stdio: 'ignore' }).unref();
        console.log(`🌐 Chrome opened: ${url}`);
    } catch (err) {
        console.warn(`⚠️ Failed to auto-open Chrome, please open manually:\n${url}`);
    }
}

const modeArg = process.argv[2];
let mode: 'build' | 'publish' | 'watch' = 'build';
if (modeArg === 'publish') mode = 'publish';
else if (modeArg === 'watch') mode = 'watch';

buildPages(mode).catch((err) => {
    console.error(`❌ Failed to build pages in ${mode} mode:`, err);
    process.exit(1);
});
