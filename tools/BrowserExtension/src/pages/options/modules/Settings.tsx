// Settings.tsx

import React, { useEffect, useState } from "react";
import { Form, Radio, Switch, InputNumber, message, Button } from "antd";
import {
    getTotalStoreSizeBytes,
    clearAllRequests,
    STORAGE_KEYS,
} from "@utils/requestStore";
import {
    DEFAULT_INJECT_TIMEOUT_SETTINGS,
    INJECT_TIMEOUT_STORAGE_KEYS,
    normalizeInjectTimeoutSettings,
} from "@/utils/injectSettings";

interface LayoutCtl {
    collapsed: boolean;
    setCollapsed: React.Dispatch<React.SetStateAction<boolean>>;
    dark: boolean;
    toggleTheme: () => void;
}

interface StoredSettings {
    theme: "light" | "dark";
    sidebarCollapsed: boolean;
    logRetentionDays: number;
    recordingEnabled?: boolean;
    diagnosticEnabled?: boolean;
    serviceHubEnabled?: boolean;
    injectTargetFrameTimeoutMs: number;
    injectWarmupFrameTimeoutMs: number;
}

const DEFAULT_SETTINGS: StoredSettings = {
    theme: "light",
    sidebarCollapsed: false,
    logRetentionDays: 2,
    recordingEnabled: false,
    diagnosticEnabled: false,
    serviceHubEnabled: false,
    injectTargetFrameTimeoutMs: DEFAULT_INJECT_TIMEOUT_SETTINGS.targetFrameTimeoutMs,
    injectWarmupFrameTimeoutMs: DEFAULT_INJECT_TIMEOUT_SETTINGS.warmupFrameTimeoutMs,
};

async function loadSettings(): Promise<StoredSettings> {
    const s = { ...DEFAULT_SETTINGS };
    try {
        const result = await chrome.storage.local.get([
            ...Object.values(STORAGE_KEYS),
            ...Object.values(INJECT_TIMEOUT_STORAGE_KEYS),
        ]);
        const injectSettings = normalizeInjectTimeoutSettings(result);

        if (typeof result[STORAGE_KEYS.LOG_RETENTION_DAYS] === "number") {
            s.logRetentionDays = result[STORAGE_KEYS.LOG_RETENTION_DAYS];
        }
        if (typeof result[STORAGE_KEYS.RECORDING_ENABLED] === "boolean") {
            s.recordingEnabled = result[STORAGE_KEYS.RECORDING_ENABLED];
        }
        if (typeof result[STORAGE_KEYS.LOG_LEVEL] === "boolean") {
            s.diagnosticEnabled = result[STORAGE_KEYS.LOG_LEVEL];
        }
        if (typeof result[STORAGE_KEYS.SERVICE_HUB_ENABLED] === "boolean") {
            s.serviceHubEnabled = result[STORAGE_KEYS.SERVICE_HUB_ENABLED];
        }
        if (result[STORAGE_KEYS.THEME] === "dark") {
            s.theme = "dark";
        }
        if (typeof result[STORAGE_KEYS.SIDEBAR_COLLAPSED] === "boolean") {
            s.sidebarCollapsed = result[STORAGE_KEYS.SIDEBAR_COLLAPSED];
        }
        s.injectTargetFrameTimeoutMs = injectSettings.targetFrameTimeoutMs;
        s.injectWarmupFrameTimeoutMs = injectSettings.warmupFrameTimeoutMs;
    } catch (e) {
        console.warn("Failed to load settings from chrome.storage", e);
    }
    return s;
}

async function saveSetting(key: string, value: any) {
    try {
        await chrome.storage.local.set({ [key]: value });
    } catch (e) {
        console.error(`Failed to save ${key}`, e);
    }
}

