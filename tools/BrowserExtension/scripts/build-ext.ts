// TypeScript
// scripts/build.ts

import path from "node:path";
import { promises as fs } from "node:fs";
import colors from "picocolors";
import { build as viteBuild, InlineConfig } from "vite";
import { NormalizedAddinItem } from "@type/addins";

import { loadExtensionsConfig, updateExtensionVersions, type ExtensionBuildTarget } from "@utils/loadExtensionsConfig";
import { loadManifestModule, writeManifestJson } from "@utils/loadManifest";

const SCRIPT_DIR = path.resolve(process.cwd(), "dist", "scripts");
const ExtensionConfigPath = path.resolve(process.cwd(), "src\\config\\extensions.config.json");

function createAliasMap(cwd: string): Record<string, string> {
    const r = (...p: string[]) => path.resolve(cwd, ...p);
    return {
        '@runtime': r('src/runtime'),
        '@config': r('src/config'),
        '@addins': r('src/addins'),
        '@manifest': r('src/manifest'),
        '@utils': r('src/utils'),
        '@assets': r('src/assets'),
        '@background': r('src/background'),
        '@content': r('src/content'),
        '@type': r('src/types'),
        '@pages': r('src/pages'),
        '@scripts': r('scripts'),
        '@': r('src'),
    };
}

/* -------------------- CLI 解析 -------------------- */

interface CliOptions {
    only?: string[];
    enabled?: boolean;
    phase: "build" | "publish";
    withAddins: "inline" | "off";
    minify: boolean;
    sourcemap: boolean;
    clean: boolean;
    noPages: boolean;
    key: boolean;// 是否在 在manifest里写入key
}


