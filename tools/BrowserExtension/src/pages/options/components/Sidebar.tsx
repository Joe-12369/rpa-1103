import React from "react";
import { Menu } from "antd";

interface Item {
    key: string;
    label: string;
    icon?: React.ReactNode;
}

export default function Sidebar(props: {
    items: Item[];
    active: string;
    onChange: (k: string) => void;
}) {
    return (
        <Menu
            mode="inline"
            selectedKeys={[props.active]}
            onClick={e => props.onChange(e.key)}
            items={props.items.map(i => ({
                key: i.key,
                icon: i.icon,
                label: i.label
            }))}
            style={{ borderRight: 0 }}
        />
    );
}