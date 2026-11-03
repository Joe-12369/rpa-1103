import fs from 'fs';
import path from 'path';
import JSZip from 'jszip';
import { spawn } from 'child_process';
import { path7za } from '7zip-bin';
import { ExtensionConfigItem } from '@type/extensionsConfig';

const Ext_Folder = path.resolve(process.cwd(), 'dist', 'extensions');
const INDEX_PATH = path.resolve(process.cwd(), "dist", "extensions-index.json");
const SHARE_DIR = '\\\\10.4.2.144\\tools\\RPA-dependencies\\csharp\\browserPlugin';
const EXT_SUFFIXES = ['.crx'];

const log = (...args: any[]) => console.log(`[upload-ext ${new Date().toLocaleTimeString()}]`, ...args);
const warn = (...args: any[]) => console.warn(`[upload-ext ${new Date().toLocaleTimeString()}]`, ...args);
const error = (...args: any[]) => console.error(`[upload-ext ${new Date().toLocaleTimeString()}]`, ...args);


const zipLocks = new Map<string, Promise<void>>();
function enqueueZipTask(zipPath: string, task: () => Promise<void>): Promise<void> {
    const prev = zipLocks.get(zipPath) ?? Promise.resolve();
    const next = prev.then(() => task()).finally(() => {
        if (zipLocks.get(zipPath) === next) zipLocks.delete(zipPath);
    });
    zipLocks.set(zipPath, next);
    return next;
}

function readIndex(): ExtensionConfigItem[] {
    const raw = fs.readFileSync(INDEX_PATH, 'utf-8');
    return (JSON.parse(raw).items || []) as ExtensionConfigItem[];
}

function findLatestCrx(dir: string, name: string = ""): string | null {
    if (!fs.existsSync(dir)) return null;
    const list = fs.readdirSync(dir).filter(f => EXT_SUFFIXES.includes(path.extname(f).toLowerCase()));
    if (!list.length) return null;
    if (name) {
        const target = list.find(f => f === name || path.parse(f).name === name);
        return target ? path.join(dir, target) : null;
    }
    let latest = '';
    let t = -1;
    for (const f of list) {
        const full = path.join(dir, f);
        const m = fs.statSync(full).mtimeMs;
        if (m > t) { t = m; latest = full; }
    }
    return latest;
}

async function extractCrx(crxFile: string, tempRoot: string): Promise<string> {
    const buf = fs.readFileSync(crxFile);
    const sig = Buffer.from([0x50, 0x4B, 0x03, 0x04]);
    const start = buf.indexOf(sig);
    if (start < 0) throw new Error('未找到 Crx 的 ZIP头');
    const zipBuf = buf.subarray(start);
    const zip = await JSZip.loadAsync(zipBuf);
    const outDir = fs.mkdtempSync(path.join(tempRoot, 'crx_'));
    await Promise.all(Object.keys(zip.files).map(async name => {
        const file = zip.files[name];
        const dest = path.join(outDir, name);
        if (file.dir) {
            fs.mkdirSync(dest, { recursive: true });
        } else {
            const data = await file.async('nodebuffer');
            fs.mkdirSync(path.dirname(dest), { recursive: true });
            fs.writeFileSync(dest, data);
        }
    }));
    return outDir;
}

function injectKey(manifestPath: string, key?: string): boolean {
    if (!key) return false;
    if (!fs.existsSync(manifestPath)) return false;
    let json: any;
    try { json = JSON.parse(fs.readFileSync(manifestPath, 'utf-8')); } catch { return false; }
    if (json.key === key) return false;
    json.key = key;
    fs.writeFileSync(manifestPath, JSON.stringify(json, null, 2), 'utf-8');
    return true;
}

function run7z(args: string[], cwd?: string): Promise<void> {
    return new Promise((resolve, reject) => {
        const p = spawn(path7za, args, { cwd, windowsHide: true });
        p.stdout.on('data', d => log('[7z]', d.toString().trim()));
        p.stderr.on('data', d => warn('[7z]', d.toString().trim()));
        p.on('error', err => reject(err));
        p.on('close', code => code === 0 ? resolve() : reject(new Error(`7z exit code ${code}`)));
    });
}

async function archiveDir(srcDir: string, outFile: string): Promise<void> {
    log(`[archiveDir] 压缩: ${srcDir} -> ${outFile}`);
    const dir = path.dirname(outFile);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    const tmpFile = path.join(dir, `${path.basename(outFile)}.tmp-${Date.now()}.zip`);
    try {
        await run7z(['a', tmpFile, '*', '-r', '-tzip', '-mx=9'], srcDir);
        try {
            fs.renameSync(tmpFile, outFile);
        } catch (e) {
            log('[archiveDir] 压缩 异常:', e);
            // 尝试覆盖替换
            if (fs.existsSync(outFile)) fs.unlinkSync(outFile);
            fs.renameSync(tmpFile, outFile);
        }
    } catch (e) {
        // 清理临时
        try { if (fs.existsSync(tmpFile)) fs.unlinkSync(tmpFile); } catch { }
        throw e;
    }
}

