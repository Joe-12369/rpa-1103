import { createDerivedManifest } from "./manifest_utils";


export const manifestV3 = {
  update_url: "https://clients2.google.com/service/update2/crx",
  manifest_version: 3,
  name: "实在Agent：AI智能体助理，开启智能办公新范式",
  version: "4.0.0.0",
  version_name: "4.0.0.0",
  description: "实在Agent插件，v3版本能力再升级，查询数据、处理文档、填报表单、网页交互等各类网页操作场景轻松应对",
  background: { service_worker: "service_worker.js" },
  content_scripts: [
    {
      all_frames: true,
      js: [
        "content.js"
      ],
      match_about_blank: true,
      matches: ["http://*/*", "https://*/*", "ftp://*/*", "file://*/*"],
      run_at: "document_start"
    }
  ],
  options_ui: {
    page: "pages/options.html",
    open_in_tab: true
  },
  action: {
    default_icon: {
      "16": "icons/indeed-16x16.png",
      "48": "icons/indeed-48x48.png",
      "128": "icons/indeed-128x128.png"
    },
    default_title: "实在智能 RPA 插件",
    default_popup: "pages/popup.html"
  },
  content_security_policy: {
    extension_pages: "script-src 'self' ; object-src 'self'; font-src 'self' https://fonts.gstatic.com/ https://res-1.cdn.office.net/;"
  },
  permissions: [
    "storage", "cookies", "tabs", "debugger", "offscreen",
    "nativeMessaging", "webNavigation", "downloads", "scripting", "activeTab"
  ],
  host_permissions: ["<all_urls>"],
  icons: {
    "16": "icons/indeed-16x16.png",
    "32": "icons/indeed-32x32.png",
    "48": "icons/indeed-48x48.png",
    "128": "icons/indeed-128x128.png"
  },
  default_locale: "zh_CN"
} satisfies {
  update_url: string;
  version_name?: string;
  manifest_version: 3;
  name: string;
  version: string;
  description: string;
  background: { service_worker: string, type?: string };
  content_scripts: {
    all_frames: boolean;
    js: string[];
    match_about_blank: boolean;
    matches: string[];
    run_at: string;
  }[];
  options_ui: { page: string; open_in_tab: boolean };
  action: {
    default_icon: Record<string, string>;
    default_title: string;
    default_popup: string;
  };
  content_security_policy: { extension_pages: string };
  permissions: string[];
  host_permissions: string[];
  icons: Record<string, string>;
  default_locale?: string;
};

export default manifestV3;
export type ManifestV3 = typeof manifestV3;
export { createDerivedManifest };