async function main() {
    const opts = parseArgs(process.argv.slice(2));
    //opts.minify = false; // 测试阶段， 强制不压缩
    const cwd = process.cwd();

    const { items: extensions } = await loadExtensionsConfig(ExtensionConfigPath, {
        cwd: cwd,
        only: opts.only,
        enabled: opts.enabled
    });

    if (extensions.length === 0) {
        console.log(colors.yellow("[ext] 没有可构建的扩展（检查 enabled 或 --only）"));
        return;
    }

    let versionMap: Record<string, string> = {};
    for (const ext of extensions) {
        console.log(colors.cyan(`\n[ext] 构建: ${ext.name} (phase=${opts.phase}, addins=${opts.withAddins})`));

        if (opts.clean) {
            console.log(colors.gray(`[ext] 清理输出目录: ${path.relative(cwd, ext.outputDir)}`));
            await removeIfExists(ext.outputDir);
        }
        await fs.mkdir(ext.outputDir, { recursive: true });

        // 1) 选择内联 addins（按扩展名 + target 过滤）|| opts.phase === "publish";
        const buildPhase = opts.phase === "build"
        const addinsPack =
            buildPhase && opts.withAddins === "inline"
                ? await prepareAddinsInlineForExtension(cwd, ext.name)
                : { content: [], background: [], libs: [] } //await emitEmptyAddinsModule(cwd);

        //  加载与修改 manifest
        const { manifest } = await loadManifestModule<any>(ext.manifest);
        const isMV3 = String(manifest.manifest_version) === "3";
        const useSwModule =
            isMV3 &&
            manifest.background &&
            manifest.background.type === 'module';

        // 拷贝内联 addins 产物到 scripts/ 并准备 manifest 引用路径
        let inlineAdded = { contentList: [] as string[], backgroundList: [] as string[] };
        if (buildPhase && opts.withAddins === "inline") {
            inlineAdded = await copyAddinsToExtAndReturnPaths(
                isMV3,
                ext.outputDir,
                addinsPack.content,
                addinsPack.background
            );
        }

        // MV3 开发/发布阶段都复制 必须的 addins 到扩展 scripts/
        if (isMV3) {
            const mv3Added = await copyAddinsToExtAndReturnPaths(
                false,
                ext.outputDir,
                [],
                [],

            );
            for (const p of mv3Added.backgroundList || []) inlineAdded.backgroundList.push(p);
            for (const p of mv3Added.contentList || []) inlineAdded.contentList.push(p);
        }

        updateManifestWithInlineAddins(
            manifest,
            isMV3,
            inlineAdded.contentList,
            addinsPack.background,
            useSwModule
        );

        if (isMV3 && manifest.background?.scripts) {
            delete manifest.background.scripts;
        }

        // const virtualModulePath = addinsPack.modulePath;
        //const virtualModulePathPosix = virtualModulePath.replace(/\\/g, '/');

        // 4) Vite 构建
        const viteConfig = createViteConfig({
            cwd,
            outDir: ext.outputDir,
            isMV3,
            minify: opts.minify,
            sourcemap: opts.sourcemap,
            virtualAddinsModule: "",//addinsPack.modulePath,         
            noPages: opts.noPages,
            inlineAdded: { ...inlineAdded, libsList: addinsPack.libs }
        });

        // (viteConfig.resolve as any).dedupe = [
        //     ...(((viteConfig.resolve as any).dedupe) || []),
        //     "react", "react-dom", "antd", "@ant-design/cssinjs"
        // ];

        //修改构建号       
        const [date, times] = generateBuildNum();
        const versionParts = ext.currentVersion.split('.');
        versionParts[versionParts.length - 1] = parseInt(versionParts[versionParts.length - 1]) + 1 + ''; // 构建号 +1
        manifest.version = versionParts.join('.');
        versionMap[ext.name] = manifest.version;
        manifest.version_name = `${manifest.version}+${ext.build}-${date}.${times}`;

        viteConfig.define = {
            ...(viteConfig.define || {}),
            __VERSION__: JSON.stringify(manifest.version),
            __NATIVE_HOST_NAME__: JSON.stringify(ext.nativeHostName),
            __CLIENT_FLAG__: JSON.stringify(ext.clientFlag),
            //__VIRTUAL_ADDINS_MODULE__: JSON.stringify(virtualModulePath)
        };

        // === 单独构建 MV3 背景 SW 为单文件 IIFE ===
        if (false) {
            const bgConfig: InlineConfig = {
                root: cwd,
                build: {
                    outDir: ext.outputDir,
                    emptyOutDir: false,
                    sourcemap: opts.sourcemap,
                    minify: opts.minify,
                    rollupOptions: {
                        plugins: [
                        ],
                        input: {
                            background: path.resolve(cwd, "src/background/entry.mv3.ts")
                        },
                        output: {
                            format: "iife",
                            inlineDynamicImports: true,
                            entryFileNames: "service_worker.js"
                        }
                    }
                },
                resolve: {
                    alias: [
                        ...(((viteConfig.resolve as any)?.alias) || []),
                        //{ find: '__VIRTUAL_ADDINS_MODULE__', replacement: virtualModulePathPosix }
                    ]
                },
                define: {
                    ...(viteConfig.define || {})
                }
            };
            await viteBuild(bgConfig);

            // 从主构建移除 SW 入口，避免重复构建覆盖单文件产物
            try {
                const inputs = (viteConfig.build!.rollupOptions!.input as any);
                if (inputs) {
                    if (inputs.service_worker) delete inputs.service_worker; // MV3 入口键是 service_worker
                    if (inputs.background) delete inputs.background;         // 兜底，防止历史命名
                }
                // 调试确认
                console.log('[ext] main inputs after prune:', Object.keys(inputs || {}));
            } catch { }
        }

        await viteBuild(viteConfig);

        // 后处理: 将 background addins 注入到 service_worker.js（文件级拼接，避免污染 Rollup source map）
        if (isMV3 && inlineAdded.backgroundList.length > 0) {
            await injectBackgroundScriptsToServiceWorker(ext.outputDir, inlineAdded.backgroundList, addinsPack.libs);
        }

        //build 版本
        if (opts.phase === "build") {
            manifest.version_name = `${manifest.version}+develop-${date}.${times}`;
            if (ext.key) {
                manifest.key = ext.key;
            }
        }
        //npm run epk 发布版本
        if (opts.key) {
            if (ext.key)
                manifest.key = ext.key;
        }

        // 写 manifest.json
        await writeManifestJson(path.join(ext.outputDir, "manifest.json"), manifest);

        //更新 extensionConfig.json 里的 currentVersion
        ext.currentVersion = manifest.version;


        // 复制资源
        //await copyIfExists(path.join(cwd, "src", "assets"), path.join(ext.outputDir, "assets"));
        //await copyIfExists(path.join(cwd, "src", "_locales"), path.join(ext.outputDir, "_locales"));

        fs.mkdir(path.join(ext.outputDir, "pages"), { recursive: true });
        await fs.copyFile(path.join(cwd, "pages/options.html"), path.join(ext.outputDir, "pages/options.html"));
        await fs.copyFile(path.join(cwd, "pages/popup.html"), path.join(ext.outputDir, "pages/popup.html"));
        await copyIfExists(path.join(cwd, "pages/assets"), path.join(ext.outputDir, "assets"));
        await copyOffscreenIfNeed(cwd, ext.outputDir, isMV3);

        // 生成 pages HTML
        if (!opts.noPages)
            await emitPageTemplates(cwd, ext.outputDir);

        console.log(colors.green(`[ext] 完成: ${ext.name} -> ${path.relative(cwd, ext.outputDir)}`));
    }

    console.log(colors.green("\n[ext] 全部扩展构建完成"));

    //更新插件版本号
    updateExtensionVersions(versionMap, ExtensionConfigPath);

    // 输出 extensions.config.json 到 dist
    try {
        const outDir = path.join(cwd, "dist");
        await fs.mkdir(outDir, { recursive: true });
        const outPath = path.join(outDir, "extensions-index.json");
        await fs.writeFile(outPath, JSON.stringify({ items: extensions }, null, 2), "utf-8");
        console.log(colors.blueBright(`[ext] 输出扩展索引文件: ${path.relative(cwd, outPath)}`));
    } catch (e) {
        console.warn(colors.redBright("[ext] 输出 extensions.config.json 失败:"), e);
    }
}

