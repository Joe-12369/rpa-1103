// TypeScript
import { createRoot } from "react-dom/client";
import { ConfigProvider, App as AntApp } from "antd";

import zhCN from "antd/locale/zh_CN";
import App from "./App";
import "antd/dist/reset.css";


function Root() {
    return (
        <ConfigProvider locale={zhCN}>
            <AntApp>
                <App />
            </AntApp>
        </ConfigProvider>
    );
}

const el = document.getElementById("root")!;
createRoot(el).render(<Root />);