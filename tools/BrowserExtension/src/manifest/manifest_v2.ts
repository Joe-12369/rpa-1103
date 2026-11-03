import { createDerivedManifest } from "./manifest_utils";


// 实在智能 RPA 插件 Manifest V2 定义
export const manifestV2 = {
  update_url: "https://clients2.google.com/service/update2/crx",
  name: "实在智能 RPA 插件",
  version: "2.7.0",
  description: "Ai-indeed WebBrowser Automation",
  web_accessible_resources: ["executor/index.js"],
  content_security_policy: "script-src 'self' 'unsafe-eval'; object-src 'self'",
  permissions: [
    "nativeMessaging",
    "management",
    "tabs",
    "webNavigation",
    "cookies",
    "clipboardRead",
    "clipboardWrite",
    "downloads",
    "debugger",
    "<all_urls>"
  ],
  background: {
    scripts: [
      "./scripts/jquery.js",
      "./scripts/rpa_server.js",
      "./scripts/backgroundscript.js",
      "background.js"
    ],
    persistent: true
  },
  options_page: "pages/options.html",
  browser_action: { "default_popup": "pages/popup.html" },
  content_scripts: [
    {
      match_about_blank: true,
      all_frames: true,
      js: ["content.js"],
      matches: [
        "http://*/*",
        "https://*/*",
        "file://*/*",
        "ftp://*/*"
      ],
      run_at: "document_start"
    }
  ],
  icons: {
    "16": "icons/indeed-16x16.png",
    "48": "icons/indeed-48x48.png",
    "128": "icons/indeed-128x128.png"
  },
  default_locale: "zh_CN",
  manifest_version: 2
} satisfies {
  update_url: string;
  name: string;
  version: string;
  description: string;
  options_page?: string;
  browser_action?: { default_popup: string };
  web_accessible_resources: string[];
  content_security_policy: string;
  permissions: string[];
  background: { scripts: string[]; persistent: boolean };
  content_scripts: {
    match_about_blank: boolean;
    all_frames: boolean;
    js: string[];
    matches: string[];
    run_at: string;
  }[];
  icons: Record<string, string>;
  default_locale: string;
  manifest_version: 2;
};

export type ManifestV2 = typeof manifestV2;
export default manifestV2;
export { createDerivedManifest };
