import {Input, Spin} from "antd";
import {useCallback, useEffect, useState} from "react";
import {SendOutlined} from "@ant-design/icons";
import styles from '../index.less';
import ErrorBoundary from "../error-boundary";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import remarkMermaid from "remark-mermaidjs";

export default function HomePage() {
    const [loading, setLoading] = useState(false);
    const [progress, setProgress] = useState('empty');
    const {ipcRenderer} = window.require("electron");

    const onSearch = useCallback(async (goal) => {
        ipcRenderer.send("ask", goal);
    }, []);

    useEffect(() => {
        ipcRenderer.on("got-ask-progress", (event, data) => {
            console.log(data);
            setProgress(data[0]);
        });

        return () => {
            ipcRenderer.removeAllListeners("got-ask-progress");
        }
    }, []);

    return <div className={styles.ask}>
        <Spin spinning={loading}>
            <Input.Search size="large"
                          onSearch={onSearch}
                          placeholder="write your goal here"
                          enterButton={<SendOutlined/>}
            />
        </Spin>
        <div className={styles.ask}>
            <ErrorBoundary key={progress}>
                <ReactMarkdown
                    remarkPlugins={[remarkGfm, remarkMermaid]}
                    children={progress}
                />
            </ErrorBoundary>
        </div>
    </div>
}