function parseArgs(argv: string[]): CliOptions {
    const isProd = process.env.NODE_ENV === "production" || argv.includes("--prod");
    const opts: CliOptions = {
        phase: isProd ? "publish" : "build",
        withAddins: isProd ? "off" : "inline", // 开发默认 inline
        minify: isProd,
        sourcemap: !isProd,
        clean: false,
        noPages: false,
        key: false
    };
    for (const a of argv) {
        if (a.startsWith("--only=")) {
            opts.only = a
                .slice(7)
                .split(",")
                .map(s => s.trim())
                .filter(Boolean);
        } else if (a.startsWith("--phase=")) {
            const v = a.slice(8);
            if (v === "build" || v === "publish") opts.phase = v;
        } else if (a.startsWith("--with-addins=")) {
            const v = a.slice("--with-addins=".length);
            if (v === "inline" || v === "off") opts.withAddins = v;
        } else if (a === "--minify") {
            opts.minify = true;
        } else if (a === "--enable") {
            opts.enabled = true;
        } else if (a === "--no-minify") {
            opts.minify = false;
        } else if (a === "--sourcemap") {
            opts.sourcemap = true;
        } else if (a === "--no-sourcemap") {
            opts.sourcemap = false;
        } else if (a === "--clean") {
            opts.clean = true;
        } else if (a === "--no-clean") {
            opts.clean = false;
        } else if (a === "--no-pages")
            opts.noPages = true;
        else if (a === "--key")
            opts.key = true;
    }

    if (opts.phase === "publish") {
        if (opts.withAddins === "inline") {
            console.log(colors.yellow("[ext] publish 阶段禁止 inline，已强制 off"));
            opts.withAddins = "off";
        }
        if (opts.sourcemap) {
            console.log(colors.yellow("[ext] publish 阶段关闭 sourcemap"));
            opts.sourcemap = false;
        }
    }
    return opts;
}

