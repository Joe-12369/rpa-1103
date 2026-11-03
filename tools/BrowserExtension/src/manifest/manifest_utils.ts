export interface DeriveOptions<TBase, TOverride> {
  overrides?: TOverride;
  omitKeys?: (keyof (TBase & TOverride))[];
  deepMerge?: boolean;
}

function isPlainObject(v: any) {
  return Object.prototype.toString.call(v) === "[object Object]";
}

function deepMergeObj(target: any, src: any) {
  const out: any = { ...target };
  for (const key of Object.keys(src)) {
    const sv = src[key];
    const tv = out[key];
    if (isPlainObject(tv) && isPlainObject(sv)) {
      out[key] = deepMergeObj(tv, sv);
    } else {
      out[key] = sv;
    }
  }
  return out;
}

/**
 * 派生 manifest
 * base: 基础对象
 * options.overrides: 覆盖或新增字段
 * options.omitKeys: 顶层要删除的字段
 * options.deepMerge: true 时对象字段用深合并，否则直接覆盖
 */
export function createDerivedManifest<
  TBase extends Record<string, any>,
  TOverride extends Partial<TBase> & Record<string, any> = {}
>(
  base: TBase,
  options: DeriveOptions<TBase, TOverride> = {}
) {
  const { overrides = {} as TOverride, omitKeys = [], deepMerge = false } = options;
  let merged: any;
  if (!deepMerge) {
    merged = { ...base, ...overrides };
  } else {
    // 逐字段: 对象做深合并, 其他直接覆盖
    merged = { ...base };
    for (const k of Object.keys(overrides)) {
      const ov = (overrides as any)[k];
      if (isPlainObject(merged[k]) && isPlainObject(ov)) {
        merged[k] = deepMergeObj(merged[k], ov);
      } else {
        merged[k] = ov;
      }
    }
  }
  for (const k of omitKeys) {
    delete merged[k as string];
  }
  return merged as Omit<TBase & TOverride, typeof omitKeys[number]>;
}
