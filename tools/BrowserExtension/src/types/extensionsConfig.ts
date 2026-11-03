// src/types/extensionsConfig.ts
import path from "node:path";

export interface ExtensionConfigItem {
  build?: string;
  name?: string;
  manifest?: string;
  outputDir?: string;
  enabled?: boolean;
  nativeHostName?: string;
  clientFlag?: string;
  key?: string;
  currentVersion?: string;
}

export interface ExtensionsConfigDefaults extends ExtensionConfigItem {
  outputBaseDir: string;
}

export interface ExtensionsConfig {
  defaults?: ExtensionsConfigDefaults;
  extensions: ExtensionConfigItem[];
}

/**
 * 校验 extensions.config.json
 */
export function validateExtensionsConfig(
  obj: unknown
): { ok: true; value: ExtensionsConfig } | { ok: false; errors: string[] } {
  const errors: string[] = [];

  if (typeof obj !== "object" || obj === null) {
    return { ok: false, errors: ["配置应为对象"] };
  }

  const root = obj as Partial<ExtensionsConfig>;

  if (!Array.isArray(root.extensions)) {
    return { ok: false, errors: ["extensions 字段必须是数组"] };
  }

  const defaults: ExtensionsConfigDefaults = {
    build: root.defaults?.build,
    name: root.defaults?.name,
    manifest: root.defaults?.manifest,
    outputDir: root.defaults?.outputDir,
    nativeHostName: root.defaults?.nativeHostName,
    clientFlag: root.defaults?.clientFlag,
    outputBaseDir: normalizeBaseDir(root.defaults?.outputBaseDir),
    enabled: root.defaults?.enabled ?? true,
    key: root.defaults?.key,
    currentVersion: root.defaults?.currentVersion
  };

  const seenNames = new Set<string>();
  const normalizedItems: ExtensionConfigItem[] = [];

  root.extensions.forEach((item, idx) => {
    const pfx = `extensions[${idx}]`;

    if (typeof item !== "object" || item === null) {
      errors.push(`${pfx} 必须是对象`);
      return;
    }

    const name = (item as ExtensionConfigItem).name;
    if (typeof name !== "string" || name.trim().length === 0) {
      errors.push(`${pfx}.name 必须是非空字符串`);
      return;
    }
    if (seenNames.has(name)) {
      errors.push(`${pfx}.name 重复: ${name}`);
      return;
    }
    seenNames.add(name);

    // manifest 合并
    const manifest = (item.manifest ?? defaults.manifest)?.trim();
    if (!manifest) {
      errors.push(`${pfx}.manifest 缺失（且 defaults.manifest 也未提供）`);
    }

    // build 合并
    const build = item.build || defaults.build || "";

    // enabled 合并
    const enabled = item.enabled !== undefined ? !!item.enabled : defaults.enabled;

    // outputDir 解析
    const resolvedOutputDir = resolveOutputDir(
      item.outputDir,
      defaults.outputBaseDir,
      name
    );

    normalizedItems.push({
      build,
      name,
      manifest: manifest || "",
      outputDir: resolvedOutputDir,
      enabled,
      nativeHostName: item.nativeHostName || defaults.nativeHostName,
      clientFlag: item.clientFlag || defaults.clientFlag,
      key: item.key || defaults.key,
      currentVersion: item.currentVersion || defaults.currentVersion
    });
  });

  if (errors.length > 0) {
    return { ok: false, errors };
  }

  return {
    ok: true,
    value: {
      defaults,
      extensions: normalizedItems
    }
  };
}

function normalizeBaseDir(dir?: string): string {
  if (!dir || !dir.trim()) return path.join("dist", "extensions");
  return dir.replace(/[/\\]+$/, ""); // 去掉末尾多余分隔符
}

function resolveOutputDir(
  outputDir: string | undefined,
  base: string,
  name: string
): string {
  if (outputDir && outputDir.trim()) {
    const raw = outputDir.trim();
    if (path.isAbsolute(raw)) return raw;
    return path.join(base, raw);
  }
  // 未提供使用 base + name
  return path.join(base, name);
}