/* -------------------- Vite 配置 -------------------- */

function createViteConfig(p: {
    cwd: string;
    outDir: string;
    isMV3: boolean;
    minify: boolean;
    sourcemap: boolean;
    virtualAddinsModule: string;
    noPages: boolean;
    inlineAdded: { contentList: string[]; backgroundList: string[], libsList?: string[] };
}): InlineConfig {
    // 入口（按 MV 版本切换）
    const contentEntry = p.isMV3
        ? path.join(p.cwd, "src", "content", "content.ts")
        : path.join(p.cwd, "src", "content", "content.ts");

    const bgKey = p.isMV3 ? "service_worker" : "background";
    const bgEntry = path.join(
        p.cwd,
        "src",
        "background",
        p.isMV3 ? "entry.mv3.ts" : "entry.mv2.ts"
    );

    const entries: Record<string, string> = {
        "content": contentEntry,
        [bgKey]: bgEntry
    };

    // if (!p.noPages) {
    //     entries["scripts/options"] = path.join(p.cwd, "src", "pages", "options", "main.tsx");
    //     entries["scripts/popup"] = path.join(p.cwd, "src", "pages", "popup", "main.tsx");
    // }

    return {
        configFile: false,
        root: p.cwd,
        logLevel: "info",
        resolve: {
            alias: createAliasMap(p.cwd)
        },
        // optimizeDeps: {
        //     include: ["react", "react-dom", "antd"],
        //     esbuildOptions: { target: "es2022" }
        // },
        build: {
            outDir: p.outDir,
            emptyOutDir: false,
            copyPublicDir: true,
            sourcemap: p.sourcemap,
            minify: p.minify ? 'esbuild' : false,
            modulePreload: false,
            cssCodeSplit: false,
            rollupOptions: {
                input: entries,
                treeshake: false,
                onwarn(warning, defaultHandler) {
                    if (warning.code === "MODULE_LEVEL_DIRECTIVE") return;
                    if (warning.code === "SOURCEMAP_ERROR") return;
                    defaultHandler(warning);
                },
                plugins: [
                    {
                        name: 'build-dce',
                        transform(code, id) {
                            if (!p.minify) {
                                return null; // build 阶段不执行
                            }
                            if (id.includes('node_modules')) return code;

                            // 移除 if (false) { ... }
                            code = code.replace(/if\s*\(\s*false\s*\)\s*\{[^{}]*\}/g, '');

                            return code;
                        }
                    },
                    {
                        name: "strip-dep-sourcemap",
                        renderChunk(code, chunk) {
                            const modIds = Object.keys(chunk.modules || {});
                            if (
                                modIds.length > 0 &&
                                modIds.every(id => /node_modules[\\/]/.test(id))
                            ) {
                                return { code, map: null };
                            }
                            return null;
                        }
                    }
                ],
                output: {
                    manualChunks(id) {
                        if (!id.includes("node_modules")) return;
                        if (/node_modules[\\/](react|react-dom|react-is|scheduler)\b/.test(id)) return "react-core";
                        if (id.includes("/antd/")) return "vendor-antd";
                        return "vendor";
                    },
                    entryFileNames: chunk => {
                        if (chunk.name === "content") return "content.js";
                        if (chunk.name === "background") return "background.js";
                        if (chunk.name === "service_worker") return "service_worker.js";
                        return "[name].js";
                    },
                    chunkFileNames: "chunks/[name]-[hash].js",
                    assetFileNames: "assets/[name][extname]"
                }
            }
        },
        esbuild: {
            minifyIdentifiers: true,
            minifySyntax: true,
            minifyWhitespace: true,
            legalComments: 'none',
            keepNames: false,
            treeShaking: false
        },
        define: {
            __MV__: JSON.stringify(p.isMV3 ? "mv3" : "mv2"),
            __BUILD_PHASE__: process.env.NODE_ENV === "production" ? false : true,
            "process.env.NODE_ENV": JSON.stringify(process.env.NODE_ENV || "development")
        }
    };
}

