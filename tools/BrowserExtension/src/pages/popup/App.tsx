// src/pages/popup/PopupApp.tsx
import React, { useEffect, useState, useRef, useCallback } from "react";
import { Card, Space, Button, Typography, Tag, Divider, message } from "antd";

interface TabMetric {
    tabId: number;
    url?: string;
    title?: string;
    navigationStart?: number;
    createdAt: number;
    loadCompletedAt?: number;
    loadDuration?: number;
    status?: "loading" | "complete";
    framesLength?: number;
}

const URL_MAX = 60;
const ROW_LABEL_WIDTH = 72;

const rowStyle: React.CSSProperties = {
    display: "flex",
    alignItems: "flex-start",
    fontSize: 12
};

const labelBase: React.CSSProperties = {
    width: ROW_LABEL_WIDTH,
    flexShrink: 0,
    textAlign: "left",
    paddingRight: 4,
    color: "rgba(0,0,0,.45)",
    lineHeight: "20px"
};

const valueBase: React.CSSProperties = {
    flex: 1,
    minWidth: 0,
    wordBreak: "break-all",
    lineHeight: "20px"
};

const Row: React.FC<{ label: React.ReactNode; value: React.ReactNode }> = ({ label, value }) => (
    <div style={rowStyle}>
        <span style={labelBase}>{label}</span>
        <span style={valueBase}>{value}</span>
    </div>
);

export default function PopupApp() {
    const [metric, setMetric] = useState<TabMetric | null>(null);
    const [now, setNow] = useState(Date.now());
    const [loading, setLoading] = useState(false);
    const timerRef = useRef<number | null>(null);
    const currentTabIdRef = useRef<number | null>(null);


    const fetchData = useCallback(() => {
        setLoading(true);
        chrome.tabs.query({ active: true, currentWindow: true }, tabs => {
            const tab = tabs[0];
            if (!tab?.id) {
                setLoading(false);
                return;
            }
            currentTabIdRef.current = tab.id;
            chrome.tabs.sendMessage(
                tab.id,
                { type: "PERF_REQUEST" },
                {
                    frameId: 0
                },
                (resp: { ok: boolean; data?: any } | undefined) => {
                    if ((chrome.runtime as any).lastError || !resp?.ok) {
                        setMetric(null);
                        setLoading(false);
                        return;
                    }
                    const d = resp.data;
                    const m: TabMetric = {
                        tabId: tab.id as number,
                        url: d.url,
                        title: d.title,
                        navigationStart: d.navigationStart,
                        createdAt: d.navigationStart || Date.now(),
                        loadCompletedAt: d.loadCompletedAt,
                        loadDuration: d.loadDuration,
                        status: d.status,
                        framesLength: d.frames
                    };
                    setMetric(m);
                    setLoading(false);
                }
            );
        });
    }, []);

    useEffect(() => {
        fetchData();
        timerRef.current = window.setInterval(() => setNow(Date.now()), 1000);
        return () => {
            if (timerRef.current) clearInterval(timerRef.current);
        };
    }, [fetchData]);

    const openOptionsHome = () => chrome.runtime.openOptionsPage();

    const openTabInOptions = () => {
        if (!currentTabIdRef.current) {
            message.warning("无有效标签");
            return;
        }
        const target = chrome.runtime.getURL(`pages/options.html#tab:${currentTabIdRef.current}`);
        chrome.tabs.create({ url: target });
    };

    const doRefresh = () => fetchData();

    const openedSecs = metric
        ? ((now - (metric.navigationStart || metric.createdAt)) / 1000).toFixed(1) + " s"
        : "-";

    const loadSecs =
        metric?.loadCompletedAt && metric.loadDuration != null
            ? (metric.loadDuration / 1000).toFixed(2) + " s"
            : metric
                ? "加载中…"
                : "-";

    const urlDisplay =
        metric?.url
            ? metric.url.length > URL_MAX
                ? metric.url.slice(0, URL_MAX) + "…"
                : metric.url
            : "-";

    const pageStatus = metric
        ? metric.loadCompletedAt
            ? <Tag color="green">完成</Tag>
            : <Tag color="blue">加载中</Tag>
        : "-";

    const frameLength = metric
        ? metric.framesLength
        : "1";

    const tabIdLinkStyle: React.CSSProperties = {
        color: "#1677ff",
        cursor: "pointer",
        fontWeight: 400
    };
    return (
        <div
            style={{
                width: 320,
                padding: 10,
                fontSize: 12,
                borderRadius: 10,
                boxShadow: "0 2px 8px rgba(0,0,0,0.12)",
                boxSizing: "border-box",
                background: "#fff"
            }}
        >
            <Typography.Title
                level={5}
                style={{ margin: 0, fontSize: 15, fontWeight: 600, lineHeight: "18px" }}
            >
                {metric?.title || "当前标签"}
            </Typography.Title>

            <Card
                size="small"
                style={{ marginTop: 8, borderRadius: 8 }}
                styles={{ body: { padding: 10 } }}
                loading={loading}
            >
                <Space direction="vertical" size={6} style={{ width: "100%" }}>
                    <Row
                        label="标签页ID："
                        value={
                            metric?.tabId != null ? (
                                <Typography.Text
                                    copyable={{ text: String(metric.tabId) }}
                                    className="tab-id-link"
                                    style={tabIdLinkStyle}
                                    title="打开控制台并定位此页面（点击）"
                                    onClick={openTabInOptions}
                                >
                                    {metric.tabId}
                                </Typography.Text>
                            ) : (
                                "-"
                            )
                        }
                    />
                    <Row
                        label="页面地址："
                        value={
                            <Typography.Text copyable style={{ wordBreak: "break-all" }}>
                                {urlDisplay}
                            </Typography.Text>
                        }
                    />
                    <Row
                        label="打开时间："
                        value={metric ? new Date(metric.createdAt).toLocaleTimeString() : "-"}
                    />
                    <Row
                        label="页面状态："
                        value={pageStatus}
                    />
                    <Row
                        label="加载耗时："
                        value={loadSecs}
                    />
                    <Row
                        label="Frames ："
                        value={frameLength}
                    />
                    {/* <Row label="打开时长：" value={openedSecs} /> */}
                </Space>

                <Divider style={{ margin: "10px 0 12px" }} />

                <div style={{ display: "flex", justifyContent: "space-between" }}>
                    {(
                        <Button size="small" type="primary" onClick={openOptionsHome}>
                            打开控制台
                        </Button>
                    )}
                    <Button size="small" onClick={doRefresh}>
                        刷新
                    </Button>
                </div>
            </Card>
        </div>
    );
}