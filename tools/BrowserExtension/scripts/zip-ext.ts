import path from "node:path";
import { promises as fsp } from "node:fs";
import fs from "node:fs";
import colors from "picocolors";
// @ts-ignore
import archiver from "archiver";

import { spawn } from "child_process";
import { path7za } from "7zip-bin";

const INDEX_PATH = path.resolve(process.cwd(), "dist", "extensions-index.json");

interface ExtensionItem {
    name: string;
    outputDir: string;
    build: string;
    // ...其他字段忽略...
}

interface IndexFile {
    items: ExtensionItem[];
}

interface CliOptions {
    only?: string[];
}

async function main() {
    const opts = parseArgs(process.argv.slice(2));
    const cwd = process.cwd();

    if (!(await exists(INDEX_PATH))) {
        console.error(colors.red(`[zip] 未找到索引文件: ${path.relative(cwd, INDEX_PATH)}`));
        process.exit(1);
    }

    let indexJson: IndexFile;
    try {
        indexJson = JSON.parse(await fsp.readFile(INDEX_PATH, "utf-8"));
    } catch (e) {
        console.error(colors.red("[zip] 解析 extensions-index.json 失败:"), e);
        process.exit(1);
        return;
    }

    const items = Array.isArray(indexJson?.items) ? indexJson.items : [];
    if (items.length === 0) {
        console.log(colors.yellow("[zip] 索引中没有可打包的扩展 items"));
        return;
    }

    // 可选过滤
    const onlySet = new Set((opts.only || []).map(s => s.trim()).filter(Boolean));
    const targets = onlySet.size > 0 ? items.filter(i => onlySet.has(i.name)) : items;

    if (targets.length === 0) {
        console.log(colors.yellow("[zip] 过滤后没有可打包的扩展"));
        return;
    }

    // 执行打包
    for (const ext of targets) {
        if (!ext?.name || !ext?.outputDir) {
            console.warn(colors.yellow("[zip] 跳过无效项(缺少 name/outputDir):"), ext);
            continue;
        }

        const outDir = path.isAbsolute(ext.outputDir) ? ext.outputDir : path.resolve(cwd, ext.outputDir);
        if (!(await exists(outDir))) {
            console.log(colors.red(`[zip] 输出目录不存在，跳过: ${path.relative(cwd, outDir)}`));
            continue;
        }

        const siblingDir = path.dirname(outDir);
        const zipName = `${ext.name}-${ext.build}.zip`;
        const zipPath = path.join(siblingDir, zipName);

        console.log(colors.cyan(`[zip] 正在打包 ${ext.name}: ${path.relative(cwd, outDir)} -> ${path.relative(cwd, zipPath)}`));
        try {
            await zipDirectoryWith7z(outDir, zipPath);
            console.log(colors.green(`[zip] 完成: ${path.relative(cwd, zipPath)}`));
        } catch (err) {
            console.error(colors.red(`[zip] 打包失败 ${ext.name}:`), err);
        }
    }
}

function parseArgs(argv: string[]): CliOptions {
    const opts: CliOptions = {};
    for (const a of argv) {
        if (a.startsWith("--only=")) {
            opts.only = a
                .slice(7)
                .split(",")
                .map(s => s.trim())
                .filter(Boolean);
        }
    }
    return opts;
}

async function zipDirectory(srcDir: string, outZip: string) {
    await fsp.mkdir(path.dirname(outZip), { recursive: true });

    return new Promise<void>((resolve, reject) => {
        const output = fs.createWriteStream(outZip);
        const archive = archiver("zip", { zlib: { level: 9 } });

        output.on("close", () => resolve());
        output.on("end", () => resolve());
        archive.on("warning", (err: any) => {
            if (err.code === "ENOENT") console.warn("[zip] warning:", err);
            else reject(err);
        });
        archive.on("error", (err: any) => reject(err));

        archive.pipe(output);
        archive.directory(srcDir, false);
        archive.finalize();
    });
}

async function zipDirectoryWith7z(srcDir: string, outZip: string) {
    await fsp.mkdir(path.dirname(outZip), { recursive: true });

    return new Promise<void>((resolve, reject) => {
        const dir = path.dirname(outZip);
        const tmpFile = path.join(dir, `${path.basename(outZip)}.tmp-${Date.now()}.zip`);
        const args = ['a', tmpFile, '*', '-r', '-tzip', '-mx=9'];
        const p = spawn(path7za, args, { cwd: srcDir, windowsHide: true });

        p.stdout.on("data", d => console.log(`[7z] ${d.toString().trim()}`));
        p.stderr.on("data", d => console.warn(`[7z] ${d.toString().trim()}`));
        p.on("error", err => {
            try { if (fs.existsSync(tmpFile)) fs.unlinkSync(tmpFile); } catch { }
            reject(err);
        });
        p.on("close", code => {
            if (code !== 0) {
                try { if (fs.existsSync(tmpFile)) fs.unlinkSync(tmpFile); } catch { }
                return reject(new Error(`7z exit code ${code}`));
            }
            try {
                try {
                    fs.renameSync(tmpFile, outZip);
                } catch (e) {
                    if (fs.existsSync(outZip)) fs.unlinkSync(outZip);
                    fs.renameSync(tmpFile, outZip);
                }
                resolve();
            } catch (e) {
                try { if (fs.existsSync(tmpFile)) fs.unlinkSync(tmpFile); } catch { }
                reject(e);
            }
        });
    });
}

async function exists(p: string) {
    try {
        await fsp.access(p);
        return true;
    } catch {
        return false;
    }
}

main().catch(err => {
    console.error(colors.red("[zip] 失败:"), err);
    process.exit(1);
});