/* -------------------- Addins 内联处理 -------------------- */

async function prepareAddinsInlineForExtension(cwd: string, extName: string) {
    const indexPath = path.join(cwd, "dist", "addins-index.json");
    if (!(await exists(indexPath))) return emitEmptyAddinsModule(cwd);

    let list: NormalizedAddinItem[] = [];
    try {
        list = JSON.parse(await fs.readFile(indexPath, "utf-8"));
        if (!Array.isArray(list)) return emitEmptyAddinsModule(cwd);
    } catch {
        return emitEmptyAddinsModule(cwd);
    }

    const filterNames = (t: "content" | "background") =>
        list
            .filter(
                i =>
                    i.embed &&
                    i.type === t &&
                    (!i.target_browser || i.target_browser.length === 0 || i.target_browser.includes(extName))
            )
            .map(i => `addins/${i.outputFileName}`);

    const content = filterNames("content");
    const background = filterNames("background");
    const libs = list.filter(i => i.sourceCategory as string === "libs").map(i => `${i.outputFileName}`);

    return { content, background, libs };

    if (content.length === 0 && background.length === 0) {
        return emitEmptyAddinsModule(cwd);
    }

    const tmpDir = path.join(cwd, ".temp");
    await fs.mkdir(tmpDir, { recursive: true });
    const file = path.join(tmpDir, `virtual-addins.${extName}.ts`);
    const code = `
/* auto-generated addins (extension=${extName}) */
export const CONTENT_ADDINS = ${JSON.stringify(content)};
export const BACKGROUND_ADDINS = ${JSON.stringify(background)};
function runtimeUrl(p){try{return (typeof chrome!=='undefined'&&chrome.runtime?.getURL)?chrome.runtime.getURL(p):p;}catch{return p;}}
function injectScript(url){
  return new Promise((res,rej)=>{
    try{
      const s=document.createElement('script');
      s.src=url;
      s.onload=()=>{s.remove();res();};
      s.onerror=e=>{s.remove();rej(e);};
      (document.head||document.documentElement).appendChild(s);
    }catch(e){rej(e);}
  });
}
export async function loadContentAddins(){
  for(const p of CONTENT_ADDINS){
    try{await injectScript(runtimeUrl(p));}catch(e){console.warn('[content-addins] fail',p,e);}
  }
}
export function loadBackgroundAddins(){
  if(typeof importScripts==='function'){
    for(const p of BACKGROUND_ADDINS){
      try{importScripts(runtimeUrl(p));}catch(e){console.warn('[background-addins] fail',p,e);}
    }
  }
}
export default { CONTENT_ADDINS, BACKGROUND_ADDINS, loadContentAddins, loadBackgroundAddins };
`;
    await fs.writeFile(file, code, "utf-8");
    return { content, background, libs, modulePath: file };
}

async function emitEmptyAddinsModule(cwd: string) {
    const tmpDir = path.join(cwd, ".temp");
    await fs.mkdir(tmpDir, { recursive: true });
    const file = path.join(tmpDir, "virtual-addins.empty.ts");
    await fs.writeFile(
        file,
        `export const CONTENT_ADDINS:string[]=[];export const BACKGROUND_ADDINS:string[]=[];export function loadContentAddins(){};export function loadBackgroundAddins(){};export default {CONTENT_ADDINS,BACKGROUND_ADDINS,loadContentAddins,loadBackgroundAddins};`,
        "utf-8"
    );
    return { content: [], background: [], libs: [], modulePath: file };
}


