import React, { useEffect, useState } from "react";
import { Table, Button, Input, Space, Select, Tag } from "antd";

import { Tooltip, Typography, message } from "antd";
import { CopyOutlined } from "@ant-design/icons";

const { Text } = Typography;

const { Option } = Select;

interface TabInfo {
    id: number;
    title: string;
    url: string;
    windowId: number;
    active: boolean;
    status: string;
    loadTime?: number;
    windowType?: string;
    navigationStart?: number;
    loadCompletedAt?: number;
    loadDuration?: number;
    framesLength?: number;
}

export default function Pages() {
    const [tabs, setTabs] = useState<TabInfo[]>([]);
    const [search, setSearch] = useState("");
    const [filter, setFilter] = useState("normal");

    useEffect(() => {
        const fetchTabs = async () => {
            chrome.tabs.query({}, async (allTabs) => {
                const metrics: TabInfo[] = await Promise.all(
                    allTabs.map((tab) => {
                        return new Promise<TabInfo>((resolve) => {
                            if (!tab.id) {
                                resolve({
                                    id: -1,
                                    title: "未知",
                                    url: "",
                                    windowId: -1,
                                    active: false,
                                    status: "unknown",
                                    windowType: "unknown",
                                });
                                return;
                            }

                            const url = tab.url || "";
                            const isNormalPage = url.startsWith("http://") || url.startsWith("https://");
                            const windowType = isNormalPage ? "normal" : "extension";

                            chrome.tabs.sendMessage(
                                tab.id,
                                { type: "PERF_REQUEST" },
                                { frameId: 0 },
                                (resp: { ok: boolean; data?: any } | undefined) => {
                                    if ((chrome.runtime as any).lastError || !resp?.ok) {
                                        resolve({
                                            id: tab.id!,
                                            title: tab.title || "无标题",
                                            url: tab.url || "",
                                            windowId: tab.windowId,
                                            active: tab.active,
                                            status: "loading",
                                            windowType,
                                        });
                                        return;
                                    }

                                    const d = resp.data;
                                    resolve({
                                        id: tab.id!,
                                        url: d.url,
                                        title: d.title,
                                        windowId: tab.windowId,
                                        active: tab.active,
                                        navigationStart: d.navigationStart,
                                        loadCompletedAt: d.loadCompletedAt,
                                        loadDuration: d.loadDuration,
                                        status: d.status,
                                        framesLength: d.frames,
                                        windowType,
                                    });
                                }
                            );
                        });
                    })
                );
                setTabs(metrics);
            });
        };
        fetchTabs();

        // 监听标签页变化
        const handleTabUpdate = () => fetchTabs();
        chrome.tabs.onCreated.addListener(handleTabUpdate);
        chrome.tabs.onRemoved.addListener(handleTabUpdate);
        chrome.tabs.onUpdated.addListener(handleTabUpdate);

        return () => {
            chrome.tabs.onCreated.removeListener(handleTabUpdate);
            chrome.tabs.onRemoved.removeListener(handleTabUpdate);
            chrome.tabs.onUpdated.removeListener(handleTabUpdate);
        };
    }, []);

    const handleSearch = (e: React.ChangeEvent<HTMLInputElement>) => {
        setSearch(e.target.value);
    };

    const handleFilterChange = (value: string) => {
        setFilter(value);
    };

    const filteredTabs = tabs.filter((tab) => {
        const matchesSearch =
            tab.title?.includes(search) || tab.url?.includes(search);

        const matchesFilter =
            filter === "all" ||
            (filter === "normal" && tab.windowType === "normal") ||
            (filter === "extension" && tab.windowType === "extension");

        return matchesSearch && matchesFilter;
    });

    const columns = [
        {
            title: "Tab ID",
            dataIndex: "id",
            key: "id",
        },
        {
            title: "标题",
            dataIndex: "title",
            key: "title",
            render: (title: string) => (
                <Space>
                    <Tooltip title={title}>
                        <Text ellipsis style={{ maxWidth: 200 }}>
                            {title}
                        </Text>
                    </Tooltip>
                    <Button
                        icon={<CopyOutlined />}
                        size="small"
                        onClick={() => {
                            navigator.clipboard.writeText(title);
                            message.success("标题已复制！");
                        }}
                    />
                </Space>
            ),
        },
        {
            title: "URL",
            dataIndex: "url",
            key: "url",
            render: (url: string) => (
                <Space>
                    <Tooltip title={url}>
                        <Text ellipsis style={{ maxWidth: 300 }}>
                            {url}
                        </Text>
                    </Tooltip>
                    <Button
                        icon={<CopyOutlined />}
                        size="small"
                        onClick={() => {
                            navigator.clipboard.writeText(url);
                            message.success("URL 已复制！");
                        }}
                    />
                </Space>
            ),
        },
        {
            title: "窗口 ID",
            dataIndex: "windowId",
            key: "windowId",
        },
        {
            title: "激活状态",
            dataIndex: "active",
            key: "active",
            render: (active: boolean) => (active ? "是" : "否"),
        },
        {
            title: "页面状态",
            dataIndex: "status",
            key: "status",
            render: (status: string) =>
                status === "complete" ? (
                    <Tag color="green">完成</Tag>
                ) : (
                    <Tag color="blue">加载中</Tag>
                ),
        },
        {
            title: "加载耗时 (ms)",
            dataIndex: "loadDuration",
            key: "loadDuration",
            render: (loadDuration?: number) =>
                loadDuration != null ? `${loadDuration} ms` : "未知",
        },
        {
            title: "操作",
            key: "action",
            render: (_: any, record: TabInfo) => (
                <Space>
                    <Button onClick={() => chrome.tabs.reload(record.id)}>刷新</Button>
                    <Button danger onClick={() => chrome.tabs.remove(record.id)}>关闭</Button>
                </Space>
            ),
        },
    ];

    return (
        <div>
            <Space style={{ marginBottom: 16 }}>
                <Select
                    defaultValue="normal"
                    style={{ width: 150 }}
                    onChange={handleFilterChange}
                >
                    <Option value="normal">普通页面</Option>
                    <Option value="extension">扩展页面</Option>
                    <Option value="all">全部页面</Option>
                </Select>
                <Input.Search
                    placeholder="搜索标签页"
                    value={search}
                    onChange={handleSearch}
                    style={{ width: 300 }}
                />
            </Space>
            <Table
                dataSource={filteredTabs}
                columns={columns}
                rowKey="id"
                pagination={false}
            />
        </div>
    );
}