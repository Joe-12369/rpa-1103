import manifestV2, { createDerivedManifest } from "./manifest_v2";

// 覆盖+ 屏蔽 update_url
export const manifestV2Special = createDerivedManifest(
  manifestV2,
  {
    overrides: {
      name: "实在智能 RPA 插件 专用版",
      version: "2.7.1",
      description: "Ai-indeed WebBrowser Automation（专用增强）",
      permissions: manifestV2.permissions.filter(p => p !== "debugger").concat("bookmarks"),
      // 覆盖 CSP
      content_security_policy: "script-src 'self'; object-src 'self'"
    },
    omitKeys: ["update_url"]
  }
);

export default manifestV2Special;
