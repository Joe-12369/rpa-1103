import React, { useEffect, useState } from 'react';
import {
    initRequestStore,
    getRetentionDates,
    getRequestsByDate,
    clearAllRequests,
    SaveEntity
} from '@utils/requestStore';
import {
    Card,
    Select,
    Input,
    Button,
    Table,
    Tabs,
    Tag,
    Space,
    message,
    Typography,
    theme,
    Divider
} from 'antd';
import {
    ReloadOutlined,
    DeleteOutlined,
    CloseOutlined,
    SearchOutlined,
    SendOutlined
} from '@ant-design/icons';

const { Text } = Typography;
const { Option } = Select;

function fmtTime(ts?: number | null): string {
    if (!ts) return '-';
    const d = new Date(ts);
    const hh = String(d.getHours()).padStart(2, '0');
    const mm = String(d.getMinutes()).padStart(2, '0');
    const ss = String(d.getSeconds()).padStart(2, '0');
    const ms = String(d.getMilliseconds()).padStart(3, '0');
    return `${hh}:${mm}:${ss}.${ms}`;
}

export default function ConsolePage(): JSX.Element {
    const { token } = theme.useToken();
    const [dates, setDates] = useState<string[]>([]);
    const [selectedDate, setSelectedDate] = useState<string>('');
    const [filter, setFilter] = useState<string>('');
    const [list, setList] = useState<SaveEntity[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [selectedItem, setSelectedItem] = useState<SaveEntity | null>(null);
    const [activeTab, setActiveTab] = useState<string>('request');
    const [editableRequestData, setEditableRequestData] = useState<string>('');

    const [pagination, setPagination] = useState({
        current: 1,
        pageSize: 15,
        total: 0
    });
    useEffect(() => {
        if (selectedItem) {
            setEditableRequestData(getRequestData(selectedItem));
        } else {
            setEditableRequestData('');
        }
    }, [selectedItem]);
    const handleResend = async () => {
        if (!selectedItem?.requestRaw) {
            message.warning('无请求数据');
            return;
        }
        let payloadData = { ...selectedItem.requestRaw };
        try {
            const editedJson = JSON.parse(editableRequestData);
            // 将编辑后的 SendData 覆盖回去
            payloadData.SendData = editedJson;
        } catch (e) {
            message.error('请求参数 JSON 格式错误，请检查');
            return;
        }

        try {
            if (chrome?.runtime?.sendMessage) {
                payloadData.connType = 'debugpage_tMsg';
                payloadData.MessageId = crypto.randomUUID();
                // 使用修改后的 payloadData
                const payload = { _svc: 'debugPage_resend', data: payloadData };
                chrome.runtime.sendMessage(payload);
                message.success('请求已重发');
            } else {
                message.error('无法发送消息，chrome.runtime.sendMessage 不可用');
            }

            handleRefresh();
        } catch (err) {
            console.error(err);
            message.error('重发失败');
        }
    };

    // 加载日期和默认数据
    async function loadDatesAndDefault() {
        setLoading(true);
        try {
            await initRequestStore();
            const ds = await getRetentionDates();
            setDates(ds);
            const def = ds[0] ?? '';
            setSelectedDate(def);
        } catch (err) {
            console.error(err);
            message.error('加载失败');
            setDates([]);
            setList([]);
        } finally {
            setLoading(false);
        }
    }

    // 按日期加载请求
    async function loadByDate(dateStr: string, page: number = 1, size: number = 20, filterStr: string = '') {
        setLoading(true);
        try {
            // 传入 filterStr
            const { total, list } = await getRequestsByDate(dateStr, size, page, filterStr);
            setList(list);
            setPagination(prev => ({ ...prev, current: page, pageSize: size, total }));
        } catch (err) {
            console.error(err);
            message.error('加载失败');
            setList([]);
            setPagination(prev => ({ ...prev, total: 0 }));
        } finally {
            setLoading(false);
        }
    }

    // 刷新
    const handleRefresh = () => {
        if (selectedDate) {
            // 刷新时带上当前的 filter
            loadByDate(selectedDate, pagination.current, pagination.pageSize, filter);
        } else {
            message.warning('请选择日期');
        }
    };

    // 清空
    const handleClear = async () => {
        try {
            await clearAllRequests();
            if (selectedDate) {
                await loadByDate(selectedDate, 1, pagination.pageSize, filter);
            }
            setSelectedItem(null);
            message.success('数据库已清空');
        } catch (err) {
            console.error(err);
            message.error('清空失败');
        }
    };

    // 过滤列表
    // const filteredList = () => {
    //     if (!filter) return list;
    //     const f = filter.toLowerCase();
    //     return list.filter(ent => {
    //         const method = String(ent.requestRaw?.SendData?.method ?? '').toLowerCase();
    //         return method.includes(f);
    //     });
    // };

    // 获取请求参数
    const getRequestData = (item: SaveEntity | null) => {
        if (!item?.requestRaw) return '{}';
        const data = item.requestRaw.SendData;
        try {
            return JSON.stringify(data, null, 2);
        } catch {
            return String(data);
        }
    };

    // 获取响应结果
    const getResponseData = (item: SaveEntity | null) => {
        if (!item?.responseRaw) return '{}';
        try {
            return JSON.stringify(item.responseRaw, null, 2);
        } catch {
            return String(item.responseRaw);
        }
    };

    // 获取错误信息
    const getErrorInfo = (item: SaveEntity | null) => {
        if (!item?.error) return '无异常';
        try {
            return JSON.stringify(item.error, null, 2);
        } catch {
            return String(item.error);
        }
    };

    // 获取调用链
    const getTrace = (item: SaveEntity | null) => {
        if (!item?.exception) return '无异常';
        return item.exception;
    };

    // 表格列定义 - 修复 Ant Design v5 兼容性
    const columns = [
        {
            title: 'MessageId',
            dataIndex: 'messageId',
            key: 'messageId',
            width: 120,
            render: (text: string) => {
                const shortText = text ? (text.length > 8 ? `${text.slice(0, 8)}...` : text) : '--';
                return (
                    <Text code ellipsis={{ tooltip: text || undefined }}>
                        {shortText}
                    </Text>
                );
            },
        },
        {
            title: '方法',
            dataIndex: 'method',
            key: 'method',
            width: 200,
            render: (method: string) => method || '-'
        },
        {
            title: '请求时间',
            dataIndex: 'requestTime',
            key: 'requestTime',
            width: 100,
        },
        {
            title: '耗时',
            dataIndex: 'duration',
            key: 'duration',
            width: 100,
        },
        {
            title: '状态',
            dataIndex: 'status',
            key: 'status',
            width: 80,
            render: (status: string | number) => {
                const statusStr = String(status);
                const isSuccess = statusStr === '200' || statusStr === '0';
                return (
                    <Tag color={isSuccess ? 'success' : 'error'}>
                        {status}
                    </Tag>
                );
            },
        },
    ];

    // 表格数据
    const tableData = list.map((ent, index) => ({
        key: ent.id ?? `${ent.requestTimestamp}-${index}`,
        messageId: ent.messageId ? String(ent.messageId) : '',
        method: ent.requestRaw?.SendData?.method ?? '-',
        requestTime: fmtTime(ent.requestTimestamp),
        duration: ent.responseTimestamp && ent.requestTimestamp
            ? `${ent.responseTimestamp - ent.requestTimestamp} ms`
            : ent.responseTimestamp == null ? 'pending' : '-',
        status: ent.responseRaw?.code ?? '-',
        original: ent,
    }));

    // 详情页标签项
    const getTabItems = () => {
        const commonStyle = {
            margin: 0,
            fontSize: token.fontSizeSM,
            fontFamily: 'monospace',
            whiteSpace: 'pre-wrap' as const,
            wordBreak: 'break-all' as const,
            maxHeight: 500,
            overflow: 'auto' as const,
            padding: token.paddingSM,
            backgroundColor: token.colorFillAlter,
            borderRadius: token.borderRadius,
            color: token.colorText,
            border: `1px solid ${token.colorBorderSecondary}`
        };

        return [
            {
                key: 'request',
                label: '请求参数',
                children: (
                    <Input.TextArea
                        style={{
                            ...commonStyle,
                            minHeight: 300,
                            resize: 'vertical',
                            backgroundColor: token.colorBgContainer,
                        }}
                        value={editableRequestData}
                        onChange={(e) => setEditableRequestData(e.target.value)}
                        spellCheck={false}
                        placeholder="在此编辑请求参数 JSON"
                    />
                ),
            },
            {
                key: 'response',
                label: '响应结果',
                children: (
                    <pre style={commonStyle}>
                        {getResponseData(selectedItem)}
                    </pre>
                ),
            },
            // {
            //     key: 'trace',
            //     label: '调用异常',
            //     children: (
            //         <pre style={commonStyle}>
            //             {getTrace(selectedItem)}
            //         </pre>
            //     ),
            // },
            {
                key: 'error',
                label: '浏览器异常',
                children: (
                    <pre style={commonStyle}>
                        {getErrorInfo(selectedItem)}
                    </pre>
                ),
            }
        ];
    };

    // 初始化
    useEffect(() => {
        loadDatesAndDefault();
    }, []);

    useEffect(() => {
        // 动态添加样式覆盖 Ant Design Layout 的默认 overflow
        const style = document.createElement('style');
        style.innerHTML = `
            .ant-layout-content {
                overflow: hidden !important;
            }
            main {
                overflow: hidden !important;
            }
            /* 确保 ConsolePage 所在容器也不滚动 */
            [class*="ant-layout"] [class*="ConsolePage"] {
                overflow: hidden !important;
            }
        `;
        document.head.appendChild(style);

        return () => {
            document.head.removeChild(style);
        };
    }, []);

    useEffect(() => {
        // 切换日期时，重置页码为1，但保留过滤条件（或者也可以选择清空 filter）
        if (selectedDate) loadByDate(selectedDate, 1, pagination.pageSize, filter);
    }, [selectedDate]);

    return (
        <div style={{
            display: 'flex',
            flexDirection: 'column',
            height: '100vh',
            width: '100%',
            overflow: 'hidden'
        }}>
            {/* 工具栏 */}
            <div style={{
                marginBottom: token.margin,
                padding: `0 ${token.padding}px`,
                flexShrink: 0,
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                width: '100%'
            }}>
                <Space>
                    <Select
                        style={{ width: 120 }}
                        value={selectedDate}
                        onChange={setSelectedDate}
                        placeholder="选择日期"
                        allowClear
                    >
                        {dates.map(date => (
                            <Option key={date} value={date}>{date}</Option>
                        ))}
                    </Select>

                    <Input
                        style={{ width: 200 }}
                        placeholder="按方法名过滤"
                        value={filter}
                        onChange={e => setFilter(e.target.value)}
                        onPressEnter={handleRefresh} // 增加回车搜索
                        prefix={<SearchOutlined />}
                        allowClear
                    />

                    <Button
                        icon={<ReloadOutlined />}
                        onClick={handleRefresh}
                    >
                        刷新
                    </Button>
                </Space>

                <Button
                    type="primary"
                    danger
                    icon={<DeleteOutlined />}
                    onClick={handleClear}
                >
                    清空数据
                </Button>
            </div>

            {/* 主内容区 */}
            <div style={{
                flex: 1,
                display: 'flex',
                minHeight: 0,
                overflow: 'hidden',
                width: '100%',
                padding: `0 ${token.padding}px ${token.padding}px`
            }}>
                <Card
                    bodyStyle={{
                        padding: 0,
                        display: 'flex',
                        flexDirection: 'column',
                        flex: 1,
                        overflow: 'hidden',
                        width: '100%'
                    }}
                    style={{
                        flex: 1,
                        display: 'flex',
                        overflow: 'hidden',
                        width: '100%'
                    }}
                >
                    <div style={{
                        display: 'flex',
                        flex: 1,
                        minHeight: 0,
                        overflow: 'hidden',
                        width: '100%'
                    }}>
                        {/* 左侧表格区域 */}
                        <div style={{
                            flex: selectedItem ? 3 : 1,
                            minWidth: 0,
                            display: 'flex',
                            flexDirection: 'column',
                            overflow: 'hidden',
                            width: '100%'
                        }}>
                            <div style={{
                                flex: 1,
                                overflow: 'hidden',
                                padding: token.padding,
                                display: 'flex',
                                flexDirection: 'column',
                                width: '100%'
                            }}>
                                {/* 表格容器 - 只有这里允许滚动 */}
                                <div style={{
                                    flex: 1,
                                    overflow: 'auto',
                                    width: '100%',
                                    position: 'relative'
                                }}>
                                    <Table
                                        size="small"
                                        columns={columns}
                                        dataSource={tableData}
                                        loading={loading}
                                        pagination={{
                                            current: pagination.current,
                                            pageSize: pagination.pageSize,
                                            total: pagination.total,
                                            onChange: (page, size) => {
                                                if (selectedDate) {
                                                    loadByDate(selectedDate, page, size, filter);
                                                }
                                            },
                                            showSizeChanger: false,
                                            showTotal: (total) => `共 ${total} 条`
                                        }}
                                        // scroll={{ y: undefined, x: 'max-content' }}
                                        onRow={(record) => ({
                                            onClick: () => setSelectedItem(record.original),
                                            style: {
                                                cursor: 'pointer',
                                            },
                                        })}
                                        rowClassName={(record) =>
                                            selectedItem?.id === record.original.id ? 'ant-table-row-selected' : ''
                                        }
                                        locale={{ emptyText: '无记录' }}
                                        style={{
                                            height: '100%',
                                            minHeight: '100%',
                                            width: '100%'
                                        }}
                                    />
                                </div>
                            </div>
                        </div>

                        {/* 右侧详情面板 */}
                        {selectedItem && (
                            <>
                                <Divider type="vertical" style={{ height: '100%', margin: 0 }} />
                                <div style={{
                                    flex: 2,
                                    minWidth: 400,
                                    display: 'flex',
                                    flexDirection: 'column',
                                    overflow: 'hidden'
                                }}>
                                    <div style={{
                                        flex: 1,
                                        padding: token.padding,
                                        overflow: 'hidden',
                                        display: 'flex',
                                        flexDirection: 'column'
                                    }}>
                                        <div style={{
                                            display: 'flex',
                                            justifyContent: 'space-between',
                                            alignItems: 'center',
                                            marginBottom: token.margin,
                                            flexShrink: 0
                                        }}>
                                            <h4 style={{ margin: 0, color: token.colorText }}>请求详情</h4>
                                            <Space split={<Divider type="vertical" />}>
                                                <Button
                                                    type="text"
                                                    size="small"
                                                    icon={<SendOutlined />}
                                                    onClick={handleResend}
                                                    style={{ color: token.colorPrimary }}
                                                >
                                                    重发
                                                </Button>
                                                <Button
                                                    type="text"
                                                    icon={<CloseOutlined />}
                                                    onClick={() => setSelectedItem(null)}
                                                    size="small"
                                                    style={{ color: token.colorTextTertiary }}
                                                />
                                            </Space>
                                        </div>

                                        <div style={{
                                            flex: 1,
                                            minHeight: 0,
                                            display: 'flex',
                                            flexDirection: 'column',
                                            overflow: 'hidden'
                                        }}>
                                            <Tabs
                                                activeKey={activeTab}
                                                onChange={setActiveTab}
                                                items={getTabItems()}
                                                style={{ flex: 1 }}
                                                tabBarStyle={{ marginBottom: token.marginSM }}
                                            />
                                        </div>
                                    </div>
                                </div>
                            </>
                        )}
                    </div>
                </Card>
            </div>
        </div>
    );
}