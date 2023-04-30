import {useContext, useEffect} from "react";
import {GlobalContext} from "@/global-context";
import {Button} from "antd";


const DocsPage = () => {
    const { globalVariable, setGlobalVariable } = useContext(GlobalContext);

    // const {ipcRenderer} = window.require('electron');
    const test = () => {
        console.log("Test");
        // ipcRenderer.send("app-info");
    }

    useEffect(() => {
        // ipcRenderer.on("got-app-path", (event, path) => {
        //     const message = `This app is located at: ${path}`;
        //     console.log("message", message)
        //     setGlobalVariable({...globalVariable, appPath: path});
        // });
        //
        // return () => {
        //     ipcRenderer.removeAllListeners("got-app-path");
        // }
    }, []);

    return (
        <div>
            <h2>Yay! Welcome to umi!</h2>
            <p>
                <Button type="primary" onClick={test}>Test</Button>
                {globalVariable.appPath}
            </p>
            <p>
                To get started, edit <code>pages/index.tsx</code> and save to reload.
            </p>
        </div>
    );
};

export default DocsPage;
