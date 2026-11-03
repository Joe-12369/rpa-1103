// TypeScript
// src/utils/loadManifest.ts
import { promises as fs } from "node:fs";
import path from "node:path";
import os from "node:os";
import crypto from "node:crypto";
import { build } from "esbuild";

export interface LoadManifestResult<T extends object = Record<string, unknown>> {
    manifest: T;
    sourcePath: string;
}

export async function loadManifestModule<T extends object = any>(absPath: string): Promise<LoadManifestResult<T>> {
    const resolved = path.resolve(absPath);
    const tmpFile = path.join(os.tmpdir(), `mf-${crypto.randomUUID()}.mjs`);

    await build({
        entryPoints: [resolved],
        outfile: tmpFile,
        bundle: true,
        format: "esm",
        platform: "node",
        target: ["node18"],
        logLevel: "silent",
        sourcemap: "inline",
    });

    const mod = await import(pathToFileURL(tmpFile));
    const manifest = (mod.default ?? mod.manifest ?? mod) as T;

    if (!manifest || typeof manifest !== "object") {
        throw new Error(`清单模块未导出对象: ${resolved}`);
    }
    return { manifest, sourcePath: resolved };
}

export async function writeManifestJson(outFile: string, manifest: object): Promise<void> {
    const dir = path.dirname(outFile);
    await fs.mkdir(dir, { recursive: true });
    await fs.writeFile(outFile, JSON.stringify(manifest, null, 2), "utf-8");
}

export function dedupe<T>(arr: T[] | undefined): T[] | undefined {
    if (!arr) return arr;
    return Array.from(new Set(arr));
}

function pathToFileURL(p: string): string {
    let u = path.resolve(p).replace(/\\/g, "/");
    if (!u.startsWith("/")) u = `/${u}`;
    return `file://${u}?v=${Date.now()}-${Math.random()}`;
}