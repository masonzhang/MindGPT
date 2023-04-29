import {Button} from "antd";
import {useEffect, useState} from "react";


export default function HomePage() {
    const [appInfo, setAppInfo] = useState<string>("")

    const {ipcRenderer} = window.require("electron");
    const test = () => {
        console.log("Test");
        ipcRenderer.send("app-info");
    }

    useEffect(() => {
        ipcRenderer.on("got-app-path", (event: string, path: string) => {
            const message = `This app is located at: ${path}`;
            setAppInfo(message);
        });
    }, []);

    return (
        <div>
            <h2>Yay! Welcome to umi!</h2>
            <p>
                <Button type="primary" onClick={test}>Test</Button>
                {appInfo}
            </p>
            <p>
                To get started, edit <code>pages/index.tsx</code> and save to reload.
            </p>
        </div>
    );
}
