
import React, { useState, useMemo, useEffect } from "react";
import {
    Layout,
    ConfigProvider,
    theme,
    Typography
} from "antd";
import {
    HomeOutlined,
    ContainerOutlined,
    ToolOutlined,
    FileSearchOutlined,
    AppstoreOutlined,
    SettingOutlined
} from "@ant-design/icons";

import Sidebar from "./components/Sidebar";

import Home from "./modules/Home";
import Pages from "./modules/Pages";
import DebugPage from "./modules/DebugPage";
import Logs from "./modules/Logs";
import ExtensionInfo from "./modules/ExtensionInfo";
import Settings from "./modules/Settings";

const { Sider, Content, Footer } = Layout;

interface MenuMeta {
    key: string;
    label: string;
    icon: React.ReactNode;
    desc?: string;
}

const MENUS: MenuMeta[] = [
    // { key: "home", label: "首页", icon: <HomeOutlined />, desc: "执行指标概览" },
    { key: "pages", label: "页面", icon: <ContainerOutlined />, desc: "已打开标签页信息" },
    { key: "debug", label: "请求", icon: <ToolOutlined />, desc: "请求信息" },
    { key: "extension", label: "扩展", icon: <AppstoreOutlined />, desc: "扩展信息" },
    { key: "settings", label: "设置", icon: <SettingOutlined />, desc: "扩展设置管理" }
];

function renderModule(
    key: string,
    layoutCtl: {
        collapsed: boolean;
        setCollapsed: React.Dispatch<React.SetStateAction<boolean>>;
        dark: boolean;
        toggleTheme: () => void;
    },
    // 新增：selectedTabId 传入 Pages 用于高亮
    selectedTabId: string | null
) {
    switch (key) {
        case "pages": return <Pages selectedTabId={selectedTabId} />;
        case "debug": return <DebugPage />;
        case "logs": return <Logs />;
        case "extension": return <ExtensionInfo />;
        case "settings": return <Settings layout={layoutCtl} />;
        default: return <Home />;
    }
}

