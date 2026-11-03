import fs from 'fs';
import path from 'path';
import JSZip from 'jszip';
import { spawn } from 'child_process';
import { path7za } from '7zip-bin';

type ExtensionConfigItem = {
    name: string;
    outputDir?: string;
    key?: string;
    enabled?: boolean;
    build?: string;
};

const Local_Ext_Folder = path.normalize(
    path.join(
        'C:\\',
        'Program Files',
        //'Z-Factory723',
        'Z-Factory',
        '7.2.0.4',
        'resources',
        'app',
        'resources',
        'support',
        'browserPlugin'
    )
);

const Local_InstallExt_Folder = path.normalize(
    path.join(
        'C:/Workspace/Projects/rpa/ii.rpa/tools/II.RPA.ExtensionInstaller/WebExtensionInstall/bin/WebExtensionInstall.exe.config'
    )
);
const INDEX_PATH = path.resolve(process.cwd(), 'dist', 'extensions-index.json');
const EXT_SUFFIXES = ['.crx'];

const log = (...args: any[]) => console.log(`[test-ext ${new Date().toLocaleTimeString()}]`, ...args);
const warn = (...args: any[]) => console.warn(`[test-ext ${new Date().toLocaleTimeString()}]`, ...args);
const error = (...args: any[]) => console.error(`[test-ext ${new Date().toLocaleTimeString()}]`, ...args);

function readIndex(): ExtensionConfigItem[] {
    if (!fs.existsSync(INDEX_PATH)) return [];
    try {
        const raw = fs.readFileSync(INDEX_PATH, 'utf-8');
        return (JSON.parse(raw).items || []) as ExtensionConfigItem[];
    } catch (e) {
        warn('readIndex: 解析索引失败:', e);
        return [];
    }
}

function findLatestCrx(dir: string, name: string = ''): string | null {
    if (!dir || !fs.existsSync(dir)) return null;
    const list = fs.readdirSync(dir).filter((f) => EXT_SUFFIXES.includes(path.extname(f).toLowerCase()));
    if (!list.length) return null;
    if (name) {
        const target = list.find((f) => f === name || path.parse(f).name === name);
        return target ? path.join(dir, target) : null;
    }
    let latest = '';
    let t = -1;
    for (const f of list) {
        const full = path.join(dir, f);
        const m = fs.statSync(full).mtimeMs;
        if (m > t) {
            t = m;
            latest = full;
        }
    }
    return latest || null;
}

async function extractCrx(crxFile: string, tempRoot: string): Promise<string> {
    const buf = fs.readFileSync(crxFile);
    const sig = Buffer.from([0x50, 0x4b, 0x03, 0x04]);
    const start = buf.indexOf(sig);
    if (start < 0) throw new Error('未找到 Crx 的 ZIP 头');
    const zipBuf = buf.subarray(start);
    const zip = await JSZip.loadAsync(zipBuf);
    const outDir = fs.mkdtempSync(path.join(tempRoot, 'crx_'));
    await Promise.all(
        Object.keys(zip.files).map(async (name) => {
            const file = zip.files[name];
            const dest = path.join(outDir, name);
            if (file.dir) {
                fs.mkdirSync(dest, { recursive: true });
            } else {
                const data = await file.async('nodebuffer');
                fs.mkdirSync(path.dirname(dest), { recursive: true });
                fs.writeFileSync(dest, data);
            }
        })
    );
    return outDir;
}

function injectKey(manifestPath: string, key?: string): boolean {
    if (!key) return false;
    if (!fs.existsSync(manifestPath)) return false;
    let json: any;
    try {
        json = JSON.parse(fs.readFileSync(manifestPath, 'utf-8'));
    } catch {
        return false;
    }
    if (json.key === key) return false;
    json.key = key;
    fs.writeFileSync(manifestPath, JSON.stringify(json, null, 2), 'utf-8');
    return true;
}

