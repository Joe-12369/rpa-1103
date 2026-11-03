// TypeScript
// scripts/build-addins.ts
import path from "node:path";
import colors from "picocolors";
import { promises as fs } from "node:fs";
import { build as esbuild, transform } from "esbuild";
import { scanAddins } from "@/utils/scanAddins";
import * as Addins from "@/types/addins";
import { OBFUSCATE_PRESETS, ObfuscateLevel } from "@/config/obfuscatePresets";
import JavaScriptObfuscator from 'javascript-obfuscator';
import { build as viteBuild, InlineConfig } from 'vite';


interface CliOptions {
    phase: "build" | "publish";
    config: string;
    clean: boolean;
    verbose: boolean;
}

async function main() {
    const opts = parseArgs(process.argv.slice(2));
    const cwd = process.cwd();

    const { items, warnings, outputDir } = await scanAddins(opts.config, cwd);

    if (warnings.length) {
        console.warn("[addins] 警告：\n" + warnings.map(w => " - " + w).join("\n"));
    }

    await fs.mkdir(outputDir, { recursive: true });
    if (opts.clean) {
        await cleanOutput(outputDir);
    }

    const builtMeta: Addins.NormalizedAddinItem[] = [];
    for (const item of items) {
        const phaseCfg = opts.phase === "build" ? item.build : item.publish;

        console.log(`[addins] 开始(${opts.phase}): ${item.name} (${item.kind})---${item.target}`);

        try {
            if (item.isLegacy) {
                await buildLegacy(item, phaseCfg, opts, outputDir);
            } else {
                await buildModule(item, phaseCfg, opts, outputDir);
            }
            builtMeta.push({
                name: item.name ?? "",
                description: item.description ?? "",
                type: item.type ?? "",
                kind: item.kind ?? "",
                outputFileName: item.name + ".js",
                enabled: item.enabled === false ? false : true,
                entry: item.entry || "",
                isLegacy: item.isLegacy,
                minify: phaseCfg.minify,
                obfuscate: phaseCfg.obfuscate || false,
                sourcemap: phaseCfg.sourcemap,
                target: item.target || "",
                target_browser: item.target_browser || [],
                embed: phaseCfg.embed,
                sourceCategory: item.sourceCategory,
                warnings: warnings || []

            });
            console.log(colors.greenBright(`[addins] ✓ 完成: ${item.name}`));
        } catch (e) {
            console.error(colors.redBright(`[addins] ✗ 失败: ${item.name} -> ${(e as Error).message}`));
        }
    }

    await writeIndexJson(outputDir, builtMeta);
    console.log(colors.blueBright(`[addins] 输出脚本索引文件: ${path.relative(cwd, path.join(outputDir, "addins-index.json"))}`));
}

function parseArgs(argv: string[]): CliOptions {
    const opts: CliOptions = {
        phase: "build",
        config: "src/config/addins.json",
        clean: false,
        verbose: false
    };
    for (const a of argv) {
        if (a.startsWith("--phase=")) {
            const p = a.slice(8);
            if (p === "build" || p === "publish") opts.phase = p;
        } else if (a.startsWith("--config=")) {
            opts.config = a.slice("--config=".length);
        } else if (a === "--clean") {
            opts.clean = true;
        } else if (a === "--verbose") {
            opts.verbose = true;
        }
    }
    return opts;
}

async function cleanOutput(dir: string) {
    const dirExists = await exists(dir);
    if (!dirExists) return;
    const ents = await fs.readdir(dir);
    await Promise.all(ents.map(e => fs.rm(path.join(dir, e), { recursive: true, force: true })));
}

