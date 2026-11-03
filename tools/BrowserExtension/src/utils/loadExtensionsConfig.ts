import { promises as fs } from "node:fs";
import path from "node:path";
import {
  validateExtensionsConfig,
  type ExtensionsConfig,
  type ExtensionConfigItem,
} from "@type/extensionsConfig";

type RequiredNonNullable<T> = {
  [K in keyof T]-?: NonNullable<T[K]>;
};


export interface LoadExtensionsOptions {
  cwd: string;               // 当前工作目录，默认 process.cwd()
  enabled?: boolean;          // 默认 true,仅构建启用的扩展
  only?: string | string[];       // 仅构建指定扩展（逗号分隔或数组）
}


export interface ExtensionBuildTarget
  extends RequiredNonNullable<ExtensionConfigItem> {
  // 绝对 manifest 路径
  //manifestAbsPath: string;
}


export interface LoadedExtensions {
  configPath: string;
  defaults: ExtensionsConfig["defaults"];
  items: ExtensionBuildTarget[];
}

export async function updateExtensionVersions(
  versionMap: Record<string, string>,
  extensionPath: string
): Promise<void> {
  const raw = await fs.readFile(extensionPath, "utf-8");
  let json: unknown;
  try {
    json = JSON.parse(raw);
  } catch (e) {
    throw new Error(`解析配置失败: ${extensionPath}\n${String(e)}`);
  }
  for (const [name, version] of Object.entries(versionMap)) {
    const item = (json as any).extensions.find((ext: { name: string; }) => ext.name === name);

    if (item) {
      item.currentVersion = version;
    }
  }

  fs.writeFile(extensionPath, JSON.stringify(json, null, 2), 'utf-8');
}

export async function loadExtensionsConfig(
  configPath: string,
  options: LoadExtensionsOptions
): Promise<LoadedExtensions> {
  const raw = await fs.readFile(configPath, "utf-8");
  let json: unknown;
  try {
    json = JSON.parse(raw);
  } catch (e) {
    throw new Error(`解析配置失败: ${configPath}\n${String(e)}`);
  }

  const validated = validateExtensionsConfig(json);
  if (!validated.ok) {
    const msg = validated.errors.map((e) => `- ${e}`).join("\n");
    throw new Error(`配置校验失败:\n${msg}`);
  }

  const filtered = applyFilters(validated.value, options);
  const items = filtered.extensions.map((ext) =>
    normalizeItem(options.cwd, ext)
  );

  // 校验 manifest 存在
  const missing = await findMissingManifests(items);
  if (missing.length) {
    const msg = missing
      .map((m) => `- ${m.name}: ${m.manifest}`)
      .join("\n");
    throw new Error(`以下 manifest 文件不存在:\n${msg}`);
  }

  // 输出目录冲突检测（绝对路径）
  const dupOut = findDuplicate(items.map((i) => i.outputDir ?? ""));
  if (dupOut.length) {
    const msg = dupOut.map((d) => `- ${d}`).join("\n");
    throw new Error(`存在重复输出目录（可能被覆盖）:\n${msg}`);
  }

  return {
    configPath,
    defaults: validated.value.defaults,
    items,
  };
}

function applyFilters(
  normalized: ExtensionsConfig,
  options: LoadExtensionsOptions
): ExtensionsConfig {
  const onlyEnabled = options.enabled ?? true;
  const onlySet = toSet(options.only);

  let list = normalized.extensions;
  if (onlyEnabled) {
    list = list.filter((e) => e.enabled);
  }
  if (onlySet) {
    list = list.filter((e) => onlySet.has(e.name ?? ""));
  }

  return { defaults: normalized.defaults, extensions: list };
}

function normalizeItem(
  cwd: string,
  item: ExtensionConfigItem
): ExtensionBuildTarget {
  const manifestAbs = path.isAbsolute(item.manifest ?? "")
    ? item.manifest
    : path.resolve(cwd, item.manifest ?? "");

  const outputDirAbs = path.isAbsolute(item.outputDir ?? "")
    ? item.outputDir
    : path.resolve(cwd, item.outputDir ?? "");

  return {
    build: item.build!,
    name: item.name!,
    manifest: manifestAbs!,
    outputDir: outputDirAbs!,
    enabled: item.enabled!,
    nativeHostName: item.nativeHostName!,
    clientFlag: item.clientFlag!,
    key: item.key!,
    currentVersion: item.currentVersion!
  };
}

async function findMissingManifests(
  items: ExtensionBuildTarget[]
): Promise<ExtensionBuildTarget[]> {
  const results = await Promise.all(
    items.map(async (i) => {
      try {
        await fs.access(i.manifest);
        return null;
      } catch {
        return i;
      }
    })
  );
  return results.filter(Boolean) as ExtensionBuildTarget[];
}

function toSet(v?: string | string[]): Set<string> | null {
  if (v == null) return null;
  const arr = Array.isArray(v)
    ? v
    : String(v)
      .split(",")
      .map((s) => s.trim())
      .filter(Boolean);
  return arr.length ? new Set(arr) : null;
}

function findDuplicate(values: string[]): string[] {
  const seen = new Set<string>();
  const dup = new Set<string>();
  for (const v of values) {
    if (seen.has(v)) dup.add(v);
    else seen.add(v);
  }
  return Array.from(dup);
}