function run7z(args: string[], cwd?: string): Promise<void> {
    return new Promise((resolve, reject) => {
        const p = spawn(path7za, args, { cwd, windowsHide: true });
        p.stdout.on('data', (d) => log('[7z]', d.toString().trim()));
        p.stderr.on('data', (d) => warn('[7z]', d.toString().trim()));
        p.on('error', (err) => reject(err));
        p.on('close', (code) => (code === 0 ? resolve() : reject(new Error(`7z exit code ${code}`))));
    });
}

async function archiveDir(srcDir: string, outFile: string): Promise<void> {
    const dir = path.dirname(outFile);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    const tmpFile = path.join(dir, `${path.basename(outFile)}.tmp-${Date.now()}.zip`);
    try {
        await run7z(['a', tmpFile, '*', '-r', '-tzip', '-mx=9'], srcDir);
        try {
            fs.renameSync(tmpFile, outFile);
        } catch {
            if (fs.existsSync(outFile)) fs.unlinkSync(outFile);
            fs.renameSync(tmpFile, outFile);
        }
        log('archiveDir: 完成', outFile);
    } catch (e) {
        try {
            if (fs.existsSync(tmpFile)) fs.unlinkSync(tmpFile);
        } catch { }
        throw e;
    }
}

function replaceConfigValues(configPath: string, keyValues: Record<string, string>): boolean {
    if (!fs.existsSync(configPath)) {
        warn(`replaceConfigValues: 未找到 config 文件: ${configPath}`);
        return false;
    }
    let xml = fs.readFileSync(configPath, 'utf-8');
    const escapeRegExp = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const replaceAddValue = (keyName: string, newValue: string) => {
        const k = escapeRegExp(keyName);
        const reDouble = new RegExp(`(<add\\s+[^>]*key\\s*=\\s*"${k}"[^>]*?value\\s*=\\s*")([^"]*)(")`, 'i');
        const reSingle = new RegExp(`(<add\\s+[^>]*key\\s*=\\s*'${k}'[^>]*?value\\s*=\\s*')([^']*)(')`, 'i');
        if (reDouble.test(xml)) {
            xml = xml.replace(reDouble, (_m, p1, _old, p3) => `${p1}${newValue}${p3}`);
            return true;
        } else if (reSingle.test(xml)) {
            xml = xml.replace(reSingle, (_m, p1, _old, p3) => `${p1}${newValue}${p3}`);
            return true;
        } else {
            return false;
        }
    };

    for (const k of Object.keys(keyValues)) {
        const ok = replaceAddValue(k, keyValues[k]);
        if (!ok) warn(`replaceConfigValues: 未找到 ${k} 节点进行替换`);
    }

    try {
        fs.writeFileSync(configPath, xml, 'utf-8');
        fs.writeFileSync(Local_InstallExt_Folder, xml, 'utf-8');
        return true;
    } catch (e) {
        warn('replaceConfigValues: 写入 config 文件失败:', e);
        return false;
    }
}

function applyFilesToLocal(extFolder: string, files: { name: string; src: string }[]) {
    const targetDir = path.join(Local_Ext_Folder, extFolder);
    fs.mkdirSync(targetDir, { recursive: true });
    for (const f of files) {
        if (!fs.existsSync(f.src)) {
            warn(`applyFilesToLocal: 源文件不存在: ${f.src}`);
            continue;
        }
        const dest = path.join(targetDir, f.name);
        try {
            fs.copyFileSync(f.src, dest);
            log(`applyFilesToLocal: 复制到本地 -> ${dest}`);
        } catch (e) {
            error('applyFilesToLocal: 复制失败:', e);
        }
    }
}

