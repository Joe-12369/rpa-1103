import React, { useEffect, useState } from "react";
import { Card, Descriptions } from "antd";

interface Info {
    name: string;
    version: string;
    version_name: string;
    manifestVersion: number | string;
    permissions: number;
}

export default function ExtensionInfo() {
    const [info, setInfo] = useState<Info | null>(null);

    useEffect(() => {
        try {
            const m = chrome?.runtime?.getManifest?.();
            if (m) {
                setInfo({
                    name: m.name,
                    version: m.version,
                    version_name: m.version_name,
                    manifestVersion: m.manifest_version,
                    permissions: (m.permissions || []).length
                });
            }
        } catch {
            /* 非扩展环境忽略 */
        }
    }, []);

    return (
        <Card>
            {info ? (
                <Descriptions column={1} size="small" bordered>
                    <Descriptions.Item label="名称">{info.name}</Descriptions.Item>
                    <Descriptions.Item label="版本">{info.version}</Descriptions.Item>
                    <Descriptions.Item label="版本名称">{info.version_name}</Descriptions.Item>
                    <Descriptions.Item label="Manifest">{info.manifestVersion}</Descriptions.Item>
                    <Descriptions.Item label="权限数">{info.permissions}</Descriptions.Item>
                </Descriptions>
            ) : (
                "未获取到 manifest（可能不在扩展环境或权限不足）"
            )}
        </Card>
    );
}