async function buildLegacy(
    item: any,
    phaseCfg: any,
    opts: CliOptions,
    outputDir: string
) {
    let code = await fs.readFile(item.sourceEntry, "utf-8");

    // esbuild minify（纯 JS，不做 bundle）
    if (phaseCfg.minify) {
        const r = await transform(code, {
            loader: "js",
            minify: true,
            sourcemap: phaseCfg.sourcemap,
            target: item.target,
            // drop: ['debugger'],
            pure: ['console.log', 'console.debug']
        });
        code = r.code;
    }

    // obfuscate
    if (phaseCfg.obfuscate) {
        const key = phaseCfg.obfuscate as ObfuscateLevel;
        const preset = key in OBFUSCATE_PRESETS
            ? OBFUSCATE_PRESETS[key]
            : OBFUSCATE_PRESETS.medium;

        code = JavaScriptObfuscator.obfuscate(code, {
            ...preset,
            sourceMap: false
        }).getObfuscatedCode();
    }

    await fs.writeFile(path.resolve(outputDir, item.outputFileName), code, "utf-8");
}


async function buildModule(
    item: any,
    phaseCfg: any,
    opts: CliOptions,
    outputDir: string
) {
    const sourcemap = phaseCfg.sourcemap;
    const _outputFile = path.resolve(outputDir, item.outputFileName);

    const viteConfig: InlineConfig = {
        build: {
            outDir: outputDir,
            emptyOutDir: false,
            copyPublicDir: false,
            sourcemap,
            minify: phaseCfg.minify ? 'esbuild' : false,
            lib: {
                entry: item.sourceEntry,
                formats: ['iife'],
                name: path.basename(item.outputFileName, '.js'),
                fileName: () => path.basename(item.outputFileName),
            },
            target: item.target,
        },
        esbuild: {
            pure: phaseCfg.minify ? ['console.log', 'console.debug'] : []
        },
        logLevel: opts.verbose ? 'info' : 'silent',
    };

    await viteBuild(viteConfig);

    if (phaseCfg.obfuscate) {
        const bundled = await fs.readFile(_outputFile, 'utf-8');
        const key = phaseCfg.obfuscate as ObfuscateLevel;
        const preset = key in OBFUSCATE_PRESETS
            ? OBFUSCATE_PRESETS[key]
            : OBFUSCATE_PRESETS.medium;

        const obfuscatedCode = JavaScriptObfuscator.obfuscate(bundled, {
            ...preset,
            sourceMap: false,
        }).getObfuscatedCode();

        await fs.writeFile(_outputFile, obfuscatedCode, 'utf-8');

        if (sourcemap) {
            const mapFile = _outputFile + '.map';
            if (await exists(mapFile)) {
                await fs.rm(mapFile);
            }
        }
    }
}

async function buildModuleWithEsbuild(
    item: any,
    phaseCfg: any,
    opts: CliOptions,
    outputDir: string
) {
    const sourcemap = phaseCfg.sourcemap;
    const _outputFile = path.resolve(outputDir, item.outputFileName);
    const result = await esbuild({
        entryPoints: [item.sourceEntry],
        bundle: true,
        format: "iife",
        platform: "browser",
        target: [item.target],
        outfile: _outputFile,
        minify: phaseCfg.minify,
        sourcemap,
        logLevel: opts.verbose ? "info" : "debug"
    });

    if (phaseCfg.obfuscate) {
        let bundled = await fs.readFile(_outputFile, "utf-8");
        const level = (phaseCfg.obfuscate.level as ObfuscateLevel);
        const preset = OBFUSCATE_PRESETS[level] || OBFUSCATE_PRESETS.medium;
        bundled = JavaScriptObfuscator.obfuscate(bundled, {
            ...preset,
            sourceMap: false
        }).getObfuscatedCode();
        await fs.writeFile(_outputFile, bundled, "utf-8");
        if (sourcemap) {
            const mapFile = _outputFile + ".map";
            if (await exists(mapFile)) await fs.rm(mapFile);
        }
    }
    if (result && "rebuild" in result) {
        await (result.rebuild as any)?.dispose();
    }
}

async function writeIndexJson(outDir: string, data: any[]) {
    const distRoot = path.resolve(outDir, "..");
    await fs.mkdir(distRoot, { recursive: true });
    const filePath = path.join(distRoot, "addins-index.json");
    await fs.writeFile(filePath, JSON.stringify(data, null, 2), "utf-8");
}

async function exists(p: string) {
    try {
        await fs.access(p);
        return true;
    } catch {
        return false;
    }
}


main().catch(e => {
    console.error(e);
    process.exit(1);
});