function updateManifestWithInlineAddins(
    manifest: any,
    isMV3: boolean,
    addedContent: string[],
    addedBackground: string[],
    useSwModule: boolean = false
) {
    const norm = (p: string) => p.replace(/^\.\/?/, "./"); // 统一 ./ 开头
    const ensureContentEntry = () => {
        if (!Array.isArray(manifest.content_scripts) || manifest.content_scripts.length === 0) {
            manifest.content_scripts = [
                { matches: ["<all_urls>"], js: [], run_at: "document_start" }
            ];
        }
        if (!Array.isArray(manifest.content_scripts[0].js)) {
            manifest.content_scripts[0].js = [];
        }
    };
    // content
    if (addedContent.length > 0) {
        ensureContentEntry();
        const jsArr = manifest.content_scripts[0].js;
        const existSet = new Set(jsArr.map((x: string) => norm(x)));
        for (const p of addedContent.map(norm)) {
            if (!existSet.has(p)) {
                jsArr.push(p);
                existSet.add(p);
            }
        }
    }
    // background
    if (!isMV3 && addedBackground.length) {
        manifest.background = manifest.background || {};
        manifest.background.scripts = manifest.background.scripts || [];
        const norm = (p: string) => p.replace(/^\.\/+/, "");
        for (const f of addedBackground) {
            const n = norm(f);
            if (!manifest.background.scripts.some((x: string) => norm(x) === n)) {
                manifest.background.scripts.push(n);
            }
        }
    }


}

