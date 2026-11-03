// TypeScript
// src/utils/scanAddins.ts
import { promises as fs } from "node:fs";
import path from "node:path";
import * as Addins from "@/types/addins";

export async function scanAddins(
    configRel = "config/addins.json",
    cwd = process.cwd()
): Promise<Addins.ScanResult> {
    const configPath = path.resolve(cwd, configRel);
    const rawTxt = await fs.readFile(configPath, "utf-8");
    let root: Addins.AddinsJsonRoot;
    try {
        root = JSON.parse(rawTxt);
    } catch (e) {
        throw new Error(`解析 addins.json 失败: ${e}`);
    }

    // 合并 libs + items
    const rawAll: { item: Addins.AddinsItem; source: "libs" | "items" }[] = [];
    (root.libs || []).forEach(i => rawAll.push({ item: i, source: "libs" }));
    (root.items || []).forEach(i => rawAll.push({ item: i, source: "items" }));

    const warnings: string[] = [];
    const seen = new Set<string>();
    const norm: Addins.NormalizedAddin[] = [];

    for (const { item, source } of rawAll) {
        if (!item || typeof item !== "object") continue;
        const name = (item.name || "").trim();
        if (!name) {
            warnings.push(`[skip] 缺少 name (source=${source})`);
            continue;
        }
        if (seen.has(name)) {
            warnings.push(`[dup] 重复的 name: ${name} (source=${source})`);
            continue;
        }
        seen.add(name);

        // const enabled = item.enabled ?? root.defaults.enabled;

        //if (!enabled) continue;

        const type = (item.type || root.defaults.type) as Addins.AddinType;
        const kind = (item.kind || root.defaults.kind) as Addins.AddinKind;
        const entryRel = (item.entry || root.defaults.entry)?.trim();
        const _targets = Array.isArray(item.target_browser)
            ? item.target_browser?.filter(Boolean)
            : root.defaults.target_browser

        if (!entryRel) {
            warnings.push(`[skip] ${name} 缺少 entry`);
            continue;
        }

        const absEntry = path.resolve(cwd, "src", "addins", entryRel);
        let exists = true;
        try { await fs.access(absEntry); } catch { exists = false; }
        if (!exists) {
            warnings.push(`[skip] 源不存在: ${name} -> ${entryRel}`);
            continue;
        }

        const buildPhase = mergePhase(root.defaults.build, item.build);
        const publishPhase = mergePhase(root.defaults.publish, item.publish);

        norm.push({
            name,
            description: item.description || "",
            type,
            kind,
            enabled: item.enabled === false ? false : true,
            entry: entryRel,
            sourceEntry: absEntry,
            outputFileName: `${name}.js`,
            build: buildPhase,
            publish: publishPhase,
            isLegacy: kind === "legacy",
            target: item.target || root.defaults.target || "",
            target_browser: _targets || [],
            sourceCategory: source
        });
    }

    return {
        items: norm,
        warnings,
        outputDir: path.resolve(cwd, root.defaults.outputDir),
        configPath
    };
}

/* -------------- Helpers -------------- */

function mergePhase(base: Addins.NormalizedPhase | undefined, over: Addins.NormalizedPhase | undefined): Addins.NormalizedPhase {
    return {
        minify: (over?.minify ?? base?.minify) || false,
        obfuscate: (over?.obfuscate ?? base?.obfuscate) || false,
        sourcemap: (over?.sourcemap ?? base?.sourcemap) || false,
        embed: (over?.embed ?? base?.embed) ?? false
    };
}


function normalizeObfuscate(v: boolean | Addins.ObfuscateLevel | undefined): { enabled: boolean; level: Addins.ObfuscateLevel } {
    if (v === false || v === undefined) return { enabled: false, level: "medium" };
    if (v === true) return { enabled: true, level: "medium" };
    if (v === "low" || v === "medium" || v === "high") return { enabled: true, level: v };
    return { enabled: true, level: "medium" };
}