export default function App() {
    const [collapsed, setCollapsed] = useState(false);
    const [active, setActive] = useState(() => {
        const saved = localStorage.getItem("active_menu");
        return saved && MENUS.some(m => m.key === saved) ? saved : "pages";
    });
    const [dark, setDark] = useState(() => localStorage.getItem("ext_theme") === "dark");

    // 被传入的 tabId，用于 Pages 高亮
    const [selectedTabId, setSelectedTabId] = useState<string | null>(null);

    const algorithms = dark
        ? [theme.darkAlgorithm]
        : [theme.defaultAlgorithm];

    const current = useMemo(
        () => MENUS.find(m => m.key === active) || MENUS[0],
        [active]
    );

    // 解析 location.hash（例如 options.html#tab:1023817732）
    useEffect(() => {
        const parseHash = () => {
            try {
                const hash = window.location.hash || "";
                const m = hash.match(/^#tab:(\d+)$/);
                if (m) {
                    const id = m[1];
                    setActive("pages");
                    localStorage.setItem("active_menu", "pages");
                    setSelectedTabId(id);
                } else {
                    setSelectedTabId(null);
                }
            } catch (e) {
                console.error("parse hash failed", e);
                setSelectedTabId(null);
            }
        };

        parseHash();
        window.addEventListener("hashchange", parseHash);
        return () => window.removeEventListener("hashchange", parseHash);
    }, []);

    const view = useMemo(
        () =>
            renderModule(active, {
                collapsed,
                setCollapsed,
                dark,
                toggleTheme: () => {
                    const next = !dark;
                    setDark(next);
                    localStorage.setItem("ext_theme", next ? "dark" : "light");
                }
            }, selectedTabId),
        [active, collapsed, dark, selectedTabId]
    );

    return (
        <ConfigProvider
            theme={{
                algorithm: algorithms,
                token: {
                    borderRadius: 6,
                    colorPrimary: "#1677ff",
                    controlHeight: 34,
                    controlHeightSM: 30,
                    controlHeightLG: 40,
                    fontSize: 14,
                    fontSizeSM: 14,
                    fontSizeLG: 16
                }
            }}
        >
            <ThemedLayout
                collapsed={collapsed}
                setCollapsed={setCollapsed}
                active={active}
                setActive={(key: string) => {
                    setActive(key);
                    localStorage.setItem("active_menu", key);
                }}
                current={current}
                dark={dark}
                toggleTheme={() => {
                    const next = !dark;
                    setDark(next);
                    localStorage.setItem("ext_theme", next ? "dark" : "light");
                }}
                view={view}
            />
        </ConfigProvider>
    );
}

function ThemedLayout(props: {
    collapsed: boolean;
    setCollapsed: (v: boolean | ((p: boolean) => boolean)) => void;
    active: string;
    setActive: (k: string) => void;
    current: MenuMeta;
    dark: boolean;
    toggleTheme: () => void;
    view: React.ReactNode;
}) {
    const { token } = theme.useToken();
    const sidebarItems = useMemo(
        () => MENUS.map(m => ({ key: m.key, label: m.label, icon: m.icon })),
        []
    );

    return (
        <Layout
            style={{
                height: "100vh",
                maxHeight: "100dvh",
                overflow: "hidden",
                background: token.colorBgLayout
            }}
        >
            <Sider
                collapsible
                collapsed={props.collapsed}
                trigger={null}
                width={260}
                style={{
                    display: "flex",
                    flexDirection: "column",
                    borderRight: `1px solid ${token.colorBorderSecondary}`,
                    background: token.colorBgContainer
                }}
            >
                {/* LOGO */}
                <div
                    style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: props.collapsed ? "center" : "center",
                        gap: 8,
                        padding: props.collapsed ? "14px 16px" : "18px 18px 10px 18px",
                        fontWeight: 600,
                        fontSize: 18,
                        lineHeight: 1,
                        color: token.colorText
                    }}
                >
                    <img
                        src={
                            chrome?.runtime?.getURL
                                ? chrome.runtime.getURL("/icons/indeed-32x32.png")
                                : "../../icons/indeed-32x32.png"
                        }
                        alt="logo"
                        width={20}
                        height={20}
                        style={{ flexShrink: 0 }}
                    />
                    {!props.collapsed && (
                        <span
                            style={{
                                display: "inline-block",
                                transform: "translateY(1px)",
                                letterSpacing: "0.5px"
                            }}
                        >
                            实在Agent 扩展
                        </span>
                    )}
                </div>

                {/* 菜单 */}
                <div
                    style={{
                        flex: 1,
                        overflowY: "auto",
                        display: "flex",
                        flexDirection: "column",
                        padding: props.collapsed ? "32px 0 12px" : "50px 0 16px"
                    }}
                >
                    <div style={{ width: props.collapsed ? "100%" : 200, margin: "0 auto" }}>
                        <Sidebar
                            items={sidebarItems}
                            active={props.active}
                            onChange={props.setActive}
                        />
                    </div>
                </div>
            </Sider>

            <Layout style={{ minWidth: 0, background: token.colorBgLayout }}>
                <Content
                    style={{
                        position: "relative",
                        overflow: "auto",
                        padding: `16px 40px 16px ${props.collapsed ? 24 : 54}px`,
                        background: token.colorBgLayout
                    }}
                >
                    <div
                        style={{
                            margin: "0 0 16px",
                            paddingBottom: 8,
                            borderBottom: `1px solid ${token.colorBorderSecondary}`,
                            display: "flex",
                            flexDirection: "column",
                            gap: 4
                        }}
                    >
                        <Typography.Title
                            level={4}
                            style={{
                                margin: 0,
                                fontSize: 18,
                                fontWeight: 600,
                                lineHeight: "1.25",
                                color: token.colorText
                            }}
                        >
                            {props.current.label}
                        </Typography.Title>

                        {props.current.desc && (
                            <Typography.Text
                                type="secondary"
                                style={{
                                    fontSize: 13,
                                    lineHeight: "18px",
                                    opacity: 0.75,
                                    paddingLeft: 2
                                }}
                            >
                                {props.current.desc}
                            </Typography.Text>
                        )}
                    </div>
                    {props.view}
                </Content>
                <Footer
                    style={{
                        padding: "6px 16px",
                        fontSize: 12,
                        textAlign: "center",
                        background: token.colorBgContainer,
                        borderTop: `1px solid ${token.colorBorderSecondary}`,
                        color: token.colorTextSecondary
                    }}
                >
                    版权所有©浙江实在智能科技有限公司 {new Date().getFullYear()}
                </Footer>
            </Layout>
        </Layout>
    );
}