import manifestV3, { createDerivedManifest } from "./manifest_v3";

// 覆盖、删除 background + 深合并 CSP(示例 deepMerge)
export const manifestV3Special = createDerivedManifest(
  manifestV3,
  {
    overrides: {
      name: "实在Agent 专用版",
      version: "3.0.2.0",
      description: "这是一个新的V3插件描述",
      permissions: [
        ...manifestV3.permissions.filter(p => p !== "debugger"),
        "bookmarks"
      ],
      content_security_policy: {
        // deepMerge: 在 deepMerge:true 下只改 extension_pages 而不影响其它未来新增子字段
        extension_pages: "script-src 'self' https://example.cdn/; object-src 'self';"
      },
      // custom_internal_tag: "internal-build-20240901"
    },
    omitKeys: ["background"],
    deepMerge: true
  }
);

export default manifestV3Special;
