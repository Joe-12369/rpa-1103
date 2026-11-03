import React from "react";
import { Card, Typography } from "antd";

export default function Home() {
    return (
        <Card>
            <Typography.Title level={5} style={{ marginTop: 0 }}>首页</Typography.Title>
            <Typography.Paragraph>后续展示消息/请求汇总。</Typography.Paragraph>
        </Card>
    );
}