export default function Settings(props: { layout?: LayoutCtl }) {
    const layout = props.layout;
    const [form] = Form.useForm<StoredSettings>();
    const [loading, setLoading] = useState(true);
    const [storeSizeMB, setStoreSizeMB] = useState<string>("-");
    const [sizeLoading, setSizeLoading] = useState<boolean>(false);

    async function refreshStoreSize() {
        setSizeLoading(true);
        try {
            const bytes = await getTotalStoreSizeBytes();
            const m = bytes / 1024 / 1024;
            setStoreSizeMB(`${m < 0.01 ? m.toFixed(3) : m.toFixed(2)} MB`);
        } catch {
            setStoreSizeMB("-");
        } finally {
            setSizeLoading(false);
        }
    }

    useEffect(() => {
        loadSettings().then((st) => {
            form.setFieldsValue(st);
            if (layout) {
                if ((st.theme === "dark") !== layout.dark) layout.toggleTheme?.();
                if (st.sidebarCollapsed !== layout.collapsed) layout.setCollapsed?.(st.sidebarCollapsed);
            }
            setLoading(false);
            refreshStoreSize();
        });
    }, []);

    async function handleClearStore() {
        try {
            await clearAllRequests();
            message.success("日志已清空");
            refreshStoreSize();
        } catch (err) {
            message.error("清空失败：" + String(err));
        }
    }

    return (
        <div style={{ maxWidth: 520 }}>
            <Form
                form={form}
                layout="vertical"
                initialValues={DEFAULT_SETTINGS}
                disabled={loading}
                onValuesChange={async (changedValues, allValues) => {
                    const { layout } = props;

                    // 诊断日志
                    if ("diagnosticEnabled" in changedValues) {
                        const en = Boolean(allValues.diagnosticEnabled);
                        await saveSetting(STORAGE_KEYS.LOG_LEVEL, en);
                    }

                    // ServiceHub 开关
                    if ("serviceHubEnabled" in changedValues) {
                        const enabled = Boolean(allValues.serviceHubEnabled);
                        await saveSetting(STORAGE_KEYS.SERVICE_HUB_ENABLED, enabled);
                    }

                    // 请求记录开关
                    if ("recordingEnabled" in changedValues) {
                        const enabled = Boolean(allValues.recordingEnabled);
                        await saveSetting(STORAGE_KEYS.RECORDING_ENABLED, enabled);
                    }

                    // 日志保留天数
                    if ("logRetentionDays" in changedValues) {
                        await saveSetting(STORAGE_KEYS.LOG_RETENTION_DAYS, allValues.logRetentionDays);
                    }

                    // 注入超时
                    if ("injectTargetFrameTimeoutMs" in changedValues) {
                        await saveSetting(
                            INJECT_TIMEOUT_STORAGE_KEYS.TARGET_FRAME_TIMEOUT_MS,
                            allValues.injectTargetFrameTimeoutMs
                        );
                    }
                    if ("injectWarmupFrameTimeoutMs" in changedValues) {
                        await saveSetting(
                            INJECT_TIMEOUT_STORAGE_KEYS.WARMUP_FRAME_TIMEOUT_MS,
                            allValues.injectWarmupFrameTimeoutMs
                        );
                    }

                    // 主题切换
                    if ("theme" in changedValues) {
                        const newTheme = allValues.theme;
                        await saveSetting(STORAGE_KEYS.THEME, newTheme);
                        if (layout && (newTheme === "dark") !== layout.dark) {
                            layout.toggleTheme?.();
                        }
                    }

                    // 侧边栏折叠
                    if ("sidebarCollapsed" in changedValues) {
                        const collapsed = allValues.sidebarCollapsed;
                        await saveSetting(STORAGE_KEYS.SIDEBAR_COLLAPSED, collapsed);
                        if (layout && collapsed !== layout.collapsed) {
                            layout.setCollapsed?.(collapsed);
                        }
                    }
                }}
            >
                <Form.Item label="诊断日志" name="diagnosticEnabled" valuePropName="checked">
                    <Switch checkedChildren="开启" unCheckedChildren="关闭" />
                </Form.Item>

                <Form.Item label="开启 ServiceHub" name="serviceHubEnabled" valuePropName="checked">
                    <Switch checkedChildren="开启" unCheckedChildren="关闭" />
                </Form.Item>

                <Form.Item label="记录请求日志" name="recordingEnabled" valuePropName="checked">
                    <Switch checkedChildren="开启" unCheckedChildren="关闭" />
                </Form.Item>

                <Form.Item
                    shouldUpdate={(prev, cur) => prev.recordingEnabled !== cur.recordingEnabled}
                    noStyle
                >
                    {({ getFieldValue }) => {
                        const rec = getFieldValue("recordingEnabled");
                        if (!rec) return null;
                        return (
                            <Form.Item
                                label="请求日志保留天数"
                                name="logRetentionDays"
                                rules={[
                                    { required: true, message: "请输入天数" },
                                    {
                                        validator(_, v) {
                                            if (v >= 1 && v <= 5) return Promise.resolve();
                                            return Promise.reject("范围 1~5 天");
                                        },
                                    },
                                ]}
                            >
                                <InputNumber min={1} max={5} style={{ width: 120 }} />
                            </Form.Item>
                        );
                    }}
                </Form.Item>

                <Form.Item
                    label="目标 frame 注入超时(ms)"
                    name="injectTargetFrameTimeoutMs"
                    rules={[
                        { required: true, message: "请输入超时时间" },
                        {
                            validator(_, v) {
                                if (v >= 500 && v <= 60000) return Promise.resolve();
                                return Promise.reject("范围 500~60000 ms");
                            },
                        },
                    ]}
                >
                    <InputNumber min={500} max={60000} step={500} style={{ width: 140 }} />
                </Form.Item>

                <Form.Item
                    label="iframe 预热超时(ms)"
                    name="injectWarmupFrameTimeoutMs"
                    rules={[
                        { required: true, message: "请输入超时时间" },
                        {
                            validator(_, v) {
                                if (v >= 500 && v <= 60000) return Promise.resolve();
                                return Promise.reject("范围 500~60000 ms");
                            },
                        },
                    ]}
                >
                    <InputNumber min={500} max={60000} step={500} style={{ width: 140 }} />
                </Form.Item>

                <Form.Item label="主题颜色" name="theme">
                    <Radio.Group optionType="button" buttonStyle="solid">
                        <Radio.Button value="light">浅色</Radio.Button>
                        <Radio.Button value="dark">深色</Radio.Button>
                    </Radio.Group>
                </Form.Item>

                <Form.Item label="左侧导航" name="sidebarCollapsed" valuePropName="checked">
                    <Switch checkedChildren="窄" unCheckedChildren="宽" />
                </Form.Item>

                <Form.Item label="日志大小">
                    <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                        <div>{sizeLoading ? "加载中..." : storeSizeMB}</div>
                        <Button danger onClick={handleClearStore} disabled={sizeLoading}>
                            清空日志
                        </Button>
                    </div>
                </Form.Item>
            </Form>
            <div style={{ fontSize: 12, opacity: 0.55, marginTop: 8 }}>
                设置即时生效 · 自动保存
            </div>
        </div>
    );
}