function buildPrefBase64(extFolder: string, manifest: any): string {
    const candidates = [
        path.join(process.cwd(), 'private', 'preferences', `${extFolder}.json`),
        path.join(process.cwd(), 'private', 'preferences', `${extFolder}-preferences.json`),
        path.join(process.cwd(), 'private', 'preferences', `${extFolder}-preferences.json`)
    ];
    let prefs: any = null;
    for (const p of candidates) {
        if (fs.existsSync(p)) {
            try {
                prefs = JSON.parse(fs.readFileSync(p, 'utf-8'));
                break;
            } catch (e) {
                warn('buildPrefBase64: 解析 preferences 失败:', e);
            }
        }
    }
    if (!prefs) {
        prefs = {};
        if (manifest) {
            if (manifest.manifest && typeof manifest.manifest === 'object') {
                prefs.manifest = { version: manifest.version, version_name: manifest.version_name ?? manifest.versionName ?? '' };
            } else {
                prefs.version = manifest.version ?? '';
                prefs.version_name = manifest.version_name ?? manifest.versionName ?? '';
            }
        }
    }
    return Buffer.from(JSON.stringify(prefs), 'utf-8').toString('base64');
}

async function run(): Promise<void> {
    log('开始执行 test-ext (本地替换模式)');
    const args = process.argv.slice(2);
    const onlyArg = args.find((a) => a.startsWith('--only=')) ?? '';
    const onlySet = new Set(
        onlyArg ? onlyArg.replace('--only=', '').split(',').map((s) => s.trim()).filter(Boolean) : []
    );

    const items = readIndex().filter((i) => i && i.enabled !== false);
    const targets = onlySet.size > 0 ? items.filter((i) => onlySet.has(i.name)) : items;
    if (!targets.length) {
        warn('没有要处理的扩展，退出');
        return;
    }

    const tempRoot = path.join(process.cwd(), 'temp_testext');
    if (fs.existsSync(tempRoot)) {
        try {
            fs.rmSync(tempRoot, { recursive: true, force: true });
        } catch (e) {
            warn('删除旧 temp 失败:', e);
        }
    }
    fs.mkdirSync(tempRoot, { recursive: true });

    try {
        for (const item of targets) {
            log(`处理: ${item.name}`);
            const outDir = item.outputDir ? (path.isAbsolute(item.outputDir) ? item.outputDir : path.resolve(process.cwd(), item.outputDir)) : '';
            let crx = findLatestCrx(outDir, '');
            if (!crx) {
                const altDir = path.join(process.cwd(), path.basename(item.outputDir ?? item.name));
                crx = findLatestCrx(altDir, '');
            }
            if (!crx) {
                warn(`未找到CRX: ${item.name}`);
                continue;
            }
            log('找到 CRX:', crx);

            let extracted: string;
            try {
                extracted = await extractCrx(crx, tempRoot);
            } catch (e) {
                error('CRX 解析失败:', e);
                continue;
            }

            const manifestPath = path.join(extracted, 'manifest.json');
            const manifest = fs.existsSync(manifestPath) ? JSON.parse(fs.readFileSync(manifestPath, 'utf-8')) : null;
            if (injectKey(manifestPath, item.key)) log('已注入 key 到 manifest.json');

            const zipOut = path.join(tempRoot, `${item.name}.zip`);
            try {
                await archiveDir(extracted, zipOut);
            } catch (e) {
                error('扩展压缩失败:', e);
                continue;
            }

            const folderName = path.basename(item.outputDir ?? item.name);
            const filesToPut = [
                { name: `${item.name}${path.extname(crx)}`, src: crx },
                { name: `${item.name}.zip`, src: zipOut }
            ];

            applyFilesToLocal(folderName, filesToPut);

            const prefBase64 = buildPrefBase64(folderName, manifest);
            const configPath = path.join(Local_Ext_Folder, 'WebExtensionInstall.exe.config');
            const version = manifest?.version ?? '';
            const version_name = manifest?.version_name ?? manifest?.versionName ?? '';
            replaceConfigValues(configPath, {
                'Chrome.Version': version,
                'Chrome.Version_Name': version_name,
                'Chrome.Pf': prefBase64
            });

            log(`完成: ${item.name}`);
        }
    } finally {
        try {
            if (fs.existsSync(tempRoot)) fs.rmSync(tempRoot, { recursive: true, force: true });
        } catch (e) {
            warn('清理 temp 失败:', e);
        }
    }

    log('本地替换完成');
}

run().catch((e) => {
    error('执行失败:', e);
    process.exit(1);
});