function rmDirSafe(d: string) {
    if (!fs.existsSync(d)) return;
    for (const n of fs.readdirSync(d)) {
        const full = path.join(d, n);
        const s = fs.statSync(full);
        if (s.isDirectory()) rmDirSafe(full); else fs.unlinkSync(full);
    }
    try { fs.rmdirSync(d); } catch { }
}



function updatePreferences(staging: string, extFolder: string) {
    try {
        if (extFolder !== 'RPAChromeExtension') return;


        const extDir = path.join(Ext_Folder, extFolder);
        if (!fs.existsSync(extDir)) { warn(`updatePreferences: 未找到扩展目录 ${extDir}`); return; }

        const manifestPath = path.join(extDir, 'manifest.json');
        if (!fs.existsSync(manifestPath)) { warn(`updatePreferences: 未找到 manifest.json: ${manifestPath}`); return; }

        let manifest: any;
        try { manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf-8')); }
        catch (e) { warn('updatePreferences: 解析 manifest.json 失败:', e); return; }

        const version = manifest.version ?? '';
        const version_name = manifest.version_name ?? manifest.versionName ?? '';


        const prefPath = path.join(process.cwd(), 'private', 'preferences', `${extFolder}.json`);
        if (!fs.existsSync(prefPath)) { warn(`updatePreferences: 未找到 preferences 文件: ${prefPath}`); return; }

        let prefs: any;

        try { prefs = JSON.parse(fs.readFileSync(prefPath, 'utf-8')); } catch (e) { warn('updatePreferences: 解析 preferences 文件失败:', e); return; }
        if (prefs.manifest && typeof prefs.manifest === 'object') {
            prefs.manifest.version = version;
            prefs.manifest.version_name = version_name;
        } else {
            prefs.version = version;
            prefs.version_name = version_name;
        }
        if (prefs.service_worker_registration_info && typeof prefs.service_worker_registration_info === 'object') {
            prefs.service_worker_registration_info.version = version;
        }

        prefs.path = `pcopnibgkbdnlaeagepigbboebdfejmb\\${version}_0`;

        // 更新 preferences
        try { fs.writeFileSync(prefPath, JSON.stringify(prefs, null, 2), 'utf-8'); } catch (e) { warn('updatePreferences: 写入 preferences 文件失败:', e); }

        // 转为 base64 字符串
        const prefBase64 = Buffer.from(JSON.stringify(prefs).replace(/_INDEED_/g, ''), 'utf-8').toString('base64');

        // 替换 staging 中 WebExtensionInstall.exe.config 的 Chrome.* 节点的 value 属性
        const configPath = path.join(staging, 'WebExtensionInstall.exe.config');
        if (!fs.existsSync(configPath)) { warn(`updatePreferences: 未找到 config 文件: ${configPath}`); return; }

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

        const replacedVersion = replaceAddValue('Chrome.Version', version);
        const replacedVersionName = replaceAddValue('Chrome.Version_Name', version_name);
        const replacedPf = replaceAddValue('Chrome.Pf', prefBase64);

        if (!replacedVersion) warn('updatePreferences: 未找到 Chrome.Version 节点进行替换');
        if (!replacedVersionName) warn('updatePreferences: 未找到 Chrome.Version_Name 节点进行替换');
        if (!replacedPf) warn('updatePreferences: 未找到 Chrome.Pf 节点进行替换');

        try {
            fs.writeFileSync(configPath, xml, 'utf-8');
            //const pfPath = path.join(staging, extFolder, 'pf.bin');         
            //console.log('write pf to', pfPath);
            // fs.writeFileSync(pfPath, JSON.stringify(prefs).replace(/_INDEED_/g, ''), 'utf-8');
        }
        catch (e) { warn('updatePreferences: 写入 config 文件失败:', e); }

        log('updatePreferences: 已更新', { manifestPath, prefPath, configPath });
    } catch (e) {
        error('updatePreferences 执行失败:', e);
    }
}

function updateTarget(zipPath: string, extFolder: string, files: { name: string; src: string }[], needRel: boolean = false): Promise<void> {
    return enqueueZipTask(zipPath, async () => {
        const tmp = fs.mkdtempSync(path.join(process.cwd(), 'temp', 'zipwrk_'));
        const staging = path.join(tmp, 'staging');
        fs.mkdirSync(staging, { recursive: true });

        try {
            if (fs.existsSync(zipPath)) {
                log(`【upload】 解压现有 zip 到暂存: ${zipPath}`);
                await run7z(['x', zipPath, `-o${staging}`, '-y']);
            } else {
                log(`【upload】 目标 zip 不存在，将新建: ${zipPath}`);
            }

            const targetDir = path.join(staging, extFolder);
            updatePreferences(staging, extFolder);
            for (const f of files) {
                if (!fs.existsSync(f.src)) {
                    error(`【upload-7z】 源文件不存在: ${f.src}`);
                }
                const dest = path.join(targetDir, f.name);
                fs.copyFileSync(f.src, dest);
                log(`【upload】 复制新文件 -> ${dest}`);
            }
            log(`【upload】 上传 qa plugin -> ${zipPath}`);
            await archiveDir(staging, zipPath);
            if (needRel) {
                const relZipPath = zipPath.replace('qa', 'release');
                fs.copyFileSync(zipPath, relZipPath);
                log(`【upload】 上传 release plugin -> ${relZipPath}`);
            }
            log(`【upload】 上传完成: ${zipPath}`);
        } finally {
            rmDirSafe(tmp);
            log(`【upload】 清理完成: ${tmp}`);
        }
    });
}

async function run() {
    log('开始执行 upload-ext');
    let [versionFolder, flag] = process.argv.slice(2);

    if (!versionFolder) {
        log('未传入版本文件夹，从共享目录选择以 "-" 结尾的文件夹');
        const dirList = fs.readdirSync(SHARE_DIR).filter(dir => dir.endsWith('-'));
        versionFolder = dirList.length > 0 ? dirList[0] : '';
        log('自动选择版本文件夹:', versionFolder || '(未找到)');
    }

    if (!['qa', 'release', 'all'].includes(flag || '')) {
        log('未传入或无效的 flag，默认使用 qa');
        flag = 'qa';
    }

    if (!versionFolder || !['qa', 'release', 'all'].includes(flag || '')) {
        error('用法: node scripts/upload-ext-7zip.ts <版本文件夹> <qa|release|all>');
        process.exit(1);
    }

    const versionPath = path.join(SHARE_DIR, versionFolder);
    if (!fs.existsSync(versionPath)) fs.mkdirSync(versionPath, { recursive: true });
    log('上传目标目录:', versionPath);

    const qaZip = path.join(versionPath, 'browserPlugin-qa.zip');
    const relZip = path.join(versionPath, 'browserPlugin-release.zip');
    const needQa = flag === 'qa' || flag === 'all';
    const needRel = flag === 'release' || flag === 'all';

    const items = readIndex().filter(i => i.enabled !== false);
    log('读取扩展索引文件，扩展项数量:', items.length);
    if (!items.length) {
        warn('无扩展上传，退出');
        return;
    }

    const tempRoot = path.join(process.cwd(), 'temp');
    if (fs.existsSync(tempRoot)) {
        try { fs.rmSync(tempRoot, { recursive: true, force: true }); } catch (e) { warn('删除旧 temp 失败:', e); }
    }
    fs.mkdirSync(tempRoot, { recursive: true });

    const tasks: Promise<void>[] = [];

    for (const item of items) {
        log(`—— 处理扩展: ${item.name} ——`);
        let crx = findLatestCrx(item.outputDir!);
        let useLocalExt = false;
        if (!crx) {
            crx = findLatestCrx(path.basename(item.outputDir!));
            useLocalExt = true;
        }
        if (!crx) { warn(`未找到CRX: ${item.name}`); continue; }
        log(`找到 CRX: ${crx}`);

        let extracted: string;
        try {
            extracted = await extractCrx(crx, tempRoot);
            log(`CRX 解压到: ${extracted}`);
        } catch (e) {
            error(`CRX 解析失败: ${(e as Error).message}`);
            continue;
        }

        const manifestPath = path.join(extracted, 'manifest.json');
        const injected = injectKey(manifestPath, item.key);
        if (injected)
            log(`已注入 key 到 manifest.json`);

        const zipOut = path.join(tempRoot, `${item.name}.zip`);
        try {
            await archiveDir(extracted, zipOut);
            log(`扩展压缩完成: ${zipOut}`);
        } catch (e) {
            error('扩展压缩失败:', e);
            continue;
        }

        const folderName = path.basename(item.outputDir!);
        const filesToPut = [
            { name: `${item.name}${path.extname(crx)}`, src: crx },
            { name: `${item.name}.zip`, src: zipOut }

        ];
        // 如果使用本地扩展文件，则不上传 zip,目前只有Chrome 需要解压一个zip 安装文件
        if (useLocalExt) {
            log(`使用本地扩展文件,不上传zip 包含的扩展文件`);
            filesToPut.splice(1, 1);
        }

        if (needQa) tasks.push(updateTarget(qaZip, folderName, filesToPut, needRel));
        //if (needRel) tasks.push(updateTarget(relZip, folderName, filesToPut));

        log(`处理完成: ${item.name}`);
    }

    log('等待所有任务完成...');
    await Promise.all(tasks);
    log('所有任务完成');

    // 清理 temp
    try { rmDirSafe(tempRoot); } catch (e) { warn('清理 temp 失败:', e); }
}

run().catch(e => {
    error('执行失败:', e);
    process.exit(1);
});