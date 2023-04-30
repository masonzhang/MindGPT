import {Alert, Col, Input, Row, Space, Spin} from "antd";
import {useCallback, useEffect, useRef, useState} from "react";
import {SendOutlined} from "@ant-design/icons";
import styles from '../index.less';
import ErrorBoundary from "../error-boundary";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import remarkMermaid from "remark-mermaidjs";
import * as signalR from '@microsoft/signalr';

export default function HomePage() {
    const [loading, setLoading] = useState(false);
    const [progress, setProgress] = useState('');
    const [output, setOutput] = useState('');
    const [connection, setConnection] = useState(null);
    const connectionRef = useRef(null);
    const [spinTip, setSpinTip] = useState('');

    const onSearch = useCallback(async (goal) => {
        connection.invoke('SetGoal', goal);
    }, [connection]);

    useEffect(() => {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl('/api/hub')
            .build();

        newConnection.on('OnGoalProgress', (progress, output) => {
            console.log(progress);
            setProgress(progress);
            console.log(output);
            setOutput(output);
        });

        newConnection.on('OnSpinTip', (tip) => {
            console.log(tip);
            setSpinTip(tip);
        });

        newConnection.start().then(() => {
            setConnection(newConnection);
            connectionRef.current = newConnection;
        });

        return () => {
            if (connectionRef.current) {
                connectionRef.current.stop();
            }
        };
    }, []);

    return <div className={styles.ask}>
        <Spin spinning={loading}>
            <Input.Search size="large"
                          defaultValue="write a book to introduce GPT, 5 chapters, and each chapter has 3-5 sections, each time you write only 1 section"
                          onSearch={onSearch}
                          placeholder="write your goal here"
                          enterButton={<SendOutlined/>}
            />
        </Spin>
        <Row className={styles.output}>
            <Col span={12}>
                <ErrorBoundary key={progress} className={styles.output} style={{background: "#EEE"}}>
                    <ReactMarkdown className={styles.output}
                        remarkPlugins={[remarkGfm, remarkMermaid]}
                        children={progress}
                    />
                </ErrorBoundary>
            </Col>
            <Col span={12}>
                {spinTip ? <Spin tip={spinTip}>
                        <Alert message="Work in progress" type=" info" style={{height: 100}}/>
                    </Spin>
                    : <div/>
                }
                <Input.TextArea className={styles.output} autoSize={{ minRows: 10}} key="output" value={output}></Input.TextArea>
            </Col>
        </Row>
    </div>
}