async function copyAddinsToExtAndReturnPaths(
    isMV3: boolean,
    outDir: string,
    contentList: string[],
    backgroundList: string[]
) {
    const ensureDir = path.join(outDir, "scripts");
    await fs.mkdir(ensureDir, { recursive: true });

    const copiedContent: string[] = [];
    for (const rel of contentList) {
        const fileName = rel.replace(/^addins\//, "");
        const srcFile = path.join(SCRIPT_DIR, fileName);
        if (!(await exists(srcFile))) {
            console.warn("[ext] 内联 content addin 缺失:", fileName);
            continue;
        }
        await fs.copyFile(srcFile, path.join(ensureDir, fileName));
        copiedContent.push(`./scripts/${fileName}`);
    }

    const copiedBackground: string[] = [];
    for (const rel of backgroundList) {
        const fileName = rel.replace(/^addins\//, "");
        const srcFile = path.join(SCRIPT_DIR, fileName);
        if (!(await exists(srcFile))) {
            console.warn("[ext] 依赖 background 缺失:", fileName);
            continue;
        }
        if (!isMV3)
            await fs.copyFile(srcFile, path.join(ensureDir, fileName));
        copiedBackground.push(srcFile);
    }

    return { contentList: copiedContent, backgroundList: copiedBackground };
}

/* -------------------- 处理pages-------------------- */
async function emitPageTemplates(cwd: string, outDir: string) {
    const pages = [
        {
            src: path.join(cwd, "src", "pages", "options", "index.html"),
            out: path.join(outDir, "pages", "options.html"),
            js: "../scripts/options.js"
        },
        {
            src: path.join(cwd, "src", "pages", "popup", "index.html"),
            out: path.join(outDir, "pages", "popup.html"),
            js: "../scripts/popup.js"
        }
    ];
    for (const p of pages) {
        if (!(await exists(p.src))) {
            console.warn("[ext] 缺少模板:", p.src);
            continue;
        }
        let html = await fs.readFile(p.src, "utf-8");
        html = html.replace(
            /<script[^>]*src=["'][^"']*main\.(t|j)sx?["'][^>]*><\/script>/i,
            `<script type="module" src="${p.js}"></script>`
        );
        await fs.mkdir(path.dirname(p.out), { recursive: true });
        await fs.writeFile(p.out, html, "utf-8");
    }
}

/* -------------------- 工具函数 -------------------- */

async function copyIfExists(src: string, dest: string) {
    if (!(await exists(src))) return;
    await copyDir(src, dest);
}
async function copyOffscreenIfNeed(cwd: string, outDir: string, isMV3: boolean) {
    if (!isMV3) return;

    // 1. 拷贝 offscreen.html
    const offscreenSrc = path.join(cwd, "src/pages/signalr/offscreen.html");
    if (await exists(offscreenSrc)) {
        const offscreenTarget = path.join(outDir, "pages", "offscreen.html");
        await fs.mkdir(path.dirname(offscreenTarget), { recursive: true });
        await fs.copyFile(offscreenSrc, offscreenTarget);
        console.log("[ext] 拷贝 offscreen.html ->", path.relative(outDir, offscreenTarget));
    } else {
        console.warn("[ext] 未找到 offscreen.html", offscreenSrc);
    }

    // 2. 拷贝 offscreen 依赖脚本   
    const targets = ["jquery.js", "rpa_server.js", "rpa_client.js"];
    for (const name of targets) {
        const src = path.join(SCRIPT_DIR, name);
        if (await exists(src)) {
            const dest = path.join(outDir, "scripts", name);
            await fs.mkdir(path.dirname(dest), { recursive: true });
            await fs.copyFile(src, dest);
            console.log("[ext] 拷贝", name, "->", path.relative(outDir, dest));
        } else {
            console.warn("[ext] 未找到", name, src);
        }
    }
}
async function copyDir(src: string, dest: string) {
    await fs.mkdir(dest, { recursive: true });
    const entries = await fs.readdir(src, { withFileTypes: true });
    for (const e of entries) {
        const s = path.join(src, e.name);
        const d = path.join(dest, e.name);
        if (e.isDirectory()) await copyDir(s, d);
        else if (e.isFile()) await fs.copyFile(s, d);
    }
}

async function removeIfExists(p: string) {
    try {
        await fs.rm(p, { recursive: true, force: true });
    } catch { }
}

async function exists(p: string) {
    try {
        await fs.access(p);
        return true;
    } catch {
        return false;
    }
}


/**
 * 将 background addins 以文件级拼接方式注入 service_worker.js，
 * 并移除 source map 引用，避免调试器使用因代码拼接而失效的 source map。
 */
async function injectBackgroundScriptsToServiceWorker(
    outDir: string,
    backgroundList: string[],
    libsList?: string[]
) {
    const swPath = path.join(outDir, "service_worker.js");
    const mapPath = swPath + ".map";

    if (!(await exists(swPath))) {
        console.warn("[ext] service_worker.js 不存在，跳过注入");
        return;
    }

    let combinedScripts = "";
    for (const scriptPath of backgroundList) {
        const fileName = path.basename(scriptPath);
        if (libsList?.includes(fileName)) continue;

        if (!(await exists(scriptPath))) {
            console.warn(`[ext] 背景脚本缺失，跳过: ${fileName}`);
            continue;
        }
        const scriptContent = await fs.readFile(scriptPath, "utf-8");

        combinedScripts +=
            `\n// --- Injected: ${fileName} ---\n` +
            scriptContent +
            `\n// --- End Injected: ${fileName} ---\n`;
    }

    if (!combinedScripts.trim()) return;

    let swContent = await fs.readFile(swPath, "utf-8");
    // 去掉 source map 引用（拼接后的代码已无法与原始 source map 对齐）
    swContent = swContent.replace(/\n\/\/# sourceMappingURL=.*$/m, "");
    swContent += "\n" + combinedScripts;

    await fs.writeFile(swPath, swContent, "utf-8");

    // 删除失效的 .map 文件
    if (await exists(mapPath)) {
        await fs.rm(mapPath);
        console.log("[ext] 已移除失效的 service_worker.js.map");
    }

    console.log(`[ext] 已将背景脚本注入 service_worker.js（文件级拼接）`);
}
function generateBuildNum() {
    const now = new Date();
    const year = now.getFullYear().toString().slice(-2);
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const time = `${now.getHours()}${now.getMinutes()}`;
    return [`${year}${month}${day}`, time];
}

main().catch(err => {
    console.error("[ext] 构建失败:", err);
    process.exit(1);
});
