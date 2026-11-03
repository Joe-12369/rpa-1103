// TypeScript
import { createRoot } from "react-dom/client";
import { ConfigProvider, App as AntApp } from "antd";

import zhCN from "antd/locale/zh_CN";
import "antd/dist/reset.css";
import PopupApp from './App';


function Root() {
    return (
        <ConfigProvider locale={zhCN}>
            <AntApp>
                <PopupApp />
            </AntApp>
        </ConfigProvider>
    );
}

const el = document.getElementById("root")!;
createRoot(el).render(<Root />);